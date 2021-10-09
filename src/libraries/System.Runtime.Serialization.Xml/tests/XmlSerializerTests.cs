// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Xunit;

public static class XmlSerializerTests
{

    public const string FakeNS = "http://example.com/XmlSerializerTests";

    [Fact]
    public static void FlagEnums_With_Different_Namespaces()
    {
        StringWriter sw = new StringWriter();
        XmlTextWriter xml = new XmlTextWriter(sw);

        TwoClasses twoClasses = new TwoClasses
        {
            First = new FirstClass { TestingEnumValues = TestEnum.First },
            Second = new SecondClass { TestingEnumValues = TestEnum.Second }
        };

        // 43675 - This line throws with inconsistent Flag.type/namespace usage
        XmlSerializer ser = new XmlSerializer(typeof(TwoClasses));

        ser.Serialize(xml, twoClasses);
        string s = sw.ToString();

        Assert.Contains("enumtest", s);
    }

    [Flags]
    public enum TestEnum
    {
        First = 1,
        Second = 2
    }

    public class FirstClass
    {
        [XmlAttribute("enumtest")]
        public TestEnum TestingEnumValues;
    }

    public class SecondClass
    {
        [XmlAttribute("enumtest", Namespace = XmlSerializerTests.FakeNS)]
        public TestEnum TestingEnumValues;
    }

    public class TwoClasses
    {
        public TwoClasses() { }

        [XmlElement("first")]
        public FirstClass First;

        [XmlElement("second", Namespace = XmlSerializerTests.FakeNS)]
        public SecondClass Second;
    }

}
