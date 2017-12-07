// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    /// <summary>
    /// Delegate for transforming raw find data into a result.
    /// </summary>
    internal delegate T FindTransform<T>(ref RawFindData findData);
}
