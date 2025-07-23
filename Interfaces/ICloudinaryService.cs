﻿namespace Ecom_api.Interfaces
{
    public interface ICloudinaryService
    {
        Task<(bool Success, string Url, string ErrorMessage)> UploadImageAsync(IFormFile file);
        Task<bool> DeleteImageAsync(string publicId);
    }
}
