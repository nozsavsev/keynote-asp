using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using keynote_asp;
using keynote_asp.AuthHandlers;
using keynote_asp.AuthHandlers.AuthorizationRequirments;
using keynote_asp.DbContexts;
using keynote_asp.Exceptions;
using keynote_asp.Models.User;
using keynote_asp.Repositories;
using keynote_asp.Services;
using keynote_asp.Services.ObjectStorage;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Keynote_asp.Nauth.API_GEN;
using Microsoft.Kiota.Abstractions;
using keynote_asp.Helpers;
using Keynote_asp.Nauth.API_GEN.Models;

namespace keynote_asp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddEnvironmentVariables();

            // Configure JSON serialization
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };
            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            builder.Services.AddSingleton(jsonSerializerOptions);

            // Add services to the container
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = jsonSerializerOptions.PropertyNamingPolicy;
                    options.JsonSerializerOptions.WriteIndented = jsonSerializerOptions.WriteIndented;
                    foreach (var converter in jsonSerializerOptions.Converters)
                    {
                        options.JsonSerializerOptions.Converters.Add(converter);
                    }
                });

            // Add DbContext
            builder.Services.AddDbContext<KeynoteDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add AutoMapper
            builder.Services.AddAutoMapper(cfg =>
            {
                cfg.LicenseKey = builder.Configuration["AutoMapper:licenceKey"];
                cfg.AddMaps(typeof(Program));
            });

            // Register Repositories
            builder.Services.AddScoped<UserRepository>();
            builder.Services.AddScoped<KeynoteRepository>();

            // Register Services
            builder.Services.AddScoped<IObjectStorageService, ObjectStorageService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<KeynoteService>();

            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<CachedCurrentService>();
            builder.Services.AddScoped<INauthApiService, NauthApiService>();

            // Configure Nauth API Client
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IAuthenticationProvider, ServiceTokenAuthenticationProvider>();
            builder.Services.AddScoped<IRequestAdapter>(sp =>
            {
                var client = sp.GetRequiredService<HttpClient>();
                var authProvider = sp.GetRequiredService<IAuthenticationProvider>();
                var nauthBaseUrl = builder.Configuration["Nauth:BaseUrl"];
                return new HttpClientRequestAdapter(authProvider, httpClient: client)
                {
                    BaseUrl = nauthBaseUrl
                };
            });
            builder.Services.AddScoped<ApiClient>(sp =>
            {
                var adapter = sp.GetRequiredService<IRequestAdapter>();
                return new ApiClient(adapter);
            });

            builder.Services.AddSignalR();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddHttpContextAccessor();

            // Configure CORS
            var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: myAllowSpecificOrigins,
                    policy =>
                    {
                        var corsOrigins = builder.Configuration.GetSection("CorsConfig").Value;
                        if (!string.IsNullOrEmpty(corsOrigins))
                        {
                            var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                  .Select(o => o.Trim())
                                                  .ToArray();
                            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                        }
                        else if (builder.Environment.IsDevelopment())
                        {
                            policy.WithOrigins("http://localhost:3000", "http://localhost:5035")
                                  .AllowAnyHeader()
                                  .AllowAnyMethod()
                                  .AllowCredentials();
                        }
                    });
            });

            // Configure Authentication
            builder.Services
                .AddAuthentication("NauthScheme")
                .AddScheme<NauthAuthenticationOptions, NauthAuthenticationHandler>("NauthScheme", options =>
                {
                    options.CookieKey = builder.Configuration["JWT:Cookiekey"] ?? "nauth";
                });

            // Configure Authorization
            builder.Services.AddAuthorization(options =>
            {
                // Default policy - requires authenticated user with verified email, enabled, and 2FA confirmed
                options.DefaultPolicy = new AuthorizationPolicyBuilder("NauthScheme")
                    .RequireAuthenticatedUser()
                    .AddRequirements(new BaseAuthorizationRequirement(
                        requireEmailVerified: true,
                        requireEnabled: true,
                        require2FAConfirmed: true))
                    .Build();

                // Policy for allowing users without verified email
                options.AddPolicy("AllowNoVerifiedEmail", policy =>
                {
                    policy.AuthenticationSchemes.Add("NauthScheme");
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new BaseAuthorizationRequirement(
                        requireEmailVerified: false,
                        requireEnabled: true,
                        require2FAConfirmed: true));
                });


                // Create policies for each Keynote permission
                foreach (KeynotePermissions permission in Enum.GetValues(typeof(KeynotePermissions)))
                {
                    var permissionKey = permission.ToString();
                    options.AddPolicy(permissionKey, policy =>
                    {
                        policy.AuthenticationSchemes.Add("NauthScheme");
                        policy.RequireAuthenticatedUser();
                        policy.AddRequirements(new HasPermissionRequirement(permissionKey));
                    });
                }
            });

            // Register Authorization Handlers
            builder.Services.AddScoped<IAuthorizationHandler, BaseRequirementHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, HasPermissionHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, UserOwnsKeynoteHandler>();

            var app = builder.Build();

            app.UseCors(myAllowSpecificOrigins);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Global error handling middleware
            app.UseMiddleware<ErrorHandlerMiddleware>(jsonSerializerOptions);

            // Handle authentication failures
            app.UseStatusCodePages(async context =>
            {
                var response = context.HttpContext.Response;
                var requestServices = context.HttpContext.RequestServices;
                var config = requestServices.GetRequiredService<IConfiguration>();
                var jsonOptions = requestServices.GetRequiredService<JsonSerializerOptions>();
                var logger = requestServices.GetRequiredService<ILogger<Program>>();

                response.ContentType = "application/json";

                if (response.StatusCode == 403)
                {
                    var reasons = context.HttpContext.GetAuthenticationFailureReasons();
                    
                    await response.WriteAsync(JsonSerializer.Serialize(
                        new ResponseWrapper<string>(WrResponseStatus.Forbidden, null, reasons), 
                        jsonOptions));
                }
                else if (response.StatusCode == 401)
                {
                    
                    await response.WriteAsync(JsonSerializer.Serialize(
                        new ResponseWrapper<string>(WrResponseStatus.Unauthorized), 
                        jsonOptions));
                }
            });

            // Add request logging middleware
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                await next();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<SignalRHubs.AuthHub>("/authhub");

            // Initialize permissions on startup
            using (var scope = app.Services.CreateScope())
            {
                var nauthAPI = scope.ServiceProvider.GetRequiredService<INauthApiService>();
                var currentService = scope.ServiceProvider.GetRequiredService<CachedCurrentService>();

                var service = await currentService.GetCurrentServiceAsync();

                foreach (var name in  Enum.GetNames<KeynotePermissions>())
                {

                    var permission = new CreatePermissionDTO();

                    permission.ServiceId = service!.Id!.ToString();
                    permission.Key = name;
                    permission.Name = name.Replace("Pr", "").SplitPascalCase();

                    await nauthAPI.CreateServicePermissionAsync(permission);

                }

            }

            await app.RunAsync();
        }
    }

    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger, JsonSerializerOptions jsonSerializerOptions)
        {
            _next = next;
            _logger = logger;
            _jsonSerializerOptions = jsonSerializerOptions;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                Console.WriteLine("ErrorHandlerMiddleware invoked.");
                await _next(context);
                Console.WriteLine("ErrorHandlerMiddleware invoked.");
            }
            catch (Exception error)
            {
                Console.WriteLine("Exception caught in ErrorHandlerMiddleware: " + error.Message);

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started, the error handler will not be executed.");
                    throw;
                }

                var response = context.Response;
                response.ContentType = "application/json";

                object responseBody;

                switch (error)
                {
                    case KeynoteException authEx:
                        response.StatusCode = StatusCodes.Status400BadRequest;
                        responseBody = new ResponseWrapper<string>(authEx.Status, authEx.Message);
                        break;
                    default:
                        response.StatusCode = StatusCodes.Status500InternalServerError;
                        _logger.LogError(error, "An unhandled exception has occurred.");
                        responseBody = new ResponseWrapper<string>(WrResponseStatus.InternalError);
                        break;
                }

                var result = JsonSerializer.Serialize(responseBody, _jsonSerializerOptions);
                await response.WriteAsync(result);
            }
        }
    }
}