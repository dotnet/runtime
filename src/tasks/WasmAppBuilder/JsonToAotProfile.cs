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

public class JsonToAotProfile : Task
{
    [Required]
    public string? Input { get; set; }
    [Required]
    public string? Output { get; set; }

    public override bool Execute ()
    {
        var reader = new ProfileReader();
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.Preserve,
        };
        byte[] inputData = File.ReadAllBytes(Input!);
        ProfileData data = JsonSerializer.Deserialize<ProfileData>(inputData, serializeOptions)!;
        var writer = new ProfileWriter();
        using (FileStream outStream = File.Create(Output!))
        {
            writer.WriteAllData(outStream, data);
        }
        return true;
    }
}