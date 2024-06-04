# Microsoft.Extensions.Configuration.Binder

Provides the functionality to bind an object to data in configuration providers for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/).

Documentation can be found at https://learn.microsoft.com/dotnet/core/extensions/configuration.

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](../README.md#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration.Binder](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.Binder/) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.

## Example
The following example shows how to bind a JSON configuration section to .NET objects.

```cs
using System;
using Microsoft.Extensions.Configuration;

class Settings
{
    public string Server { get; set; }
    public string Database { get; set; }
    public Endpoint[] Endpoints { get; set; }
}

class Endpoint
{
    public string IPAddress { get; set; }
    public int Port { get; set; }
}

class Program
{
    static void Main()
    {
        // Build a configuration object from JSON file
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Bind a configuration section to an instance of Settings class
        Settings settings = config.GetSection("Settings").Get<Settings>();

        // Read simple values
        Console.WriteLine($"Server: {settings.Server}");
        Console.WriteLine($"Database: {settings.Database}");

        // Read nested objects
        Console.WriteLine("Endpoints: ");
        
        foreach (Endpoint endpoint in settings.Endpoints)
        {
            Console.WriteLine($"{endpoint.IPAddress}:{endpoint.Port}");
        }
    }
}
```

To run this example, include an `appsettings.json` file with the following content in your project:

```json
{
  "Settings": {
    "Server": "example.com",
    "Database": "Northwind",
    "Endpoints": [
      {
        "IPAddress": "192.168.0.1",
        "Port": "80"
      },
      {
        "IPAddress": "192.168.10.1",
        "Port": "8080"
      }
    ]
  }
}
```

You can include a configuration file using a code like this in your `.csproj` file:

```xml
<ItemGroup>
  <Content Include="appsettings.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

## Contributing changes to the source generator

Along with the binder assembly that uses reflection, we ship a source generator assembly that provides a reflection and AOT compatible implementation. It works by intercepting regular binding calls and redirecting them to new routines that execute strongly typed binding logic.

### Worfklow

- [Build the .NET libraries and runtime assemblies](https://github.com/dotnet/runtime/tree/7ed33d80c92fa0b7ae740df60a460e984f2f442b#how-can-i-contribute)
- Make the generator changes in the `gen\` directory
- Validate the changes by running the source generator tests in `tests\SourceGeneratorTests`

### Testing

Here's a general command for running the the generator tests. They assume that you're making the changes in a Windows OS and running the from the `tests\SourceGeneratorTests\` directory.

```ps
dotnet build /t:test
```

If applicable, add new tests to validate your contribution. See [this documentation](https://github.com/dotnet/runtime/blob/7ed33d80c92fa0b7ae740df60a460e984f2f442b/docs/workflow/README.md#full-instructions-on-building-and-testing-the-runtime-repo) for details.

#### Stale generator bits

Sometimes the SDK uses stale bits of the generator. This can lead to unexpected test behavior or failures. Killing `dotnet.exe` processes usually solves the issue, e.g. with `taskkill /F /IM dotnet.exe /T`.

#### Updating baselines

Some contributions might change the logic emitted by the generator. We maintain baseline [source files](https://github.com/dotnet/runtime/tree/e3e9758a10870a8f99a93a25e54ab2837d3abefc/src/libraries/Microsoft.Extensions.Configuration.Binder/tests/SourceGenerationTests/Baselines) to track the code emitted to handle some core binding scenarios.

If the emitted code changes, these tests will fail locally and in continuous integration checks (for PRs) changes. You would need to update the baseline source files, manually or by using the following commands (PowerShell):

```ps
> $env:RepoRootDir = "D:\repos\dotnet_runtime"
> dotnet build t:test -f /p:UpdateBaselines=true
```

We have a [test helper](https://github.com/dotnet/runtime/blob/e3e9758a10870a8f99a93a25e54ab2837d3abefc/src/libraries/Microsoft.Extensions.Configuration.Binder/tests/SourceGenerationTests/GeneratorTests.Helpers.cs#L105-L118) to update the baselines. It requires setting an environment variable called `RepoRootDir` to the root repo path. In additon, the `UpdateBaselines` MSBuild property needs to be set to `true`.

After updating the baselines, inspect the changes to verify that they are valid. Note that the baseline tests will fail if the new code causes errors when building the resulting compilation.