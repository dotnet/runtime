// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.NET.Sdk.WebAssembly;

namespace Microsoft.NET.Sdk.WebAssembly;

// This task does the build work of processing the project inputs and producing a set of pseudo-static web assets.
public class ComputeWasmBuildAssets : Task
{
    [Required]
    public ITaskItem[] Candidates { get; set; }

    public ITaskItem CustomIcuCandidate { get; set; }

    [Required]
    public ITaskItem[] ProjectAssembly { get; set; }

    [Required]
    public ITaskItem[] ProjectDebugSymbols { get; set; }

    [Required]
    public ITaskItem[] SatelliteAssemblies { get; set; }

    [Required]
    public ITaskItem[] ProjectSatelliteAssemblies { get; set; }

    [Required]
    public string OutputPath { get; set; }

    [Required]
    public bool TimeZoneSupport { get; set; }

    [Required]
    public bool InvariantGlobalization { get; set; }

    [Required]
    public bool CopySymbols { get; set; }

    public bool FingerprintDotNetJs { get; set; }

    public bool EnableThreads { get; set; }

    [Output]
    public ITaskItem[] AssetCandidates { get; set; }

    [Output]
    public ITaskItem[] FilesToRemove { get; set; }

    public override bool Execute()
    {
        var filesToRemove = new List<ITaskItem>();
        var assetCandidates = new List<ITaskItem>();

        try
        {
            if (ProjectAssembly.Length != 1)
            {
                Log.LogError("Invalid number of project assemblies '{0}'", string.Join("," + Environment.NewLine, ProjectAssembly.Select(a => a.ItemSpec)));
                return true;
            }

            if (ProjectDebugSymbols.Length > 1)
            {
                Log.LogError("Invalid number of symbol assemblies '{0}'", string.Join("," + Environment.NewLine, ProjectDebugSymbols.Select(a => a.ItemSpec)));
                return true;
            }

            if (AssetsComputingHelper.TryGetAssetFilename(CustomIcuCandidate, out string customIcuCandidateFilename))
            {
                var customIcuCandidate = AssetsComputingHelper.GetCustomIcuAsset(CustomIcuCandidate);
                assetCandidates.Add(customIcuCandidate);
            }

            for (int i = 0; i < Candidates.Length; i++)
            {
                var candidate = Candidates[i];
                if (AssetsComputingHelper.ShouldFilterCandidate(candidate, TimeZoneSupport, InvariantGlobalization, CopySymbols, customIcuCandidateFilename, EnableThreads, out var reason))
                {
                    Log.LogMessage(MessageImportance.Low, "Skipping asset '{0}' because '{1}'", candidate.ItemSpec, reason);
                    filesToRemove.Add(candidate);
                    continue;
                }

                var satelliteAssembly = SatelliteAssemblies.FirstOrDefault(s => s.ItemSpec == candidate.ItemSpec);
                if (satelliteAssembly != null)
                {
                    var inferredCulture = satelliteAssembly.GetMetadata("DestinationSubDirectory").Trim('\\', '/');
                    Log.LogMessage(MessageImportance.Low, "Found satellite assembly '{0}' asset for candidate '{1}' with inferred culture '{2}'", satelliteAssembly.ItemSpec, candidate.ItemSpec, inferredCulture);

                    var assetCandidate = new TaskItem(satelliteAssembly);
                    assetCandidate.SetMetadata("AssetKind", "Build");
                    assetCandidate.SetMetadata("AssetRole", "Related");
                    assetCandidate.SetMetadata("AssetTraitName", "Culture");
                    assetCandidate.SetMetadata("AssetTraitValue", inferredCulture);
                    assetCandidate.SetMetadata("RelativePath", $"_framework/{inferredCulture}/{satelliteAssembly.GetMetadata("FileName")}{satelliteAssembly.GetMetadata("Extension")}");
                    assetCandidate.SetMetadata("RelatedAsset", Path.GetFullPath(Path.Combine(OutputPath, "wwwroot", "_framework", Path.GetFileName(assetCandidate.GetMetadata("ResolvedFrom")))));

                    assetCandidates.Add(assetCandidate);
                    continue;
                }

                string candidateFileName = candidate.GetMetadata("FileName");
                if ((candidateFileName == "dotnet" || candidateFileName == "dotnet.worker") && candidate.GetMetadata("Extension") == ".js")
                {
                    string newDotnetJSFileName = null;
                    string newDotNetJSFullPath = null;
                    if (FingerprintDotNetJs)
                    {
                        var itemHash = FileHasher.GetFileHash(candidate.ItemSpec);
                        newDotnetJSFileName = $"{candidateFileName}.{candidate.GetMetadata("NuGetPackageVersion")}.{itemHash}.js";

                        var originalFileFullPath = Path.GetFullPath(candidate.ItemSpec);
                        var originalFileDirectory = Path.GetDirectoryName(originalFileFullPath);

                        newDotNetJSFullPath = Path.Combine(originalFileDirectory, newDotnetJSFileName);
                    }
                    else
                    {
                        newDotNetJSFullPath = candidate.ItemSpec;
                        newDotnetJSFileName = Path.GetFileName(newDotNetJSFullPath);
                    }

                    var newDotNetJs = new TaskItem(newDotNetJSFullPath, candidate.CloneCustomMetadata());
                    newDotNetJs.SetMetadata("OriginalItemSpec", candidate.ItemSpec);

                    var newRelativePath = $"_framework/{newDotnetJSFileName}";
                    newDotNetJs.SetMetadata("RelativePath", newRelativePath);

                    newDotNetJs.SetMetadata("AssetTraitName", "WasmResource");
                    newDotNetJs.SetMetadata("AssetTraitValue", "native");

                    assetCandidates.Add(newDotNetJs);
                    continue;
                }
                else
                {
                    string relativePath = AssetsComputingHelper.GetCandidateRelativePath(candidate);
                    candidate.SetMetadata("RelativePath", relativePath);
                }

                // Workaround for https://github.com/dotnet/aspnetcore/issues/37574.
                // For items added as "Reference" in project references, the OriginalItemSpec is incorrect.
                // Ignore it, and use the FullPath instead.
                if (candidate.GetMetadata("ReferenceSourceTarget") == "ProjectReference")
                {
                    candidate.SetMetadata("OriginalItemSpec", candidate.ItemSpec);
                }

                var culture = candidate.GetMetadata("Culture");
                if (!string.IsNullOrEmpty(culture))
                {
                    candidate.SetMetadata("AssetKind", "Build");
                    candidate.SetMetadata("AssetRole", "Related");
                    candidate.SetMetadata("AssetTraitName", "Culture");
                    candidate.SetMetadata("AssetTraitValue", culture);
                    var fileName = candidate.GetMetadata("FileName");
                    var suffixIndex = fileName.Length - ".resources".Length;
                    var relatedAssetPath = Path.GetFullPath(Path.Combine(
                        OutputPath,
                        "wwwroot",
                        "_framework",
                        fileName.Substring(0, suffixIndex) + ProjectAssembly[0].GetMetadata("Extension")));

                    candidate.SetMetadata("RelatedAsset", relatedAssetPath);

                    Log.LogMessage(MessageImportance.Low, "Found satellite assembly '{0}' asset for inferred candidate '{1}' with culture '{2}'", candidate.ItemSpec, relatedAssetPath, culture);
                }

                assetCandidates.Add(candidate);
            }

            var intermediateAssembly = new TaskItem(ProjectAssembly[0]);
            intermediateAssembly.SetMetadata("RelativePath", $"_framework/{intermediateAssembly.GetMetadata("FileName")}{intermediateAssembly.GetMetadata("Extension")}");
            assetCandidates.Add(intermediateAssembly);

            if (ProjectDebugSymbols.Length > 0)
            {
                var debugSymbols = new TaskItem(ProjectDebugSymbols[0]);
                debugSymbols.SetMetadata("RelativePath", $"_framework/{debugSymbols.GetMetadata("FileName")}{debugSymbols.GetMetadata("Extension")}");
                assetCandidates.Add(debugSymbols);
            }

            for (int i = 0; i < ProjectSatelliteAssemblies.Length; i++)
            {
                var projectSatelliteAssembly = ProjectSatelliteAssemblies[i];
                var candidateCulture = projectSatelliteAssembly.GetMetadata("Culture");
                Log.LogMessage(
                    "Found satellite assembly '{0}' asset for project '{1}' with culture '{2}'",
                    projectSatelliteAssembly.ItemSpec,
                    intermediateAssembly.ItemSpec,
                    candidateCulture);

                var assetCandidate = new TaskItem(Path.GetFullPath(projectSatelliteAssembly.ItemSpec), projectSatelliteAssembly.CloneCustomMetadata());
                var projectAssemblyAssetPath = Path.GetFullPath(Path.Combine(
                    OutputPath,
                    "wwwroot",
                    "_framework",
                    ProjectAssembly[0].GetMetadata("FileName") + ProjectAssembly[0].GetMetadata("Extension")));

                var normalizedPath = assetCandidate.GetMetadata("TargetPath").Replace('\\', '/');

                assetCandidate.SetMetadata("AssetKind", "Build");
                assetCandidate.SetMetadata("AssetRole", "Related");
                assetCandidate.SetMetadata("AssetTraitName", "Culture");
                assetCandidate.SetMetadata("AssetTraitValue", candidateCulture);
                assetCandidate.SetMetadata("RelativePath", Path.Combine("_framework", normalizedPath));
                assetCandidate.SetMetadata("RelatedAsset", projectAssemblyAssetPath);

                assetCandidates.Add(assetCandidate);
            }

            for (var i = 0; i < assetCandidates.Count; i++)
            {
                var candidate = assetCandidates[i];
                ApplyUniqueMetadataProperties(candidate);
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            return false;
        }

        FilesToRemove = filesToRemove.ToArray();
        AssetCandidates = assetCandidates.ToArray();

        return !Log.HasLoggedErrors;
    }

    private static void ApplyUniqueMetadataProperties(ITaskItem candidate)
    {
        var extension = candidate.GetMetadata("Extension");
        var filename = candidate.GetMetadata("FileName");
        switch (extension)
        {
            case ".dll":
                if (string.IsNullOrEmpty(candidate.GetMetadata("AssetTraitName")))
                {
                    candidate.SetMetadata("AssetTraitName", "WasmResource");
                    candidate.SetMetadata("AssetTraitValue", "runtime");
                }
                if (string.Equals(candidate.GetMetadata("ResolvedFrom"), "{HintPathFromItem}", StringComparison.Ordinal))
                {
                    candidate.RemoveMetadata("OriginalItemSpec");
                }
                break;
            case ".wasm":
            case ".blat":
            case ".dat" when filename.StartsWith("icudt"):
                candidate.SetMetadata("AssetTraitName", "WasmResource");
                candidate.SetMetadata("AssetTraitValue", "native");
                break;
            case ".pdb":
                candidate.SetMetadata("AssetTraitName", "WasmResource");
                candidate.SetMetadata("AssetTraitValue", "symbol");
                candidate.RemoveMetadata("OriginalItemSpec");
                break;
            default:
                break;
        }
    }
}
