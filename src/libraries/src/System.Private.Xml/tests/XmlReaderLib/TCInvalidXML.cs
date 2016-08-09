// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;

namespace System.Xml.Tests
{
    public partial class TCInvalidXML : TCXMLReaderBaseGeneral
    {
        // Type is System.Xml.Tests.TCInvalidXML
        // Test Case
        public override void AddChildren()
        {
            // for function Read36a
            {
                this.AddChild(new CVariation(Read36a) { Attribute = new Variation("Read with surrogate in attr.val.begin") });
            }


            // for function Read36b
            {
                this.AddChild(new CVariation(Read36b) { Attribute = new Variation("Read with surrogate in attr.val.mid") });
            }


            // for function Read36c
            {
                this.AddChild(new CVariation(Read36c) { Attribute = new Variation("Read with surrogate in attr.val.end") });
            }


            // for function Read37
            {
                this.AddChild(new CVariation(Read37) { Attribute = new Variation("Read with surrogate in a DTD") });
            }


            // for function Read37a
            {
                this.AddChild(new CVariation(Read37a) { Attribute = new Variation("Read with surrogate in a DTD.begin") });
            }


            // for function Read37b
            {
                this.AddChild(new CVariation(Read37b) { Attribute = new Variation("Read with surrogate in a DTD.mid") });
            }


            // for function Read37c
            {
                this.AddChild(new CVariation(Read37c) { Attribute = new Variation("Read with surrogate in a DTD.end") });
            }


            // for function InvalidCommentCharacters
            {
                this.AddChild(new CVariation(InvalidCommentCharacters) { Attribute = new Variation("For non-well-formed XMLs, check for the line info in the error message") });
            }


            // for function FactoryReaderInvalidCharacter
            {
                this.AddChild(new CVariation(FactoryReaderInvalidCharacter) { Attribute = new Variation("The XmlReader is reporting errors with -ve column values") });
            }


            // for function Read1
            {
                this.AddChild(new CVariation(Read1) { Attribute = new Variation("Read with invalid content") });
            }


            // for function Read2
            {
                this.AddChild(new CVariation(Read2) { Attribute = new Variation("Read with invalid end tag") });
            }


            // for function Read3
            {
                this.AddChild(new CVariation(Read3) { Attribute = new Variation("Read with invalid name") });
            }


            // for function Read4
            {
                this.AddChild(new CVariation(Read4) { Attribute = new Variation("Read with invalid characters") });
            }


            // for function Read6
            {
                this.AddChild(new CVariation(Read6) { Attribute = new Variation("Read with missing root end element") });
            }


            // for function Read11
            {
                this.AddChild(new CVariation(Read11) { Attribute = new Variation("Read with invalid namespace") });
            }


            // for function Read12
            {
                this.AddChild(new CVariation(Read12) { Attribute = new Variation("Attribute containing invalid character &") });
            }


            // for function Read13
            {
                this.AddChild(new CVariation(Read13) { Attribute = new Variation("Incomplete DOCTYPE") });
            }


            // for function Read14
            {
                this.AddChild(new CVariation(Read14) { Attribute = new Variation("Undefined namespace") });
            }


            // for function Read15
            {
                this.AddChild(new CVariation(Read15) { Attribute = new Variation("Read an XML Fragment which has unclosed elements") });
            }


            // for function Read21
            {
                this.AddChild(new CVariation(Read21) { Attribute = new Variation("Read invalid PIs") });
            }


            // for function Read22
            {
                this.AddChild(new CVariation(Read22) { Attribute = new Variation("Tag name > 4K, invalid") });
            }


            // for function Read22a
            {
                this.AddChild(new CVariation(Read22a) { Attribute = new Variation("Surrogate char in name > 4K, invalid") });
            }


            // for function Read23
            {
                this.AddChild(new CVariation(Read23) { Attribute = new Variation("Line number/position of whitespace before external entity (regression)") });
            }


            // for function Read24a
            {
                this.AddChild(new CVariation(Read24a) { Attribute = new Variation("1.Valid XML declaration.Errata5") { Param = "1.0" } });
            }


            // for function Read25b
            {
                this.AddChild(new CVariation(Read25b) { Attribute = new Variation("4.Invalid XML declaration.version") { Param = "0.9" } });
                this.AddChild(new CVariation(Read25b) { Attribute = new Variation("3.Invalid XML declaration.version") { Param = "0.1" } });
                this.AddChild(new CVariation(Read25b) { Attribute = new Variation("1.Invalid XML declaration.version") { Param = "2.0" } });
                this.AddChild(new CVariation(Read25b) { Attribute = new Variation("5.Invalid XML declaration.version") { Param = "1" } });
                this.AddChild(new CVariation(Read25b) { Attribute = new Variation("6.Invalid XML declaration.version") { Param = "1.a" } });
                this.AddChild(new CVariation(Read25b) { Attribute = new Variation("7.Invalid XML declaration.version") { Param = "#45" } });
                this.AddChild(new CVariation(Read25b) { Attribute = new Variation("8.Invalid XML declaration.version") { Param = "\\uD812" } });
                this.AddChild(new CVariation(Read25b) { Attribute = new Variation("2.Invalid XML declaration.version") { Param = "1.1." } });
            }


            // for function Read26b
            {
                this.AddChild(new CVariation(Read26b) { Attribute = new Variation("6.Invalid XML declaration.standalone") { Param = "0" } });
                this.AddChild(new CVariation(Read26b) { Attribute = new Variation("2.Invalid XML declaration.standalone") { Param = "false" } });
                this.AddChild(new CVariation(Read26b) { Attribute = new Variation("4.Invalid XML declaration.standalone") { Param = "No" } });
                this.AddChild(new CVariation(Read26b) { Attribute = new Variation("5.Invalid XML declaration.standalone") { Param = "1" } });
                this.AddChild(new CVariation(Read26b) { Attribute = new Variation("3.Invalid XML declaration.standalone") { Param = "Yes" } });
                this.AddChild(new CVariation(Read26b) { Attribute = new Variation("7.Invalid XML declaration.standalone") { Param = "#45" } });
                this.AddChild(new CVariation(Read26b) { Attribute = new Variation("8.Invalid XML declaration.standalone") { Param = "\\uD812" } });
                this.AddChild(new CVariation(Read26b) { Attribute = new Variation("1.Invalid XML declaration.standalone") { Param = "true" } });
            }


            // for function Read28
            {
                this.AddChild(new CVariation(Read28) { Attribute = new Variation("Read with invalid surrogate inside comment") });
            }


            // for function Read29a
            {
                this.AddChild(new CVariation(Read29a) { Attribute = new Variation("Read with invalid surrogate inside comment.begin") });
            }


            // for function Read29b
            {
                this.AddChild(new CVariation(Read29b) { Attribute = new Variation("Read with invalid surrogate inside comment.mid") });
            }


            // for function Read29c
            {
                this.AddChild(new CVariation(Read29c) { Attribute = new Variation("Read with invalid surrogate inside comment.end") });
            }


            // for function Read30
            {
                this.AddChild(new CVariation(Read30) { Attribute = new Variation("Read with invalid surrogate inside PI") });
            }


            // for function Read30a
            {
                this.AddChild(new CVariation(Read30a) { Attribute = new Variation("Read with invalid surrogate inside PI.begin") });
            }


            // for function Read30b
            {
                this.AddChild(new CVariation(Read30b) { Attribute = new Variation("Read with invalid surrogate inside PI.mid") });
            }


            // for function Read30c
            {
                this.AddChild(new CVariation(Read30c) { Attribute = new Variation("Read with invalid surrogate inside PI.end") });
            }


            // for function Read31
            {
                this.AddChild(new CVariation(Read31) { Attribute = new Variation("Read an invalid character which is a lower part of the surrogate pair") });
            }


            // for function Read31a
            {
                this.AddChild(new CVariation(Read31a) { Attribute = new Variation("Read an invalid character which is a lower part of the surrogate pair.begin") });
            }


            // for function Read31b
            {
                this.AddChild(new CVariation(Read31b) { Attribute = new Variation("Read an invalid character which is a lower part of the surrogate pair.mid") });
            }


            // for function Read31c
            {
                this.AddChild(new CVariation(Read31c) { Attribute = new Variation("Read an invalid character which is a lower part of the surrogate pair.end") });
            }


            // for function Read32
            {
                this.AddChild(new CVariation(Read32) { Attribute = new Variation("Read with surrogate in a name") });
            }


            // for function Read32a
            {
                this.AddChild(new CVariation(Read32a) { Attribute = new Variation("Read with surrogate in a name.begin") });
            }


            // for function Read32b
            {
                this.AddChild(new CVariation(Read32b) { Attribute = new Variation("Read with surrogate in a name.mid") });
            }


            // for function Read32c
            {
                this.AddChild(new CVariation(Read32c) { Attribute = new Variation("Read with surrogate in a name.end") });
            }


            // for function Read33
            {
                this.AddChild(new CVariation(Read33) { Attribute = new Variation("Read with invalid surrogate inside text") });
            }


            // for function Read33a
            {
                this.AddChild(new CVariation(Read33a) { Attribute = new Variation("Read with invalid surrogate inside text.begin") });
            }


            // for function Read33b
            {
                this.AddChild(new CVariation(Read33b) { Attribute = new Variation("Read with invalid surrogate inside text.mid") });
            }


            // for function Read33c
            {
                this.AddChild(new CVariation(Read33c) { Attribute = new Variation("Read with invalid surrogate inside text.end") });
            }


            // for function Read34
            {
                this.AddChild(new CVariation(Read34) { Attribute = new Variation("Read with invalid surrogate inside CDATA") });
            }


            // for function Read34a
            {
                this.AddChild(new CVariation(Read34a) { Attribute = new Variation("Read with invalid surrogate inside CDATA.begin") });
            }


            // for function Read34b
            {
                this.AddChild(new CVariation(Read34b) { Attribute = new Variation("Read with invalid surrogate inside CDATA.mid") });
            }


            // for function Read34c
            {
                this.AddChild(new CVariation(Read34c) { Attribute = new Variation("Read with invalid surrogate inside CDATA.end") });
            }


            // for function Read35
            {
                this.AddChild(new CVariation(Read35) { Attribute = new Variation("Read with surrogate in attr.name") });
            }


            // for function Read35a
            {
                this.AddChild(new CVariation(Read35a) { Attribute = new Variation("Read with surrogate in attr.name.begin") });
            }


            // for function Read35b
            {
                this.AddChild(new CVariation(Read35b) { Attribute = new Variation("Read with surrogate in attr.name.mid") });
            }


            // for function Read35c
            {
                this.AddChild(new CVariation(Read35c) { Attribute = new Variation("Read with surrogate in attr.name.end") });
            }


            // for function Read36
            {
                this.AddChild(new CVariation(Read36) { Attribute = new Variation("Read with surrogate in attr.val") });
            }
        }
    }
}
