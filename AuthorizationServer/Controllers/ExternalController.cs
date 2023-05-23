using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AuthorizationServer.Controllers
{
    public class ExternalController : Controller
    {
        [HttpGet]
        public IActionResult Challenge(string scheme, string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl)) returnUrl = "~/";

            // validate returnUrl - either it is a valid OIDC URL or back to a local page
            if (Url.IsLocalUrl(returnUrl) == false)
            {
                // user might have clicked on a malicious link - should be logged
                throw new Exception("invalid return URL");
            }

            // start challenge and roundtrip the return URL and scheme 
            var props = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback)),
                Items =
                {
                    { "returnUrl", returnUrl },
                    { "scheme", scheme },
                }
            };

            return Challenge(props, scheme);

        }

        public async Task<IActionResult> Callback()
        {
            // Read external identity from the temporary cookie
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var returnUrl = result.Properties.Items["returnUrl"] ?? "~/";
            if (result?.Succeeded != true)
            {
                throw new Exception("External authentication error");
            }

            var claimsIdentity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

            if (result.Principal != null)
            {
                //Now add the values on claim and redirect to the page to be accessed after successful login
                claimsIdentity.AddClaim(result.Principal.FindFirst(ClaimTypes.Name));
                claimsIdentity.AddClaim(result.Principal.FindFirst(ClaimTypes.Email));

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            }            

            return Redirect(returnUrl);
        }
 
    }
}
