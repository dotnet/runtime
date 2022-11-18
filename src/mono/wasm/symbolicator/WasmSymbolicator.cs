// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.DotNet.XHarness.Common;

namespace Microsoft.WebAssembly.Internal;

internal sealed class WasmSymbolicator
{
    private const string ReplaceSectionGroupName = "replaceSection";
    private const string FuncNumGroupName= "funcNum";
    private readonly Dictionary<int, string> _wasmFunctionMap = new();
    private readonly ILogger _logger;
    private readonly List<Regex> _regexes = new();

    public bool CanSymbolicate => _wasmFunctionMap.Count > 0 && _regexes.Count > 0;

    public WasmSymbolicator(string? symbolsMapFile, string? symbolPatternsFile, bool throwOnMissing, ILogger logger)
    {
        _logger = logger;
        if (string.IsNullOrEmpty(symbolsMapFile) || !File.Exists(symbolsMapFile))
        {
            if (throwOnMissing)
                throw new ArgumentException($"Cannot find symbols file: {symbolsMapFile}");

            _logger.LogWarning($"Cannot find symbols file {symbolsMapFile}");
            return;
        }

        if (string.IsNullOrEmpty(symbolPatternsFile) || !File.Exists(symbolPatternsFile))
        {
            if (throwOnMissing)
                throw new ArgumentException($"Cannot find symbol patterns file: {symbolPatternsFile}");

            _logger.LogWarning("no symbol patterns file given");
            return;
        }

        int i = 0;
        foreach (var line in File.ReadAllLines(symbolsMapFile))
        {
            string[] parts = line.Split(new char[] { ':' }, 2);
            if (parts.Length != 2)
            {
                _logger.LogWarning($"Unexpected symbol map format at line {i + 1} in {symbolsMapFile}");
                break;
            }

            if (int.TryParse(parts[0], out int num))
                _wasmFunctionMap[num] = parts[1];
        }

        foreach (var line in File.ReadAllLines(symbolPatternsFile))
        {
            if (string.IsNullOrEmpty(line) || line[0] == '#')
                continue;

            try
            {
                Regex regex = new(line);
                if (!regex.GetGroupNames().Where(n => n == FuncNumGroupName).Any())
                {
                    _logger.LogWarning($"Ignoring pattern with missing `{FuncNumGroupName}` group: {line}");
                    continue;
                }
                _regexes.Add(regex);
            }
            catch (ArgumentException ae)
            {
                _logger.LogWarning($"Failed to compile regex for symbol patterns: {line} with error: {ae.Message}");
                continue;
            }
        }
    }

    public string Symbolicate(string line)
    {
        if (!CanSymbolicate)
            return line;

        foreach (var regex in _regexes)
        {
            var newLine = regex.Replace(line, new MatchEvaluator(m =>
            {
                if (!int.TryParse(m.Groups?[FuncNumGroupName]?.Value, out int funcNum))
                    return m.ToString();

                if (!_wasmFunctionMap.TryGetValue(funcNum, out string? name))
                    name = "<unknown>";

                if (m.Groups?.ContainsKey(ReplaceSectionGroupName) == true)
                {
                    string hay = m.Groups[ReplaceSectionGroupName].Value;
                    return m.ToString().Replace(hay, $"{name} ({hay})");
                }
                else
                {
                    string hay = m.ToString();
                    return m.ToString().Replace(hay, $"at {name} ({hay})");
                }
            }));

            if (newLine != line)
                return newLine;
        }
        return line;
    }

}
