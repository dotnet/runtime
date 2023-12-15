# System.Private.Xml
This is the primary XML (eXtensible Markup Language) assembly. It provides standards-based support for processing XML.

It provides core implementations of various types including [`XMLReader`](https://learn.microsoft.com/dotnet/api/system.xml.xmlreader), [`XMLWriter`](https://learn.microsoft.com/dotnet/api/system.xml.xmlwriter), [`XMLDocument`](https://learn.microsoft.com/dotnet/api/system.xml.xmldocument), [`XMLSerializer`](https://learn.microsoft.com/dotnet/api/system.xml.serialization.xmlserializer), and more. These types are exposed via the various `System.Xml[.*]` assemblies.

Documentation can be found at https://learn.microsoft.com/dotnet/standard/serialization/introducing-xml-serialization.

## Contribution Bar
- [x] [We only consider lower-risk or high-impact fixes to maintain or improve quality](../../libraries/README.md#primary-bar)

## Source

* XmlReader and XmlWriter: [../System.Xml.ReaderWriter](../System.Xml.ReaderWriter)
* XmlDocument: [../System.Xml.XmlDocument](../System.Xml.XmlDocument)
* XmlSerializer: [../System.Xml.XmlSerializer](../System.Xml.XmlSerializer)

Higher level APIs for performing LINQ operations on XML entities:
* LINQ to XML: [../System.Private.Xml.Linq](../System.Private.Xml.Linq)
* XDocument: [../System.Xml.XDocument](../System.Xml.XDocument)
* XPath: [../System.Xml.XPath](../System.Xml.XPath), [../System.Xml.XPath.XDocument](../System.Xml.XPath.XDocument)

APIs to support the creation and validation of XML digital signatures:
* System.Security.Cryptography.Xml: [../System.Security.Cryptography.Xml](../System.Security.Cryptography.Xml); for example [`EncryptedXml`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.xml.encryptedxml) and [`SignedXml`](https://learn.microsoft.com/dotnet/api/system.security.cryptography.xml.signedxml).

## Deployment
The XML processing libraries are included in the shared framework and also shipped as NuGet packages.
* [System.Xml.ReaderWriter](https://www.nuget.org/packages/System.Xml.ReaderWriter)
* [System.Xml.XmlDocument](https://www.nuget.org/packages/System.Xml.XmlDocument)
* [System.Xml.XmlSerializer](https://www.nuget.org/packages/System.Xml.XmlSerializer)
* [System.Xml.XDocument](https://www.nuget.org/packages/System.Xml.XDocument)
* [System.Xml.XPath](https://www.nuget.org/packages/System.Xml.XPath)

The System.Security.Cryptography.Xml library is shipped as a NuGet package as part of .NET Platform Extensions.
* [System.Security.Cryptography.Xml](https://www.nuget.org/packages/System.Security.Cryptography.Xml/)

These NuGet packages are considered "legacy" and should not be referenced by projects compatible with .NET Standard 2.0.