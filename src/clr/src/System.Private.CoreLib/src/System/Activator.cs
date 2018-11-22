// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Globalization;
using System.Runtime.Remoting;
using System.Threading;

namespace System
{
    /// <summary>
    /// Activator contains the Activation (CreateInstance/New) methods for late bound support.
    /// </summary>
    public static class Activator
    {
        internal const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        public static object CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture) =>
            CreateInstance(type, bindingAttr, binder, args, culture, null);

        public static object CreateInstance(Type type, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (type is System.Reflection.Emit.TypeBuilder)
                throw new NotSupportedException(SR.NotSupported_CreateInstanceWithTypeBuilder);

            // If they didn't specify a lookup, then we will provide the default lookup.
            const int LookupMask = 0x000000FF;
            if ((bindingAttr & (BindingFlags)LookupMask) == 0)
                bindingAttr |= ConstructorDefault;

            if (activationAttributes?.Length > 0)
                throw new PlatformNotSupportedException(SR.NotSupported_ActivAttr);

            if (type.UnderlyingSystemType is RuntimeType rt)
                return rt.CreateInstanceImpl(bindingAttr, binder, args, culture, activationAttributes);

            throw new ArgumentException(SR.Arg_MustBeType, nameof(type));
        }

        public static object CreateInstance(Type type, params object[] args) =>
            CreateInstance(type, ConstructorDefault, null, args, null, null);

        public static object CreateInstance(Type type, object[] args, object[] activationAttributes) =>
            CreateInstance(type, ConstructorDefault, null, args, null, activationAttributes);

        public static object CreateInstance(Type type) =>
            CreateInstance(type, nonPublic: false);

        public static object CreateInstance(Type type, bool nonPublic) =>
            CreateInstance(type, nonPublic, wrapExceptions: true);

        internal static object CreateInstance(Type type, bool nonPublic, bool wrapExceptions)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (type.UnderlyingSystemType is RuntimeType rt)
                return rt.CreateInstanceDefaultCtor(publicOnly: !nonPublic, skipCheckThis: false, fillCache: true, wrapExceptions: wrapExceptions);

            throw new ArgumentException(SR.Arg_MustBeType, nameof(type));
        }

        [System.Security.DynamicSecurityMethod]
        public static ObjectHandle CreateInstance(string assemblyName, string typeName)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstanceInternal(assemblyName,
                                          typeName,
                                          false,
                                          Activator.ConstructorDefault,
                                          null,
                                          null,
                                          null,
                                          null,
                                          ref stackMark);
        }

        [System.Security.DynamicSecurityMethod]
        public static ObjectHandle CreateInstance(string assemblyName, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstanceInternal(assemblyName,
                                          typeName,
                                          ignoreCase,
                                          bindingAttr,
                                          binder,
                                          args,
                                          culture,
                                          activationAttributes,
                                          ref stackMark);
        }

        [System.Security.DynamicSecurityMethod]
        public static ObjectHandle CreateInstance(string assemblyName, string typeName, object[] activationAttributes)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return CreateInstanceInternal(assemblyName,
                                          typeName,
                                          false,
                                          Activator.ConstructorDefault,
                                          null,
                                          null,
                                          null,
                                          activationAttributes,
                                          ref stackMark);
        }

        private static ObjectHandle CreateInstanceInternal(string assemblyString,
                                                           string typeName,
                                                           bool ignoreCase,
                                                           BindingFlags bindingAttr,
                                                           Binder binder,
                                                           object[] args,
                                                           CultureInfo culture,
                                                           object[] activationAttributes,
                                                           ref StackCrawlMark stackMark)
        {
            Type type = null;
            Assembly assembly = null;
            if (assemblyString == null)
            {
                assembly = Assembly.GetExecutingAssembly(ref stackMark);
            }
            else
            {
                RuntimeAssembly assemblyFromResolveEvent;
                AssemblyName assemblyName = RuntimeAssembly.CreateAssemblyName(assemblyString, out assemblyFromResolveEvent);
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
                        assemblyName, null, ref stackMark,
                        true /*thrownOnFileNotFound*/);
                }
            }

            if (type == null)
            {                
                type = assembly.GetType(typeName, true /*throwOnError*/, ignoreCase);
            }

            object o = Activator.CreateInstance(type,
                                                bindingAttr,
                                                binder,
                                                args,
                                                culture,
                                                activationAttributes);

            return (o != null) ? new ObjectHandle(o) : null;          
        }

        public static ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName)
        {
            return CreateInstanceFrom(assemblyFile, typeName, null);
        }

        public static ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            return CreateInstanceFromInternal(assemblyFile,
                                              typeName,
                                              ignoreCase,
                                              bindingAttr,
                                              binder,
                                              args,
                                              culture,
                                              activationAttributes);
        }

        public static ObjectHandle CreateInstanceFrom(string assemblyFile, string typeName, object[] activationAttributes)
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

        private static ObjectHandle CreateInstanceFromInternal(string assemblyFile,
                                                               string typeName,
                                                               bool ignoreCase,
                                                               BindingFlags bindingAttr,
                                                               Binder binder,
                                                               object[] args,
                                                               CultureInfo culture,
                                                               object[] activationAttributes)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyFile);
            Type t = assembly.GetType(typeName, true, ignoreCase);

            object o = Activator.CreateInstance(t,
                                                bindingAttr,
                                                binder,
                                                args,
                                                culture,
                                                activationAttributes);

            return (o != null) ? new ObjectHandle(o) : null;
        }

        public static T CreateInstance<T>()
        {
            var rt = (RuntimeType)typeof(T);

            // This is a workaround to maintain compatibility with V2. Without this we would throw a NotSupportedException for void[].
            // Array, Ref, and Pointer types don't have default constructors.
            if (rt.HasElementType)
                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, rt));

            // Skip the CreateInstanceCheckThis call to avoid perf cost and to maintain compatibility with V2 (throwing the same exceptions).
            return (T)rt.CreateInstanceDefaultCtor(publicOnly: true, skipCheckThis: true, fillCache: true, wrapExceptions: true);
        }
    }
}
