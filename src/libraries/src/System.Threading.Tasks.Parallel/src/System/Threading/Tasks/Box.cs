// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
