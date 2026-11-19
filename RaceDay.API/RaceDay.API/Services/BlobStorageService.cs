using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    // handling all image uploads to azure blob storage through the api
    public class BlobStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        // reading the azure connection string and container name from user secrets
        public BlobStorageService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureBlobStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Blob Storage connection string is missing.");
            _containerName = configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        }

        // uploading an image file to azure blob storage and returning its public url
        public async Task<string> UploadFileAsync(IFormFile file, string folderPrefix)
        {
            // connecting to the blob container and creating it if it doesn't exist yet
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // generating a unique file name so uploaded images don't overwrite each other
            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{folderPrefix}/{Guid.NewGuid()}{extension}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // uploading the image stream to azure and setting the content type so browsers display it
            using var stream = file.OpenReadStream();
            var httpHeaders = new BlobHttpHeaders { ContentType = file.ContentType };
            await blobClient.UploadAsync(stream, httpHeaders);

            // returning the public blob url to save in sql server
            return blobClient.Uri.ToString();
        }
    }
}