// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
namespace System.Runtime.CompilerServices {

    using System;

    // Custom attribute to indicating a TypeDef is a discardable attribute
[System.Runtime.InteropServices.ComVisible(true)]
    public class DiscardableAttribute : Attribute
    {
        public DiscardableAttribute()
        {
        }
    }
}
