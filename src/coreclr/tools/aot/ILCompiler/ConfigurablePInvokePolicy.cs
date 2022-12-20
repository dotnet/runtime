// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using Internal.IL;
using Internal.TypeSystem;

namespace ILCompiler
{
    internal sealed class ConfigurablePInvokePolicy : PInvokeILEmitterConfiguration
    {
        private readonly TargetDetails _target;
        private readonly Dictionary<string, HashSet<string>> _directPInvokes;

        public ConfigurablePInvokePolicy(TargetDetails target, IReadOnlyList<string> directPInvokes, IReadOnlyList<string> directPInvokeLists)
        {
            _directPInvokes = new Dictionary<string, HashSet<string>>(target.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            // * is always a direct call
            _directPInvokes.Add("*", null);

            foreach (var file in directPInvokeLists)
            {
                foreach (var entry in File.ReadLines(file))
                {
                    AddDirectPInvoke(entry);
                }
            }

            foreach (var entry in directPInvokes)
            {
                AddDirectPInvoke(entry);
            }

            _target = target;
        }

        private void AddDirectPInvoke(string entry)
        {
            // Ignore comments
            if (entry.StartsWith('#'))
                return;

            entry = entry.Trim();

            // Ignore empty entries
            if (string.IsNullOrEmpty(entry))
                return;

            int separator = entry.IndexOf('!');

            if (separator != -1)
            {
                string libraryName = entry.Substring(0, separator);
                string entrypointName = entry.Substring(separator + 1);

                if (_directPInvokes.TryGetValue(libraryName, out HashSet<string> entrypointSet))
                {
                    // All entrypoints from the library are direct
                    if (entrypointSet == null)
                        return;
                }
                else
                {
                    _directPInvokes.Add(libraryName, entrypointSet = new HashSet<string>());
                }

                entrypointSet.Add(entrypointName);
            }
            else
            {
                // All entrypoints from the library are direct
                _directPInvokes[entry] = null;
            }
        }

        private IEnumerable<string> ModuleNameVariations(string name)
        {
            yield return name;

            if (_target.IsWindows)
            {
                string suffix = ".dll";

                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    yield return name.Substring(0, name.Length - suffix.Length);
            }
            else
            {
                string suffix = _target.IsOSX ? ".dylib" : ".so";

                if (name.EndsWith(suffix, StringComparison.Ordinal))
                    yield return name.Substring(0, name.Length - suffix.Length);
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

        public override bool GenerateDirectCall(MethodDesc method, out string externName)
        {
            var pInvokeMetadata = method.GetPInvokeMethodMetadata();

            foreach (var moduleName in ModuleNameVariations(pInvokeMetadata.Module))
            {
                if (_directPInvokes.TryGetValue(moduleName, out HashSet<string> entrypoints))
                {
                    string entryPointMetadataName = pInvokeMetadata.Name ?? method.Name;

                    if (entrypoints == null)
                    {
                        externName = entryPointMetadataName;
                        return true;
                    }

                    foreach (var entryPointName in EntryPointNameVariations(entryPointMetadataName, pInvokeMetadata.Flags))
                    {
                        if (entrypoints.Contains(entryPointName))
                        {
                            externName = entryPointName;
                            return true;
                        }
                    }
                }
            }

            externName = null;
            return false;
        }
    }
}
