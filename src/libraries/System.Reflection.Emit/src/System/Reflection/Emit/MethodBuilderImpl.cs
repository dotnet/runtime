// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
        private MethodAttributes _attributes;
        private MethodImplAttributes _methodImplFlags;
        private readonly string _name;
        private readonly CallingConventions _callingConventions;
        private readonly TypeBuilderImpl _declaringType;

        internal List<CustomAttributeWrapper> _customAttributes = new();

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

            _methodImplFlags = MethodImplAttributes.IL;
        }

        internal BlobBuilder GetMethodSignatureBlob() =>
            MetadataSignatureHelper.MethodSignatureEncoder(_module, _parameterTypes, ReturnType, IsStatic);

        protected override bool InitLocalsCore { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        protected override GenericTypeParameterBuilder[] DefineGenericParametersCore(params string[] names) => throw new NotImplementedException();
        protected override ParameterBuilder DefineParameterCore(int position, ParameterAttributes attributes, string? strParamName) => throw new NotImplementedException();
        protected override ILGenerator GetILGeneratorCore(int size) => throw new NotImplementedException();
        protected override void SetCustomAttributeCore(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (!IsPseudoCustomAttribute(con.ReflectedType!.FullName!, con, binaryAttribute))
            {
                _customAttributes.Add(new CustomAttributeWrapper(con, binaryAttribute));
            }
        }

        private bool IsPseudoCustomAttribute(string attributeName, ConstructorInfo con, byte[] data)
        {
            switch (attributeName)
            {
                case "System.Runtime.CompilerServices.MethodImplAttribute":
                    int impla = data[2];
                    impla |= data[3] << 8;
                    _methodImplFlags |= (MethodImplAttributes)impla;
                    break;

                case "System.Runtime.InteropServices.DllImportAttribute":
                    CustomAttributeInfo attr = CustomAttributeInfo.DecodeCustomAttribute(con, data);
                    bool preserveSig = true;

                    /* TODO
                     * pi_dll = (string?)attr._ctorArgs[0];
                    if (pi_dll == null || pi_dll.Length == 0)
                        throw new ArgumentException(SR.Arg_DllNameCannotBeEmpty);

                    native_cc = Runtime.InteropServices.CallingConvention.Winapi;*/

                    for (int i = 0; i < attr._namedParamNames.Length; ++i)
                    {
                        string name = attr._namedParamNames[i];
                        object? value = attr._namedParamValues[i];

                        if (name == "PreserveSig")
                            preserveSig = (bool)value!;
                        /*else if (name == "CallingConvention") // TODO: this values might need to be covered
                            native_cc = (CallingConvention)value!;
                        else if (name == "CharSet")
                            charset = (CharSet)value!;
                        else if (name == "EntryPoint")
                            pi_entry = (string)value!;
                        else if (name == "ExactSpelling")
                            ExactSpelling = (bool)value!;
                        else if (name == "SetLastError")
                            SetLastError = (bool)value!;
                        else if (name == "BestFitMapping")
                            BestFitMapping = (bool)value!;
                        else if (name == "ThrowOnUnmappableChar")
                            ThrowOnUnmappableChar = (bool)value!;*/
                    }

                    _attributes |= MethodAttributes.PinvokeImpl;
                    if (preserveSig)
                        _methodImplFlags |= MethodImplAttributes.PreserveSig;
                    break;
                case "System.Runtime.InteropServices.PreserveSigAttribute":
                    _methodImplFlags |= MethodImplAttributes.PreserveSig;
                    break;
                case "System.Runtime.CompilerServices.SpecialNameAttribute":
                    _attributes |= MethodAttributes.SpecialName;
                    break;
                case "System.Security.SuppressUnmanagedCodeSecurityAttribute":
                    _attributes |= MethodAttributes.HasSecurity;
                    return false;
                default: return false;
            }

            return true;
        }

        protected override void SetImplementationFlagsCore(MethodImplAttributes attributes)
        {
            _methodImplFlags = attributes;
        }
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
            => _methodImplFlags;

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
