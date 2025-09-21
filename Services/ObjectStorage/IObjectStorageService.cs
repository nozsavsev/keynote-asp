namespace keynote_asp.Services.ObjectStorage
{
    public interface IObjectStorageService
    {

        public Task<bool> UploadFileAsync(string bucketName, string key, Stream fileStream, string contentType);
        public Task<bool> FileExistsAsync(string bucketName, string key);
        public Task DeleteFileAsync(string bucketName, string key);

    }
}
