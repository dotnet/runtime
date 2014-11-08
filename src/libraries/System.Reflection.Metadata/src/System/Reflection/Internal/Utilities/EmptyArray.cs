// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Internal
{
    internal static class EmptyArray<T>
    {
        internal static readonly T[] Instance = new T[0];
    }
}
