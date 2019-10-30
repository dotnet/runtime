// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCXMLException : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCXMLException
        // Test Case
        public override void AddChildren()
        {
            // for function v0
            {
                this.AddChild(new CVariation(v0) { Attribute = new Variation("XmlException()") });
            }


            // for function v1
            {
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlException(message)") });
            }


            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlException(message, innerException) ") });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("XmlException(message, innerException, line, position)") });
            }


            // for function ReadingElementWithDuplicateAttributesThrowsException
            {
                this.AddChild(new CVariation(ReadingElementWithDuplicateAttributesThrowsException) { Attribute = new Variation("Read an element with duplicate attributes") });
            }


            // for function ReadingForbiddenXmlNamespaceAliasThrows
            {
                this.AddChild(new CVariation(ReadingForbiddenXmlNamespaceAliasThrows) { Attribute = new Variation("Read an element with xmlns:xml namespace") });
            }


            // for function ReadingXmlElementWithNameStartingWithNumberThrows
            {
                this.AddChild(new CVariation(ReadingXmlElementWithNameStartingWithNumberThrows) { Attribute = new Variation("Read an XML element whose name starts with 1") });
            }


            // for function InvalidEncodingCharacterThrowsExceptionWithCorrectLineNumberAndPosition
            {
                this.AddChild(new CVariation(InvalidEncodingCharacterThrowsExceptionWithCorrectLineNumberAndPosition) { Attribute = new Variation("XmlReader: scanner error on invalid character for an encoding give pointer to scanner buffer, not file position") });
            }


            // for function UnexpectedTokenThrowsErrorWithCorrectPositions
            {
                this.AddChild(new CVariation(UnexpectedTokenThrowsErrorWithCorrectPositions) { Attribute = new Variation("XmlDocument.Load: XmlException contains ambiguous error when finding unexpected token") });
            }


            // for function SourceUri1
            {
                this.AddChild(new CVariation(SourceUri1) { Attribute = new Variation("Check to see if SourceUri1 is set correctly") });
            }
        }
    }
}
