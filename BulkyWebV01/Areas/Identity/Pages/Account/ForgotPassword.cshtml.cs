// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Bulky.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BulkyWebV01.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [Display(Name = "Secret Recovery Code")]
            public string SecretCode { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email) as ApplicationUser;
                
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email address.");
                    return Page();
                }

                // Check if user has a secret code set
                if (string.IsNullOrEmpty(user.SecretCode))
                {
                    ModelState.AddModelError(string.Empty, "This account doesn't have a secret recovery code set. Please contact support.");
                    return Page();
                }

                // Verify secret code
                if (user.SecretCode != Input.SecretCode)
                {
                    ModelState.AddModelError(string.Empty, "Invalid secret recovery code.");
                    return Page();
                }

                // Generate password reset token and redirect to reset password page
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                return RedirectToPage("./ResetPassword", new { code, email = Input.Email });
            }

            return Page();
        }
    }
}
