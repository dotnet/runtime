// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Xml.Linq;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class SDMSamplesTests : XLinqTestCase
        {
            public partial class SDM_Attribute : XLinqTestCase
            {
                public void AttributeConstructor()
                {
                    string value = "bar";

                    // Name/value constructor.
                    try
                    {
                        XAttribute a = new XAttribute(null, value);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    try
                    {
                        XAttribute a = new XAttribute("foo", null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Codepaths for special-casing xmlns namespace

                    XName name = XName.Get("xmlns", string.Empty);
                    XAttribute att1 = new XAttribute(name, value);
                    Validate.AttributeNameAndValue(att1, "xmlns", value);

                    name = XName.Get("xmlns", "namespacename");
                    att1 = new XAttribute(name, value);
                    Validate.AttributeNameAndValue(att1, "{namespacename}xmlns", value);

                    name = XName.Get("foo", string.Empty);
                    att1 = new XAttribute(name, value);
                    Validate.AttributeNameAndValue(att1, "foo", value);

                    name = XName.Get("foo", "namespacename");
                    att1 = new XAttribute(name, value);
                    Validate.AttributeNameAndValue(att1, "{namespacename}foo", value);

                    // Copy constructor.

                    try
                    {
                        XAttribute a = new XAttribute((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    XAttribute att2 = new XAttribute(att1);
                    Validate.AttributeNameAndValue(att2, att1.Name.ToString(), att1.Value);
                }

                /// <summary>
                /// Tests EmptySequence on XAttribute.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "AttributeEmptySequence")]
                public void AttributeEmptySequence()
                {
                    Validate.Enumerator(XAttribute.EmptySequence, new XAttribute[0]);
                    Validate.Enumerator(XAttribute.EmptySequence, new XAttribute[0]);
                }

                /// <summary>
                /// Tests IsNamespaceDeclaration on XAttribute.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "AttributeIsNamespaceDeclaration")]
                public void AttributeIsNamespaceDeclaration()
                {
                    XAttribute att1 = new XAttribute("{http://bogus}name", "value");
                    XAttribute att2 = new XAttribute("{http://www.w3.org/2000/xmlns/}name", "value");

                    Validate.IsEqual(att1.IsNamespaceDeclaration, false);
                    Validate.IsEqual(att2.IsNamespaceDeclaration, true);
                }

                /// <summary>
                /// Tests Parent on XAttribute.
                /// </summary>
                /// <param name="context"></param>
                /// <returns></returns>
                //[Variation(Desc = "AttributeParent")]
                public void AttributeParent()
                {
                    XAttribute a = new XAttribute("att-name", "value");
                    XElement e = new XElement("elem-name");

                    Validate.IsNull(a.Parent);

                    e.Add(a);

                    Validate.IsEqual(a.Parent, e);

                    e.RemoveAttributes();

                    Validate.IsNull(a.Parent);
                }

                /// <summary>
                /// Validate behavior of the Value property on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeValue")]
                public void AttributeValue()
                {
                    XAttribute a = new XAttribute("foo", 10m);

                    Validate.IsEqual(a.Value, "10");

                    try
                    {
                        a.Value = null;
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    a.Value = "100";
                    Validate.IsEqual(a.Value, "100");
                }

                /// <summary>
                /// Validates the behavior of the Remove method on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeRemove")]
                public void AttributeRemove()
                {
                    XElement e = new XElement("element");
                    XAttribute a = new XAttribute("attribute", "value");

                    // Can't remove when no parent.
                    try
                    {
                        a.Remove();
                        Validate.ExpectedThrow(typeof(InvalidOperationException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(InvalidOperationException));
                    }

                    e.Add(a);
                    Validate.Count(e.Attributes(), 1);

                    a.Remove();
                    Validate.Count(e.Attributes(), 0);
                }

                /// <summary>
                /// Validates the explicit string conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToString")]
                public void AttributeExplicitToString()
                {
                    XAttribute e2 = new XAttribute("x", string.Empty);
                    XAttribute e3 = new XAttribute("x", 10.0);

                    string s0 = (string)((XAttribute)null);
                    string s2 = (string)e2;
                    string s3 = (string)e3;

                    Validate.IsNull(s0);
                    Validate.String(s2, string.Empty);
                    Validate.String(s3, "10");
                }

                /// <summary>
                /// Validates the explicit boolean conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToBoolean")]
                public void AttributeExplicitToBoolean()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        bool b = (bool)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "true");
                    XAttribute e4 = new XAttribute("x", "false");
                    XAttribute e5 = new XAttribute("x", "0");
                    XAttribute e6 = new XAttribute("x", "1");

                    try
                    {
                        bool b1 = (bool)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        bool b2 = (bool)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    Validate.IsEqual((bool)e3, true);
                    Validate.IsEqual((bool)e4, false);
                    Validate.IsEqual((bool)e5, false);
                    Validate.IsEqual((bool)e6, true);
                }

                /// <summary>
                /// Validates the explicit int32 conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToInt32")]
                public void AttributeExplicitToInt32()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        int i = (int)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "2147483648");
                    XAttribute e4 = new XAttribute("x", "5");

                    try
                    {
                        int i1 = (int)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        int i2 = (int)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        int i3 = (int)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((int)e4, 5);
                }

                /// <summary>
                /// Validates the explicit uint32 conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToUInt32")]
                public void AttributeExplicitToUInt32()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        uint i = (uint)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "4294967296");
                    XAttribute e4 = new XAttribute("x", "5");

                    try
                    {
                        uint i1 = (uint)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        uint i2 = (uint)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        uint i3 = (uint)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((uint)e4, 5u);
                }

                /// <summary>
                /// Validates the explicit int64 conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToInt64")]
                public void AttributeExplicitToInt64()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        long i = (long)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "18446744073709551616");
                    XAttribute e4 = new XAttribute("x", "5");

                    try
                    {
                        long i1 = (long)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        long i2 = (long)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        long i3 = (long)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((long)e4, 5L);
                }

                /// <summary>
                /// Validates the explicit uint64 conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToUInt64")]
                public void AttributeExplicitToUInt64()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        ulong i = (ulong)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "18446744073709551616");
                    XAttribute e4 = new XAttribute("x", "5");

                    try
                    {
                        ulong i1 = (ulong)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        ulong i2 = (ulong)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        ulong i3 = (ulong)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((ulong)e4, 5UL);
                }

                /// <summary>
                /// Validates the explicit float conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToFloat")]
                public void AttributeExplicitToFloat()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        float f = (float)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "5e+500");
                    XAttribute e4 = new XAttribute("x", "5.0");

                    try
                    {
                        float f1 = (float)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        float f2 = (float)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        float i3 = (float)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((float)e4, 5.0f);
                }

                /// <summary>
                /// Validates the explicit double conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToDouble")]
                public void AttributeExplicitToDouble()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        double f = (double)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "5e+5000");
                    XAttribute e4 = new XAttribute("x", "5.0");

                    try
                    {
                        double f1 = (double)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        double f2 = (double)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        double f3 = (double)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((double)e4, 5.0);
                }

                /// <summary>
                /// Validates the explicit decimal conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToDecimal")]
                public void AttributeExplicitToDecimal()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        decimal d = (decimal)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "111111111111111111111111111111111111111111111111");
                    XAttribute e4 = new XAttribute("x", "5.0");

                    try
                    {
                        decimal d1 = (decimal)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        decimal d2 = (decimal)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        decimal d3 = (decimal)e3;
                        Validate.ExpectedThrow(typeof(OverflowException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(OverflowException));
                    }

                    Validate.IsEqual((decimal)e4, 5m);
                }

                /// <summary>
                /// Validates the explicit DateTime conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToDateTime")]
                public void AttributeExplicitToDateTime()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        DateTime d = (DateTime)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "1968-01-07");

                    try
                    {
                        DateTime d1 = (DateTime)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        DateTime d2 = (DateTime)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    Validate.IsEqual((DateTime)e3, new DateTime(1968, 1, 7));
                }

                /// <summary>
                /// Validates the explicit TimeSpan conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToTimeSpan")]
                public void AttributeExplicitToTimeSpan()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        TimeSpan d = (TimeSpan)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", "PT1H2M3S");

                    try
                    {
                        TimeSpan d1 = (TimeSpan)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        TimeSpan d2 = (TimeSpan)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    Validate.IsEqual((TimeSpan)e3, new TimeSpan(1, 2, 3));
                }

                /// <summary>
                /// Validates the explicit guid conversion operator on XAttribute.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToGuId")]
                public void AttributeExplicitToGuid()
                {
                    // Calling explicit operator with null should result in exception.
                    try
                    {
                        Guid g = (Guid)((XAttribute)null);
                        Validate.ExpectedThrow(typeof(ArgumentNullException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(ArgumentNullException));
                    }

                    string guid = "2b67e9fb-97ad-4258-8590-8bc8c2d32df5";

                    // Test various values.
                    XAttribute e1 = new XAttribute("x", string.Empty);
                    XAttribute e2 = new XAttribute("x", "bogus");
                    XAttribute e3 = new XAttribute("x", guid);

                    try
                    {
                        Guid g1 = (Guid)e1;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    try
                    {
                        Guid g2 = (Guid)e2;
                        Validate.ExpectedThrow(typeof(FormatException));
                    }
                    catch (Exception ex)
                    {
                        Validate.Catch(ex, typeof(FormatException));
                    }

                    Validate.IsEqual((Guid)e3, new Guid(guid));
                }

                /// <summary>
                /// Validates the explicit conversion operators on XAttribute
                /// for nullable value types.
                /// </summary>
                /// <returns>true if pass, false if fail</returns>
                //[Variation(Desc = "AttributeExplicitToNullables")]
                public void AttributeExplicitToNullables()
                {
                    string guid = "cd8d69ed-fef9-4283-aaf4-216463e4496f";

                    bool? b = (bool?)new XAttribute("x", true);
                    int? i = (int?)new XAttribute("x", 5);
                    uint? u = (uint?)new XAttribute("x", 5);
                    long? l = (long?)new XAttribute("x", 5);
                    ulong? ul = (ulong?)new XAttribute("x", 5);
                    float? f = (float?)new XAttribute("x", 5);
                    double? n = (double?)new XAttribute("x", 5);
                    decimal? d = (decimal?)new XAttribute("x", 5);
                    DateTime? dt = (DateTime?)new XAttribute("x", "1968-01-07");
                    TimeSpan? ts = (TimeSpan?)new XAttribute("x", "PT1H2M3S");
                    Guid? g = (Guid?)new XAttribute("x", guid);

                    Validate.IsEqual(b.Value, true);
                    Validate.IsEqual(i.Value, 5);
                    Validate.IsEqual(u.Value, 5u);
                    Validate.IsEqual(l.Value, 5L);
                    Validate.IsEqual(ul.Value, 5uL);
                    Validate.IsEqual(f.Value, 5.0f);
                    Validate.IsEqual(n.Value, 5.0);
                    Validate.IsEqual(d.Value, 5.0m);
                    Validate.IsEqual(dt.Value, new DateTime(1968, 1, 7));
                    Validate.IsEqual(ts.Value, new TimeSpan(1, 2, 3));
                    Validate.IsEqual(g.Value, new Guid(guid));

                    b = (bool?)((XAttribute)null);
                    i = (int?)((XAttribute)null);
                    u = (uint?)((XAttribute)null);
                    l = (long?)((XAttribute)null);
                    ul = (ulong?)((XAttribute)null);
                    f = (float?)((XAttribute)null);
                    n = (double?)((XAttribute)null);
                    d = (decimal?)((XAttribute)null);
                    dt = (DateTime?)((XAttribute)null);
                    ts = (TimeSpan?)((XAttribute)null);
                    g = (Guid?)((XAttribute)null);

                    Validate.IsNull(b);
                    Validate.IsNull(i);
                    Validate.IsNull(u);
                    Validate.IsNull(l);
                    Validate.IsNull(ul);
                    Validate.IsNull(f);
                    Validate.IsNull(n);
                    Validate.IsNull(d);
                    Validate.IsNull(dt);
                    Validate.IsNull(ts);
                    Validate.IsNull(g);
                }
            }
        }
    }
}