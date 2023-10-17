## About

<!-- A description of the package and where one can find more documentation -->

System.DirectoryServices.Protocols provides a managed implementation of Lightweight Directory Access Protocol (LDAP) version 3 and Directory Services Markup Language (DSML) version 2.0 (V2) standards.

It primarily uses the `LdapConnection` type for interacting with LDAP servers, using system native libraries to establish TCP/IP or UDP LDAP connections.
Supports both Windows and Unix, but certain features, such as setting client or server certificate options, are not available on Unix.

## Key Features

<!-- The key features of this package -->

* Managed implementation of LDAP v3 and DSML V2 standards.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Using the `LdapConnection` type, you can establish connections to LDAP servers and issue requests.

Here is a simple example:

```csharp
using System.DirectoryServices.Protocols;

// Create a new LdapConnection instance using the server URL.
using (LdapConnection connection = new LdapConnection("ldap.example.com")) {

    // Some credentials
    connection.Credential = new NetworkCredential(dn, password);

    // Connect to the server
    connection.Bind();

    // Perform LDAP operations
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.DirectoryServices.Protocols.LdapConnection`
* `System.DirectoryServices.Protocols.DirectoryAttribute`
* `System.DirectoryServices.Protocols.DirectoryOperation`
* `System.DirectoryServices.Protocols.DirectoryRequest`
* `System.DirectoryServices.Protocols.DirectoryResponse`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/system.directoryservices.protocols)
* [Active Directory Domain Services](https://learn.microsoft.com/windows/win32/ad/active-directory-domain-services)

## Related Packages

<!-- The related packages associated with this package -->

* [System.DirectoryServices](https://www.nuget.org/packages/System.DirectoryServices/)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.DirectoryServices.Protocols is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
