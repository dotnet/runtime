// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.Configuration.DotEnv;

internal sealed partial class DotEnvConfigurationFileParser
{
    private DotEnvConfigurationFileParser() { }

    public static Dictionary<string, string?> Parse(Stream input) => ParseStream(input);

    private static Dictionary<string, string?> ParseStream(Stream input)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(input);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is null)
            {
                break;
            }

            line = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            // Find the = separator
            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == line.Length - 1)
            {
                continue; // Invalid line format
            }

            var key = line[..separatorIndex].Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = line[(separatorIndex + 1)..].Trim();

            // Handle quoted values
            data[key] = ProcessValue(value, data);
        }

        return data;
    }

    private static string? ProcessValue(string? value, Dictionary<string, string?> data)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Handle double-quoted values
        if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
        {
            value = value[1..^1];

            // Unescape common escape sequences within double quotes
            value = value.Replace("\\\"", "\"")
                         .Replace("\\n", "\n")
                         .Replace("\\r", "\r")
                         .Replace("\\t", "\t")
                         .Replace("\\\\", "\\");

            // Expand environment variables in double-quoted values
            value = ExpandEnvironmentVariables(value, data);

            return value;
        }

        // Handle single-quoted values
        if (value.Length >= 2 && value.StartsWith('\'') && value.EndsWith('\''))
        {
            // Single quotes preserve literal values - no expansion
            return value[1..^1];
        }

        // Handle unquoted values - remove inline comments
        int commentIndex = value.IndexOf(" #");
        if (commentIndex >= 0)
        {
            value = value[..commentIndex].Trim();
        }

        // Expand environment variables in unquoted values
        value = ExpandEnvironmentVariables(value, data);

        return value;
    }

    private static string? ExpandEnvironmentVariables(string? value, Dictionary<string, string?> data)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Match ${VAR_NAME} or $VAR_NAME patterns
        var variablePattern = VariableNamePattern();
        
        return variablePattern.Replace(value, match =>
        {
            // Get variable name from either ${VAR} or $VAR format
            string varName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            
            // First, try to get the value from the internal data dictionary
            if (data.TryGetValue(varName, out string? internalValue))
            {
                return internalValue ?? string.Empty;
            }
            
            // Fall back to system environment variable
            string? envValue = Environment.GetEnvironmentVariable(varName);
            
            // Return the environment variable value or the original match if not found
            return envValue ?? match.Value;
        });
    }

    [GeneratedRegex(@"\$\{([^}]+)\}|\$([A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled)]
    private static partial Regex VariableNamePattern();
}
