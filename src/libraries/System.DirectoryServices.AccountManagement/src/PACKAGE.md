## About

<!-- A description of the package and where one can find more documentation -->
Provides uniform access and manipulation of security principals across multiple principal stores. The principal objects in the Account Management API include computer, group and user objects. The principal stores includes:
 * Active Directory Domain Services (AD DS)
 * Active Directory Lightweight Directory Services (AD LDS)
 * Machine SAM (MSAM).

## Key Features

<!-- The key features of this package -->

* Basic directory operations such as creating and updating security principals. The application requires less knowledge of the underlying stores to perform these operations.
* Applications can extend the object model to include new types of directory objects.
* Account management tasks, such as enabling and disabling a user account.
* Cross-store support allows group objects in the Active Directory Domain Services (AD DS), Active Directory Lightweight Directory Services (AD LDS), and Machine SAM (MSAM) databases to contain members from different types of stores.
* Query by example searching, available on the PrincipalSearcher class, enables applications to set properties on a principal object and search the selected store for other objects that contain matching property values.
* Enhanced search on computer, user and group principal objects enables applications to search the selected store for matching principal objects.
* Recursive search, available on the group principal object, enables applications to search a group recursively and return only principal objects that are leaf nodes.
* Credential validation against the Machine SAM, AD DS, and AD LS stores.
* Connections speeds are increased by using the Fast Concurrent Bind (FSB) feature when available. Connection caching decreases the number of ports used.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

```cs
// Create the principal context for the usr object.
PrincipalContext ctx = new PrincipalContext(ContextType.Domain, "fabrikam.com", "CN=Users,DC=fabrikam,DC=com", "administrator", "securelyStoredPassword");

// Create the principal user object from the context.
UserPrincipal usr = new UserPrincipal(ctx);
usr.AdvancedSearchFilter.LastLogonTime(DateTime.Now, MatchType.LessThan); 
usr.AdvancedSearchFilter.LastLogonTime(DateTime.Yesterday, MatchType.GreaterThan);

// Create a PrincipalSearcher object.
PrincipalSearcher ps = new PrincipalSearcher(usr);
PrincipalSearchResult<Principal> fr = ps.FindAll();
foreach (UserPrincipal u in results)
{
    Console.WriteLine(u.Name);
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.DirectoryServices.AccountManagement.PrincipalContext`
* `System.DirectoryServices.AccountManagement.PrincipalSearcher`
* `System.DirectoryServices.AccountManagement.Principal` and its subclasses: `System.DirectoryServices.AccountManagement.UserPrincipal`, `System.DirectoryServices.AccountManagement.GroupPrincipal` and `System.DirectoryServices.AccountManagement.ComputerPrincipal`

## Additional Documentation

<!-- Links to further documentation. Remove conceptual documentation if not available for the library. -->

* Conceptual documentations:
  - [System.DirectoryServices.AccountManagement Namespace Overview](https://learn.microsoft.com/previous-versions/bb384379(v=vs.90))
  - [About System.DirectoryServices.AccountManagement](https://learn.microsoft.com//previous-versions/bb384375(v=vs.90))
  - [Using System.DirectoryServices.AccountManagement](https://learn.microsoft.com/previous-versions/bb384384(v=vs.90))
* API documentation
  - [System.DirectoryServices.AccountManagement namespace](https://learn.microsoft.com/dotnet/api/system.directoryservices.accountmanagement)

## Related Packages

[System.DirectoryServices](https://learn.microsoft.com/dotnet/api/system.directoryservices)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.DirectoryServices.AccountManagement is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
