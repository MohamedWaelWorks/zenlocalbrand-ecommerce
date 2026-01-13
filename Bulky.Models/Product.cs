using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bulky.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }
        public string Description { get; set; }

        [Required]
        public string ISBN { get; set; }

        [Required]
        public string Author { get; set; }

        [Required]
        [Display(Name = "List Price")]
        [Range(1, 3000)]
        public double ListPrice { get; set; }

        [Required]
        [Display(Name = "Price")]
        [Range(1, 3000)]
        public double Price { get; set; }

        [Display(Name = "Sale Price")]
        [Range(0, 3000)]
        public double? SalePrice { get; set; }

        [Display(Name = "Out of Stock")]
        public bool IsOutOfStock { get; set; } = false;

        [Display(Name = "Available Sizes")]
        public string AvailableSizes { get; set; } // Stored as comma-separated: "S,M,L,XL"

        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [ValidateNever]
        // It is the navigation property to the category table
        public Category Category { get; set; }

        //[ValidateNever]
        //public string ImageUrl { get; set; }

        //public int TestProperty { get; set; }


        [ValidateNever]
        public List<ProductImage> ProductImages { get; set; }

    }
}
