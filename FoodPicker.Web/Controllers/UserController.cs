using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FoodPicker.Web.Enums;
using FoodPicker.Infrastructure.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FoodPicker.Web.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = AuthorizationPolicies.AccessInternalAdminAreas)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.OrderBy(u => u.Name).ToListAsync();
            return View("List", users);
        }

        public class UserViewModel
        {
            public string Id { get; set; }
            
            [Required]
            public string Name { get; set; }
            
            [Required]
            public string Username { get; set; }
            
            [DataType(DataType.Password)]
            public string Password { get; set; }
            
            [Required]
            public bool IsAdmin { get; set; }

            [Required] public bool VoteIsRequired { get; set; } = true;
            [Required] public bool IsActive { get; set; } = true;
        }
        
        [HttpGet]
        [Route("Create")]
        [Route("{id}")]
        public async Task<IActionResult> CreateOrEdit(string id)
        {
            UserViewModel model;
            if (string.IsNullOrEmpty(id))
            {
                // create
                model = new UserViewModel {Id = id};
            }
            else
            {
                // edit
                var user = await _userManager.FindByIdAsync(id);
                model = new UserViewModel
                {
                    Id = user.Id,
                    Name = user.Name,
                    Username = user.UserName,
                    Password = null,
                    IsAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
                    VoteIsRequired = user.VoteIsRequired,
                    IsActive = user.IsActive,
                };
            }
            return View(model);
        } 
        
        [HttpPost]
        [Route("Create", Name = "UserCreate")]
        [Route("{id}", Name = "UserEdit")]
        public async Task<IActionResult> CreateOrEdit(string id, [FromForm] UserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            
            ApplicationUser dbModel;
            if (string.IsNullOrEmpty(id))
            {
                dbModel = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    UserName = model.Username,
                    VoteIsRequired = model.VoteIsRequired,
                    IsActive = model.IsActive,
                };
                await _userManager.CreateAsync(dbModel);
            }
            else
            {
                dbModel = await _userManager.FindByIdAsync(id);
                dbModel.Name = model.Name;
                dbModel.UserName = model.Username;
                dbModel.VoteIsRequired = model.VoteIsRequired;
                dbModel.IsActive = model.IsActive;
                await _userManager.UpdateAsync(dbModel);
            }
            
            if (!string.IsNullOrEmpty(model.Password))
            {

                IdentityResult passwordResult;
                if (await _userManager.HasPasswordAsync(dbModel))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(dbModel);
                    passwordResult = await _userManager.ResetPasswordAsync(dbModel, token, model.Password);
                }
                else
                {
                    passwordResult = await _userManager.AddPasswordAsync(dbModel, model.Password);
                }
                if (passwordResult.Succeeded) return RedirectToRoute("UserEdit", new { id = dbModel.Id });
                foreach (var passwordResultError in passwordResult.Errors)
                {
                    ModelState.AddModelError("Password", passwordResultError.Description);
                }

                return View(model);
            }

            var userIsAdmin = await _userManager.IsInRoleAsync(dbModel, "Admin");
            if (model.IsAdmin && !userIsAdmin)
            {
                await _userManager.AddToRoleAsync(dbModel, "Admin");
            }
            else if (!model.IsAdmin && userIsAdmin)
            {
                await _userManager.RemoveFromRoleAsync(dbModel, "Admin");
            }
            
            return RedirectToRoute("UserEdit", new {id = dbModel.Id});
        }
    }
}