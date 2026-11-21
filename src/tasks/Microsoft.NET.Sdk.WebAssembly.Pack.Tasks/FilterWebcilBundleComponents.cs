// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Sdk.WebAssembly;

public sealed class FilterWebcilBundleComponents : Task
{
    [Required]
    public string BundlePath { get; set; }

    [Required]
    public ITaskItem[] Candidates { get; set; }

    [Output]
    public ITaskItem[] UnbundledCandidates { get; set; } = Array.Empty<ITaskItem>();

    public override bool Execute()
    {
        try
        {
            var bundle = new WebcilBundleReader(BundlePath);
            var unbundled = new List<ITaskItem>();
            foreach (ITaskItem candidate in Candidates)
            {
                if (!bundle.ContainsMatchingAssembly(candidate.ItemSpec))
                {
                    unbundled.Add(candidate);
                }
            }
            UnbundledCandidates = unbundled.ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or BadImageFormatException or ArgumentException or OverflowException)
        {
            Log.LogError($"Cannot use Webcil component bundle '{BundlePath}': {ex.Message}");
        }

        return !Log.HasLoggedErrors;
    }
}
