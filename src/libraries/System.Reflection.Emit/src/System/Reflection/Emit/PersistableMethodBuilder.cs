// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit.Experiment
{
    internal sealed class PersistableMethodBuilder : MethodBuilder
    {
        internal Type? _returnType;
        internal Type[]? _parametersTypes;
        private readonly PersistableModuleBuilder _module;

        internal PersistableMethodBuilder(string name, MethodAttributes attributes, CallingConventions callingConventions, Type? returnType,
            Type[]? parameters, PersistableModuleBuilder module, PersistableTypeBuilder declaringType)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(module);

            _module = module;
            Name = name;
            Attributes = attributes;
            CallingConvention = callingConventions;
            _returnType = returnType;
            _parametersTypes = parameters;
            DeclaringType = declaringType;
            Module = declaringType.Module;
        }

        protected override bool InitLocalsCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names) => throw new NotImplementedException();
        protected override ParameterBuilder DefineParameterCore(int position, ParameterAttributes attributes, string? strParamName) => throw new NotImplementedException();
        protected override ILGenerator GetILGeneratorCore(int size) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(CustomAttributeBuilder customBuilder) => throw new NotImplementedException();
        protected override void SetImplementationFlagsCore(MethodImplAttributes attributes) => throw new NotImplementedException();
        protected override void SetSignatureCore(Type? returnType, Type[]? returnTypeRequiredCustomModifiers, Type[]? returnTypeOptionalCustomModifiers, Type[]? parameterTypes, Type[][]? parameterTypeRequiredCustomModifiers, Type[][]? parameterTypeOptionalCustomModifiers) => throw new NotImplementedException();
        public override string Name { get; }
        public override MethodAttributes Attributes { get; }
        public override CallingConventions CallingConvention { get; }
        public override TypeBuilder DeclaringType { get; }
        public override Module Module { get; }
        public override bool ContainsGenericParameters { get => throw new NotImplementedException(); }
        public override bool IsGenericMethod { get => throw new NotImplementedException(); }
        public override bool IsGenericMethodDefinition { get => throw new NotImplementedException(); }
        public override bool IsSecurityCritical { get => throw new NotImplementedException(); }
        public override bool IsSecuritySafeCritical { get => throw new NotImplementedException(); }
        public override bool IsSecurityTransparent { get => throw new NotImplementedException(); }
        public override int MetadataToken { get => throw new NotImplementedException(); }
        public override RuntimeMethodHandle MethodHandle { get => throw new NotImplementedException(); }
        public override Type? ReflectedType { get => throw new NotImplementedException(); }
        public override ParameterInfo ReturnParameter { get => throw new NotImplementedException(); }
        public override Type ReturnType { get => throw new NotImplementedException(); }
        public override ICustomAttributeProvider ReturnTypeCustomAttributes { get => throw new NotImplementedException(); }

        public override MethodInfo GetBaseDefinition()
            => throw new NotImplementedException();

        public override object[] GetCustomAttributes(bool inherit)
            => throw new NotImplementedException();

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            => throw new NotImplementedException();

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
            => throw new NotImplementedException();

        public override bool IsDefined(Type attributeType, bool inherit)
            => throw new NotImplementedException();

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCodeAttribute("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params System.Type[] typeArguments)
            => throw new NotImplementedException();
    }
}
