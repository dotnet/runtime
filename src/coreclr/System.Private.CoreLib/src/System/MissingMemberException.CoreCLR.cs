// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace System
{
    public partial class MissingMemberException : MemberAccessException, ISerializable
    {
        internal static string FormatSignature(byte[]? signature)
        {
            if (signature == null || signature.Length == 0)
            {
                return "Unknown signature";
            }

            ValueStringBuilder stringBuilder = new ValueStringBuilder(stackalloc char[256]);

            try
            {
                ReadOnlySpan<byte> sig = signature;

                int cconv = BinaryPrimitives.ReadInt32LittleEndian(sig);
                sig = sig.Slice(sizeof(int));

                if (cconv == (int)CorCallingConvention.IMAGE_CEE_CS_CALLCONV_FIELD)
                {
                    UnparseType(sig, ref stringBuilder);
                }
                else
                {
                    int nargs = BinaryPrimitives.ReadInt32LittleEndian(sig);
                    sig = sig.Slice(sizeof(int));

                    // return type
                    sig = sig.Slice(UnparseType(sig, ref stringBuilder));
                    stringBuilder.Append('(');

                    for (int i = 0; i < nargs; i++)
                    {
                        sig = sig.Slice(UnparseType(sig, ref stringBuilder));
                        if (i != nargs - 1)
                        {
                            stringBuilder.Append(", ");
                        }
                    }

                    stringBuilder.Append(')');
                }

                return stringBuilder.ToString();
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentOutOfRangeException)
            {
                stringBuilder.Dispose();
                throw new ArgumentException(SR.Argument_BadSigFormat, nameof(signature));
            }
        }

        private static int UnparseType(ReadOnlySpan<byte> sig, ref ValueStringBuilder stringBuilder)
        {
            int bytesConsumed = 1;

            switch ((CorElementType)sig[0])
            {
                case CorElementType.ELEMENT_TYPE_VOID:
                    stringBuilder.Append("void");
                    break;
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    stringBuilder.Append("boolean");
                    break;
                case CorElementType.ELEMENT_TYPE_CHAR:
                    stringBuilder.Append("char");
                    break;
                case CorElementType.ELEMENT_TYPE_U1:
                    stringBuilder.Append("unsigned byte");
                    break;
                case CorElementType.ELEMENT_TYPE_I1:
                    stringBuilder.Append("byte");
                    break;
                case CorElementType.ELEMENT_TYPE_U2:
                    stringBuilder.Append("unsigned short");
                    break;
                case CorElementType.ELEMENT_TYPE_I2:
                    stringBuilder.Append("short");
                    break;
                case CorElementType.ELEMENT_TYPE_U4:
                    stringBuilder.Append("unsigned int");
                    break;
                case CorElementType.ELEMENT_TYPE_I4:
                    stringBuilder.Append("int");
                    break;
                case CorElementType.ELEMENT_TYPE_U8:
                    stringBuilder.Append("unsigned long");
                    break;
                case CorElementType.ELEMENT_TYPE_I8:
                    stringBuilder.Append("long");
                    break;
                case CorElementType.ELEMENT_TYPE_U:
                    stringBuilder.Append("native unsigned");
                    break;
                case CorElementType.ELEMENT_TYPE_I:
                    stringBuilder.Append("native int");
                    break;
                case CorElementType.ELEMENT_TYPE_R4:
                    stringBuilder.Append("float");
                    break;
                case CorElementType.ELEMENT_TYPE_R8:
                    stringBuilder.Append("double");
                    break;
                case CorElementType.ELEMENT_TYPE_STRING:
                    stringBuilder.Append("String");
                    break;
                case CorElementType.ELEMENT_TYPE_VAR:
                case CorElementType.ELEMENT_TYPE_OBJECT:
                    stringBuilder.Append("Object");
                    break;

                case CorElementType.ELEMENT_TYPE_PTR:
                    bytesConsumed += UnparseType(sig.Slice(1), ref stringBuilder);
                    stringBuilder.Append('*');
                    break;

                case CorElementType.ELEMENT_TYPE_BYREF:
                    bytesConsumed += UnparseType(sig.Slice(1), ref stringBuilder);
                    stringBuilder.Append('&');
                    break;

                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                case CorElementType.ELEMENT_TYPE_CLASS:
                    int length = sig.Slice(1).IndexOf((byte)'\0');
                    if (length == -1)
                    {
                        throw new ArgumentOutOfRangeException(); // rethrown by caller
                    }
                    stringBuilder.Append(Encoding.UTF8.GetString(sig.Slice(1, length)));
                    bytesConsumed += length + 1;
                    break;

                case CorElementType.ELEMENT_TYPE_SZARRAY:
                    bytesConsumed += UnparseType(sig.Slice(1), ref stringBuilder);
                    stringBuilder.Append("[]");
                    break;

                case CorElementType.ELEMENT_TYPE_ARRAY:
                    bytesConsumed += UnparseType(sig.Slice(1), ref stringBuilder);
                    sig = sig.Slice(1 + bytesConsumed);
                    int rank = BinaryPrimitives.ReadInt32LittleEndian(sig);
                    sig = sig.Slice(sizeof(int));
                    bytesConsumed += sizeof(int);
                    if (rank > 0)
                    {
                        int nSizes = BinaryPrimitives.ReadInt32LittleEndian(sig);
                        sig = sig.Slice(sizeof(int) * (nSizes + 1));
                        bytesConsumed += sizeof(int) * (nSizes + 1);

                        int nBounds = BinaryPrimitives.ReadInt32LittleEndian(sig);
                        // sig = sig.Slice(sizeof(int) * (nBounds + 1));
                        bytesConsumed += sizeof(int) * (nBounds + 1);

                        stringBuilder.Append('[');
                        for (int i = 1; i < rank; i++)
                        {
                            stringBuilder.Append(',');
                        }
                        stringBuilder.Append(']');
                    }
                    break;

                case CorElementType.ELEMENT_TYPE_TYPEDBYREF:
                    stringBuilder.Append("&");
                    break;
                case CorElementType.ELEMENT_TYPE_FNPTR:
                    stringBuilder.Append("ftnptr");
                    break;
                default:
                    stringBuilder.Append("?");
                    break;
            }

            return bytesConsumed;
        }
    }

    internal enum CorCallingConvention
    {
        IMAGE_CEE_CS_CALLCONV_FIELD = 0x6
    }
}
