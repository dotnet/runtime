// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed class ConstructorBuilderImpl : ConstructorBuilder
    {
        private readonly MethodBuilderImpl _methodBuilder;
        internal bool _isDefaultConstructor;

        public ConstructorBuilderImpl(string name, MethodAttributes attributes, CallingConventions callingConvention,
            Type[]? parameterTypes, ModuleBuilderImpl mod, TypeBuilderImpl type)
        {
            _methodBuilder = new MethodBuilderImpl(name, attributes, callingConvention, null, parameterTypes, mod, type);

            type._methodDefStore!.Add(_methodBuilder);
        }
        protected override bool InitLocalsCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        protected override ParameterBuilder DefineParameterCore(int iSequence, ParameterAttributes attributes, string strParamName) => throw new NotImplementedException();
        protected override ILGenerator GetILGeneratorCore(int streamSize) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();
        protected override void SetImplementationFlagsCore(MethodImplAttributes attributes) => throw new NotImplementedException();
        public override string Name => _methodBuilder.Name;
        public override MethodAttributes Attributes => _methodBuilder.Attributes;
        public override CallingConventions CallingConvention => throw new NotImplementedException();
        public override TypeBuilder DeclaringType => _methodBuilder.DeclaringType;
        public override Module Module => _methodBuilder.Module;
        public override int MetadataToken => _methodBuilder.MetadataToken;
        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override Type? ReflectedType => _methodBuilder.ReflectedType;
        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();
        public override ParameterInfo[] GetParameters() => throw new NotImplementedException();
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
            => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) =>
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
    }
}
