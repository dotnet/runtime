// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;

namespace System.Reflection.Emit
{
    internal sealed class MethodBuilderImpl : MethodBuilder
    {
        private readonly Type _returnType;
        private readonly Type[]? _parameterTypes;
        private readonly ModuleBuilderImpl _module;
        private readonly MethodAttributes _attributes;
        private readonly string _name;
        private readonly CallingConventions _callingConventions;
        private readonly TypeBuilderImpl _declaringType;

        internal MethodBuilderImpl(string name, MethodAttributes attributes, CallingConventions callingConventions, Type? returnType,
            Type[]? parameterTypes, ModuleBuilderImpl module, TypeBuilderImpl declaringType)
        {
            _module = module;
            _returnType = returnType ?? _module.GetTypeFromCoreAssembly(CoreTypeId.Void);
            _name = name;
            _attributes = attributes;
            _callingConventions = callingConventions;
            _declaringType = declaringType;

            if (parameterTypes != null)
            {
                _parameterTypes = new Type[parameterTypes.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    ArgumentNullException.ThrowIfNull(_parameterTypes[i] = parameterTypes[i], nameof(parameterTypes));
                }
            }
        }

        internal BlobBuilder GetMethodSignatureBlob() =>
            MetadataSignatureHelper.MethodSignatureEncoder(_module, _parameterTypes, ReturnType, !IsStatic);

        protected override bool InitLocalsCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names) => throw new NotImplementedException();
        protected override ParameterBuilder DefineParameterCore(int position, ParameterAttributes attributes, string? strParamName) => throw new NotImplementedException();
        protected override ILGenerator GetILGeneratorCore(int size) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();
        protected override void SetImplementationFlagsCore(MethodImplAttributes attributes) => throw new NotImplementedException();
        protected override void SetSignatureCore(Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes,
            Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers) => throw new NotImplementedException();
        public override string Name => _name;
        public override MethodAttributes Attributes => _attributes;
        public override CallingConventions CallingConvention => _callingConventions;
        public override TypeBuilder DeclaringType => _declaringType;
        public override Module Module => _module;
        public override bool ContainsGenericParameters { get => throw new NotSupportedException(SR.NotSupported_DynamicModule); }
        public override bool IsGenericMethod { get => throw new NotImplementedException(); }
        public override bool IsGenericMethodDefinition { get => throw new NotImplementedException(); }
        public override bool IsSecurityCritical => true;
        public override bool IsSecuritySafeCritical => false;
        public override bool IsSecurityTransparent => false;
        public override int MetadataToken { get => throw new NotImplementedException(); }
        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override Type? ReflectedType { get => throw new NotImplementedException(); }
        public override ParameterInfo ReturnParameter { get => throw new NotImplementedException(); }
        public override Type ReturnType => _returnType;
        public override ICustomAttributeProvider ReturnTypeCustomAttributes { get => throw new NotImplementedException(); }

        public override MethodInfo GetBaseDefinition() => this;

        public override object[] GetCustomAttributes(bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override Type[] GetGenericArguments()
            => throw new NotImplementedException();

        public override MethodInfo GetGenericMethodDefinition()
            => throw new NotImplementedException();

        public override int GetHashCode()
            => throw new NotImplementedException();

        public override MethodImplAttributes GetMethodImplementationFlags()
            => throw new NotImplementedException();

        public override ParameterInfo[] GetParameters()
            => throw new NotImplementedException();

        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
             => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotSupportedException(SR.NotSupported_DynamicModule);

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCodeAttribute("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params System.Type[] typeArguments)
            => throw new NotImplementedException();
    }
}
