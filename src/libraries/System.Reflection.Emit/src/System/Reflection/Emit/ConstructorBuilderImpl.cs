// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed class ConstructorBuilderImpl : ConstructorBuilder
    {
        internal readonly MethodBuilderImpl _methodBuilder;
        internal bool _isDefaultConstructor;

        public ConstructorBuilderImpl(string name, MethodAttributes attributes, CallingConventions callingConvention, Type[]? parameterTypes,
            Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers, ModuleBuilderImpl module, TypeBuilderImpl type)
        {
            _methodBuilder = new MethodBuilderImpl(name, attributes, callingConvention, returnType: null, returnTypeRequiredCustomModifiers: null,
                returnTypeOptionalCustomModifiers: null, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, module, type);

            type._methodDefinitions.Add(_methodBuilder);
        }

        protected override bool InitLocalsCore
        {
            get => _methodBuilder.InitLocals;
            set => _methodBuilder.InitLocals = value;
        }

        protected override ParameterBuilder DefineParameterCore(int iSequence, ParameterAttributes attributes, string strParamName) =>
            _methodBuilder.DefineParameter(iSequence, attributes, strParamName);

        protected override ILGenerator GetILGeneratorCore(int streamSize)
        {
            if (_isDefaultConstructor)
            {
                throw new InvalidOperationException(SR.InvalidOperation_DefaultConstructorILGen);
            }

            return _methodBuilder.GetILGenerator(streamSize);
        }

        protected override void SetCustomAttributeCore(ConstructorInfo con, ReadOnlySpan<byte> binaryAttribute) =>
            _methodBuilder.SetCustomAttribute(con, binaryAttribute);

        protected override void SetImplementationFlagsCore(MethodImplAttributes attributes) =>
            _methodBuilder.SetImplementationFlags(attributes);

        public override string Name => _methodBuilder.Name;

        public override MethodAttributes Attributes => _methodBuilder.Attributes;

        public override CallingConventions CallingConvention
        {
            get
            {
                if (DeclaringType!.IsGenericType)
                {
                    return CallingConventions.HasThis;
                }

                return CallingConventions.Standard;
            }
        }

        public override Type DeclaringType => _methodBuilder.DeclaringType!;

        public override Module Module => _methodBuilder.Module;

        public override int MetadataToken => _methodBuilder.MetadataToken;

        public override Type? ReflectedType => _methodBuilder.ReflectedType;

        public override MethodImplAttributes GetMethodImplementationFlags() => _methodBuilder.GetMethodImplementationFlags();

        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override ParameterInfo[] GetParameters() => _methodBuilder.GetParameters();
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
            => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) =>
            throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);
    }
}
