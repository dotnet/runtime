// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.TypeSystem.Interop;

namespace ILCompiler
{
    /// <summary>
    /// The P/Invokes that resolve to statically linked native code, given as <c>--directpinvoke</c>
    /// entries. An entry names either a whole module or a single entry point as
    /// <c>module!entrypoint</c>, matching the NativeAOT <c>DirectPInvoke</c> item.
    /// </summary>
    /// <remarks>
    /// The per-entry-point form is what lets an app bind a few functions from a library that is
    /// linked implicitly, such as libc, without claiming the rest of it: a module-wide entry would
    /// also bind functions the library doesn't provide and fail the native link.
    /// </remarks>
    public sealed class DirectPInvokeSet
    {
        // A null set means every entry point of the module is direct.
        private readonly Dictionary<string, HashSet<string>> _entries = new(StringComparer.Ordinal);

        public DirectPInvokeSet(IEnumerable<string> entries)
        {
            foreach (string entry in entries)
                Add(entry);
        }

        private void Add(string entry)
        {
            entry = entry.Trim();
            if (entry.Length == 0 || entry.StartsWith('#'))
                return;

            int separator = entry.IndexOf('!');
            if (separator == -1)
            {
                _entries[entry] = null;
                return;
            }

            string module = entry.Substring(0, separator);
            string entryPoint = entry.Substring(separator + 1);

            if (!_entries.TryGetValue(module, out HashSet<string> entryPoints))
                _entries.Add(module, entryPoints = new HashSet<string>(StringComparer.Ordinal));

            entryPoints?.Add(entryPoint);
        }

        /// <summary>Whether every entry point of <paramref name="module"/> is direct.</summary>
        public bool ContainsModule(TargetDetails target, string module)
        {
            foreach (string variation in MarshalHelpers.GetPInvokeModuleNameVariations(target, module))
            {
                if (_entries.TryGetValue(variation, out HashSet<string> entryPoints) && entryPoints is null)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="entryPoint"/> in <paramref name="module"/> is direct.</summary>
        public bool Contains(TargetDetails target, string module, string entryPoint)
        {
            foreach (string variation in MarshalHelpers.GetPInvokeModuleNameVariations(target, module))
            {
                if (_entries.TryGetValue(variation, out HashSet<string> entryPoints)
                    && (entryPoints is null || entryPoints.Contains(entryPoint)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
