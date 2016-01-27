// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Threading.Tasks
{
    /// <summary>Utility class for allocating value types as heap variables.</summary>
    internal class Box<T>
    {
        internal T Value;

        internal Box(T value)
        {
            this.Value = value;
        }
    }  // class Box<T>
}  // namespace
