using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobImages.AzureBlob
{
  public interface IAzureBlobClient
  {
    public Task<bool> DeleteBlobAsync(string name, string containerName);

    public Task<IEnumerable<string>> GetAllBlobsAsync(string containerName);

    public Task<string> UploadBlobAsync(string name, string imageBlob, string containerName);
  }
}
