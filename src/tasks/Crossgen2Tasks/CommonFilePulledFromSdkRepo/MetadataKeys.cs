// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.Build.Tasks
{
    internal static class MetadataKeys
    {
        // General Metadata
        public const string Name = "Name";
        public const string Type = "Type";
        public const string Version = "Version";
        public const string FileGroup = "FileGroup";
        public const string Path = "Path";
        public const string ResolvedPath = "ResolvedPath";
        public const string IsImplicitlyDefined = "IsImplicitlyDefined";
        public const string IsTopLevelDependency = "IsTopLevelDependency";
        public const string AllowExplicitVersion = "AllowExplicitVersion";
        public const string RelativePath = "RelativePath";
        public const string DiagnosticLevel = "DiagnosticLevel";

        // Target Metadata
        public const string RuntimeIdentifier = "RuntimeIdentifier";
        public const string TargetFrameworkMoniker = "TargetFrameworkMoniker";
        public const string TargetFramework = "TargetFramework";
        public const string FrameworkName = "FrameworkName";
        public const string FrameworkVersion = "FrameworkVersion";
        public const string IsTrimmable = "IsTrimmable";
        public const string RuntimeFrameworkName = "RuntimeFrameworkName";
        public const string RuntimePackRuntimeIdentifiers = "RuntimePackRuntimeIdentifiers";

        // SDK Metadata
        public const string SDKPackageItemSpec = "SDKPackageItemSpec";
        public const string OriginalItemSpec = "OriginalItemSpec";
        public const string SDKRootFolder = "SDKRootFolder";
        public const string ShimRuntimeIdentifier = "ShimRuntimeIdentifier";
        public const string RuntimePackAlwaysCopyLocal = "RuntimePackAlwaysCopyLocal";

        // Foreign Keys
        public const string ParentTarget = "ParentTarget";
        public const string ParentTargetLibrary = "ParentTargetLibrary";
        public const string ParentPackage = "ParentPackage";

        // Tags
        public const string Analyzer = "Analyzer";
        public const string AnalyzerLanguage = "AnalyzerLanguage";
        public const string TransitiveProjectReference = "TransitiveProjectReference";

        // Diagnostics
        public const string DiagnosticCode = "DiagnosticCode";
        public const string Message = "Message";
        public const string FilePath = "FilePath";
        public const string Severity = "Severity";
        public const string StartLine = "StartLine";
        public const string StartColumn = "StartColumn";
        public const string EndLine = "EndLine";
        public const string EndColumn = "EndColumn";

        // Publish Target Manifest
        public const string RuntimeStoreManifestNames = "RuntimeStoreManifestNames";

        // Conflict Resolution
        public const string OverriddenPackages = "OverriddenPackages";

        // Package assets
        public const string NuGetIsFrameworkReference = "NuGetIsFrameworkReference";
        public const string NuGetPackageId = "NuGetPackageId";
        public const string NuGetPackageVersion = "NuGetPackageVersion";
        public const string NuGetSourceType = "NuGetSourceType";
        public const string PathInPackage = "PathInPackage";
        public const string PackageDirectory = "PackageDirectory";
        public const string Publish = "Publish";

        // References
        public const string ExternallyResolved = "ExternallyResolved";
        public const string HintPath = "HintPath";
        public const string MSBuildSourceProjectFile = "MSBuildSourceProjectFile";
        public const string Private = "Private";
        public const string Pack = "Pack";
        public const string ReferenceSourceTarget = "ReferenceSourceTarget";
        public const string TargetPath = "TargetPath";
        public const string CopyLocal = "CopyLocal";

        // Targeting packs
        public const string PackageConflictPreferredPackages = "PackageConflictPreferredPackages";

        // Runtime packs
        public const string DropFromSingleFile = "DropFromSingleFile";
        public const string RuntimePackLabels = "RuntimePackLabels";
        public const string AdditionalFrameworkReferences = "AdditionalFrameworkReferences";

        // Content files
        public const string PPOutputPath = "PPOutputPath";
        public const string CodeLanguage = "CodeLanguage";
        public const string CopyToOutput = "CopyToOutput";
        public const string BuildAction = "BuildAction";
        public const string OutputPath = "OutputPath";
        public const string CopyToPublishDirectory = "CopyToPublishDirectory";
        public const string ExcludeFromSingleFile = "ExcludeFromSingleFile";

        // Resource assemblies
        public const string Culture = "Culture";
        // The DestinationSubDirectory is the directory containing the asset, relative to the destination folder.
        public const string DestinationSubDirectory = "DestinationSubDirectory";

        // Copy local assets
        // The DestinationSubPath is the path to the asset, relative to the destination folder.
        public const string DestinationSubPath = "DestinationSubPath";
        public const string AssetType = "AssetType";

        public const string ReferenceOnly = "ReferenceOnly";

        public const string Aliases = "Aliases";

        // ReadyToRun
        public const string DotNetHostPath = "DotNetHostPath";
        public const string JitPath = "JitPath";
        public const string TargetOS = "TargetOS";
        public const string TargetArch = "TargetArch";
        public const string DiaSymReader = "DiaSymReader";
        public const string CreatePDBCommand = "CreatePDBCommand";
        public const string OutputR2RImage = "OutputR2RImage";
        public const string OutputPDBImage = "OutputPDBImage";
        public const string EmitSymbols = "EmitSymbols";
        public const string IsVersion5 = "IsVersion5";
        public const string CreateCompositeImage = "CreateCompositeImage";
    }
}
