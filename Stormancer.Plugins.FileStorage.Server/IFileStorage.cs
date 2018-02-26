using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormancer.Server.FileStorage
{
    public interface IFileStorage
    {
        Task UploadFile(string path, Stream content, string mimeType);

        Task<FileDescription> DownloadFile(string path);

        Task<Uri> GetDownloadUrl(string path);

        Task DeleteFile(string path);
    }

    public class FileDescription
    {
        public string Path { get; set; }
        public string ContentType { get; set; }
        public Stream Content { get; set; }
    }
}
