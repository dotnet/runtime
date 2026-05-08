// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;

using Internal.TypeSystem;

namespace Internal
{
    /// <summary>
    /// Managed implementation of the version-resilient hash code algorithm.
    /// </summary>
    internal static partial class VersionResilientHashCode
    {
        public static int ModuleNameHashCode(ModuleDesc module)
        {
            IAssemblyDesc assembly = module.Assembly;
            Debug.Assert(assembly == module);
            return NameHashCode(assembly.Name);
        }
    }
}
