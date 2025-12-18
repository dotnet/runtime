# WebAssembly Browser App Template

This template creates a .NET app that runs on WebAssembly in a browser.

## Template Options

### UseMonoRuntime

**Parameter:** `--UseMonoRuntime`  
**Type:** boolean  
**Default:** `true`

Determines whether to use the Mono runtime for WebAssembly.

When set to `true`, the generated project file will include:
```xml
<UseMonoRuntime>true</UseMonoRuntime>
```

#### Usage Examples

Create a project with Mono runtime (default):
```bash
dotnet new wasmbrowser -n MyApp
```

Or explicitly specify:
```bash
dotnet new wasmbrowser -n MyApp --UseMonoRuntime true
```

Create a project without the Mono runtime property:
```bash
dotnet new wasmbrowser -n MyApp --UseMonoRuntime false
```

## Getting Started

After creating your project:

1. Navigate to your project directory:
   ```bash
   cd MyApp
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser to the URL displayed in the console output (typically `https://localhost:7000` or `http://localhost:5000`).

## Project Structure

- **Program.cs** - The entry point of your application
- **wwwroot/** - Static web assets (HTML, CSS, JavaScript)
- **Properties/** - Project properties and configuration

## Learn More

- [.NET WebAssembly Documentation](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly)
- [Mono Runtime](https://www.mono-project.com/)
