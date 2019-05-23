// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using CultureInfo = System.Globalization.CultureInfo;

namespace System.Reflection.Emit
{
    public sealed class FieldBuilder : FieldInfo
    {
        #region Private Data Members
        private int m_fieldTok;
        private FieldToken m_tkField;
        private TypeBuilder m_typeBuilder;
        private string m_fieldName;
        private FieldAttributes m_Attributes;
        private Type m_fieldType;
        #endregion

        #region Constructor
        internal FieldBuilder(TypeBuilder typeBuilder, string fieldName, Type type,
            Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes)
        {
            if (fieldName == null)
                throw new ArgumentNullException(nameof(fieldName));

            if (fieldName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(fieldName));

            if (fieldName[0] == '\0')
                throw new ArgumentException(SR.Argument_IllegalName, nameof(fieldName));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type == typeof(void))
                throw new ArgumentException(SR.Argument_BadFieldType);

            m_fieldName = fieldName;
            m_typeBuilder = typeBuilder;
            m_fieldType = type;
            m_Attributes = attributes & ~FieldAttributes.ReservedMask;

            SignatureHelper sigHelp = SignatureHelper.GetFieldSigHelper(m_typeBuilder.Module);
            sigHelp.AddArgument(type, requiredCustomModifiers, optionalCustomModifiers);

            int sigLength;
            byte[] signature = sigHelp.InternalGetSignature(out sigLength);

            ModuleBuilder module = m_typeBuilder.GetModuleBuilder();
            m_fieldTok = TypeBuilder.DefineField(JitHelpers.GetQCallModuleOnStack(ref module),
                typeBuilder.TypeToken.Token, fieldName, signature, sigLength, m_Attributes);

            m_tkField = new FieldToken(m_fieldTok, type);
        }

        #endregion

        #region Internal Members
        internal void SetData(byte[]? data, int size)
        {
            ModuleBuilder module = m_typeBuilder.GetModuleBuilder();
            ModuleBuilder.SetFieldRVAContent(JitHelpers.GetQCallModuleOnStack(ref module), m_tkField.Token, data, size);
        }
        #endregion

        #region MemberInfo Overrides
        internal int MetadataTokenInternal
        {
            get { return m_fieldTok; }
        }

        public override Module Module
        {
            get { return m_typeBuilder.Module; }
        }

        public override string Name
        {
            get { return m_fieldName; }
        }

        public override Type? DeclaringType
        {
            get
            {
                if (m_typeBuilder.m_isHiddenGlobalType == true)
                    return null;

                return m_typeBuilder;
            }
        }

        public override Type? ReflectedType
        {
            get
            {
                if (m_typeBuilder.m_isHiddenGlobalType == true)
                    return null;

                return m_typeBuilder;
            }
        }

        #endregion

        #region FieldInfo Overrides
        public override Type FieldType
        {
            get { return m_fieldType; }
        }

        public override object? GetValue(object? obj)
        {
            // NOTE!!  If this is implemented, make sure that this throws 
            // a NotSupportedException for Save-only dynamic assemblies.
            // Otherwise, it could cause the .cctor to be executed.

            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override void SetValue(object? obj, object? val, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            // NOTE!!  If this is implemented, make sure that this throws 
            // a NotSupportedException for Save-only dynamic assemblies.
            // Otherwise, it could cause the .cctor to be executed.

            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get { throw new NotSupportedException(SR.NotSupported_DynamicModule); }
        }

        public override FieldAttributes Attributes
        {
            get { return m_Attributes; }
        }

        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        }

        #endregion

        #region Public Members
        public FieldToken GetToken()
        {
            return m_tkField;
        }

        public void SetOffset(int iOffset)
        {
            m_typeBuilder.ThrowIfCreated();

            ModuleBuilder module = m_typeBuilder.GetModuleBuilder();
            TypeBuilder.SetFieldLayoutOffset(JitHelpers.GetQCallModuleOnStack(ref module), GetToken().Token, iOffset);
        }

        public void SetConstant(object? defaultValue)
        {
            m_typeBuilder.ThrowIfCreated();

            if (defaultValue == null && m_fieldType.IsValueType)
            {
                // nullable types can hold null value.
                if (!(m_fieldType.IsGenericType && m_fieldType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    throw new ArgumentException(SR.Argument_ConstantNull);
            }

            TypeBuilder.SetConstantValue(m_typeBuilder.GetModuleBuilder(), GetToken().Token, m_fieldType, defaultValue);
        }


        public void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
                throw new ArgumentNullException(nameof(con));

            if (binaryAttribute == null)
                throw new ArgumentNullException(nameof(binaryAttribute));

            ModuleBuilder module = (m_typeBuilder.Module as ModuleBuilder)!;

            m_typeBuilder.ThrowIfCreated();

            TypeBuilder.DefineCustomAttribute(module,
                m_tkField.Token, module.GetConstructorToken(con).Token, binaryAttribute, false, false);
        }

        public void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
                throw new ArgumentNullException(nameof(customBuilder));

            m_typeBuilder.ThrowIfCreated();

            ModuleBuilder? module = m_typeBuilder.Module as ModuleBuilder;

            customBuilder.CreateCustomAttribute(module!, m_tkField.Token);
        }

        #endregion
    }
}
