// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCErrorConditionWriter : XmlWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCErrorConditionWriter
        // Test Case
        public override void AddChildren()
        {
            // for function var_01
            {
                this.AddChild(new CVariation(var_01) { Attribute = new Variation("XmlWriter.Create((Stream)null, ws)") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var_01) { Attribute = new Variation("XmlWriter.Create((XmlWriter)null)") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var_01) { Attribute = new Variation("XmlWriter.Create((TextWriter)null, ws)") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var_01) { Attribute = new Variation("XmlWriter.Create((StringBuilder)null, ws)") { Param = 7, Pri = 2 } });
                this.AddChild(new CVariation(var_01) { Attribute = new Variation("XmlWriter.Create((XmlWriter)null, ws)") { Param = 8, Pri = 2 } });
                this.AddChild(new CVariation(var_01) { Attribute = new Variation("XmlWriter.Create((Stream)null)") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_01) { Attribute = new Variation("XmlWriter.Create((TextWriter)null)") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_01) { Attribute = new Variation("XmlWriter.Create((StringBuilder)null)") { Param = 3, Pri = 2 } });
            }


            // for function var_02
            {
                this.AddChild(new CVariation(var_02) { Attribute = new Variation("XmlWriter.WriteAttributes(null, false)") { Param = false, Pri = 2 } });
                this.AddChild(new CVariation(var_02) { Attribute = new Variation("XmlWriter.WriteAttributes(null, true)") { Param = true, Pri = 2 } });
            }


            // for function var_03
            {
                this.AddChild(new CVariation(var_03) { Attribute = new Variation("XmlWriter.WriteNode((XmlReader)null, true)") { Param = true, Pri = 2 } });
                this.AddChild(new CVariation(var_03) { Attribute = new Variation("XmlWriter.WriteNode((XmlReader)null, false)") { Param = false, Pri = 2 } });
            }


            // for function var_04
            {
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteProcessingInstruction(null, null)") { Param = 13, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteNmToken(null)") { Param = 12, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteStartElement('a', null, null)") { Param = 20, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteAttributeString(null, null, null)") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteAttributeString('a', null, null, null)") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteAttributeString(null, null, 'a', null)") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteDocType(null, null, null, null)") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteElementString(null, null)") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteElementString(null, null, null)") { Param = 7, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteElementString('a', null, null, null)") { Param = 8, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteElementString('a', null, 'a', null)") { Param = 9, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteEntityRef(null)") { Param = 10, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteName(null)") { Param = 11, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteAttributeString(null, null)") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteQualifiedName(null, null)") { Param = 14, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteStartAttribute(null)") { Param = 15, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteStartAttribute(null, null)") { Param = 16, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteStartAttribute('a', null, null)") { Param = 17, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteStartElement(null)") { Param = 18, Pri = 2 } });
                this.AddChild(new CVariation(var_04) { Attribute = new Variation("XmlWriter.WriteStartElement(null, null)") { Param = 19, Pri = 2 } });
            }


            // for function var_05
            {
                this.AddChild(new CVariation(var_05) { Attribute = new Variation("XmlWriter.WriteValue((object)null)") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var_05) { Attribute = new Variation("XmlWriter.WriteRaw(null)") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var_05) { Attribute = new Variation("XmlWriter.WriteBinHex(null)") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_05) { Attribute = new Variation("XmlWriter.WriteBase64(null)") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_05) { Attribute = new Variation("XmlWriter.WriteChars(null)") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var_05) { Attribute = new Variation("XmlWriter.LookupPrefix(null)") { Param = 4, Pri = 2 } });
            }


            // for function var_07
            {
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteDocType('a', '\ud800', '\ud800', '\ud800')") { Param = 30, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteNmToken('\ud800')") { Param = 21, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteStartAttribute('a', '\ud800', '\ud800')") { Param = 26, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteStartElement('\ud800')") { Param = 27, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteStartElement('\ud800', '\ud800')") { Param = 28, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteStartElement('a', '\ud800', '\ud800')") { Param = 29, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteComment") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteDocType('a', 'b', '\ud800', '\ud800')") { Param = 31, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteDocType('a', 'b', 'c', '\ud800')") { Param = 32, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteAttributeString('a', '\ud800')") { Param = 33, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteElementString('a', '\ud800')") { Param = 34, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteProcessingInstruction('a', '\ud800')") { Param = 35, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteQualifiedName('a', '\ud800')") { Param = 36, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteRaw") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteName('\ud800')") { Param = 20, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteProcessingInstruction('\ud800', '\ud800')") { Param = 22, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteQualifiedName('\ud800', '\ud800')") { Param = 23, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteStartAttribute('\ud800')") { Param = 24, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteStartAttribute('\ud800', '\ud800')") { Param = 25, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteCharEntity") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteCData") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteEntityRef") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteString") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteSurrogateCharEntity") { Param = 7, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteValue((string)null)") { Param = 8, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteWhitespace") { Param = 9, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteAttributeString('\ud800', '\ud800')") { Param = 10, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteAttributeString('a', '\ud800', '\ud800')") { Param = 11, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteAttributeString('a', 'b', '\ud800', '\ud800')") { Param = 12, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteAttributeString('a', 'b', 'c', '\ud800')") { Param = 13, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteDocType('\ud800', '\ud800', '\ud800', '\ud800')") { Param = 14, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteElementString('\ud800', '\ud800')") { Param = 15, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteElementString('a', '\ud800', '\ud800')") { Param = 16, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteElementString('a', 'b', '\ud800', '\ud800')") { Param = 17, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteElementString('a', 'b', 'c', '\ud800')") { Param = 18, Pri = 2 } });
                this.AddChild(new CVariation(var_07) { Attribute = new Variation("XmlWriter.WriteEntityRef('\ud800')") { Param = 19, Pri = 2 } });
            }


            // for function var_10
            {
                this.AddChild(new CVariation(var_10) { Attribute = new Variation("XmlWriter:WriteRaw with wrong size throws wrong exception") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_10) { Attribute = new Variation("XmlWriter:WriteBase64 with wrong size throws wrong exception") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var_10) { Attribute = new Variation("XmlWriter:WriteBinHex with wrong size throws wrong exception") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var_10) { Attribute = new Variation("XmlWriter:WriteChars with wrong size throws wrong exception") { Param = 1, Pri = 2 } });
            }


            // for function var_11
            {
                this.AddChild(new CVariation(var_11) { Attribute = new Variation("XmlWriterSettings.ConformanceLevel - invalid values") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_11) { Attribute = new Variation("XmlWriterSettings.NewLineHandling - invalid values") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_11) { Attribute = new Variation("XmlWriterSettings.ConformanceLevel - invalid values") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var_11) { Attribute = new Variation("XmlWriterSettings.NewLineHandling - invalid values") { Param = 4, Pri = 2 } });
            }


            // for function var_12
            {
                this.AddChild(new CVariation(var_12) { Attribute = new Variation("XmlWriter.Create((TextWriter), ws)") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var_12) { Attribute = new Variation("XmlWriter.Create((XmlWriter))") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var_12) { Attribute = new Variation("XmlWriter.Create((Stream), ws)") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var_12) { Attribute = new Variation("XmlWriter.Create((Stream))") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_12) { Attribute = new Variation("XmlWriter.Create((StringBuilder), ws)") { Param = 7, Pri = 2 } });
                this.AddChild(new CVariation(var_12) { Attribute = new Variation("XmlWriter.Create((XmlWriter), ws)") { Param = 8, Pri = 2 } });
                this.AddChild(new CVariation(var_12) { Attribute = new Variation("XmlWriter.Create((TextWriter))") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_12) { Attribute = new Variation("XmlWriter.Create((StringBuilder)l)") { Param = 3, Pri = 2 } });
            }


            // for function var_13
            {
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteElementString(String.Empty, String.Empty, String.Empty, String.Empty)") { Param = 14, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteStartElement(String.Empty, String.Empty, String.Empty)") { Param = 25, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteDocType(String.Empty, String.Empty, String.Empty, String.Empty)") { Param = 26, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteProcessingInstruction(String.Empty, String.Empty)") { Param = 27, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteAttributeString(String.Empty, String.Empty, 'String.Empty, String.Empty)") { Param = 10, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteDocType(String.Empty, String.Empty, String.Empty, String.Empty)") { Param = 11, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteStartElement(String.Empty, String.Empty)") { Param = 24, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteCData(String.Empty)") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteEntityRef(String.Empty)") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteRaw(String.Empty)") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteString(String.Empty)") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteValue(String.Empty)") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteWhitespace(String.Empty)") { Param = 7, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteAttributeString(String.Empty)") { Param = 8, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteAttributeString(String.Empty, String.Empty, String.Empty)") { Param = 9, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteElementString(String.Empty, String.Empty)") { Param = 12, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteElementString(String.Empty, String.Empty, String.Empty)") { Param = 13, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteComment(String.Empty)") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteEntityRef(String.Empty)") { Param = 15, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteName(String.Empty)") { Param = 16, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteNmToken(String.Empty)") { Param = 17, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteProcessingInstruction(String.Empty, String.Empty)") { Param = 18, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteQualifiedName(String.Empty, String.Empty)") { Param = 19, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteStartAttribute(String.Empty)") { Param = 20, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteStartAttribute(String.Empty, String.Empty)") { Param = 21, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteStartAttribute(String.Empty, String.Empty, String.Empty)") { Param = 22, Pri = 2 } });
                this.AddChild(new CVariation(var_13) { Attribute = new Variation("XmlWriter.WriteStartElement(String.Empty)") { Param = 23, Pri = 2 } });
            }


            // for function var_14
            {
                this.AddChild(new CVariation(var_14) { Attribute = new Variation("XmlWriterSettings.NewLineChars - invalid values") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_14) { Attribute = new Variation("XmlWriterSettings.IndentChars - invalid values") { Param = 1, Pri = 2 } });
            }


            // for function var_15
            {
                this.AddChild(new CVariation(var_15) { Attribute = new Variation("XmlWriter properties after Dispose") { Pri = 2 } });
            }


            // for function var_16
            {
                this.AddChild(new CVariation(var_16) { Attribute = new Variation("XmlWriter properties after Error") { Pri = 2 } });
            }


            // for function bug601305
            {
                this.AddChild(new CVariation(bug601305) { Attribute = new Variation("601305 XmlWriter.WriteStartElement() should inspect attributes before emitting the element tag") { Pri = 2 } });
            }


            // for function var17
            {
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.CheckCharacters - readonly") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.Indent - readonly") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.IndentChars - readonly") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.OmitXmlDeclaration - readonly") { Param = 10, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.CloseOutput - readonly") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.ConformanceLevel - readonly") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.Encoding - readonly") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.NewLineChars - readonly") { Param = 7, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.NewLineHandling - readonly") { Param = 8, Pri = 2 } });
                this.AddChild(new CVariation(var17) { Attribute = new Variation("writer.Settings.NewLineOnAttributes - readonly") { Param = 9, Pri = 2 } });
            }


            // for function var_18
            {
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteName after close") { Param = 17, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteStartElement after close") { Param = 25, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteString after close") { Param = 26, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteSurrogateCharEntity after close") { Param = 27, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteValue after close") { Param = 28, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteWhitespace after close") { Param = 29, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteNmToken after close") { Param = 18, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteNode after close") { Param = 19, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteProcessingInstruction after close") { Param = 20, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteEntityRef after close") { Param = 15, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteFullEndElement after close") { Param = 16, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteRaw(string) after close") { Param = 21, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteRaw(buffer) after close") { Param = 22, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteStartAttribute after close") { Param = 23, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteStartDocument after close") { Param = 24, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteQualifiedName after close") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteAttributes after close") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteAttributeString after close") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteBase64 after close") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteBinHex after close") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteCData after close") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteCharEntity after close") { Param = 7, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteChars after close") { Param = 8, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteComment after close") { Param = 9, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteDocType after close") { Param = 10, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteElementString after close") { Param = 11, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteEndAttribute after close") { Param = 12, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteEndDocument after close") { Param = 13, Pri = 2 } });
                this.AddChild(new CVariation(var_18) { Attribute = new Variation("WriteEndElement after close") { Param = 14, Pri = 2 } });
            }


            // for function var_19
            {
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteQualifiedName after error") { Param = 1, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteStartDocument after error") { Param = 24, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteString after error") { Param = 26, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteSurrogateCharEntity after error") { Param = 27, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteValue after error") { Param = 28, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteWhitespace after error") { Param = 29, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteElementString after error") { Param = 11, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteStartElement after error") { Param = 25, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteAttributes after error") { Param = 2, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteAttributeString after error") { Param = 3, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteBase64 after error") { Param = 4, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteBinHex after error") { Param = 5, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteCData after error") { Param = 6, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteCharEntity after error") { Param = 7, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteChars after error") { Param = 8, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteComment after error") { Param = 9, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteDocType after error") { Param = 10, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteName after error") { Param = 17, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteStartAttribute after error") { Param = 23, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteEndDocument after error") { Param = 13, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteEndElement after error") { Param = 14, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteEntityRef after error") { Param = 15, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteFullEndElement after error") { Param = 16, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteEndAttribute after error") { Param = 12, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteNmToken after error") { Param = 18, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteNode after error") { Param = 19, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteProcessingInstruction after error") { Param = 20, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteRaw(string) after error") { Param = 21, Pri = 2 } });
                this.AddChild(new CVariation(var_19) { Attribute = new Variation("WriteRaw(buffer) after error") { Param = 22, Pri = 2 } });
            }


            // for function var_20
            {
                this.AddChild(new CVariation(var_20) { Attribute = new Variation("504503:Assert when write attribute value with invalid surrogate pair and encoding ASCII") });
            }


            // for function var_21
            {
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteString attr swap surrogate pair") { Param = 33 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteQualifiedName1 swap surrogate pair") { Param = 23 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteStartAttribute2 swap surrogate pair") { Param = 26 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteStartAttribute3 swap surrogate pair") { Param = 27 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteStartElemen1 swap surrogate pair") { Param = 28 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteStartElemen2 swap surrogate pair") { Param = 29 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteStartElemen3 swap surrogate pair") { Param = 30 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteString swap surrogate pair") { Param = 31 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteWhitespace swap surrogate pair") { Param = 32 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteValue attr swap surrogate pair") { Param = 1 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteSurrogateCharEntity swap surrogate pair") { Param = 34 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteValue elem swap surrogate pair") { Param = 6 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteNmToken swap surrogate pair") { Param = 22 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteComment attr swap surrogate pair") { Param = 2 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteQualifiedName2 swap surrogate pair") { Param = 24 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteStartAttribute1 swap surrogate pair") { Param = 25 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteCData attr swap surrogate pair") { Param = 3 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteProcessingInstruction attr swap surrogate pair") { Param = 4 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteRaw attr swap surrogate pair") { Param = 5 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteComment elem swap surrogate pair") { Param = 7 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteCData elem swap surrogate pair") { Param = 8 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteProcessingInstruction elem swap surrogate pair") { Param = 9 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteRaw elem swap surrogate pair") { Param = 10 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteAttributeString swap surrogate pair") { Param = 11 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteCharEntity swap surrogate pair") { Param = 12 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteDocType pubid swap surrogate pair") { Param = 13 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteDocType sysid swap surrogate pair") { Param = 14 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteDocType subset swap surrogate pair") { Param = 15 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteElementString1 swap surrogate pair") { Param = 16 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteElementString2 swap surrogate pair") { Param = 17 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteElementString3 swap surrogate pair") { Param = 18 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteElementString4 swap surrogate pair") { Param = 19 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteEntityRef swap surrogate pair") { Param = 20 } });
                this.AddChild(new CVariation(var_21) { Attribute = new Variation("WriteName swap surrogate pair") { Param = 21 } });
            }


            // for function bug600541
            {
                this.AddChild(new CVariation(bug600541) { Attribute = new Variation("System.Xml.XmlWriter::WriteAttributes") { Param = 2 } });
                this.AddChild(new CVariation(bug600541) { Attribute = new Variation("System.Xml.XmlWriter::WriteAttributes") { Param = 3 } });
                this.AddChild(new CVariation(bug600541) { Attribute = new Variation("System.Xml.XmlWriter::WriteAttributes") { Param = 1 } });
                this.AddChild(new CVariation(bug600541) { Attribute = new Variation("System.Xml.XmlWriter::WriteAttributes") { Param = 4 } });
            }


            // for function bug630890
            {
                this.AddChild(new CVariation(bug630890) { Attribute = new Variation("Schema.XmlUntypedStringConverter::ToString") });
            }


            // for function PassingArrayWithNullOrEmptyItemsCausesWriteValueToFail
            {
                this.AddChild(new CVariation(PassingArrayWithNullOrEmptyItemsCausesWriteValueToFail) { Attribute = new Variation("PassingArrayWithNullOrEmptyItemsCausesWriteValueToFail:XMLWriter.WriteValue fails with ArgumentNullException if passed an array which has null/empty items") });
            }
        }
    }
}
