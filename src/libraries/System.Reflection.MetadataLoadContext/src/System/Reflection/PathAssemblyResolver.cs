// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace System.Reflection
{
    /// <summary>
    /// An assembly resolver that uses paths to every assembly that may be loaded.
    /// The file name is expected to be the same as the assembly's simple name.
    /// Multiple assemblies can exist on disk with the same name but in different directories.
    /// A single instance of PathAssemblyResolver can be used with multiple MetadataAssemblyResolver instances.
    /// </summary>
    /// <remarks>
    /// In order for an AssemblyName to match to a loaded assembly, AssemblyName.Name must be equal (casing ignored).
    /// - If AssemblyName.PublicKeyToken is specified, it must be equal.
    /// - If AssemblyName.PublicKeyToken is not specified, assemblies with no PublicKeyToken are selected over those with a PublicKeyToken.
    /// - If more than one assembly matches, the assembly with the highest Version is returned.
    /// - CultureName is ignored.
    /// </remarks>
    public class PathAssemblyResolver : MetadataAssemblyResolver
    {
        private readonly Dictionary<string, List<string>> _fileToPaths = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="System.Reflection.PathAssemblyResolver"/> class.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">Thrown when assemblyPaths is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown when a path is invalid.</exception>
        public PathAssemblyResolver(IEnumerable<string> assemblyPaths)
        {
            if (assemblyPaths is null)
                throw new ArgumentNullException(nameof(assemblyPaths));

            foreach (string path in assemblyPaths)
            {
                if (string.IsNullOrEmpty(path))
                    throw new ArgumentException(SR.Format(SR.Arg_InvalidPath, path), nameof(assemblyPaths));

                string file = Path.GetFileNameWithoutExtension(path);
                if (file.Length == 0)
                    throw new ArgumentException(SR.Format(SR.Arg_InvalidPath, path), nameof(assemblyPaths));

                List<string>? paths;
                if (!_fileToPaths.TryGetValue(file, out paths))
                {
                    _fileToPaths.Add(file, paths = new List<string>());
                }
                paths.Add(path);
            }
        }

        public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
        {
            Debug.Assert(assemblyName.Name != null);
            Assembly? candidateWithSamePkt = null;
            Assembly? candidateIgnoringPkt = null;
            if (_fileToPaths.TryGetValue(assemblyName.Name, out List<string>? paths))
            {
                ReadOnlySpan<byte> pktFromName = assemblyName.GetPublicKeyToken();

                foreach (string path in paths)
                {
                    Assembly assemblyFromPath = context.LoadFromAssemblyPath(path);
                    AssemblyName assemblyNameFromPath = assemblyFromPath.GetName();
                    if (assemblyName.Name.Equals(assemblyNameFromPath.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        ReadOnlySpan<byte> pktFromAssembly = assemblyNameFromPath.GetPublicKeyToken();

                        // Find exact match on PublicKeyToken including treating no PublicKeyToken as its own entry.
                        if (pktFromName.SequenceEqual(pktFromAssembly))
                        {
                            // Pick the highest version.
                            if (candidateWithSamePkt == null || assemblyNameFromPath.Version > candidateWithSamePkt.GetName().Version)
                            {
                                candidateWithSamePkt = assemblyFromPath;
                            }
                        }
                        // If assemblyName does not specify a PublicKeyToken, or assemblyName is marked 'Retargetable',
                        // then still consider those with a PublicKeyToken and take the highest version available.
                        else if ((candidateWithSamePkt == null && pktFromName.IsEmpty) ||
                            ((assemblyName.Flags & AssemblyNameFlags.Retargetable) != 0))
                        {
                            // Pick the highest version.
                            if (candidateIgnoringPkt == null || assemblyNameFromPath.Version > candidateIgnoringPkt.GetName().Version)
                            {
                                candidateIgnoringPkt = assemblyFromPath;
                            }
                        }
                    }
                }
            }

            return candidateWithSamePkt ?? candidateIgnoringPkt;
        }
    }
}
