// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace HelixCache;

internal static class Results
{
    internal static TestSummary Validate(string directory, ExecutionRecord execution)
    {
        ResultReceipt receipt = JsonFile.Read(Path.Combine(directory, "result.json"), CacheJsonContext.Default.ResultReceipt);
        JsonFile.ValidateVersion(receipt.SchemaVersion);
        if (!receipt.Completed || !receipt.InfrastructureSucceeded ||
            receipt.ExitCode != execution.Inputs.ExpectedExitCode ||
            receipt.WorkItemName != execution.Inputs.Name ||
            receipt.TargetQueue != execution.Inputs.TargetQueue ||
            !Guid.TryParse(receipt.JobId, out _) || receipt.ResultFiles.Count == 0)
        {
            throw new InvalidDataException(SR.Get("Result", directory));
        }

        TestSummary summary = new();
        HashSet<string> reports = new(StringComparer.Ordinal);
        foreach (string report in receipt.ResultFiles)
        {
            JsonFile.RelativePath(report);
            JsonFile.Require(reports.Add(report), nameof(receipt.ResultFiles));
            string path = Path.Combine(directory, report);
            using XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            XDocument document = XDocument.Load(reader);
            if (document.Root is not XElement root || root.Name != "assemblies" || !root.Elements("assembly").Any())
            {
                throw new InvalidDataException(SR.Get("Report", report));
            }

            foreach (XElement assembly in root.Elements("assembly"))
            {
                TestSummary actual = new();
                foreach (XElement test in assembly.Descendants("test"))
                {
                    checked
                    {
                        actual.Total++;
                        switch ((string?)test.Attribute("result"))
                        {
                            case "Pass":
                                actual.Passed++;
                                break;
                            case "Skip":
                                actual.Skipped++;
                                break;
                            default:
                                throw new InvalidDataException(SR.Get("Report", report));
                        }
                    }
                }

                if (actual.Total == 0 || Count(assembly, "total") != actual.Total ||
                    Count(assembly, "passed") != actual.Passed || Count(assembly, "skipped") != actual.Skipped ||
                    Count(assembly, "failed") != 0 ||
                    assembly.Descendants("error").Any() || assembly.Descendants("failure").Any() ||
                    (assembly.Attribute("errors") is not null && Count(assembly, "errors") != 0))
                {
                    throw new InvalidDataException(SR.Get("Report", report));
                }

                checked
                {
                    summary.Total += actual.Total;
                    summary.Passed += actual.Passed;
                    summary.Skipped += actual.Skipped;
                }
            }
        }

        return summary;
    }

    private static int Count(XElement assembly, string name)
    {
        if (!int.TryParse((string?)assembly.Attribute(name), NumberStyles.None, CultureInfo.InvariantCulture, out int count))
        {
            throw new InvalidDataException(SR.Get("Report", name));
        }
        return count;
    }
}
