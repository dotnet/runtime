// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

namespace System.Reflection.Emit
{
    public sealed partial class DynamicMethod : MethodInfo
    {
        // The context when the method was created. We use this to do the RestrictedMemberAccess checks.
        // These checks are done when the method is compiled. This can happen at an arbitrary time,
        // when CreateDelegate or Invoke is called, or when another DynamicMethod executes OpCodes.Call.
        // We capture the creation context so that we can do the checks against the same context,
        // irrespective of when the method gets compiled. Note that the DynamicMethod does not know when
        // it is ready for use since there is not API which indictates that IL generation has completed.
        private static volatile Module? s_anonymouslyHostedDynamicMethodsModule;
        private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();

        //
        // class initialization (ctor and init)
        //

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes)
        {
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                null,   // m
                false,  // skipVisibility
                true);
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             bool restrictedSkipVisibility)
        {
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                null,   // m
                restrictedSkipVisibility,
                true);
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Module m)
        {
            ArgumentNullException.ThrowIfNull(m);

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                false,  // skipVisibility
                false);
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Module m,
                             bool skipVisibility)
        {
            ArgumentNullException.ThrowIfNull(m);

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                skipVisibility,
                false);
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             MethodAttributes attributes,
                             CallingConventions callingConvention,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Module m,
                             bool skipVisibility)
        {
            ArgumentNullException.ThrowIfNull(m);

            Init(name,
                attributes,
                callingConvention,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                skipVisibility,
                false);
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Type owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                owner,  // owner
                null,   // m
                false,  // skipVisibility
                false);
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Type owner,
                             bool skipVisibility)
        {
            ArgumentNullException.ThrowIfNull(owner);

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                owner,  // owner
                null,   // m
                skipVisibility,
                false);
        }

        [RequiresDynamicCode("Creating a DynamicMethod requires dynamic code.")]
        public DynamicMethod(string name,
                             MethodAttributes attributes,
                             CallingConventions callingConvention,
                             Type? returnType,
                             Type[]? parameterTypes,
                             Type owner,
                             bool skipVisibility)
        {
            ArgumentNullException.ThrowIfNull(owner);

            Init(name,
                attributes,
                callingConvention,
                returnType,
                parameterTypes,
                owner,  // owner
                null,   // m
                skipVisibility,
                false);
        }

        // We create a transparent assembly to host DynamicMethods. Since the assembly does not have any
        // non-public fields (or any fields at all), it is a safe anonymous assembly to host DynamicMethods
        private static Module GetDynamicMethodsModule()
        {
            if (s_anonymouslyHostedDynamicMethodsModule != null)
                return s_anonymouslyHostedDynamicMethodsModule;

            AssemblyBuilder.EnsureDynamicCodeSupported();

            lock (s_anonymouslyHostedDynamicMethodsModuleLock)
            {
                if (s_anonymouslyHostedDynamicMethodsModule != null)
                    return s_anonymouslyHostedDynamicMethodsModule;

                AssemblyName assemblyName = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");

                var assembly = RuntimeAssemblyBuilder.InternalDefineDynamicAssembly(assemblyName,
                    AssemblyBuilderAccess.Run, AssemblyLoadContext.Default, null);

                // this always gets the internal module.
                s_anonymouslyHostedDynamicMethodsModule = assembly.ManifestModule!;
            }

            return s_anonymouslyHostedDynamicMethodsModule;
        }

        [MemberNotNull(nameof(_parameterTypes))]
        [MemberNotNull(nameof(_returnType))]
        [MemberNotNull(nameof(_module))]
        [MemberNotNull(nameof(_name))]
        private void Init(string name,
                          MethodAttributes attributes,
                          CallingConventions callingConvention,
                          Type? returnType,
                          Type[]? signature,
                          Type? owner,
                          Module? m,
                          bool skipVisibility,
                          bool transparentMethod)
        {
            ArgumentNullException.ThrowIfNull(name);

            AssemblyBuilder.EnsureDynamicCodeSupported();

            if (attributes != (MethodAttributes.Static | MethodAttributes.Public) || callingConvention != CallingConventions.Standard)
                throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);

            // check and store the signature
            if (signature != null)
            {
                _parameterTypes = new RuntimeType[signature.Length];
                for (int i = 0; i < signature.Length; i++)
                {
                    if (signature[i] == null)
                        throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
                    _parameterTypes[i] = (signature[i].UnderlyingSystemType as RuntimeType)!;
                    if (_parameterTypes[i] == null || _parameterTypes[i] == typeof(void))
                        throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
                }
            }
            else
            {
                _parameterTypes = Array.Empty<RuntimeType>();
            }

            // check and store the return value
            _returnType = returnType is null ?
                (RuntimeType)typeof(void) :
                (returnType.UnderlyingSystemType as RuntimeType) ?? throw new NotSupportedException(SR.Arg_InvalidTypeInRetType);

            if (transparentMethod)
            {
                Debug.Assert(owner == null && m == null, "owner and m cannot be set for transparent methods");
                _module = GetDynamicMethodsModule();
                _restrictedSkipVisibility = skipVisibility;
            }
            else
            {
                Debug.Assert(m != null || owner != null, "Constructor should ensure that either m or owner is set");
                Debug.Assert(m == null || !m.Equals(s_anonymouslyHostedDynamicMethodsModule), "The user cannot explicitly use this assembly");
                Debug.Assert(m == null || owner == null, "m and owner cannot both be set");

                if (m != null)
                    _module = RuntimeModuleBuilder.GetRuntimeModuleFromModule(m); // this returns the underlying module for all RuntimeModule and ModuleBuilder objects.
                else
                {
                    if (owner?.UnderlyingSystemType is RuntimeType rtOwner)
                    {
                        if (rtOwner.HasElementType || rtOwner.ContainsGenericParameters
                            || rtOwner.IsGenericParameter || rtOwner.IsInterface)
                            throw new ArgumentException(SR.Argument_InvalidTypeForDynamicMethod);

                        _typeOwner = rtOwner;
                        _module = rtOwner.GetRuntimeModule();
                    }
                    else
                    {
                        _module = null!;
                    }
                }

                _skipVisibility = skipVisibility;
            }

            // initialize remaining fields
            _ilGenerator = null;
            _initLocals = true;
            _methodHandle = null;
            _name = name;
            _attributes = attributes;
            _callingConvention = callingConvention;
        }

        //
        // MethodInfo api.
        //

        public override string ToString()
        {
            var sbName = new ValueStringBuilder(MethodNameBufferSize);

            sbName.Append(ReturnType.FormatTypeName());
            sbName.Append(' ');
            sbName.Append(Name);

            sbName.Append('(');
            AppendParameters(ref sbName, GetParameterTypes(), CallingConvention);
            sbName.Append(')');

            return sbName.ToString();
        }

        public override string Name => _name;

        public override Type? DeclaringType => null;

        public override Type? ReflectedType => null;

        public override Module Module => _module;

        // we cannot return a MethodHandle because we cannot track it via GC so this method is off limits
        public override RuntimeMethodHandle MethodHandle => throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod);

        public override MethodAttributes Attributes => _attributes;

        public override CallingConventions CallingConvention => _callingConvention;

        public override MethodInfo GetBaseDefinition() => this;

        public override ParameterInfo[] GetParameters() =>
            GetParametersAsSpan().ToArray();

        internal override ReadOnlySpan<ParameterInfo> GetParametersAsSpan() => LoadParameters();

        public override MethodImplAttributes GetMethodImplementationFlags() =>
            MethodImplAttributes.IL | MethodImplAttributes.NoInlining;

        public override bool IsSecurityCritical => true;

        public override bool IsSecuritySafeCritical => false;

        public override bool IsSecurityTransparent => false;

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            if (attributeType.UnderlyingSystemType is not RuntimeType attributeRuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            bool includeMethodImplAttribute = attributeType.IsAssignableFrom(typeof(MethodImplAttribute));
            object[] result = CustomAttribute.CreateAttributeArrayHelper(attributeRuntimeType, includeMethodImplAttribute ? 1 : 0);
            if (includeMethodImplAttribute)
            {
                result[0] = new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags());
            }
            return result;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            // support for MethodImplAttribute PCA
            return new object[] { new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags()) };
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);

            return attributeType.IsAssignableFrom(typeof(MethodImplAttribute));
        }

        public override Type ReturnType => _returnType;

        public override ParameterInfo ReturnParameter => new RuntimeParameterInfo(this, null, _returnType, -1);

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

        //
        // DynamicMethod specific methods
        //

        public ParameterBuilder? DefineParameter(int position, ParameterAttributes attributes, string? parameterName)
        {
            if (position < 0 || position > _parameterTypes.Length)
                throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);
            position--; // it's 1 based. 0 is the return value

            if (position >= 0)
            {
                RuntimeParameterInfo[] parameters = LoadParameters();
                parameters[position].SetName(parameterName);
                parameters[position].SetAttributes(attributes);
            }
            return null;
        }

        public ILGenerator GetILGenerator()
        {
            return GetILGenerator(64);
        }

        public bool InitLocals
        {
            get => _initLocals;
            set => _initLocals = value;
        }

        internal RuntimeType[] ArgumentTypes => _parameterTypes;

        private RuntimeParameterInfo[] LoadParameters()
        {
            if (_parameters == null)
            {
                Type[] parameterTypes = _parameterTypes;
                RuntimeParameterInfo[] parameters = new RuntimeParameterInfo[parameterTypes.Length];
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    parameters[i] = new RuntimeParameterInfo(this, null, parameterTypes[i], i);
                }

                _parameters ??= parameters; // should we Interlocked.CompareExchange?
            }

            return _parameters;
        }
    }
}
