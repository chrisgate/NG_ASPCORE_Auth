using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ng_Core_Auth.Helpers;
using Ng_Core_Auth.Models;

namespace Ng_Core_Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signManager;
        private readonly AppSettings _appSettings;

        public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signManager,IOptions<AppSettings> appSettings)
        {
            this._userManager = userManager;
            this._signManager = signManager;
            this._appSettings = appSettings.Value;
        }
        [HttpPost("[action]")]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel formData)
        {
            //Will hold all errors related to Registration
            List<string> errorList = new List<string>();
            var user = new IdentityUser
            {
                Email = formData.Email,
                UserName = formData.Username,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var result = await _userManager.CreateAsync(user, formData.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
                return Ok(new { username = user.UserName, email = user.Email, status = 1, message = "Registration Successfully" });
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                    errorList.Add(error.Description);
                }
            }
            return BadRequest(new JsonResult(errorList));
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel formData)
        {
            //Get the User from Database
            var user = await _userManager.FindByNameAsync(formData.Username);
            var roles = await _userManager.GetRolesAsync(user);
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_appSettings.Secret));
            double tokenExpiryTime = Convert.ToDouble(_appSettings.ExpireTime);

            if (user != null && await _userManager.CheckPasswordAsync(user,formData.Password))
            {
                // COnfirmation of email

                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(JwtRegisteredClaimNames.Sub,formData.Username),
                        new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.NameIdentifier,user.Id),
                        new Claim(ClaimTypes.Role,roles.FirstOrDefault()),
                        new Claim("LoggedOn",DateTime.Now.ToString())
                    }),
                    SigningCredentials = new SigningCredentials(key,SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _appSettings.Site,
                    Audience = _appSettings.Audience,
                    Expires = DateTime.UtcNow.AddMinutes(tokenExpiryTime)
                    
                };

                //Generate Token
                var token = tokenHandler.CreateToken(tokenDescriptor);
                return Ok(new { token = tokenHandler.WriteToken(token),expiration = token.ValidTo,username = user.UserName,userRole = roles.FirstOrDefault() });
            }

            //return error
            ModelState.AddModelError("", "Username/Password was not Found");
            return Unauthorized(new { LoginError = "Please Check the Login Credentials = Invalid Username/Password was entered" });
        }
    }
}