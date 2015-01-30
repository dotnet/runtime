// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
