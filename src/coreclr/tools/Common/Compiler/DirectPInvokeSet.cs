// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// The P/Invokes to call directly, given as <c>--directpinvoke</c> entries and
    /// <c>--directpinvokelist</c> files. An entry names a whole module, or a single entry point as
    /// <c>module!entrypoint</c>. Module <c>*</c> is always direct.
    /// </summary>
    public sealed class DirectPInvokeSet
    {
        private readonly TargetDetails _target;

        // A null set means every entry point of the module is direct.
        private readonly Dictionary<string, HashSet<string>> _entries;

        public DirectPInvokeSet(TargetDetails target, IEnumerable<string> directPInvokes, IEnumerable<string> directPInvokeLists)
        {
            _target = target;
            _entries = new Dictionary<string, HashSet<string>>(target.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            {
                { "*", null },
            };

            foreach (string file in directPInvokeLists)
            {
                foreach (string entry in File.ReadLines(file))
                    Add(entry);
            }

            foreach (string entry in directPInvokes)
                Add(entry);
        }

        private void Add(string entry)
        {
            if (entry.StartsWith('#'))
                return;

            entry = entry.Trim();
            if (entry.Length == 0)
                return;

            int separator = entry.IndexOf('!');
            if (separator == -1)
            {
                _entries[entry] = null;
                return;
            }

            string module = entry.Substring(0, separator);
            if (!_entries.TryGetValue(module, out HashSet<string> entryPoints))
                _entries.Add(module, entryPoints = new HashSet<string>());

            entryPoints?.Add(entry.Substring(separator + 1));
        }

        /// <summary>Whether every entry point of <paramref name="module"/> is direct.</summary>
        public bool ContainsModule(string module)
        {
            foreach (string variation in ModuleNameVariations(module))
            {
                if (_entries.TryGetValue(variation, out HashSet<string> entryPoints) && entryPoints is null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the native symbol a direct call to the P/Invoke <paramref name="method"/> binds to.
        /// Returns false when the P/Invoke is not direct.
        /// </summary>
        public bool TryGetDirectEntryPoint(MethodDesc method, out string entryPoint)
        {
            PInvokeMetadata metadata = method.GetPInvokeMethodMetadata();
            string entryPointMetadataName = metadata.Name ?? method.GetName();

            foreach (string module in ModuleNameVariations(metadata.Module))
            {
                if (!_entries.TryGetValue(module, out HashSet<string> entryPoints))
                    continue;

                if (entryPoints is null)
                {
                    entryPoint = entryPointMetadataName;
                    return true;
                }

                foreach (string variation in EntryPointNameVariations(entryPointMetadataName, metadata.Flags))
                {
                    if (entryPoints.Contains(variation))
                    {
                        entryPoint = variation;
                        return true;
                    }
                }
            }

            entryPoint = null;
            return false;
        }

        private IEnumerable<string> ModuleNameVariations(string name)
        {
            yield return name;

            if (_target.IsWindows)
            {
                const string Suffix = ".dll";

                if (name.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase))
                    yield return name.Substring(0, name.Length - Suffix.Length);
            }
            else
            {
                string suffix = _target.IsApplePlatform ? ".dylib" : ".so";
                bool hasSharedLibraryExtension = name.EndsWith(suffix, StringComparison.Ordinal);
                const string LibPrefix = "lib";
                bool hasLibPrefix = name.StartsWith(LibPrefix, StringComparison.Ordinal);

                if (hasSharedLibraryExtension)
                    yield return name.Substring(0, name.Length - suffix.Length);
                if (hasLibPrefix)
                    yield return name.Substring(LibPrefix.Length);
                if (hasLibPrefix && hasSharedLibraryExtension)
                    yield return name.Substring(LibPrefix.Length, name.Length - suffix.Length - LibPrefix.Length);
            }
        }

        private IEnumerable<string> EntryPointNameVariations(string name, PInvokeFlags flags)
        {
            if (_target.IsWindows && !flags.ExactSpelling)
            {
                // Mirror CharSet normalization from Marshaller.CreateMarshaller
                bool isAnsi = flags.CharSet switch
                {
                    CharSet.Ansi => true,
                    CharSet.Unicode => false,
                    CharSet.Auto => false,
                    _ => true
                };

                if (isAnsi)
                {
                    // For ANSI, look for the user-provided entry point name first.
                    // If that does not exist, try the charset suffix.
                    yield return name;
                    yield return name + "A";
                }
                else
                {
                    // For Unicode, look for the entry point name with the charset suffix first.
                    // The 'W' API takes precedence over the undecorated one.
                    yield return name + "W";
                    yield return name;
                }
            }
            else
            {
                yield return name;
            }
        }
    }
}
