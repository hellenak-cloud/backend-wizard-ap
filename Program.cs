var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

app.UseHttpsRedirection();



app.MapGet("/api/classify", async(string name, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest(new
        {
            status = "error",
            message = "Missing or empty name parameter"
        });
    }
    
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
    if (data == null || data.gender == null || data.count == 0)
    {
        return Results.UnprocessableEntity(new
        {
          status = "error",
        message = "No prediction available for the provided name"  
        });
    }
    var isConfident = data.probability >= 0.7 && data.count >= 100;
    var result = new
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
    };
    return Results.Ok(result);
});

app.Run();
public class GenderizeResponse
{
    public int count{get; set;}
    public string name {get; set;}
    public string gender {get; set;}
    public double probability {get; set;}
}


