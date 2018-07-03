// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using OLEDB.Test.ModuleCore;
using System.IO;
using XmlCoreTest.Common;

namespace System.Xml.Tests
{
    ////////////////////////////////////////////////////////////////
    // TestCase TCXML AttributeAccess
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCAttributeAccess : TCXMLReaderBaseGeneral
    {
        [Variation("Attribute Access test using ordinal (Ascending Order)", Pri = 0)]
        public int TestAttributeAccess1()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            string[] astr = new string[10];
            string n;
            string qname;

            DataReader.PositionOnElement("ACT0");
            CError.WriteLine("============With Namespace");

            int start = 1;
            int end = DataReader.AttributeCount;
            if (IsXsltReader())
            {
                // That's because after the Transform the order of the attribute is changed
                // <ACT0 xmlns:foo="http://www.foo.com" foo:Attr0="0" foo:Attr1="1111111101" foo:Attr2="222222202" foo:Attr3="333333303" foo:Attr4="444444404" />
                // becomes
                // <ACT0 foo:Attr0="0" foo:Attr1="1111111101" foo:Attr2="222222202" foo:Attr3="333333303" foo:Attr4="444444404" xmlns:foo="http://www.foo.com" />
                start = 0;
                end = DataReader.AttributeCount - 1;
            }
            for (int i = start; i < end; i++)
            {
                if (IsXsltReader() || IsXPathNavigatorReader())
                {
                    astr[i] = DataReader[i];
                    n = strAttr + i;
                }
                else
                {
                    astr[i - 1] = DataReader[i];
                    n = strAttr + (i - 1);
                }
                qname = "foo:" + n;
                CError.WriteLine(qname);
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");
                CError.WriteLine(i + " GetAttribute(i)=" + DataReader.GetAttribute(i) + " this[i]=" + DataReader[i] + " Value=" + DataReader.Value);

                CError.Compare(DataReader[n, strNamespace], DataReader.GetAttribute(n, strNamespace), "Compare this(name,strNamespace) with GetAttribute(name,strNamespace)");

                CError.Compare(DataReader[i], DataReader[n, strNamespace], "Compare this(i) with this(name,strNamespace)");
                CError.Compare(DataReader.MoveToAttribute(n, strNamespace), true, "MoveToAttribute(name,strNamespace)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(n, strNamespace), "Compare MoveToAttribute(name,strNamespace) with GetAttribute(name,strNamespace)");

                CError.WriteLine(i + " GetAttribute(n,strNamespace)=" + DataReader.GetAttribute(n, strNamespace) + " this[n,strNamespace]=" + DataReader[n, strNamespace] + " Value=" + DataReader.Value);

                CError.Compare(DataReader[i], DataReader[qname], "Compare this(i) with this(qname)");
                CError.Compare(DataReader.MoveToAttribute(qname), true, "MoveToAttribute(qname)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(qname), "Compare MoveToAttribute(qname) with GetAttribute(qname)");

                CError.WriteLine(i + " GetAttribute(qname)=" + DataReader.GetAttribute(qname) + " this[qname]=" + DataReader[qname] + " Value=" + DataReader.Value);
            }

            DataReader.PositionOnElement("ACT1");
            CError.WriteLine("============Without Namespace");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                CError.Compare(astr[i], DataReader.GetAttribute(i), "Compare value with GetAttribute");
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");
                CError.WriteLine(i + " GetAttribute(i)=" + DataReader.GetAttribute(i) + " this[i]=" + DataReader[i] + " Value=" + DataReader.Value);

                n = strAttr + i;
                CError.Compare(DataReader[n], DataReader.GetAttribute(n), "Compare this(name) with GetAttribute(name)");

                CError.Compare(DataReader[i], DataReader[n], "Compare this(i) with this(name)");
                CError.Compare(DataReader.MoveToAttribute(n), true, "MoveToAttribute(name)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(n), "Compare MoveToAttribute(name) with GetAttribute(name)");

                CError.WriteLine(i + " GetAttribute(n)=" + DataReader.GetAttribute(n) + " this[n]=" + DataReader[n] + " Value=" + DataReader.Value);
            }

            return TEST_PASS;
        }

        [Variation("Attribute Access test using ordinal (Descending Order)")]
        public int TestAttributeAccess2()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;
            ReloadSource();
            string[] astr = new string[10];
            string n;
            string qname;

            DataReader.PositionOnElement("ACT0");
            CError.WriteLine("============With Namespace");
            int start = 1;
            int end = DataReader.AttributeCount;
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                // That's because after the Transform the order of the attribute is changed
                // <ACT0 xmlns:foo="http://www.foo.com" foo:Attr0="0" foo:Attr1="1111111101" foo:Attr2="222222202" foo:Attr3="333333303" foo:Attr4="444444404" />
                // becomes
                // <ACT0 foo:Attr0="0" foo:Attr1="1111111101" foo:Attr2="222222202" foo:Attr3="333333303" foo:Attr4="444444404" xmlns:foo="http://www.foo.com" />
                start = 0;
                end = DataReader.AttributeCount - 1;
            }
            for (int i = end - 1; i >= start; i--)
            {
                if (IsXsltReader())
                {
                    astr[i] = DataReader[i];
                    n = strAttr + i;
                }
                else
                {
                    astr[i - 1] = DataReader[i];
                    n = strAttr + (i - 1);
                }
                qname = "foo:" + n;

                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");
                CError.WriteLine(i + " GetAttribute(i)=" + DataReader.GetAttribute(i) + " this[i]=" + DataReader[i] + " Value=" + DataReader.Value);

                CError.Compare(DataReader[n, strNamespace], DataReader.GetAttribute(n, strNamespace), "Compare this(name,strNamespace) with GetAttribute(name,strNamespace)");

                CError.Compare(DataReader[i], DataReader[n, strNamespace], "Compare this(i) with this(name,strNamespace)");
                CError.Compare(DataReader.MoveToAttribute(n, strNamespace), true, "MoveToAttribute(name,strNamespace)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(n, strNamespace), "Compare MoveToAttribute(name,strNamespace) with GetAttribute(name,strNamespace)");
                CError.WriteLine(i + " GetAttribute(n,strNamespace)=" + DataReader.GetAttribute(n, strNamespace) + " this[n,strNamespace]=" + DataReader[n, strNamespace] + " Value=" + DataReader.Value);

                CError.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Compare this(qname) with GetAttribute(qname)");

                CError.Compare(DataReader[i], DataReader[qname], "Compare this(i) with this(qname)");
                CError.Compare(DataReader.MoveToAttribute(qname), true, "MoveToAttribute(qname)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(qname), "Compare MoveToAttribute(qname) with GetAttribute(qname)");
                CError.WriteLine(i + " GetAttribute(qname)=" + DataReader.GetAttribute(qname) + " this[qname]=" + DataReader[qname] + " Value=" + DataReader.Value);
            }

            DataReader.PositionOnElement("ACT1");
            CError.WriteLine("============Without Namespace");
            for (int i = (DataReader.AttributeCount - 1); i > 0; i--)
            {
                n = strAttr + i;

                CError.WriteLine(i + " " + astr[i] + " GetAttribute(i)=" + DataReader.GetAttribute(i));
                CError.Compare(astr[i], DataReader.GetAttribute(i), "Compare value with GetAttribute");
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");
                CError.WriteLine(i + " GetAttribute(i)=" + DataReader.GetAttribute(i) + " this[i]=" + DataReader[i] + " Value=" + DataReader.Value);

                CError.Compare(DataReader[n], DataReader.GetAttribute(n), "Compare this(name) with GetAttribute(name)");

                CError.Compare(DataReader[i], DataReader[n], "Compare this(i) with this(name)");
                CError.Compare(DataReader.MoveToAttribute(n), true, "MoveToAttribute(name)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(n), "Compare MoveToAttribute(name) with GetAttribute(name)");

                CError.WriteLine(i + " GetAttribute(n)=" + DataReader.GetAttribute(n) + " this[n]=" + DataReader[n] + " Value=" + DataReader.Value);
            }

            return TEST_PASS;
        }

        [Variation("Attribute Access test using ordinal (Odd number)", Pri = 0)]
        public int TestAttributeAccess3()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            string[] astr = new string[10];
            string n;
            string qname;

            DataReader.PositionOnElement("ACT0");
            CError.WriteLine("============With Namespace");
            int start = 1;
            int end = DataReader.AttributeCount;
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                // That's because after the Transform the order of the attribute is changed
                // <ACT0 xmlns:foo="http://www.foo.com" foo:Attr0="0" foo:Attr1="1111111101" foo:Attr2="222222202" foo:Attr3="333333303" foo:Attr4="444444404" />
                // becomes
                // <ACT0 foo:Attr0="0" foo:Attr1="1111111101" foo:Attr2="222222202" foo:Attr3="333333303" foo:Attr4="444444404" xmlns:foo="http://www.foo.com" />
                start = 0;
                end = DataReader.AttributeCount - 1;
            }
            for (int i = start; i < end; i += 2)
            {
                if (IsXsltReader() || IsXPathNavigatorReader())
                {
                    astr[i] = DataReader[i];
                    n = strAttr + i;
                }
                else
                {
                    astr[i - 1] = DataReader[i];
                    n = strAttr + (i - 1);
                }
                qname = "foo:" + n;

                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");
                CError.WriteLine(i + " GetAttribute(i)=" + DataReader.GetAttribute(i) + " this[i]=" + DataReader[i] + " Value=" + DataReader.Value);

                CError.Compare(DataReader[n, strNamespace], DataReader.GetAttribute(n, strNamespace), "Compare this(name,strNamespace) with GetAttribute(name,strNamespace)");

                CError.Compare(DataReader[i], DataReader[n, strNamespace], "Compare this(i) with this(name,strNamespace)");
                CError.Compare(DataReader.MoveToAttribute(n, strNamespace), true, "MoveToAttribute(name,strNamespace)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(n, strNamespace), "Compare MoveToAttribute(name,strNamespace) with GetAttribute(name,strNamespace)");

                CError.WriteLine(i + " GetAttribute(n,strNamespace)=" + DataReader.GetAttribute(n, strNamespace) + " this[n,strNamespace]=" + DataReader[n, strNamespace] + " Value=" + DataReader.Value);
                CError.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Compare this(qname) with GetAttribute(qname)");

                CError.Compare(DataReader[i], DataReader[qname], "Compare this(i) with this(qname)");
                CError.Compare(DataReader.MoveToAttribute(qname), true, "MoveToAttribute(qname)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(qname), "Compare MoveToAttribute(qname) with GetAttribute(qname)");

                CError.WriteLine(i + " GetAttribute(qname)=" + DataReader.GetAttribute(qname) + " this[qname]=" + DataReader[qname] + " Value=" + DataReader.Value);
            }

            DataReader.PositionOnElement("ACT1");
            CError.WriteLine("============Without Namespace");
            for (int i = 0; i < DataReader.AttributeCount; i += 2)
            {
                CError.Compare(astr[i], DataReader.GetAttribute(i), "Compare value with GetAttribute");
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");
                CError.WriteLine(i + " GetAttribute(i)=" + DataReader.GetAttribute(i) + " this[i]=" + DataReader[i] + " Value=" + DataReader.Value);

                n = strAttr + i;
                CError.Compare(DataReader[n], DataReader.GetAttribute(n), "Compare this(name) with GetAttribute(name)");

                CError.Compare(DataReader[i], DataReader[n], "Compare this(i) with this(name)");
                CError.Compare(DataReader.MoveToAttribute(n), true, "MoveToAttribute(name)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(n), "Compare MoveToAttribute(name) with GetAttribute(name)");

                CError.WriteLine(i + " GetAttribute(n)=" + DataReader.GetAttribute(n) + " this[n]=" + DataReader[n] + " Value=" + DataReader.Value);
            }

            return TEST_PASS;
        }

        [Variation("Attribute Access test using ordinal (Even number)")]
        public int TestAttributeAccess4()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED; ReloadSource();
            string[] astr = new string[10];
            string n;
            string qname;

            DataReader.PositionOnElement("ACT0");
            CError.WriteLine("============With Namespace");
            int start = 1;
            int end = DataReader.AttributeCount;
            if (IsXsltReader())
            {
                // That's because after the Transform the order of the attribute is changed
                // <ACT0 xmlns:foo="http://www.foo.com" foo:Attr0="0" foo:Attr1="1111111101" foo:Attr2="222222202" foo:Attr3="333333303" foo:Attr4="444444404" />
                // becomes
                // <ACT0 foo:Attr0="0" foo:Attr1="1111111101" foo:Attr2="222222202" foo:Attr3="333333303" foo:Attr4="444444404" xmlns:foo="http://www.foo.com" />
                start = 0;
                end = DataReader.AttributeCount - 1;
            }
            for (int i = start; i < end; i += 3)
            {
                if (IsXsltReader() || IsXPathNavigatorReader())
                {
                    astr[i] = DataReader[i];
                    n = strAttr + i;
                }
                else
                {
                    astr[i - 1] = DataReader[i];
                    n = strAttr + (i - 1);
                }
                qname = "foo:" + n;

                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");
                CError.WriteLine(i + " GetAttribute(i)=" + DataReader.GetAttribute(i) + " this[i]=" + DataReader[i] + " Value=" + DataReader.Value);

                CError.Compare(DataReader[n, strNamespace], DataReader.GetAttribute(n, strNamespace), "Compare this(name,strNamespace) with GetAttribute(name,strNamespace)");

                CError.Compare(DataReader[i], DataReader[n, strNamespace], "Compare this(i) with this(name,strNamespace)");
                CError.Compare(DataReader.MoveToAttribute(n, strNamespace), true, "MoveToAttribute(name,strNamespace)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(n, strNamespace), "Compare MoveToAttribute(name,strNamespace) with GetAttribute(name,strNamespace)");

                CError.WriteLine(i + " GetAttribute(n,strNamespace)=" + DataReader.GetAttribute(n, strNamespace) + " this[n,strNamespace]=" + DataReader[n, strNamespace] + " Value=" + DataReader.Value);
                CError.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Compare this(qname) with GetAttribute(qname)");

                CError.Compare(DataReader[i], DataReader[qname], "Compare this(i) with this(qname)");
                CError.Compare(DataReader.MoveToAttribute(qname), true, "MoveToAttribute(qname)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(qname), "Compare MoveToAttribute(qname) with GetAttribute(qname)");

                CError.WriteLine(i + " GetAttribute(qname)=" + DataReader.GetAttribute(qname) + " this[qname]=" + DataReader[qname] + " Value=" + DataReader.Value);
            }

            DataReader.PositionOnElement("ACT1");
            CError.WriteLine("============Without Namespace");
            for (int i = 0; i < DataReader.AttributeCount; i += 3)
            {
                CError.Compare(astr[i], DataReader.GetAttribute(i), "Compare value with GetAttribute");
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Compare this with GetAttribute");
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Compare MoveToAttribute(i) with GetAttribute");
                CError.WriteLine(i + " GetAttribute(i)=" + DataReader.GetAttribute(i) + " this[i]=" + DataReader[i] + " Value=" + DataReader.Value);

                n = strAttr + i;
                CError.Compare(DataReader[n], DataReader.GetAttribute(n), "Compare this(name) with GetAttribute(name)");

                CError.Compare(DataReader[i], DataReader[n], "Compare this(i) with this(name)");
                CError.Compare(DataReader.MoveToAttribute(n), true, "MoveToAttribute(name)");
                CError.Compare(DataReader.Value, DataReader.GetAttribute(n), "Compare MoveToAttribute(name) with GetAttribute(name)");

                CError.WriteLine(i + " GetAttribute(n)=" + DataReader.GetAttribute(n) + " this[n]=" + DataReader[n] + " Value=" + DataReader.Value);
            }

            return TEST_PASS;
        }

        [Variation("Attribute Access with namespace=null")]
        public int TestAttributeAccess5()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");

            CError.Compare(DataReader[strAttr + 1, null], "1111111101", "Item");
            CError.Compare(DataReader.GetAttribute(strAttr + 1, null), "1111111101", "GA");
            CError.Compare(DataReader.MoveToAttribute(strAttr + 1, null), "MTA");


            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML This[Name] and This[Name, Namespace]
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCThisName : TCXMLReaderBaseGeneral
    {
        [Variation("This[Name] Verify with GetAttribute(Name)", Pri = 0)]
        public int ThisWithName1()
        {
            string strName;
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                CError.Compare(DataReader[strName], DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("This[Name, null] Verify with GetAttribute(Name)")]
        public int ThisWithName2()
        {
            string strName;
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                CError.Compare(DataReader[strName, null], DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("This[Name] Verify with GetAttribute(Name,null)")]
        public int ThisWithName3()
        {
            string strName;
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                CError.Compare(DataReader[strName], DataReader.GetAttribute(strName, null), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("This[Name, NamespaceURI] Verify with GetAttribute(Name, NamespaceURI)", Pri = 0)]
        public int ThisWithName4()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                CError.Compare(DataReader[strName, strNamespace], DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName,strNamespace) and this[strName,strNamespace]");
            }
            return TEST_PASS;
        }

        [Variation("This[Name, null] Verify not the same as GetAttribute(Name, NamespaceURI)")]
        public int ThisWithName5()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            DataReader.DumpOneNode();
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + (i - 1);
                if (DataReader.GetAttribute(strName, strNamespace) == DataReader[strName, null])
                {
                    throw new CTestException(CTestBase.TEST_FAIL, CurVariation.Desc);
                }
            }
            return TEST_PASS;
        }

        [Variation("This[Name, NamespaceURI] Verify not the same as GetAttribute(Name, null)")]
        public int ThisWithName6()
        {
            string strName;

            ReloadSource();
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + (i - 1);
                if (DataReader.GetAttribute(strName, null) == DataReader[strName, strNamespace])
                    throw new CTestException(CTestBase.TEST_FAIL, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("This[Name] Verify with MoveToAttribute(Name)", Pri = 0)]
        public int ThisWithName7()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                DataReader.MoveToAttribute(strName);
                CError.Compare(DataReader.Value, DataReader[strName], "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("This[Name, null] Verify with MoveToAttribute(Name)")]
        public int ThisWithName8()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                DataReader.MoveToAttribute(strName);
                CError.Compare(DataReader.Value, DataReader[strName, null], "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("This[Name] Verify with MoveToAttribute(Name,null)")]
        public int ThisWithName9()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                DataReader.MoveToAttribute(strName, null);
                CError.Compare(DataReader.Value, DataReader[strName], "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("This[Name, NamespaceURI] Verify not the same as MoveToAttribute(Name, null)", Pri = 0)]
        public int ThisWithName10()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + (i - 1);
                DataReader.MoveToAttribute(strName, null);
                if (DataReader[strName, strNamespace] == DataReader.Value)
                    throw new CTestException(CTestBase.TEST_FAIL, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("This[Name, null] Verify not the same as MoveToAttribute(Name, NamespaceURI)")]
        public int ThisWithName11()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                DataReader.MoveToAttribute(strName, strNamespace);
                if (DataReader[strName, null] == DataReader.Value)
                    throw new CTestException(CTestBase.TEST_FAIL, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("This[Name, namespace] Verify not the same as MoveToAttribute(Name, namespace)")]
        public int ThisWithName12()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + (i - 1);
                DataReader.MoveToAttribute(strName, strNamespace);
                CError.WriteLine(strName + " " + strNamespace + " DataReader.Value=" + DataReader.Value + " DataReader[strName, strNamespace]=" + DataReader[strName, strNamespace]);
                CError.Compare(DataReader.Value, DataReader[strName, strNamespace], "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("This(String.Empty)")]
        public int ThisWithName13()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY1");
            CError.Compare(DataReader[string.Empty], null, "Compare this[String.Empty] with null");
            return TEST_PASS;
        }

        [Variation("This[String.Empty,String.Empty]")]
        public int ThisWithName14()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY1");
            CError.Compare(DataReader[string.Empty, string.Empty], null, "Compare GetAttribute(strName) and this[strName]");
            return TEST_PASS;
        }

        [Variation("This[QName] Verify with GetAttribute(Name, NamespaceURI)", Pri = 0)]
        public int ThisWithName15()
        {
            string strName;
            string qname;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                qname = "foo:" + strName;
                CError.Compare(DataReader[qname], DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName,strNamespace) and this[qname]");
                CError.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Ordinal (" + i + "): Compare GetAttribute(qname) and this[qname]");
            }
            return TEST_PASS;
        }

        [Variation("This[QName] invalid Qname")]
        public int ThisWithName16()
        {
            string strName;
            string qname;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");

            int i = 1;
            strName = strAttr + i;
            qname = "foo1:" + strName;
            CError.Compare(DataReader.MoveToAttribute(qname), false, "MoveToAttribute(invalid qname)");
            CError.Compare(DataReader[qname], null, "Compare this[invalid qname] with null");
            CError.Compare(DataReader.GetAttribute(qname), null, "Compare GetAttribute(invalid qname) with null");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML MoveToAttribute(Name) and MoveToAttribute(Name, Namespace)
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCMoveToAttribute : TCXMLReaderBaseGeneral
    {
        [Variation("MoveToAttribute(String.Empty)")]
        public int MoveToAttributeWithName1()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY1");
            CError.Compare(DataReader.MoveToAttribute(string.Empty), false, "Compare the call to MoveToAttribute");
            CError.Compare(DataReader.Value, string.Empty, "Compare MoveToAttribute with String.Empty");
            return TEST_PASS;
        }

        [Variation("MoveToAttribute(String.Empty,String.Empty)")]
        public int MoveToAttributeWithName2()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY1");
            CError.Compare(DataReader.MoveToAttribute(string.Empty, string.Empty), false, "Compare the call to MoveToAttribute");
            CError.Compare(DataReader.Value, string.Empty, "Compare MoveToAttribute(strName)");
            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML GetAttribute(Ordinal)
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCGetAttributeOrdinal : TCXMLReaderBaseGeneral
    {
        [Variation("GetAttribute(i) Verify with This[i] - Double Quote", Pri = 0)]
        public int GetAttributeWithGetAttrDoubleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare GetAttribute(i) and this[i]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute[i] Verify with This[i] - Single Quote")]
        public int OrdinalWithGetAttrSingleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare GetAttribute(i) and this[i]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(i) Verify with MoveToAttribute[i] - Double Quote", Pri = 0)]
        public int GetAttributeWithMoveAttrDoubleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                string str = DataReader.GetAttribute(i);

                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                CError.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(i) Verify with MoveToAttribute[i] - Single Quote")]
        public int GetAttributeWithMoveAttrSingleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                string str = DataReader.GetAttribute(i);

                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader[i], "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                CError.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(i) NegativeOneOrdinal", Pri = 0)]
        public int NegativeOneOrdinal()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                string str = DataReader.GetAttribute(-1);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteLine(e + " : " + e.Message);
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
            CError.WriteLine("No Exception Thrown");
            return TEST_FAIL;
        }

        [Variation("GetAttribute(i) FieldCountOrdinal")]
        public int FieldCountOrdinal()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            try
            {
                string str = DataReader.GetAttribute(DataReader.AttributeCount);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("GetAttribute(i) OrdinalPlusOne", Pri = 0)]
        public int OrdinalPlusOne()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                string str = DataReader.GetAttribute(DataReader.AttributeCount + 1);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteLine(e + " : " + e.Message);
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
            CError.WriteLine("No Exception Thrown");
            return TEST_FAIL;
        }

        [Variation("GetAttribute(i) OrdinalMinusOne")]
        public int OrdinalMinusOne()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                string str = DataReader.GetAttribute(-2);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML GetAttribute(Name) and GetAttribute(Name, Namespace)
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCGetAttributeName : TCXMLReaderBaseGeneral
    {
        [Variation("GetAttribute(Name) Verify with This[Name]", Pri = 0)]
        public int GetAttributeWithName1()
        {
            string strName;
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                CError.Compare(DataReader[strName], DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name, null) Verify with This[Name]")]
        public int GetAttributeWithName2()
        {
            string strName;
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                CError.Compare(DataReader[strName], DataReader.GetAttribute(strName, null), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name) Verify with This[Name,null]")]
        public int GetAttributeWithName3()
        {
            string strName;
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                CError.Compare(DataReader[strName, null], DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name, NamespaceURI) Verify with This[Name, NamespaceURI]", Pri = 0)]
        public int GetAttributeWithName4()
        {
            string strName;
            string qname;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                qname = "foo:" + strName;
                CError.Compare(DataReader[strName, strNamespace], DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName,strNamespace) and this[strName,strNamespace]");
                CError.Compare(DataReader[qname], DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName,strNamespace) and this[strName,strNamespace]");
                CError.Compare(DataReader[qname], DataReader.GetAttribute(qname), "Ordinal (" + i + "): Compare GetAttribute(qname) and this[qname]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name, null) Verify not the same as This[Name, NamespaceURI]")]
        public int GetAttributeWithName5()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + (i - 1);
                if (DataReader.GetAttribute(strName, null) == DataReader[strName, strNamespace])
                {
                    if (DataReader[strName, strNamespace] == string.Empty)
                        throw new CTestException(CTestBase.TEST_FAIL, CurVariation.Desc);
                }
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name, NamespaceURI) Verify not the same as This[Name, null]")]
        public int GetAttributeWithName6()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            DataReader.DumpOneNode();
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + (i - 1);
                if (DataReader.GetAttribute(strName, strNamespace) == DataReader[strName, null])
                    throw new CTestException(CTestBase.TEST_FAIL, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name) Verify with MoveToAttribute(Name)")]
        public int GetAttributeWithName7()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                DataReader.MoveToAttribute(strName);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name,null) Verify with MoveToAttribute(Name)", Pri = 1)]
        public int GetAttributeWithName8()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                DataReader.MoveToAttribute(strName);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(strName, null), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name) Verify with MoveToAttribute(Name,null)", Pri = 1)]
        public int GetAttributeWithName9()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                DataReader.MoveToAttribute(strName, null);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(strName), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name, NamespaceURI) Verify not the same as MoveToAttribute(Name, null)")]
        public int GetAttributeWithName10()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + (i - 1);
                DataReader.MoveToAttribute(strName, null);
                if (DataReader.GetAttribute(strName, strNamespace) == DataReader.Value)
                    throw new CTestException(CTestBase.TEST_FAIL, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name, null) Verify not the same as MoveToAttribute(Name, NamespaceURI)")]
        public int GetAttributeWithName11()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + i;
                DataReader.MoveToAttribute(strName, strNamespace);
                if (DataReader.GetAttribute(strName, null) == DataReader.Value)
                    throw new CTestException(CTestBase.TEST_FAIL, CurVariation.Desc);
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(Name, namespace) Verify not the same as MoveToAttribute(Name, namespace)")]
        public int GetAttributeWithName12()
        {
            string strName;

            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 1; i < DataReader.AttributeCount; i++)
            {
                strName = strAttr + (i - 1);
                DataReader.MoveToAttribute(strName, strNamespace);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(strName, strNamespace), "Ordinal (" + i + "): Compare GetAttribute(strName) and this[strName]");
            }
            return TEST_PASS;
        }

        [Variation("GetAttribute(String.Empty)")]
        public int GetAttributeWithName13()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            CError.Compare(DataReader.GetAttribute(string.Empty), null, "Compare GetAttribute(strName) and this[strName]");
            return TEST_PASS;
        }

        [Variation("GetAttribute(String.Empty,String.Empty)")]
        public int GetAttributeWithName14()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            CError.Compare(DataReader.GetAttribute(string.Empty, string.Empty), null, "Compare GetAttribute(strName) and this[strName]");
            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML This[Ordinal]
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCThisOrdinal : TCXMLReaderBaseGeneral
    {
        [Variation("This[i] Verify with GetAttribute[i] - Double Quote", Pri = 0)]
        public int OrdinalWithGetAttrDoubleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare GetAttribute[i] and this[i]");
            }
            return TEST_PASS;
        }

        [Variation("This[i] Verify with GetAttribute[i] - Single Quote")]
        public int OrdinalWithGetAttrSingleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                CError.Compare(DataReader[i], DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare GetAttribute[i] and this[i]");
            }
            return TEST_PASS;
        }

