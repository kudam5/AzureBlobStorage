using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobImages.AzureBlob
{

  public class AzureBlobClient: IAzureBlobClient
  {
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _azureStorageConnectionString = "Your_ConnectionString";

    public AzureBlobClient()
    {
      _blobServiceClient = new BlobServiceClient(_azureStorageConnectionString);
    }

    public async Task<bool> DeleteBlobAsync(string name, string containerName)
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

      var client = containerClient.GetBlobClient(name);

      bool result = await client.DeleteIfExistsAsync();
      return result;
    }

    public async Task<IEnumerable<string>> GetAllBlobsAsync(string containerName)
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
    
      var files = new List<string>();

      var blobs = containerClient.GetBlobsAsync();

      await foreach (var blob in blobs)
      {
        files.Add(blob.Name);
      }
      return files;
    }

    public Task<string> GetBlobAsync(string name, string containerName)
    {
     
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

      var blob = containerClient.GetBlobClient(name);

      return Task.FromResult($"{blob.Uri.AbsoluteUri}");
    }

    public async Task<string> UploadBlobAsync(string name, string imageBlob, string containerName)
    {

      var imageValidation = ValidateImage(imageBlob);

      if (imageValidation.isValid == false)
        return imageValidation.errorMessage;

      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

      var imageCorrellationId = Guid.NewGuid();

      var fileName = $"{imageCorrellationId}/{name}.{imageValidation.imageType}";

      //if file exists it will be replaced
      var blobClient = containerClient.GetBlobClient(fileName);

      var blobHttpHeaders = new BlobHttpHeaders
      {
        ContentType = $"image/{imageValidation.imageType}"
      };

      var res = await blobClient.UploadAsync(BinaryData.FromBytes(imageValidation.byteImage).ToStream(), blobHttpHeaders);
    
      if (res is null)
        return string.Empty;

      return fileName;

      //store the meta data in the DB.

    }

    private static (byte[] byteImage, string errorMessage, bool isValid, ImageTypes? imageType) ValidateImage(string imageBlob)
    {
      byte[] imageByte;
      System.Drawing.Image image;

      //validate image value. 
      try
      {
        imageByte = Convert.FromBase64String(imageBlob);

        // size in bytes : 5kb = 512 000 bytes
        var imageMemorySize = (imageByte.Length);
        if (imageMemorySize > 5024000) //5mb
        {
          return (byteImage: null, errorMessage: "Image cannot be greater than 5mb", isValid: false, imageType:null);
        }

        image = ConvertToImage(imageByte);

      }
      catch (Exception ex)
      {
        return (image: null, errorMessage: "Invalid image", isValid: false, ImageTypes: null);

      }

      var type = GetImageType(image);
      if (type == ImageTypes.Unknown)
      {
        return (byteImage: null, errorMessage: "Invalid image format, format must be PNG or JPEG.", isValid: false, ImageTypes:null);        
      }

      return (byteImage: imageByte, errorMessage: string.Empty, isValid: true, ImageTypes: type);

    }

    private static System.Drawing.Image ConvertToImage(byte[] byteValue)
    {
      System.Drawing.Image image;

      using (MemoryStream ms = new MemoryStream(byteValue))
      {
        image = System.Drawing.Image.FromStream(ms);
      }

      return image;
    }

    private static ImageTypes GetImageType(System.Drawing.Image image)
    {
      if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Jpeg))
      {
        return ImageTypes.JPEG;
      }
      else if (image.RawFormat.Equals(System.Drawing.Imaging.ImageFormat.Png))
      {
        return ImageTypes.PNG;
      }
      else
      {
        return ImageTypes.Unknown;
      }
    }

    private static System.Drawing.Image ReSizeImage(System.Drawing.Image imageToResize, System.Drawing.Size size)
    {
      return (System.Drawing.Image)(new System.Drawing.Bitmap(imageToResize, size));
    }

    private static byte[] ConvertToByte(System.Drawing.Image image)
    {
      using (MemoryStream mStream = new MemoryStream())
      {
        image.Save(mStream, image.RawFormat);
        return mStream.ToArray();
      }
    }


    /// <summary>
    /// Represents the image types accepted
    /// </summary>
    public enum ImageTypes
    {
      /// <summary>
      /// Defines the common image extension (Joint Photographic Experts Group)
      /// </summary>
      JPEG = 1,

      /// <summary>
      /// Defines the common image extension (Portable Network Graphics)
      /// </summary>
      PNG = 2,

      /// <summary>
      /// Unsupported image type
      /// </summary>
      Unknown = 3
    }
  }
}

