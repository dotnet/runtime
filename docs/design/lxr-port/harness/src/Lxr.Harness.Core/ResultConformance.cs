// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Lxr.Harness.Core;

/// <summary>The outcome of a conformance check, kept as a value so it can be asserted on.</summary>
public sealed class ConformanceReport
{
    public required string Subject { get; init; }

    public required IReadOnlyList<string> Errors { get; init; }

    public bool Ok => Errors.Count == 0;

    public string Format()
    {
        if (Ok)
        {
            return $"conformance: {Subject} is schema-v{RunResult.SchemaVersion} conformant.";
        }

        var text = new System.Text.StringBuilder();
        text.Append("conformance: ").Append(Subject).Append(" has ").Append(Errors.Count).AppendLine(" violation(s):");
        foreach (string error in Errors)
        {
            text.Append("  - ").AppendLine(error);
        }

        return text.ToString();
    }
}

/// <summary>
/// Validates the harness's own output against the schema it claims to emit.
///
/// <para>A conformance check that has never rejected anything is indistinguishable from an absent
/// one, so the harness ships a negative test alongside it: a deliberately malformed record must be
/// rejected, and the rejection is part of the evidence rather than an implementation detail.</para>
/// </summary>
public static class ResultConformance
{
    private static readonly string[] RequiredResultFields =
    [
        "scenario", "collector", "operationsPerSecond", "pauseAverageMs", "pauseP99Ms", "pauseMaxMs",
        "workingSetMb", "committedMb", "notes",
        "latencyP50Ms", "latencyP99Ms", "latencyP999Ms", "latencyP9999Ms", "latencyMaxMs", "latencyMethod",
        "arrivalRatePerSecond", "achievedRatePerSecond", "overloaded",
        "heapFactor", "heapLimitMb",
        "requestedConfig", "observedConfig", "configEvidence", "unverifiedKnobs",
        "collectorConfirmed", "valid", "invalidReason", "status", "skipReason",
        "runtimeBuildId", "runtimeDescription", "coreclrSha256",
        "invocations", "seed", "inducedCollections",
    ];

    private static readonly string[] ValidStatuses =
    [
        RunStatus.Ok, RunStatus.Failed, RunStatus.Timeout, RunStatus.Crashed, RunStatus.Skipped,
    ];

    public static ConformanceReport CheckFile(string path) =>
        new() { Subject = path, Errors = Validate(path) };

