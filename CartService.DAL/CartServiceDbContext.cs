namespace CartService.DAL
{
    using Microsoft.EntityFrameworkCore;
    using Models;

    public class CartServiceDbContext:DbContext
    {
        public CartServiceDbContext(DbContextOptions<CartServiceDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
    }
}