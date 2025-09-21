using Amazon.Runtime;
using Amazon.S3;

namespace keynote_asp.Services.ObjectStorage
{
    public class ObjectStorageService(IConfiguration config, ILogger<ObjectStorageService> log) : IObjectStorageService
    {
        private readonly string _access = config["Amazon:ACCESS_KEY"]!;
        private readonly string _secret = config["Amazon:SECRET_KEY"]!;
        private readonly string _region = config["Amazon:region"]!;

        private AmazonS3Client _s3Client
        {
            get
            {
                BasicAWSCredentials credentials = new BasicAWSCredentials(_access, _secret);
                return new AmazonS3Client(credentials, Amazon.RegionEndpoint.GetBySystemName(_region));
            }
        }

        public async Task<bool> UploadFileAsync(string bucketName, string key, Stream fileStream, string contentType)
        {

            var request = new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                ContentType = contentType,
                InputStream = fileStream,
            };

            try
            {

                var result = await _s3Client.PutObjectAsync(request);

                log.LogInformation("Uploaded file {key} to bucket {bucketName} with status code {statusCode}", key, bucketName, result.HttpStatusCode);

                return result.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error uploading file {key} to bucket {bucketName}", key, bucketName);
                return false;
            }

        }

        public async Task<bool> FileExistsAsync(string bucketName, string key)
        {

            var request = new Amazon.S3.Model.GetObjectMetadataRequest
            {
                BucketName = bucketName,
                Key = key
            };
            try
            {
                var response = await _s3Client.GetObjectMetadataAsync(request);
                return true;
            }
            catch (Amazon.S3.AmazonS3Exception e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                else
                {
                    log.LogError(e, "Error checking if file {key} exists in bucket {bucketName}", key, bucketName);
                    return false;
                }
            }

        }


        public async Task DeleteFileAsync(string bucketName, string key)
        {

            var request = new Amazon.S3.Model.DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };
            try
            {
                var response = await _s3Client.DeleteObjectAsync(request);
                log.LogInformation("Deleted file {key} from bucket {bucketName} with status code {statusCode}", key, bucketName, response.HttpStatusCode);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error deleting file {key} from bucket {bucketName}", key, bucketName);
            }

        }

    }
}
