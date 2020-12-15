// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Profiler.Aot;

public class AotProfileToJson : Task
{
    [Required]
    public string? Input { get; set; }
    [Required]
    public string? Output { get; set; }

    public override bool Execute ()
    {
        var reader = new ProfileReader();
        ProfileData data;
        using (FileStream stream = File.OpenRead(Input!))
        {
            data = reader.ReadAllData(stream);
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.Preserve,
                IgnoreNullValues = true,
                WriteIndented = true,
            };
            string s = JsonSerializer.Serialize(data, serializeOptions);
            File.WriteAllText(Output!, s);
        }
        return true;
    }
}