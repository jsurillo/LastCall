using BrosCode.LastCall.Business.Mapping;
using BrosCode.LastCall.Entity.DbContext;
using BrosCode.LastCall.Entity.Repository;
using BrosCode.LastCall.Entity.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(typeof(LastCallMappingProfile).Assembly);
builder.Services.AddDbContext<LastCallDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("LastCallDb")));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
