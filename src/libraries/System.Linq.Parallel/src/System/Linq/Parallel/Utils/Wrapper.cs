// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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