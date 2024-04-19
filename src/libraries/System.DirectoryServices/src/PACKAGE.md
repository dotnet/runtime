## About

<!-- A description of the package and where one can find more documentation -->

Provides easy access to [Active Directory Domain Services](https://learn.microsoft.com/windows/win32/ad/active-directory-domain-services) from managed code. `Microsoft Active Directory Domain Services` are the foundation for distributed networks built on Windows 2000 Server, Windows Server 2003 and Microsoft Windows Server 2008 operating systems that use domain controllers. The namespace contains two component classes, [DirectoryEntry](https://learn.microsoft.com/dotnet/api/system.directoryservices.directoryentry) and [DirectorySearcher](https://learn.microsoft.com/dotnet/api/system.directoryservices.directorysearcher), which use the Active Directory Services Interfaces (ADSI) technology. ADSI is the set of interfaces that Microsoft provides as a flexible tool for working with a variety of network providers. ADSI gives the administrator the ability to locate and manage resources on a network with relative ease, regardless of the size of the network.

## Key Features

<!-- The key features of this package -->

Active Directory Domain Services use a tree structure. Each node in the tree contains a set of properties. Use this library to traverse, search, and modify the tree, and read and write to the properties of a node.

* The [DirectoryEntry](https://learn.microsoft.com/dotnet/api/system.directoryservices.directoryentry) class encapsulates a node or object in the Active Directory Domain Services hierarchy. Use this class for binding to objects, reading properties, and updating attributes. Together with helper classes, DirectoryEntry provides support for life-cycle management and navigation methods, including creating, deleting, renaming, moving a child node, and enumerating children.
* Use the [DirectorySearcher](https://learn.microsoft.com/dotnet/api/system.directoryservices.directorysearcher) class to perform queries against the Active Directory Domain Services hierarchy. LDAP is the only system-supplied Active Directory Service Interfaces (ADSI) provider that supports searching. A search of the Active Directory Domain Services hierarchy through [DirectorySearcher](https://learn.microsoft.com/dotnet/api/system.directoryservices.directorysearcher) returns instances of [SearchResult](https://learn.microsoft.com/dotnet/api/system.directoryservices.searchresult), which are contained in an instance of the [SearchResultCollection](https://learn.microsoft.com/dotnet/api/system.directoryservices.searchresultcollection) class.
* Network administrators write scripts and applications that access Active Directory Domain Services to automate common administrative tasks, such as adding users and groups, managing printers, and setting permissions for network resources.

## How to Use

<!-- A compelling example on how to use this package with code, as well as any specific guidelines for when to use the package -->

Install the `System.DirectoryServices` library from nuget

```dotnetcli
dotnet add package System.DirectoryServices --version 7.0.1
```

The sample needs a real path to an Active Directory server to work properly:

```cs
using System.DirectoryServices;

namespace TestDirectoryServices
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DirectoryEntry rootDse = new DirectoryEntry("LDAP://RootDSE");
            string configNamingContext = rootDse.Properties["configurationNamingContext"].Value.ToString();

            DirectoryEntry certTemplates = new DirectoryEntry("LDAP://CN=Certificate Templates,CN=Public Key Services,CN=Services," + configNamingContext);
            DirectorySearcher templatesSearch = new DirectorySearcher(certTemplates, "(objectClass=pKICertificateTemplate)", null, SearchScope.OneLevel);

            SearchResultCollection templates = templatesSearch.FindAll();

            foreach (SearchResult template in templates)
            {
                Console.WriteLine($"Name: {template.Properties["name"][0]} ({template.Properties["displayName"][0]})");
                Console.WriteLine($"Flags: {template.Properties["msPKI-Enrollment-Flag"][0]}");
            }
        }
    }
}
```

## Main Types

<!-- The main types provided in this library -->

The main types provided by this library are:

* `System.DirectoryServices.DirectoryEntry`
* `System.DirectoryServices.DirectorySearcher`

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.directoryservices)
* [Active Directory Domain Services](https://learn.microsoft.com/windows/win32/ad/active-directory-domain-services)
* [Active Directory Service Interfaces](https://learn.microsoft.com/windows/win32/adsi/active-directory-service-interfaces-adsi)
* [Lightweight Directory Access Protocol (LDAP)](https://learn.microsoft.com/previous-versions/windows/desktop/ldap/lightweight-directory-access-protocol-ldap-api)

## Related Packages

* [System.DirectoryServices.AccountManagement](https://learn.microsoft.com/dotnet/api/system.directoryservices.accountmanagement)
* [System.DirectoryServices.Protocols](https://learn.microsoft.com/dotnet/api/system.directoryservices.protocols)

## Feedback & Contributing

<!-- How to provide feedback on this package and contribute to it -->

System.DirectoryServices is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
