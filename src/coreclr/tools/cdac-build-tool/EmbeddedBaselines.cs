// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Diagnostics.DataContract.BuildTool;

public partial class EmbeddedBaselines
{
    public const string TemplateResourceNamePrefix = "Microsoft.DotNet.Diagnostics.DataContract.Baseline:";
    public const string TemplateResourceNameEscapePrefix = @"Microsoft\.DotNet\.Diagnostics\.DataContract\.Baseline:";
    public const string TemplateResourceNameExt = ".jsonc";
    public const string TemplateResourceNameEscapeExt = @"\.jsonc";

    [GeneratedRegex("^" + TemplateResourceNameEscapePrefix + "(.+)" + TemplateResourceNameEscapeExt + "$", RegexOptions.CultureInvariant)]
    private static partial Regex BaselineRegex();

    private static string[] GetBaselineNames()
    {
        var assembly = typeof(EmbeddedBaselines).Assembly;
        var resources = assembly.GetManifestResourceNames();
        var baselineNames = new List<string>();
        foreach (var resource in resources)
        {
            var match = BaselineRegex().Match(resource);
            if (match.Success)
            {
                baselineNames.Add(match.Groups[1].Value);
            }
        }
        return baselineNames.ToArray();
    }

    private static readonly Lazy<IReadOnlyList<string>> _baselineNames = new(GetBaselineNames);
    public static IReadOnlyList<string> BaselineNames => _baselineNames.Value;

    public static string GetBaselineContent(string name)
    {
        var assembly = typeof(EmbeddedBaselines).Assembly;
        var resourceName = TemplateResourceNamePrefix + name + TemplateResourceNameExt;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Baseline '{name}' not found");
        }
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
