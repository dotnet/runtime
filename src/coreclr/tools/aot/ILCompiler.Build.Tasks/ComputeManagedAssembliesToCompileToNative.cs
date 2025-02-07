// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;



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

        [Output]
        public ITaskItem[] ManagedAssemblies
        {
            get;
            set;
        }

        [Output]
        public ITaskItem[] SatelliteAssemblies
        {
            get;
            set;
        }

        [Output]
        public ITaskItem[] AssembliesToSkipPublish
        {
            get;
            set;
        }

        public override bool Execute()
        {
            var list = new List<ITaskItem>();
            var assembliesToSkipPublish = new List<ITaskItem>();
            var satelliteAssemblies = new List<ITaskItem>();
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
                    assembliesToSkipPublish.Add(taskItem);
                    continue;
                }

                if (isFromRuntimePack && taskItem.GetMetadata("AssetType")?.Equals("native", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Skip the native components of the runtime pack, we don't need them for NativeAOT.
                    assembliesToSkipPublish.Add(taskItem);
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
                        // Use that version instead of swapping to the NativeAOT one.
                        list.Add(taskItem);
                    }
                    else if (assemblyFileName == "System.Private.CoreLib.dll" && GetFileVersion(itemSpec).CompareTo(GetFileVersion(frameworkItem.ItemSpec)) > 0)
                    {
                        // Validate that we aren't trying to use an older NativeAOT package against a newer non-NativeAOT runtime pack.
                        // That's not supported.
                        Log.LogError($"Overriding System.Private.CoreLib.dll with a newer version is not supported. Attempted to use {itemSpec} instead of {frameworkItem.ItemSpec}.");
                    }

                    assembliesToSkipPublish.Add(taskItem);
                    continue;
                }

                // For all other files, check if they are managed assemblies.
                // If they're managed, skip publishing them and categorize them correctly as inputs to ILC.
                // If they're not managed assemblies, then they're native dependencies. Allow them to be published.
                try
                {
                    using (FileStream moduleStream = File.OpenRead(itemSpec))
                    using (var module = new PEReader(moduleStream))
                    {
                        if (module.HasMetadata)
                        {
                            MetadataReader moduleMetadataReader = module.GetMetadataReader();
                            if (moduleMetadataReader.IsAssembly)
                            {
                                string culture = moduleMetadataReader.GetString(moduleMetadataReader.GetAssemblyDefinition().Culture);

                                assembliesToSkipPublish.Add(taskItem);

                                // Split satellite assemblies from normal assemblies
                                if (culture == "" || culture.Equals("neutral", StringComparison.OrdinalIgnoreCase))
                                {
                                    list.Add(taskItem);
                                }
                                else
                                {
                                    satelliteAssemblies.Add(taskItem);
                                }
                            }
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                }
            }

            ManagedAssemblies = list.ToArray();
            AssembliesToSkipPublish = assembliesToSkipPublish.ToArray();
            SatelliteAssemblies = satelliteAssemblies.ToArray();

            return true;

            static Version GetFileVersion(string path)
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(path);
                return new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.FilePrivatePart);
            }
        }
    }
}
