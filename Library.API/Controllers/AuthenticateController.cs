﻿using Library.API.Entities;
using Library.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace Library.API.Controllers
{
    [Route("auth")]
    [ApiController]
    [Authorize]
    public class AuthenticateController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public AuthenticateController(IConfiguration configuration, UserManager<User> userManager,
            RoleManager<Role> roleManager)
        {
            Configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IConfiguration Configuration { get; }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateTokenAsync([FromBody]LoginUserDto loginUser)
        {
            var user = await _userManager.FindByEmailAsync(loginUser.UserName);
            if (user == null)
            {
                return Unauthorized();
            }

            var result = _userManager.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, loginUser.Password);
            if (result != PasswordVerificationResult.Success)
            {
                return Unauthorized();
            }

            var userClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var roleItem in userRoles)
            {
                userClaims.Add(new Claim(ClaimTypes.Role, roleItem));
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };
            claims.AddRange(userClaims);
            var tokenConfigSection = Configuration.GetSection("Security:Token");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenConfigSection["Key"]));
            var signCredential = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwtToken = new JwtSecurityToken(
                issuer: tokenConfigSection["Issuer"],
                audience: tokenConfigSection["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: signCredential);
            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                expiration = TimeZoneInfo.ConvertTimeFromUtc(jwtToken.ValidTo, TimeZoneInfo.Local)
            });
        }

        [HttpPost("register", Name = nameof(AddUserAsync))]
        [AllowAnonymous]
        public async Task<IActionResult> AddUserAsync(RegisterUser registerUser)
        {
            if (registerUser.Grade is > 4 or 0)
            {
                throw new ArgumentException("Grade不合法，只能设置1,2,3,4");
            }

            var user = new User()
            {
                UserName = registerUser.UserName,
                Email = registerUser.Email,
                Grade = registerUser.Grade,
            };
            var result = await _userManager.CreateAsync(user, registerUser.Password);

            if (result.Succeeded)
            {
                var role = await _roleManager.FindByNameAsync("User");
                await _userManager.AddToRoleAsync(user, role.Name);
                return Ok();
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.Errors.FirstOrDefault()?.Description);
                return BadRequest(ModelState);
            }
        }


        [HttpPut("/changePassword/{id:guid}")]

        public async Task<IActionResult> UpdateUserAsync(Guid id, UserPasswordChangeDto userPasswordChangeDto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            var token = new JwtSecurityToken(Request.Headers.Authorization[0]["Bearer ".Length..]);
            var userName = token.Claims.FirstOrDefault(claim => claim.Type == JwtRegisteredClaimNames.Sub)!.Value;
            if (userName!=user.UserName)
            {
                throw new Exception("请输入自己的ID");
            }       
            if (user == null)
            {
                throw new Exception("此用户不存在");
            }

            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, userPasswordChangeDto.NewPassword);
            await _userManager.UpdateAsync(user);
            return NoContent();
        }

        [HttpPut("/changeEmail/{id:guid}")]
        public async Task<IActionResult> UpdateUserAsync(Guid id, UserEmailChangeDto userEmailChangeDto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                throw new Exception("此用户不存在");
            }
            user.Email = userEmailChangeDto.Email;
            await _userManager.UpdateAsync(user);
            return Ok();
        }
    }
}
