﻿using System;
using System.ComponentModel.DataAnnotations;

namespace ConsoleAppNetCore9Ef9.EntityFrameworkCore.Entities
{
    public record Guest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(300)]
        public string Name { get; set; }

        public DateTime RegisterDate { get; set; }

        public int? NullableInt { get; set; }

        public Guest()
        {
            
        }

        public Guest(string name, DateTime registerDate)
        {
            Name = name;
            RegisterDate = registerDate;
        }
    }
}