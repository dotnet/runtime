// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Emit
{
    internal sealed class CustomAttributeWrapper
    {
        internal ConstructorInfo constructorInfo;
        internal byte[] binaryAttribute;

        public CustomAttributeWrapper(ConstructorInfo constructorInfo, byte[] binaryAttribute)
        {
            this.constructorInfo = constructorInfo;
            this.binaryAttribute = binaryAttribute;
        }
    }

    internal struct CustomAttributeInfo
    {
        public ConstructorInfo _ctor;
        public object?[] _ctorArgs;
        public string[] _namedParamNames;
        public object?[] _namedParamValues;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(String)'",
            Justification = "The 'enumTypeName' only available at runtime")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicFields', 'DynamicallyAccessedMemberTypes.NonPublicFields' in call to 'System.Type.GetField(String, BindingFlags)'",
            Justification = "Could not propagate attribute into 'ctor.DeclaringType' only available at runtime")]
        internal static CustomAttributeInfo DecodeCustomAttribute(ConstructorInfo ctor, byte[] data)
        {
            int pos;
            CustomAttributeInfo info = default;

            // Prolog
            if (data.Length < 2)
                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_InvalidCustomAttributeLength, data.Length));
            if ((data[0] != 0x1) || (data[1] != 0x00))
                throw new InvalidOperationException(SR.InvalidOperation_InvalidProlog);
            pos = 2;

            ParameterInfo[] pi = ctor.GetParameters();
            info._ctor = ctor;
            info._ctorArgs = new object?[pi.Length];
            for (int i = 0; i < pi.Length; ++i)
                info._ctorArgs[i] = DecodeCustomAttributeValue(pi[i].ParameterType, data, pos, out pos);

            int numNamed = data[pos] + (data[pos + 1] * 256);
            pos += 2;

            info._namedParamNames = new string[numNamed];
            info._namedParamValues = new object[numNamed];
            for (int i = 0; i < numNamed; ++i)
            {
                int namedType = data[pos++];
                int dataType = data[pos++];
                string? enumTypeName = null;

                if (dataType == 0x55)
                {
                    int len2 = DecodeLen(data, pos, out pos);
                    enumTypeName = StringFromBytes(data, pos, len2);
                    pos += len2;
                }

                int len = DecodeLen(data, pos, out pos);
                string name = StringFromBytes(data, pos, len);
                info._namedParamNames[i] = name;
                pos += len;

                if (namedType == 0x53)
                {
                    /* Field */
                    FieldInfo? fi = ctor.DeclaringType!.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fi == null)
                        throw new InvalidOperationException(SR.Format(SR.InvalidOperation_EmptyFieldForCustomAttributeType, ctor.DeclaringType, name));

                    object? val = DecodeCustomAttributeValue(fi.FieldType, data, pos, out pos);
                    if (enumTypeName != null)
                    {
                        Type enumType = Type.GetType(enumTypeName)!;
                        val = Enum.ToObject(enumType, val!);
                    }

                    info._namedParamValues[i] = val;
                }
                else
                    throw new InvalidOperationException(SR.Format(SR.InvalidOperation_UnknownNamedType, namedType));
            }

            return info;
        }

        private static string StringFromBytes(byte[] data, int pos, int len)
        {
            return Text.Encoding.UTF8.GetString(data, pos, len);
        }

        private static int DecodeLen(byte[] data, int pos, out int rpos)
        {
            int len;
            if ((data[pos] & 0x80) == 0)
            {
                len = (int)(data[pos++] & 0x7f);
            }
            else if ((data[pos] & 0x40) == 0)
            {
                len = ((data[pos] & 0x3f) << 8) + data[pos + 1];
                pos += 2;
            }
            else
            {
                len = ((data[pos] & 0x1f) << 24) + (data[pos + 1] << 16) + (data[pos + 2] << 8) + data[pos + 3];
                pos += 4;
            }
            rpos = pos;
            return len;
        }

        private static object? DecodeCustomAttributeValue(Type t, byte[] data, int pos, out int rpos)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.String:
                    if (data[pos] == 0xff)
                    {
                        rpos = pos + 1;
                        return null;
                    }
                    int len = DecodeLen(data, pos, out pos);
                    rpos = pos + len;
                    return StringFromBytes(data, pos, len);
                case TypeCode.Int32:
                    rpos = pos + 4;
                    return data[pos] + (data[pos + 1] << 8) + (data[pos + 2] << 16) + (data[pos + 3] << 24);
                case TypeCode.Boolean:
                    rpos = pos + 1;
                    return (data[pos] == 0) ? false : true;
                case TypeCode.Object:
                    int subtype = data[pos];
                    pos += 1;

                    if (subtype >= 0x02 && subtype <= 0x0e)
                        return DecodeCustomAttributeValue(ElementTypeToType(subtype), data, pos, out rpos);
                    else
                        throw new NotImplementedException(SR.NotImplemented_UnhandledSubType);
                default:
                    throw new NotImplementedException(SR.Format(SR.NotImplemented_TypeForValueNotHandled, t));
            }
        }

        private static Type ElementTypeToType(int elementType) =>
           /* Partition II, section 23.1.16 */
           elementType switch
           {
               0x02 => typeof(bool),
               0x03 => typeof(char),
               0x04 => typeof(sbyte),
               0x05 => typeof(byte),
               0x06 => typeof(short),
               0x07 => typeof(ushort),
               0x08 => typeof(int),
               0x09 => typeof(uint),
               0x0a => typeof(long),
               0x0b => typeof(ulong),
               0x0c => typeof(float),
               0x0d => typeof(double),
               0x0e => typeof(string),
               _ => throw new ArgumentException(SR.Format(SR.ArgumentException_InvalidTypeArgument, elementType)),
           };
    }
}
