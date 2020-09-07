using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using LearnNetCore.ViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Web.Controllers
{
    public class AccountController : Controller
    {
        readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:44373/api/")
        };
        [Route("login")]
        public IActionResult Login()
        {
            return View();
        }
        [Route("register")]
        public IActionResult Register()
        {
            return View();
        }
        [Route("Verify")]
        public IActionResult verify()
        {
            return View();
        }


        [Route("validate")]
        public IActionResult Validate(UserVM userVM)
        {
            if (userVM.UserName == null)
            {
                var jsonUserVM = JsonConvert.SerializeObject(userVM);
                var buffer = System.Text.Encoding.UTF8.GetBytes(jsonUserVM);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var resTask = client.PostAsync("user/login/", byteContent);
                resTask.Wait();
                var result = resTask.Result;
                if (result.IsSuccessStatusCode)
                {
                    var data = result.Content.ReadAsStringAsync().Result;
                    if (data != "")
                    {
                        //var json = JsonConvert.DeserializeObject(data).ToString();
                        //var account = JsonConvert.DeserializeObject<UserVM>(json);
                        var handler = new JwtSecurityTokenHandler();
                        var tokenS = handler.ReadJwtToken(data);
                        var account = new UserVM();
                        account.Id = tokenS.Claims.First(claim => claim.Type == "Id").Value;
                        account.UserName = tokenS.Claims.First(claim => claim.Type == "UserName").Value;
                        account.Email = tokenS.Claims.First(claim => claim.Type == "Email").Value;
                        account.RoleName = tokenS.Claims.First(claim => claim.Type == "RoleName").Value;

                        if (account.VerifyCode != null)
                        {
                            if (userVM.VerifyCode != account.VerifyCode)
                            {
                                return Json(new { status = true, msg = "Check your Code" });
                            }
                        }

                        else if (account.RoleName == "Admin" || account.RoleName == "Sales")
                        {
                            HttpContext.Session.SetString("id", account.Id);
                            HttpContext.Session.SetString("username", account.UserName);
                            HttpContext.Session.SetString("email", account.Email);
                            HttpContext.Session.SetString("lvl", account.RoleName);
                            if (account.RoleName == "Admin")
                            {
                                return Json(new { status = true, msg = "Login Successfully !", acc = "Admin" });
                            }
                            else
                            {
                                return Json(new { status = true, msg = "Login Successfully !", acc = "Sales" });
                            }
                        }
                        else
                        {
                            return Json(new { status = false, msg = "Invalid Username or Password!" });
                        }
                    }
                    else
                    {
                        return Json(new { status = false, msg = "Username Not Found!" });
                    }
                }
                else
                {
                    //return RedirectToAction("Login","Auth");
                    return Json(new { status = false, msg = "Username Not Found!" });
                }
            }
            else if (userVM.UserName != null)
            {
                var json = JsonConvert.SerializeObject(userVM);
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var result = client.PostAsync("user/", byteContent).Result;
                if (result.IsSuccessStatusCode)
                {
                    return Json(new { status = true, code = result, msg = "Register Success! " });
                }
                else
                {
                    return Json(new { status = false, msg = "Something Wrong!" });
                }
            }
            return Redirect("/login");
        }


        [Route("verifCode")]
        public IActionResult VerifCode(UserVM userVM)
        {
            if (userVM.VerifyCode != null)
            {
                var jsonUserVM = JsonConvert.SerializeObject(userVM);
                var buffer = System.Text.Encoding.UTF8.GetBytes(jsonUserVM);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var result = client.PostAsync("user/code/", byteContent).Result;
                if (result.IsSuccessStatusCode)
                {
                    var data = result.Content.ReadAsStringAsync().Result;
                    if (data != "")
                    {
                        var json = JsonConvert.DeserializeObject(data).ToString();
                        var account = JsonConvert.DeserializeObject<UserVM>(json);
                        if (account.RoleName == "Admin" || account.RoleName == "Sales")
                        {
                            HttpContext.Session.SetString("id", account.Id);
                            HttpContext.Session.SetString("uname", account.UserName);
                            HttpContext.Session.SetString("email", account.Email);
                            HttpContext.Session.SetString("lvl", account.RoleName);
                            if (account.RoleName == "Admin")
                            {
                                return Json(new { status = true, msg = "Welcome !", acc = "Admin" });
                            }
                            else
                            {
                                return Json(new { status = true, msg = "Welcome !", acc = "Sales" });
                            }
                        }
                        else
                        {
                            return Json(new { status = false, msg = "Invalid Username or Password!" });
                        }
                    }
                    else
                    {
                        return Json(new { status = false, msg = "Username Not Found!" });
                    }
                    //var data = result.Content.ReadAsStringAsync().Result;
                    //var json = JsonConvert.DeserializeObject(data).ToString();
                    //var account = JsonConvert.DeserializeObject<UserVM>(json);
                    //var dataLogin = new UserVM()
                    //{
                    //    Email = account.Email,
                    //    Password = account.Password
                    //};
                    //this.Validate(dataLogin);
                    //return Json(new { status = true, code = result, msg = "Login Success! " });
                }
                else
                {
                    return Json(new { status = false, msg = "Your Code is Wrong!" });
                }
            }
            else
            {
                return Json(new { status = false, msg = "Something Wrong!" });
            }
        }


        [Route("getjwt")]
        public IActionResult GetName()
        {
            var stream = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJJZCI6ImRiM2VhZmIxLTkyMWUtNDdmYS1hOGFiLTIwNDYxMzkxM2FlMCIsIlVzZXJuYW1lIjoiUmlmcXkiLCJFbWFpbCI6Im11aGFtbWFkcmlmcWkwQGdtYWlsLmNvbSIsIlJvbGVOYW1lIjoiU2FsZXMiLCJleHAiOjE1OTk1NDY0MTYsImlzcyI6IkludmVudG9yeUF1dGhlbnRpY2F0aW9uU2VydmVyIiwiYXVkIjoiSW52ZW50b3J5c2VydmljZVBvc3RtYW50Q2xpZW50In0.ziIjgvqJdH17w4HwHGzvXyZTUz41S06i0xHWGxAnY2M";
            var handler = new JwtSecurityTokenHandler();
            var tokenS = handler.ReadJwtToken(stream);
            //var cek = tokenS.Payload;
            //cek.u

            //var jsonToken = handler.ReadToken(stream);
            //var tokenS = handler.ReadToken(stream) as JwtSecurityToken;

            //var id = tokenS.Claims.First(claim => claim.Type == "Id").Value;
            //var uname = tokenS.Claims.First(claim => claim.Type == "Username").Value;
            //var mail = tokenS.Claims.First(claim => claim.Type == "Email").Value;
            //var role = tokenS.Claims.First(claim => claim.Type == "RoleName").Value;

            var user = new UserVM()
            {
                Id = tokenS.Claims.First(claim => claim.Type == "Id").Value,
                UserName = tokenS.Claims.First(claim => claim.Type == "UserName").Value,
                Email = tokenS.Claims.First(claim => claim.Type == "Email").Value,
                RoleName = tokenS.Claims.First(claim => claim.Type == "RoleName").Value,
            };

            var usrVm = new UserVM();
            //return Json(user);
            return Json(tokenS.Payload);
        }


        [Route("logout")]
        public IActionResult Logout()
        {
            //HttpContext.Session.Remove("id");
            HttpContext.Session.Clear();
            return Redirect("/login");
        }
    }
}