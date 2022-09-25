using Confluent.Kafka;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Mime;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
                .AddSqlServer(builder.Configuration.GetConnectionString("SqlServer"),
                    name: "SqlServer", tags: new string[] { "db", "data" })
                .AddRabbitMQ(builder.Configuration.GetConnectionString("rabbitMQ"),
                    name: "RabbitMQ", tags: new string[] { "messaging" })
                .AddMongoDb(builder.Configuration.GetConnectionString("mongoDb"),
                    name: "MongoDB", tags: new string[] { "db", "data" })
                .AddKafka(new ProducerConfig { BootstrapServers = "null" },
                    name: "Confluent Kafka", tags: new string[] { "messaging" })
                .AddUrlGroup(new Uri("null"),
                    name: "Jaeger", tags: new string[] { "url", "rest", "tracing", "microservices" });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/status", new HealthCheckOptions()
{
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(new
        {
            currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            statusApplication = report.Status.ToString(),
            healthChecks = report.Entries.Select(e => new
            {
                check = e.Key,
                status = Enum.GetName(typeof(HealthStatus), e.Value.Status)
            })
        });
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(result);
    }
});

app.UseHttpsRedirection();
app.Run();