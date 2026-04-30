var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "DemoIisApi is running on IIS.");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    application = "DemoIisApi",
    deployedBy = "Jenkins + Docker + Ansible",
    timestamp = DateTime.UtcNow
}));

app.Run();