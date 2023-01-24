// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Microsoft.Build.Utilities;

namespace Build.Tasks
{
    public abstract class DesktopCompatibleTask : Task
    {
        static DesktopCompatibleTask()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // apply any existing policy
            AssemblyName referenceName = new AssemblyName(AppDomain.CurrentDomain.ApplyPolicy(args.Name));

            string fileName = referenceName.Name + ".dll";
            string assemblyPath;
            string probingPath;
            Assembly assm;

            // look next to requesting assembly
            assemblyPath = args.RequestingAssembly?.Location;
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                probingPath = Path.Combine(Path.GetDirectoryName(assemblyPath), fileName);
                Debug.WriteLine($"Considering {probingPath} based on RequestingAssembly");
                if (Probe(probingPath, referenceName.Version, out assm))
                {
                    return assm;
                }
            }

            // look next to the executing assembly
            assemblyPath = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(assemblyPath))
            {
                probingPath = Path.Combine(Path.GetDirectoryName(assemblyPath), fileName);

                Debug.WriteLine($"Considering {probingPath} based on ExecutingAssembly");
                if (Probe(probingPath, referenceName.Version, out assm))
                {
                    return assm;
                }
            }

            // look in AppDomain base directory
            probingPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            Debug.WriteLine($"Considering {probingPath} based on BaseDirectory");
            if (Probe(probingPath, referenceName.Version, out assm))
            {
                return assm;
            }

            // look in current directory
            Debug.WriteLine($"Considering {fileName}");
            if (Probe(fileName, referenceName.Version, out assm))
            {
                return assm;
            }

            return null;
        }

        /// <summary>
        /// Considers a path to load for satisfying an assembly ref and loads it
        /// if the file exists and version is sufficient.
        /// </summary>
        /// <param name="filePath">Path to consider for load</param>
        /// <param name="minimumVersion">Minimum version to consider</param>
        /// <param name="assembly">loaded assembly</param>
        /// <returns>true if assembly was loaded</returns>
        private static bool Probe(string filePath, Version minimumVersion, out Assembly assembly)
        {
            if (File.Exists(filePath))
            {
                AssemblyName name = AssemblyName.GetAssemblyName(filePath);

                if (name.Version >= minimumVersion)
                {
                    assembly = Assembly.Load(name);
                    return true;
                }
            }

            assembly = null;
            return false;
        }
    }
}
