// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

using Internal.TypeSystem;
using Internal.TypeSystem.Interop;

namespace ILCompiler
{
    /// <summary>
    /// The P/Invokes to call directly, given as <c>--directpinvoke</c> entries and
    /// <c>--directpinvokelist</c> files. An entry names a whole module, or a single entry point as
    /// <c>module!entrypoint</c>.
    /// </summary>
    public sealed class DirectPInvokeSet
    {
        private readonly TargetDetails _target;

        // A null set means every entry point of the module is direct.
        private readonly Dictionary<string, HashSet<string>> _entries;

        public DirectPInvokeSet(TargetDetails target, IEnumerable<string> directPInvokes, IEnumerable<string> directPInvokeLists)
        {
            _target = target;
            _entries = new Dictionary<string, HashSet<string>>(target.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            foreach (string file in directPInvokeLists)
            {
                foreach (string entry in File.ReadLines(file))
                    Add(entry);
            }

            foreach (string entry in directPInvokes)
                Add(entry);
        }

        public void Add(string entry)
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

        /// <summary>
        /// Gets the direct entry points of the module named exactly <paramref name="module"/>, or null
        /// when all of them are direct.
        /// </summary>
        public bool TryGetEntryPoints(string module, out IReadOnlySet<string> entryPoints)
        {
            bool found = _entries.TryGetValue(module, out HashSet<string> set);
            entryPoints = set;
            return found;
        }

        /// <summary>Whether every entry point of <paramref name="module"/> is direct.</summary>
        public bool ContainsModule(string module)
        {
            foreach (string variation in MarshalHelpers.GetPInvokeModuleNameVariations(_target, module))
            {
                if (TryGetEntryPoints(variation, out IReadOnlySet<string> entryPoints) && entryPoints is null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether <paramref name="entryPoint"/> in <paramref name="module"/> is direct, without the
        /// Windows charset suffixes <c>A</c> and <c>W</c>.
        /// </summary>
        public bool Contains(string module, string entryPoint)
        {
            foreach (string variation in MarshalHelpers.GetPInvokeModuleNameVariations(_target, module))
            {
                if (TryGetEntryPoints(variation, out IReadOnlySet<string> entryPoints)
                    && (entryPoints is null || entryPoints.Contains(entryPoint)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
