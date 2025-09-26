using keynote_asp.Dtos;
using keynote_asp.Helpers;
using keynote_asp.Models.Keynote;
using keynote_asp.Repositories;
using keynote_asp.Services.ObjectStorage;
using Keynote_asp.Nauth.API_GEN.Models;

namespace keynote_asp.Services
{
    public class KeynoteService(KeynoteRepository repository, IConfiguration config, IObjectStorageService storage) : GenericService<DB_Keynote>(repository)
    {


        public async Task<DB_Keynote> AddAsync(CreateKeynoteDTO keynote, UserDTO user)
        {
            var dbKeynote = new DB_Keynote();

            dbKeynote.Name = keynote.Name;
            dbKeynote.Description = keynote.Description;
            dbKeynote.UserId = long.Parse(user.Id);

            var stream = keynote.Keynote.OpenReadStream();

            dbKeynote.TotalFrames = PDF.CountPages(stream);

            stream.Position = 0;

            await storage.UploadFileAsync(config["Amazon:bucketName"]!, $"keynotes/{dbKeynote.Id}/keynote.pdf", stream, "application/pdf");
            dbKeynote.KeynoteUrl = $"https://{config["Amazon:bucketName"]}.s3.{config["Amazon:region"]}.amazonaws.com/keynotes/{dbKeynote.Id}/keynote.pdf";

            if (keynote.PresentorNotes != null)
            {
                await storage.UploadFileAsync(config["Amazon:bucketName"]!, $"keynotes/{dbKeynote.Id}/presentor-notes.pdf", keynote.PresentorNotes.OpenReadStream(), "application/pdf");
                dbKeynote.PresentorNotesUrl = $"https://{config["Amazon:bucketName"]}.s3.{config["Amazon:region"]}.amazonaws.com/keynotes/{dbKeynote.Id}/presentor-notes.pdf";
            }

            if (keynote.MobileKeynote != null)
            {
                await storage.UploadFileAsync(config["Amazon:bucketName"]!, $"keynotes/{dbKeynote.Id}/keynote-mobile.pdf", keynote.MobileKeynote.OpenReadStream(), "application/pdf");
                dbKeynote.MobileKeynoteUrl = $"https://{config["Amazon:bucketName"]}.s3.{config["Amazon:region"]}.amazonaws.com/keynotes/{dbKeynote.Id}/keynote-mobile.pdf";
            }

            dbKeynote = await _repository.AddAsync(dbKeynote);

            return dbKeynote;
        }

        public override async Task DeleteByidAsync(long id)
        {
            var keynote = GetByIdAsync(id);

            await storage.DeleteFileAsync(config["Amazon:bucketName"]!, $"keynotes/{id}/keynote.pdf");
            await storage.DeleteFileAsync(config["Amazon:bucketName"]!, $"keynotes/{id}/presentor-notes.pdf");
            await storage.DeleteFileAsync(config["Amazon:bucketName"]!, $"keynotes/{id}/keynote-mobile.pdf");

            await base.DeleteByidAsync(id);
        }


    }
}
