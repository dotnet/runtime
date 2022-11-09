# Microsoft.Extensions.Configuration.UserSecrets

User secrets configuration provider implementation for [Microsoft.Extensions.Configuration](https://www.nuget.org/packages/Microsoft.Extensions.Configuration/). User secrets mechanism enables you to override application configuration settings with values stored in the local secrets file. You can use [UserSecretsConfigurationExtensions.AddUserSecrets](https://learn.microsoft.com/dotnet/api/microsoft.extensions.configuration.usersecretsconfigurationextensions.addusersecrets) extension method on `IConfigurationBuilder` to add user secrets provider to the configuration builder.

Documentation can be found at https://learn.microsoft.com/aspnet/core/security/app-secrets

## Contribution Bar
- [x] [We consider new features, new APIs, bug fixes, and performance changes](https://github.com/dotnet/runtime/tree/main/src/libraries#contribution-bar)

The APIs and functionality are mature, but do get extended occasionally.

## Deployment
[Microsoft.Extensions.Configuration.UserSecrets](https://www.nuget.org/packages/Microsoft.Extensions.Configuration.UserSecrets/) is included in the ASP.NET Core shared framework. The package is deployed as out-of-band (OOB) too and can be referenced into projects directly.
