// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.SqlServer.TDS.Login7
{
    /// <summary>
    /// Helper class that takes care of setting property value
    /// </summary>
    public class TDSLogin7TokenOffsetProperty
    {
        /// <summary>
        /// Property which value is being set
        /// </summary>
        public PropertyInfo Property { get; set; }

        /// <summary>
        /// Position of the value in the data stream
        /// </summary>
        public uint Position { get; set; }

        /// <summary>
        /// Length of the property value in the data stream
        /// </summary>
        public uint Length { get; set; }

        /// <summary>
        /// This property is used to distinguish between "value" position in the stream and "offset of the value" position
        /// </summary>
        public bool IsOffsetOffset { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
		public TDSLogin7TokenOffsetProperty(PropertyInfo property, ushort position, ushort length)
        {
            Property = property;
            Position = position;
            Length = length;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
		public TDSLogin7TokenOffsetProperty(PropertyInfo property, ushort position, ushort length, bool isOffsetOffset) :
            this(property, position, length)
        {
            IsOffsetOffset = isOffsetOffset;
        }
    }
}
