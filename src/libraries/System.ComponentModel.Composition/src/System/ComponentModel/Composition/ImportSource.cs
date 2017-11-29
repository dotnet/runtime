// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    /// <summary>
    /// Option placed on an import to determine how composition searches for exports.
    /// </summary>
    public enum ImportSource : int
    {
        /// <summary>
        /// The import can be satisfied with values from the current or parent (or other ancestor) containers  (scopes)
        /// </summary>
        Any = 0,

        /// <summary>
        /// The import can be satisfied with values from the current container (scope) 
        /// </summary>
        Local = 1,

        /// <summary>
        /// The import can only be satisfied with values from the parent container (or other ancestor containers) (scopes) 
        /// </summary>
        NonLocal = 2

    }
}
