# System.DirectoryServices.Protocols
This assembly contains types that provide a managed implementation of the Lightweight Directory Access Protocol (LDAP) version 3 and Directory Services Markup Language (DSML) version 2.0 (V2) standards. The main type used for interacting with an LDAP server is [LdapConnection](https://learn.microsoft.com/dotnet/api/system.directoryservices.protocols.ldapconnection) which uses some system native libraries to create a TCP/IP or UDP LDAP connection to a server, and is able to perform different types of requests to communicate to it. This assembly is supported both in Windows and Unix environments.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.directoryservices.protocols.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](/src/libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](/src/libraries/README.md#secondary-bars)

This assembly's API hasn't really changed much in the past years, and most of the investment have been towards adding Unix support since prior to .NET 5, this assembly was only supported in Windows. We still haven't achieved feature parity between Windows and Unix, so most of the investment will continue to be making sure that things that are supported in Windows would also work in Unix. Also, this assembly's API design was done prior to the existance of `async/await` model, and given it is heavily reliant on network communication, feature requests to modernize the API could also be considered.

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22+label%3Aarea-System.DirectoryServices) issues.

## Deployment
[System.DirectoryServices.Protocols](https://www.nuget.org/packages/System.DirectoryServices.Protocols) is a NuGet package that get's shipped along with every release of .NET.
