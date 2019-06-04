// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** EnumBuilder is a helper class to build Enum ( a special type ). 
**
** 
===========================================================*/

using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection.Emit
{
    sealed public class EnumBuilder : TypeInfo
    {
        public override bool IsAssignableFrom(TypeInfo? typeInfo)
        {
            if (typeInfo == null) return false;
            return IsAssignableFrom(typeInfo.AsType());
        }

        // Define literal for enum

        public FieldBuilder DefineLiteral(string literalName, object? literalValue)
        {
            // Define the underlying field for the enum. It will be a non-static, private field with special name bit set. 
            FieldBuilder fieldBuilder = m_typeBuilder.DefineField(
                literalName,
                this,
                FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal);
            fieldBuilder.SetConstant(literalValue);
            return fieldBuilder;
        }

        public TypeInfo? CreateTypeInfo()
        {
            return m_typeBuilder.CreateTypeInfo();
        }

        // CreateType cause EnumBuilder to be baked.
        public Type? CreateType()
        {
            return m_typeBuilder.CreateType();
        }

        // Get the internal metadata token for this class.
        public TypeToken TypeToken
        {
            get { return m_typeBuilder.TypeToken; }
        }


        // return the underlying field for the enum
        public FieldBuilder UnderlyingField
        {
            get { return m_underlyingField; }
        }

        public override string Name
        {
            get { return m_typeBuilder.Name; }
        }

        /// <summary>
        /// abstract methods defined in the base class
        /// </summary>
        public override Guid GUID
        {
            get
            {
                return m_typeBuilder.GUID;
            }
        }

        public override object? InvokeMember(
            string name,
            BindingFlags invokeAttr,
            Binder? binder,
            object? target,
            object?[]? args,
            ParameterModifier[]? modifiers,
            CultureInfo? culture,
            string[]? namedParameters)
        {
            return m_typeBuilder.InvokeMember(name, invokeAttr, binder, target, args, modifiers, culture, namedParameters);
        }

        public override Module Module
        {
            get { return m_typeBuilder.Module; }
        }

        public override Assembly Assembly
        {
            get { return m_typeBuilder.Assembly; }
        }

        public override RuntimeTypeHandle TypeHandle
        {
            get { return m_typeBuilder.TypeHandle; }
        }

        public override string? FullName
        {
            get { return m_typeBuilder.FullName; }
        }

        public override string? AssemblyQualifiedName
        {
            get
            {
                return m_typeBuilder.AssemblyQualifiedName;
            }
        }

        public override string? Namespace
        {
            get { return m_typeBuilder.Namespace; }
        }

        public override Type? BaseType
        {
            get { return m_typeBuilder.BaseType; }
        }

        protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
        {
            return m_typeBuilder.GetConstructor(bindingAttr, binder, callConvention,
                            types, modifiers);
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetConstructors(bindingAttr);
        }

        protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder,
                CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
        {
            if (types == null)
                return m_typeBuilder.GetMethod(name, bindingAttr);
            else
                return m_typeBuilder.GetMethod(name, bindingAttr, binder, callConvention, types, modifiers);
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetMethods(bindingAttr);
        }

        public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetField(name, bindingAttr);
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetFields(bindingAttr);
        }

        public override Type? GetInterface(string name, bool ignoreCase)
        {
            return m_typeBuilder.GetInterface(name, ignoreCase);
        }

        public override Type[] GetInterfaces()
        {
            return m_typeBuilder.GetInterfaces();
        }

        public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetEvent(name, bindingAttr);
        }

        public override EventInfo[] GetEvents()
        {
            return m_typeBuilder.GetEvents();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder,
                Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetProperties(bindingAttr);
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetNestedTypes(bindingAttr);
        }

        public override Type? GetNestedType(string name, BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetNestedType(name, bindingAttr);
        }

        public override MemberInfo[] GetMember(string name, MemberTypes type, BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetMember(name, type, bindingAttr);
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetMembers(bindingAttr);
        }

        public override InterfaceMapping GetInterfaceMap(Type interfaceType)
        {
            return m_typeBuilder.GetInterfaceMap(interfaceType);
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            return m_typeBuilder.GetEvents(bindingAttr);
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            return m_typeBuilder.Attributes;
        }

        public override bool IsTypeDefinition => true;

        public override bool IsSZArray => false;

        protected override bool IsArrayImpl()
        {
            return false;
        }
        protected override bool IsPrimitiveImpl()
        {
            return false;
        }

        protected override bool IsValueTypeImpl()
        {
            return true;
        }

        protected override bool IsByRefImpl()
        {
            return false;
        }

        protected override bool IsPointerImpl()
        {
            return false;
        }

        protected override bool IsCOMObjectImpl()
        {
            return false;
        }

        public override bool IsConstructedGenericType
        {
            get
            {
                return false;
            }
        }

        public override Type? GetElementType()
        {
            return m_typeBuilder.GetElementType();
        }

        protected override bool HasElementTypeImpl()
        {
            return m_typeBuilder.HasElementType;
        }

        // Legacy: JScript needs it.
        public override Type GetEnumUnderlyingType()
        {
            return m_underlyingField.FieldType;
        }

        public override Type UnderlyingSystemType
        {
            get
            {
                return GetEnumUnderlyingType();
            }
        }

        //ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit)
        {
            return m_typeBuilder.GetCustomAttributes(inherit);
        }

        // Return a custom attribute identified by Type
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_typeBuilder.GetCustomAttributes(attributeType, inherit);
        }

        // Use this function if client decides to form the custom attribute blob themselves

        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            m_typeBuilder.SetCustomAttribute(con, binaryAttribute);
        }

        // Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            m_typeBuilder.SetCustomAttribute(customBuilder);
        }

        // Return the class that declared this Field.
        public override Type? DeclaringType
        {
            get { return m_typeBuilder.DeclaringType; }
        }

        // Return the class that was used to obtain this field.

        public override Type? ReflectedType
        {
            get { return m_typeBuilder.ReflectedType; }
        }


        // Returns true if one or more instance of attributeType is defined on this member. 
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return m_typeBuilder.IsDefined(attributeType, inherit);
        }

        /*****************************************************
         * 
         * private/protected functions
         * 
         */

        public override Type MakePointerType()
        {
            return SymbolType.FormCompoundType("*", this, 0)!;
        }

        public override Type MakeByRefType()
        {
            return SymbolType.FormCompoundType("&", this, 0)!;
        }

        public override Type MakeArrayType()
        {
            return SymbolType.FormCompoundType("[]", this, 0)!;
        }

        public override Type MakeArrayType(int rank)
        {
            if (rank <= 0)
                throw new IndexOutOfRangeException();

            string szrank = "";
            if (rank == 1)
            {
                szrank = "*";
            }
            else
            {
                for (int i = 1; i < rank; i++)
                    szrank += ",";
            }

            string s = string.Format(CultureInfo.InvariantCulture, "[{0}]", szrank); // [,,]
            return SymbolType.FormCompoundType(s, this, 0)!;
        }


        // Constructs a EnumBuilder.
        // EnumBuilder can only be a top-level (not nested) enum type.
        internal EnumBuilder(
            string name,                       // name of type
            Type underlyingType,             // underlying type for an Enum
            TypeAttributes visibility,              // any bits on TypeAttributes.VisibilityMask)
            ModuleBuilder module)                     // module containing this type
        {
            // Client should not set any bits other than the visibility bits.
            if ((visibility & ~TypeAttributes.VisibilityMask) != 0)
                throw new ArgumentException(SR.Argument_ShouldOnlySetVisibilityFlags, nameof(name));
            m_typeBuilder = new TypeBuilder(name, visibility | TypeAttributes.Sealed, typeof(System.Enum), null, module, PackingSize.Unspecified, TypeBuilder.UnspecifiedTypeSize, null);

            // Define the underlying field for the enum. It will be a non-static, private field with special name bit set. 
            m_underlyingField = m_typeBuilder.DefineField("value__", underlyingType, FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName);
        }

        /*****************************************************
         * 
         * private data members
         * 
         */
        internal TypeBuilder m_typeBuilder;
        private FieldBuilder m_underlyingField;
    }
}
