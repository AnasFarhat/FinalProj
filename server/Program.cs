using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.StaticFiles;
using PartnersWebApi.Interfaces;
using PartnersWebApi.Repositories;
using PartnersWebApi.Repository;
using PartnersWebApi.Services;
using StackExchange.Redis;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using PartnersWebApi.Models;
using PartnersWebApi.Hubs;



var builder = WebApplication.CreateBuilder(args);


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins(
                "https://proj.ruppin.ac.il", 
                "http://localhost:8081"      
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); 
    });
});

builder.Services.AddHttpClient<IChatAiService, GeminiAiService>();
builder.Services.AddScoped<IUsersRepository, SQLUsersRepository>();
builder.Services.AddScoped<ITripsRepository, SQLTripsRepository>();
builder.Services.AddScoped<IFeedbacksRepository, SQLFeedbacksRepository>();
builder.Services.AddScoped<ICommunityRepository, SQLCommunityRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<INotificationsRepository, SQLNotificationsRepository>();
builder.Services.AddScoped<IChatRepository, SQLChatRepository>();
builder.Services.AddScoped<IConnectionsRepository, SQLConnectionsRepository>();
builder.Services.AddScoped<IMessagesRepository, SQLMessagesRepository>();     
builder.Services.AddScoped<IMapService, MapService>();
builder.Logging.AddConsole();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ITriviaRepository, TriviaRepository>();
builder.Services.AddScoped<IGeminiTriviaService, GeminiTriviaService>();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});



builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "נא להזין את הטוקן בלבד (ללא המילה Bearer)"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
     
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                    {
                        accessToken = authHeader.Substring("Bearer ".Length);
                    }
                }

                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddSingleton<PartnersWebApi.Services.PresenceStore>();
builder.Services.AddHostedService<StaleSessionCleaner>();
builder.Services.AddSignalR();



var app = builder.Build();
app.MapHub<LocationHub>("/hubs/location");

var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "Uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".jfif"] = "image/jpeg";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/Uploads",
    ContentTypeProvider = provider
});

app.UseStaticFiles();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("./v1/swagger.json", "Partners API V1");
});

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
