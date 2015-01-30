// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices {
[Serializable]
[AttributeUsage(AttributeTargets.Struct, Inherited = true),
     System.Runtime.InteropServices.ComVisible(true)]
    public sealed class NativeCppClassAttribute : Attribute
    {
        public NativeCppClassAttribute () {}
    }
}
