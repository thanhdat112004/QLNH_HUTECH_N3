using DoAN.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace DoAN.Areas.Admin.Controllers
{
    public class HashPasswordAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionArguments.ContainsKey("user") && context.ActionArguments.ContainsKey("password"))
            {
                var user = context.ActionArguments["user"] as User;
                var password = context.ActionArguments["password"] as string;

                if (user != null && !string.IsNullOrEmpty(password))
                {
                    using var sha = SHA256.Create();
                    var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                    user.PasswordHash = Convert.ToHexString(hashBytes);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}