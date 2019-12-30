﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Contra.Areas.Identity.Data;
using Contra.Data;
using Contra.Models;

namespace Contra.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class v1Controller : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<OpenTalonUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public v1Controller(ApplicationDbContext context,
                            UserManager<OpenTalonUser> userManager,
                            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Administrator")]
        [Route("role/create/{*role}")]
        [HttpPost]
        public async Task<string> CreateRole(string role)
        {
            if (_roleManager.RoleExistsAsync(role).Result)
                return "Role already exists!";

            await _roleManager.CreateAsync(new IdentityRole(role));

            return $"Created {role} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("role/delete/{*role}")]
        [HttpPost]
        public async Task<string> DeleteRole(string role)
        {
            if (!_roleManager.RoleExistsAsync(role).Result)
                return "Role does not exist!";
            if (role == "Administrator" || role == "Staff") 
                return "Role cannot be deleted!";

            await _roleManager.DeleteAsync(_roleManager.FindByNameAsync(role).Result);

            return $"Deleted {role} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("user/{userID}/ensure/{*role}")]
        [HttpPost]
        public async Task<string> EnsureRole(string userID, string role)
        {
            if (!_roleManager.RoleExistsAsync(role).Result)
                return "Requested role not found.";

            var user = _userManager.Users.FirstOrDefault(u => u.Id == userID);
            if (user == null)
                return "Requested user not found.";

            await _userManager.AddToRoleAsync(user, role);
            return $"Added {role} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("user/{userID}/enfeeble/{*role}")]
        [HttpPost]
        public async Task<string> EnfeebleRole(string userID, string role)
        {
            if (!_roleManager.RoleExistsAsync(role).Result)
                return "Requested role not found.";

            var user = _userManager.Users.FirstOrDefault(u => u.Id == userID);
            if (user == null)
                return "Requested user not found.";

            await _userManager.RemoveFromRoleAsync(user, role);
            return $"Removed {role} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("comment/approve/{*id}")]
        [HttpPost]
        public async Task<string> CommentApprove(int? id)
        {
            if (id == null) return "Requested resource not found.";

            Comment comment = await _context.Comment.FindAsync(id);
            if (comment != null)
            {
                comment.Approved = ApprovalStatus.Approved;
                await _context.SaveChangesAsync();
            }
            else return $"Comment {id} does not exist in the database.";

            return $"Approved comment {id} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("comment/delist/{*id}")]
        [HttpPost]
        public async Task<string> CommentDelist(int? id)
        {
            if (id == null) return "Requested resource not found.";

            Comment comment = await _context.Comment.FindAsync(id);
            if (comment != null)
            {
                comment.Approved = ApprovalStatus.Rejected;
                await _context.SaveChangesAsync();
            }
            else return $"Comment {id} does not exist in the database.";

            return $"Delisted comment {id} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("comment/delete/{*id}")]
        [HttpPost]
        public async Task<string> CommentDelete(int? id)
        {
            if (id == null) return "Requested resource not found.";

            Comment comment = await _context.Comment.FindAsync(id);
            if (comment != null)
            {
                _context.Comment.Remove(comment);
                await _context.SaveChangesAsync();
            }
            else return $"Comment {id} does not exist in the database.";

            return $"Deleted comment {id} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("article/approve/{*id}")]
        [HttpPost]
        public async Task<string> ArticleApprove(int? id)
        {
            if (id == null) return "Requested resource not found.";

            Article article = await _context.Article.FindAsync(id);
            if (article != null)
            {
                article.Approved = ApprovalStatus.Approved;
                await _context.SaveChangesAsync();
            }
            else return $"Article {id} does not exist in the database.";

            return $"Approved article {id} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("article/delist/{*id}")]
        [HttpPost]
        public async Task<string> ArticleDelist(int? id)
        {
            if (id == null) return "Requested resource not found.";

            Article article = await _context.Article.FindAsync(id);
            if (article != null)
            {
                article.Approved = ApprovalStatus.Rejected;
                await _context.SaveChangesAsync();
            }
            else return $"Article {id} does not exist in the database.";

            return $"Delisted article {id} successfully!";
        }

        [Authorize(Roles = "Administrator")]
        [Route("article/delete/{*id}")]
        [HttpPost]
        public async Task<string> ArticleDelete(int? id)
        {
            if (id == null) return "Requested resource not found.";

            Article article = await _context.Article.FindAsync(id);
            List<Comment> comments = (from c in _context.Comment
                                      where c.PostId == id
                                      select c).ToList();
            if (article != null)
            {
                _context.Article.Remove(article);
                _context.Comment.RemoveRange(comments);
                await _context.SaveChangesAsync();
            }
            else return $"Article {id} does not exist in the database.";

            return $"Deleted article {id} successfully!";
        }

        [Authorize]
        [Route("account/{id}/picture/{*url}")]
        [HttpPost]
        public async Task<string> ChangeProfilePicture(string id, string url)
        {
            if (_userManager.GetUserId(User) == id)
            {
                OpenTalonUser user = await _userManager.GetUserAsync(User);
                if (url == "reset")
                {
                    StringBuilder sb = new StringBuilder();
                    using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                    {
                        byte[] inputBytes = Encoding.ASCII.GetBytes(user.Email.Trim().ToLower());
                        byte[] hashBytes = md5.ComputeHash(inputBytes);

                        for (int i = 0; i < hashBytes.Length; i++)
                            sb.Append(hashBytes[i].ToString("X2"));
                    }
                    user.ProfilePictureURL = "https://gravatar.com/avatar/" + sb.ToString() + "?d=identicon";
                    await _userManager.UpdateAsync(user);
                }
                else
                {
                    user.ProfilePictureURL = url;
                    await _userManager.UpdateAsync(user);
                }

                return "Successfully changed profile picture!";
            }
            else return "Not authorized!";
        }

        [Route("account/{id}/picture")]
        [HttpGet]
        public async Task<string> GetProfilePicture(string id)
        {
            OpenTalonUser user = await _userManager.FindByIdAsync(id);
            if (user != null)
                return user.ProfilePictureURL;
            else return "Not found!";
        }

        [Route("article/info/{*id}")]
        [HttpGet]
        public async Task<string> ArticleInfo(int? id)
        {
            if (id == null) return "Requested resource not found.";

            Article article = await _context.Article.FindAsync(id);
            if (article == null) return $"Article {id} does not exist in the database.";
            if (article.Approved != ApprovalStatus.Approved) return "401 Unauthorized";

            Dictionary<string, string> info = new Dictionary<string, string>()
            {
                { "id", article.Id.ToString() },
                { "title", article.Title },
                { "author", article.AuthorName },
                { "date", article.Date.ToString() },
                { "tags", article.SummaryShort },
                { "summary", article.SummaryLong },
                { "image", article.ThumbnailURL }
            };

            return JsonConvert.SerializeObject(info);
        }

        [Route("article/list/{query}/{*skip}")]
        [HttpGet]
        public string ArticleGetBulk(string query, int skip)
        {
            if (string.IsNullOrEmpty(query)) return "";

            List<Article> articles;
            if (_userManager.GetUserId(User) == query)
            {
                articles = (from a in _context.Article
                            where a.OwnerID == query
                            orderby a.Date descending
                            select a).ToList();
            }
            else
            {
                articles = (query.ToLower()) switch
                {
                    "new" => (from a in _context.Article
                              where a.Approved == ApprovalStatus.Approved
                              orderby a.Date descending
                              select a).ToList(),
                    _ => (from a in _context.Article
                          where a.Approved == ApprovalStatus.Approved &&
                              (a.Title.Contains(query) ||
                              a.SummaryShort.Contains(query) ||
                              a.OwnerID == query)
                          orderby a.Date descending
                          select a).ToList()
                };
            }

            articles = articles.Skip(skip * 8).Take(8).ToList();
            if (articles.Count == 0) return "";

            List<Dictionary<string, string>> info = new List<Dictionary<string, string>>();
            foreach (Article a in articles)
            {
                Dictionary<string, string> i = new Dictionary<string, string>()
                {
                    { "id", a.Id.ToString() },
                    { "title", a.Title },
                    { "author", a.AuthorName },
                    { "date", a.Date.ToString() },
                    { "tags", a.SummaryShort },
                    { "summary", a.SummaryLong },
                    { "image", a.ThumbnailURL }
                };
                info.Add(i);
            }

            return JsonConvert.SerializeObject(info);
        }

        [Authorize(Roles = "Administrator")]
        [Route("generate")]
        public async Task<string> Generate()
        {
            Article placeholder = new Article
            {
                Approved = ApprovalStatus.Approved,
                AuthorName = "Sei",
                OwnerID = "",
                Date = DateTime.Now,
                Title = "Autogen",
                SummaryShort = "Arts, Opinion, Autogen",
                SummaryLong = "Ever wonder what an autogenerated article looks like?",
                Content = "Now you know!",
                Views = 0
            };

            string[] urls = new string[5] { "/img/img01.jpg",
                                            "/img/img02.jpg",
                                            "/img/img03.jpg",
                                            "/img/img04.jpg",
                                            "/img/img05.jpg" };
            Random rnd = new Random();
            placeholder.ThumbnailURL = urls[rnd.Next(0, 5)];
            _context.Article.Add(placeholder);
            await _context.SaveChangesAsync();
            return "201 Created";
        }

        [Authorize(Roles = "Administrator")]
        [Route("degenerate")]
        public async Task<string> Degenerate()
        {
            List<Article> a = (from c in _context.Article
                               where c.Title == "Autogen"
                               select c).ToList();
            _context.Article.RemoveRange(a);
            await _context.SaveChangesAsync();
            return "205 Reset";
        }
    } 
}