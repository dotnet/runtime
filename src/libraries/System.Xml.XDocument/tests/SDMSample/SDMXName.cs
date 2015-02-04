// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class SDMSamplesTests : XLinqTestCase
        {
            public partial class SDM_XName : XLinqTestCase
            {
                /// <summary>
                /// Gets an XML qualified name for an XName, for interop.
                /// </summary>
                /// <param name="name">XName.</param>
                /// <returns>XmlQualifiedName.</returns>
                internal static XmlQualifiedName GetQName(XName name)
                {
                    return new XmlQualifiedName(name.LocalName, name.Namespace.NamespaceName);
                }

                /// <summary>
                /// Tests trying to use an invalid name with XName.Get.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "NameGetInvalId")]
                public void NameGetInvalid()
                {
                    try
                    {
                        XName.Get(null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    try
                    {
                        XName.Get(null, "foo");
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    try
                    {
                        XName.Get(string.Empty, "foo");
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    try
                    {
                        XName.Get(string.Empty);
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }

                    try
                    {
                        XName.Get("{}");
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }

                    try
                    {
                        XName.Get("{foo}");
                        Validate.ExpectedThrow(typeof(ArgumentException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentException));
                    }
                }

                /// <summary>
                /// Tests the operators on XName.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "NameOperators")]
                public void NameOperators()
                {
                    // Implicit conversion from string.
                    XName name = (XName)(string)null;
                    Validate.IsNull(name);

                    name = (XName)"foo";
                    Validate.String(name.Namespace.NamespaceName, "");
                    Validate.String(name.LocalName, "foo");

                    name = (XName)"{bar}foo";
                    Validate.String(name.Namespace.NamespaceName, "bar");
                    Validate.String(name.LocalName, "foo");

                    // Conversion to XmlQualifiedName
                    XmlQualifiedName qname = GetQName(name);
                    Validate.String(qname.Namespace, "bar");
                    Validate.String(qname.Name, "foo");

                    // Equality, which should be based on reference equality.
                    XName ns1 = (XName)"foo";
                    XName ns2 = (XName)"foo";
                    XName ns3 = (XName)"bar";
                    XName ns4 = null;

                    Validate.IsReferenceEqual(ns1, ns2);
                    Validate.IsNotReferenceEqual(ns1, ns3);
                    Validate.IsNotReferenceEqual(ns2, ns3);

                    bool b1 = ns1 == ns2;   // equal
                    bool b2 = ns1 == ns3;   // not equal
                    bool b3 = ns1 == ns4;   // not equal

                    Validate.IsEqual(b1, true);
                    Validate.IsEqual(b2, false);
                    Validate.IsEqual(b3, false);

                    b1 = ns1 != ns2;   // false
                    b2 = ns1 != ns3;   // true
                    b3 = ns1 != ns4;   // true

                    Validate.IsEqual(b1, false);
                    Validate.IsEqual(b2, true);
                    Validate.IsEqual(b3, true);
                }

                /// <summary>
                /// Tests trying to use an invalid name with XNamespace.Get.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "NamespaceGetNull")]
                public void NamespaceGetNull()
                {
                    try
                    {
                        XNamespace.Get(null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }
                }

                /// <summary>
                /// Tests the operators on XNamespace.
                /// </summary>
                /// <param name="contextValue"></param>
                /// <returns></returns>
                //[Variation(Desc = "NamespaceOperators")]
                public void NamespaceOperators()
                {
                    // Implicit conversion from string.
                    XNamespace ns = (XNamespace)(string)null;
                    Validate.IsNull(ns);

                    ns = (XNamespace)"foo";
                    Validate.String(ns.NamespaceName, "foo");

                    // Operator +
                    XName name;

                    try
                    {
                        name = (XNamespace)null + "localname";
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    try
                    {
                        name = ns + (string)null;
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    name = ns + "localname";
                    Validate.String(name.LocalName, "localname");
                    Validate.String(name.Namespace.NamespaceName, "foo");

                    // Equality, which should be based on reference equality.
                    XNamespace ns1 = (XNamespace)"foo";
                    XNamespace ns2 = (XNamespace)"foo";
                    XNamespace ns3 = (XNamespace)"bar";
                    XNamespace ns4 = null;

                    Validate.IsReferenceEqual(ns1, ns2);
                    Validate.IsNotReferenceEqual(ns1, ns3);
                    Validate.IsNotReferenceEqual(ns2, ns3);

                    bool b1 = ns1 == ns2;   // equal
                    bool b2 = ns1 == ns3;   // not equal
                    bool b3 = ns1 == ns4;   // not equal

                    Validate.IsEqual(b1, true);
                    Validate.IsEqual(b2, false);
                    Validate.IsEqual(b3, false);

                    b1 = ns1 != ns2;   // false
                    b2 = ns1 != ns3;   // true
                    b3 = ns1 != ns4;   // true

                    Validate.IsEqual(b1, false);
                    Validate.IsEqual(b2, true);
                    Validate.IsEqual(b3, true);
                }
            }
        }
    }
}

