// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;



namespace Build.Tasks
{
    public class ComputeManagedAssembliesToCompileToNative : DesktopCompatibleTask
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
        public ITaskItem[] AssembliesToSkipPublish
        {
            get;
            set;
        }

        public override bool Execute()
        {
            var list = new List<ITaskItem>();
            var assembliesToSkipPublish = new List<ITaskItem>();
            var nativeAotFrameworkAssembliesToUse = new HashSet<string>();

            foreach (ITaskItem taskItem in SdkAssemblies)
            {
                nativeAotFrameworkAssembliesToUse.Add(Path.GetFileName(taskItem.ItemSpec));
            }

            foreach (ITaskItem taskItem in FrameworkAssemblies)
            {
                nativeAotFrameworkAssembliesToUse.Add(Path.GetFileName(taskItem.ItemSpec));
            }

            foreach (ITaskItem taskItem in Assemblies)
            {
                // In the case of disk-based assemblies, this holds the file path
                string itemSpec = taskItem.ItemSpec;

                // Skip the native apphost (whose name ends up colliding with the native output binary) and supporting libraries
                if (itemSpec.EndsWith(DotNetAppHostExecutableName, StringComparison.OrdinalIgnoreCase) || itemSpec.Contains(DotNetHostFxrLibraryName) || itemSpec.Contains(DotNetHostPolicyLibraryName))
                {
                    assembliesToSkipPublish.Add(taskItem);
                    continue;
                }

                // Prototype aid - remove the native CoreCLR runtime pieces from the publish folder
                if (itemSpec.Contains("microsoft.netcore.app") && (itemSpec.Contains("\\native\\") || itemSpec.Contains("/native/")))
                {
                    assembliesToSkipPublish.Add(taskItem);
                    continue;
                }

                var assemblyFileName = Path.GetFileName(itemSpec);

                if (assemblyFileName == "WindowsBase.dll")
                {
                    // There are two instances of WindowsBase.dll, one small one, in the NativeAOT framework
                    // and real one in WindowsDesktop SDK. We want to make sure that if both are present,
                    // we will use the one from WindowsDesktop SDK, and not from NativeAOT framework.
                    foreach (ITaskItem taskItemToSkip in FrameworkAssemblies)
                    {
                        if (Path.GetFileName(taskItemToSkip.ItemSpec) == assemblyFileName)
                        {
                            assembliesToSkipPublish.Add(taskItemToSkip);
                            break;
                        }
                    }

                    assembliesToSkipPublish.Add(taskItem);
                    list.Add(taskItem);
                    continue;
                }

                // Remove any assemblies whose implementation we want to come from NativeAOT's package.
                // Currently that's System.Private.* SDK assemblies and a bunch of framework assemblies.
                if (nativeAotFrameworkAssembliesToUse.Contains(assemblyFileName))
                {
                    assembliesToSkipPublish.Add(taskItem);
                    continue;
                }

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
                                if (culture == "" || culture.Equals("neutral", StringComparison.OrdinalIgnoreCase))
                                {
                                    // NativeAOT doesn't consume resource assemblies yet so skip them
                                    list.Add(taskItem);
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

            return true;
        }
    }
}
