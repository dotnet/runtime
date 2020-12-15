// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Profiler.Aot;

public class AotProfileFilter : Task
{
    [Required]
    public string? Input { get; set; }
    [Required]
    public string? Output { get; set; }
    public string[]? ExcludeMethods { get; set; }

    public override bool Execute ()
    {
        var reader = new ProfileReader();
        ProfileData profile;
        using (FileStream stream = File.OpenRead(Input!))
             profile = reader.ReadAllData(stream);

        if (ExcludeMethods != null)
        {
            var newMethods = new List<MethodRecord>();
            foreach (MethodRecord method in profile.Methods)
            {
                bool isFiltered = ExcludeMethods!.Any (e => method.Name == e);
                if (!isFiltered)
                    newMethods.Add (method);
            }
            profile.Methods = newMethods.ToArray();
        }

        var writer = new ProfileWriter();
        using (FileStream outStream = File.Create(Output!))
             writer.WriteAllData(outStream, profile);

        return true;
    }
}