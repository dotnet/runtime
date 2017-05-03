// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class RuntimeReference
    {
        public static List<RuntimeLibrary> RemoveReferences(IReadOnlyList<RuntimeLibrary> runtimeLibraries, IEnumerable<string> packages)
        {
            List<RuntimeLibrary> result = new List<RuntimeLibrary>();

            foreach (var runtimeLib in runtimeLibraries)
            {
                if (string.IsNullOrEmpty(packages.FirstOrDefault(elem => runtimeLib.Name.Equals(elem, StringComparison.OrdinalIgnoreCase))))
                {
                    List<Dependency> toRemoveDependecy = new List<Dependency>();
                    foreach (var dependency in runtimeLib.Dependencies)
                    {
                        if (!string.IsNullOrEmpty(packages.FirstOrDefault(elem => dependency.Name.Equals(elem, StringComparison.OrdinalIgnoreCase))))
                        {
                            toRemoveDependecy.Add(dependency);
                        }
                    }

                    if (toRemoveDependecy.Count > 0)
                    {
                        List<Dependency> modifiedDependencies = new List<Dependency>();
                        foreach (var dependency in runtimeLib.Dependencies)
                        {
                            if (!toRemoveDependecy.Contains(dependency))
                            {
                                modifiedDependencies.Add(dependency);
                            }
                        }


                        result.Add(new RuntimeLibrary(runtimeLib.Type,
                                                      runtimeLib.Name,
                                                      runtimeLib.Version,
                                                      runtimeLib.Hash,
                                                      runtimeLib.RuntimeAssemblyGroups,
                                                      runtimeLib.NativeLibraryGroups,
                                                      runtimeLib.ResourceAssemblies,
                                                      modifiedDependencies,
                                                      runtimeLib.Serviceable));

                    }
                    else if (string.IsNullOrEmpty(packages.FirstOrDefault(elem => runtimeLib.Name.Equals(elem, StringComparison.OrdinalIgnoreCase))))
                    {
                        result.Add(runtimeLib);
                    }
                }
            }
            return result;
        }
    }
}
