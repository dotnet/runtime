// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.EnvChange
{
    /// <summary>
    /// Environment change token "ENVCHANGE"
    /// </summary>
    public class TDSEnvChangeToken : TDSPacketToken
    {
        /// <summary>
        /// Type of the token
        /// </summary>
        public TDSEnvChangeTokenType Type { get; set; }

        /// <summary>
        /// Old value of the token (optional)
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// New value of the token
        /// </summary>
        public object NewValue { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSEnvChangeToken()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSEnvChangeToken(TDSEnvChangeTokenType type)
        {
            Type = type;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSEnvChangeToken(TDSEnvChangeTokenType type, object newValue) :
            this(type)
        {
            NewValue = newValue;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSEnvChangeToken(TDSEnvChangeTokenType type, object newValue, object oldValue) :
            this(type, newValue)
        {
            OldValue = oldValue;
        }

        /// <summary>
        /// Inflate the token
        /// NOTE: This operation is not continuable and assumes that the entire token is available in the stream
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public override bool Inflate(Stream source)
        {
            // We skip the token identifier because it is read by token factory

            // Read token length
            ushort tokenLength = TDSUtilities.ReadUShort(source);

            // Check if length can accomodate at least the type
            if (tokenLength == 0)
            {
                // We're done inflating this token
                return true;
            }

            // Read the token type
            Type = (TDSEnvChangeTokenType)source.ReadByte();

            // Update the token length left
            tokenLength--;

            // Check if we can squeez anything else
            if (tokenLength == 0)
            {
                // We're done inflating this token
                return true;
            }

            // Read the rest of the token based on the token type
            switch (Type)
            {
                case TDSEnvChangeTokenType.Database:
                case TDSEnvChangeTokenType.Language:
                case TDSEnvChangeTokenType.CharacterSet:
                case TDSEnvChangeTokenType.PacketSize:
                case TDSEnvChangeTokenType.RealTimeLogShipping:
                    {
                        // Read new value length
                        byte valueLength = (byte)source.ReadByte();

                        // Update token length
                        tokenLength--;

                        // Read string of the specified size
                        NewValue = TDSUtilities.ReadString(source, (ushort)(valueLength * 2));

                        // Update token length
                        tokenLength -= (ushort)(valueLength * 2);

                        // Check if old value can fit in
                        if (tokenLength == 0)
                        {
                            // Old value won't fit in
                            break;
                        }

                        // Read old value length
                        valueLength = (byte)source.ReadByte();

                        // Update token length
                        tokenLength--;

                        // Read string of the specified size
                        OldValue = TDSUtilities.ReadString(source, (ushort)(valueLength * 2));

                        // Update token length
                        tokenLength -= (ushort)(valueLength * 2);

                        // Inflation is complete
                        break;
                    }
                case TDSEnvChangeTokenType.Routing:
                    {
                        // Read the new value length
                        ushort valueLength = TDSUtilities.ReadUShort(source);

                        // Update token length
                        tokenLength -= 2;  // sizeof(ushort)

                        // Instantiate new value
                        NewValue = new TDSRoutingEnvChangeTokenValue();

                        // Inflate new value
                        if (!(NewValue as TDSRoutingEnvChangeTokenValue).Inflate(source))
                        {
                            // We should never reach this point
                            throw new Exception("Routing information inflation failed");
                        }

                        // Read always-zero old value unsigned short
                        if (TDSUtilities.ReadUShort(source) != 0)
                        {
                            // We should never reach this point
                            throw new Exception("Non-zero old value for routing information");
                        }

                        break;
                    }
                case TDSEnvChangeTokenType.SQLCollation:
                    {
                        // Read new value length
                        byte valueLength = (byte)source.ReadByte();

                        // Update token length
                        tokenLength--;

                        // Check if old value can fit in
                        if (tokenLength == 0)
                        {
                            // Old value won't fit in
                            break;
                        }

                        // Allocate the buffer
                        byte[] byteValue = new byte[valueLength];

                        // Read bytes off the wire
                        source.Read(byteValue, 0, byteValue.Length);

                        // Set new value
                        NewValue = byteValue;

                        // Update token length
                        tokenLength -= valueLength;

                        // Check if old value can fit in
                        if (tokenLength == 0)
                        {
                            // Old value won't fit in
                            break;
                        }

                        // Read old value length
                        valueLength = (byte)source.ReadByte();

                        // Update token length
                        tokenLength--;

                        // Check if old value can fit in
                        if (tokenLength == 0)
                        {
                            // Old value won't fit in
                            break;
                        }

                        // Allocate the buffer
                        byteValue = new byte[valueLength];

                        // Read bytes off the wire
                        source.Read(byteValue, 0, byteValue.Length);

                        // Set old value
                        OldValue = byteValue;

                        // Update token length
                        tokenLength -= valueLength;

                        // Inflation is complete
                        break;
                    }
                default:
                    {
                        // Skip the rest of the token
                        byte[] tokenData = new byte[tokenLength];
                        source.Read(tokenData, 0, tokenData.Length);

                        break;
                    }
            }

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Allocate stream for token data
            // We need to cache it to calculate the environment change token length
            MemoryStream cache = new MemoryStream();

            // Write environment change type
            cache.WriteByte((byte)Type);

            // Write the rest of the token based on the token type
            switch (Type)
            {
                case TDSEnvChangeTokenType.Database:
                case TDSEnvChangeTokenType.Language:
                case TDSEnvChangeTokenType.CharacterSet:
                case TDSEnvChangeTokenType.PacketSize:
                case TDSEnvChangeTokenType.RealTimeLogShipping:
                    {
                        // Write new value length
                        cache.WriteByte((byte)(string.IsNullOrEmpty((string)NewValue) ? 0 : ((string)NewValue).Length));

                        // Write new value
                        TDSUtilities.WriteString(cache, (string)NewValue);

                        // Write old value length
                        cache.WriteByte((byte)(string.IsNullOrEmpty((string)OldValue) ? 0 : ((string)OldValue).Length));

                        // Write old value
                        TDSUtilities.WriteString(cache, (string)OldValue);

                        break;
                    }
                case TDSEnvChangeTokenType.Routing:
                    {
                        // Create a sub-cache to determine the value length
                        MemoryStream subCache = new MemoryStream();

                        // Check if new value exists
                        if (NewValue != null)
                        {
                            // Deflate token value
                            (NewValue as TDSRoutingEnvChangeTokenValue).Deflate(subCache);
                        }

                        // Write new value length
                        TDSUtilities.WriteUShort(cache, (ushort)subCache.Length);

                        // Write new value
                        subCache.WriteTo(cache);

                        // Write zero for the old value length
                        TDSUtilities.WriteUShort(cache, 0);

                        break;
                    }
                case TDSEnvChangeTokenType.SQLCollation:
                    {
                        // Write new value length
                        cache.WriteByte((byte)(NewValue != null ? ((byte[])NewValue).Length : 0));

                        // Check if we have a new value
                        if (NewValue != null)
                        {
                            // Write new value
                            cache.Write((byte[])NewValue, 0, ((byte[])NewValue).Length);
                        }

                        // Write old value length
                        cache.WriteByte((byte)(OldValue != null ? ((byte[])OldValue).Length : 0));

                        // Check if we have a old value
                        if (OldValue != null)
                        {
                            // Write old value
                            cache.Write((byte[])OldValue, 0, ((byte[])OldValue).Length);
                        }

                        break;
                    }
                default:
                    {
                        throw new Exception("Unrecognized environment change type");
                    }
            }

            // Write token identifier
            destination.WriteByte((byte)TDSTokenType.EnvironmentChange);

            // Write length
            TDSUtilities.WriteUShort(destination, (ushort)cache.Length);

            // Write token data
            cache.WriteTo(destination);
        }
    }
}
