// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    /// <summary>
    /// Specifies the aggregate function type.
    /// </summary>
    internal enum AggregateType
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,
        /// <summary>
        /// Sum.
        /// </summary>
        Sum = 4,
        /// <summary>
        /// Average value of the aggregate set.
        /// </summary>
        Mean = 5,
        /// <summary>
        /// The minimum value of the set.
        /// </summary>
        Min = 6,
        /// <summary>
        /// The maximum value of the set.
        /// </summary>
        Max = 7,
        /// <summary>
        /// First element of the set.
        /// </summary>
        First = 8,
        /// <summary>
        /// The count of the set.
        /// </summary>
        Count = 9,
        /// <summary>
        /// Variance.
        /// </summary>
        Var = 10,
        /// <summary>
        /// Standard deviation.
        /// </summary>
        StDev = 11
    }
}
