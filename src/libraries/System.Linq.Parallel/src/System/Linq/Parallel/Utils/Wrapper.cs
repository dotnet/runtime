// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Wrapper.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Linq.Parallel
{
    /// <summary>
    /// A struct to wrap any arbitrary object reference or struct.  Used for situations
    /// where we can't tolerate null values (like keys for hashtables).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct Wrapper<T>
    {
        internal T Value;

        internal Wrapper(T value)
        {
            this.Value = value;
        }
    }
}
