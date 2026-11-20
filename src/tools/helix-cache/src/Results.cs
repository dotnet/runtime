// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace HelixCache;

internal readonly record struct TestOutcome(string Assembly, string Name, string Result, string SkipReason);

internal sealed record TestRun(ResultReceipt Receipt, TestSummary Summary, Dictionary<TestOutcome, int> Outcomes)
{
    internal bool HasSameOutcomes(TestRun other) =>
        Outcomes.Count == other.Outcomes.Count &&
        Outcomes.All(p => other.Outcomes.TryGetValue(p.Key, out int count) && count == p.Value);
}

internal static class Results
{
    internal static TestRun Read(string directory, ExecutionRecord execution)
    {
        using FileStream file = File.OpenRead(Path.Combine(directory, "result.json"));
        ResultReceipt receipt = JsonSerializer.Deserialize(file, CacheJsonContext.Default.ResultReceipt)
            ?? throw new InvalidDataException($"Missing result receipt: {directory}.");
        Cache.ValidateVersion(receipt.SchemaVersion);
        if (!receipt.Completed || !receipt.InfrastructureSucceeded ||
            receipt.ExitCode != execution.Inputs.ExpectedExitCode ||
            receipt.WorkItemName != execution.Inputs.Name || receipt.TargetQueue != execution.Inputs.TargetQueue ||
            !Guid.TryParse(receipt.JobId, out _) || receipt.ResultFiles.Count == 0 ||
            receipt.ResultFiles.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException($"Results are incomplete, unsuccessful, or do not match the execution record: {directory}.");
        }

        TestSummary summary = new();
        Dictionary<TestOutcome, int> outcomes = [];
        HashSet<string> reports = new(StringComparer.Ordinal);
        foreach (string report in receipt.ResultFiles)
        {
            Snapshot.ValidateRelativePath(report);
            if (!reports.Add(report))
            {
                throw new InvalidDataException($"Duplicate report in {nameof(receipt.ResultFiles)}: {report}.");
            }
            ReadReport(Path.Combine(directory, report), summary, outcomes);
        }
        return new TestRun(receipt, summary, outcomes);
    }

    private static void ReadReport(string path, TestSummary summary, Dictionary<TestOutcome, int> outcomes)
    {
        using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        XDocument document = XDocument.Load(reader);
        if (document.Root is not XElement root || root.Name != "assemblies" || !root.Elements("assembly").Any() ||
            document.Descendants("error").Any() || document.Descendants("failure").Any())
        {
            throw InvalidReport(path);
        }
        foreach (XElement assembly in root.Elements("assembly"))
        {
            string? assemblyName = Path.GetFileName((string?)assembly.Attribute("name"));
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                throw InvalidReport(path);
            }
            TestSummary actual = new();
            foreach (XElement test in assembly.Elements("collection").Elements("test"))
            {
                string? name = (string?)test.Attribute("name");
                string? result = (string?)test.Attribute("result");
                if (string.IsNullOrWhiteSpace(name) || result is not ("Pass" or "Skip"))
                {
                    throw InvalidReport(path);
                }
                TestOutcome outcome = new(assemblyName, name, result,
                    result == "Skip" ? (string?)test.Element("reason")?.Element("message") ?? "" : "");
                outcomes.TryGetValue(outcome, out int count);
                checked
                {
                    outcomes[outcome] = count + 1;
                    actual.Total++;
                    actual.Passed += result == "Pass" ? 1 : 0;
                    actual.Skipped += result == "Skip" ? 1 : 0;
                }
            }
            if (actual.Total == 0 || assembly.Descendants("test").Count() != actual.Total ||
                Count(assembly, "total") != actual.Total || Count(assembly, "passed") != actual.Passed ||
                Count(assembly, "skipped") != actual.Skipped || Count(assembly, "failed") != 0 ||
                (assembly.Attribute("errors") is not null && Count(assembly, "errors") != 0))
            {
                throw InvalidReport(path);
            }
            checked
            {
                summary.Total += actual.Total;
                summary.Passed += actual.Passed;
                summary.Skipped += actual.Skipped;
            }
        }
    }

    private static InvalidDataException InvalidReport(string path) =>
        new($"Expected a completed, nonempty, passing xUnit report with named tests and assemblies: {path}.");

    private static int Count(XElement assembly, string name)
    {
        if (!int.TryParse((string?)assembly.Attribute(name), NumberStyles.None, CultureInfo.InvariantCulture, out int count))
        {
            throw new InvalidDataException($"Invalid xUnit report count: {name}.");
        }
        return count;
    }
}
