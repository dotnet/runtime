// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;

namespace System.Runtime.Loader.Tests
{
    public enum LoadBy
    {
        Path,
        Stream
    }
    
    public class ResourceAssemblyLoadContext : AssemblyLoadContext
    {
        public LoadBy LoadBy { get; set; }

        public ResourceAssemblyLoadContext()
        {
            LoadBy = LoadBy.Path;
        }

        // A custom load context which only loads a given assembly if it is an embedded resource.
        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assembly = assemblyName.Name + ".dll";
            var currentAsm = typeof(ResourceAssemblyLoadContext).GetTypeInfo().Assembly;
            var asmStream = currentAsm.GetManifestResourceStream("System.Runtime.Loader.Tests." + assembly);

            if (asmStream == null)
            {
                return null;
            }

            if (LoadBy == LoadBy.Path)
            {
                // corerun blindly adds all the assemblies (including test assemblies) in the current directory to the TPA list.
                // Custom Load Contexts cannot be used to load an assembly in the TPA list.
                // Hence using this hack - where the user test assembly "System.Runtime.Loader.TestAssembly" is added as an embedded resource.
                // This custom load context will extract that resource and store it at the %temp% path at runtime.
                // This prevents the corerun from adding the test assembly to the TPA list.                
                // Once loaded it is not possible to unload the assembly, therefore it cannot be deleted.
                var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                // Create the folder since it will not exist already
                Directory.CreateDirectory(tempPath);
                
                string path = Path.Combine(tempPath, assembly);
                using (FileStream output = File.OpenWrite(path))
                {
                    asmStream.CopyTo(output);
                }

                return LoadFromAssemblyPath(path);
            }
            else if (LoadBy == LoadBy.Stream)
            {
                return LoadFromStream(asmStream);
            }

            return null;
        }
    }
}
