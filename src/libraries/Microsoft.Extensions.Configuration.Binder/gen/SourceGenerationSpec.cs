// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{

    internal sealed record SourceGenerationSpec(
        Dictionary<MethodSpecifier, HashSet<TypeSpec>> Methods,
        HashSet<string> Namespaces)
    {
        private MethodSpecifier? _methodsToGen;
        public MethodSpecifier MethodsToGen
        {
            get
            {
                if (!_methodsToGen.HasValue)
                {
                    _methodsToGen = MethodSpecifier.None;

                    foreach (KeyValuePair<MethodSpecifier, HashSet<TypeSpec>> method in Methods)
                    {
                        if (method.Value.Count > 0)
                        {
                            MethodSpecifier specifier = method.Key;

                            if (specifier is MethodSpecifier.Configure or MethodSpecifier.Get)
                            {
                                _methodsToGen |= MethodSpecifier.HasValueOrChildren;
                            }
                            else if (specifier is MethodSpecifier.BindCore)
                            {
                                _methodsToGen |= MethodSpecifier.HasChildren;
                            }

                            _methodsToGen |= specifier;
                        }
                    }
                }

                return _methodsToGen.Value;
            }
        }
    }

    [Flags]
    internal enum MethodSpecifier
    {
        None = 0x0,
        Bind = 0x1,
        Get = 0x2,
        Configure = 0x4,
        BindCore = 0x8,
        HasValueOrChildren = 0x10,
        HasChildren = 0x20,
    }
}
