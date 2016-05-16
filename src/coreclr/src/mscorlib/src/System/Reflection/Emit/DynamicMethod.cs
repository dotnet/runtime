// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection.Emit
{
    using System;
    using System.Collections.Generic;
    using CultureInfo = System.Globalization.CultureInfo;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
    using System.Runtime.InteropServices;

    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class DynamicMethod : MethodInfo
    {
        private RuntimeType[] m_parameterTypes;
        internal IRuntimeMethodInfo m_methodHandle;
        private RuntimeType m_returnType;
        private DynamicILGenerator m_ilGenerator;
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        private DynamicILInfo m_DynamicILInfo;
        private bool m_fInitLocals;
        private RuntimeModule m_module;
        internal bool m_skipVisibility;
        internal RuntimeType m_typeOwner; // can be null

        // We want the creator of the DynamicMethod to control who has access to the
        // DynamicMethod (just like we do for delegates). However, a user can get to
        // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc.
        // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would
        // not be able to bound access to the DynamicMethod. Hence, we need to ensure that 
        // we do not allow direct use of RTDynamicMethod.
        private RTDynamicMethod m_dynMethod;

        // needed to keep the object alive during jitting
        // assigned by the DynamicResolver ctor
        internal DynamicResolver m_resolver;

        // Always false unless we are in an immersive (non dev mode) process.
#if FEATURE_APPX
        private bool m_profileAPICheck;

        private RuntimeAssembly m_creatorAssembly;
#endif

        internal bool m_restrictedSkipVisibility;
        // The context when the method was created. We use this to do the RestrictedMemberAccess checks.
        // These checks are done when the method is compiled. This can happen at an arbitrary time,
        // when CreateDelegate or Invoke is called, or when another DynamicMethod executes OpCodes.Call.
        // We capture the creation context so that we can do the checks against the same context,
        // irrespective of when the method gets compiled. Note that the DynamicMethod does not know when
        // it is ready for use since there is not API which indictates that IL generation has completed.
#if FEATURE_COMPRESSEDSTACK
        internal CompressedStack m_creationContext;
#endif // FEATURE_COMPRESSEDSTACK
        private static volatile InternalModuleBuilder s_anonymouslyHostedDynamicMethodsModule;
        private static readonly object s_anonymouslyHostedDynamicMethodsModuleLock = new object();
        
        //
        // class initialization (ctor and init)
        //

        private DynamicMethod() { }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard, 
                returnType, 
                parameterTypes,
                null,   // owner
                null,   // m
                false,  // skipVisibility
                true,
                ref stackMark);  // transparentMethod
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public DynamicMethod(string name,
                             Type returnType,
                             Type[] parameterTypes,
                             bool restrictedSkipVisibility)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                null,   // m
                restrictedSkipVisibility,
                true,
                ref stackMark);  // transparentMethod
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public DynamicMethod(string name, 
                             Type returnType, 
                             Type[] parameterTypes, 
                             Module m) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(m, ref stackMark, false);
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                false,  // skipVisibility
                false,
                ref stackMark);  // transparentMethod
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public DynamicMethod(string name, 
                             Type returnType, 
                             Type[] parameterTypes, 
                             Module m, 
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(m, ref stackMark, skipVisibility);
            Init(name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                skipVisibility,
                false,
                ref stackMark); // transparentMethod
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public DynamicMethod(string name, 
                             MethodAttributes attributes, 
                             CallingConventions callingConvention, 
                             Type returnType, 
                             Type[] parameterTypes, 
                             Module m, 
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(m, ref stackMark, skipVisibility);
            Init(name,
                attributes,
                callingConvention,
                returnType,
                parameterTypes,
                null,   // owner
                m,      // m
                skipVisibility,
                false,
                ref stackMark); // transparentMethod
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public DynamicMethod(string name, 
                             Type returnType, 
                             Type[] parameterTypes, 
                             Type owner) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(owner, ref stackMark, false);
            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard, 
                returnType, 
                parameterTypes,
                owner,  // owner
                null,   // m
                false,  // skipVisibility
                false,
                ref stackMark); // transparentMethod
        }
        
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public DynamicMethod(string name, 
                             Type returnType, 
                             Type[] parameterTypes, 
                             Type owner, 
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(owner, ref stackMark, skipVisibility);
            Init(name, 
                MethodAttributes.Public | MethodAttributes.Static, 
                CallingConventions.Standard, 
                returnType, 
                parameterTypes, 
                owner,  // owner
                null,   // m
                skipVisibility,
                false,
                ref stackMark); // transparentMethod
        }
        
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #else
        [System.Security.SecuritySafeCritical]
        #endif
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public DynamicMethod(string name, 
                             MethodAttributes attributes, 
                             CallingConventions callingConvention, 
                             Type returnType, 
                             Type[] parameterTypes, 
                             Type owner, 
                             bool skipVisibility) {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            PerformSecurityCheck(owner, ref stackMark, skipVisibility);
            Init(name, 
                attributes, 
                callingConvention, 
                returnType, 
                parameterTypes, 
                owner,  // owner
                null,   // m
                skipVisibility, 
                false,
                ref stackMark); // transparentMethod
        }

        // helpers for intialization

        static private void CheckConsistency(MethodAttributes attributes, CallingConventions callingConvention) {
            // only static public for method attributes
            if ((attributes & ~MethodAttributes.MemberAccessMask) != MethodAttributes.Static)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
            if ((attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
            Contract.EndContractBlock();

            // only standard or varargs supported
            if (callingConvention != CallingConventions.Standard && callingConvention != CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
            
            // vararg is not supported at the moment
            if (callingConvention == CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_DynamicMethodFlags"));
        }

        // We create a transparent assembly to host DynamicMethods. Since the assembly does not have any
        // non-public fields (or any fields at all), it is a safe anonymous assembly to host DynamicMethods
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        private static RuntimeModule GetDynamicMethodsModule()
        {
            if (s_anonymouslyHostedDynamicMethodsModule != null)
                return s_anonymouslyHostedDynamicMethodsModule;

            lock (s_anonymouslyHostedDynamicMethodsModuleLock)
            {
                if (s_anonymouslyHostedDynamicMethodsModule != null)
                    return s_anonymouslyHostedDynamicMethodsModule;

                ConstructorInfo transparencyCtor = typeof(SecurityTransparentAttribute).GetConstructor(Type.EmptyTypes);
                CustomAttributeBuilder transparencyAttribute = new CustomAttributeBuilder(transparencyCtor, EmptyArray<Object>.Value);
                List<CustomAttributeBuilder> assemblyAttributes = new List<CustomAttributeBuilder>();
                assemblyAttributes.Add(transparencyAttribute);
#if !FEATURE_CORECLR
                // On the desktop, we need to use the security rule set level 1 for anonymously hosted
                // dynamic methods.  In level 2, transparency rules are strictly enforced, which leads to
                // errors when a fully trusted application causes a dynamic method to be generated that tries
                // to call a method with a LinkDemand or a SecurityCritical method.  To retain compatibility
                // with the v2.0 and v3.x frameworks, these calls should be allowed.
                //
                // If this rule set was not explicitly called out, then the anonymously hosted dynamic methods
                // assembly would inherit the rule set from the creating assembly - which would cause it to
                // be level 2 because mscorlib.dll is using the level 2 rules.
                ConstructorInfo securityRulesCtor = typeof(SecurityRulesAttribute).GetConstructor(new Type[] { typeof(SecurityRuleSet) });
                CustomAttributeBuilder securityRulesAttribute =
                    new CustomAttributeBuilder(securityRulesCtor, new object[] { SecurityRuleSet.Level1 });
                assemblyAttributes.Add(securityRulesAttribute);
#endif // !FEATURE_CORECLR

                AssemblyName assemblyName = new AssemblyName("Anonymously Hosted DynamicMethods Assembly");
                StackCrawlMark stackMark = StackCrawlMark.LookForMe;

                AssemblyBuilder assembly = AssemblyBuilder.InternalDefineDynamicAssembly(
                    assemblyName,
                    AssemblyBuilderAccess.Run,
                    null, null, null, null, null,
                    ref stackMark,
                    assemblyAttributes,
                    SecurityContextSource.CurrentAssembly);

                AppDomain.PublishAnonymouslyHostedDynamicMethodsAssembly(assembly.GetNativeHandle());

                // this always gets the internal module.
                s_anonymouslyHostedDynamicMethodsModule = (InternalModuleBuilder)assembly.ManifestModule;
            }

            return s_anonymouslyHostedDynamicMethodsModule;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe void Init(String name, 
                                 MethodAttributes attributes, 
                                 CallingConventions callingConvention, 
                                 Type returnType, 
                                 Type[] signature, 
                                 Type owner, 
                                 Module m, 
                                 bool skipVisibility,
                                 bool transparentMethod,
                                 ref StackCrawlMark stackMark)
        {
            DynamicMethod.CheckConsistency(attributes, callingConvention);

            // check and store the signature
            if (signature != null) {
                m_parameterTypes = new RuntimeType[signature.Length];
                for (int i = 0; i < signature.Length; i++) {
                    if (signature[i] == null) 
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature"));
                    m_parameterTypes[i] = signature[i].UnderlyingSystemType as RuntimeType;
                    if ( m_parameterTypes[i] == null || !(m_parameterTypes[i] is RuntimeType) || m_parameterTypes[i] == (RuntimeType)typeof(void) ) 
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidTypeInSignature"));
                }
            }
            else {
                m_parameterTypes = Array.Empty<RuntimeType>();
            }
            
            // check and store the return value
            m_returnType = (returnType == null) ? (RuntimeType)typeof(void) : returnType.UnderlyingSystemType as RuntimeType;
            if ( (m_returnType == null) || !(m_returnType is RuntimeType) || m_returnType.IsByRef ) 
                throw new NotSupportedException(Environment.GetResourceString("Arg_InvalidTypeInRetType"));

            if (transparentMethod)
            {
                Contract.Assert(owner == null && m == null, "owner and m cannot be set for transparent methods");
                m_module = GetDynamicMethodsModule();
                if (skipVisibility)
                {
                    m_restrictedSkipVisibility = true;
                }

#if FEATURE_COMPRESSEDSTACK
                m_creationContext = CompressedStack.Capture();
#endif // FEATURE_COMPRESSEDSTACK
            }
            else
            {
                Contract.Assert(m != null || owner != null, "PerformSecurityCheck should ensure that either m or owner is set");
                Contract.Assert(m == null || !m.Equals(s_anonymouslyHostedDynamicMethodsModule), "The user cannot explicitly use this assembly");
                Contract.Assert(m == null || owner == null, "m and owner cannot both be set");

                if (m != null)
                    m_module = m.ModuleHandle.GetRuntimeModule(); // this returns the underlying module for all RuntimeModule and ModuleBuilder objects.
                else
                {
                    RuntimeType rtOwner = null;
                    if (owner != null)
                        rtOwner = owner.UnderlyingSystemType as RuntimeType;

                    if (rtOwner != null)
                    {
                        if (rtOwner.HasElementType || rtOwner.ContainsGenericParameters
                            || rtOwner.IsGenericParameter || rtOwner.IsInterface)
                            throw new ArgumentException(Environment.GetResourceString("Argument_InvalidTypeForDynamicMethod"));

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
                throw new ArgumentNullException("name");

#if FEATURE_APPX
            if (AppDomain.ProfileAPICheck)
            {
                if (m_creatorAssembly == null)
                    m_creatorAssembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);

                if (m_creatorAssembly != null && !m_creatorAssembly.IsFrameworkAssembly())
                    m_profileAPICheck = true;
            }
#endif // FEATURE_APPX

            m_dynMethod = new RTDynamicMethod(this, name, attributes, callingConvention);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void PerformSecurityCheck(Module m, ref StackCrawlMark stackMark, bool skipVisibility)
        {
            if (m == null) 
                throw new ArgumentNullException("m");
            Contract.EndContractBlock();
#if !FEATURE_CORECLR

            RuntimeModule rtModule;
            ModuleBuilder mb = m as ModuleBuilder;
            if (mb != null)
                rtModule = mb.InternalModule;
            else
                rtModule = m as RuntimeModule;

            if (rtModule == null)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeModule"), "m");
            }

            // The user cannot explicitly use this assembly
            if (rtModule == s_anonymouslyHostedDynamicMethodsModule)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"), "m");

            // ask for member access if skip visibility
            if (skipVisibility) 
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();

#if !FEATURE_CORECLR
            // ask for control evidence if outside of the caller assembly
            RuntimeType callingType = RuntimeMethodHandle.GetCallerType(ref stackMark);
            m_creatorAssembly = callingType.GetRuntimeAssembly();
            if (m.Assembly != m_creatorAssembly)
            {
                // Demand the permissions of the assembly where the DynamicMethod will live
                CodeAccessSecurityEngine.ReflectionTargetDemandHelper(PermissionType.SecurityControlEvidence,
                                                                      m.Assembly.PermissionSet);
            }
#else //FEATURE_CORECLR
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
#pragma warning restore 618
#endif //FEATURE_CORECLR
#endif //!FEATURE_CORECLR
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void PerformSecurityCheck(Type owner, ref StackCrawlMark stackMark, bool skipVisibility)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");
#if !FEATURE_CORECLR

            RuntimeType rtOwner = owner as RuntimeType;
            if (rtOwner == null)
                rtOwner = owner.UnderlyingSystemType as RuntimeType;

            if (rtOwner == null)
                throw new ArgumentNullException("owner", Environment.GetResourceString("Argument_MustBeRuntimeType"));

            // get the type the call is coming from
            RuntimeType callingType = RuntimeMethodHandle.GetCallerType(ref stackMark);

            // ask for member access if skip visibility
            if (skipVisibility) 
                new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
            else
            {
                // if the call is not coming from the same class ask for member access
                if (callingType != rtOwner)
                    new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
            }
#if !FEATURE_CORECLR
            m_creatorAssembly = callingType.GetRuntimeAssembly();

            // ask for control evidence if outside of the caller module
            if (rtOwner.Assembly != m_creatorAssembly)
            {
                // Demand the permissions of the assembly where the DynamicMethod will live
                CodeAccessSecurityEngine.ReflectionTargetDemandHelper(PermissionType.SecurityControlEvidence,
                                                                      owner.Assembly.PermissionSet);
            }
#else //FEATURE_CORECLR
#pragma warning disable 618
                new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Demand();
#pragma warning restore 618
#endif //FEATURE_CORECLR
#endif //!FEATURE_CORECLR
        }

        //
        // Delegate and method creation
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        public sealed override Delegate CreateDelegate(Type delegateType) {
            if (m_restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation.
                GetMethodDescriptor();
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(m_methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, null, GetMethodDescriptor());
            // stash this MethodInfo by brute force.  
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [System.Runtime.InteropServices.ComVisible(true)]
        public sealed override Delegate CreateDelegate(Type delegateType, Object target) {
            if (m_restrictedSkipVisibility)
            {
                // Compile the method since accessibility checks are done as part of compilation
                GetMethodDescriptor();
                System.Runtime.CompilerServices.RuntimeHelpers._CompileMethod(m_methodHandle);
            }

            MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, GetMethodDescriptor());
            // stash this MethodInfo by brute force. 
            d.StoreDynamicMethod(GetMethodInfo());
            return d;
        }

#if FEATURE_APPX
        internal bool ProfileAPICheck
        {
            get
            {
                return m_profileAPICheck;
            }

            [FriendAccessAllowed]
            set
            {
                m_profileAPICheck = value;
            }
        }
#endif

        // This is guaranteed to return a valid handle
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe RuntimeMethodHandle GetMethodDescriptor() {
            if (m_methodHandle == null) {
                lock (this) {
                    if (m_methodHandle == null) {
                        if (m_DynamicILInfo != null)
                            m_DynamicILInfo.GetCallableMethod(m_module, this);
                        else {
                            if (m_ilGenerator == null || m_ilGenerator.ILOffset == 0)
                                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_BadEmptyMethodBody", Name));
    
                            m_ilGenerator.GetCallableMethod(m_module, this);
                        }
                    }
                }
            }
            return new RuntimeMethodHandle(m_methodHandle);
        }

        //
        // MethodInfo api. They mostly forward to RTDynamicMethod
        //

        public override String ToString() { return m_dynMethod.ToString(); }

        public override String Name { get { return m_dynMethod.Name; } }

        public override Type DeclaringType { get { return m_dynMethod.DeclaringType; } }

        public override Type ReflectedType { get { return m_dynMethod.ReflectedType; } }

        public override Module Module { get { return m_dynMethod.Module; } }

        // we cannot return a MethodHandle because we cannot track it via GC so this method is off limits
        public override RuntimeMethodHandle MethodHandle { get { throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod")); } }

        public override MethodAttributes Attributes { get { return m_dynMethod.Attributes; } }

        public override CallingConventions CallingConvention { get { return m_dynMethod.CallingConvention; } }

        public override MethodInfo GetBaseDefinition() { return this; }

        [Pure]
        public override ParameterInfo[] GetParameters() { return m_dynMethod.GetParameters(); }

        public override MethodImplAttributes GetMethodImplementationFlags() { return m_dynMethod.GetMethodImplementationFlags(); }

        //
        // Security transparency accessors
        //
        // Since the dynamic method may not be JITed yet, we don't always have the runtime method handle
        // which is needed to determine the official runtime transparency status of the dynamic method.  We
        // fall back to saying that the dynamic method matches the transparency of its containing module
        // until we get a JITed version, since dynamic methods cannot have attributes of their own.
        //

        public override bool IsSecurityCritical
        {
            [SecuritySafeCritical]
            get
            {
                if (m_methodHandle != null)
                {
                    return RuntimeMethodHandle.IsSecurityCritical(m_methodHandle);
                }
                else if (m_typeOwner != null)
                {
                    RuntimeAssembly assembly = m_typeOwner.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);

                    return assembly.IsAllSecurityCritical();
                }
                else
                {
                    RuntimeAssembly assembly = m_module.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);

                    return assembly.IsAllSecurityCritical();
                }
            }
        }

        public override bool IsSecuritySafeCritical
        {
            [SecuritySafeCritical]
            get
            {
                if (m_methodHandle != null)
                {
                    return RuntimeMethodHandle.IsSecuritySafeCritical(m_methodHandle);
                }
                else if (m_typeOwner != null)
                {
                    RuntimeAssembly assembly = m_typeOwner.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);

                    return assembly.IsAllPublicAreaSecuritySafeCritical();
                }
                else
                {
                    RuntimeAssembly assembly = m_module.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);

                    return assembly.IsAllSecuritySafeCritical();
                }
            }
        }

        public override bool IsSecurityTransparent
        {
            [SecuritySafeCritical]
            get
            {
                if (m_methodHandle != null)
                {
                    return RuntimeMethodHandle.IsSecurityTransparent(m_methodHandle);
                }
                else if (m_typeOwner != null)
                {
                    RuntimeAssembly assembly = m_typeOwner.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);

                    return !assembly.IsAllSecurityCritical();
                }
                else
                {
                    RuntimeAssembly assembly = m_module.Assembly as RuntimeAssembly;
                    Contract.Assert(assembly != null);

                    return !assembly.IsAllSecurityCritical();
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture) {
            if ((CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CallToVarArg"));
            Contract.EndContractBlock();

            //
            // We do not demand any permission here because the caller already has access
            // to the current DynamicMethod object, and it could just as easily emit another 
            // Transparent DynamicMethod to call the current DynamicMethod.
            //

            RuntimeMethodHandle method = GetMethodDescriptor();
            // ignore obj since it's a static method

            // create a signature object
            Signature sig = new Signature(
                this.m_methodHandle, m_parameterTypes, m_returnType, CallingConvention);


            // verify arguments
            int formalCount = sig.Arguments.Length;
            int actualCount = (parameters != null) ? parameters.Length : 0;
            if (formalCount != actualCount)
                throw new TargetParameterCountException(Environment.GetResourceString("Arg_ParmCnt"));

            // if we are here we passed all the previous checks. Time to look at the arguments
            Object retValue = null;
            if (actualCount > 0)
            {
                Object[] arguments = CheckArguments(parameters, binder, invokeAttr, culture, sig);
                retValue = RuntimeMethodHandle.InvokeMethod(null, arguments, sig, false);
                // copy out. This should be made only if ByRef are present.
                for (int index = 0; index < arguments.Length; index++)
                    parameters[index] = arguments[index];
            }
            else
            {
                retValue = RuntimeMethodHandle.InvokeMethod(null, null, sig, false);
            }

            GC.KeepAlive(this);
            return retValue;
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return m_dynMethod.GetCustomAttributes(attributeType, inherit); 
        }

        public override Object[] GetCustomAttributes(bool inherit) { return m_dynMethod.GetCustomAttributes(inherit); }

        public override bool IsDefined(Type attributeType, bool inherit) { return m_dynMethod.IsDefined(attributeType, inherit); }

        public override Type ReturnType { get { return m_dynMethod.ReturnType; } }

        public override ParameterInfo ReturnParameter { get { return m_dynMethod.ReturnParameter; } }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes { get { return m_dynMethod.ReturnTypeCustomAttributes; } }

        //
        // DynamicMethod specific methods
        //

        public ParameterBuilder DefineParameter(int position, ParameterAttributes attributes, String parameterName) {
            if (position < 0 || position > m_parameterTypes.Length)
                throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_ParamSequence"));
            position--; // it's 1 based. 0 is the return value
        
            if (position >= 0) {
                ParameterInfo[] parameters = m_dynMethod.LoadParameters();
                parameters[position].SetName(parameterName);
                parameters[position].SetAttributes(attributes);
            }
            return null;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public DynamicILInfo GetDynamicILInfo()
        {
#pragma warning disable 618
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
#pragma warning restore 618

            if (m_DynamicILInfo != null)
                return m_DynamicILInfo;

            return GetDynamicILInfo(new DynamicScope());
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal DynamicILInfo GetDynamicILInfo(DynamicScope scope)
        {
            if (m_DynamicILInfo == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                        null, CallingConvention, ReturnType, null, null, m_parameterTypes, null, null).GetSignature(true);
                m_DynamicILInfo = new DynamicILInfo(scope, this, methodSignature);
            }

            return m_DynamicILInfo;
        }

        public ILGenerator GetILGenerator() {
            return GetILGenerator(64);
        }

       [System.Security.SecuritySafeCritical]  // auto-generated
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

        public bool InitLocals {
            get {return m_fInitLocals;}
            set {m_fInitLocals = value;}
        }

        //
        // Internal API
        //
         
        internal MethodInfo GetMethodInfo() {
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
        internal class RTDynamicMethod : MethodInfo {

            internal DynamicMethod m_owner;
            ParameterInfo[] m_parameters;
            String m_name;
            MethodAttributes m_attributes;
            CallingConventions m_callingConvention;

            //
            // ctors
            //
            private RTDynamicMethod() {}

            internal RTDynamicMethod(DynamicMethod owner, String name, MethodAttributes attributes, CallingConventions callingConvention) {
                m_owner = owner;
                m_name = name;
                m_attributes = attributes;
                m_callingConvention = callingConvention;
            }
            
            //
            // MethodInfo api
            //
            public override String ToString() {
                return ReturnType.FormatTypeName() + " " + FormatNameAndSig();
            }

            public override String Name { 
                get { return m_name; }
            }

            public override Type DeclaringType { 
                get { return null; }
            }

            public override Type ReflectedType { 
                get { return null; }
            }

            public override Module Module { 
                get { return m_owner.m_module; }
            }

            public override RuntimeMethodHandle MethodHandle { 
                get { throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_NotAllowedInDynamicMethod")); }   
            }

            public override MethodAttributes Attributes { 
                get { return m_attributes; }
            }    

            public override CallingConventions CallingConvention { 
                get { return m_callingConvention; }
            }
            
            public override MethodInfo GetBaseDefinition() {
                return this;
            }
            
            [Pure]
            public override ParameterInfo[] GetParameters() {
                ParameterInfo[] privateParameters = LoadParameters();
                ParameterInfo[] parameters = new ParameterInfo[privateParameters.Length];
                Array.Copy(privateParameters, 0, parameters, 0, privateParameters.Length);
                return parameters;
            }
            
            public override MethodImplAttributes GetMethodImplementationFlags() {
                return MethodImplAttributes.IL | MethodImplAttributes.NoInlining;
            }

            public override Object Invoke(Object obj, BindingFlags invokeAttr, Binder binder, Object[] parameters, CultureInfo culture) {
                // We want the creator of the DynamicMethod to control who has access to the
                // DynamicMethod (just like we do for delegates). However, a user can get to
                // the corresponding RTDynamicMethod using Exception.TargetSite, StackFrame.GetMethod, etc.
                // If we allowed use of RTDynamicMethod, the creator of the DynamicMethod would
                // not be able to bound access to the DynamicMethod. Hence, we do not allow
                // direct use of RTDynamicMethod.
                throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeMethodInfo"), "this");
            }

            public override Object[] GetCustomAttributes(Type attributeType, bool inherit) {
                if (attributeType == null)
                    throw new ArgumentNullException("attributeType");
                Contract.EndContractBlock();

                if (attributeType.IsAssignableFrom(typeof(MethodImplAttribute))) 
                    return new Object[] { new MethodImplAttribute(GetMethodImplementationFlags()) };
                else
                    return EmptyArray<Object>.Value;
            }

            public override Object[] GetCustomAttributes(bool inherit) {
                // support for MethodImplAttribute PCA
                return new Object[] { new MethodImplAttribute(GetMethodImplementationFlags()) };
            }
            
            public override bool IsDefined(Type attributeType, bool inherit) {
                if (attributeType == null)
                    throw new ArgumentNullException("attributeType");
                Contract.EndContractBlock();

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

            public override Type ReturnType
            {
                get
                {
                    return m_owner.m_returnType;
                }
            }

            public override ParameterInfo ReturnParameter { 
                get { return null; } 
            }

            public override ICustomAttributeProvider ReturnTypeCustomAttributes {
                get { return GetEmptyCAHolder(); }
            }

            //
            // private implementation
            //

            internal ParameterInfo[] LoadParameters() {
                if (m_parameters == null) {
                    Type[] parameterTypes = m_owner.m_parameterTypes;
                    ParameterInfo[] parameters = new ParameterInfo[parameterTypes.Length];
                    for (int i = 0; i < parameterTypes.Length; i++) 
                        parameters[i] = new RuntimeParameterInfo(this, null, parameterTypes[i], i);
                    if (m_parameters == null) 
                        // should we interlockexchange?
                        m_parameters = parameters;
                }
                return m_parameters;
            }
            
            // private implementation of CA for the return type
            private ICustomAttributeProvider GetEmptyCAHolder() {
                return new EmptyCAHolder();
            }

            ///////////////////////////////////////////////////
            // EmptyCAHolder
            private class EmptyCAHolder : ICustomAttributeProvider {
                internal EmptyCAHolder() {}

                Object[] ICustomAttributeProvider.GetCustomAttributes(Type attributeType, bool inherit) {
                    return EmptyArray<Object>.Value;
                }

                Object[] ICustomAttributeProvider.GetCustomAttributes(bool inherit) {
                    return EmptyArray<Object>.Value;
                }

                bool ICustomAttributeProvider.IsDefined (Type attributeType, bool inherit) {
                    return false;
                }
            }

        }
    
    }

}

