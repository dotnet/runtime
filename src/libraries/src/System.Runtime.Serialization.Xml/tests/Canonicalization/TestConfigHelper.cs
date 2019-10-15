// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

internal static class TestConfigHelper
{
    private static readonly XmlSerializer s_Serializer = new XmlSerializer(typeof(TestCasesConfig));
    private static TestCase s_currentTest;
    private static TestCasesConfig s_allTests;

    public static TestCase CurrentTest
    {
        get { return s_currentTest; }
    }

    public static TestCasesConfig AllTests
    {
        get { return s_allTests; }
    }

    public static void LoadAllTests(string path)
    {
        using (var stream = File.OpenRead(path))
        {
            s_allTests = (TestCasesConfig)s_Serializer.Deserialize(stream);
        }
    }

    public static TestCase GetTest(string name)
    {
        return s_currentTest = s_allTests.TestCases.Where(tc => tc.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
    }
}

[XmlType]
[XmlRoot("testcases", IsNullable = false)]
public class TestCasesConfig
{
    [XmlElement("testcase")]
    public List<TestCase> TestCases;
}

[XmlType]
public class TestCase
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlElement("input")]
    public List<Input> Inputs;
}

[XmlType]
public class Input
{
    [XmlElement("argu")]
    public List<Argument> Arguments;

    [XmlElement("expect")]
    public object Result { get; set; }
}

[XmlType]
public class Argument
{
    [XmlAttribute("name")]
    public string Name { get; set; }

    [XmlAttribute("value")]
    public string Value { get; set; }

    [XmlElement("choice", IsNullable = true)]
    public List<string> Choices { get; set; }
}