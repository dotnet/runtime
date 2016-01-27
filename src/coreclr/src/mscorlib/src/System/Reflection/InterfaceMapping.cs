// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// 
// Interface Map.  This struct returns the mapping of an interface into the actual methods on a class
//    that implement that interface.
//
//
namespace System.Reflection {
    using System;

[System.Runtime.InteropServices.ComVisible(true)]
    public struct InterfaceMapping {
[System.Runtime.InteropServices.ComVisible(true)]
        public Type                TargetType;            // The type implementing the interface
[System.Runtime.InteropServices.ComVisible(true)]
        public Type                InterfaceType;        // The type representing the interface
[System.Runtime.InteropServices.ComVisible(true)]
        public MethodInfo[]        TargetMethods;        // The methods implementing the interface
[System.Runtime.InteropServices.ComVisible(true)]
        public MethodInfo[]        InterfaceMethods;    // The methods defined on the interface
    }
}
