// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
namespace System.Reflection.Emit {
    
    
    using System;
    using System.Reflection;
    using System.IO;
    using System.Text;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Globalization;
    using System.Diagnostics.Contracts;
    
    [HostProtection(MayLeakOnAbort = true)]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_CustomAttributeBuilder))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class CustomAttributeBuilder : _CustomAttributeBuilder
    {
        // public constructor to form the custom attribute with constructor and constructor
        // parameters.
        public CustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs)
        {
            InitCustomAttributeBuilder(con, constructorArgs,
                                       new PropertyInfo[]{}, new Object[]{},
                                       new FieldInfo[]{}, new Object[]{});
        }
    
        // public constructor to form the custom attribute with constructor, constructor
        // parameters and named properties.
        public CustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs,
                                      PropertyInfo[] namedProperties, Object[] propertyValues)
        {
            InitCustomAttributeBuilder(con, constructorArgs, namedProperties,
                                       propertyValues, new FieldInfo[]{}, new Object[]{});
        }

        // public constructor to form the custom attribute with constructor and constructor
        // parameters.
        public CustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs,
                                      FieldInfo[] namedFields, Object[] fieldValues)
        {
            InitCustomAttributeBuilder(con, constructorArgs, new PropertyInfo[]{},
                                       new Object[]{}, namedFields, fieldValues);
        }

        // public constructor to form the custom attribute with constructor and constructor
        // parameters.
        public CustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs,
                                      PropertyInfo[] namedProperties, Object[] propertyValues,
                                      FieldInfo[] namedFields, Object[] fieldValues)
        {
            InitCustomAttributeBuilder(con, constructorArgs, namedProperties,
                                       propertyValues, namedFields, fieldValues);
        }

        // Check that a type is suitable for use in a custom attribute.
        private bool ValidateType(Type t)
        {
            if (t.IsPrimitive || t == typeof(String) || t == typeof(Type))
                return true;
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
                if (t.GetArrayRank() != 1)
                    return false;
                return ValidateType(t.GetElementType());
            }
            return t == typeof(Object);
        }

        internal void InitCustomAttributeBuilder(ConstructorInfo con, Object[] constructorArgs,
                                                 PropertyInfo[] namedProperties, Object[] propertyValues,
                                                 FieldInfo[] namedFields, Object[] fieldValues)
        {
            if (con == null)
                throw new ArgumentNullException("con");
            if (constructorArgs == null)
                throw new ArgumentNullException("constructorArgs");
            if (namedProperties == null)
                throw new ArgumentNullException("namedProperties");
            if (propertyValues == null)
                throw new ArgumentNullException("propertyValues");
            if (namedFields == null)
                throw new ArgumentNullException("namedFields");
            if (fieldValues == null)
                throw new ArgumentNullException("fieldValues");
            if (namedProperties.Length != propertyValues.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayLengthsDiffer"), "namedProperties, propertyValues");
            if (namedFields.Length != fieldValues.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_ArrayLengthsDiffer"), "namedFields, fieldValues");
            Contract.EndContractBlock();

            if ((con.Attributes & MethodAttributes.Static) == MethodAttributes.Static ||
                (con.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadConstructor"));
                                               
            if ((con.CallingConvention & CallingConventions.Standard) != CallingConventions.Standard)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadConstructorCallConv"));

            // Cache information used elsewhere.
            m_con = con;
            m_constructorArgs = new Object[constructorArgs.Length];
            Array.Copy(constructorArgs, 0, m_constructorArgs, 0, constructorArgs.Length);

            Type[] paramTypes;
            int i;

            // Get the types of the constructor's formal parameters.
            paramTypes = con.GetParameterTypes();

            // Since we're guaranteed a non-var calling convention, the number of arguments must equal the number of parameters.
            if (paramTypes.Length != constructorArgs.Length)
                throw new ArgumentException(Environment.GetResourceString("Argument_BadParameterCountsForConstructor"));

            // Verify that the constructor has a valid signature (custom attributes only support a subset of our type system).
            for (i = 0; i < paramTypes.Length; i++)
                if (!ValidateType(paramTypes[i]))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeInCustomAttribute"));

            // Now verify that the types of the actual parameters are compatible with the types of the formal parameters.
            for (i = 0; i < paramTypes.Length; i++)
            {
                if (constructorArgs[i] == null)
                    continue;
                TypeCode paramTC = Type.GetTypeCode(paramTypes[i]);
                if (paramTC != Type.GetTypeCode(constructorArgs[i].GetType()))
                    if (paramTC != TypeCode.Object || !ValidateType(constructorArgs[i].GetType())) 
                        throw new ArgumentException(Environment.GetResourceString("Argument_BadParameterTypeForConstructor", i));
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
                if (namedProperties[i] == null)
                    throw new ArgumentNullException("namedProperties[" + i + "]");

                // Allow null for non-primitive types only.
                Type propType = namedProperties[i].PropertyType;
                if (propertyValues[i] == null && propType.IsPrimitive)
                    throw new ArgumentNullException("propertyValues[" + i + "]");

                // Validate property type.
                if (!ValidateType(propType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeInCustomAttribute"));

                // Property has to be writable.
                if (!namedProperties[i].CanWrite)                
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotAWritableProperty"));
                    
                // Property has to be from the same class or base class as ConstructorInfo.
                if (namedProperties[i].DeclaringType != con.DeclaringType
                    && (!(con.DeclaringType is TypeBuilderInstantiation))
                    && !con.DeclaringType.IsSubclassOf(namedProperties[i].DeclaringType))
                {
                    // Might have failed check because one type is a XXXBuilder
                    // and the other is not. Deal with these special cases
                    // separately.
                    if (!TypeBuilder.IsTypeEqual(namedProperties[i].DeclaringType, con.DeclaringType))
                    {
                        // IsSubclassOf is overloaded to do the right thing if
                        // the constructor is a TypeBuilder, but we still need
                        // to deal with the case where the property's declaring
                        // type is one.
                        if (!(namedProperties[i].DeclaringType is TypeBuilder) ||
                            !con.DeclaringType.IsSubclassOf(((TypeBuilder)namedProperties[i].DeclaringType).BakedRuntimeType))
                            throw new ArgumentException(Environment.GetResourceString("Argument_BadPropertyForConstructorBuilder"));
                    }
                }

                // Make sure the property's type can take the given value.
                // Note that there will be no coersion.
                if (propertyValues[i] != null &&
                    propType != typeof(Object) &&
                    Type.GetTypeCode(propertyValues[i].GetType()) != Type.GetTypeCode(propType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                
                // First a byte indicating that this is a property.
                writer.Write((byte)CustomAttributeEncoding.Property);

                // Emit the property type, name and value.
                EmitType(writer, propType);
                EmitString(writer, namedProperties[i].Name);
                EmitValue(writer, propType, propertyValues[i]);
            }

            // Emit all the field sets.
            for (i = 0; i < namedFields.Length; i++)
            {
                // Validate the field.
                if (namedFields[i] == null)
                    throw new ArgumentNullException("namedFields[" + i + "]");

                // Allow null for non-primitive types only.
                Type fldType = namedFields[i].FieldType;
                if (fieldValues[i] == null && fldType.IsPrimitive)
                    throw new ArgumentNullException("fieldValues[" + i + "]");

                // Validate field type.
                if (!ValidateType(fldType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadTypeInCustomAttribute"));

                // Field has to be from the same class or base class as ConstructorInfo.
                if (namedFields[i].DeclaringType != con.DeclaringType
                    && (!(con.DeclaringType is TypeBuilderInstantiation))
                    && !con.DeclaringType.IsSubclassOf(namedFields[i].DeclaringType))
                {
                    // Might have failed check because one type is a XXXBuilder
                    // and the other is not. Deal with these special cases
                    // separately.
                    if (!TypeBuilder.IsTypeEqual(namedFields[i].DeclaringType, con.DeclaringType))
                    {
                        // IsSubclassOf is overloaded to do the right thing if
                        // the constructor is a TypeBuilder, but we still need
                        // to deal with the case where the field's declaring
                        // type is one.
                        if (!(namedFields[i].DeclaringType is TypeBuilder) ||
                            !con.DeclaringType.IsSubclassOf(((TypeBuilder)namedFields[i].DeclaringType).BakedRuntimeType))
                            throw new ArgumentException(Environment.GetResourceString("Argument_BadFieldForConstructorBuilder"));
                    }
                }

                // Make sure the field's type can take the given value.
                // Note that there will be no coersion.
                if (fieldValues[i] != null &&
                    fldType != typeof(Object) &&
                    Type.GetTypeCode(fieldValues[i].GetType()) != Type.GetTypeCode(fldType))
                    throw new ArgumentException(Environment.GetResourceString("Argument_ConstantDoesntMatch"));
                
                // First a byte indicating that this is a field.
                writer.Write((byte)CustomAttributeEncoding.Field);

                // Emit the field type, name and value.
                EmitType(writer, fldType);
                EmitString(writer, namedFields[i].Name);
                EmitValue(writer, fldType, fieldValues[i]);
            }

            // Create the blob array.
            m_blob = ((MemoryStream)writer.BaseStream).ToArray();
        }

        private void EmitType(BinaryWriter writer, Type type)
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
                        Contract.Assert(false, "Invalid primitive type");
                        break;
                }
            }
            else if (type.IsEnum)
            {
                writer.Write((byte)CustomAttributeEncoding.Enum);
                EmitString(writer, type.AssemblyQualifiedName);
            }
            else if (type == typeof(String))
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
                EmitType(writer, type.GetElementType());
            }
            else
            {
                // Tagged object case.
                writer.Write((byte)CustomAttributeEncoding.Object);
            }
        }

        private void EmitString(BinaryWriter writer, String str)
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
                writer.Write((byte)((length >> 8) | 0x80));
                writer.Write((byte)(length & 0xff));
            }
            else
            {
                writer.Write((byte)((length >> 24) | 0xc0));
                writer.Write((byte)((length >> 16) & 0xff));
                writer.Write((byte)((length >> 8) & 0xff));
                writer.Write((byte)(length & 0xff));
            }
            writer.Write(utf8Str);
        }

        private void EmitValue(BinaryWriter writer, Type type, Object value)
        {
            if (type.IsEnum)
            {
                switch (Type.GetTypeCode(Enum.GetUnderlyingType(type)))
                {
                    case TypeCode.SByte:
                        writer.Write((sbyte)value);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)value);
                        break;
                    case TypeCode.Int16:
                        writer.Write((short)value);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((ushort)value);
                        break;
                    case TypeCode.Int32:
                        writer.Write((int)value);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((uint)value);
                        break;
                    case TypeCode.Int64:
                        writer.Write((long)value);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((ulong)value);
                        break;
                    default:
                        Contract.Assert(false, "Invalid enum base type");
                        break;
                }
            }
            else if (type == typeof(String))
            {
                if (value == null)
                    writer.Write((byte)0xff);
                else
                    EmitString(writer, (String)value);
            }
            else if (type == typeof(Type))
            {
                if (value == null)
                    writer.Write((byte)0xff);
                else
                {
                    String typeName = TypeNameBuilder.ToString((Type)value, TypeNameBuilder.Format.AssemblyQualifiedName);
                    if (typeName == null)
                        throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeForCA",
                                                                  value.GetType()));
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
                    Type et = type.GetElementType();
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
                        writer.Write((sbyte)value);
                        break;
                    case TypeCode.Byte:
                        writer.Write((byte)value);
                        break;
                    case TypeCode.Char:
                        writer.Write(Convert.ToUInt16((char)value));
                        break;
                    case TypeCode.Boolean:
                        writer.Write((byte)((bool)value ? 1 : 0));
                        break;
                    case TypeCode.Int16:
                        writer.Write((short)value);
                        break;
                    case TypeCode.UInt16:
                        writer.Write((ushort)value);
                        break;
                    case TypeCode.Int32:
                        writer.Write((int)value);
                        break;
                    case TypeCode.UInt32:
                        writer.Write((uint)value);
                        break;
                    case TypeCode.Int64:
                        writer.Write((long)value);
                        break;
                    case TypeCode.UInt64:
                        writer.Write((ulong)value);
                        break;
                    case TypeCode.Single:
                        writer.Write((float)value);
                        break;
                    case TypeCode.Double:
                        writer.Write((double)value);
                        break;
                    default:
                        Contract.Assert(false, "Invalid primitive type");
                        break;
                }
            }
            else if (type == typeof(object))
            {
                // Tagged object case. Type instances aren't actually Type, they're some subclass (such as RuntimeType or
                // TypeBuilder), so we need to canonicalize this case back to Type. If we have a null value we follow the convention
                // used by C# and emit a null typed as a string (it doesn't really matter what type we pick as long as it's a
                // reference type).
                Type ot = value == null ? typeof(String) : value is Type ? typeof(Type) : value.GetType();

                // value cannot be a "System.Object" object.
                // If we allow this we will get into an infinite recursion
                if (ot == typeof(object))
                    throw new ArgumentException(Environment.GetResourceString("Argument_BadParameterTypeForCAB", ot.ToString()));
                
                EmitType(writer, ot);
                EmitValue(writer, ot, value);
            }
            else
            {
                string typename = "null";

                if (value != null)
                    typename = value.GetType().ToString();
                
                throw new ArgumentException(Environment.GetResourceString("Argument_BadParameterTypeForCAB", typename));
            }
        }

          
         

        // return the byte interpretation of the custom attribute
        [System.Security.SecurityCritical]  // auto-generated
        internal void CreateCustomAttribute(ModuleBuilder mod, int tkOwner)
        {
            CreateCustomAttribute(mod, tkOwner, mod.GetConstructorToken(m_con).Token, false);
        }

        //*************************************************
        // Upon saving to disk, we need to create the memberRef token for the custom attribute's type
        // first of all. So when we snap the in-memory module for on disk, this token will be there.
        // We also need to enforce the use of MemberRef. Because MemberDef token might move. 
        // This function has to be called before we snap the in-memory module for on disk (i.e. Presave on
        // ModuleBuilder.
        //*************************************************
        [System.Security.SecurityCritical]  // auto-generated
        internal int PrepareCreateCustomAttributeToDisk(ModuleBuilder mod)
        {
            return mod.InternalGetConstructorToken(m_con, true).Token;
        }

        //*************************************************
        // Call this function with toDisk=1, after on disk module has been snapped.
        //*************************************************
        [System.Security.SecurityCritical]  // auto-generated
        internal void CreateCustomAttribute(ModuleBuilder mod, int tkOwner, int tkAttrib, bool toDisk)
        {
            TypeBuilder.DefineCustomAttribute(mod, tkOwner, tkAttrib, m_blob, toDisk, 
                                                      typeof(System.Diagnostics.DebuggableAttribute) == m_con.DeclaringType);
        }

#if !FEATURE_CORECLR
        void _CustomAttributeBuilder.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _CustomAttributeBuilder.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _CustomAttributeBuilder.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _CustomAttributeBuilder.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif

        internal ConstructorInfo    m_con;
        internal Object[]           m_constructorArgs;
        internal byte[]             m_blob;
    }
}
