var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Squares_ApiService>("apiservice");

builder.AddProject<Projects.Squares_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
