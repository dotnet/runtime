// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// An interface to represent values of runtime variables.
    /// </summary>
    public interface IRuntimeVariables
    {
        /// <summary>
        /// Count of the variables.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// An indexer to get/set the values of the runtime variables.
        /// </summary>
        /// <param name="index">An index of the runtime variable.</param>
        /// <returns>The value of the runtime variable.</returns>
        object this[int index] { get; set; }
    }
}
