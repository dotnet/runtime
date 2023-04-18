// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed class FieldBuilderImpl : FieldBuilder
    {
        private readonly TypeBuilderImpl _typeBuilder;
        private readonly string _fieldName;
        private readonly FieldAttributes _attributes;
        private readonly Type _fieldType;

        internal FieldBuilderImpl(TypeBuilderImpl typeBuilder, string fieldName, Type type, FieldAttributes attributes)
        {
            _fieldName = fieldName;
            _typeBuilder = typeBuilder;
            _fieldType = type;
            _attributes = attributes & ~FieldAttributes.ReservedMask;
        }

        #region MemberInfo Overrides
        protected override void SetConstantCore(object? defaultValue) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();

        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();
        protected override void SetOffsetCore(int iOffset) => throw new NotImplementedException();

        public override int MetadataToken => throw new NotImplementedException();

        public override Module Module => _typeBuilder.Module;

        public override string Name => _fieldName;

        public override Type? DeclaringType => _typeBuilder;

        public override Type? ReflectedType => _typeBuilder;

        #endregion

        #region FieldInfo Overrides
        public override Type FieldType => _fieldType;

        public override object? GetValue(object? obj) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override void SetValue(object? obj, object? val, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
            => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override RuntimeFieldHandle FieldHandle => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override FieldAttributes Attributes => _attributes;

        #endregion

        #region ICustomAttributeProvider Implementation
        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        #endregion
    }
}
