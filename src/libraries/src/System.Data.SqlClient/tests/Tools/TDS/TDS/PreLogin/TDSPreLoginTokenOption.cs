// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.PreLogin
{
    /// <summary>
    /// Token of the pre-login packet
    /// </summary>
    public class TDSPreLoginTokenOption : IInflatable, IDeflatable
    {
        /// <summary>
        /// Token type
        /// </summary>
        public TDSPreLoginTokenOptionType Type { get; set; }

        /// <summary>
        /// The size of the token declaration
        /// </summary>
        public ushort TokenLength
        {
            get { return (ushort)(Type != TDSPreLoginTokenOptionType.Terminator ? (sizeof(byte) + sizeof(ushort) + sizeof(ushort)) : sizeof(byte)); }
        }

        /// <summary>
        /// Position of the token in the data buffer
        /// </summary>
        public ushort Position { get; set; }

        /// <summary>
        /// Token data
        /// </summary>
        public ushort Length { get; set; }

        /// <summary>
        /// Default
        /// </summary>
        public TDSPreLoginTokenOption()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSPreLoginTokenOption(TDSPreLoginTokenOptionType type)
        {
            Type = type;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSPreLoginTokenOption(TDSPreLoginTokenOptionType type, ushort length)
        {
            Type = type;
            Length = length;
        }

        /// <summary>
        /// Inflate the option
        /// </summary>
        public bool Inflate(Stream source)
        {
            // Read pre-login packet token type
            Type = (TDSPreLoginTokenOptionType)source.ReadByte();

            // Check if terminator found
            if (Type != TDSPreLoginTokenOptionType.Terminator)
            {
                // Read token position and length
                Position = (ushort)((source.ReadByte() << 8) + source.ReadByte());
                Length = (ushort)((source.ReadByte() << 8) + source.ReadByte());
            }

            return true;
        }

        /// <summary>
        /// Deflate the option
        /// </summary>
        public void Deflate(Stream destination)
        {
            // Write type
            destination.WriteByte((byte)Type);

            // Check if terminator token
            if (Type != TDSPreLoginTokenOptionType.Terminator)
            {
                // Write position
                destination.WriteByte((byte)((Position >> 8) & 0xff));
                destination.WriteByte((byte)(Position & 0xff));

                // Write length
                destination.WriteByte((byte)((Length >> 8) & 0xff));
                destination.WriteByte((byte)(Length & 0xff));
            }
        }
    }
}
