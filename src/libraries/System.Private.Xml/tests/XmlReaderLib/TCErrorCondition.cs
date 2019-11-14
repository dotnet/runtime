// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCErrorCondition : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCErrorCondition
        // Test Case
        public override void AddChildren()
        {
            // for function v1
            {
                this.AddChild(new CVariation(v1) { Attribute = new Variation("new XmlTextReader(null)") { Param = 17 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((Stream)null, null, (XmlParserContext)null)") { Param = 9 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((string)null, null, null)") { Param = 10 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((TextReader)null, null, (string)null)") { Param = 11 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((TextReader)null, null, (XmlParserContext)null)") { Param = 12 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("new XmlNamespaceManager(null)") { Param = 13 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.IsName(null)") { Param = 14 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.IsNameToken(null)") { Param = 15 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("new XmlValidatingReader(null)") { Param = 16 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((string)null)") { Param = 1 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("new XmlNodeReader(null)") { Param = 18 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((TextReader)null)") { Param = 2 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((Stream)null)") { Param = 3 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((string)null, null)") { Param = 4 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((TextReader)null, null)") { Param = 5 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((Stream)null, null)") { Param = 6 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((XmlReader)null, null)") { Param = 7 } });
                this.AddChild(new CVariation(v1) { Attribute = new Variation("XmlReader.Create((Stream)null, null, (string) null)") { Param = 8 } });
            }


            // for function v1a
            {
                this.AddChild(new CVariation(v1a) { Attribute = new Variation("XmlReader.Create(String.Empty)") { Param = 1 } });
                this.AddChild(new CVariation(v1a) { Attribute = new Variation("XmlReader.Create(String.Empty, null)") { Param = 2 } });
                this.AddChild(new CVariation(v1a) { Attribute = new Variation("XmlReader.Create(String.Empty, null, (XmlParserContext)null)") { Param = 3 } });
            }


            // for function v1b
            {
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("XmlReader.Create(TextReader, XmlReaderSettings, XmlParserContext)") { Param = 12 } });
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("XmlReader.Create(String)") { Param = 2 } });
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("XmlReader.Create(TextReader)") { Param = 3 } });
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("XmlReader.Create(String, XmlReaderSettings)") { Param = 5 } });
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("XmlReader.Create(TextReader, XmlReaderSettings)") { Param = 6 } });
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("XmlReader.Create(XmlReader, XmlReaderSettings)") { Param = 7 } });
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("XmlReader.Create(String, XmlReaderSettings, XmlParserContext)") { Param = 10 } });
                this.AddChild(new CVariation(v1b) { Attribute = new Variation("XmlReader.Create(TextReader, XmlReaderSettings, string)") { Param = 11 } });
            }


            // for function v2
            {
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadContentAsBase64(null, 0, 0)") { Param = 7 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsLong('a', null)") { Param = 26 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsObject(null, null)") { Param = 27 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsObject('a', null)") { Param = 28 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsString(null, null)") { Param = 29 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsString('a', null)") { Param = 30 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToDescendant(null)") { Param = 31 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToDescendant(null, null)") { Param = 32 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToDescendant('a', null)") { Param = 33 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToFollowing(null)") { Param = 34 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToFollowing(null, null)") { Param = 35 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToFollowing('a', null)") { Param = 36 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToNextSibling(null)") { Param = 37 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToNextSibling(null, null)") { Param = 38 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadToNextSibling('a', null)") { Param = 39 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadValueChunk(null, 0, 0)") { Param = 40 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadContentAs(null, null)") { Param = 41 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadContentAsBinHex(null, 0, 0)") { Param = 8 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAs(null, null, null, null)") { Param = 9 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAs(null, null, 'a', null)") { Param = 10 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsBase64(null, 0, 0)") { Param = 11 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsBinHex(null, 0, 0)") { Param = 12 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsBoolean(null, null)") { Param = 13 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsBoolean('a', null)") { Param = 14 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsDecimal(null, null)") { Param = 17 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsDecimal('a', null)") { Param = 18 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsDouble(null, null)") { Param = 19 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsDouble('a', null)") { Param = 20 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsFloat(null, null)") { Param = 21 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsFloat('a', null)") { Param = 22 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsInt(null, null)") { Param = 23 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsInt('a', null)") { Param = 24 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.ReadElementContentAsLong(null, null)") { Param = 25 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader[null])") { Param = 1 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader[null, null]") { Param = 2 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.GetAttribute(null)") { Param = 3 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.GetAttribute(null, null)") { Param = 4 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.MoveToAttribute(null)") { Param = 5 } });
                this.AddChild(new CVariation(v2) { Attribute = new Variation("XmlReader.MoveToAttribute(null, null)") { Param = 6 } });
            }


            // for function v3
            {
                this.AddChild(new CVariation(v3) { Attribute = new Variation("XmlReader.GetAttribute(-1)") { Param = 3 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("XmlReader.GetAttribute(0)") { Param = 4 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("XmlReader.MoveToAttribute(-1)") { Param = 5 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("XmlReader.MoveToAttribute(0)") { Param = 6 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("XmlReader[0]") { Param = 2 } });
                this.AddChild(new CVariation(v3) { Attribute = new Variation("XmlReader[-1]") { Param = 1 } });
            }


            // for function v4
            {
                this.AddChild(new CVariation(v4) { Attribute = new Variation("nsm.AddNamespace('p', null)") { Param = 1 } });
                this.AddChild(new CVariation(v4) { Attribute = new Variation("nsm.RemoveNamespace('p', null)") { Param = 2 } });
            }


            // for function v5
            {
                this.AddChild(new CVariation(v5) { Attribute = new Variation("nsm.RemoveNamespace(null, 'ns1')") { Param = 2 } });
                this.AddChild(new CVariation(v5) { Attribute = new Variation("nsm.AddNamespace(null, 'ns1')") { Param = 1 } });
            }


            // for function v6
            {
                this.AddChild(new CVariation(v6) { Attribute = new Variation("DataReader.ReadContentAs(null, nsm)") });
            }


            // for function v7
            {
                this.AddChild(new CVariation(v7) { Attribute = new Variation("nsm.AddNamespace('xml', 'ns1')") });
            }


            // for function V8
            {
                this.AddChild(new CVariation(V8) { Attribute = new Variation("Test Integrity of all values after Error") });
            }


            // for function V9a
            {
                this.AddChild(new CVariation(V9a) { Attribute = new Variation("2.Test Integrity of all values after Dispose") });
            }


            // for function v10
            {
                this.AddChild(new CVariation(v10) { Attribute = new Variation("XmlCharType::IsName, IsNmToken") });
            }


            // for function v11
            {
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAs(String.Empty, String.Empty, String.Empty, String.Empty)") { Param = 9 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsString('a', String.Empty)") { Param = 30 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToDescendant(String.Empty)") { Param = 31 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToDescendant(String.Empty, String.Empty)") { Param = 32 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToDescendant('a', String.Empty)") { Param = 33 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToFollowing(String.Empty)") { Param = 34 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToFollowing(String.Empty, String.Empty)") { Param = 35 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToFollowing('a', String.Empty)") { Param = 36 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToNextSibling(String.Empty)") { Param = 37 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToNextSibling(String.Empty, String.Empty)") { Param = 38 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadToNextSibling('a', String.Empty)") { Param = 39 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsString(String.Empty, String.Empty)") { Param = 29 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader[String.Empty, String.Empty]") { Param = 2 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.GetAttribute(String.Empty)") { Param = 3 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.GetAttribute(String.Empty, String.Empty)") { Param = 4 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.MoveToAttribute(String.Empty)") { Param = 5 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.MoveToAttribute(String.Empty, String.Empty)") { Param = 6 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader[String.Empty])") { Param = 1 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAs(String.Empty, String.Empty, 'a', String.Empty)") { Param = 10 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsBoolean(String.Empty, String.Empty)") { Param = 13 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsBoolean('a', String.Empty)") { Param = 14 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsDecimal(String.Empty, String.Empty)") { Param = 17 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsDecimal('a', String.Empty)") { Param = 18 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsDouble(String.Empty, String.Empty)") { Param = 19 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsDouble('a', String.Empty)") { Param = 20 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsFloat(String.Empty, String.Empty)") { Param = 21 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsFloat('a', String.Empty)") { Param = 22 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsInt(String.Empty, String.Empty)") { Param = 23 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsInt('a', String.Empty)") { Param = 24 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsLong(String.Empty, String.Empty)") { Param = 25 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsLong('a', String.Empty)") { Param = 26 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsObject(String.Empty, String.Empty)") { Param = 27 } });
                this.AddChild(new CVariation(v11) { Attribute = new Variation("XmlReader.ReadElementContentAsObject('a', String.Empty)") { Param = 28 } });
            }


            // for function var13
            {
                this.AddChild(new CVariation(var13) { Attribute = new Variation("XmlReaderSettings.LineNumberOffset - invalid values") { Param = 1 } });
                this.AddChild(new CVariation(var13) { Attribute = new Variation("XmlReaderSettings.LinePositionOffset - invalid values") { Param = 2 } });
            }


            // for function v14
            {
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadValueChunk(c, 0, -1)") { Param = 9 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadElementContentAsBase64(b, -1, 0)") { Param = 7 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadElementContentAsBinHex(b, -1, 0)") { Param = 8 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadValueChunk(c, -1, 1)") { Param = 16 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadContentAsBinHex(b, -1, 0)") { Param = 2 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadContentAsBase64(b, 0, -1)") { Param = 3 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadContentAsBinHex(b, 0, -1)") { Param = 4 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadContentAsBase64(b, 0, 2)") { Param = 5 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadContentAsBinHex(b, 0, 2)") { Param = 6 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadContentAsBase64(b, -1, 0)") { Param = 1 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadElementContentAsBase64(b, 0, -1)") { Param = 10 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadElementContentAsBinHex(b, 0, -1)") { Param = 11 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadValueChunk(c, 0, -1)") { Param = 12 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadElementContentAsBase64(b, 0, 2)") { Param = 13 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadElementContentAsBinHex(b, 0, 2)") { Param = 14 } });
                this.AddChild(new CVariation(v14) { Attribute = new Variation("XmlReader.ReadValueChunk(c, 0, 2)") { Param = 15 } });
            }


            // for function var15
            {
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.MaxCharactersFromEntities - readonly") { Param = 12 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.IgnoreWhitespace - readonly") { Param = 8 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.DtdProcessing - readonly") { Param = 13 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.MaxCharactersInDocument - readonly") { Param = 9 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.IgnoreComments - readonly") { Param = 6 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.IgnoreProcessingInstructions - readonly") { Param = 7 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.LineNumberOffset - readonly") { Param = 1 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.LinePositionOffset - readonly") { Param = 2 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.CheckCharacters - readonly") { Param = 3 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.CloseInput - readonly") { Param = 4 } });
                this.AddChild(new CVariation(var15) { Attribute = new Variation("DataReader.Settings.ConformanceLevel - readonly") { Param = 5 } });
            }


            // for function V16
            {
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsLong(a,b)") { Param = 31 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsDouble()") { Param = 26 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsFloat(a,b)") { Param = 27 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsFloat()") { Param = 28 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsInt(a,b)") { Param = 29 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsInt()") { Param = 30 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("Readcontentas in close state and call ReadContentAsBase64") { Param = 1 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsLong()") { Param = 32 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsObject(a,b)") { Param = 33 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsString(a,b)") { Param = 34 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadToDescendant(a)") { Param = 35 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadToFollowing(a)") { Param = 36 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadToNextSibling(a)") { Param = 37 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadAttributeValue()") { Param = 38 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ResolveEntity()") { Param = 39 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAs(typeof(String), null)") { Param = 12 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsObject()") { Param = 13 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsDouble(a,b)") { Param = 25 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("Readcontentas in close state and call ReadElementContentAsBase64") { Param = 3 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("Readcontentas in close state and call ReadElementContentAsBinHex") { Param = 4 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("Readcontentas in close state and call ReadValueChunk") { Param = 5 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader[a])") { Param = 6 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader[a, b]") { Param = 7 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.GetAttribute(a)") { Param = 8 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.GetAttribute(a, b)") { Param = 9 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.MoveToAttribute(a)") { Param = 10 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.MoveToAttribute(a, b)") { Param = 11 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAs(typeof(String), null)") { Param = 18 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsDecimal()") { Param = 24 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsString()") { Param = 14 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadToDescendant(a, b)") { Param = 15 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadToFollowing(a, b)") { Param = 16 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadToNextSibling(a, b)") { Param = 17 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("Readcontentas in close state and call ReadContentAsBinHex") { Param = 2 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsBoolean(a,b)") { Param = 19 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsBoolean()") { Param = 20 } });
                this.AddChild(new CVariation(V16) { Attribute = new Variation("XmlReader.ReadElementContentAsDecimal(a,b)") { Param = 23 } });
            }
        }
    }
}
