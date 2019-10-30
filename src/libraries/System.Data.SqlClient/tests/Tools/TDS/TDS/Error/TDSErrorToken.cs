// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.Error
{
    /// <summary>
    /// Environment change token "ERROR"
    /// </summary>
    public class TDSErrorToken : TDSPacketToken
    {
        /// <summary>
        /// Error number
        /// </summary>
        public uint Number { get; set; }

        /// <summary>
        /// Error state
        /// </summary>
        public byte State { get; set; }

        /// <summary>
        /// Error class
        /// </summary>
        public byte Class { get; set; }

        /// <summary>
        /// Description of the error
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Name of the server generated an error
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Procedure that produced an error
        /// </summary>
        public string ProcedureName { get; set; }

        /// <summary>
        /// Line number at which an error occurred
        /// </summary>
        public uint Line { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSErrorToken()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSErrorToken(uint number)
        {
            Number = number;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSErrorToken(uint number, byte state) :
            this(number)
        {
            State = state;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSErrorToken(uint number, byte state, byte clazz) :
            this(number, state)
        {
            Class = clazz;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSErrorToken(uint number, byte state, byte clazz, string message) :
            this(number, state, clazz)
        {
            Message = message;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSErrorToken(uint number, byte state, byte clazz, string message, string serverName) :
            this(number, state, clazz, message)
        {
            ServerName = serverName;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSErrorToken(uint number, byte state, byte clazz, string message, string serverName, string procedureName) :
            this(number, state, clazz, message, serverName)
        {
            ProcedureName = procedureName;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSErrorToken(uint number, byte state, byte clazz, string message, string serverName, string procedureName, uint line) :
            this(number, state, clazz, message, serverName, procedureName)
        {
            Line = line;
        }

        /// <summary>
        /// Inflating constructor
        /// </summary>
        public TDSErrorToken(Stream source)
        {
            // Inflate the token
            Inflate(source);
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

            // Read the number
            Number = TDSUtilities.ReadUInt(source);

            // Read state
            State = (byte)source.ReadByte();

            // Read class
            Class = (byte)source.ReadByte();

            // Read the message text length
            ushort textLength = TDSUtilities.ReadUShort(source);

            // Read the message itself
            Message = TDSUtilities.ReadString(source, (ushort)(textLength * 2));

            // Read server name length
            textLength = (byte)source.ReadByte();

            // Read server name
            ServerName = TDSUtilities.ReadString(source, (ushort)(textLength * 2));

            // Read procedure name length
            textLength = (byte)source.ReadByte();

            // Read procedure name
            ProcedureName = TDSUtilities.ReadString(source, (ushort)(textLength * 2));

            // Read the line number
            Line = TDSUtilities.ReadUInt(source);

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public override void Deflate(Stream destination)
        {
            // Write token identifier
            destination.WriteByte((byte)TDSTokenType.Error);

            // Calculate the length
            ushort totalLength = (ushort)(sizeof(uint) // Number
                + sizeof(byte) // State
                + sizeof(byte) // Class
                + sizeof(ushort) + (string.IsNullOrEmpty(Message) ? 0 : Message.Length) * sizeof(char) // Message
                + sizeof(byte) + (string.IsNullOrEmpty(ServerName) ? 0 : ServerName.Length) * sizeof(char) // Server Name
                + sizeof(byte) + (string.IsNullOrEmpty(ProcedureName) ? 0 : ProcedureName.Length) * sizeof(char) // Procedure Name
                + sizeof(uint)); // Line number

            // Write token length
            TDSUtilities.WriteUShort(destination, totalLength);

            // Write the number
            TDSUtilities.WriteUInt(destination, Number);

            // Write state
            destination.WriteByte((byte)State);

            // Write class
            destination.WriteByte((byte)Class);

            // Write message text length
            TDSUtilities.WriteUShort(destination, (ushort)(string.IsNullOrEmpty(Message) ? 0 : Message.Length));

            // Write the message itself
            TDSUtilities.WriteString(destination, Message);

            // Write server name length
            destination.WriteByte((byte)(string.IsNullOrEmpty(ServerName) ? 0 : ServerName.Length));

            // Write server name
            TDSUtilities.WriteString(destination, ServerName);

            // Write procedure name length
            destination.WriteByte((byte)(string.IsNullOrEmpty(ProcedureName) ? 0 : ProcedureName.Length));

            // Write procedure name
            TDSUtilities.WriteString(destination, ProcedureName);

            // Write the line number
            TDSUtilities.WriteUInt(destination, Line);
        }
    }
}
