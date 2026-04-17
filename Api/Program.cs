using Lab6.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// Apply migrations / Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // In a real app we'd use context.Database.Migrate();
    // For this lab, EnsureCreated is simpler for a clean start in container
    context.Database.EnsureCreated();

    if (!context.Products.Any())
    {
        var products = Enumerable.Range(1, 10000).Select(i => new Product
        {
            Name = $"Product {i}",
            Price = (decimal)(Random.Shared.NextDouble() * 100)
        }).ToList();

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}


app.Run();
