var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Squares_ApiService>("apiservice");

_ = builder.AddNpmApp("squares-react", "../squares-react", "dev")
    .WithEnvironment("PORT", "5173")
    .WithHttpEndpoint(5174, 5173)
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
