// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Reflection.Runtime.Dispensers
{
    //
    // A monikor for each reflection cache. The name should follow the style "key" followed by underscore followed by "value".
    //
    internal enum DispenserScenario
    {
        // Assembly + NamespaceTypeName to Type
        AssemblyAndNamespaceTypeName_Type,

        // Assembly refName to Assembly
        AssemblyRefName_Assembly,

        // RuntimeAssembly to CaseInsensitiveTypeDictionary
        RuntimeAssembly_CaseInsensitiveTypeDictionary,

        // Scope definition handle to RuntimeAssembly
        Scope_Assembly,
    }
}
