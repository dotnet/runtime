// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class ManagedToNativeGenerator : Task
{
    [Required]
    public string[]? Assemblies { get; set; }

    [Required]
    public string? RuntimeIcallTableFile { get; set; }

    [Required, NotNull]
    public string? IcallOutputPath { get; set; }

    [Required, NotNull]
    public string[]? PInvokeModules { get; set; }

    [Required, NotNull]
    public string? PInvokeOutputPath { get; set; }

    [Required, NotNull]
    public string? InterpToNativeOutputPath { get; set; }

    [Output]
    public string FileWrites { get; private set; } = string.Empty;

    public override bool Execute()
    {
        var pinvoke = new PInvokeTableGenerator()
        {
            Assemblies = Assemblies,
            Modules = PInvokeModules,
            OutputPath = PInvokeOutputPath,
        };
        var icall = new IcallTableGenerator()
        {
            Assemblies = Assemblies,
            RuntimeIcallTableFile = RuntimeIcallTableFile,
            OutputPath = IcallOutputPath,
        };

        if (pinvoke.Execute() && icall.Execute())
        {
            var m2n = new InterpToNativeGenerator()
            {
                Cookies = Enumerable.Concat(pinvoke.Cookies!, icall.Cookies!).Distinct().ToArray(),
                OutputPath = InterpToNativeOutputPath,
            };

            FileWrites = $"{pinvoke.FileWrites};{icall.FileWrites};{m2n.FileWrites}";
            return m2n.Execute();
        }

        return false;
    }
}
