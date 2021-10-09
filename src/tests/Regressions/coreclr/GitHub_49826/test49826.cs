// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

class Program
{
    static int Main()
    {
        JsonSerializerOptions options = new JsonSerializerOptions();
        JsonConverter converter = options.GetConverter(typeof(DateTime));
        Console.WriteLine("Converter type: {0}", converter.GetType());
        return converter != null ? 100 : 1;
    }
}
