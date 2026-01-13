using Bulky.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Company> Companies { get; set; }

        public DbSet<ShoppingCart> ShoppingCarts { get; set; }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        public DbSet<OrderHeader> OrderHeaders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<ProductImage> ProductImages { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // when we are add identity then we must add this line.

            base.OnModelCreating(modelBuilder);


            // modelBuilder.Entity<Category>().HasData(
            //new Category { Id = 20, Name = "Action", DisplayOrder = 1 },
            //new Category { Id = 22, Name = "Funny", DisplayOrder = 2 },
            //new Category { Id = 21, Name = "History", DisplayOrder = 3 },
            //new Category { Id = 4, Name = "Sci-Fi ii", DisplayOrder = 4 }
            //);
           


                modelBuilder.Entity<Category>().HasData(

                new Category { Id = 1, Name = "Action", DisplayOrder = 1 },

                new Category { Id = 2, Name = "SciFi", DisplayOrder = 2 },

                new Category { Id = 4, Name = "Horror", DisplayOrder = 12 }

                );


            modelBuilder.Entity<Company>().HasData(

                new Company { Id = 1, 
                    Name = "Tech Solution", 
                    StreetAddress = "Maninagar",
                    City = "Ahmedabad",
                    PostalCode="3524",
                    State="Gujarat", 
                    PhoneNumber = "81452466" },

                new Company {
                    Id = 2,
                    Name = "Prisa Solution",
                    StreetAddress = "Maninagar",
                    City = "Ahmedabad",
                    PostalCode = "3522",
                    State = "Gujarat",
                    PhoneNumber = "25145441544"
                },

                new Company {
                    Id = 3,
                    Name = "Edith Solution",
                    StreetAddress = "Maninagar",
                    City = "Ahmedabad",
                    PostalCode = "2544",
                    State = "Gujarat",
                    PhoneNumber = "58755856"
                }

                );

            // Product seed data removed - admin can add products manually




        }



    }
}
