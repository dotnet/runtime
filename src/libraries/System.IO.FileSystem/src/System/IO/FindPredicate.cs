// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    /// <summary>
    /// Interface for filtering out find results.
    /// </summary>
    internal delegate bool FindPredicate<TState>(ref RawFindData findData, TState state);
}
