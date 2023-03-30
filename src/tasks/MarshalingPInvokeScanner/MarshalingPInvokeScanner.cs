// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

    private static readonly char[] s_charsToReplace = new char[] { '.', '-', '+' };

    // Avoid sharing this cache with all the invocations of this task throughout the build
    private readonly Dictionary<string, string> _symbolNameFixups = new Dictionary<string, string>();

    public override bool Execute()
    {
        if (Assemblies is null || Assemblies!.Length == 0)
        {
            Log.LogError($"{nameof(MarshalingPInvokeScanner)}.{nameof(Assemblies)} cannot be empty");
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
        if (Assemblies is not null)
            IncompatibleAssemblies = ScanAssemblies(Assemblies);
    }

    private string[] ScanAssemblies(string[] assemblies)
    {
        List<string> incompatible = new List<string>();

        PathAssemblyResolver resolver = new PathAssemblyResolver(assemblies);
        using MetadataLoadContext mlc = new MetadataLoadContext(resolver, "System.Private.CoreLib");
        foreach (string aname in assemblies)
        {
            Assembly assy = mlc.LoadFromAssemblyPath(aname);
            List<PInvoke> pinvokes = new List<PInvoke>();
            List<string> signatures = new List<string>();
            List<PInvokeCallback> callbacks = new List<PInvokeCallback>();
            PInvokeCollector pinvokeCollector = new PInvokeCollector(Log);

            foreach (Type type in assy.GetTypes())
                pinvokeCollector.CollectPInvokes(pinvokes, callbacks, signatures, type);

            if (IsAssemblyIncompatible(assy, pinvokes, signatures, callbacks))
                incompatible.Add(aname);
        }

        return incompatible.ToArray();
    }

    #pragma warning disable IDE0060
    private bool IsAssemblyIncompatible(Assembly assy, List<PInvoke> pivs, List<string> strs, List<PInvokeCallback> cbks)
    {
        // Assembly is incompatible with the lightweight mono marshaler if it does not have the
        // DisableRuntimeMarshallingAttribute and has P/Invokes with nonblittable types.
        IList<CustomAttributeData> attrs = assy.GetCustomAttributesData();
        foreach (CustomAttributeData attr in attrs)
        {
            if (attr.AttributeType == typeof(DisableRuntimeMarshallingAttribute))
                return false;
        }

        try
        {
            foreach (PInvoke piv in pivs)
            {
                foreach (ParameterInfo parInfo in piv.Method.GetParameters())
                {
                    if (!PInvokeCollector.IsBlittable(parInfo.ParameterType))
                        return true;
                }

                if (!PInvokeCollector.IsBlittable(piv.Method.ReturnType) &&
                    piv.Method.ReturnType.FullName != "System.Void")
                    return true;
            }
        }
        catch (NotSupportedException ex)
        {
            Log.LogWarning(null, "WASM0001", "", "", 0, 0, 0, 0,
                $"Could not parse method signature because '{ex.Message}'. This will result in the assembly being marked as incompatible with the lightweight Mono marshaler, potentially as a false positive. ");
            return true;
        }

        return false;
    }
    #pragma warning restore IDE0060


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
