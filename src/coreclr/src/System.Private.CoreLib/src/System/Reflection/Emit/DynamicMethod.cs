// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Reflection.Emit
{
    public sealed class DynamicMethod : MethodInfo
    {
        private RuntimeType[] m_parameterTypes = null!;
        internal IRuntimeMethodInfo? m_methodHandle;
        private RuntimeType m_returnType = null!;
        private DynamicILGenerator? m_ilGenerator;
        private DynamicILInfo? m_DynamicILInfo;
        private bool m_fInitLocals;
        private RuntimeModule m_module = null!;
        internal bool m_skipVisibility;
        internal RuntimeType? m_typeOwner; // can be null

        // We want the creator of the DynamicMethod to control who has access to the
        // DynamicMethod (just like we do for delegates). However, a user can get to
        // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc.
        // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would
        // not be able to bound access to the DynamicMethod. Hence, we need to ensure that 
        // we do not allow direct use of RTDynamicMethod.
        private RTDynamicMethod m_dynMethod = null!;

        // needed to keep the object alive during jitting
        // assigned by the DynamicResolver ctor
        internal DynamicResolver? m_resolver;

        internal bool m_restrictedSkipVisibility;
        // The context when the method was created. We use this to do the RestrictedMemberAccess checks.
        // These checks are done when the method is compiled. This can happen at an arbitrary time,
        // when CreateDelegate or Invoke is called, or when another DynamicMethod executes OpCodes.Call.
        // We capture the creation context so that we can do the checks against the same context,
        // irrespective of when the method gets compiled. Note that the DynamicMethod does not know when
        // it is ready for use since there is not API which indictates that IL generation has completed.
        private static volatile InternalModuleBuilder s_anonymouslyHostedDynamicMethodsModule;
        private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();

        //
        // class initialization (ctor and init)
        //

        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes)
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

        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes,
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

        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes,
                             Module m)
        {
            if (m == null)
                throw new ArgumentNullException(nameof(m));

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

        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes,
                             Module m,
                             bool skipVisibility)
        {
            if (m == null)
                throw new ArgumentNullException(nameof(m));

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

        public DynamicMethod(string name,
                             MethodAttributes attributes,
                             CallingConventions callingConvention,
                             Type returnType,
                             Type[] parameterTypes,
                             Module m,
                             bool skipVisibility)
        {
            if (m == null)
                throw new ArgumentNullException(nameof(m));

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

        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes,
                             Type owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

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

        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes,
                             Type owner,
                             bool skipVisibility)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

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

        public DynamicMethod(string name,
                             MethodAttributes attributes,
                             CallingConventions callingConvention,
                             Type returnType,
                             Type[] parameterTypes,
                             Type owner,
                             bool skipVisibility)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

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

        // helpers for intialization

        private static void CheckConsistency(MethodAttributes attributes, CallingConventions callingConvention)
        {
            // only public static for method attributes
            if ((attributes & ~MethodAttributes.MemberAccessMask) != MethodAttributes.Static)
                throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);
            if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);

            // only standard or varargs supported
            if (callingConvention != CallingConventions.Standard && callingConvention != CallingConventions.VarArgs)
                throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);

            // vararg is not supported at the moment
            if (callingConvention == CallingConventions.VarArgs)
                throw new NotSupportedException(SR.NotSupported_DynamicMethodFlags);
        }

        // We create a transparent assembly to host DynamicMethods. Since the assembly does not have any
        // non-public fields (or any fields at all), it is a safe anonymous assembly to host DynamicMethods
        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        private static RuntimeModule GetDynamicMethodsModule()
        {
            if (s_anonymouslyHostedDynamicMethodsModule != null)
                return s_anonymouslyHostedDynamicMethodsModule;

            lock (s_anonymouslyHostedDynamicMethodsModuleLock)
            {
                if (s_anonymouslyHostedDynamicMethodsModule != null)
                    return s_anonymouslyHostedDynamicMethodsModule;

                AssemblyName assemblyName = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");
                StackCrawlMark stackMark = StackCrawlMark.LookForMe;

                AssemblyBuilder assembly = AssemblyBuilder.InternalDefineDynamicAssembly(
                    assemblyName,
                    AssemblyBuilderAccess.Run,
                    ref stackMark,
                    null);

                // this always gets the internal module.
                s_anonymouslyHostedDynamicMethodsModule = (InternalModuleBuilder)assembly.ManifestModule!;
            }

            return s_anonymouslyHostedDynamicMethodsModule;
        }

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
            DynamicMethod.CheckConsistency(attributes, callingConvention);

            // check and store the signature
            if (signature != null)
            {
                m_parameterTypes = new RuntimeType[signature.Length];
                for (int i = 0; i < signature.Length; i++)
                {
                    if (signature[i] == null)
                        throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
                    m_parameterTypes[i] = (signature[i].UnderlyingSystemType as RuntimeType)!;
                    if (m_parameterTypes[i] == null || m_parameterTypes[i] == typeof(void))
                        throw new ArgumentException(SR.Arg_InvalidTypeInSignature);
                }
            }
            else
            {
                m_parameterTypes = Array.Empty<RuntimeType>();
            }

            // check and store the return value
            m_returnType = (returnType == null) ? (RuntimeType)typeof(void) : (returnType.UnderlyingSystemType as RuntimeType)!;
            if (m_returnType == null)
                throw new NotSupportedException(SR.Arg_InvalidTypeInRetType);

            if (transparentMethod)
            {
                Debug.Assert(owner == null && m == null, "owner and m cannot be set for transparent methods");
                m_module = GetDynamicMethodsModule();
                if (skipVisibility)
                {
                    m_restrictedSkipVisibility = true;
                }
            }
            else
            {
                Debug.Assert(m != null || owner != null, "Constructor should ensure that either m or owner is set");
                Debug.Assert(m == null || !m.Equals(s_anonymouslyHostedDynamicMethodsModule), "The user cannot explicitly use this assembly");
                Debug.Assert(m == null || owner == null, "m and owner cannot both be set");

                if (m != null)
                    m_module = m.ModuleHandle.GetRuntimeModule(); // this returns the underlying module for all RuntimeModule and ModuleBuilder objects.
                else
                {
                    RuntimeType? rtOwner = null;
                    if (owner != null)
                        rtOwner = owner.UnderlyingSystemType as RuntimeType;

                    if (rtOwner != null)
                    {
                        if (rtOwner.HasElementType || rtOwner.ContainsGenericParameters
                            || rtOwner.IsGenericParameter || rtOwner.IsInterface)
                            throw new ArgumentException(SR.Argument_InvalidTypeForDynamicMethod);

                        m_typeOwner = rtOwner;
                        m_module = rtOwner.GetRuntimeModule();
                    }
                }

                m_skipVisibility = skipVisibility;
            }

            // initialize remaining fields
            m_ilGenerator = null;
            m_fInitLocals = true;
            m_methodHandle = null;

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            m_dynMethod = new RTDynamicMethod(this, name, attributes, callingConvention);
        }

        //
        // Delegate and method creation
        //

        public sealed override Delegate CreateDelegate(Type delegateType)
        {
            if (m_restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation.
                GetMethodDescriptor();
                IRuntimeMethodInfo? methodHandle = m_methodHandle;
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(methodHandle != null ? methodHandle.Value : RuntimeMethodHandleInternal.EmptyHandle);
                GC.KeepAlive(methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, null, GetMethodDescriptor());
            // stash this MethodInfo by brute force.  
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }

        public sealed override Delegate CreateDelegate(Type delegateType, object? target)
        {
            if (m_restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation
                GetMethodDescriptor();
                IRuntimeMethodInfo? methodHandle = m_methodHandle;
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(methodHandle != null ? methodHandle.Value : RuntimeMethodHandleInternal.EmptyHandle);
                GC.KeepAlive(methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, GetMethodDescriptor());
            // stash this MethodInfo by brute force. 
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }

        // This is guaranteed to return a valid handle
        internal RuntimeMethodHandle GetMethodDescriptor()
        {
            if (m_methodHandle == null)
            {
                lock (this)
                {
                    if (m_methodHandle == null)
                    {
                        if (m_DynamicILInfo != null)
                            m_DynamicILInfo.GetCallableMethod(m_module, this);
                        else
                        {
                            if (m_ilGenerator == null || m_ilGenerator.ILOffset == 0)
                                throw new InvalidOperationException(SR.Format(SR.InvalidOperation_BadEmptyMethodBody, Name));

                            m_ilGenerator.GetCallableMethod(m_module, this);
                        }
                    }
                }
            }
            return new RuntimeMethodHandle(m_methodHandle!);
        }

        //
        // MethodInfo api. They mostly forward to RTDynamicMethod
        //

        public override string ToString() { return m_dynMethod.ToString(); }

        public override string Name { get { return m_dynMethod.Name; } }

        public override Type? DeclaringType { get { return m_dynMethod.DeclaringType; } }

        public override Type? ReflectedType { get { return m_dynMethod.ReflectedType; } }

        public override Module Module { get { return m_dynMethod.Module; } }

        // we cannot return a MethodHandle because we cannot track it via GC so this method is off limits
        public override RuntimeMethodHandle MethodHandle { get { throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod); } }

        public override MethodAttributes Attributes { get { return m_dynMethod.Attributes; } }

        public override CallingConventions CallingConvention { get { return m_dynMethod.CallingConvention; } }

        public override MethodInfo GetBaseDefinition() { return this; }

        public override ParameterInfo[] GetParameters() { return m_dynMethod.GetParameters(); }

        public override MethodImplAttributes GetMethodImplementationFlags() { return m_dynMethod.GetMethodImplementationFlags(); }

        public override bool IsSecurityCritical
        {
            get { return true; }
        }

        public override bool IsSecuritySafeCritical
        {
            get { return false; }
        }

        public override bool IsSecurityTransparent
        {
            get { return false; }
        }

        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(SR.NotSupported_CallToVarArg);

            //
            // We do not demand any permission here because the caller already has access
            // to the current DynamicMethod object, and it could just as easily emit another 
            // Transparent DynamicMethod to call the current DynamicMethod.
            //

            RuntimeMethodHandle method = GetMethodDescriptor();
            // ignore obj since it's a static method

            // create a signature object
            Signature sig = new Signature(
                this.m_methodHandle!, m_parameterTypes, m_returnType, CallingConvention);


            // verify arguments
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(SR.Arg_ParmCnt);

            // if we are here we passed all the previous checks. Time to look at the arguments
            bool wrapExceptions = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
            object retValue;
            if (actualCount > 0)
            {
                object[] arguments = CheckArguments(parameters!, binder, invokeAttr, culture, sig);
                retValue = RuntimeMethodHandle.InvokeMethod(null, arguments, sig, false, wrapExceptions);
                // copy out. This should be made only if ByRef are present.
                for (int index = 0; index < arguments.Length; index++)
                    parameters![index] = arguments[index];
            }
            else
            {
                retValue = RuntimeMethodHandle.InvokeMethod(null, null, sig, false, wrapExceptions);
            }

            GC.KeepAlive(this);
            return retValue;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_dynMethod.GetCustomAttributes(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit) { return m_dynMethod.GetCustomAttributes(inherit); }

        public override bool IsDefined(Type attributeType, bool inherit) { return m_dynMethod.IsDefined(attributeType, inherit); }

        public override Type? ReturnType { get { return m_dynMethod.ReturnType; } }

        public override ParameterInfo? ReturnParameter { get { return m_dynMethod.ReturnParameter; } }

        public override ICustomAttributeProvider? ReturnTypeCustomAttributes { get { return m_dynMethod.ReturnTypeCustomAttributes; } }

        //
        // DynamicMethod specific methods
        //

        public ParameterBuilder? DefineParameter(int position, ParameterAttributes attributes, string? parameterName)
        {
            if (position < 0 || position > m_parameterTypes.Length)
                throw new ArgumentOutOfRangeException(SR.ArgumentOutOfRange_ParamSequence);
            position--; // it's 1 based. 0 is the return value

            if (position >= 0)
            {
                RuntimeParameterInfo[] parameters = m_dynMethod.LoadParameters();
                parameters[position].SetName(parameterName);
                parameters[position].SetAttributes(attributes);
            }
            return null;
        }

        public DynamicILInfo GetDynamicILInfo()
        {
            if (m_DynamicILInfo == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                        null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_DynamicILInfo = new DynamicILInfo(this, methodSignature);
            }
            return m_DynamicILInfo;
        }

        public ILGenerator GetILGenerator()
        {
            return GetILGenerator(64);
        }

        public ILGenerator GetILGenerator(int streamSize)
        {
            if (m_ilGenerator == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                    null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_ilGenerator = new DynamicILGenerator(this, methodSignature, streamSize);
            }
            return m_ilGenerator;
        }

        public bool InitLocals
        {
            get { return m_fInitLocals; }
            set { m_fInitLocals = value; }
        }

        //
        // Internal API
        //

        internal MethodInfo GetMethodInfo()
        {
            return m_dynMethod;
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
            internal DynamicMethod m_owner;
            private RuntimeParameterInfo[]? m_parameters;
            private string m_name;
            private MethodAttributes m_attributes;
            private CallingConventions m_callingConvention;

            internal RTDynamicMethod(DynamicMethod owner, string name, MethodAttributes attributes, CallingConventions callingConvention)
            {
                m_owner = owner;
                m_name = name;
                m_attributes = attributes;
                m_callingConvention = callingConvention;
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

            public override string Name
            {
                get { return m_name; }
            }

            public override Type? DeclaringType
            {
                get { return null; }
            }

            public override Type? ReflectedType
            {
                get { return null; }
            }

            public override Module Module
            {
                get { return m_owner.m_module; }
            }

            public override RuntimeMethodHandle MethodHandle
            {
                get { throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInDynamicMethod); }
            }

            public override MethodAttributes Attributes
            {
                get { return m_attributes; }
            }

            public override CallingConventions CallingConvention
            {
                get { return m_callingConvention; }
            }

            public override MethodInfo GetBaseDefinition()
            {
                return this;
            }

            public override ParameterInfo[] GetParameters()
            {
                ParameterInfo[] privateParameters = LoadParameters();
                ParameterInfo[] parameters = new ParameterInfo[privateParameters.Length];
                Array.Copy(privateParameters, 0, parameters, 0, privateParameters.Length);
                return parameters;
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
                if (attributeType == null)
                    throw new ArgumentNullException(nameof(attributeType));

                if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
                    return new object[] { new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags()) };
                else
                    return Array.Empty<object>();
            }

            public override object[] GetCustomAttributes(bool inherit)
            {
                // support for MethodImplAttribute PCA
                return new object[] { new MethodImplAttribute((MethodImplOptions)GetMethodImplementationFlags()) };
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                if (attributeType == null)
                    throw new ArgumentNullException(nameof(attributeType));

                if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute)))
                    return true;
                else
                    return false;
            }

            public override bool IsSecurityCritical
            {
                get { return m_owner.IsSecurityCritical; }
            }

            public override bool IsSecuritySafeCritical
            {
                get { return m_owner.IsSecuritySafeCritical; }
            }

            public override bool IsSecurityTransparent
            {
                get { return m_owner.IsSecurityTransparent; }
            }

