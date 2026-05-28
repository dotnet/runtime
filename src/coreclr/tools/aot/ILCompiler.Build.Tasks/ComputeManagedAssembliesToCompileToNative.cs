// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;



namespace Build.Tasks
{
    public class ComputeManagedAssembliesToCompileToNative : Task
    {
        [Required]
        public ITaskItem[] Assemblies
        {
            get;
            set;
        }

        /// <summary>
        /// The NativeAOT-specific System.Private.* assemblies that must be used instead of the default CoreCLR versions.
        /// </summary>
        [Required]
        public ITaskItem[] SdkAssemblies
        {
            get;
            set;
        }

        /// <summary>
        /// The set of NativeAOT-specific framework assemblies we currently need to use which will replace the same-named ones
        /// in the app's closure.
        /// </summary>
        [Required]
        public ITaskItem[] FrameworkAssemblies
        {
            get;
            set;
        }

        /// <summary>
        /// The native apphost (whose name ends up colliding with the native output binary)
        /// </summary>
        [Required]
        public string DotNetAppHostExecutableName
        {
            get;
            set;
        }

        /// <summary>
        /// The CoreCLR dotnet host fixer library that can be skipped during publish
        /// </summary>
        [Required]
        public string DotNetHostFxrLibraryName
        {
            get;
            set;
        }

        /// <summary>
        /// The CoreCLR dotnet host policy library that can be skipped during publish
        /// </summary>
        [Required]
        public string DotNetHostPolicyLibraryName
        {
            get;
            set;
        }

        /// <summary>
        /// CoreCLR runtime pack files (apphost, native assets, managed assemblies replaced by NativeAOT equivalents)
        /// that should be removed from the publish output and replaced with NativeAOT runtime pack assemblies.
        /// </summary>
        [Output]
        public ITaskItem[] RuntimePackFilesToSkipPublish
        {
            get;
            set;
        }

        public override bool Execute()
        {
            var runtimePackFilesToSkipPublish = new List<ITaskItem>();
            var nativeAotFrameworkAssembliesToUse = new Dictionary<string, ITaskItem>();

            foreach (ITaskItem taskItem in SdkAssemblies)
            {
                var fileName = Path.GetFileName(taskItem.ItemSpec);
                if (!nativeAotFrameworkAssembliesToUse.ContainsKey(fileName))
                    nativeAotFrameworkAssembliesToUse.Add(fileName, taskItem);
            }

            foreach (ITaskItem taskItem in FrameworkAssemblies)
            {
                var fileName = Path.GetFileName(taskItem.ItemSpec);
                if (!nativeAotFrameworkAssembliesToUse.ContainsKey(fileName))
                    nativeAotFrameworkAssembliesToUse.Add(fileName, taskItem);
            }

            foreach (ITaskItem taskItem in Assemblies)
            {
                // In the case of disk-based assemblies, this holds the file path
                string itemSpec = taskItem.ItemSpec;
                string assemblyFileName = Path.GetFileName(itemSpec);
                bool isFromRuntimePack = taskItem.GetMetadata("NuGetPackageId")?.StartsWith("Microsoft.NETCore.App.Runtime.", StringComparison.OrdinalIgnoreCase) == true;

                // Skip the native apphost (whose name ends up colliding with the native output binary) and supporting libraries
                if (itemSpec.EndsWith(DotNetAppHostExecutableName, StringComparison.OrdinalIgnoreCase) || itemSpec.Contains(DotNetHostFxrLibraryName) || itemSpec.Contains(DotNetHostPolicyLibraryName))
                {
                    runtimePackFilesToSkipPublish.Add(taskItem);
                    continue;
                }

                if (isFromRuntimePack && taskItem.GetMetadata("AssetType")?.Equals("native", StringComparison.OrdinalIgnoreCase) == true
                    && !assemblyFileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)
                    && !assemblyFileName.EndsWith(".dex", StringComparison.OrdinalIgnoreCase))
                {
                    // Skip the native components of the runtime pack, we don't need them for NativeAOT.
                    runtimePackFilesToSkipPublish.Add(taskItem);
                    continue;
                }

                // Remove any assemblies whose implementation we want to come from NativeAOT's package.
                // Currently that's System.Private.* SDK assemblies and a bunch of framework assemblies.
                if (nativeAotFrameworkAssembliesToUse.TryGetValue(assemblyFileName, out ITaskItem frameworkItem))
                {
                    // If the assembly is part of the Microsoft.NETCore.App.Runtime runtime pack, we want to swap it with the corresponding package from the NativeAOT SDK.
                    // Otherwise we want to use the assembly the user has referenced.
                    if (!isFromRuntimePack)
                    {
                        // The assembly was overridden by an OOB package through standard .NET SDK conflict resolution.
                        // Don't swap to the NativeAOT version; the user's version stays in
                        // ResolvedFileToPublish and will be picked up as an ILC input via
                        // its PostprocessAssembly=true metadata.
                        continue;
                    }
                    else if (assemblyFileName == "System.Private.CoreLib.dll" && GetFileVersion(itemSpec).CompareTo(GetFileVersion(frameworkItem.ItemSpec)) > 0)
                    {
                        // Validate that we aren't trying to use an older NativeAOT package against a newer non-NativeAOT runtime pack.
                        // That's not supported.
                        Log.LogError($"Overriding System.Private.CoreLib.dll with a newer version is not supported. Attempted to use {itemSpec} instead of {frameworkItem.ItemSpec}.");
                    }

                    runtimePackFilesToSkipPublish.Add(taskItem);
                    continue;
                }
            }

            RuntimePackFilesToSkipPublish = runtimePackFilesToSkipPublish.ToArray();

            return true;

            static Version GetFileVersion(string path)
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                return new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
            }
        }
    }
}
