// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// Activator is an object that contains the Activation (CreateInstance/New) 
//  methods for late bound support.
//
// 
// 
//
namespace System {

    using System;
    using System.Reflection;
    using System.Runtime.Remoting;
#if FEATURE_REMOTING    
    using System.Runtime.Remoting.Activation;
    using Message = System.Runtime.Remoting.Messaging.Message;
#endif
    using System.Security;
    using CultureInfo = System.Globalization.CultureInfo;
    using Evidence = System.Security.Policy.Evidence;
    using StackCrawlMark = System.Threading.StackCrawlMark;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using AssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    // Only statics, does not need to be marked with the serializable attribute
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_Activator))]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class Activator : _Activator
    {
        internal const int LookupMask                 = 0x000000FF;
        internal const BindingFlags ConLookup         = (BindingFlags) (BindingFlags.Instance | BindingFlags.Public);
        internal const BindingFlags ConstructorDefault= BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        // This class only contains statics, so hide the worthless constructor
        private Activator()
        {
        }

        // CreateInstance
        // The following methods will create a new instance of an Object
        // Full Binding Support
        // For all of these methods we need to get the underlying RuntimeType and
        //  call the Impl version.
        static public Object CreateInstance(Type type,
                                            BindingFlags bindingAttr,
                                            Binder binder,
                                            Object[] args,
                                            CultureInfo culture) 
        {
            return CreateInstance(type, bindingAttr, binder, args, culture, null);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        static public Object CreateInstance(Type type,
                                            BindingFlags bindingAttr,
                                            Binder binder,
                                            Object[] args,
                                            CultureInfo culture,
                                            Object[] activationAttributes)
        {
            if ((object)type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();

            if (type is System.Reflection.Emit.TypeBuilder)
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CreateInstanceWithTypeBuilder"));

            // If they didn't specify a lookup, then we will provide the default lookup.
            if ((bindingAttr & (BindingFlags) LookupMask) == 0)
                bindingAttr |= Activator.ConstructorDefault;

            if (activationAttributes != null && activationAttributes.Length > 0){
                // If type does not derive from MBR
                // throw notsupportedexception
#if FEATURE_REMOTING                
                if(type.IsMarshalByRef){
                    // The fix below is preventative.
                    //
                    if(!(type.IsContextful)){
                        if(activationAttributes.Length > 1 || !(activationAttributes[0] is UrlAttribute))
                           throw new NotSupportedException(Environment.GetResourceString("NotSupported_NonUrlAttrOnMBR"));
                    }
                }
                else
#endif                    
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_ActivAttrOnNonMBR" ));
            }

            RuntimeType rt = type.UnderlyingSystemType as RuntimeType;

            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"type");

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return rt.CreateInstanceImpl(bindingAttr,binder,args,culture,activationAttributes, ref stackMark);
        }

        static public Object CreateInstance(Type type, params Object[] args)
        {
            return CreateInstance(type,
                                  Activator.ConstructorDefault,
                                  null,
                                  args,
                                  null,
                                  null);
        }

        static public Object CreateInstance(Type type,
                                            Object[] args,
                                            Object[] activationAttributes)
        {
             return CreateInstance(type,
                                   Activator.ConstructorDefault,
                                   null,
                                   args,
                                   null,
                                   activationAttributes);
        }
        
        static public Object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type, false);
        }

        /*
         * Create an instance using the name of type and the assembly where it exists. This allows
         * types to be created remotely without having to load the type locally.
         */

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        static public ObjectHandle CreateInstance(String assemblyName,
                                                  String typeName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstance(assemblyName,
                                  typeName, 
                                  false,
                                  Activator.ConstructorDefault,
                                  null,
                                  null,
                                  null,
                                  null,
                                  null,
                                  ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable                                                  
        static public ObjectHandle CreateInstance(String assemblyName,
                                                  String typeName,
                                                  Object[] activationAttributes)
                                                  
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstance(assemblyName,
                                  typeName, 
                                  false,
                                  Activator.ConstructorDefault,
                                  null,
                                  null,
                                  null,
                                  activationAttributes,
                                  null,
                                  ref stackMark);
        }
            
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        static public Object CreateInstance(Type type, bool nonPublic)
        {
            if ((object)type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();

            RuntimeType rt = type.UnderlyingSystemType as RuntimeType;

            if (rt == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "type");

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return rt.CreateInstanceDefaultCtor(!nonPublic, false, true, ref stackMark);
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        static public T CreateInstance<T>()
        {
            RuntimeType rt = typeof(T) as RuntimeType;

            // This is a workaround to maintain compatibility with V2. Without this we would throw a NotSupportedException for void[].
            // Array, Ref, and Pointer types don't have default constructors.
            if (rt.HasElementType)
                throw new MissingMethodException(Environment.GetResourceString("Arg_NoDefCTor"));

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;

            // Skip the CreateInstanceCheckThis call to avoid perf cost and to maintain compatibility with V2 (throwing the same exceptions).
            return (T)rt.CreateInstanceDefaultCtor(true /*publicOnly*/, true /*skipCheckThis*/, true /*fillCache*/, ref stackMark);
        }

        static public ObjectHandle CreateInstanceFrom(String assemblyFile,
                                                      String typeName)
                                         
        {
            return CreateInstanceFrom(assemblyFile, typeName, null);
        }

        static public ObjectHandle CreateInstanceFrom(String assemblyFile,
                                                      String typeName,
                                                      Object[] activationAttributes)
                                         
        {
            return CreateInstanceFrom(assemblyFile,
                                      typeName, 
                                      false,
                                      Activator.ConstructorDefault,
                                      null,
                                      null,
                                      null,
                                      activationAttributes);
        }
                                  
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstance which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public ObjectHandle CreateInstance(String assemblyName, 
                                                  String typeName, 
                                                  bool ignoreCase,
                                                  BindingFlags bindingAttr, 
                                                  Binder binder,
                                                  Object[] args,
                                                  CultureInfo culture,
                                                  Object[] activationAttributes,
                                                  Evidence securityInfo)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstance(assemblyName,
                                  typeName,
                                  ignoreCase,
                                  bindingAttr,
                                  binder,
                                  args,
                                  culture,
                                  activationAttributes,
                                  securityInfo,
                                  ref stackMark);
        }

        [SecuritySafeCritical]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static ObjectHandle CreateInstance(string assemblyName,
                                                  string typeName,
                                                  bool ignoreCase,
                                                  BindingFlags bindingAttr,
                                                  Binder binder,
                                                  object[] args,
                                                  CultureInfo culture,
                                                  object[] activationAttributes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstance(assemblyName,
                                  typeName,
                                  ignoreCase,
                                  bindingAttr,
                                  binder,
                                  args,
                                  culture,
                                  activationAttributes,
                                  null,
                                  ref stackMark);
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal ObjectHandle CreateInstance(String assemblyString, 
                                                    String typeName, 
                                                    bool ignoreCase,
                                                    BindingFlags bindingAttr, 
                                                    Binder binder,
                                                    Object[] args,
                                                    CultureInfo culture,
                                                    Object[] activationAttributes,
                                                    Evidence securityInfo,
                                                    ref StackCrawlMark stackMark)
        {
#if FEATURE_CAS_POLICY
            if (securityInfo != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            Type type = null;
            Assembly assembly = null;
            if (assemblyString == null) {
                assembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
            } else {
                RuntimeAssembly assemblyFromResolveEvent;
                AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(assemblyString, false /*forIntrospection*/, out assemblyFromResolveEvent);
                if (assemblyFromResolveEvent != null) {
                    // Assembly was resolved via AssemblyResolve event
                    assembly = assemblyFromResolveEvent;
                } else if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime) {
                    // WinRT type - we have to use Type.GetType
                    type = Type.GetType(typeName + ", " + assemblyString, true /*throwOnError*/, ignoreCase);
                } else {
                    // Classic managed type
                    assembly = RuntimeAssembly.InternalLoadAssemblyName(
                        assemblyName, securityInfo, null, ref stackMark,
                        true /*thrownOnFileNotFound*/, false /*forIntrospection*/, false /*suppressSecurityChecks*/);
                }
            }

            if (type == null) {
                // It's classic managed type (not WinRT type)
                Log(assembly != null, "CreateInstance:: ", "Loaded " + assembly.FullName, "Failed to Load: " + assemblyString);
                if(assembly == null) return null;

                type = assembly.GetType(typeName, true /*throwOnError*/, ignoreCase);
            }
            
            Object o = Activator.CreateInstance(type,
                                                bindingAttr,
                                                binder,
                                                args,
                                                culture,
                                                activationAttributes);

            Log(o != null, "CreateInstance:: ", "Created Instance of class " + typeName, "Failed to create instance of class " + typeName);
            if(o == null)
                return null;
            else {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
        }

        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstanceFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        static public ObjectHandle CreateInstanceFrom(String assemblyFile,
                                                      String typeName, 
                                                      bool ignoreCase,
                                                      BindingFlags bindingAttr, 
                                                      Binder binder,
                                                      Object[] args,
                                                      CultureInfo culture,
                                                      Object[] activationAttributes,
                                                      Evidence securityInfo)
                                               
        {
#if FEATURE_CAS_POLICY
            if (securityInfo != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            return CreateInstanceFromInternal(assemblyFile,
                                              typeName,
                                              ignoreCase,
                                              bindingAttr,
                                              binder,
                                              args,
                                              culture,
                                              activationAttributes,
                                              securityInfo);
        }

        public static ObjectHandle CreateInstanceFrom(string assemblyFile,
                                                      string typeName,
                                                      bool ignoreCase,
                                                      BindingFlags bindingAttr,
                                                      Binder binder,
                                                      object[] args,
                                                      CultureInfo culture,
                                                      object[] activationAttributes)
        {
            return CreateInstanceFromInternal(assemblyFile,
                                              typeName,
                                              ignoreCase,
                                              bindingAttr,
                                              binder,
                                              args,
                                              culture,
                                              activationAttributes,
                                              null);
        }

        private static ObjectHandle CreateInstanceFromInternal(String assemblyFile,
                                                               String typeName, 
                                                               bool ignoreCase,
                                                               BindingFlags bindingAttr, 
                                                               Binder binder,
                                                               Object[] args,
                                                               CultureInfo culture,
                                                               Object[] activationAttributes,
                                                               Evidence securityInfo)
        {
#if FEATURE_CAS_POLICY
            Contract.Assert(AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled || securityInfo == null);
#endif // FEATURE_CAS_POLICY

#pragma warning disable 618
            Assembly assembly = Assembly.LoadFrom(assemblyFile, securityInfo);
#pragma warning restore 618
            Type t = assembly.GetType(typeName, true, ignoreCase);
            
            Object o = Activator.CreateInstance(t,
                                                bindingAttr,
                                                binder,
                                                args,
                                                culture,
                                                activationAttributes);

            Log(o != null, "CreateInstanceFrom:: ", "Created Instance of class " + typeName, "Failed to create instance of class " + typeName);
            if(o == null)
                return null;
            else {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
        }

        //
        // This API is designed to be used when a host needs to execute code in an AppDomain
        // with restricted security permissions. In that case, we demand in the client domain
        // and assert in the server domain because the server domain might not be trusted enough
        // to pass the security checks when activating the type.
        //

        [System.Security.SecurityCritical]  // auto-generated_required
        public static ObjectHandle CreateInstance (AppDomain domain, string assemblyName, string typeName) {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();
            return domain.InternalCreateInstanceWithNoSecurity(assemblyName, typeName);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("Methods which use evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstance which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static ObjectHandle CreateInstance (AppDomain domain,
                                                   string assemblyName,
                                                   string typeName,
                                                   bool ignoreCase,
                                                   BindingFlags bindingAttr,
                                                   Binder binder,
                                                   Object[] args,
                                                   CultureInfo culture,
                                                   Object[] activationAttributes,
                                                   Evidence securityAttributes) {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();

#if FEATURE_CAS_POLICY
            if (securityAttributes != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            return domain.InternalCreateInstanceWithNoSecurity(assemblyName, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        [SecurityCritical]
        public static ObjectHandle CreateInstance(AppDomain domain,
                                                  string assemblyName,
                                                  string typeName,
                                                  bool ignoreCase,
                                                  BindingFlags bindingAttr,
                                                  Binder binder,
                                                  object[] args,
                                                  CultureInfo culture,
                                                  object[] activationAttributes)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();

            return domain.InternalCreateInstanceWithNoSecurity(assemblyName,
                                                               typeName,
                                                               ignoreCase,
                                                               bindingAttr,
                                                               binder,
                                                               args,
                                                               culture,
                                                               activationAttributes,
                                                               null);
        }

        //
        // This API is designed to be used when a host needs to execute code in an AppDomain
        // with restricted security permissions. In that case, we demand in the client domain
        // and assert in the server domain because the server domain might not be trusted enough
        // to pass the security checks when activating the type.
        //

        [System.Security.SecurityCritical]  // auto-generated_required
        public static ObjectHandle CreateInstanceFrom (AppDomain domain, string assemblyFile, string typeName) {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();
            return domain.InternalCreateInstanceFromWithNoSecurity(assemblyFile, typeName);
        }

        [System.Security.SecurityCritical]  // auto-generated_required
        [Obsolete("Methods which use Evidence to sandbox are obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateInstanceFrom which does not take an Evidence parameter. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static ObjectHandle CreateInstanceFrom (AppDomain domain,
                                                       string assemblyFile,
                                                       string typeName,
                                                       bool ignoreCase,
                                                       BindingFlags bindingAttr,
                                                       Binder binder,
                                                       Object[] args,
                                                       CultureInfo culture,
                                                       Object[] activationAttributes,
                                                       Evidence securityAttributes) {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();

#if FEATURE_CAS_POLICY
            if (securityAttributes != null && !AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_RequiresCasPolicyImplicit"));
            }
#endif // FEATURE_CAS_POLICY

            return domain.InternalCreateInstanceFromWithNoSecurity(assemblyFile, typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes, securityAttributes);
        }

        [SecurityCritical]
        public static ObjectHandle CreateInstanceFrom(AppDomain domain,
                                                      string assemblyFile,
                                                      string typeName,
                                                      bool ignoreCase,
                                                      BindingFlags bindingAttr,
                                                      Binder binder,
                                                      object[] args,
                                                      CultureInfo culture,
                                                      object[] activationAttributes)
        {
            if (domain == null)
                throw new ArgumentNullException("domain");
            Contract.EndContractBlock();

            return domain.InternalCreateInstanceFromWithNoSecurity(assemblyFile,
                                                                   typeName,
                                                                   ignoreCase,
                                                                   bindingAttr,
                                                                   binder,
                                                                   args,
                                                                   culture,
                                                                   activationAttributes,
                                                                   null);
        }

#if FEATURE_CLICKONCE
        [System.Security.SecuritySafeCritical]  // auto-generated
        public static ObjectHandle CreateInstance (ActivationContext activationContext) {
            AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
            if (domainManager == null)
                domainManager = new AppDomainManager();

            return domainManager.ApplicationActivator.CreateInstance(activationContext);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static ObjectHandle CreateInstance (ActivationContext activationContext, string[] activationCustomData) {
            AppDomainManager domainManager = AppDomain.CurrentDomain.DomainManager;
            if (domainManager == null)
                domainManager = new AppDomainManager();

            return domainManager.ApplicationActivator.CreateInstance(activationContext, activationCustomData);
        }
#endif // FEATURE_CLICKONCE

        public static ObjectHandle CreateComInstanceFrom(String assemblyName,
                                                         String typeName)                                         
        {
            return CreateComInstanceFrom(assemblyName,
                                         typeName,
                                         null,
                                         AssemblyHashAlgorithm.None);
                                         
        }
                                         
        public static ObjectHandle CreateComInstanceFrom(String assemblyName,
                                                         String typeName,
                                                         byte[] hashValue, 
                                                         AssemblyHashAlgorithm hashAlgorithm)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyName, hashValue, hashAlgorithm);

            Type t = assembly.GetType(typeName, true, false);

            Object[] Attr = t.GetCustomAttributes(typeof(ComVisibleAttribute),false);
            if (Attr.Length > 0)
            {
                if (((ComVisibleAttribute)Attr[0]).Value == false)
                    throw new TypeLoadException(Environment.GetResourceString( "Argument_TypeMustBeVisibleFromCom" ));
            }

            Log(assembly != null, "CreateInstance:: ", "Loaded " + assembly.FullName, "Failed to Load: " + assemblyName);

            if(assembly == null) return null;

  
            Object o = Activator.CreateInstance(t,
                                                Activator.ConstructorDefault,
                                                null,
                                                null,
                                                null,
                                                null);

            Log(o != null, "CreateInstance:: ", "Created Instance of class " + typeName, "Failed to create instance of class " + typeName);
            if(o == null)
                return null;
            else {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
        }

#if FEATURE_REMOTING
        //  This method is a helper method and delegates to the remoting 
        //  services to do the actual work. 
        [System.Security.SecurityCritical]  // auto-generated_required
        static public Object GetObject(Type type, String url)
        {
            return GetObject(type, url, null);
        }
        
        //  This method is a helper method and delegates to the remoting 
        //  services to do the actual work. 
        [System.Security.SecurityCritical]  // auto-generated_required
        static public Object GetObject(Type type, String url, Object state)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.EndContractBlock();
            return RemotingServices.Connect(type, url, state);
        }
#endif

        [System.Diagnostics.Conditional("_DEBUG")]
        private static void Log(bool test, string title, string success, string failure)
        {
#if FEATURE_REMOTING
            if(test)
                BCLDebug.Trace("REMOTE", "{0}{1}", title, success);
            else
                BCLDebug.Trace("REMOTE", "{0}{1}", title, failure);
#endif            
        }
        
        void _Activator.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _Activator.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _Activator.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        // If you implement this method, make sure to include _Activator.Invoke in VM\DangerousAPIs.h and
        // include _Activator in SystemDomain::IsReflectionInvocationMethod in AppDomain.cpp.
        void _Activator.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
    }
}

