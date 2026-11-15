// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Represents a policy that decides which assemblies have the names of their types stripped
    /// from the reflection metadata and the stack trace data emitted into the executable.
    /// </summary>
    public sealed class MetadataNameStrippingPolicy
    {
        private readonly HashSet<string> _strippedAssemblies;

        /// <summary>
        /// Gets a policy that doesn't strip the names of any assemblies.
        /// </summary>
        public static MetadataNameStrippingPolicy None { get; } = new MetadataNameStrippingPolicy(Array.Empty<string>());

        /// <param name="strippedAssemblies">Simple names of the assemblies to strip the names from.</param>
        public MetadataNameStrippingPolicy(IEnumerable<string> strippedAssemblies)
        {
            _strippedAssemblies = new HashSet<string>(strippedAssemblies, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if the names of the types defined in <paramref name="module"/> are stripped.
        /// </summary>
        public bool IsStripped(ModuleDesc module)
        {
            return _strippedAssemblies.Count > 0
                && module.Assembly is IAssemblyDesc assembly
                && _strippedAssemblies.Contains(assembly.GetName().Name);
        }
    }
}
