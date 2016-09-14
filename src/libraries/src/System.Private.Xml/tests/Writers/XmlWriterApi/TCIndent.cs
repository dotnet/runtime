// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    public partial class TCIndent : XmlFactoryWriterTestCaseBase
    {
        // Type is System.Xml.Tests.TCIndent
        // Test Case
        public override void AddChildren()
        {
            if (WriterType == WriterType.CustomWriter)
            {
                return;
            }
            // for function indent_1
            {
                this.AddChild(new CVariation(indent_1) { Attribute = new Variation("Simple test when false") { id = 1, Pri = 0 } });
            }


            // for function indent_2
            {
                this.AddChild(new CVariation(indent_2) { Attribute = new Variation("Simple test when true") { id = 2, Pri = 0 } });
            }


            // for function indent_3
            {
                this.AddChild(new CVariation(indent_3) { Attribute = new Variation("Indent = false, element content is empty") { id = 3, Pri = 0 } });
            }


            // for function indent_4
            {
                this.AddChild(new CVariation(indent_4) { Attribute = new Variation("Indent = true, element content is empty") { id = 4, Pri = 0 } });
            }


            // for function indent_5
            {
                this.AddChild(new CVariation(indent_5) { Attribute = new Variation("Indent = false, element content is empty, FullEndElement") { id = 5, Pri = 0 } });
            }


            // for function indent_6
            {
                this.AddChild(new CVariation(indent_6) { Attribute = new Variation("Indent = true, element content is empty, FullEndElement") { id = 6, Pri = 0 } });
            }


            // for function indent_7
            {
                this.AddChild(new CVariation(indent_7) { Attribute = new Variation("Indent = true, mixed content") { id = 7, Pri = 0 } });
            }


            // for function indent_8
            {
                this.AddChild(new CVariation(indent_8) { Attribute = new Variation("Indent = true, mixed content, FullEndElement") { id = 8, Pri = 0 } });
            }


            // for function indent_9
            {
                this.AddChild(new CVariation(indent_9) { Attribute = new Variation("Other types of non-text nodes") { id = 9, Priority = 0 } });
            }


            // for function indent_10
            {
                this.AddChild(new CVariation(indent_10) { Attribute = new Variation("Mixed content after child") { id = 10, Priority = 0 } });
            }


            // for function indent_11
            {
                this.AddChild(new CVariation(indent_11) { Attribute = new Variation("Mixed content - CData") { id = 11, Priority = 0 } });
            }


            // for function indent_12
            {
                this.AddChild(new CVariation(indent_12) { Attribute = new Variation("Mixed content - Whitespace") { id = 12, Priority = 0 } });
            }


            // for function indent_13
            {
                this.AddChild(new CVariation(indent_13) { Attribute = new Variation("Mixed content - Raw") { id = 13, Priority = 0 } });
            }


            // for function indent_14
            {
                this.AddChild(new CVariation(indent_14) { Attribute = new Variation("Mixed content - EntityRef") { id = 14, Priority = 0 } });
            }


            // for function indent_15
            {
                this.AddChild(new CVariation(indent_15) { Attribute = new Variation("Nested Elements - with EndDocument") { id = 15, Priority = 0 } });
            }


            // for function indent_16
            {
                this.AddChild(new CVariation(indent_16) { Attribute = new Variation("Nested Elements - with EndElement") { id = 16, Priority = 0 } });
            }


            // for function indent_17
            {
                this.AddChild(new CVariation(indent_17) { Attribute = new Variation("Nested Elements - with FullEndElement") { id = 17, Priority = 0 } });
            }


            // for function indent_18
            {
                this.AddChild(new CVariation(indent_18) { Attribute = new Variation("NewLines after root element") { id = 18, Priority = 0 } });
            }


            // for function indent_19
            {
                this.AddChild(new CVariation(indent_19) { Attribute = new Variation("Elements with attributes") { id = 19, Priority = 0 } });
            }


            // for function indent_20
            {
                this.AddChild(new CVariation(indent_20) { Attribute = new Variation("First PI with start document no xmldecl") { id = 20, Priority = 1 } });
            }


            // for function indent_21
            {
                this.AddChild(new CVariation(indent_21) { Attribute = new Variation("First comment with start document no xmldecl") { id = 21, Priority = 1 } });
            }


            // for function indent_22
            {
                this.AddChild(new CVariation(indent_22) { Attribute = new Variation("PI in mixed content - Auto") { Param = 0, id = 23, Priority = 1 } });
                this.AddChild(new CVariation(indent_22) { Attribute = new Variation("PI in mixed content - Document") { Param = 2, id = 22, Priority = 1 } });
            }


            // for function indent_24
            {
                this.AddChild(new CVariation(indent_24) { Attribute = new Variation("Comment in mixed content - Document") { Param = 2, id = 24, Priority = 1 } });
                this.AddChild(new CVariation(indent_24) { Attribute = new Variation("Comment in mixed content - Auto") { Param = 0, id = 25, Priority = 1 } });
            }


            // for function indent_26
            {
                this.AddChild(new CVariation(indent_26) { Attribute = new Variation("Mixed content after end element - Auto") { Param = 0, id = 27, Priority = 1 } });
                this.AddChild(new CVariation(indent_26) { Attribute = new Variation("Mixed content after end element - Document") { Param = 2, id = 26, Priority = 1 } });
            }


            // for function indent_28
            {
                this.AddChild(new CVariation(indent_28) { Attribute = new Variation("First element - no decl") { id = 28, Priority = 1 } });
            }


            // for function indent_29
            {
                this.AddChild(new CVariation(indent_29) { Attribute = new Variation("First element - with decl") { Param = true, id = 29, Priority = 1 } });
            }


            // for function indent_30
            {
                this.AddChild(new CVariation(indent_30) { Attribute = new Variation("Bad indentation of elements with mixed content data - Fragment") { Param = 1, id = 32, Priority = 1 } });
                this.AddChild(new CVariation(indent_30) { Attribute = new Variation("Bad indentation of elements with mixed content data - Document") { Param = 2, id = 30, Priority = 1 } });
                this.AddChild(new CVariation(indent_30) { Attribute = new Variation("Bad indentation of elements with mixed content data - Auto") { Param = 0, id = 31, Priority = 1 } });
            }


            // for function indent_33
            {
                this.AddChild(new CVariation(indent_33) { Attribute = new Variation("Indentation error - no new line after PI only if document contains no DocType node - Document") { Param = 2, id = 33, Priority = 1 } });
                this.AddChild(new CVariation(indent_33) { Attribute = new Variation("Indentation error - no new line after PI only if document contains no DocType node - Auto") { Param = 0, id = 34, Priority = 1 } });
            }


            // for function indent_36
            {
                this.AddChild(new CVariation(indent_36) { Attribute = new Variation("Indentation error - no new line after PI only if document contains DocType node - Document") { Param = 2, id = 36, Priority = 1 } });
                this.AddChild(new CVariation(indent_36) { Attribute = new Variation("Indentation error - no new line after PI only if document contains DocType node - Auto") { Param = 0, id = 37, Priority = 1 } });
            }


            // for function indent_39
            {
                this.AddChild(new CVariation(indent_39) { Attribute = new Variation("Auto") { Param = 0, id = 40, Priority = 1 } });
                this.AddChild(new CVariation(indent_39) { Attribute = new Variation("Fragment") { Param = 1, id = 41, Priority = 1 } });
                this.AddChild(new CVariation(indent_39) { Attribute = new Variation("Document") { Param = 2, id = 39, Priority = 1 } });
            }
        }
    }
}
