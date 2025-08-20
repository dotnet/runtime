// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// The <see cref="ReferenceHandler"/> to be used at run time.
    /// </summary>
    public enum JsonKnownReferenceHandler
    {
        /// <summary>
        /// Specifies that circular references should throw exceptions.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Specifies that the built-in <see cref="ReferenceHandler.Preserve"/> be used to handle references.
        /// </summary>
        Preserve = 1,

        /// <summary>
        /// Specifies that the built-in <see cref="ReferenceHandler.IgnoreCycles"/> be used to ignore cyclic references.
        /// </summary>
        IgnoreCycles = 2,
    }
}
