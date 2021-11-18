// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.DotNet.XHarness.TestRunners.Xunit;
using WasmTestRunner;
using Xunit;

public class SimpleWasmTestRunner
{
    public static async Task<int> Main(string[] args)
    {
        var testAssembly = args[0];
        var excludedTraits = new List<string>();
        var includedTraits = new List<string>();
        var includedNamespaces = new List<string>();
        var includedClasses = new List<string>();
        var includedMethods = new List<string>();

        for (int i = 1; i < args.Length; i++)
        {
            var option = args[i];
            switch (option)
            {
                case "-notrait":
                    excludedTraits.Add (args[i + 1]);
                    i++;
                    break;
                case "-trait":
                    includedTraits.Add (args[i + 1]);
                    i++;
                    break;
                case "-namespace":
                    includedNamespaces.Add (args[i + 1]);
                    i++;
                    break;
                case "-class":
                    includedClasses.Add (args[i + 1]);
                    i++;
                    break;
                case "-method":
                    includedMethods.Add (args[i + 1]);
                    i++;
                    break;
                default:
                    throw new ArgumentException($"Invalid argument '{option}'.");
            }
        }


        var filters = new XunitFilters();

        foreach (var trait in excludedTraits) ParseEqualSeparatedArgument(filters.ExcludedTraits, trait);
        foreach (var trait in includedTraits) ParseEqualSeparatedArgument(filters.IncludedTraits, trait);
        foreach (var ns in includedNamespaces) filters.IncludedNamespaces.Add(ns);
        foreach (var cl in includedClasses) filters.IncludedClasses.Add(cl);
        foreach (var me in includedMethods) filters.IncludedMethods.Add(me);

        var result = await XThreadlessXunitTestRunner.Run(testAssembly, printXml: true, filters);
        return result;
    }

    private static void ParseEqualSeparatedArgument(Dictionary<string, List<string>> targetDictionary, string argument)
    {
        var parts = argument.Split('=');
        if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1]))
        {
            throw new ArgumentException($"Invalid argument value '{argument}'.", nameof(argument));
        }

        var name = parts[0];
        var value = parts[1];
        List<string> excludedTraits;
        if (targetDictionary.TryGetValue(name, out excludedTraits!))
        {
            excludedTraits.Add(value);
        }
        else
        {
            targetDictionary[name] = new List<string> { value };
        }
    }
}
