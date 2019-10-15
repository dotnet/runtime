// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.SqlServer.TDS.ColMetadata
{
    /// <summary>
    /// Class that describes metadata of a single column
    /// </summary>
    public class TDSColumnData : IInflatable, IDeflatable
    {
        /// <summary>
        /// User type ID of the data type of the column
        /// </summary>
        public uint UserType { get; set; }

        /// <summary>
        /// Type of the data
        /// </summary>
        public TDSDataType DataType { get; set; }

        /// <summary>
        /// Information specific to the data type
        /// </summary>
        public object DataTypeSpecific { get; set; }

        /// <summary>
        /// Column metadata flags
        /// </summary>
        public TDSColumnDataFlags Flags { get; set; }

        /// <summary>
        /// Fully qualified base table name for the column
        /// </summary>
        public IList<string> TableName { get; set; }

        /// <summary>
        /// Name of the column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSColumnData()
        {
            // Create flags
            Flags = new TDSColumnDataFlags();
        }

        /// <summary>
        /// Inflate the token
        /// </summary>
        /// <param name="source">Stream to inflate the token from</param>
        /// <returns>TRUE if inflation is complete</returns>
        public bool Inflate(Stream source)
        {
            // Read user type
            UserType = TDSUtilities.ReadUInt(source);

            // Read flags
            Flags = new TDSColumnDataFlags(TDSUtilities.ReadUShort(source));

            // Read server type
            DataType = (TDSDataType)source.ReadByte();

            // Dispatch further reading based on the type
            switch (DataType)
            {
                case TDSDataType.Binary:
                case TDSDataType.VarBinary:
                case TDSDataType.Char:
                case TDSDataType.VarChar:
                // The above types are deprecated after TDS 7205.

                case TDSDataType.BitN:
                case TDSDataType.Guid:
                case TDSDataType.IntN:
                case TDSDataType.MoneyN:
                case TDSDataType.FloatN:
                case TDSDataType.DateTimeN:
                    {
                        // Byte length
                        DataTypeSpecific = source.ReadByte();
                        break;
                    }
                case TDSDataType.DateN:
                    {
                        // No details
                        DataTypeSpecific = null;
                        break;
                    }
                case TDSDataType.TimeN:
                case TDSDataType.DateTime2N:
                case TDSDataType.DateTimeOffsetN:
                    {
                        // Scale
                        DataTypeSpecific = source.ReadByte();
                        break;
                    }
                case TDSDataType.DecimalN:
                case TDSDataType.NumericN:
                    {
                        // Read values
                        byte length = (byte)source.ReadByte();
                        byte precision = (byte)source.ReadByte();
                        byte scale = (byte)source.ReadByte();

                        // Decimal data type specific
                        DataTypeSpecific = new TDSDecimalColumnSpecific(length, precision, scale);
                        break;
                    }
                case TDSDataType.BigBinary:
                case TDSDataType.BigVarBinary:
                    {
                        // Short length
                        DataTypeSpecific = TDSUtilities.ReadUShort(source);
                        break;
                    }
                case TDSDataType.BigChar:
                case TDSDataType.BigVarChar:
                case TDSDataType.NChar:
                case TDSDataType.NVarChar:
                    {
                        // SHILOH CHAR types have collation associated with it.
                        TDSShilohVarCharColumnSpecific typedSpecific = new TDSShilohVarCharColumnSpecific();

                        // Read length
                        typedSpecific.Length = TDSUtilities.ReadUShort(source);

                        // Create collation
                        typedSpecific.Collation = new TDSColumnDataCollation();

                        // Read collation
                        typedSpecific.Collation.WCID = TDSUtilities.ReadUInt(source);
                        typedSpecific.Collation.SortID = (byte)source.ReadByte();

                        DataTypeSpecific = typedSpecific;
                        break;
                    }
                case TDSDataType.Text:
                case TDSDataType.NText:
                    {
                        // YukonTextType.Len + YukonTextType.tdsCollationInfo + YukonTextType.cParts
                        // cb = sizeof(LONG) + sizeof(TDSCOLLATION) + sizeof(BYTE);
                        break;
                    }
                case TDSDataType.Image:
                    {
                        // Data length
                        DataTypeSpecific = TDSUtilities.ReadUInt(source);
                        break;
                    }
                case TDSDataType.SSVariant:
                    {
                        // Data length
                        DataTypeSpecific = TDSUtilities.ReadUInt(source);
                        break;
                    }
                case TDSDataType.Udt:
                    {
                        // hr = GetUDTColFmt(pvOwner, dwTimeout);
                        break;
                    }
                case TDSDataType.Xml:
                    {
                        // cb = sizeof(lpColFmt->YukonXmlType.bSchemaPresent);
                        break;
                    }
            }

            // Check if table name is available
            if (DataType == TDSDataType.Text || DataType == TDSDataType.NText || DataType == TDSDataType.Image)
            {
                // Allocate table name container
                TableName = new List<string>();

                // Read part count
                byte partCount = (byte)source.ReadByte();

                // Write each part
                for (byte bPart = 0; bPart < partCount; bPart++)
                {
                    // Read table part length and value
                    TableName.Add(TDSUtilities.ReadString(source, (ushort)(TDSUtilities.ReadUShort(source) * 2)));
                }
            }

            // Read column name
            Name = TDSUtilities.ReadString(source, (ushort)(source.ReadByte() * 2));

            return true;
        }

        /// <summary>
        /// Deflate the token
        /// </summary>
        /// <param name="destination">Stream to deflate token to</param>
        public void Deflate(Stream destination)
        {
            // Write user type
            TDSUtilities.WriteUInt(destination, UserType);

            // Convert flags to value and write them
            TDSUtilities.WriteUShort(destination, Flags.ToUShort());

            // Write type
            destination.WriteByte((byte)DataType);

            // Dispatch further writing based on the type
            switch (DataType)
            {
                case TDSDataType.Binary:
                case TDSDataType.VarBinary:
                case TDSDataType.Char:
                case TDSDataType.VarChar:
                case TDSDataType.BitN:
                case TDSDataType.Guid:
                case TDSDataType.IntN:
                case TDSDataType.MoneyN:
                case TDSDataType.FloatN:
                case TDSDataType.DateTimeN:
                    {
                        // Byte length
                        destination.WriteByte((byte)DataTypeSpecific);
                        break;
                    }
                case TDSDataType.DateN:
                    {
                        // No details
                        break;
                    }
                case TDSDataType.TimeN:
                case TDSDataType.DateTime2N:
                case TDSDataType.DateTimeOffsetN:
                    {
                        // Scale
                        destination.WriteByte((byte)DataTypeSpecific);
                        break;
                    }
                case TDSDataType.DecimalN:
                case TDSDataType.NumericN:
                    {
                        // Cast to type-specific information
                        TDSDecimalColumnSpecific typeSpecific = DataTypeSpecific as TDSDecimalColumnSpecific;

                        // Write values
                        destination.WriteByte(typeSpecific.Length);
                        destination.WriteByte(typeSpecific.Precision);
                        destination.WriteByte(typeSpecific.Scale);
                        break;
                    }
                case TDSDataType.BigBinary:
                case TDSDataType.BigVarBinary:
                    {
                        // Short length
                        TDSUtilities.WriteUShort(destination, (ushort)DataTypeSpecific);
                        break;
                    }
                case TDSDataType.BigChar:
                case TDSDataType.BigVarChar:
                case TDSDataType.NChar:
                case TDSDataType.NVarChar:
                    {
                        // Cast to type specific information
                        TDSShilohVarCharColumnSpecific typedSpecific = DataTypeSpecific as TDSShilohVarCharColumnSpecific;

                        // Write length
                        TDSUtilities.WriteUShort(destination, typedSpecific.Length);

                        // Write collation
                        TDSUtilities.WriteUInt(destination, typedSpecific.Collation.WCID);
                        destination.WriteByte(typedSpecific.Collation.SortID);
                        break;
                    }
                case TDSDataType.Text:
                case TDSDataType.NText:
                    {
                        // YukonTextType.Len + YukonTextType.tdsCollationInfo + YukonTextType.cParts
                        // cb = sizeof(LONG) + sizeof(TDSCOLLATION) + sizeof(BYTE);
                        break;
                    }
                case TDSDataType.Image:
                    {
                        // Integer length
                        TDSUtilities.WriteUInt(destination, (uint)DataTypeSpecific);
                        break;
                    }
                case TDSDataType.SSVariant:
                    {
                        // Data length
                        TDSUtilities.WriteUInt(destination, (uint)DataTypeSpecific);
                        break;
                    }
                case TDSDataType.Udt:
                    {
                        // hr = GetUDTColFmt(pvOwner, dwTimeout);
                        break;
                    }
                case TDSDataType.Xml:
                    {
                        // cb = sizeof(lpColFmt->YukonXmlType.bSchemaPresent);
                        break;
                    }
            }

            // Check if we need to write table name
            if ((DataType == TDSDataType.Text || DataType == TDSDataType.NText || DataType == TDSDataType.Image) && (TableName != null))
            {
                // Write part count
                destination.WriteByte((byte)TableName.Count);

                // Write each part
                foreach (string part in TableName)
                {
                    // Write table part length
                    TDSUtilities.WriteUShort(destination, (ushort)(string.IsNullOrEmpty(part) ? 0 : part.Length));

                    // Write table part
                    TDSUtilities.WriteString(destination, part);
                }
            }

            // Write column name length
            destination.WriteByte((byte)(string.IsNullOrEmpty(Name) ? 0 : Name.Length));

            // Write column name
            TDSUtilities.WriteString(destination, Name);
        }
    }
}
