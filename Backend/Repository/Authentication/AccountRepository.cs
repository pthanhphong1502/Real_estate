﻿using Backend.Mapper;
using Backend.Models;
using Backend.Repository.Authentication.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Repository.Authentication
{
    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IConfiguration configuration;
        private readonly RoleManager<IdentityRole> roleManager;

        public AccountRepository(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.roleManager = roleManager;

        }

        public async Task<TokenResponse> Login(Login model)
        {
            var user = await userManager.FindByNameAsync(model.Username);

            if (user != null)
            {
                if (!user.LockoutEnabled)
                {
                    // User is locked
                    return new TokenResponse { Status = "Locked" };
                }

                var passwordValid = await userManager.CheckPasswordAsync(user, model.Password);

                if (passwordValid)
                {
                    var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

                    var userRoles = await userManager.GetRolesAsync(user);
                    foreach (var role in userRoles)
                    {
                        authClaims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                    }

                    var authenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]));

                    var token = new JwtSecurityToken(
                        issuer: configuration["JWT:ValidIssuer"],
                        audience: configuration["JWT:ValidAudience"],
                        expires: DateTime.Now.AddHours(2),
                        claims: authClaims,
                        signingCredentials: new SigningCredentials(authenKey, SecurityAlgorithms.HmacSha512Signature)
                    );

                    return new TokenResponse
                    {
                        Token = new JwtSecurityTokenHandler().WriteToken(token),
                        Expiry = token.ValidTo,
                        UserId = user.Id,
                        Status = "Success"
                    };
                }
                else
                {
                    // Incorrect username or password
                    return new TokenResponse { Token = string.Empty };
                }
            }

            // User not found
            return new TokenResponse { Token = string.Empty };
        }

        public async Task<IdentityResult> RegisterUser(Register model)
        {
            var userExist = await userManager.FindByNameAsync(model.Username);
            var emailExist = await userManager.FindByEmailAsync(model.Email);
            if (userExist != null || emailExist != null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "Username or Email has been registered." });
            }
            var user = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Username,
                FullName = model.FullName,
                Promotion = 40000,
                PhoneNumber = model.PhoneNumber,
                TypeAccount = "Basic"
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                //kiểm tra role Customer đã có
                if (!await roleManager.RoleExistsAsync(AppRole.User))
                {
                    await roleManager.CreateAsync(new IdentityRole(AppRole.User));
                }

                await userManager.AddToRoleAsync(user, AppRole.User);
            }
            return result;
        }

        public async Task<IdentityResult> RegisterAdmin(Register model)
        {
            var user = new ApplicationUser
            {
                Email = model.Email,
                UserName = model.Username
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                //kiểm tra role Customer đã có
                if (!await roleManager.RoleExistsAsync(AppRole.Admin))
                {
                    await roleManager.CreateAsync(new IdentityRole(AppRole.Admin));
                }

                await userManager.AddToRoleAsync(user, AppRole.Admin);
            }
            return result;
        }

        public async Task LogOutAsync()
        {
            await signInManager.SignOutAsync();
        }
    }
}