        [Variation("This[i] Verify with MoveToAttribute[i] - Double Quote", Pri = 0)]
        public int OrdinalWithMoveAttrDoubleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                string str = DataReader[i];

                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader[i], "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                CError.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
            }
            return TEST_PASS;
        }

        [Variation("This[i] Verify with MoveToAttribute[i] - Single Quote")]
        public int OrdinalWithMoveAttrSingleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                string str = DataReader[i];

                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader[i], "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                CError.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
            }
            return TEST_PASS;
        }

        [Variation("ThisOrdinal NegativeOneOrdinal", Pri = 0)]
        public int NegativeOneOrdinal()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                string str = DataReader[-1];
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteLine(e + " : " + e.Message);
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
            CError.WriteLine("No Exception Thrown");
            return TEST_FAIL;
        }

        [Variation("ThisOrdinal FieldCountOrdinal")]
        public int FieldCountOrdinal()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            try
            {
                string str = DataReader[DataReader.AttributeCount];
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("ThisOrdinal OrdinalPlusOne", Pri = 0)]
        public int OrdinalPlusOne()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                string str = DataReader[DataReader.AttributeCount + 1];
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteLine(e + " : " + e.Message);
                throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
            }
            CError.WriteLine("No Exception Thrown");
            return TEST_FAIL;
        }

        [Variation("ThisOrdinal OrdinalMinusOne")]
        public int OrdinalMinusOne()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                string str = DataReader[-2];
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }
    }


    ////////////////////////////////////////////////////////////////
    // TestCase TCXML MoveToAttribute(Ordinal)
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCMoveToAttributeOrdinal : TCXMLReaderBaseGeneral
    {
        [Variation("MoveToAttribute(i) Verify with This[i] - Double Quote", Pri = 0)]
        public int MoveToAttributeWithGetAttrDoubleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader[i], DataReader.Value, "Ordinal (" + i + "): Compare GetAttribute(i) and this[i]");
            }
            return TEST_PASS;
        }

        [Variation("MoveToAttribute(i) Verify with This[i] - Single Quote")]
        public int MoveToAttributeWithGetAttrSingleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader[i], DataReader.Value, "Ordinal (" + i + "): Compare GetAttribute(i) and this[i]");
            }
            return TEST_PASS;
        }

        [Variation("MoveToAttribute(i) Verify with GetAttribute(i) - Double Quote", Pri = 0)]
        public int MoveToAttributeWithMoveAttrDoubleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                string str = DataReader.GetAttribute(i);

                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader.GetAttribute(i), "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                CError.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
            }
            return TEST_PASS;
        }

        [Variation("MoveToAttribute(i) Verify with GetAttribute[i] - Single Quote")]
        public int MoveToAttributeWithMoveAttrSingleQ()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            for (int i = 0; i < DataReader.AttributeCount; i++)
            {
                string str = DataReader.GetAttribute(i);

                DataReader.MoveToAttribute(i);
                CError.Compare(DataReader.Value, DataReader[i], "Ordinal (" + i + "): Compare MoveToAttribute[i] and this[i]");
                CError.Compare(str, DataReader.Value, "Ordinal (" + i + "): Compare MoveToAttribute[i] and string");
            }
            return TEST_PASS;
        }

        [Variation("MoveToAttribute(i) NegativeOneOrdinal", Pri = 0)]
        public int NegativeOneOrdinal()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                DataReader.MoveToAttribute(-1);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("MoveToAttribute(i) FieldCountOrdinal")]
        public int FieldCountOrdinal()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");
            try
            {
                DataReader.MoveToAttribute(DataReader.AttributeCount);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("MoveToAttribute(i) OrdinalPlusOne", Pri = 0)]
        public int OrdinalPlusOne()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                DataReader.MoveToAttribute(DataReader.AttributeCount + 1);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }

        [Variation("MoveToAttribute(i) OrdinalMinusOne")]
        public int OrdinalMinusOne()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            try
            {
                DataReader.MoveToAttribute(-2);
            }
            catch (ArgumentOutOfRangeException exc)
            {
                CError.WriteLine(exc + " : " + exc.Message);
                return TEST_PASS;
            }
            throw new CTestException(CTestBase.TEST_FAIL, WRONG_EXCEPTION);
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML MoveToFirstAttribute()
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCMoveToFirstAttribute : TCXMLReaderBaseGeneral
    {
        [Variation("MoveToFirstAttribute() When AttributeCount=0, <EMPTY1/> ", Pri = 0)]
        public int MoveToFirstAttribute1()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY1");
            CError.Compare(DataReader.MoveToFirstAttribute(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When AttributeCount=0, <NONEMPTY1>ABCDE</NONEMPTY1> ")]
        public int MoveToFirstAttribute2()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONEMPTY1");
            CError.Compare(DataReader.MoveToFirstAttribute(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=0, with namespace")]
        public int MoveToFirstAttribute3()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");

            string strFirst;

            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            strFirst = DataReader.Value;

            CError.Compare(strFirst, DataReader.GetAttribute(0), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=0, without namespace")]
        public int MoveToFirstAttribute4()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");

            string strFirst;

            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            strFirst = DataReader.Value;

            CError.Compare(strFirst, DataReader.GetAttribute(0), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=middle, with namespace")]
        public int MoveToFirstAttribute5()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");

            string strFirst;

            DataReader.MoveToAttribute((int)((DataReader.AttributeCount) / 2));
            CError.WriteLine("Middle value=" + DataReader.Value);
            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            strFirst = DataReader.Value;

            CError.Compare(strFirst, DataReader.GetAttribute(0), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=middle, without namespace")]
        public int MoveToFirstAttribute6()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");

            string strFirst;

            DataReader.MoveToAttribute((int)((DataReader.AttributeCount) / 2));
            CError.WriteLine("Middle value=" + DataReader.Value);
            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            strFirst = DataReader.Value;

            CError.Compare(strFirst, DataReader.GetAttribute(0), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=end, with namespace")]
        public int MoveToFirstAttribute7()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");

            string strFirst;

            DataReader.MoveToAttribute((DataReader.AttributeCount) - 1);
            CError.WriteLine("End value=" + DataReader.Value);
            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            strFirst = DataReader.Value;

            CError.Compare(strFirst, DataReader.GetAttribute(0), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=end, without namespace")]
        public int MoveToFirstAttribute8()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");

            string strFirst;

            DataReader.MoveToAttribute((DataReader.AttributeCount) - 1);
            CError.WriteLine("End value=" + DataReader.Value);
            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            strFirst = DataReader.Value;

            CError.Compare(strFirst, DataReader.GetAttribute(0), CurVariation.Desc);
            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML MoveToNextAttribute()
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCMoveToNextAttribute : TCXMLReaderBaseGeneral
    {
        [Variation("MoveToNextAttribute() When AttributeCount=0, <EMPTY1/> ", Pri = 0)]
        public int MoveToNextAttribute1()
        {
            ReloadSource();
            DataReader.PositionOnElement("EMPTY1");
            CError.Compare(DataReader.MoveToNextAttribute(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToNextAttribute() When AttributeCount=0, <NONEMPTY1>ABCDE</NONEMPTY1> ")]
        public int MoveToNextAttribute2()
        {
            ReloadSource();
            DataReader.PositionOnElement("NONEMPTY1");
            CError.Compare(DataReader.MoveToNextAttribute(), false, CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToNextAttribute() When iOrdinal=0, with namespace")]
        public int MoveToNextAttribute3()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");

            string strValue;

            CError.WriteLine("Move to first Attribute===========");
            CError.Compare(DataReader.MoveToNextAttribute(), true, CurVariation.Desc);
            strValue = DataReader.Value;

            CError.Compare(strValue, DataReader.GetAttribute(0), CurVariation.Desc);

            CError.WriteLine("Move to second Attribute===========");
            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            CError.Compare(DataReader.MoveToNextAttribute(), true, CurVariation.Desc);
            strValue = DataReader.Value;

            CError.Compare(strValue, DataReader.GetAttribute(1), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToNextAttribute() When iOrdinal=0, without namespace")]
        public int MoveToNextAttribute4()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");
            string strValue;

            CError.WriteLine("Move to first Attribute===========");
            CError.Compare(DataReader.MoveToNextAttribute(), true, CurVariation.Desc);
            strValue = DataReader.Value;

            CError.Compare(strValue, DataReader.GetAttribute(0), CurVariation.Desc);

            CError.WriteLine("Move to second Attribute===========");
            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            CError.Compare(DataReader.MoveToNextAttribute(), true, CurVariation.Desc);
            strValue = DataReader.Value;

            CError.Compare(strValue, DataReader.GetAttribute(1), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=middle, with namespace")]
        public int MoveToFirstAttribute5()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");

            string strValue0;
            string strValue;
            int iMid = (DataReader.AttributeCount) / 2;

            DataReader.MoveToAttribute(iMid + 1);
            CError.WriteLine("NextFromMiddle value=" + DataReader.Value);
            strValue0 = DataReader.Value;

            DataReader.MoveToAttribute(iMid);
            CError.Compare(DataReader.MoveToNextAttribute(), true, CurVariation.Desc);
            strValue = DataReader.Value;

            CError.Compare(strValue0, strValue, CurVariation.Desc);
            CError.Compare(strValue, DataReader.GetAttribute(iMid + 1), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=middle, without namespace")]
        public int MoveToFirstAttribute6()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");

            string strValue0;
            string strValue;
            int iMid = (DataReader.AttributeCount) / 2;

            DataReader.MoveToAttribute(iMid + 1);
            CError.WriteLine("Middle value=" + DataReader.Value);
            strValue0 = DataReader.Value;

            DataReader.MoveToAttribute(iMid);
            CError.Compare(DataReader.MoveToNextAttribute(), true, CurVariation.Desc);
            strValue = DataReader.Value;

            CError.Compare(strValue0, strValue, CurVariation.Desc);
            CError.Compare(strValue, DataReader.GetAttribute(iMid + 1), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=end, with namespace")]
        public int MoveToFirstAttribute7()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT0");

            string strFirst;

            DataReader.MoveToAttribute((DataReader.AttributeCount) - 1);
            CError.WriteLine("End value=" + DataReader.Value);
            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            strFirst = DataReader.Value;

            CError.Compare(strFirst, DataReader.GetAttribute(0), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute() When iOrdinal=end, without namespace")]
        public int MoveToFirstAttribute8()
        {
            ReloadSource();
            DataReader.PositionOnElement("ACT1");

            string strFirst;

            DataReader.MoveToAttribute((DataReader.AttributeCount) - 1);
            CError.WriteLine("End value=" + DataReader.Value);
            CError.Compare(DataReader.MoveToFirstAttribute(), true, CurVariation.Desc);
            strFirst = DataReader.Value;

            CError.Compare(strFirst, DataReader.GetAttribute(0), CurVariation.Desc);
            return TEST_PASS;
        }

        [Variation("XmlReader: Does not count depth for attributes of xml decl. and Doctype")]
        public int MoveToNextAttribute9()
        {
            ReloadSource(Path.Combine(TestData, "Common", "Bug424573.xml"));
            DataReader.Read();
            if (DataReader.HasAttributes)
            {
                for (int i = 0; i < DataReader.AttributeCount; i++)
                {
                    DataReader.MoveToNextAttribute();
                    if (DataReader.NodeType == XmlNodeType.Attribute && DataReader.Depth != 1)
                    {
                        CError.WriteLine("Unexpected attribute depth: {0}\n", DataReader.Depth);
                        return TEST_FAIL;
                    }
                }
            }
            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML Attribute Test when NodeType != Attributes
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCAttributeTest : TCXMLReaderBaseGeneral
    {
        [Variation("Attribute Test On None")]
        public int TestAttributeTestNodeType_None()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.None) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCount");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("Attribute Test  On Element", Pri = 0)]
        public int TestAttributeTestNodeType_Element()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Element) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("Attribute Test On Text", Pri = 0)]
        public int TestAttributeTestNodeType_Text()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Text) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("Attribute Test On CDATA")]
        public int TestAttributeTestNodeType_CDATA()
        {
            ReloadSource();

            // No CDATA for Xslt
            if (IsXsltReader() || IsXPathNavigatorReader())
            {
                while (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
                    return TEST_FAIL;
                return TEST_PASS;
            }

            if (FindNodeType(XmlNodeType.CDATA) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("Attribute Test On ProcessingInstruction")]
        public int TestAttributeTestNodeType_ProcessingInstruction()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.ProcessingInstruction) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("AttributeTest On Comment")]
        public int TestAttributeTestNodeType_Comment()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Comment) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("AttributeTest On DocumentType", Pri = 0)]
        public int TestAttributeTestNodeType_DocumentType()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.DocumentType) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 1, "Checking AttributeCoung");
                CError.Compare(DataReader.HasAttributes, true, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), true, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("AttributeTest On Whitespace")]
        public int TestAttributeTestNodeType_Whitespace()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.Whitespace) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("AttributeTest On EndElement")]
        public int TestAttributeTestNodeType_EndElement()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.EndElement) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCount");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("AttributeTest On XmlDeclaration", Pri = 0)]
        public int TestAttributeTestNodeType_XmlDeclaration()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) == TEST_PASS)
            {
                int nCount = (IsXsltReader() || IsXPathNavigatorReader()) ? 1 : 3;
                CError.Compare(DataReader.AttributeCount, nCount, "Checking AttributeCount");
                CError.Compare(DataReader.HasAttributes, true, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), true, "Checking MoveToFirstAttribute");

                bool bNext = !(IsXsltReader() || IsXPathNavigatorReader()); // XsltReader has only one attribute for XmlDeclaration
                CError.Compare(DataReader.MoveToNextAttribute(), bNext, "Checking MoveToNextAttribute");
            }
            return TEST_PASS;
        }

        [Variation("Attribute Test On EntityReference")]
        public int TestAttributeTestNodeType_EntityReference()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.EntityReference) == TEST_PASS)
            {
                CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCoung");
                CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
                CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("AttributeTest On EndEntity")]
        public int TestAttributeTestNodeType_EndEntity()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            ReloadSource();
            PositionOnNodeType(XmlNodeType.EndEntity);

            CError.Compare(DataReader.AttributeCount, 0, "Checking AttributeCount");
            CError.Compare(DataReader.HasAttributes, false, "Checking HasAttributes");
            CError.Compare(DataReader.MoveToFirstAttribute(), false, "Checking MoveToFirstAttribute");
            CError.Compare(DataReader.MoveToNextAttribute(), false, "Checking MoveToNextAttribute");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML AttributeTest on XmlDeclaration DCR52258
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCAttributeXmlDeclaration : TCXMLReaderBaseGeneral
    {
        private static string[] s_attrNames = { "version", "encoding", "standalone" };
        private static string[] s_attrValues = { "1.0", "UTF-8", "no" };
        private static int s_attrCount = 3;

        public override int Init(object objParam)
        {
            int ret = base.Init(objParam);

            // xslt doesn't have encoding and standalone
            s_attrCount = (IsXsltReader() || IsXPathNavigatorReader()) ? 1 : 3;
            if (IsBinaryReader())
            {
                s_attrValues = new string[] { "1.0", "utf-8", "no" };
            }

            return ret;
        }

        private void CheckAttribute(int nPos)
        {
            CheckAttribute(nPos, XmlNodeType.Attribute);
        }

        private void CheckAttribute(int nPos, XmlNodeType nt)
        {
            CError.Compare(DataReader.VerifyNode(nt, s_attrNames[nPos], s_attrValues[nPos]), true, "CheckAttribute");
        }

        [Variation("AttributeCount and HasAttributes", Pri = 0)]
        public int TAXmlDecl_1()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            CError.Compare(DataReader.AttributeCount, s_attrCount, "Checking AttributeCount");
            CError.Compare(DataReader.HasAttributes, (s_attrCount > 0), "Checking HasAttributes");
            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute/MoveToNextAttribute navigation", Pri = 0)]
        public int TAXmlDecl_2()
        {
            if (IsBinaryReader())
                return TEST_SKIPPED;

            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            // Check version attribute
            CError.Compare(DataReader.MoveToFirstAttribute(), true, "MTFA");
            CheckAttribute(0);

            if (!(IsXsltReader() || IsXPathNavigatorReader())) // Xslt has only one XmlDeclaration attribute
            {
                // Check encoding attribute
                CError.Compare(DataReader.MoveToNextAttribute(), true, "MTNA");
                CheckAttribute(1);

                // Check standalone attribute
                CError.Compare(DataReader.MoveToNextAttribute(), true, "MTNA");
                CheckAttribute(2);
            }

            // Check if no more attribute
            CError.Compare(DataReader.MoveToNextAttribute(), false, "MTNA");

            return TEST_PASS;
        }

        [Variation("MoveToFirstAttribute/MoveToNextAttribute succesive calls")]
        public int TAXmlDecl_3()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            // Call MoveToNextAttribute first
            CError.Compare(DataReader.MoveToNextAttribute(), true, "MTNA");
            CheckAttribute(0);

            if (!(IsXsltReader() || IsXPathNavigatorReader())) // Xslt has only one XmlDeclaration attribute
            {
                // Go to 2nd attribute
                CError.Compare(DataReader.MoveToNextAttribute(), true, "MTNA");
                CheckAttribute(1);

                // Rewind
                CError.Compare(DataReader.MoveToFirstAttribute(), true, "MTNA");
                CheckAttribute(0);

                // Go to last attribute
                CError.Compare(DataReader.MoveToNextAttribute(), true, "MTNA");
                CError.Compare(DataReader.MoveToNextAttribute(), true, "MTNA");
                CheckAttribute(2);

                // Rewind
                CError.Compare(DataReader.MoveToFirstAttribute(), true, "MTNA");
                CheckAttribute(0);

                // Go to after the last attribute
                CError.Compare(DataReader.MoveToNextAttribute(), true, "MTNA");
                CError.Compare(DataReader.MoveToNextAttribute(), true, "MTNA");
                CError.Compare(DataReader.MoveToNextAttribute(), false, "MTNA");
                CheckAttribute(2);
            }

            // Rewind
            CError.Compare(DataReader.MoveToFirstAttribute(), true, "MTNA");
            CheckAttribute(0);

            return TEST_PASS;
        }

        [Variation("MoveToAttribute attribute access")]
        public int TAXmlDecl_4()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            CError.Compare(DataReader.AttributeCount, s_attrCount, "AttributeCount");

            // Ordinal access
            for (int i = 0; i < s_attrCount; i++)
            {
                DataReader.MoveToAttribute(i);
                CheckAttribute(i);
            }

            // Name access
            for (int i = s_attrCount - 1; i >= 0; i--)
            {
                CError.Compare(DataReader.MoveToAttribute(s_attrNames[i]), true, "MTA");
                CheckAttribute(i);
            }

            // Name & Namespace access
            for (int i = s_attrCount - 1; i >= 0; i--)
            {
                CError.Compare(DataReader.MoveToAttribute(s_attrNames[i], null), true, "MTA");
                CheckAttribute(i);
            }

            // Name & Namespace access
            for (int i = s_attrCount - 1; i >= 0; i--)
            {
                CError.Compare(DataReader.MoveToAttribute(s_attrNames[i], string.Empty), true, "MTA");
                CheckAttribute(i);
            }

            return TEST_PASS;
        }

        [Variation("MoveToAttribute attribute access with invalid index")]
        public int TAXmlDecl_5()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            DataReader.MoveToAttribute(s_attrCount / 2);
            CheckAttribute(s_attrCount / 2);

            // Invalid index
            try
            {
                DataReader.MoveToAttribute(-1);
                return TEST_FAIL;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            return TEST_PASS;
        }

        [Variation("GetAttribute attribute access")]
        public int TAXmlDecl_6()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            CError.Compare(DataReader.AttributeCount, s_attrCount, "AttributeCount");

            // reader on element
            for (int i = 0; i < s_attrCount; i++)
            {
                // Ordinal access
                CError.Compare(DataReader.GetAttribute(i), s_attrValues[i], "GA ordinal");

                // Name access
                CError.Compare(DataReader.GetAttribute(s_attrNames[i]), s_attrValues[i], "GA name");

                // Name & Namespace access
                CError.Compare(DataReader.GetAttribute(s_attrNames[i], null), s_attrValues[i], "GA name & namespace");

                // Name & Namespace access
                CError.Compare(DataReader.GetAttribute(s_attrNames[i], string.Empty), s_attrValues[i], "GA name & namespace");
            }

            // reader on middle attribute
            DataReader.MoveToAttribute(s_attrCount / 2);

            for (int i = s_attrCount - 1; i >= 0; i--)
            {
                // Ordinal access
                CError.Compare(DataReader.GetAttribute(i), s_attrValues[i], "GA ordinal");

                // Name access
                CError.Compare(DataReader.GetAttribute(s_attrNames[i]), s_attrValues[i], "GA name");

                // Name & Namespace access
                CError.Compare(DataReader.GetAttribute(s_attrNames[i], null), s_attrValues[i], "GA name & namespace");

                // Name & Namespace access
                CError.Compare(DataReader.GetAttribute(s_attrNames[i], string.Empty), s_attrValues[i], "GA name & namespace");
            }

            return TEST_PASS;
        }

        [Variation("GetAttribute attribute access with invalid index")]
        public int TAXmlDecl_7()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            CError.Compare(DataReader.AttributeCount, s_attrCount, "AttributeCount");

            // Invalid index
            try
            {
                DataReader.GetAttribute(-1);
                return TEST_FAIL;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                DataReader.GetAttribute(s_attrCount);
                return TEST_FAIL;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            // Invalid name
            CError.Compare(DataReader.GetAttribute("s"), null, "GA");

            // Invalid namespace
            CError.Compare(DataReader.GetAttribute(s_attrNames[0], "zzz"), null, "GA");

            return TEST_PASS;
        }

        [Variation("this[] attribute access")]
        public int TAXmlDecl_8()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            CError.Compare(DataReader.AttributeCount, s_attrCount, "AttributeCount");

            // reader on element
            for (int i = 0; i < s_attrCount; i++)
            {
                // Ordinal access
                CError.Compare(DataReader[i], s_attrValues[i], "[] ordinal");

                // Name access
                CError.Compare(DataReader[s_attrNames[i]], s_attrValues[i], "[] name");

                // Name & Namespace access
                CError.Compare(DataReader[s_attrNames[i], null], s_attrValues[i], "[] name & namespace");

                // Name & Namespace access
                CError.Compare(DataReader[s_attrNames[i], string.Empty], s_attrValues[i], "[] name & namespace");
            }

            // reader on middle attribute
            DataReader.MoveToAttribute(s_attrCount / 2);

            for (int i = s_attrCount - 1; i >= 0; i--)
            {
                // Ordinal access
                CError.Compare(DataReader[i], s_attrValues[i], "[] ordinal");

                // Name access
                CError.Compare(DataReader[s_attrNames[i]], s_attrValues[i], "[] name");

                // Name & Namespace access
                CError.Compare(DataReader[s_attrNames[i], null], s_attrValues[i], "[] name & namespace");

                // Name & Namespace access
                CError.Compare(DataReader[s_attrNames[i], string.Empty], s_attrValues[i], "[] name & namespace");
            }

            return TEST_PASS;
        }

        [Variation("this[] attribute access with invalid index")]
        public int TAXmlDecl_9()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            // Invalid index
            try
            {
                string s = DataReader[-1];
                return TEST_FAIL;
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            try
            {
                string s = DataReader[s_attrCount];
                return TEST_FAIL;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            // Invalid name
            CError.Compare(DataReader["s"], null, "[]");

            // Invalid namespace
            CError.Compare(DataReader[s_attrNames[0], "zzz"], null, "[]");

            return TEST_PASS;
        }

        [Variation("ReadAttributeValue on XmlDecl attributes")]
        public int TAXmlDecl_10()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            CError.Compare(DataReader.AttributeCount, s_attrCount, "AttributeCount");

            for (int i = 0; i < s_attrCount; i++)
            {
                DataReader.MoveToAttribute(i);

                // Ordinal access
                CError.Compare(DataReader.ReadAttributeValue(), true, "RAV");
                CError.Compare(DataReader.VerifyNode(XmlNodeType.Text, string.Empty, s_attrValues[i]), true, "Attribute");
                CError.Compare(DataReader.ReadAttributeValue(), false, "RAV");
            }

            return TEST_PASS;
        }

        [Variation("LocalName, NamespaceURI and Prefix on XmlDecl attributes")]
        public int TAXmlDecl_11()
        {
            ReloadSource();
            if (FindNodeType(XmlNodeType.XmlDeclaration) != TEST_PASS)
                return TEST_FAIL;

            CError.Compare(DataReader.AttributeCount, s_attrCount, "AttributeCount");

            for (int i = 0; i < s_attrCount; i++)
            {
                DataReader.MoveToAttribute(i);

                // Ordinal access
                CError.Compare(DataReader.LocalName, s_attrNames[i], "LN");
                CError.Compare(DataReader.Prefix, string.Empty, "P");
                CError.Compare(DataReader.NamespaceURI, string.Empty, "NU");
            }

            return TEST_PASS;
        }

        [Variation("Whitespace between XmlDecl attributes")]
        public int TAXmlDecl_12()
        {
            if (IsXmlNodeReader() || IsXmlNodeReaderDataDoc() || IsXPathNavigatorReader() || IsBinaryReader())
                return TEST_SKIPPED;

            string strxml = "<?xml version='1.0'          standalone='yes'?><ROOT/>";
            ReloadSourceStr(strxml);
            DataReader.Read();
            CError.Compare(DataReader.Value, "version='1.0'          standalone='yes'", "value");

            return TEST_PASS;
        }

        [Variation("MoveToElement on XmlDeclaration attributes")]
        public int TAXmlDecl_13()
        {
            ReloadSource();
            PositionOnNodeType(XmlNodeType.XmlDeclaration);

            DataReader.MoveToFirstAttribute();
            CError.Compare(DataReader.VerifyNode(XmlNodeType.Attribute, s_attrNames[0], s_attrValues[0]), "Attribute");

            DataReader.MoveToElement();
            CError.Compare(DataReader.NodeType, XmlNodeType.XmlDeclaration, "nt");
            CError.Compare(DataReader.Name, "xml", "name");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase TCXML xmlns as local name DCR50345
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCXmlns : TCXMLReaderBaseGeneral
    {
        private string _ST_ENS1 = "EMPTY_NAMESPACE1"; //<EMPTY_NAMESPACE1 Attr0="0" xmlns="14"/>
        private string _ST_NS2 = "NAMESPACE2"; //<NAMESPACE2 xmlns:bar="1">

        [Variation("Name, LocalName, Prefix and Value with xmlns=ns attribute", Pri = 0)]
        public int TXmlns1()
        {
            ReloadSource();

            DataReader.PositionOnElement(_ST_ENS1);
            DataReader.MoveToAttribute("xmlns");

            CError.Compare(DataReader.LocalName, "xmlns", "ln");
            CError.Compare(DataReader.Name, "xmlns", "n");
            CError.Compare(DataReader.Prefix, string.Empty, "p");
            CError.Compare(DataReader.Value, "14", "v");

            return TEST_PASS;
        }

        [Variation("Name, LocalName, Prefix and Value with xmlns:p=ns attribute")]
        public int TXmlns2()
        {
            ReloadSource();

            DataReader.PositionOnElement(_ST_NS2);
            DataReader.MoveToAttribute(0);

            CError.Compare(DataReader.LocalName, "bar", "ln");
            CError.Compare(DataReader.Name, "xmlns:bar", "n");
            CError.Compare(DataReader.Prefix, "xmlns", "p");
            CError.Compare(DataReader.Value, "1", "v");

            return TEST_PASS;
        }

        [Variation("LookupNamespace with xmlns=ns attribute")]
        public int TXmlns3()
        {
            ReloadSource();

            DataReader.PositionOnElement(_ST_ENS1);
            DataReader.MoveToAttribute(1);

            CError.Compare(DataReader.LookupNamespace("xmlns"), "http://www.w3.org/2000/xmlns/", "ln");

            return TEST_PASS;
        }

        [Variation("MoveToAttribute access on xmlns attribute")]
        public int TXmlns4()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            DataReader.PositionOnElement(_ST_ENS1);
            DataReader.MoveToAttribute(1);

            CError.Compare(DataReader.LocalName, "xmlns", "ln");
            CError.Compare(DataReader.Name, "xmlns", "n");
            CError.Compare(DataReader.Prefix, string.Empty, "p");
            CError.Compare(DataReader.Value, "14", "v");

            DataReader.MoveToElement();
            CError.Compare(DataReader.MoveToAttribute("xmlns"), true, "mta(str)");

            CError.Compare(DataReader.LocalName, "xmlns", "ln");
            CError.Compare(DataReader.Name, "xmlns", "n");
            CError.Compare(DataReader.Prefix, string.Empty, "p");
            CError.Compare(DataReader.Value, "14", "v");

            DataReader.MoveToElement();
            CError.Compare(DataReader.MoveToAttribute("xmlns", "http://www.w3.org/2000/xmlns/"), true, "mta(str, str)");

            CError.Compare(DataReader.LocalName, "xmlns", "ln");
            CError.Compare(DataReader.Name, "xmlns", "n");
            CError.Compare(DataReader.Prefix, string.Empty, "p");
            CError.Compare(DataReader.Value, "14", "v");

            DataReader.MoveToElement();
            CError.Compare(DataReader.MoveToAttribute("xmlns", "14"), false, "mta inv");

            return TEST_PASS;
        }

        [Variation("GetAttribute access on xmlns attribute")]
        public int TXmlns5()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            DataReader.PositionOnElement(_ST_ENS1);
            CError.Compare(DataReader.GetAttribute(1), "14", "ga(i)");
            CError.Compare(DataReader.GetAttribute("xmlns"), "14", "ga(str)");
            CError.Compare(DataReader.GetAttribute("xmlns", "http://www.w3.org/2000/xmlns/"), "14", "ga(str, str)");
            CError.Compare(DataReader.GetAttribute("xmlns", "14"), null, "ga inv");

            return TEST_PASS;
        }

        [Variation("this[xmlns] attribute access")]
        public int TXmlns6()
        {
            if (IsXPathNavigatorReader())
                return TEST_SKIPPED;

            ReloadSource();
            DataReader.PositionOnElement(_ST_ENS1);
            CError.Compare(DataReader[1], "14", "this[i]");
            CError.Compare(DataReader["xmlns"], "14", "this[str]");
            CError.Compare(DataReader["xmlns", "http://www.w3.org/2000/xmlns/"], "14", "this[str, str]");
            CError.Compare(DataReader["xmlns", "14"], null, "this inv");

            return TEST_PASS;
        }
    }

    ////////////////////////////////////////////////////////////////
    // TestCase bounded namespace to xmlns prefix DCR50881, DCR57490
    //
    ////////////////////////////////////////////////////////////////
    [InheritRequired()]
    public abstract partial class TCXmlnsPrefix : TCXMLReaderBaseGeneral
    {
        private string _ST_ENS1 = "EMPTY_NAMESPACE1"; //<EMPTY_NAMESPACE1 Attr0="0" xmlns="14"/>
        private string _ST_NS2 = "NAMESPACE2"; //<NAMESPACE2 xmlns:bar="1">
        private string _strXmlns = "http://www.w3.org/2000/xmlns/";

        [Variation("NamespaceURI of xmlns:a attribute", Pri = 0)]
        public int TXmlnsPrefix1()
        {
            ReloadSource();

            DataReader.PositionOnElement(_ST_NS2);
            DataReader.MoveToAttribute(0);

            CError.Compare(DataReader.NamespaceURI, _strXmlns, "nu");

            return TEST_PASS;
        }

        [Variation("NamespaceURI of element/attribute with xmlns attribute", Pri = 0)]
        public int TXmlnsPrefix2()
        {
            ReloadSource();

            DataReader.PositionOnElement(_ST_ENS1);
            CError.Compare(DataReader.NamespaceURI, "14", "nue");

            DataReader.MoveToAttribute("Attr0");
            CError.Compare(DataReader.NamespaceURI, string.Empty, "nu");

            DataReader.MoveToAttribute("xmlns");
            CError.Compare(DataReader.NamespaceURI, _strXmlns, "nu");

            return TEST_PASS;
        }

        [Variation("LookupNamespace with xmlns prefix")]
        public int TXmlnsPrefix3()
        {
            ReloadSource();

            DataReader.Read();
            CError.Compare(DataReader.LookupNamespace("xmlns"), _strXmlns, "ln");

            return TEST_PASS;
        }

        [Variation("Define prefix for 'www.w3.org/2000/xmlns'", Pri = 0)]
        public int TXmlnsPrefix4()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            string strxml = "<ROOT xmlns:pxmlns='http://www.w3.org/2000/xmlns/'/>";
            ReloadSourceStr(strxml);

            try
            {
                DataReader.Read();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Redefine namespace attached to xmlns prefix")]
        public int TXmlnsPrefix5()
        {
            if (!IsXmlTextReader())
                return TEST_SKIPPED;

            string strxml = "<ROOT xmlns:xmlns='http://www.w3.org/2002/xmlns/'/>";
            ReloadSourceStr(strxml);

            try
            {
                DataReader.Read();
            }
            catch (XmlException)
            {
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //[Variation("False duplicates and non-duplicates possible in the XmlReader during attribute normalization", Param = true)]
        //[Variation("False duplicates and non-duplicates possible in the XmlReader during attribute normalization", Param = false)]
        public int TXmlnsPrefix6()
        {
            bool param = (bool)CurVariation.Param;
            string xml = "bug511965" + param + ".xml";
            MemoryStream ms = new MemoryStream();
            XmlWriter w = XmlWriter.Create(ms);
            w.WriteStartDocument(true);
            w.WriteStartElement("root");
            for (int i = 0; i < 250; i++)
            {
                WriteAttribute(w, param, "a" + i, "stra\u00DFe");
            }
            WriteAttribute(w, param, "strasse", "stra\u00DFe");
            WriteAttribute(w, param, "stra\u00DFe", "stra\u00DFe");
            w.WriteEndElement();
            w.WriteEndDocument();
            w.Flush();
            FilePathUtil.addStream(xml, ms);
            ReloadSource(xml);
            while (DataReader.Read()) ;
            return TEST_PASS;
        }
        public void WriteAttribute(XmlWriter w, bool param, string name, string value)
        {
            if (param)
                w.WriteAttributeString(name, "xmlns", value);
            else
                w.WriteAttributeString(name, value);
        }
    }
}
