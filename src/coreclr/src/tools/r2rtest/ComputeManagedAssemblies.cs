// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

class ComputeManagedAssemblies
{
    public static IEnumerable<string> GetManagedAssembliesInFolder(string folder, string fileNamePattern = "*.*")
    {
        foreach (var file in Directory.GetFiles(folder, fileNamePattern, SearchOption.TopDirectoryOnly))
        {
            if (IsManaged(file))
            {
                yield return file;
            }
        }
    }

    /// <summary>
    /// Returns a list of assemblies in "folder" whose simple name does not exist in "simpleNames". Each discovered
    /// assembly is added to "simpleNames" to build a list without duplicates. When collecting multiple folders of assemblies
    /// consider the call order for this function so duplicates are ignored as expected. For exmaple, CORE_ROOT's S.P.CoreLib
    /// should normally win.
    /// </summary>
    /// <param name="simpleNames">Used to keep track of which simple names were used across multiple invocations of this method</param>
    public static IEnumerable<string> GetManagedAssembliesInFolderNoSimpleNameDuplicates(HashSet<string> simpleNames, string folder, string fileNamePattern = "*.*")
    {
        foreach (var file in GetManagedAssembliesInFolder(folder, fileNamePattern))
        {
            var simpleName = Path.GetFileNameWithoutExtension(file);
            if (!simpleNames.Contains(simpleName))
            {
                yield return file;
                simpleNames.Add(simpleName);
            }
        }
    }

    static ConcurrentDictionary<string, bool> _isManagedCache = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public static bool IsManaged(string file)
    {
        // Only files named *.dll and *.exe are considered as possible assemblies
        if (!Path.HasExtension(file) || (Path.GetExtension(file) != ".dll" && Path.GetExtension(file) != ".exe"))
            return false;

        bool isManaged;
        lock (_isManagedCache)
        {
            if (_isManagedCache.TryGetValue(file, out isManaged))
            {
                return isManaged;
            }
        }

        try
        {
            using (FileStream moduleStream = File.OpenRead(file))
            using (var module = new PEReader(moduleStream))
            {
                if (module.HasMetadata)
                {
                    MetadataReader moduleMetadataReader = module.GetMetadataReader();
                    if (moduleMetadataReader.IsAssembly)
                    {
                        string culture = moduleMetadataReader.GetString(moduleMetadataReader.GetAssemblyDefinition().Culture);

                        if (culture == "" || culture.Equals("neutral", StringComparison.OrdinalIgnoreCase))
                        {
                            isManaged = true;
                        }
                    }
                }
            }
        }
        catch (BadImageFormatException)
        {
            isManaged = false;
        }

        _isManagedCache[file] = isManaged;

        return isManaged;
    }
}
