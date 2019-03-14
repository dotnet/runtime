// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Determines how a given class is treated when it is (de)serialized.
    /// </summary>
    internal enum ClassType
    {
        Object = 0,
        Value = 1,
        Enumerable = 2,
    }
}
