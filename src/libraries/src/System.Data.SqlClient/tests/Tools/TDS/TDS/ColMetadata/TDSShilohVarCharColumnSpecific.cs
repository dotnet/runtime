// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.ColMetadata
{
    /// <summary>
    /// Metadata associated with Shiloh variable character column
    /// </summary>
    public class TDSShilohVarCharColumnSpecific
    {
        /// <summary>
        /// Length of the data
        /// </summary>
        public ushort Length { get; set; }

        /// <summary>
        /// Collation
        /// </summary>
        public TDSColumnDataCollation Collation { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSShilohVarCharColumnSpecific()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSShilohVarCharColumnSpecific(ushort length)
        {
            Length = length;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSShilohVarCharColumnSpecific(ushort length, TDSColumnDataCollation collation)
        {
            Length = length;
            Collation = collation;
        }
    }
}
