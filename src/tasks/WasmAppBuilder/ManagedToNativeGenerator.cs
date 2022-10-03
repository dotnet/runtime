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

#nullable enable

public class ManagedToNativeGenerator : Task
{
    [Required]
    public string[]? Assemblies { get; set; }

    public string? RuntimeIcallTableFile { get; set; }

    public string? IcallOutputPath { get; set; }

    [Required, NotNull]
    public string[]? PInvokeModules { get; set; }

    [Required, NotNull]
    public string? PInvokeOutputPath { get; set; }

    [Required, NotNull]
    public string? InterpToNativeOutputPath { get; set; }

    [Output]
    public string[]? FileWrites { get; private set; }

    public override bool Execute()
    {
        if (Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        if (PInvokeModules!.Length == 0)
        {
            Log.LogError($"{nameof(ManagedToNativeGenerator)}.{nameof(PInvokeModules)} cannot be empty");
            return false;
        }

        try
        {
            ExecuteInternal();
            return !Log.HasLoggedErrors;
        }
        catch (LogAsErrorException e)
        {
            Log.LogError(e.Message);
            return false;
        }
    }

    private void ExecuteInternal()
    {
        var pinvoke = new PInvokeTableGenerator(Log);
        var icall = new IcallTableGenerator(Log);

        IEnumerable<string> cookies = Enumerable.Concat(
            pinvoke.GenPInvokeTable(PInvokeModules, Assemblies!, PInvokeOutputPath!),
            icall.GenIcallTable(RuntimeIcallTableFile, Assemblies!, IcallOutputPath)
        );

        var m2n = new InterpToNativeGenerator(Log);
        m2n.Generate(cookies, InterpToNativeOutputPath!);

        FileWrites = IcallOutputPath != null
            ? new string[] { PInvokeOutputPath, IcallOutputPath, InterpToNativeOutputPath }
            : new string[] { PInvokeOutputPath, InterpToNativeOutputPath };
    }
}
