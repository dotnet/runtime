## About

<!-- A description of the package and where one can find more documentation -->

Provides support for managing access control lists for `Microsoft.Win32.RegistryKey`.

## Key Features

<!-- The key features of this package -->

* Get access control lists for a registry key.
* Get a specific sections of an access control list.
* Set the access control list for a registry key.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```csharp
using Microsoft.Win32;
using System.Security.AccessControl;

// Open a registry key (or create it if it doesn't exist)
using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("TestKey");
if (registryKey == null)
{
    Console.WriteLine("Failed to create or open the registry key.");
    return;
}

// Get the current access control list (ACL) for the registry key
RegistrySecurity registrySecurity = registryKey.GetAccessControl();
Console.WriteLine("Current Access Control List (ACL):");
Console.WriteLine(registrySecurity.GetSecurityDescriptorSddlForm(AccessControlSections.Access));

// Create a new access rule granting full control to the current user
string currentUser = Environment.UserName;
RegistryAccessRule accessRule = new RegistryAccessRule(currentUser, RegistryRights.FullControl, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow);

// Add the new access rule to the ACL
registrySecurity.AddAccessRule(accessRule);

// Set the updated ACL on the registry key
registryKey.SetAccessControl(registrySecurity);

// Get and display the updated ACL for the registry key using the second GetAccessControl overload
RegistrySecurity updatedRegistrySecurity = registryKey.GetAccessControl(AccessControlSections.Access);
Console.WriteLine("Updated Access Control List (ACL):");
Console.WriteLine(updatedRegistrySecurity.GetSecurityDescriptorSddlForm(AccessControlSections.Access));
```

## Main Types

<!-- The main types provided in this library -->

The main type provided by this library is:

* `Microsoft.Win32.RegistryAclExtensions`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* [API documentation](https://learn.microsoft.com/dotnet/api/microsoft.win32.registryaclextensions)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

Microsoft.Win32.Registry.AccessControl is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
