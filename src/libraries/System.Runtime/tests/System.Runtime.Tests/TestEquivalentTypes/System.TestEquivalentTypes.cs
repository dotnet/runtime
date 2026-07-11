// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Tests
{
    // This file is compiled into both System.Runtime.Tests.dll and System.TestEquivalentTypes.dll.
    // The two resulting types are type-equivalent because they have the same name, namespace, and
    // matching [TypeIdentifier] scope and identifier. See TypeIdentifierAttribute for details.
    [TypeIdentifier("31F8EDB4-A306-4EBB-8C3D-B9F4B28F1DE9", "7B43F12E-AEF2-4987-B01D-B8B6B39E8C41")]
    public delegate void EquivalentDelegate();
}
