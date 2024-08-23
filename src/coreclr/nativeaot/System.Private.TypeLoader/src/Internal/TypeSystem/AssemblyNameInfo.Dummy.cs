// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Dummy implementation of AssemlyNameInfo for runtime type system
    public abstract class AssemblyNameInfo
    {
        public abstract string FullName { get; }
    }
}
