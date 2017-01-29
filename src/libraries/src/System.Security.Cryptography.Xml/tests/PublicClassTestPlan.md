# Test Plan

The goal is to write automated tests for the System.Security.Cryptography.Xml
namespace such that it can be incorporated into the .Net core libraries. 

There is no articulated quality bar, like a code coverage percentage. However,
there is an implicit high bar for the quality set for .Net core. This is 
security sensitive so additional testing of possible attacks and ensuring
no forseeable vulnerabilities is required. 

The code must comply with the spec (insert link here). It should also 
revisit the changes introduced in MS16-035 (insert details).

There is no deadline or other requirements.

The author of this file is @anthonylangsworth with @tintoy. Please contact
either of us if you have questions. @tintoy with @anthonylangsworth will
manage the overall contribution process.

## Methodology

To run tests:
1. Build [CoreFx on Windows](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md).
2. Load the solution file 
   `src\System.Security.Cryptography.Xml\System.Security.Cryptography.Xml.sln`
   in Visual Studio 2015 SP 1 or later, set the 
   System.Security.Cryptograpy.Xml.Tests project as the startup project and 
   hit F5.

Tests:
1. must be written using xUnit, as per the standard for .Net core.
2. must follow the C# 
   [Coding style](https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/coding-style.md) 
   guidelines. 
3. should follow the behavior of the existing 
   System.Security.Cryptography.Xml namespace. If this is not possible,
   such as from missing dependencies or platform differences, please 
   raise this for further discussion.
4. must pass.

This file will be deleted before merging into master.

## Assignments

The initial phase involves writing tests that cover all exposed public classes
and methods. The second phase will drill into use of `SignedXml` to ensure
it meets the standard.

### Phase 1 (Unit Testing)

The following table lists the public classes exposed by the 
System.Security.Cryptography.Xml namespace.

Done | Class                                        | Who
----------------------------------------------------|--------------------
 [ ] | CipherData                                   |
 [ ] | DataObject                                   |
 [ ] | EncryptedType                                |
 [ ] |  1.EncryptedData                             |
 [ ] |  2.EncryptedKey                              |
 [ ] | EncryptedReference                           |
 [ ] |  1.CipherReference                           |
 [ ] |  2.DataReference                             |
 [ ] |  3.KeyReference                              |
 [ ] | EncryptedXml                                 |
 [ ] | EncryptionMethod                             |
 [ ] | EncryptionProperty                           |
 [ ] | EncryptionPropertyCollection                 |
 [ ] | KeyInfo                                      |
 [ ] | KeyInfoClause                                |
 [ ] |  1.DSAKeyValue                               |
 [ ] |  2.KeyInfoEncryptedKey                       |
 [ ] |  3.KeyInfoName                               |
 [ ] |  4.KeyInfoNode                               |
 [ ] |  5.KeyInfoRetrievalMethod                    |
 [ ] |  6.KeyInfoX509Data                           |
 [ ] |  7.RSAKeyValue                               |
 [ ] | Reference                                    |
 [ ] | ReferenceList                                |
 [ ] | Signature                                    |
 [ ] | SignedInfo                                   |
 [ ] | SignedXml (unit testing only)                |
 [ ] | Transform                                    |
 [ ] |  1.XmlDecryptionTransform                    |
 [ ] |  2.XmlDsigBase64Transform                    |
 [ ] |  3.XmlDsigC14NTransform                      |
 [ ] |  4.XmlDsigC14NWithCommentsTransform          |
 [ ] |  5.XmlDsigEnvelopedSignatureTransform        |
 [ ] |  6.XmlDsigExcC14NTransform                   |
 [ ] |  7.XmlDsigExcC14NWithCommentsTransform       |
 [ ] |  8.XmlDsigXPathTransform                     |
 [ ] |  9.XmlDsigXsltTransform                      |
 [ ] |  10.XmlLicenseTransform                      |
 [ ] | TransformChain                               |
 [ ] | X509IssuerSerial                             |
	
### Phase 3 (Spec and Integration Testing)

TBA

## Questions

1. (Assigned to @anthonylangsworth) Can we use InternalsVisibleTo to get access 
   to the internal classes? It will certainly make testing easier but risks 
   possible exploitation.