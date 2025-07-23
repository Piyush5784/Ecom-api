using Microsoft.AspNetCore.Identity;
using Ecom_api.Models;

namespace Ecom_api.Interfaces
{
    public interface IEmailSenderApplicationInterface
    {
        Task SendContactMessageToAdminAsync(Contact contact);
    }
}
