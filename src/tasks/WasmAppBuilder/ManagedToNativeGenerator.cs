// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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

    private static readonly char[] s_charsToReplace = new[] { '.', '-', '+' };

    // Avoid sharing this cache with all the invocations of this task throughout the build
    private readonly Dictionary<string, string> _symbolNameFixups = new();

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
        var pinvoke = new PInvokeTableGenerator(FixupSymbolName, Log);
        var icall = new IcallTableGenerator(FixupSymbolName, Log);

        IEnumerable<string> cookies = Enumerable.Concat(
            pinvoke.Generate(PInvokeModules, Assemblies!, PInvokeOutputPath!),
            icall.Generate(RuntimeIcallTableFile, Assemblies!, IcallOutputPath)
        );

        var m2n = new InterpToNativeGenerator(Log);
        m2n.Generate(cookies, InterpToNativeOutputPath!);

        FileWrites = IcallOutputPath != null
            ? new string[] { PInvokeOutputPath, IcallOutputPath, InterpToNativeOutputPath }
            : new string[] { PInvokeOutputPath, InterpToNativeOutputPath };
    }

    public string FixupSymbolName(string name)
    {
        if (_symbolNameFixups.TryGetValue(name, out string? fixedName))
            return fixedName;

        UTF8Encoding utf8 = new();
        byte[] bytes = utf8.GetBytes(name);
        StringBuilder sb = new();

        foreach (byte b in bytes)
        {
            if ((b >= (byte)'0' && b <= (byte)'9') ||
                (b >= (byte)'a' && b <= (byte)'z') ||
                (b >= (byte)'A' && b <= (byte)'Z') ||
                (b == (byte)'_'))
            {
                sb.Append((char)b);
            }
            else if (s_charsToReplace.Contains((char)b))
            {
                sb.Append('_');
            }
            else
            {
                sb.Append($"_{b:X}_");
            }
        }

        fixedName = sb.ToString();
        _symbolNameFixups[name] = fixedName;
        return fixedName;
    }
}
