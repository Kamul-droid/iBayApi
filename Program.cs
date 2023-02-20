using Ibay.Context;
using Microsoft.EntityFrameworkCore;
using iBayApi.Controllers;
using NuGet.DependencyResolver;
using Ibay.Models;
using iBay.Repositories;
using Ibay.Repositories;
using System.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;
using System.Xml.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("iBayApi"));
});


// Add Manager of User Roles
builder.Services.AddIdentity<User, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<MyDbContext>()
    .AddDefaultTokenProviders();


//Ajout de nos Scopes
builder.Services.AddScoped<DbContext, MyDbContext>();

//builder.Services.AddScoped<UserManager>(sp => UserManager.Create());

//builder.Services.AddScoped<IBasicRepository<User>, UserRepository>();
builder.Services.AddControllers().AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddScoped<IBasicRepository<Product>, ProductRepository>();
builder.Services.AddScoped<IBasicRepository<Cart>, CartRepository>();


//builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(

options => {
    //options.SwaggerDoc("V1", new OpenApiInfo
    //{
    //    Version = "V1",
    //    Title = "WebAPI For IBay",
    //    Description = " WebAPI For Online Shop "
    //});
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                }
            },
            new List < string > ()
        }
    });

    
}

);


builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = iBayApi.ConfigurationManager.AppSetting["JWT:ValidIssuer"],
        ValidAudience = iBayApi.ConfigurationManager.AppSetting["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(iBayApi.ConfigurationManager.AppSetting["JWT:Secret"]))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.CreatePerOwinContext<UserManager>(UserManager.Create);
app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.Run();
