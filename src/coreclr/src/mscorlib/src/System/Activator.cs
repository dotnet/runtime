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

namespace System
{
    using System;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Security;
    using CultureInfo = System.Globalization.CultureInfo;
    using Evidence = System.Security.Policy.Evidence;
    using StackCrawlMark = System.Threading.StackCrawlMark;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using AssemblyHashAlgorithm = System.Configuration.Assemblies.AssemblyHashAlgorithm;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    // Only statics, does not need to be marked with the serializable attribute
    public sealed class Activator
    {
        internal const int LookupMask = 0x000000FF;
        internal const BindingFlags ConLookup = (BindingFlags)(BindingFlags.Instance | BindingFlags.Public);
        internal const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

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

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        static public Object CreateInstance(Type type,
                                            BindingFlags bindingAttr,
                                            Binder binder,
                                            Object[] args,
                                            CultureInfo culture,
                                            Object[] activationAttributes)
        {
            if ((object)type == null)
                throw new ArgumentNullException(nameof(type));
            Contract.EndContractBlock();

            if (type is System.Reflection.Emit.TypeBuilder)
                throw new NotSupportedException(SR.NotSupported_CreateInstanceWithTypeBuilder);

            // If they didn't specify a lookup, then we will provide the default lookup.
            if ((bindingAttr & (BindingFlags)LookupMask) == 0)
                bindingAttr |= Activator.ConstructorDefault;

            if (activationAttributes != null && activationAttributes.Length > 0)
            {
                throw new PlatformNotSupportedException(SR.NotSupported_ActivAttr);
            }

            RuntimeType rt = type.UnderlyingSystemType as RuntimeType;

            if (rt == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return rt.CreateInstanceImpl(bindingAttr, binder, args, culture, activationAttributes, ref stackMark);
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

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
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

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod                                                  
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

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        static public Object CreateInstance(Type type, bool nonPublic)
        {
            if ((object)type == null)
                throw new ArgumentNullException(nameof(type));
            Contract.EndContractBlock();

            RuntimeType rt = type.UnderlyingSystemType as RuntimeType;

            if (rt == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return rt.CreateInstanceDefaultCtor(!nonPublic, false, true, ref stackMark);
        }

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
        static public T CreateInstance<T>()
        {
            RuntimeType rt = typeof(T) as RuntimeType;

            // This is a workaround to maintain compatibility with V2. Without this we would throw a NotSupportedException for void[].
            // Array, Ref, and Pointer types don't have default constructors.
            if (rt.HasElementType)
                throw new MissingMethodException(SR.Arg_NoDefCTor);

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

        [System.Security.DynamicSecurityMethod] // Methods containing StackCrawlMark local var has to be marked DynamicSecurityMethod
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
            Type type = null;
            Assembly assembly = null;
            if (assemblyString == null)
            {
                assembly = RuntimeAssembly.GetExecutingAssembly(ref stackMark);
            }
            else
            {
                RuntimeAssembly assemblyFromResolveEvent;
                AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(assemblyString, false /*forIntrospection*/, out assemblyFromResolveEvent);
                if (assemblyFromResolveEvent != null)
                {
                    // Assembly was resolved via AssemblyResolve event
                    assembly = assemblyFromResolveEvent;
                }
                else if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
                {
                    // WinRT type - we have to use Type.GetType
                    type = Type.GetType(typeName + ", " + assemblyString, true /*throwOnError*/, ignoreCase);
                }
                else
                {
                    // Classic managed type
                    assembly = RuntimeAssembly.InternalLoadAssemblyName(
                        assemblyName, securityInfo, null, ref stackMark,
                        true /*thrownOnFileNotFound*/, false /*forIntrospection*/);
                }
            }

            if (type == null)
            {
                // It's classic managed type (not WinRT type)
                Log(assembly != null, "CreateInstance:: ", "Loaded " + assembly.FullName, "Failed to Load: " + assemblyString);
                if (assembly == null) return null;

                type = assembly.GetType(typeName, true /*throwOnError*/, ignoreCase);
            }

            Object o = Activator.CreateInstance(type,
                                                bindingAttr,
                                                binder,
                                                args,
                                                culture,
                                                activationAttributes);

            Log(o != null, "CreateInstance:: ", "Created Instance of class " + typeName, "Failed to create instance of class " + typeName);
            if (o == null)
                return null;
            else
            {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
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
            if (o == null)
                return null;
            else
            {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
        }

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

            Object[] Attr = t.GetCustomAttributes(typeof(ComVisibleAttribute), false);
            if (Attr.Length > 0)
            {
                if (((ComVisibleAttribute)Attr[0]).Value == false)
                    throw new TypeLoadException(SR.Argument_TypeMustBeVisibleFromCom);
            }

            Log(assembly != null, "CreateInstance:: ", "Loaded " + assembly.FullName, "Failed to Load: " + assemblyName);

            if (assembly == null) return null;


            Object o = Activator.CreateInstance(t,
                                                Activator.ConstructorDefault,
                                                null,
                                                null,
                                                null,
                                                null);

            Log(o != null, "CreateInstance:: ", "Created Instance of class " + typeName, "Failed to create instance of class " + typeName);
            if (o == null)
                return null;
            else
            {
                ObjectHandle Handle = new ObjectHandle(o);
                return Handle;
            }
        }

        [System.Diagnostics.Conditional("_DEBUG")]
        private static void Log(bool test, string title, string success, string failure)
        {
        }
    }
}