#pragma warning disable CS8608 // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/23268
            public override Type ReturnType
            {
                get
                {
                    return m_owner.m_returnType;
                }
            }
#pragma warning restore CS8608

            public override ParameterInfo? ReturnParameter
            {
                get { return null; }
            }

#pragma warning disable CS8608 // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/23268
            public override ICustomAttributeProvider ReturnTypeCustomAttributes
            {
                get { return GetEmptyCAHolder(); }
            }
#pragma warning restore CS8608

            //
            // private implementation
            //

            internal RuntimeParameterInfo[] LoadParameters()
            {
                if (m_parameters == null)
                {
                    Type[] parameterTypes = m_owner.m_parameterTypes;
                    RuntimeParameterInfo[] parameters = new RuntimeParameterInfo[parameterTypes.Length];
                    for (int i = 0; i < parameterTypes.Length; i++)
                        parameters[i] = new RuntimeParameterInfo(this, null, parameterTypes[i], i);
                    if (m_parameters == null)
                        // should we interlockexchange?
                        m_parameters = parameters;
                }
                return m_parameters;
            }

            // private implementation of CA for the return type
            private ICustomAttributeProvider GetEmptyCAHolder()
            {
                return new EmptyCAHolder();
            }

            ///////////////////////////////////////////////////
            // EmptyCAHolder
            private class EmptyCAHolder : ICustomAttributeProvider
            {
                internal EmptyCAHolder() { }

                object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit)
                {
                    return Array.Empty<object>();
                }

                object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit)
                {
                    return Array.Empty<object>();
                }

                bool ICustomAttributeProvider.IsDefined(Type attributeType, bool inherit)
                {
                    return false;
                }
            }
        }
    }
}

