using System;
using Bcrypt = BCrypt.Net.BCrypt;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LearnNetCore.Context;
using LearnNetCore.Models;
using LearnNetCore.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using System.Text;
using LearnNetCore.Services;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;

namespace LearnNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly MyContext _context;
        public IConfiguration _configuration;
        public UserManager<User> _userManager;
        AttrEmail attrEmail = new AttrEmail();
        RandomDigit randDig = new RandomDigit();
        SmtpClient client = new SmtpClient();

        public UserController(MyContext myContext, UserManager<User> userManager, IConfiguration iconfiguration)
        {
            _context = myContext;
            _configuration = iconfiguration;
            _userManager = userManager;
        }

        // GET api/values
        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet]
        //public async Task<List<User>> GetAll()
        public List<UserVM> GetAll()
        {
            List<UserVM> list = new List<UserVM>();
            foreach (var item in _context.Users)
            {
                var rolee = _context.RoleUsers.Where(ru => ru.User.Id == item.Id).FirstOrDefault();
                var role = _context.Roles.Where(r => r.Id == rolee.RoleId).FirstOrDefault();
                UserVM user = new UserVM()
                {
                    Id = item.Id,
                    UserName = item.UserName,
                    Email = item.Email,
                    Password = item.PasswordHash,
                    Phone = item.PhoneNumber,
                    RoleName = role.Name
                };
                list.Add(user);
            }
            return list;
            //return await _context.Users.ToListAsync<User>();
        }

        [HttpGet("{id}")]
        public UserVM GetID(string id)
        {

            //var rolee = _context.RoleUsers.Where(ru => ru.UserId == id).FirstOrDefault();
            var getId = _context.Users.Find(id);
            UserVM user = new UserVM()
            {
                Id = getId.Id,
                UserName = getId.UserName,
                Email = getId.Email,
                Password = getId.PasswordHash,
                Phone = getId.PhoneNumber,
                //RoleName = id.Name
            };
            return user;
        }

        [HttpPost]
        public IActionResult Create(UserVM userVM)
        {
            if (ModelState.IsValid)
            {
                client.Port = 587;
                client.Host = "smtp.gmail.com";
                client.EnableSsl = true;
                client.Timeout = 10000;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(attrEmail.mail, attrEmail.pass);

                var code = randDig.GenerateRandom();
                var fill = "Hi " + userVM.UserName + "\n\n"
                          + "Try this Code to Confirm: \n"
                          + code
                          + "\n\nThank You";

                MailMessage mm = new MailMessage("donotreply@domain.com", userVM.Email, "Create Email", fill);
                mm.BodyEncoding = UTF8Encoding.UTF8;
                mm.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
                client.Send(mm);

                userVM.RoleName = "Sales";
                var user = new User();
                var roleuser = new RoleUser();
                var role = _context.Roles.Where(r => r.Name == userVM.RoleName).FirstOrDefault();
                user.UserName = userVM.UserName;
                user.Email = userVM.Email;
                user.EmailConfirmed = false;
                user.PasswordHash = Bcrypt.HashPassword(userVM.Password);
                user.PhoneNumber = userVM.Phone;
                user.PhoneNumberConfirmed = false;
                user.TwoFactorEnabled = false;
                user.LockoutEnabled = false;
                user.AccessFailedCount = 0;
                user.SecurityStamp = code;
                roleuser.Role = role;
                roleuser.User = user;
                _context.RoleUsers.AddAsync(roleuser);
                _context.Users.AddAsync(user);
                _context.SaveChanges();
                return Ok("Successfully Created");
            }
            return BadRequest("registration failed!");

        }

        [HttpPut("{id}")]
        public IActionResult Update(string id, UserVM userVM)
        {
            if (ModelState.IsValid)
            {
                var getId = _context.Users.Find(id);
                var hasbcrypt = BCrypt.Net.BCrypt.HashPassword(userVM.Password, 12);
                getId.Id = userVM.Id;
                getId.UserName = userVM.UserName;
                getId.Email = userVM.Email;
                getId.PasswordHash = hasbcrypt;
                getId.PhoneNumber = userVM.Phone;
                _context.SaveChanges();
                return Ok("Successfully Update");
            }
            return BadRequest("Update Failed!");

        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            if (ModelState.IsValid)
            {
                var getUR = _context.RoleUsers.Where(a => a.UserId == id).FirstOrDefault();
                var getId = _context.Users.Find(id);
                _context.RoleUsers.Remove(getUR);
                _context.Users.Remove(getId);
                _context.SaveChanges();
                return Ok("Successfully Delete");
            }
            return BadRequest("Delete Failed!");

        }


        [HttpPost]
        [Route("Register")]
        public IActionResult Register(UserVM userVM)
        {
            if (ModelState.IsValid)
            {
                this.Create(userVM);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        //[HttpGet("{}")]
        [Route("Login")]
        public IActionResult Login(UserVM userVM)
        {
            if (ModelState.IsValid)
            {
                var getUserRole = _context.RoleUsers.Include("User").Include("Role").SingleOrDefault(x => x.User.Email == userVM.Email);
                if (getUserRole == null)
                {
                    return NotFound();
                }
                else if (userVM.Password == null || userVM.Password.Equals(""))
                {
                    return BadRequest(new { msg = "Password must filled" });
                }
                else if (!Bcrypt.Verify(userVM.Password, getUserRole.User.PasswordHash))
                {
                    return BadRequest(new { msg = "Password is Wrong" });
                }
                else
                {
                    //var user = new UserVM();
                    //user.Id = getUserRole.User.Id;
                    //user.UserName = getUserRole.User.UserName;
                    //user.Email = getUserRole.User.Email;
                    //user.Password = getUserRole.User.PasswordHash;
                    //user.Phone = getUserRole.User.PhoneNumber;
                    //user.RoleName = getUserRole.Role.Name;
                    if (getUserRole != null)
                    {
                        if (getUserRole.User.SecurityStamp != null)
                        {
                            var claims = new List<Claim> {
                                new Claim("Id", getUserRole.User.Id),
                                new Claim("UserName", getUserRole.User.UserName),
                                new Claim("Email", getUserRole.User.Email),
                                new Claim("RoleName", getUserRole.Role.Name),
                                new Claim("VerifyCode", getUserRole.User.SecurityStamp)
                            };
                            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddDays(1), signingCredentials: signIn);
                            return Ok(new JwtSecurityTokenHandler().WriteToken(token));
                        }
                        else
                        {
                            var claims = new List<Claim> {
                                new Claim("Id", getUserRole.User.Id),
                                new Claim("UserName", getUserRole.User.UserName),
                                new Claim("Email", getUserRole.User.Email),
                                new Claim("RoleName", getUserRole.Role.Name)
                            };
                            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                            var signIn = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], claims, expires: DateTime.UtcNow.AddDays(1), signingCredentials: signIn);
                            return Ok(new JwtSecurityTokenHandler().WriteToken(token));
                        }
                    }
                    return BadRequest("Invalid credentials");
                }

            }
            return BadRequest("Login Failed");
        }

        [HttpPost]
        [Route("code")]
        public IActionResult VerifyCode(UserVM userVM)
        {
            if (ModelState.IsValid)
            {
                var getUserRole = _context.RoleUsers.Include("User").Include("Role").SingleOrDefault(x => x.User.Email == userVM.Email);
                if (getUserRole == null)
                {
                    return NotFound();
                }
                else if (userVM.VerifyCode != getUserRole.User.SecurityStamp)
                {
                    return BadRequest(new { msg = "Your Code is Wrong" });
                }
                else
                {
                    //var user = new UserVM();
                    //user.Id = getUserRole.User.Id;
                    //user.Username = getUserRole.User.UserName;
                    //user.Email = getUserRole.User.Email;
                    //user.Password = getUserRole.User.PasswordHash;
                    //user.Phone = getUserRole.User.PhoneNumber;
                    //user.RoleName = getUserRole.Role.Name;
                    //return StatusCode(200, user);
                    return StatusCode(200, new
                    {
                        Id = getUserRole.User.Id,
                        Username = getUserRole.User.UserName,
                        Email = getUserRole.User.Email,
                        RoleName = getUserRole.Role.Name,
                        //Email = getUserRole.User.Email,
                        //Password = getUserRole.User.PasswordHash
                    });
                }
            }
            return BadRequest("Verify Code is Failed");
        }


    }

}