    /// <summary>
    /// Checks a document without going through disk, by emitting it exactly as
    /// <see cref="ResultWriter"/> would. Validating the serialized form rather than the object graph
    /// is deliberate: the schema is the JSON, and a check against the in-memory objects could pass
    /// while the emitter wrote something else.
    /// </summary>
    public static ConformanceReport Check(ResultDocument document)
    {
        string temporary = Path.Combine(Path.GetTempPath(), $"lxr-conformance-{Guid.NewGuid():N}.json");
        try
        {
            ResultWriter.Write(temporary, document);
            return new ConformanceReport { Subject = document.Id, Errors = Validate(temporary) };
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    public static IReadOnlyList<string> Validate(string path)
    {
        var violations = new List<string>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException ex)
        {
            return [$"{path}: not valid JSON: {ex.Message}"];
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind is not JsonValueKind.Object)
            {
                return [$"{path}: root is {root.ValueKind}, expected an object"];
            }

            // schemaVersion is required so a v1 reader cannot silently mis-read a v2 record.
            if (!root.TryGetProperty("schemaVersion", out JsonElement schemaVersion))
            {
                violations.Add($"{path}: missing required 'schemaVersion'");
            }
            else if (schemaVersion.ValueKind is not JsonValueKind.Number || schemaVersion.GetInt32() != RunResult.SchemaVersion)
            {
                violations.Add($"{path}: schemaVersion must be {RunResult.SchemaVersion}");
            }

            // The canvas merges every file's 'checkpoints' array. A document that carried its rows at
            // the top level would parse, contribute nothing and report no error, so the wrapper is
            // checked explicitly rather than assumed.
            if (!root.TryGetProperty("checkpoints", out JsonElement checkpoints) || checkpoints.ValueKind is not JsonValueKind.Array)
            {
                violations.Add($"{path}: missing 'checkpoints' array (v1 nests every checkpoint under it)");
                return violations;
            }

            if (checkpoints.GetArrayLength() == 0)
            {
                violations.Add($"{path}: 'checkpoints' is empty, so this file contributes nothing to the board");
            }

            foreach (JsonElement checkpoint in checkpoints.EnumerateArray())
            {
                if (checkpoint.ValueKind is not JsonValueKind.Object)
                {
                    violations.Add($"{path}: checkpoint is {checkpoint.ValueKind}, expected an object");
                    continue;
                }

                foreach (string field in new[] { "id", "date", "stepId", "notes" })
                {
                    if (!checkpoint.TryGetProperty(field, out JsonElement value) || value.ValueKind is not JsonValueKind.String)
                    {
                        violations.Add($"{path}: missing or non-string checkpoint field '{field}'");
                    }
                }

                if (!checkpoint.TryGetProperty("results", out JsonElement results) || results.ValueKind is not JsonValueKind.Array)
                {
                    violations.Add($"{path}: missing 'results' array");
                    continue;
                }

                int index = 0;
                foreach (JsonElement result in results.EnumerateArray())
                {
                    ValidateResult(path, index++, result, violations);
                }
            }
        }

        return violations;
    }

