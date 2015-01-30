// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All),
     ComVisible(false)]
    internal sealed class DecoratedNameAttribute : Attribute
    {
        public DecoratedNameAttribute(string decoratedName)
        {}
    }
}

