using Microsoft.WindowsAzure.Storage.Blob;
using Server.Plugins.Configuration;
using Stormancer.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server.FileStorage
{
    class AzureBlobFileStorage : IFileStorage
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private string connectionString;
        private string container;
        private static bool _containerCreated;

        public AzureBlobFileStorage(IConfiguration configuration, ILogger logger)
        {
            _logger = logger;
            _configuration = configuration;
            _configuration.SettingsChanged += (sender, e) => ApplyConfig();
            ApplyConfig();
        }

        private void ApplyConfig()
        {
            connectionString = (string)(_configuration.Settings?.fileStorage?.azureBlob?.connectionString);
            container = (string)(_configuration.Settings?.fileStorage?.azureBlob?.container);
        }

        private async Task<CloudBlobContainer> GetBlobContainer()
        {
            try
            {
                var account = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(connectionString);

                var client = account.CreateCloudBlobClient();
                var container =  client.GetContainerReference(this.container);

                if (!_containerCreated)
                {
                   
                    await container.CreateIfNotExistsAsync();
                    _containerCreated = true;
                }
                return container;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "storage", "Failed to create Azure blob storage client. Check the configuration 'fileStorage.azureBlob.connectionString'", ex);
                throw;
            }
        }
        public async Task<FileDescription> DownloadFile(string path)
        {
            var container = await GetBlobContainer();

            var blob = await container.GetBlobReferenceFromServerAsync(path);

            var stream = await blob.OpenReadAsync();
            return new FileDescription { Path = path, Content = stream, ContentType = blob.Properties.ContentType };
        }

        public async Task<Uri> GetDownloadUrl(string path)
        {
            if(path == null)
            {
                throw new ArgumentNullException("path");
            }

            var container = await GetBlobContainer();

            var blob = container.GetBlobReference(path);

            //Blob is private, create a link;
            var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTime.UtcNow + TimeSpan.FromHours(1)
            });
                      
            return new Uri(blob.Uri.AbsoluteUri + sas);
        }

        public async Task UploadFile(string path, Stream content, string mimeType)
        {
            var container = await GetBlobContainer();

            var blobReference = container.GetBlockBlobReference(path);

            await blobReference.UploadFromStreamAsync(content);
            blobReference.Properties.ContentType = mimeType;
            await blobReference.SetPropertiesAsync();
        }

        public async Task DeleteFile(string path)
        {
            var container = await GetBlobContainer();

            var blobReference = container.GetBlobReference(path);

            await blobReference.DeleteAsync();
        }
    }
}
