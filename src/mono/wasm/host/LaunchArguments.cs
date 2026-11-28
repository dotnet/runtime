// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.WebAssembly.AppHost;

internal sealed class LaunchArguments
{
    public const string Usage = "Usage: [--dotnet-env=NAME=VALUE] [--dotnet-runtime-property=NAME=JSON_SCALAR] [--dotnet-help] [--] [application arguments]";

    public string[] ApplicationArguments { get; private set; } = [];
    public Dictionary<string, string> EnvironmentVariables { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, JsonElement> RuntimeProperties { get; } = new(StringComparer.Ordinal);
    [JsonIgnore]
    public bool Help { get; private set; }

    public static LaunchArguments Parse(IEnumerable<string> arguments)
    {
        string[] args = arguments.ToArray();
        var result = new LaunchArguments();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--")
            {
                result.ApplicationArguments = args[(i + 1)..];
                break;
            }
            if (!arg.StartsWith("--dotnet-", StringComparison.Ordinal))
            {
                result.ApplicationArguments = args[i..];
                break;
            }
            if (arg == "--dotnet-help")
            {
                result.Help = true;
                continue;
            }
            bool environment = arg.StartsWith("--dotnet-env=", StringComparison.Ordinal);
            bool property = arg.StartsWith("--dotnet-runtime-property=", StringComparison.Ordinal);
            if (!environment && !property)
                throw InvalidOption(arg);

            string assignment = arg[(environment ? "--dotnet-env=".Length : "--dotnet-runtime-property=".Length)..];
            int separator = assignment.IndexOf('=');
            if (separator <= 0 || assignment.Contains('\0'))
                throw InvalidOption(arg);

            string name = assignment[..separator];
            string text = assignment[(separator + 1)..];
            if (environment)
            {
                result.EnvironmentVariables[name] = text;
                continue;
            }

            JsonDocument json;
            try
            {
                json = JsonDocument.Parse(text);
            }
            catch (JsonException)
            {
                throw InvalidOption(arg);
            }
            using (json)
            {
                JsonElement value = json.RootElement;
                bool valid = value.ValueKind switch
                {
                    JsonValueKind.String => !value.GetString()!.Contains('\0'),
                    JsonValueKind.Number => value.TryGetDouble(out double number) && double.IsFinite(number),
                    JsonValueKind.True or JsonValueKind.False => true,
                    _ => false
                };
                if (!valid || name is "HOST_RUNTIME_CONTRACT" or "TRUSTED_PLATFORM_ASSEMBLIES" or "ENTRY_ASSEMBLY_NAME"
                    or "NATIVE_DLL_SEARCH_DIRECTORIES" or "APP_PATHS" or "APP_CONTEXT_BASE_DIRECTORY" or "RUNTIME_IDENTIFIER")
                    throw InvalidOption(arg);

                result.RuntimeProperties[name] = value.Clone();
            }
        }
        return result;
    }

    private static CommandLineException InvalidOption(string option) =>
        new($"Invalid .NET launch option '{option}'. Use --dotnet-help for usage.");
}
