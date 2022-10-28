// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
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

            lock (s_anonymouslyHostedDynamicMethodsModuleLock)
            {
                if (s_anonymouslyHostedDynamicMethodsModule != null)
                    return s_anonymouslyHostedDynamicMethodsModule;

                AssemblyName assemblyName = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");

                AssemblyBuilder assembly = AssemblyBuilder.InternalDefineDynamicAssembly(
                    assemblyName,
                    AssemblyBuilderAccess.Run,
                    typeof(object).Assembly,
                    null,
                    null);

                // this always gets the internal module.
                s_anonymouslyHostedDynamicMethodsModule = assembly.ManifestModule!;
            }

            return s_anonymouslyHostedDynamicMethodsModule;
        }

        [MemberNotNull(nameof(_parameterTypes))]
        [MemberNotNull(nameof(_returnType))]
        [MemberNotNull(nameof(_dynMethod))]
        [MemberNotNull(nameof(_module))]
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
                    _module = ModuleBuilder.GetRuntimeModuleFromModule(m); // this returns the underlying module for all RuntimeModule and ModuleBuilder objects.
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
            _dynMethod = new RTDynamicMethod(this, name, attributes, callingConvention);
        }

        //
        // MethodInfo api. They mostly forward to RTDynamicMethod
        //

        public override string ToString() => _dynMethod.ToString();

        public override string Name => _dynMethod.Name;

        public override Type? DeclaringType => _dynMethod.DeclaringType;

        public override Type? ReflectedType => _dynMethod.ReflectedType;

        public override Module Module => _dynMethod.Module;

        // we cannot return a MethodHandle because we cannot track it via GC so this method is off limits
        public override RuntimeMethodHandle MethodHandle => throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod);

        public override MethodAttributes Attributes => _dynMethod.Attributes;

        public override CallingConventions CallingConvention => _dynMethod.CallingConvention;

        public override MethodInfo GetBaseDefinition() { return this; }

        public override ParameterInfo[] GetParameters() => _dynMethod.GetParameters();

        public override MethodImplAttributes GetMethodImplementationFlags() => _dynMethod.GetMethodImplementationFlags();

        public override bool IsSecurityCritical => true;

        public override bool IsSecuritySafeCritical => false;

        public override bool IsSecurityTransparent => false;

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => _dynMethod.GetCustomAttributes(attributeType, inherit);

        public override object[] GetCustomAttributes(bool inherit) => _dynMethod.GetCustomAttributes(inherit);

        public override bool IsDefined(Type attributeType, bool inherit) => _dynMethod.IsDefined(attributeType, inherit);

        public override Type ReturnType => _dynMethod.ReturnType;

        public override ParameterInfo ReturnParameter => _dynMethod.ReturnParameter;

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => _dynMethod.ReturnTypeCustomAttributes;

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
                RuntimeParameterInfo[] parameters = _dynMethod.LoadParameters();
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

        //
        // Internal API
        //

        internal MethodInfo GetMethodInfo()
        {
            return _dynMethod;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////
        // RTDynamicMethod
        //
        // this is actually the real runtime instance of a method info that gets used for invocation
        // We need this so we never leak the DynamicMethod out via an exception.
        // This way the DynamicMethod creator is the only one responsible for DynamicMethod access,
        // and can control exactly who gets access to it.
        //
        internal sealed class RTDynamicMethod : MethodInfo
        {
            internal DynamicMethod _owner;
            private RuntimeParameterInfo[]? _parameters;
            private string _name;
            private MethodAttributes _attributes;
            private CallingConventions _callingConvention;

            internal RTDynamicMethod(DynamicMethod owner, string name, MethodAttributes attributes, CallingConventions callingConvention)
            {
                _owner = owner;
                _name = name;
                _attributes = attributes;
                _callingConvention = callingConvention;
            }

            //
            // MethodInfo api
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

            public override Module Module => _owner._module;

            public override RuntimeMethodHandle MethodHandle => throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod);

            public override MethodAttributes Attributes => _attributes;

            public override CallingConventions CallingConvention => _callingConvention;

            public override MethodInfo GetBaseDefinition()
            {
                return this;
            }

            public override ParameterInfo[] GetParameters()
            {
                ParameterInfo[] privateParameters = LoadParameters();
                ParameterInfo[] parameters = new ParameterInfo[privateParameters.Length];
                Array.Copy(privateParameters, parameters, privateParameters.Length);
                return parameters;
            }

            internal override ParameterInfo[] GetParametersNoCopy()
            {
                return LoadParameters();
            }

            public override MethodImplAttributes GetMethodImplementationFlags()
            {
                return MethodImplAttributes.IL | MethodImplAttributes.NoInlining;
            }

            public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
            {
                // We want the creator of the DynamicMethod to control who has access to the
                // DynamicMethod (just like we do for delegates). However, a user can get to
                // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc.
                // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would
                // not be able to bound access to the DynamicMethod. Hence, we do not allow
                // direct use of RTDynamicMethod.
                throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo, "this");
            }

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

            public override bool IsSecurityCritical => _owner.IsSecurityCritical;

            public override bool IsSecuritySafeCritical => _owner.IsSecuritySafeCritical;

            public override bool IsSecurityTransparent => _owner.IsSecurityTransparent;

            public override Type ReturnType => _owner._returnType;

            public override ParameterInfo ReturnParameter => new RuntimeParameterInfo(this, null, _owner._returnType, -1);

            public override ICustomAttributeProvider ReturnTypeCustomAttributes => new EmptyCAHolder();

            internal RuntimeParameterInfo[] LoadParameters()
            {
                if (_parameters == null)
                {
                    Type[] parameterTypes = _owner._parameterTypes;
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
}
