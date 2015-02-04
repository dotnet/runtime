// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XmlDiff;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class MiscTests : XLinqTestCase
        {
            public partial class XHashtableAPI : XLinqTestCase
            {
                //[Variation(Priority = 0, Desc = "None Namespace, Same Name: Single XNamespace and XName Objects")]
                public void NoneNamespaceSameNameElements()
                {
                    XElement a = new XElement(XNamespace.None + "Name"), b = new XElement(XNamespace.None + "Name");
                    TestLog.Compare(object.ReferenceEquals(XNamespace.None, b.Name.Namespace), "Not None namespace");
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(object.ReferenceEquals(a.Name, b.Name), "Different XName Objects");
                }

                //[Variation(Priority = 0, Desc = "Xml Namespace, Different Name: Single XNamespace, Multiple XName Objects")]
                public void XmlNamespaceDifferentNameElements()
                {
                    XElement a = new XElement(XNamespace.Xml + "NameOne"), b = new XElement(XNamespace.Xml + "NameTwo");
                    TestLog.Compare(object.ReferenceEquals(XNamespace.Xml, b.Name.Namespace), "Not Xml namespace");
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, b.Name), "Same XName Objects");
                }

                //[Variation(Priority = 0, Desc = "Xmlns Namespace, Same Name: Single XNamespace and XName Objects")]
                public void XmlnsNamespaceSameNameElements()
                {
                    XElement a = new XElement(XNamespace.Xmlns + "Name"), b = new XElement(XNamespace.Xmlns + "Name");
                    TestLog.Compare(object.ReferenceEquals(XNamespace.Xmlns, a.Name.Namespace), "Not Xmlns namespace");
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(object.ReferenceEquals(a.Name, b.Name), "Different XName Objects");
                }

                //[Variation(Priority = 0, Desc = "Default Namespace, Different Name: Single XNamespace, Multiple XName Objects")]
                public void DefaultNamespaceDifferentNameElements()
                {
                    XElement a = new XElement("NameOne"), b = new XElement("NameTwo");
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, b.Name), "Same XName Objects");
                }

                //[Variation(Priority = 1, Desc = "Same Namespace, Same Name: Single XName and XNamespace Objects")]
                public void SameNamespaceSameNameAttributes()
                {
                    XAttribute a = new XAttribute("{Namespace}Name", "a"), b = new XAttribute("{Namespace}Name", "b");
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(object.ReferenceEquals(a.Name, b.Name), "Different XName Objects");
                }

                //[Variation(Priority = 1, Desc = "Same Namespaces, Different Names: Single XNamespace, Multiple XName Objects")]
                public void SameNamespaceDifferentNamesElements()
                {
                    XElement a = new XElement("{NameSpace}NameOne"), b = new XElement("{NameSpace}NameTwo");
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, b.Name), "Same XName Object");
                }

                //[Variation(Priority = 1, Desc = "Different Namespaces, Same Name: Multiple XNamespace and XName Objects")]
                public void DifferentNamespacesSameNameAttributes()
                {
                    XAttribute a = new XAttribute("{NamespaceOne}Name", "a"), b = new XAttribute("{NamespaceTwo}Name", "b");
                    TestLog.Compare(!object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Same XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, b.Name), "Same XName Objects");
                }

                //[Variation(Priority = 1, Desc = "Different Namespaces and Names: Multiple XNamespaces and XName Objects")]
                public void DifferentNamespacesAndNamesElements()
                {
                    XElement a = new XElement("{NameSpaceOne}NameOne"), b = new XElement("{NameSpaceTwo}NameTwo");
                    TestLog.Compare(!object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Same XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, b.Name), "Same XName Object");
                }

                //[Variation(Priority = 1, Desc = "Same Document, Default Namespace, Same Name: Single XNamespace and XName Objects")]
                public void SameDocumentDefaultNamespaceSameNameElements()
                {
                    XDocument xDoc = new XDocument(new XElement("Name", new XElement("Name")));
                    TestLog.Compare(object.ReferenceEquals((xDoc.Nodes().First() as XElement).Name.Namespace,
                        (xDoc.Nodes().Last() as XElement).Name.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(object.ReferenceEquals((xDoc.Nodes().First() as XElement).Name,
                        (xDoc.Nodes().Last() as XElement).Name), "Different XName Objects");
                }

                //[Variation(Priority = 1, Desc = "Different Document, Same Namespace, Same Name: Single XNamespace and XName Objects")]
                public void DifferentDocumentSameNamespaceSameNameElements()
                {
                    XDocument xDoc1 = new XDocument(new XElement("{Namespace}Name"));
                    XDocument xDoc2 = new XDocument(new XElement("{Namespace}Name"));
                    TestLog.Compare(object.ReferenceEquals((xDoc1.FirstNode as XElement).Name.Namespace,
                        (xDoc2.FirstNode as XElement).Name.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(object.ReferenceEquals((xDoc1.FirstNode as XElement).Name,
                        (xDoc2.FirstNode as XElement).Name), "Different XName Objects");
                }

                //[Variation(Priority = 1, Desc = "Implicit Conversion, Same XName: Single XNamespace and XName Objects")]
                public void ImplicitSameName()
                {
                    XElement a = new XElement("Name");
                    XName xName = "Name";
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, xName.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(object.ReferenceEquals(a.Name, xName), "Different XName Object");
                }

                //[Variation(Priority = 1, Desc = "Implicit Conversion, Different XName: Single XNamespace and Multiple XName Objects")]
                public void ImplicitDifferentName()
                {
                    XElement a = new XElement("OneName");
                    XName xName = "OtherName";
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, xName.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, xName), "Same XName Object");
                }

                //[Variation(Priority = 1, Desc = "Explicit Conversion, Same XName: Single XNamespace and XName Objects")]
                public void ExplicitSameName()
                {
                    XElement a = new XElement((XName)"Name");
                    XName xName = (XName)"Name";
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, xName.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(object.ReferenceEquals(a.Name, xName), "Different XName Object");
                }

                //[Variation(Priority = 1, Desc = "Explicit Conversion, Different XName: Single XNamespace and Multiple XName Objects")]
                public void ExplicitDifferentName()
                {
                    XElement a = new XElement((XName)"OneName");
                    XName xName = (XName)"OtherName";
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, xName.Namespace), "Different XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, xName), "Same XName Object");
                }

                //[Variation(Priority = 1, Desc = "Explicit Conversion, Same Hashcode: Multiple XNamespace and XName Objects")]
                public void ExplicitSameHashcodeElements()
                {
                    XElement a = new XElement((XName)"{A2WVL}A2WVK");
                    XName xName = (XName)"{A2WVK}A2WVL";
                    TestLog.Compare(!object.ReferenceEquals(a.Name.Namespace, xName.Namespace), "Same XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, xName), "Same XName Object");
                }

                //[Variation(Priority = 0, Desc = "Different Names, Default Namespace, Same Hashcode: Single XNamespace and Multiple XName Objects")]
                public void DifferentNameSameHashcodeElements()
                {
                    XElement a = new XElement("A2WVL"), b = new XElement("A2WVK");
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Same XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, b.Name), "Same XName Object");
                }

                //[Variation(Priority = 0, Desc = "Different Names, None Namespace, Same Hashcode: Single XNamespace and Multiple XName Objects")]
                public void DifferentNameNoneNamespaceSameHashcodeElements()
                {
                    XElement a = new XElement(XNamespace.None + "A2WVL"), b = new XElement(XNamespace.None + "A2WVK");
                    TestLog.Compare(object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Same XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, b.Name), "Same XName Object");
                }

                //[Variation(Priority = 0, Desc = "Different Names, Different Namespace, Same Hashcode: Multiple XNamespace and XName Objects")]
                public void DifferentNamespaceAndNameSameHashcodeElements()
                {
                    XElement a = new XElement("{A2WVL}A2WVK"), b = new XElement("{A2WVK}A2WVL");
                    TestLog.Compare(!object.ReferenceEquals(a.Name.Namespace, b.Name.Namespace), "Same XNamespace Objects");
                    TestLog.Compare(!object.ReferenceEquals(a.Name, b.Name), "Same XName Object");
                }
            }
        }
    }
}