    private static void ValidateResult(string path, int index, JsonElement result, List<string> violations)
    {
        string where = $"{path}: results[{index}]";
        if (result.ValueKind is not JsonValueKind.Object)
        {
            violations.Add($"{where} is {result.ValueKind}, expected an object");
            return;
        }

        foreach (string field in RequiredResultFields)
        {
            if (!result.TryGetProperty(field, out _))
            {
                violations.Add($"{where}: missing required field '{field}'");
            }
        }

        // The scenario id has to be one the harness actually knows, or a chart could be built from a
        // row that corresponds to no defined workload at all.
        if (result.TryGetProperty("scenario", out JsonElement scenario) &&
            scenario.ValueKind is JsonValueKind.String &&
            ScenarioCatalog.Find(scenario.GetString()!) is null)
        {
            violations.Add($"{where}: 'scenario' is '{scenario.GetString()}', which is not in the scenario catalogue");
        }

        if (result.TryGetProperty("operationsPerSecond", out JsonElement ops) &&
            ops.ValueKind is JsonValueKind.Number && ops.GetDouble() < 0)
        {
            violations.Add($"{where}: 'operationsPerSecond' is negative");
        }

        if (result.TryGetProperty("status", out JsonElement status))
        {
            if (status.ValueKind is not JsonValueKind.String || Array.IndexOf(ValidStatuses, status.GetString()) < 0)
            {
                violations.Add($"{where}: 'status' must be one of {string.Join(", ", ValidStatuses)}");
            }
            else if (status.GetString() == RunStatus.Skipped &&
                     (!result.TryGetProperty("skipReason", out JsonElement skipReason) || skipReason.ValueKind is not JsonValueKind.String))
            {
                // A skip must be declared, never a silent absence.
                violations.Add($"{where}: status 'skipped' requires a non-null 'skipReason'");
            }
        }

        bool valid = result.TryGetProperty("valid", out JsonElement validElement) && validElement.ValueKind is JsonValueKind.True;

        // A run that is not valid must carry an explanation, but which field carries it is determined
        // by status: a declared skip explains itself through 'skipReason' (already required above) and
        // is a non-run rather than an invalid run, so demanding 'invalidReason' as well would only
        // invite the redundant and uninformative invalidReason "skipped".
        bool isDeclaredSkip = result.TryGetProperty("status", out JsonElement statusForSkip) &&
            statusForSkip.ValueKind is JsonValueKind.String &&
            statusForSkip.GetString() == RunStatus.Skipped;

        if (!valid && !isDeclaredSkip &&
            (!result.TryGetProperty("invalidReason", out JsonElement reason) || reason.ValueKind is not JsonValueKind.String))
        {
            violations.Add($"{where}: 'valid' is false so 'invalidReason' must say why");
        }

        if (valid)
        {
            if (!result.TryGetProperty("collectorConfirmed", out JsonElement confirmed) || confirmed.ValueKind is not JsonValueKind.True)
            {
                // "A run whose collector cannot be confirmed is invalid."
                violations.Add($"{where}: 'valid' is true but 'collectorConfirmed' is not");
            }

            if (!result.TryGetProperty("runtimeBuildId", out JsonElement buildId) || buildId.ValueKind is not JsonValueKind.String)
            {
                violations.Add($"{where}: a valid run must carry a 'runtimeBuildId'");
            }

            // A run that timed out, crashed, failed or was skipped did not produce a measurement, so
            // it cannot also be valid. Without this the two fields could disagree and a chart built on
            // 'valid' alone would plot a run that never finished.
            if (result.TryGetProperty("status", out JsonElement validStatus) &&
                validStatus.ValueKind is JsonValueKind.String &&
                validStatus.GetString() != RunStatus.Ok)
            {
                violations.Add($"{where}: 'valid' is true but 'status' is '{validStatus.GetString()}'; only an '{RunStatus.Ok}' run can be valid");
            }
        }

        // A latency percentile without a stated method could be a closed-loop number, which would be
        // exactly the mistake this harness exists to prevent.
        bool hasLatency = result.TryGetProperty("latencyP99Ms", out JsonElement p99) && p99.ValueKind is JsonValueKind.Number;
        if (hasLatency &&
            (!result.TryGetProperty("latencyMethod", out JsonElement method) || method.ValueKind is not JsonValueKind.String))
        {
            violations.Add($"{where}: latency percentiles present without 'latencyMethod'; cannot be assumed coordinated-omission free");
        }

        if (hasLatency &&
            (!result.TryGetProperty("arrivalRatePerSecond", out JsonElement rate) || rate.ValueKind is not JsonValueKind.Number))
        {
            violations.Add($"{where}: open-loop latency is meaningless without 'arrivalRatePerSecond'");
        }

        ValidatePercentileOrdering(where, result, violations);

        foreach (string field in new[] { "requestedConfig", "observedConfig", "configEvidence" })
        {
            if (result.TryGetProperty(field, out JsonElement map) && map.ValueKind is not JsonValueKind.Object)
            {
                violations.Add($"{where}: '{field}' must be an object");
            }
        }

        if (result.TryGetProperty("unverifiedKnobs", out JsonElement knobs) && knobs.ValueKind is not JsonValueKind.Array)
        {
            violations.Add($"{where}: 'unverifiedKnobs' must be an array");
        }
    }

    private static void ValidatePercentileOrdering(string where, JsonElement result, List<string> violations)
    {
        string[] ordered = ["latencyP50Ms", "latencyP99Ms", "latencyP999Ms", "latencyP9999Ms", "latencyMaxMs"];
        double previous = double.NegativeInfinity;
        string previousName = string.Empty;

        foreach (string name in ordered)
        {
            if (!result.TryGetProperty(name, out JsonElement element) || element.ValueKind is not JsonValueKind.Number)
            {
                continue;
            }

            double value = element.GetDouble();
            if (value < previous)
            {
                violations.Add(
                    $"{where}: '{name}' ({value.ToString(CultureInfo.InvariantCulture)}) is below '{previousName}' " +
                    $"({previous.ToString(CultureInfo.InvariantCulture)}); percentiles cannot decrease");
            }

            previous = value;
            previousName = name;
        }
    }
}
