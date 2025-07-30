// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

public struct TestGroup
{
    public string Isa { get; set; }
    public string LoadIsa { get; set; }
    public (string TemplateConfig, Dictionary<string, string> KeyValuePairs)[] Tests { get; set; }

    public TestGroup(string Isa, string LoadIsa, (string, Dictionary<string, string>)[] Tests)
    {
        this.Isa = Isa;
        this.LoadIsa = LoadIsa;
        this.Tests = Tests;
    }

    public (string, Dictionary<string, string>)[] GetTests()
    {
        var self = this;
        return Tests.Select(t =>
        {
            var data = new Dictionary<string, string>(t.KeyValuePairs);
            if (!string.IsNullOrEmpty(self.Isa)) data["Isa"] = self.Isa;
            if (!string.IsNullOrEmpty(self.LoadIsa)) data["LoadIsa"] = self.LoadIsa;
            return (t.TemplateConfig, data);
        }).ToArray();
    }
}
