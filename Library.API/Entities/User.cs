﻿using Microsoft.AspNetCore.Identity;

namespace Library.API.Entities
{
    public class User : IdentityUser
    {
        /// <summary>
        /// 年级1,2,3,4
        /// </summary>
        public byte? Grade { get; set; }
    }
}