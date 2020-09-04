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

namespace LearnNetCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly MyContext _context;
        public UserController(MyContext myContext)
        {
            _context = myContext;
        }

        // GET api/values
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
            userVM.RoleName = "Sales";
            var user = new User();
            var roleUser = new RoleUser();
            var hasbcrypt = BCrypt.Net.BCrypt.HashPassword(userVM.Password,12);
            var role = _context.Roles.Where(r => r.Name == userVM.RoleName).FirstOrDefault();
            user.UserName = userVM.UserName;
            user.Email = userVM.Email;
            user.EmailConfirmed = false;
            user.PasswordHash = hasbcrypt;
            user.PhoneNumber = userVM.Phone;
            user.PhoneNumberConfirmed = false;
            user.TwoFactorEnabled = false;
            user.LockoutEnabled = false;
            user.AccessFailedCount = 0;
            roleUser.User = user;
            roleUser.Role = role;
            _context.RoleUsers.AddAsync(roleUser);
            _context.Users.AddAsync(user);
            _context.SaveChanges();
            return Ok("Successfully Created");
            //return data;
        }

        [HttpPut("{id}")]
        public IActionResult Update(string id, UserVM userVM)
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

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var getUR = _context.RoleUsers.Where(a=>a.UserId == id).FirstOrDefault();
            var getId = _context.Users.Find(id);
            _context.RoleUsers.Remove(getUR);
            _context.Users.Remove(getId);
            _context.SaveChanges();
            return Ok("Successfully Delete");
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
                    var user = new UserVM();
                    user.Id = getUserRole.User.Id;
                    user.UserName = getUserRole.User.UserName;
                    user.Email = getUserRole.User.Email;
                    user.Password = getUserRole.User.PasswordHash;
                    user.Phone = getUserRole.User.PhoneNumber;
                    user.RoleName = getUserRole.Role.Name;
                    return StatusCode(200, user);
                }
            }
            return BadRequest(500);
        }
    }

}