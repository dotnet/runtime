// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data
{
    /// <summary>
    /// Indicates the action that occurs when a <see cref='System.Data.ForeignKeyConstraint'/>
    /// is enforced.
    /// </summary>
    public enum Rule
    {
        /// <summary>
        /// No action occurs.
        /// </summary>
        None = 0,
        /// <summary>
        /// Changes are cascaded through the relationship.
        /// </summary>
        Cascade = 1,
        /// <summary>
        /// Null values are set in the rows affected by the deletion.
        /// </summary>
        SetNull = 2,
        /// <summary>
        /// Default values are set in the rows affected by the deletion.
        /// </summary>
        SetDefault = 3
    }
}
