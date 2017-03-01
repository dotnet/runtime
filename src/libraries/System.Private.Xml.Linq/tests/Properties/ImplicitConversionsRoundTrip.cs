// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using Microsoft.Test.ModuleCore;

namespace CoreXml.Test.XLinq
{
    public partial class FunctionalTests : TestModule
    {
        public partial class PropertiesTests : XLinqTestCase
        {
            public static object Explicit(Type ret, XAttribute data)
            {
                switch (ret.Name)
                {
                    case "Boolean":
                        return (Boolean)data;
                    case "Int32":
                        return (Int32)data;
                    case "UInt32":
                        return (UInt32)data;
                    case "Int64":
                        return (Int64)data;
                    case "UInt64":
                        return (UInt64)data;
                    case "Single":
                        return (Single)data;
                    case "Double":
                        return (Double)data;
                    case "Decimal":
                        return (Decimal)data;
                    case "DateTime":
                        return (DateTime)data;
                    case "DateTimeOffset":
                        return (DateTimeOffset)data;
                    case "TimeSpan":
                        return (TimeSpan)data;
                    case "Guid":
                        return (Guid)data;
                    case "Nullable`1":
                        {
                            switch (ret.ToString())
                            {
                                case "System.Nullable`1[System.Boolean]":
                                    return (Boolean?)data;
                                case "System.Nullable`1[System.Int32]":
                                    return (Int32?)data;
                                case "System.Nullable`1[System.Int64]":
                                    return (Int64?)data;
                                case "System.Nullable`1[System.UInt32]":
                                    return (UInt32?)data;
                                case "System.Nullable`1[System.UInt64]":
                                    return (UInt64?)data;
                                case "System.Nullable`1[System.Single]":
                                    return (Single?)data;
                                case "System.Nullable`1[System.Double]":
                                    return (Double?)data;
                                case "System.Nullable`1[System.Decimal]":
                                    return (Decimal?)data;
                                case "System.Nullable`1[System.DateTime]":
                                    return (DateTime?)data;
                                case "System.Nullable`1[System.DateTimeOffset]":
                                    return (DateTimeOffset?)data;
                                case "System.Nullable`1[System.TimeSpan]":
                                    return (TimeSpan?)data;
                                case "System.Nullable`1[System.Guid]":
                                    return (Guid?)data;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            public static object Explicit(Type ret, XElement data)
            {
                switch (ret.Name)
                {
                    case "Boolean":
                        return (Boolean)data;
                    case "Int32":
                        return (Int32)data;
                    case "UInt32":
                        return (UInt32)data;
                    case "Int64":
                        return (Int64)data;
                    case "UInt64":
                        return (UInt64)data;
                    case "Single":
                        return (Single)data;
                    case "Double":
                        return (Double)data;
                    case "Decimal":
                        return (Decimal)data;
                    case "DateTime":
                        return (DateTime)data;
                    case "DateTimeOffset":
                        return (DateTimeOffset)data;
                    case "TimeSpan":
                        return (TimeSpan)data;
                    case "Guid":
                        return (Guid)data;
                    case "Nullable`1":
                        {
                            switch (ret.ToString())
                            {
                                case "System.Nullable`1[System.Boolean]":
                                    return (Boolean?)data;
                                case "System.Nullable`1[System.Int32]":
                                    return (Int32?)data;
                                case "System.Nullable`1[System.Int64]":
                                    return (Int64?)data;
                                case "System.Nullable`1[System.UInt32]":
                                    return (UInt32?)data;
                                case "System.Nullable`1[System.UInt64]":
                                    return (UInt64?)data;
                                case "System.Nullable`1[System.Single]":
                                    return (Single?)data;
                                case "System.Nullable`1[System.Double]":
                                    return (Double?)data;
                                case "System.Nullable`1[System.Decimal]":
                                    return (Decimal?)data;
                                case "System.Nullable`1[System.DateTime]":
                                    return (DateTime?)data;
                                case "System.Nullable`1[System.DateTimeOffset]":
                                    return (DateTimeOffset?)data;
                                case "System.Nullable`1[System.TimeSpan]":
                                    return (TimeSpan?)data;
                                case "System.Nullable`1[System.Guid]":
                                    return (Guid?)data;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }


            public enum ExplicitCastTestType
            {
                RoundTrip,
                XmlConvert
            }

            public enum NodeCreateType
            {
                Constructor,
                SetValue
            }

            //[TestCase(Name = "XElement - value conversion round trip   (constructor)", Params = new object[] { typeof(XElement), ExplicitCastTestType.RoundTrip, NodeCreateType.Constructor })]
            //[TestCase(Name = "XAttribute - value conversion round trip (constructor)", Params = new object[] { typeof(XAttribute), ExplicitCastTestType.RoundTrip, NodeCreateType.Constructor })]
            //[TestCase(Name = "XElement - XmlConvert conformance        (constructor)", Params = new object[] { typeof(XElement), ExplicitCastTestType.XmlConvert, NodeCreateType.Constructor })]
            //[TestCase(Name = "XAttribute - XmlConvert conformance      (constructor)", Params = new object[] { typeof(XAttribute), ExplicitCastTestType.XmlConvert, NodeCreateType.Constructor })]
            //[TestCase(Name = "XElement - value conversion round trip   (SetValue)", Params = new object[] { typeof(XElement), ExplicitCastTestType.RoundTrip, NodeCreateType.SetValue })]
            //[TestCase(Name = "XAttribute - value conversion round trip (SetValue)", Params = new object[] { typeof(XAttribute), ExplicitCastTestType.RoundTrip, NodeCreateType.SetValue })]
            //[TestCase(Name = "XElement - XmlConvert conformance        (SetValue)", Params = new object[] { typeof(XElement), ExplicitCastTestType.XmlConvert, NodeCreateType.SetValue })]
            //[TestCase(Name = "XAttribute - XmlConvert conformance      (SetValue)", Params = new object[] { typeof(XAttribute), ExplicitCastTestType.XmlConvert, NodeCreateType.SetValue })]
            public partial class XElement_Op_Eplicit : XLinqTestCase
            {
                private object[] _data = new object[] {
                    // bool
                    true,
                    false,

                    // Int32
                    (Int32) 1001,
                    (Int32) 0,
                    (Int32) (-321),
                    Int32.MaxValue,
                    Int32.MinValue,

                    // UInt32
                    (UInt32) 1001,
                    (UInt32) 0,
                    UInt32.MaxValue,
                    UInt32.MinValue,

                    (Int64) 0,
                    (Int64) (-641),
                    Int64.MaxValue,
                    Int64.MinValue,

                    (UInt64) 1001,
                    (UInt64) 0,
                    UInt64.MaxValue,
                    UInt64.MinValue,

                    // float
                    (Single) 12.1,
                    (Single) (-12.1),
                    (Single) 0.0,
                    Single.Epsilon,
                    Single.NaN,
                    Single.PositiveInfinity,
                    Single.NegativeInfinity,
                    Single.MinValue,
                    Single.MaxValue,

                    // double 
                    (Double)12.1,
                    (Double)(-12.1),
                    (Double)0.0,
                    Double.Epsilon,
                    Double.NaN,
                    Double.PositiveInfinity,
                    Double.NegativeInfinity,
                    Double.MinValue,
                    Double.MaxValue,

                    // decimal
                    (Decimal)12.1,
                    (Decimal)(-12.1),
                    (Decimal) 0.0,
                    Decimal.MinValue,
                    Decimal.MaxValue,
                    Decimal.MinusOne,
                    Decimal.One,
                    Decimal.Zero,

                    //// DateTimeOffset
                    DateTimeOffset.Now,
                    DateTimeOffset.MaxValue,
                    DateTimeOffset.MinValue,
                    DateTimeOffset.UtcNow,
                    new DateTimeOffset (DateTime.Today),
                    new DateTimeOffset (1989,11,17,19,30,00,TimeSpan.FromHours(8)),
                    new DateTimeOffset (1989,11,17,19,30,00,TimeSpan.FromHours(-8)),

                    // timespan
                    TimeSpan.MaxValue,
                    TimeSpan.MinValue,
                    TimeSpan.Zero,
                    TimeSpan.FromHours (1.4),
                    TimeSpan.FromMilliseconds (1.0),
                    TimeSpan.FromMinutes (5.0),

                    // Guid
                    System.Guid.Empty,
                    System.Guid.NewGuid(),
                    System.Guid.NewGuid()
                };

                public static Dictionary<Type, Type> typeMapper;

                static XElement_Op_Eplicit()
                {
                    if (typeMapper == null)
                    {
                        typeMapper = new Dictionary<Type, Type>();
                        typeMapper.Add(typeof(bool), typeof(bool?));
                        typeMapper.Add(typeof(Int32), typeof(Int32?));
                        typeMapper.Add(typeof(Int64), typeof(Int64?));
                        typeMapper.Add(typeof(UInt32), typeof(UInt32?));
                        typeMapper.Add(typeof(UInt64), typeof(UInt64?));
                        typeMapper.Add(typeof(Single), typeof(Single?));
                        typeMapper.Add(typeof(Double), typeof(Double?));
                        typeMapper.Add(typeof(Decimal), typeof(Decimal?));
                        typeMapper.Add(typeof(DateTime), typeof(DateTime?));
                        typeMapper.Add(typeof(DateTimeOffset), typeof(DateTimeOffset?));
                        typeMapper.Add(typeof(TimeSpan), typeof(TimeSpan?));
                        typeMapper.Add(typeof(Guid), typeof(Guid?));
                    }
                }

                protected override void DetermineChildren()
                {
                    base.DetermineChildren();
                    Type type = Params[0] as Type;
                    ExplicitCastTestType testType = (ExplicitCastTestType)Params[1];
                    NodeCreateType createType = (NodeCreateType)Params[2];

                    // add normal types
                    foreach (object o in _data)
                    {
                        string desc = o.GetType().ToString() + " : ";
                        // On Arabic locale DateTime and DateTimeOffset types threw on serialization if the date was 
                        // too big for the Arabic calendar
                        if (o is DateTime)
                            desc += ((DateTime)o).ToString(CultureInfo.InvariantCulture);
                        else if (o is DateTimeOffset)
                            desc += ((DateTimeOffset)o).ToString(CultureInfo.InvariantCulture);
                        else
                            desc += o;

                        AddChild(new ExplicitCastVariation(testType, createType, type, o, o.GetType(), this, desc));
                    }

                    // add Nullable types check (not applicable for XmlConvert tests)
                    if (testType == ExplicitCastTestType.RoundTrip)
                    {
                        foreach (object o in _data)
                        {
                            string desc = o.GetType().ToString() + " : ";
                            // On Arabic locale DateTime and DateTimeOffset types threw on serialization if the date was 
                            // too big for the Arabic calendar
                            if (o is DateTime)
                                desc += ((DateTime)o).ToString(CultureInfo.InvariantCulture);
                            else if (o is DateTimeOffset)
                                desc += ((DateTimeOffset)o).ToString(CultureInfo.InvariantCulture);
                            else
                                desc += o;
                            AddChild(new ExplicitCastVariation(testType, createType, type, o, typeMapper[o.GetType()], this, desc));
                        }
                    }
                }


                //[Variation(Desc = "XElement.SetValue(null)", Param = null)]
                //[Variation(Desc = "XElement.SetValue(null)", Param = "")]
                //[Variation(Desc = "XElement.SetValue(null)", Param = "text")]
                //[Variation(Desc = "XElement.SetValue(null)", Param = typeof(XElement))]
                public void SetValueNull()
                {
                    XElement el = new XElement("elem", Variation.Param is Type ? new XElement("X") : Variation.Param);
                    try
                    {
                        el.SetValue(null);
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentNullException) { }
                }

                //[Variation(Desc = "XAttribute.SetValue(null)")]
                public void SetValueNullAttr()
                {
                    XAttribute a = new XAttribute("a", "A");
                    try
                    {
                        a.SetValue(null);
                        throw new TestException(TestResult.Failed, "");
                    }
                    catch (ArgumentNullException) { }
                }

                //[Variation(Desc = "Conversion to bool overloads (1,True,true)", Params = new object[] { true, new string[] { "1", "True", "true","TRUE", " TRue " } })]
                //[Variation(Desc = "Conversion to bool overloads (0,False,false)", Params = new object[] {  false, new string[] { "0", "False", "false", "FALSE", " FalsE " } })]
                public void ConversionoBool()
                {
                    Type type = Params[0] as Type;
                    NodeCreateType nodeCreateType = (NodeCreateType)Params[2];

                    bool expV = (bool)CurrentChild.Params[0];
                    string[] strs = CurrentChild.Params[1] as string[];

                    object node = null;

                    foreach (string data in strs)
                    {
                        switch (nodeCreateType)
                        {
                            case NodeCreateType.Constructor:
                                if (type == typeof(XElement))
                                {
                                    node = new XElement(XName.Get("name"), data);
                                }
                                else if (type == typeof(XAttribute))
                                {
                                    node = new XAttribute(XName.Get("name"), data);
                                }
                                break;
                            case NodeCreateType.SetValue:
                                if (type == typeof(XElement))
                                {
                                    node = new XElement(XName.Get("name"), "");
                                    ((XElement)node).SetValue(data);
                                }
                                else if (type == typeof(XAttribute))
                                {
                                    node = new XAttribute(XName.Get("name"), "");
                                    ((XAttribute)node).SetValue(data);
                                }
                                break;
                            default:
                                TestLog.Compare(false, "test failed: wrong create type");
                                break;
                        }

                        object retData = type == typeof(XElement) ? (bool)(node as XElement) : (bool)(node as XAttribute);
                        TestLog.Compare(retData.Equals(expV), "Data verification for string :: " + data);
                    }
                }
            }

            public partial class ExplicitCastVariation : TestVariation
            {
                private object _data;
                private Type _nodeType;
                private Type _retType;
                private ExplicitCastTestType _testType;
                private NodeCreateType _nodeCreateType;
                private string _desc;

                public ExplicitCastVariation(ExplicitCastTestType testType, NodeCreateType nodeCreateType, Type nodeType, object data, Type retType, TestCase testCase, string desc)
                {
                    _desc = desc;
                    _data = data;
                    _nodeType = nodeType;
                    _testType = testType;
                    _retType = retType;
                    _nodeCreateType = nodeCreateType;
                }

                private XObject CreateContainer()
                {
                    if (_nodeCreateType == NodeCreateType.Constructor)
                    {
                        if (_nodeType.Name.Equals("XElement"))
                        {
                            return new XElement(XName.Get("name"), _data);
                        }

                        if (_nodeType.Name.Equals("XAttribute"))
                        {
                            return new XAttribute(XName.Get("name"), _data);
                        }
                    }
                    else if (_nodeCreateType == NodeCreateType.SetValue)
                    {
                        if (_nodeType.Name.Equals("XElement"))
                        {
                            var node = new XElement(XName.Get("name"), "");
                            node.SetValue(_data);
                            return node;
                        }

                        if (_nodeType.Name.Equals("XAttribute"))
                        {
                            var node = new XAttribute(XName.Get("name"), "");
                            node.SetValue(_data);
                            return node;
                        }
                    }

                    throw new ArgumentOutOfRangeException("Unknown NodeCreateType: " + _nodeCreateType);
                }

                public override TestResult Execute()
                {
                    var node = CreateContainer();

                    switch (_testType)
                    {
                        case ExplicitCastTestType.RoundTrip:
                            if (_nodeType.Name.Equals("XElement"))
                            {
                                var retData = Explicit(_retType, (XElement)node);
                                TestLog.Compare(retData, _data, "XElement (" + _retType + ")");
                            }
                            else
                            {
                                var retData = Explicit(_retType, (XAttribute)node);
                                TestLog.Compare(retData, _data, "XElement (" + _retType + ")");
                            }
                            break;
                        case ExplicitCastTestType.XmlConvert:
                            string xmlConv = "";
                            switch (_data.GetType().Name)
                            {
                                case "Boolean":
                                    xmlConv = XmlConvert.ToString((Boolean)_data);
                                    break;
                                case "Int32":
                                    xmlConv = XmlConvert.ToString((Int32)_data);
                                    break;
                                case "UInt32":
                                    xmlConv = XmlConvert.ToString((UInt32)_data);
                                    break;
                                case "Int64":
                                    xmlConv = XmlConvert.ToString((Int64)_data);
                                    break;
                                case "UInt64":
                                    xmlConv = XmlConvert.ToString((UInt64)_data);
                                    break;
                                case "Single":
                                    xmlConv = XmlConvert.ToString((Single)_data);
                                    break;
                                case "Double":
                                    xmlConv = XmlConvert.ToString((Double)_data);
                                    break;
                                case "Decimal":
                                    xmlConv = XmlConvert.ToString((Decimal)_data);
                                    break;
                                case "DateTime":
                                    TestLog.Skip("DateTime Convert include +8:00");
                                    break;
                                case "DateTimeOffset":
                                    xmlConv = XmlConvert.ToString((DateTimeOffset)_data);
                                    break;
                                case "TimeSpan":
                                    xmlConv = XmlConvert.ToString((TimeSpan)_data);
                                    break;
                                case "Guid":
                                    xmlConv = XmlConvert.ToString((Guid)_data);
                                    break;
                                default:
                                    TestLog.Skip("No XmlConvert.ToString (" + _data.GetType().Name + ")");
                                    break;
                            }
                            string value = node is XElement ? ((XElement)node).Value : ((XAttribute)node).Value;
                            TestLog.Compare(value == xmlConv, "XmlConvert verification");
                            break;
                        default:
                            TestLog.Compare(false, "Test failed: wrong test type");
                            break;
                    }
                    return TestResult.Passed;
                }
            }

            public partial class XElement_Op_Eplicit_Null : XLinqTestCase
            {
                protected override void DetermineChildren()
                {
                    Type nodeType = Params[0] as Type;

                    foreach (Type retType in XElement_Op_Eplicit.typeMapper.Values)
                    {
                        AddChild(new ExplicitCastNullVariation(nodeType, retType, false, this, retType.ToString()));
                    }
                    foreach (Type retType in XElement_Op_Eplicit.typeMapper.Keys)
                    {
                        AddChild(new ExplicitCastNullVariation(nodeType, retType, true, this, retType.ToString()));
                    }
                }
            }

            public partial class ExplicitCastNullVariation : TestVariation
            {
                private Type _nodeType, _retType;
                private bool _shouldThrow;

                public ExplicitCastNullVariation(Type nodeType, Type retType, bool shouldThrow, TestCase tc, string desc)
                {
                    this.Desc = desc;
                    _nodeType = nodeType;
                    _retType = retType;
                    _shouldThrow = shouldThrow;
                }

                public override TestResult Execute()
                {
                    try
                    {
                        object ret = null;
                        if (_nodeType.Equals(typeof(XElement)))
                        {
                            XElement e = null;
                            ret = Explicit(_retType, e);
                        }
                        else if (_nodeType.Equals(typeof(XAttribute)))
                        {
                            XAttribute a = null;
                            ret = Explicit(_retType, a);
                        }
                        TestLog.Compare(!_shouldThrow, "The call should throw!");
                        TestLog.Compare(ret == null, "Return value should be null");
                    }
                    catch (ArgumentNullException e)
                    {
                        TestLog.Compare(e is ArgumentNullException, "e.InnerException is ArgumentNullException");
                        TestLog.Compare(_shouldThrow, "shouldThrow");
                    }
                    return TestResult.Passed;
                }
            }
        }
    }
}
