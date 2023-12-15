# System.Security.Cryptography.Xml

This assembly provides support for the [W3C XML Signature Syntax and Processing (First Edition)](https://www.w3.org/TR/xmldsig-core-20020212/) specification (also known as xmldsig), via the [SignedXml](https://learn.microsoft.com/dotnet/api/system.security.cryptography.xml.signedxml) class; and for the [W3C XML Encryption Syntax and Processing (First Edition)](https://www.w3.org/TR/2002/REC-xmlenc-core-20021210/) specification (also known as xmlenc), via the [EncryptedXml](https://learn.microsoft.com/dotnet/api/system.security.cryptography.xml.encryptedxml) class.

## Contribution Bar

- [x] [We only consider fixes that unblock critical issues](../README.md#primary-bar)

There are circumstances where this library and other implementations of xmldsig will produce incompatible answers (signatures produced by one cannot be verified by another).  These issues generally cannot be fixed, as they would invalidate signatures produced by previous versions of the library in those same circumstances.

Because of these incompatibilities, and design concerns with the specification itself, **we do not recommend the use of this library by any application** unless it needs to interoperate with existing files or services that are already based on these formats.

There are newer editions/versions of both the xmlenc and xmldsig specifications, but this implementation will not be updated to incorporate their changes.

## Deployment

The System.Security.Cryptography.Xml assembly is shipped as a [NuGet package](https://www.nuget.org/packages/System.Security.Cryptography.Xml/).
