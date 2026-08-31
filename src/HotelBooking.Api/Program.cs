using System.Reflection;
using HotelBooking.Api;
using HotelBooking.Api.ModelBinding;
using HotelBooking.Domain.Repositories;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Repositories;
using HotelBooking.Infrastructure.Resilience;
using HotelBooking.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    // Query-string DateOnly values must be unambiguous (yyyy-MM-dd) - the default binder
    // parses using the server's current culture, which would silently read "07/09/2026" as
    // 7 September or 9 July depending on where this happens to be hosted.
    options.ModelBinderProviders.Insert(0, new IsoDateOnlyModelBinderProvider());
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Booking API",
        Version = "v1",
        Description = "A hotel room booking API - find a hotel, find available rooms, book a room, and look up a booking by reference.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddDbContext<HotelBookingDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Services.AddSingleton<IDatabaseResiliencePipeline, DatabaseResiliencePipeline>();
builder.Services.AddScoped<IHotelRepository, HotelRepository>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<TestDataSeeder>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Applies any pending migrations on startup, including creating the schema from scratch
// the first time this boots against an empty Azure SQL database.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<HotelBookingDbContext>().Database.MigrateAsync();
}

// Swagger is deliberately available in every environment, not just Development - the API
// requires no authentication and is meant to be explored and tested on the deployed
// Azure instance, not just locally.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Booking API v1");
});

app.MapGet("/", () => Results.Redirect("/swagger"));

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the top-level statements' generated Program class so WebApplicationFactory<Program>
// can find it from the test project.
public partial class Program;
