// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lxr.Harness.Runner;

/// <summary>
/// Injects collector configuration into a worker's <c>runtimeconfig.json</c> as
/// <c>configProperties</c>.
///
/// <para>This is the public host-property channel, and using it rather than <c>DOTNET_*</c>
/// environment variables is a deliberate correctness decision. The property channel parses integers
/// with base zero, i.e. decimal (<c>configuration.cpp</c> line 86), while the environment channel
/// parses GC integers as hexadecimal (<c>gcenv.ee.cpp</c> line 1338). A heap count of 16 delivered
/// through the environment would silently request twenty-two heaps, and nothing in the result would
/// say so.</para>
///
/// <para>The pristine file produced by the build is captured on first use and every launch is written
/// from that template, so configuration from one cell can never leak into the next - which would be a
/// particularly quiet way to corrupt an A/B comparison.</para>
/// </summary>
public static class RuntimeConfigWriter
{
    private static readonly Dictionary<string, string> Templates = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();

    public static string ConfigPathFor(string workerAssembly) =>
        Path.ChangeExtension(workerAssembly, ".runtimeconfig.json");

    public static void ApplyProperties(string workerAssembly, IReadOnlyDictionary<string, string> properties)
    {
        string configPath = ConfigPathFor(workerAssembly);
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException(
                $"Worker '{workerAssembly}' has no runtimeconfig.json, so collector configuration cannot be delivered through the host-property channel.",
                configPath);
        }

        lock (Gate)
        {
            if (!Templates.TryGetValue(configPath, out string? template))
            {
                template = File.ReadAllText(configPath);
                Templates[configPath] = template;
            }

            JsonNode root = JsonNode.Parse(template)
                ?? throw new InvalidDataException($"'{configPath}' is not a JSON object.");

            JsonNode options = root["runtimeOptions"]
                ?? throw new InvalidDataException($"'{configPath}' has no runtimeOptions section.");

            var configProperties = options["configProperties"] as JsonObject;
            if (configProperties is null)
            {
                configProperties = new JsonObject();
                options["configProperties"] = configProperties;
            }

            foreach (KeyValuePair<string, string> property in properties)
            {
                configProperties[property.Key] = Coerce(property.Value);
            }

            File.WriteAllText(configPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    /// <summary>
    /// Restores the file the build produced. Leaving a mutated runtimeconfig behind would make a later
    /// manual run silently inherit the last cell's collector.
    /// </summary>
    public static void Restore(string workerAssembly)
    {
        string configPath = ConfigPathFor(workerAssembly);
        lock (Gate)
        {
            if (Templates.TryGetValue(configPath, out string? template))
            {
                File.WriteAllText(configPath, template);
            }
        }
    }

    public static void RestoreAll()
    {
        lock (Gate)
        {
            foreach (KeyValuePair<string, string> entry in Templates)
            {
                File.WriteAllText(entry.Key, entry.Value);
            }
        }
    }

    private static JsonNode Coerce(string value) =>
        bool.TryParse(value, out bool flag) ? JsonValue.Create(flag)
        : long.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long number) ? JsonValue.Create(number)
        : JsonValue.Create(value);
}
