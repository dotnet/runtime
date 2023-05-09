// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
**
** CustomAttributeBuilder is a helper class to help building custom attribute.
**
**
===========================================================*/

using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System.Reflection.Emit
{
    public class CustomAttributeBuilder
    {
        private readonly ConstructorInfo m_con;
        private readonly object?[] m_constructorArgs;
        private readonly byte[] m_blob;

        internal ConstructorInfo Ctor => m_con;

        internal byte[] Data => m_blob;

        // public constructor to form the custom attribute with constructor and constructor
        // parameters.
        public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs) :
            this(con, constructorArgs, Array.Empty<PropertyInfo>(), Array.Empty<object>(), Array.Empty<FieldInfo>(), Array.Empty<object>())
        {
        }

        // public constructor to form the custom attribute with constructor, constructor
        // parameters and named properties.
        public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs, PropertyInfo[] namedProperties, object?[] propertyValues) :
            this(con, constructorArgs, namedProperties, propertyValues, Array.Empty<FieldInfo>(), Array.Empty<object>())
        {
        }

        // public constructor to form the custom attribute with constructor and constructor
        // parameters.
        public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs, FieldInfo[] namedFields, object?[] fieldValues) :
            this(con, constructorArgs, Array.Empty<PropertyInfo>(), Array.Empty<object>(), namedFields, fieldValues)
        {
        }

        // public constructor to form the custom attribute with constructor and constructor
        // parameters.
        public CustomAttributeBuilder(ConstructorInfo con, object?[] constructorArgs, PropertyInfo[] namedProperties, object?[] propertyValues, FieldInfo[] namedFields, object?[] fieldValues)
        {
            ArgumentNullException.ThrowIfNull(con);
            ArgumentNullException.ThrowIfNull(constructorArgs);
            ArgumentNullException.ThrowIfNull(namedProperties);
            ArgumentNullException.ThrowIfNull(propertyValues);
            ArgumentNullException.ThrowIfNull(namedFields);
            ArgumentNullException.ThrowIfNull(fieldValues);

            AssemblyBuilder.EnsureDynamicCodeSupported();

#pragma warning disable CA2208 // Instantiate argument exceptions correctly, combination of arguments used
            if (namedProperties.Length != propertyValues.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer, "namedProperties, propertyValues");
            if (namedFields.Length != fieldValues.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer, "namedFields, fieldValues");
#pragma warning restore CA2208

            if ((con.Attributes & MethodAttributes.Static) == MethodAttributes.Static ||
                (con.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
                throw new ArgumentException(SR.Argument_BadConstructor);

            if ((con.CallingConvention & CallingConventions.Standard) != CallingConventions.Standard)
                throw new ArgumentException(SR.Argument_BadConstructorCallConv);

            // Cache information used elsewhere.
            m_con = con;
            m_constructorArgs = new object?[constructorArgs.Length];
            Array.Copy(constructorArgs, m_constructorArgs, constructorArgs.Length);

            Type[] paramTypes;
            int i;

            // Get the types of the constructor's formal parameters.
            paramTypes = con.GetParameterTypes();

            // Since we're guaranteed a non-var calling convention, the number of arguments must equal the number of parameters.
            if (paramTypes.Length != constructorArgs.Length)
                throw new ArgumentException(SR.Argument_BadParameterCountsForConstructor);

            // Verify that the constructor has a valid signature (custom attributes only support a subset of our type system).
            for (i = 0; i < paramTypes.Length; i++)
                if (!ValidateType(paramTypes[i]))
                    throw new ArgumentException(SR.Argument_BadTypeInCustomAttribute);

            // Now verify that the types of the actual parameters are compatible with the types of the formal parameters.
            for (i = 0; i < paramTypes.Length; i++)
            {
                object? constructorArg = constructorArgs[i];
                if (constructorArg == null)
                {
                    if (paramTypes[i].IsValueType)
                    {
                        throw new ArgumentNullException($"{nameof(constructorArgs)}[{i}]");
                    }
                    continue;
                }
                VerifyTypeAndPassedObjectType(paramTypes[i], constructorArg.GetType(), $"{nameof(constructorArgs)}[{i}]");
            }

            // Allocate a memory stream to represent the CA blob in the metadata and a binary writer to help format it.
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);

            // Write the blob protocol version (currently 1).
            writer.Write((ushort)1);

            // Now emit the constructor argument values (no need for types, they're inferred from the constructor signature).
            for (i = 0; i < constructorArgs.Length; i++)
                EmitValue(writer, paramTypes[i], constructorArgs[i]);

            // Next a short with the count of properties and fields.
            writer.Write((ushort)(namedProperties.Length + namedFields.Length));

            // Emit all the property sets.
            for (i = 0; i < namedProperties.Length; i++)
            {
                // Validate the property.
                PropertyInfo property = namedProperties[i];
                if (property == null)
                    throw new ArgumentNullException("namedProperties[" + i + "]");

                // Allow null for non-primitive types only.
                Type propType = property.PropertyType;
                object? propertyValue = propertyValues[i];
                if (propertyValue == null && propType.IsValueType)
                    throw new ArgumentNullException("propertyValues[" + i + "]");

                // Validate property type.
                if (!ValidateType(propType))
                    throw new ArgumentException(SR.Argument_BadTypeInCustomAttribute);

                // Property has to be writable.
                if (!property.CanWrite)
                    throw new ArgumentException(SR.Argument_NotAWritableProperty);

                // Property has to be from the same class or base class as ConstructorInfo.
                if (property.DeclaringType != con.DeclaringType
                    && (!(con.DeclaringType is TypeBuilderInstantiation))
                    && !con.DeclaringType!.IsSubclassOf(property.DeclaringType!))
                {
                    // Might have failed check because one type is a XXXBuilder
                    // and the other is not. Deal with these special cases
                    // separately.
                    if (!RuntimeTypeBuilder.IsTypeEqual(property.DeclaringType, con.DeclaringType))
                    {
                        // IsSubclassOf is overloaded to do the right thing if
                        // the constructor is a TypeBuilder, but we still need
                        // to deal with the case where the property's declaring
                        // type is one.
                        if (!(property.DeclaringType is TypeBuilder) ||
                            !con.DeclaringType.IsSubclassOf(((RuntimeTypeBuilder)property.DeclaringType).BakedRuntimeType))
                            throw new ArgumentException(SR.Argument_BadPropertyForConstructorBuilder);
                    }
                }

                // Make sure the property's type can take the given value.
                // Note that there will be no coercion.
                if (propertyValue != null)
                {
                    VerifyTypeAndPassedObjectType(propType, propertyValue.GetType(), $"{nameof(propertyValues)}[{i}]");
                }

                // First a byte indicating that this is a property.
                writer.Write((byte)CustomAttributeEncoding.Property);

                // Emit the property type, name and value.
                EmitType(writer, propType);
                EmitString(writer, namedProperties[i].Name);
                EmitValue(writer, propType, propertyValue);
            }

            // Emit all the field sets.
            for (i = 0; i < namedFields.Length; i++)
            {
                // Validate the field.
                FieldInfo namedField = namedFields[i];
                if (namedField == null)
                    throw new ArgumentNullException("namedFields[" + i + "]");

                // Allow null for non-primitive types only.
                Type fldType = namedField.FieldType;
                object? fieldValue = fieldValues[i];
                if (fieldValue == null && fldType.IsValueType)
                    throw new ArgumentNullException("fieldValues[" + i + "]");

                // Validate field type.
                if (!ValidateType(fldType))
                    throw new ArgumentException(SR.Argument_BadTypeInCustomAttribute);

                // Field has to be from the same class or base class as ConstructorInfo.
                if (namedField.DeclaringType != con.DeclaringType
                    && (!(con.DeclaringType is TypeBuilderInstantiation))
                    && !con.DeclaringType!.IsSubclassOf(namedField.DeclaringType!))
                {
                    // Might have failed check because one type is a XXXBuilder
                    // and the other is not. Deal with these special cases
                    // separately.
                    if (!RuntimeTypeBuilder.IsTypeEqual(namedField.DeclaringType, con.DeclaringType))
                    {
                        // IsSubclassOf is overloaded to do the right thing if
                        // the constructor is a TypeBuilder, but we still need
                        // to deal with the case where the field's declaring
                        // type is one.
                        if (!(namedField.DeclaringType is TypeBuilder) ||
                            !con.DeclaringType.IsSubclassOf(((RuntimeTypeBuilder)namedFields[i].DeclaringType!).BakedRuntimeType))
                            throw new ArgumentException(SR.Argument_BadFieldForConstructorBuilder);
                    }
                }

                // Make sure the field's type can take the given value.
                // Note that there will be no coercion.
                if (fieldValue != null)
                {
                    VerifyTypeAndPassedObjectType(fldType, fieldValue.GetType(), $"{nameof(fieldValues)}[{i}]");
                }

                // First a byte indicating that this is a field.
                writer.Write((byte)CustomAttributeEncoding.Field);

                // Emit the field type, name and value.
                EmitType(writer, fldType);
                EmitString(writer, namedField.Name);
                EmitValue(writer, fldType, fieldValue);
            }

            // Create the blob array.
            m_blob = ((MemoryStream)writer.BaseStream).ToArray();
        }

        // Check that a type is suitable for use in a custom attribute.
        private bool ValidateType(Type t)
        {
            if (t.IsPrimitive)
            {
                return t != typeof(IntPtr) && t != typeof(UIntPtr);
            }
            if (t == typeof(string) || t == typeof(Type))
            {
                return true;
            }
            if (t.IsEnum)
            {
                switch (Type.GetTypeCode(Enum.GetUnderlyingType(t)))
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        return true;
                    default:
                        return false;
                }
            }
            if (t.IsArray)
            {
                return t.GetArrayRank() == 1 && ValidateType(t.GetElementType()!);
            }
            return t == typeof(object);
        }

        private static void VerifyTypeAndPassedObjectType(Type type, Type passedType, string paramName)
        {
            if (type != typeof(object) && Type.GetTypeCode(passedType) != Type.GetTypeCode(type))
            {
                throw new ArgumentException(SR.Argument_ConstantDoesntMatch);
            }
            if (passedType == typeof(IntPtr) || passedType == typeof(UIntPtr))
            {
                throw new ArgumentException(SR.Format(SR.Argument_BadParameterTypeForCAB, passedType), paramName);
            }
        }

        private static void EmitType(BinaryWriter writer, Type type)
        {
            if (type.IsPrimitive)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.SByte:
                        writer.Write((byte)CustomAttributeEncoding.SByte);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)CustomAttributeEncoding.Byte);
                        break;
                    case TypeCode.Char:
                        writer.Write((byte)CustomAttributeEncoding.Char);
                        break;
                    case TypeCode.Boolean:
                        writer.Write((byte)CustomAttributeEncoding.Boolean);
                        break;
                    case TypeCode.Int16:
                        writer.Write((byte)CustomAttributeEncoding.Int16);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((byte)CustomAttributeEncoding.UInt16);
                        break;
                    case TypeCode.Int32:
                        writer.Write((byte)CustomAttributeEncoding.Int32);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((byte)CustomAttributeEncoding.UInt32);
                        break;
                    case TypeCode.Int64:
                        writer.Write((byte)CustomAttributeEncoding.Int64);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((byte)CustomAttributeEncoding.UInt64);
                        break;
                    case TypeCode.Single:
                        writer.Write((byte)CustomAttributeEncoding.Float);
                        break;
                    case TypeCode.Double:
                        writer.Write((byte)CustomAttributeEncoding.Double);
                        break;
                    default:
                        Debug.Fail("Invalid primitive type");
                        break;
                }
            }
            else if (type.IsEnum)
            {
                writer.Write((byte)CustomAttributeEncoding.Enum);
                EmitString(writer, type.AssemblyQualifiedName!);
            }
            else if (type == typeof(string))
            {
                writer.Write((byte)CustomAttributeEncoding.String);
            }
            else if (type == typeof(Type))
            {
                writer.Write((byte)CustomAttributeEncoding.Type);
            }
            else if (type.IsArray)
            {
                writer.Write((byte)CustomAttributeEncoding.Array);
                EmitType(writer, type.GetElementType()!);
            }
            else
            {
                // Tagged object case.
                writer.Write((byte)CustomAttributeEncoding.Object);
            }
        }

        private static void EmitString(BinaryWriter writer, string str)
        {
            // Strings are emitted with a length prefix in a compressed format (1, 2 or 4 bytes) as used internally by metadata.
            byte[] utf8Str = Encoding.UTF8.GetBytes(str);
            uint length = (uint)utf8Str.Length;
            if (length <= 0x7f)
            {
                writer.Write((byte)length);
            }
            else if (length <= 0x3fff)
            {
                writer.Write(BinaryPrimitives.ReverseEndianness((short)(length | 0x80_00)));
            }
            else
            {
                writer.Write(BinaryPrimitives.ReverseEndianness(length | 0xC0_00_00_00));
            }
            writer.Write(utf8Str);
        }

        private static void EmitValue(BinaryWriter writer, Type type, object? value)
        {
            if (type.IsEnum)
            {
                switch (Type.GetTypeCode(Enum.GetUnderlyingType(type)))
                {
                    case TypeCode.SByte:
                        writer.Write((sbyte)value!);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)value!);
                        break;
                    case TypeCode.Int16:
                        writer.Write((short)value!);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((ushort)value!);
                        break;
                    case TypeCode.Int32:
                        writer.Write((int)value!);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((uint)value!);
                        break;
                    case TypeCode.Int64:
                        writer.Write((long)value!);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((ulong)value!);
                        break;
                    default:
                        Debug.Fail("Invalid enum base type");
                        break;
                }
            }
            else if (type == typeof(string))
            {
                if (value == null)
                    writer.Write((byte)0xff);
                else
                    EmitString(writer, (string)value);
            }
            else if (type == typeof(Type))
            {
                if (value == null)
                    writer.Write((byte)0xff);
                else
                {
                    string? typeName = TypeNameBuilder.ToString((Type)value, TypeNameBuilder.Format.AssemblyQualifiedName);
                    if (typeName == null)
                        throw new ArgumentException(SR.Format(SR.Argument_InvalidTypeForCA, value.GetType()));
                    EmitString(writer, typeName);
                }
            }
            else if (type.IsArray)
            {
                if (value == null)
                    writer.Write((uint)0xffffffff);
                else
                {
                    Array a = (Array)value;
                    Type et = type.GetElementType()!;
                    writer.Write(a.Length);
                    for (int i = 0; i < a.Length; i++)
                        EmitValue(writer, et, a.GetValue(i));
                }
            }
            else if (type.IsPrimitive)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.SByte:
                        writer.Write((sbyte)value!);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)value!);
                        break;
                    case TypeCode.Char:
                        writer.Write(Convert.ToUInt16((char)value!));
                        break;
                    case TypeCode.Boolean:
                        writer.Write((byte)((bool)value! ? 1 : 0));
                        break;
                    case TypeCode.Int16:
                        writer.Write((short)value!);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((ushort)value!);
                        break;
                    case TypeCode.Int32:
                        writer.Write((int)value!);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((uint)value!);
                        break;
                    case TypeCode.Int64:
                        writer.Write((long)value!);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((ulong)value!);
                        break;
                    case TypeCode.Single:
                        writer.Write((float)value!);
                        break;
                    case TypeCode.Double:
                        writer.Write((double)value!);
                        break;
                    default:
                        Debug.Fail("Invalid primitive type");
                        break;
                }
            }
            else if (type == typeof(object))
            {
                // Tagged object case. Type instances aren't actually Type, they're some subclass (such as RuntimeType or
                // TypeBuilder), so we need to canonicalize this case back to Type. If we have a null value we follow the convention
                // used by C# and emit a null typed as a string (it doesn't really matter what type we pick as long as it's a
                // reference type).
                Type ot = value == null ? typeof(string) : value is Type ? typeof(Type) : value.GetType();

                // value cannot be a "System.Object" object.
                // If we allow this we will get into an infinite recursion
                if (ot == typeof(object))
                    throw new ArgumentException(SR.Format(SR.Argument_BadParameterTypeForCAB, ot));

                EmitType(writer, ot);
                EmitValue(writer, ot, value);
            }
            else
            {
                string typename = "null";

                if (value != null)
                    typename = value.GetType().ToString();

                throw new ArgumentException(SR.Format(SR.Argument_BadParameterTypeForCAB, typename));
            }
        }

        // return the byte interpretation of the custom attribute
        internal void CreateCustomAttribute(RuntimeModuleBuilder mod, int tkOwner)
        {
            RuntimeTypeBuilder.DefineCustomAttribute(mod, tkOwner, mod.GetMethodMetadataToken(m_con), m_blob);
        }
    }
}
