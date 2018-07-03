// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class MiscFunctionalTests : TestModule
    {
        public partial class MiscTests : XLinqTestCase
        {
            public enum GetNameType
            {
                FromString,
                TwoParamGet,
                ExpandedName,
                XNamespacePlusOperator
            }

            //[TestCase(Name = "XName API - expanded name", Param = GetNameType.ExpandedName)]
            //[TestCase(Name = "XName API - expanded name (From string)", Param = GetNameType.FromString)]
            //[TestCase(Name = "XName API - two param Get", Param = GetNameType.TwoParamGet)]
            //[TestCase(Name = "XName API - XNamespace + string", Param = GetNameType.XNamespacePlusOperator)]
            public partial class XNameAPI : XLinqTestCase
            {
                //[Variation(Priority = 0, Desc = "XName.Get: No Namespace")]
                public void Variation1()
                {
                    XName name = GetName("nonamespace");
                    TestLog.Compare(name.LocalName.Equals("nonamespace"), "Invalid LocalName");
                    TestLog.Compare(name.Namespace, XNamespace.None, "Invalid Namespace");

                    foreach (GetNameType type in Enum.GetValues(typeof(GetNameType)))
                    {
                        TestLog.Compare(object.ReferenceEquals(name, GetName("nonamespace")), "reference equal for " + type);
                    }
                }

                //3 ways how to get the XName
                public XName GetName(string name, string ns, GetNameType type)
                {
                    switch (type)
                    {
                        case GetNameType.FromString:
                            return (ns == null || ns.Length == 0) ? name : "{" + ns + "}" + name;
                        case GetNameType.TwoParamGet:
                            return XName.Get(name, ns);
                        case GetNameType.ExpandedName:
                            return (ns == null || ns.Length == 0) ? XName.Get(name) : XName.Get("{" + ns + "}" + name);
                        case GetNameType.XNamespacePlusOperator:
                            return (ns == null || ns.Length == 0) ? XName.Get(name) : XNamespace.Get(ns) + name;
                        default:
                            TestLog.Compare(false, "Test failed: Invalid XName creation method specified");
                            break;
                    }
                    return null;
                }

                public XName GetName(string name, string ns)
                {
                    return GetName(name, ns, (GetNameType)Param);
                }

                public XName GetName(string name)
                {
                    return GetName(name, "");
                }

                //[Variation(Priority = 0, Desc = "XName.Get: Valid Namespace")]
                public void Variation2()
                {
                    XName name = GetName("name", "namespace");
                    TestLog.Compare(name.LocalName.Equals("name"), "Invalid LocalName");
                    TestLog.Compare(name.NamespaceName.Equals("namespace"), "Invalid Namespace Name");
                    TestLog.Compare(name.Namespace.NamespaceName.Equals("namespace"), "Invalid Namespace Name");

                    foreach (GetNameType type in Enum.GetValues(typeof(GetNameType)))
                    {
                        TestLog.Compare(object.ReferenceEquals(name, GetName("name", "namespace")), "reference equal for " + type);
                    }
                }

                //[Variation(Priority = 0, Desc = "XName.Get: Xmlns Namespace")]
                public void Variation3()
                {
                    XName name = GetName("name", "http://www.w3.org/2000/xmlns/");
                    TestLog.Compare(name.LocalName.Equals("name"), "Invalid LocalName");
                    TestLog.Compare(name.Namespace, XNamespace.Xmlns, "Invalid Namespace");
                    foreach (GetNameType type in Enum.GetValues(typeof(GetNameType)))
                    {
                        TestLog.Compare(object.ReferenceEquals(name, GetName("name", "http://www.w3.org/2000/xmlns/")), "reference equal for " + type);
                    }
                }

                //[Variation(Priority = 0, Desc = "XName.Get: Xml Namespace")]
                public void Variation4()
                {
                    XName name = GetName("name", "http://www.w3.org/XML/1998/namespace");
                    TestLog.Compare(name.LocalName.Equals("name"), "Invalid LocalName");
                    TestLog.Compare(name.Namespace, XNamespace.Xml, "Invalid Namespace");
                    foreach (GetNameType type in Enum.GetValues(typeof(GetNameType)))
                    {
                        TestLog.Compare(object.ReferenceEquals(name, GetName("name", "http://www.w3.org/XML/1998/namespace")), "reference equal for " + type);
                    }
                }

                //[Variation(Priority = 1, Desc = "XName.Get: Invalid name (empty string)")]
                public void Variation5()
                {
                    try
                    {
                        XName name = GetName(" ");
                        throw new TestFailedException("XName.Get(\" \") did not throw");
                    }
                    catch (XmlException) { }
                }

                //[Variation(Priority = 1, Desc = "XName.Get: Invalid name (null)")]
                public void Variation6()
                {
                    try
                    {
                        XName name = XName.Get(null);
                        throw new TestFailedException("XName.Get(null) did not throw");
                    }
                    catch (ArgumentException) { }
                }

                //[Variation(Priority = 0, Desc = "IEquatable: same names")]
                public void Variation12()
                {
                    XName nameOne = GetName("nameOne", "ns1");
                    XName nameTwo = GetName("nameOne", "ns1");
                    TestLog.Compare(nameOne == nameTwo, "nameOne and nameTwo are not the same objects");
                    TestLog.Compare(nameOne.Equals(nameTwo), "nameOne and nameTwo are not the same objects");
                    TestLog.Compare(((IEquatable<XName>)nameOne).Equals(nameTwo), "nameOne and nameTwo are not the same objects");
                }

                //[Variation(Priority = 0, Desc = "IEquatable: different names (NS)")]
                public void Variation13()
                {
                    XName nameOne = GetName("nameOne", "ns1");
                    XName nameTwo = GetName("nameOne", "ns2");
                    TestLog.Compare(nameOne != nameTwo, "nameOne and nameTwo are the same objects");
                    TestLog.Compare(!nameOne.Equals(nameTwo), "nameOne and nameTwo are the same objects");
                    TestLog.Compare(!((IEquatable<XName>)nameOne).Equals(nameTwo), "nameOne and nameTwo are not the same objects");
                }
            }
        }
    }
}
