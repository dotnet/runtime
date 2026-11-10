// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using TestLibrary;

public class Program
{
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static int TestEntryPoint()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        JsonConverter converter = options.GetConverter(typeof(DateTime));
        Console.WriteLine("Converter type: {0}", converter.GetType());
        return converter != null ? 100 : 1;
    }
}
