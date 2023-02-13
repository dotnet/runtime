// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection.Emit;

namespace System.Reflection.Metadata.Experiment
{
    internal class PersistableFieldBuilder : FieldBuilder
    {
        //private int _fieldToken;
        private PersistableTypeBuilder _typeBuilder;
        private string _fieldName;
        private FieldAttributes _attributes;
        private Type _fieldType;

        internal PersistableFieldBuilder(PersistableTypeBuilder typeBuilder, string fieldName, Type type,
            Type[]? requiredCustomModifiers, Type[]? optionalCustomModifiers, FieldAttributes attributes)
        {
            ArgumentException.ThrowIfNullOrEmpty(fieldName);

            ArgumentNullException.ThrowIfNull(type);

            if (type == typeof(void))
                throw new ArgumentException("Bad field type");

            _fieldName = fieldName;
            _typeBuilder = typeBuilder;
            _fieldType = type;
            _attributes = attributes & ~FieldAttributes.ReservedMask;

            SignatureHelper sigHelp = SignatureHelper.GetFieldSigHelper(_typeBuilder.Module);
            sigHelp.AddArgument(type, requiredCustomModifiers, optionalCustomModifiers);

            /*byte[] signature = sigHelp.InternalGetSignature(out int sigLength);

            PersistableModuleBuilder module = _typeBuilder.GetModuleBuilder();
            m_fieldTok = RuntimeTypeBuilder.DefineField(new QCallModule(ref module),
                typeBuilder.TypeToken, fieldName, signature, sigLength, m_Attributes);*/
        }

        #region MemberInfo Overrides
        protected override void SetConstantCore(object? defaultValue) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();
        protected override void SetOffsetCore(int iOffset) => throw new NotImplementedException();

        public override int MetadataToken => throw new NotImplementedException();

        public override Module Module => _typeBuilder.Module;

        public override string Name => _fieldName;

        public override Type? DeclaringType
        {
            get
            {
                /*if (_typeBuilder.m_isHiddenGlobalType)
                    return null;*/

                return _typeBuilder;
            }
        }

        public override Type? ReflectedType
        {
            get
            {
                /*if (m_typeBuilder.m_isHiddenGlobalType)
                    return null;*/

                return _typeBuilder;
            }
        }

        #endregion

        #region FieldInfo Overrides
        public override Type FieldType => _fieldType;

        public override object? GetValue(object? obj) => throw new NotSupportedException();

        public override void SetValue(object? obj, object? val, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture) => throw new NotSupportedException();
        public override RuntimeFieldHandle FieldHandle => throw new NotSupportedException();

        public override FieldAttributes Attributes => _attributes;

        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException();

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException();
        #endregion
    }
}
