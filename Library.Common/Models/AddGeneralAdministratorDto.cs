﻿using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;


namespace Library.Common.Models
{
    public class AddGeneralAdministratorDto
    {
        [Required, MinLength(4)]
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("email")][EmailAddress] public string Email { get; set; }

        [JsonProperty("password")]
        [MinLength(6)]
        public string Password { get; set; }
    }
}
