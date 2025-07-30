// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

public struct TestGroup
{
    public (string TemplateConfig, Dictionary<string, string> KeyValuePairs)[] Tests { get; set; }

    public TestGroup((string, Dictionary<string, string>)[] tests)
    {
        Tests = tests;
    }

    public (string, Dictionary<string, string>)[] GetTests()
    {
        return Tests.Select(t =>
        {
            var data = new Dictionary<string, string>(t.KeyValuePairs);
            return (t.TemplateConfig, data);
        }).ToArray();
    }
}
