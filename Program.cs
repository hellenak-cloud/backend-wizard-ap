using System;
using BackendWizardAPI.Data;
using BackendWizardAPI.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=profiles.db"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");
app.UseHttpsRedirection();


// =====================
// CREATE PROFILE
// =====================
app.MapPost("/api/profiles", async (
    ProfileRequest request,
    AppDbContext db,
    IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(request?.Name))
    {
        return Results.BadRequest(new { status = "error", message = "Name is required" });
    }

    var name = request.Name.Trim().ToLower();

    var existing = await db.Profiles.FirstOrDefaultAsync(x => x.Name == name);
    if (existing != null)
    {
        return Results.Ok(new { status = "success", message = "Profile already exists", data = existing });
    }

    var client = httpClientFactory.CreateClient();

    var genderTask = client.GetStringAsync($"https://api.genderize.io?name={name}");
    var ageTask = client.GetStringAsync($"https://api.agify.io?name={name}");
    var countryTask = client.GetStringAsync($"https://api.nationalize.io?name={name}");

    await Task.WhenAll(genderTask, ageTask, countryTask);

    var genderData = System.Text.Json.JsonSerializer.Deserialize<GenderResponse>(genderTask.Result);
    var ageData = System.Text.Json.JsonSerializer.Deserialize<AgeResponse>(ageTask.Result);
    var countryData = System.Text.Json.JsonSerializer.Deserialize<CountryResponse>(countryTask.Result);

    // safe fallback handling
    var gender = genderData?.gender ?? "unknown";
    var genderProb = genderData?.probability ?? 0;
    var sampleSize = genderData?.count ?? 0;

    var age = ageData?.age ?? 0;

    var ageGroup = age switch
    {
        <= 12 => "child",
        <= 19 => "teenager",
        <= 59 => "adult",
        _ => "senior"
    };

    var topCountry = countryData?.country?
        .OrderByDescending(c => c.probability)
        .FirstOrDefault();

    var countryId = topCountry?.country_id ?? "XX";
    var countryProb = topCountry?.probability ?? 0;

    var profile = new Profile
    {
        Id = Guid.NewGuid(),
        Name = name,
        Gender = gender,
        GenderProbability = genderProb,
        SampleSize = sampleSize,
        Age = age,
        AgeGroup = ageGroup,
        CountryId = countryId,
        CountryProbability = countryProb,
        CreatedAt = DateTime.UtcNow
    };

    db.Profiles.Add(profile);
    await db.SaveChangesAsync();

    return Results.Created($"/api/profiles/{profile.Id}", new
    {
        status = "success",
        data = profile
    });
});


// =====================
// GET ALL
// =====================
app.MapGet("/api/profiles", async (
    string? gender,
    string? country_id,
    string? age_group,
    AppDbContext db) =>
{
    var query = db.Profiles.AsQueryable();

    if (!string.IsNullOrWhiteSpace(gender))
        query = query.Where(x => x.Gender == gender);

    if (!string.IsNullOrWhiteSpace(country_id))
        query = query.Where(x => x.CountryId == country_id);

    if (!string.IsNullOrWhiteSpace(age_group))
        query = query.Where(x => x.AgeGroup == age_group);

    var result = await query.ToListAsync();

    return Results.Ok(new
    {
        status = "success",
        count = result.Count,
        data = result.Select(x => new
        {
            id = x.Id,
            name = x.Name,
            gender = x.Gender,
            age = x.Age,
            age_group = x.AgeGroup,
            country_id = x.CountryId
        })
    });
});


// =====================
// GET BY ID
// =====================
app.MapGet("/api/profiles/{id}", async (string id, AppDbContext db) =>
{
    if (!Guid.TryParse(id, out var guid))
    {
        return Results.BadRequest(new { status = "error", message = "Invalid ID format" });
    }

    var profile = await db.Profiles.FindAsync(guid);

    return profile == null
        ? Results.NotFound(new { status = "error", message = "Not found" })
        : Results.Ok(new { status = "success", data = profile });
});


// =====================
// DELETE
// =====================
app.MapDelete("/api/profiles/{id}", async (string id, AppDbContext db) =>
{
    if (!Guid.TryParse(id, out var guid))
    {
        return Results.BadRequest(new { status = "error", message = "Invalid ID format" });
    }

    var profile = await db.Profiles.FindAsync(guid);

    if (profile == null)
        return Results.NotFound();

    db.Profiles.Remove(profile);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();


// =====================
// DTOs
// =====================
public class ProfileRequest
{
    public string Name { get; set; } = "";
}

public class GenderResponse
{
    public string? gender { get; set; }
    public double probability { get; set; }
    public int count { get; set; }
}

public class AgeResponse
{
    public int? age { get; set; }
}

public class CountryResponse
{
    public List<CountryItem>? country { get; set; }
}

public class CountryItem
{
    public string country_id { get; set; } = "";
    public double probability { get; set; }
}