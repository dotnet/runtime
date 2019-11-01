// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.DataAnnotations.Schema
{
    /// <summary>
    ///     The pattern used to generate values for a property in the database.
    /// </summary>
    public enum DatabaseGeneratedOption
    {
        /// <summary>
        ///     The database does not generate values.
        /// </summary>
        None = 0,

        /// <summary>
        ///     The database generates a value when a row is inserted.
        /// </summary>
        Identity = 1,

        /// <summary>
        ///     The database generates a value when a row is inserted or updated.
        /// </summary>
        Computed = 2
    }
}
