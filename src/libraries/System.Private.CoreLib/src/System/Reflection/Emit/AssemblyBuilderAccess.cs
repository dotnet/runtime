// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This enumeration defines the access modes for a dynamic assembly.
// EE uses these enum values..look for m_dwDynamicAssemblyAccess in Assembly.hpp

namespace System.Reflection.Emit
{
    [Flags]
    public enum AssemblyBuilderAccess
    {
        Run = 1,
        RunAndCollect = 8 | Run
    }
}
