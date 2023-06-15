// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

//
// System.Reflection.Emit/CustomAttributeBuilder.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    [StructLayout(LayoutKind.Sequential)]
    public partial class CustomAttributeBuilder
    {
        private ConstructorInfo ctor = null!;
        private byte[] data = null!;
        private object?[]? args;
        private PropertyInfo[] namedProperties = null!;
        private object?[] propertyValues = null!;
        private FieldInfo[] namedFields = null!;
        private object?[] fieldValues = null!;

        internal ConstructorInfo Ctor
        {
            get { return ctor; }
        }

        internal byte[] Data
        {
            get { return data; }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern byte[] GetBlob(Assembly asmb, ConstructorInfo con, object?[] constructorArgs, PropertyInfo[] namedProperties, object[] propertyValues, FieldInfo[] namedFields, object[] fieldValues);

        internal object Invoke()
        {
            object result = ctor.Invoke(args);

            for (int i = 0; i < namedFields.Length; i++)
                namedFields[i].SetValue(result, fieldValues[i]);

            for (int i = 0; i < namedProperties.Length; i++)
                namedProperties[i].SetValue(result, propertyValues[i]);

            return result;
        }

        internal CustomAttributeBuilder(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute)
        {
            ArgumentNullException.ThrowIfNull(con);

            ctor = con;
            data = binaryAttribute.ToArray();
            /* should we check that the user supplied data is correct? */
        }

        public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs)
        {
            Initialize(con, constructorArgs, Array.Empty<PropertyInfo>(), Array.Empty<object>(),
                    Array.Empty<FieldInfo>(), Array.Empty<object>());
        }
        public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs,
                FieldInfo[] namedFields, object[] fieldValues)
        {
            Initialize(con, constructorArgs, Array.Empty<PropertyInfo>(), Array.Empty<object>(),
                    namedFields, fieldValues);
        }
        public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs,
                PropertyInfo[] namedProperties, object[] propertyValues)
        {
            Initialize(con, constructorArgs, namedProperties, propertyValues, Array.Empty<FieldInfo>(),
                    Array.Empty<object>());
        }
        public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs,
                PropertyInfo[] namedProperties, object[] propertyValues,
                FieldInfo[] namedFields, object[] fieldValues)
        {
            Initialize(con, constructorArgs, namedProperties, propertyValues, namedFields, fieldValues);
        }

        private static bool IsValidType(Type t)
        {
            /* FIXME: Add more checks */
            if (t.IsArray && t.GetArrayRank() > 1)
                return false;
            if (t is TypeBuilder && t.IsEnum)
            {
                // Check that the enum is properly constructed, the unmanaged code
                // depends on this
                Enum.GetUnderlyingType(t);
            }
            if (t.IsClass && !(t.IsArray || t == typeof(object) || typeof(Type).IsAssignableFrom(t) || t == typeof(string) || t.Assembly.GetName().Name == "mscorlib"))
                return false;
            if (t.IsValueType && !(t.IsPrimitive || t.IsEnum || ((t.Assembly is AssemblyBuilder) && t.Assembly.GetName().Name == "mscorlib")))
                return false;
            return true;
        }

        private static bool IsValidParam(object o, Type paramType)
        {
            Type t = o.GetType();
            if (!IsValidType(t))
                return false;
            if (paramType == typeof(object))
            {
                if (t.IsArray && t.GetArrayRank() == 1)
                    return IsValidType(t.GetElementType()!);
                if (!t.IsPrimitive && !typeof(Type).IsAssignableFrom(t) && t != typeof(string) && !t.IsEnum)
                    return false;
            }
            return true;
        }

        private static bool IsValidValue(Type type, object? value)
        {
            if (type.IsValueType && value == null)
                return false;
            if (type.IsArray && type.GetElementType()!.IsValueType && value != null)
            {
                foreach (object? v in (Array)value!)
                {
                    if (v == null)
                        return false;
                }
            }
            return true;
        }

        private void Initialize(ConstructorInfo con, object?[] constructorArgs,
                PropertyInfo[] namedProperties, object[] propertyValues,
                FieldInfo[] namedFields, object[] fieldValues)
        {
            ctor = con;
            args = constructorArgs;
            this.namedProperties = namedProperties;
            this.propertyValues = propertyValues;
            this.namedFields = namedFields;
            this.fieldValues = fieldValues;

            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(constructorArgs);
            ArgumentNullException.ThrowIfNull(namedProperties);
            ArgumentNullException.ThrowIfNull(propertyValues);
            ArgumentNullException.ThrowIfNull(namedFields);
            ArgumentNullException.ThrowIfNull(fieldValues);

            AssemblyBuilder.EnsureDynamicCodeSupported();

            if (con.GetParametersCount() != constructorArgs.Length)
                throw new ArgumentException(SR.Argument_BadParameterCountsForConstructor);
            if (namedProperties.Length != propertyValues.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer, "namedProperties, propertyValues");
            if (namedFields.Length != fieldValues.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer, "namedFields, fieldValues");
            if ((con.Attributes & MethodAttributes.Static) == MethodAttributes.Static ||
                    (con.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
                throw new ArgumentException(SR.Argument_BadConstructor);

            // Here coreclr does
            // if ((con.CallingConvention & CallingConventions.Standard) != CallingConventions.Standard)
            //    throw new ArgumentException(SR.Argument_BadConstructorCallConv);

            Type atype = ctor.DeclaringType!;
            int i;
            i = 0;
            foreach (FieldInfo fi in namedFields)
            {
                Type t = fi.DeclaringType!;
                if ((atype != t) && (!t.IsSubclassOf(atype)) && (!atype.IsSubclassOf(t)))
                    throw new ArgumentException(SR.Format(SR.Argument_FieldDoesNotBelongToConstructorClass, fi.Name));
                if (!IsValidType(fi.FieldType))
                    throw new ArgumentException(SR.Format(SR.Argument_FieldDoesNotHaveAValidType, fi.Name));
                if (!IsValidValue(fi.FieldType, fieldValues[i]))
                    throw new ArgumentException(SR.Format(SR.Argument_FieldDoesNotHaveAValidValue, fi.Name));
                // FIXME: Check enums and TypeBuilders as well
                if (fieldValues[i] != null)
                    // IsEnum does not seem to work on TypeBuilders
                    if (!(fi.FieldType is TypeBuilder) && !fi.FieldType.IsEnum && !fi.FieldType.IsInstanceOfType(fieldValues[i]))
                    {
                        //
                        // mcs always uses object[] for array types and
                        // MS.NET allows this
                        //
                        if (!fi.FieldType.IsArray)
                            throw new ArgumentException(SR.Format(SR.Argument_UnmatchedFieldValueAndType, fi.Name, fi.FieldType));
                    }
                i++;
            }

            i = 0;
            foreach (PropertyInfo pi in namedProperties)
            {
                if (!pi.CanWrite)
                    throw new ArgumentException(SR.Format(SR.Argument_PropertyMissingSetter, pi.Name));
                Type t = pi.DeclaringType!;
                if ((atype != t) && (!t.IsSubclassOf(atype)) && (!atype.IsSubclassOf(t)))
                    throw new ArgumentException(SR.Format(SR.Argument_PropertyClassUnmatchedWithConstructor, pi.Name));
                if (!IsValidType(pi.PropertyType))
                    throw new ArgumentException(SR.Format(SR.Argument_PropertyInvalidType, pi.Name));
                if (!IsValidValue(pi.PropertyType, propertyValues[i]))
                    throw new ArgumentException(SR.Format(SR.Argument_PropertyInvalidValue, pi.Name));
                if (propertyValues[i] != null)
                {
                    if (!(pi.PropertyType is TypeBuilder) && !pi.PropertyType.IsEnum && !pi.PropertyType.IsInstanceOfType(propertyValues[i]))
                        if (!pi.PropertyType.IsArray)
                            throw new ArgumentException(SR.Format(SR.Argument_PropertyUnmatchingPropertyType, pi.Name, pi.PropertyType, propertyValues[i]));
                }
                i++;
            }

            i = 0;
            foreach (ParameterInfo pi in GetParameters(con))
            {
                if (pi != null)
                {
                    Type paramType = pi.ParameterType;
                    if (!IsValidType(paramType))
                        throw new ArgumentException(SR.Format(SR.Argument_ParameterInvalidType, i));
                    if (!IsValidValue(paramType, constructorArgs[i]))
                        throw new ArgumentException(SR.Format(SR.Argument_ParameterInvalidValue, i));

                    if (constructorArgs[i] != null)
                    {
                        if (!(paramType is TypeBuilder) && !paramType.IsEnum && !paramType.IsInstanceOfType(constructorArgs[i]))
                            if (!paramType.IsArray)
                                throw new ArgumentException(SR.Format(SR.Argument_ParameterHasUnmatchedArgumentValue, i, paramType, constructorArgs[i]));
                        if (!IsValidParam(constructorArgs[i]!, paramType))
                            throw new ArgumentException(SR.Format(SR.Argument_BadParameterTypeForCAB, constructorArgs[i]!.GetType()));
                    }
                }
                i++;
            }

            data = GetBlob(atype.Assembly, con, constructorArgs, namedProperties, propertyValues, namedFields, fieldValues);
        }

        /* helper methods */
        internal static int decode_len(ReadOnlySpan<byte> data, int pos, out int rpos)
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

        internal static string string_from_bytes(ReadOnlySpan<byte> data, int pos, int len)
        {
            return Text.Encoding.UTF8.GetString(data.Slice(pos, len));
        }

        internal static string? decode_string(byte[] data, int pos, out int rpos)
        {
            if (data[pos] == 0xff)
            {
                rpos = pos + 1;
                return null;
            }
            else
            {
                int len = decode_len(data, pos, out pos);
                string s = string_from_bytes(data, pos, len);
                pos += len;
                rpos = pos;
                return s;
            }
        }

        internal string? string_arg()
        {
            return decode_string(data, 2, out _);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern",
            Justification = "Types referenced from custom attributes are preserved")]
        internal static UnmanagedMarshal get_umarshal(CustomAttributeBuilder customBuilder, bool is_field)
        {
            byte[] data = customBuilder.Data;
            UnmanagedType subtype = (UnmanagedType)0x50; /* NATIVE_MAX */
            int sizeConst = -1;
            int sizeParamIndex = -1;
            bool hasSize = false;
            int value;
            int utype; /* the (stupid) ctor takes a short or an enum ... */
            string? marshalTypeName = null;
            Type? marshalTypeRef = null;
            string marshalCookie = string.Empty;
            utype = (int)data[2];
            utype |= ((int)data[3]) << 8;

            string? first_type_name = GetParameters(customBuilder.Ctor)[0].ParameterType.FullName;
            int pos = 6;
            if (first_type_name == "System.Int16")
                pos = 4;
            int nnamed = (int)data[pos++];
            nnamed |= ((int)data[pos++]) << 8;

            for (int i = 0; i < nnamed; ++i)
            {
                int paramType; // What is this ?

                /* Skip field/property signature */
                int fieldPropSig = (int)data[pos++];
                /* Read type */
                paramType = ((int)data[pos++]);
                if (paramType == 0x55)
                {
                    /* enums, the value is preceded by the type */
                    decode_string(data, pos, out pos);
                }
                string? named_name = decode_string(data, pos, out pos);

                switch (named_name)
                {
                    case "ArraySubType":
                        value = (int)data[pos++];
                        value |= ((int)data[pos++]) << 8;
                        value |= ((int)data[pos++]) << 16;
                        value |= ((int)data[pos++]) << 24;
                        subtype = (UnmanagedType)value;
                        break;
                    case "SizeConst":
                        value = (int)data[pos++];
                        value |= ((int)data[pos++]) << 8;
                        value |= ((int)data[pos++]) << 16;
                        value |= ((int)data[pos++]) << 24;
                        sizeConst = value;
                        hasSize = true;
                        break;
                    case "SafeArraySubType":
                        value = (int)data[pos++];
                        value |= ((int)data[pos++]) << 8;
                        value |= ((int)data[pos++]) << 16;
                        value |= ((int)data[pos++]) << 24;
                        subtype = (UnmanagedType)value;
                        break;
                    case "IidParameterIndex":
                        pos += 4;
                        break;
                    case "SafeArrayUserDefinedSubType":
                        decode_string(data, pos, out pos);
                        break;
                    case "SizeParamIndex":
                        value = (int)data[pos++];
                        value |= ((int)data[pos++]) << 8;
                        sizeParamIndex = value;
                        hasSize = true;
                        break;
                    case "MarshalType":
                        marshalTypeName = decode_string(data, pos, out pos);
                        break;
                    case "MarshalTypeRef":
                        marshalTypeName = decode_string(data, pos, out pos);
                        if (marshalTypeName != null)
                            marshalTypeRef = Type.GetType(marshalTypeName);
                        break;
                    case "MarshalCookie":
                        marshalCookie = decode_string(data, pos, out pos)!;
                        break;
                    default:
                        throw new Exception(SR.Format(SR.Exception_UnknownMarshalAsAttributeField, named_name));
                }
            }

            switch ((UnmanagedType)utype)
            {
                case UnmanagedType.LPArray:
                    if (hasSize)
                        return UnmanagedMarshal.DefineLPArrayInternal(subtype, sizeConst, sizeParamIndex);
                    else
                        return UnmanagedMarshal.DefineLPArray(subtype);
#if FEATURE_COMINTEROP
			case UnmanagedType.SafeArray:
				return UnmanagedMarshal.DefineSafeArray (subtype);
#endif
                case UnmanagedType.ByValArray:
                    if (!is_field)
                        throw new ArgumentException(SR.Argument_UnmanagedTypeOnlyValidOnFields);

                    return UnmanagedMarshal.DefineByValArray(sizeConst);
                case UnmanagedType.ByValTStr:
                    return UnmanagedMarshal.DefineByValTStr(sizeConst);
#if FEATURE_COMINTEROP
			case UnmanagedType.CustomMarshaler:
				return UnmanagedMarshal.DefineCustom (marshalTypeRef, marshalCookie, marshalTypeName, Guid.Empty);
#endif
                default:
                    return UnmanagedMarshal.DefineUnmanagedMarshal((UnmanagedType)utype);
            }
        }

        private static Type elementTypeToType(int elementType) =>
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
                _ => throw new Exception(SR.Format(SR.ArgumentException_InvalidTypeArgument, elementType)),
            };

        private static object? decode_cattr_value(Type t, ReadOnlySpan<byte> data, int pos, out int rpos)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.String:
                    if (data[pos] == 0xff)
                    {
                        rpos = pos + 1;
                        return null;
                    }
                    int len = decode_len(data, pos, out pos);
                    rpos = pos + len;
                    return string_from_bytes(data, pos, len);
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
                        return decode_cattr_value(elementTypeToType(subtype), data, pos, out rpos);
                    else
                        throw new Exception(SR.Exception_UnhandledSubType);
                default:
                    throw new Exception("FIXME: Type " + t + " not yet handled in decode_cattr_value.");
            }
        }

        internal struct CustomAttributeInfo
        {
            public ConstructorInfo ctor;
            public object?[] ctorArgs;
            public string[] namedParamNames;
            public object?[] namedParamValues;
        }

        internal static CustomAttributeInfo decode_cattr(CustomAttributeBuilder customBuilder)
        {
            byte[] data = customBuilder.Data;
            ConstructorInfo ctor = customBuilder.Ctor;
            return decode_cattr(ctor, data);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern",
            Justification = "Types referenced from custom attributes are preserved")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Types referenced from custom attributes are preserved")]
        internal static CustomAttributeInfo decode_cattr(ConstructorInfo ctor, ReadOnlySpan<byte> data)
        {
            int pos;

            CustomAttributeInfo info = default;

            // Prolog
            if (data.Length < 2)
                throw new Exception(SR.Format(SR.Exception_InvalidCustomAttributeLength, data.Length));
            if ((data[0] != 0x1) || (data[1] != 0x00))
                throw new Exception(SR.Exception_InvalidProlog);
            pos = 2;

            ParameterInfo[] pi = GetParameters(ctor);
            info.ctor = ctor;
            info.ctorArgs = new object?[pi.Length];
            for (int i = 0; i < pi.Length; ++i)
                info.ctorArgs[i] = decode_cattr_value(pi[i].ParameterType, data, pos, out pos);

            int num_named = data[pos] + (data[pos + 1] * 256);
            pos += 2;

            info.namedParamNames = new string[num_named];
            info.namedParamValues = new object[num_named];
            for (int i = 0; i < num_named; ++i)
            {
                int named_type = data[pos++];
                int data_type = data[pos++];
                string? enum_type_name = null;

                if (data_type == 0x55)
                {
                    int len2 = decode_len(data, pos, out pos);
                    enum_type_name = string_from_bytes(data, pos, len2);
                    pos += len2;
                }

                int len = decode_len(data, pos, out pos);
                string name = string_from_bytes(data, pos, len);
                info.namedParamNames[i] = name;
                pos += len;

                if (named_type == 0x53)
                {
                    /* Field */
                    FieldInfo? fi = ctor.DeclaringType!.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fi == null)
                        throw new Exception(SR.Format(SR.Exception_EmptyFieldForCustomAttributeType, ctor.DeclaringType, name));

                    object? val = decode_cattr_value(fi.FieldType, data, pos, out pos);
                    if (enum_type_name != null)
                    {
                        Type enumType = Type.GetType(enum_type_name)!;
                        val = Enum.ToObject(enumType, val!);
                    }

                    info.namedParamValues[i] = val;
                }
                else
                    // FIXME:
                    throw new Exception(SR.Format(SR.Exception_UnknownNamedType, named_type));
            }

            return info;
        }

        private static ParameterInfo[] GetParameters(ConstructorInfo ctor)
        {
            if (ctor is ConstructorBuilder cb)
                return cb.GetParametersInternal();

            return ctor.GetParametersInternal();
        }
    }
}
#endif
