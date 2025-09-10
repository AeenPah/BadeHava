using Microsoft.EntityFrameworkCore;
using BadeHava.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=DBBadeHava.db"));

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();

