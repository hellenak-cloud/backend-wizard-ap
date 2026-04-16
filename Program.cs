var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();


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


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseCors("AllowAll");

app.UseHttpsRedirection();



app.MapGet("/api/classify", async (string name, IHttpClientFactory httpClientFactory) =>
{
    
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest(new
        {
            status = "error",
            message = "Missing or empty name parameter"
        });
    }

    try
    {
        var client = httpClientFactory.CreateClient();
        var url = $"https://api.genderize.io/?name={name}";

        var response = await client.GetStringAsync(url);

        if (string.IsNullOrWhiteSpace(response))
        {
            return Results.StatusCode(502);
        }

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var data = System.Text.Json.JsonSerializer.Deserialize<GenderizeResponse>(response, options);

        
        if (data == null || string.IsNullOrEmpty(data.gender) || data.count <= 0)
        {
            return Results.UnprocessableEntity(new
            {
                status = "error",
                message = "No prediction available for the provided name"
            });
        }

        
        var isConfident = data.probability >= 0.7 && data.count >= 100;

        
        return Results.Ok(new
        {
            status = "success",
            data = new
            {
                name = data.name,
                gender = data.gender,
                probability = data.probability,
                sample_size = data.count,
                is_confident = isConfident,
                processed_at = DateTime.UtcNow.ToString("o")
            }
        });
    }
    catch
    {
        // ✅ Handles API failure (502/500 case)
        return Results.StatusCode(502);
    }
});

app.Run();



public class GenderizeResponse
{
    public int count { get; set; }
    public string? name { get; set; }
    public string? gender { get; set; }
    public double probability { get; set; }
}