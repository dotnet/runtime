// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace System.Reflection.Metadata.Ecma335
{
    internal static class MetadataWriterUtilities
    {
        public static SignatureTypeCode GetConstantTypeCode(object value)
        {
            if (value == null)
            {
                // The encoding of Type for the nullref value for FieldInit is ELEMENT_TYPE_CLASS with a Value of a zero.
                return (SignatureTypeCode)0x12; // TODO
            }

            Debug.Assert(!value.GetType().GetTypeInfo().IsEnum);

            // Perf: Note that JIT optimizes each expression val.GetType() == typeof(T) to a single register comparison.
            // Also the checks are sorted by commonality of the checked types.

            if (value.GetType() == typeof(int))
            {
                return SignatureTypeCode.Int32;
            }

            if (value.GetType() == typeof(string))
            {
                return SignatureTypeCode.String;
            }

            if (value.GetType() == typeof(bool))
            {
                return SignatureTypeCode.Boolean;
            }

            if (value.GetType() == typeof(char))
            {
                return SignatureTypeCode.Char;
            }

            if (value.GetType() == typeof(byte))
            {
                return SignatureTypeCode.Byte;
            }

            if (value.GetType() == typeof(long))
            {
                return SignatureTypeCode.Int64;
            }

            if (value.GetType() == typeof(double))
            {
                return SignatureTypeCode.Double;
            }

            if (value.GetType() == typeof(short))
            {
                return SignatureTypeCode.Int16;
            }

            if (value.GetType() == typeof(ushort))
            {
                return SignatureTypeCode.UInt16;
            }

            if (value.GetType() == typeof(uint))
            {
                return SignatureTypeCode.UInt32;
            }

            if (value.GetType() == typeof(sbyte))
            {
                return SignatureTypeCode.SByte;
            }

            if (value.GetType() == typeof(ulong))
            {
                return SignatureTypeCode.UInt64;
            }

            if (value.GetType() == typeof(float))
            {
                return SignatureTypeCode.Single;
            }

            throw new ArgumentException(SR.Format(SR.InvalidConstantValueOfType, value.GetType()), nameof(value));
        }

        internal static void SerializeRowCounts(BlobBuilder writer, ImmutableArray<int> rowCounts)
        {
            for (int i = 0; i < rowCounts.Length; i++)
            {
                int rowCount = rowCounts[i];
                if (rowCount > 0)
                {
                    writer.WriteInt32(rowCount);
                }
            }
        }
    }
}
