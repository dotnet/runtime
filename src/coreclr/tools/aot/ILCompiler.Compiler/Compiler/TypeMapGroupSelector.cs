// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// Selects type map groups (the type argument of TypeMapAttribute&lt;T&gt; and TypeMapAssociationAttribute&lt;T&gt;)
    /// by name. A selector is either <see cref="AllGroups"/>, a type name in the ECMA-335 SerString format
    /// ('Namespace.Outer+Nested', optionally followed by ',AssemblyName'), or both.
    /// Selectors without an assembly name match groups in any assembly with that full name.
    /// </summary>
    public sealed class TypeMapGroupSelector
    {
        public const string AllGroups = "*";

        private readonly string[] _selectors;
        private readonly TypeDesc[] _resolvedGroups;

        public TypeMapGroupSelector(TypeSystemContext context, IEnumerable<string> selectors)
        {
            List<string> names = [];
            List<TypeDesc> resolved = [];
            foreach (string selector in selectors)
            {
                string name = selector.Trim();
                if (name.Length == 0)
                    continue;

                TypeDesc type = null;
                if (name != AllGroups)
                {
                    try
                    {
                        // Resolves assembly qualified names, and unqualified names in the system module.
                        type = context.SystemModule.GetTypeByCustomAttributeTypeName(name, throwIfNotFound: false);
                    }
                    catch (TypeSystemException)
                    {
                    }
                }

                names.Add(name);
                resolved.Add(type);
            }

            _selectors = [.. names];
            _resolvedGroups = [.. resolved];
        }

        public IReadOnlyList<string> Selectors => _selectors;

        /// <summary>
        /// Returns the indices of the selectors (see <see cref="Selectors"/>) that select the group.
        /// </summary>
        public IEnumerable<int> GetMatchingSelectors(TypeDesc group)
        {
            string unqualifiedName = null;
            bool unqualifiedNameComputed = false;

            for (int i = 0; i < _selectors.Length; i++)
            {
                if (_selectors[i] == AllGroups || _resolvedGroups[i] == group)
                {
                    yield return i;
                }
                else
                {
                    // Unqualified names resolve in the system module only. Fall back to comparing the name for groups in other assemblies.
                    if (!unqualifiedNameComputed)
                    {
                        unqualifiedNameComputed = true;
                        try
                        {
                            unqualifiedName = new CustomAttributeTypeNameFormatter().FormatName(group, false);
                        }
                        catch (NotSupportedException)
                        {
                            // Group types that can't be named (function pointers, generic parameters) can't be selected by name.
                        }
                    }

                    if (unqualifiedName == _selectors[i])
                        yield return i;
                }
            }
        }
    }
}
