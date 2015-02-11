// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class SDMSamplesTests : XLinqTestCase
        {
            public partial class SDM_Misc : XLinqTestCase
            {
                //[Variation(Desc = "NodeTypes")]
                public void NodeTypes()
                {
                    XDocument document = new XDocument();
                    XElement element = new XElement("x");
                    XText text = new XText("text-value");
                    XComment comment = new XComment("comment");
                    XProcessingInstruction processingInstruction = new XProcessingInstruction("target", "data");

                    Validate.IsEqual(document.NodeType, XmlNodeType.Document);
                    Validate.IsEqual(element.NodeType, XmlNodeType.Element);
                    Validate.IsEqual(text.NodeType, XmlNodeType.Text);
                    Validate.IsEqual(comment.NodeType, XmlNodeType.Comment);
                    Validate.IsEqual(processingInstruction.NodeType, XmlNodeType.ProcessingInstruction);
                }
            }
        }
    }
}