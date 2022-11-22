# System.Security.Claims

This assembly provides support for claims-based identities for the [System.Security.Principal.IIdentity](https://learn.microsoft.com/dotnet/api/system.security.principal.iidentity) and [System.Security.Principal.IPrincipal](https://learn.microsoft.com/dotnet/api/system.security.principal.iprincipal) interfaces.

## Contribution Bar

- [x] [We only consider lower-risk or high-impact fixes to maintain or improve quality](../README.md#primary-bar)

## Source

* `Claim`, `ClaimsIdentity`, `ClaimsPrincipal`, `GenericIdentity`, `GenericPrincipal` and other foundational elements are in the [src](src/) subdirectory.
* `WindowsIdentity` and `WindowsPrincipal` are part of the [System.Security.Principal.Windows](../System.Security.Principal.Windows) library.
* Claims, Identities, or Principals created from the Microsoft.IdentityModel.Tokens.Saml, Microsoft.IdentityModel.JsonWebTokens, or System.IdentityModel.Tokens.Jwt assemblies are in the [AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/) repository.

## Deployment

The System.Security.Claims assembly is part of the shared framework, and ships with every new release of .NET.
