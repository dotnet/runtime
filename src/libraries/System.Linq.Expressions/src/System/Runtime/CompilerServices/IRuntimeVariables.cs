// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
