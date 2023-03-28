// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class MarshalingPInvokeScanner : Task
{
    [Required]
    public string[]? Assemblies { get; set; }

    [Output]
    public string[]? IncompatibleAssemblies { get; private set; }

    private static readonly char[] s_charsToReplace = new[] { '.', '-', '+' };

    // Avoid sharing this cache with all the invocations of this task throughout the build
    private readonly Dictionary<string, string> _symbolNameFixups = new();

    public override bool Execute()
    {
        if (Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(MarshalingPInvokeScanner)}.{nameof(Assemblies)} cannot be empty");
            return false;
        }

        List<string> incompatibleAssemblies = new List<string>();

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
        if (Assemblies != null)
        {
            ScanAssemblies(Assemblies);
        }
    }

    private void ScanAssemblies(string[] assemblies)
    {
        if (assemblies != null)

        {
            var resolver = new System.Reflection.PathAssemblyResolver(assemblies);
            using var mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
            foreach (string aname in assemblies)
            {
                var a = mlc.LoadFromAssemblyPath(aname);
                List<PInvoke> pinvokes = new List<PInvoke>();
                var signatures = new List<string>();
                var callbacks = new List<PInvokeCallback>();
                PInvokeCollector pinvokeCollector = new PInvokeCollector(Log);

                foreach (var type in a.GetTypes())
                {
                    pinvokeCollector.CollectPInvokes(pinvokes, callbacks, signatures, type);
                }
            }
        }
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
