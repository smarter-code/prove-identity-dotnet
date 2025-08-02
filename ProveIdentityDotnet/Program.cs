
using ProveIdentityDotnet.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache(); //Enable memory caching



builder.Services.Configure<ProveSettings>(
    builder.Configuration.GetSection("ProveSettings")); // Bind ProveSettings


builder.Services.AddScoped<ProveVerificationService>(); // Register ProveVerificationService


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();


app.MapFallbackToFile("index.html"); // Serve the frontend

app.Run();