# System.Private.Xml.Linq
This assembly implements APIs for processing XML entities with LINQ operations, along with APIs to navigate and validate these entities. See the [`System.Private.Xml` README file](../System.Private.Xml/README.md) for an overview of the XML-processing libraries the framework provides.

It provides core implementations of [`XDocument`](https://learn.microsoft.com/dotnet/api/system.xml.linq.xdocument), [`XContainer`](https://learn.microsoft.com/dotnet/api/system.xml.linq.xcontainer), and related types.

Documentation can be found at https://learn.microsoft.com/dotnet/standard/linq/linq-xml-overview.

## Contribution Bar
- [x] [We only consider lower-risk or high-impact fixes to maintain or improve quality](/src/libraries/README.md#primary-bar)

## Source
* XDocument: [../System.Xml.XDocument](../System.Xml.XDocument)
* XPath APIs for XDocument: [../System.Xml.XPath.XDocument](../System.Xml.XPath.XDocument)

## Deployment
The LINQ to XML processing libraries are included in the shared framework and also shipped as NuGet packages. The NuGet packages are considered "legacy" and should not be referenced by projects compatible with .NET Standard 2.0.
* [System.Xml.XDocument](https://www.nuget.org/packages/System.Xml.XDocument)
* [System.Xml.XPath.XDocument](https://www.nuget.org/packages/System.Xml.XPath.XDocument)