// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.Modules;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.TypeParsing;
using System.Reflection.Runtime.CustomAttributes;
using System.Collections.Generic;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;
using Internal.Reflection.Core.NonPortable;

using System.Security;

namespace System.Reflection.Runtime.Assemblies
{
    //
    // The runtime's implementation of an Assembly.
    //
    internal abstract partial class RuntimeAssemblyInfo : RuntimeAssembly, IEquatable<RuntimeAssemblyInfo>
    {
        public bool Equals(RuntimeAssemblyInfo? other)
        {
            if (other == null)
                return false;

            return this.Equals((object)other);
        }

        public sealed override string FullName
        {
            get
            {
                return GetName().FullName;
            }
        }

        public sealed override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public abstract override Module ManifestModule { get; }

        public sealed override IEnumerable<Module> Modules
        {
            get
            {
                yield return ManifestModule;
            }
        }

        public sealed override Module GetModule(string name)
        {
            if (name == ManifestModule.ScopeName)
                return ManifestModule;

            return null;
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public sealed override Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            if (name == null)
                throw new ArgumentNullException();
            if (name.Length == 0)
                throw new ArgumentException();

            TypeName typeName = TypeParser.ParseAssemblyQualifiedTypeName(name, throwOnError: throwOnError);
            if (typeName == null)
                return null;
            if (typeName is AssemblyQualifiedTypeName)
            {
                if (throwOnError)
                    throw new ArgumentException(SR.Argument_AssemblyGetTypeCannotSpecifyAssembly);  // Cannot specify an assembly qualifier in a typename passed to Assembly.GetType()
                else
                    return null;
            }

            CoreAssemblyResolver coreAssemblyResolver = GetRuntimeAssemblyIfExists;
            CoreTypeResolver coreTypeResolver =
                delegate (Assembly containingAssemblyIfAny, string coreTypeName)
                {
                    if (containingAssemblyIfAny == null)
                        return GetTypeCore(coreTypeName, ignoreCase: ignoreCase);
                    else
                        return containingAssemblyIfAny.GetTypeCore(coreTypeName, ignoreCase: ignoreCase);
                };
            GetTypeOptions getTypeOptions = new GetTypeOptions(coreAssemblyResolver, coreTypeResolver, throwOnError: throwOnError, ignoreCase: ignoreCase);

            return typeName.ResolveType(this, getTypeOptions);
        }

#pragma warning disable 0067  // Silence warning about ModuleResolve not being used.
        public sealed override event ModuleResolveEventHandler? ModuleResolve;
#pragma warning restore 0067

        public sealed override bool ReflectionOnly => false; // ReflectionOnly loading not supported.

        public sealed override bool IsCollectible => false; // Unloading not supported.

        internal abstract RuntimeAssemblyName RuntimeAssemblyName { get; }

        public sealed override AssemblyName GetName()
        {
            return RuntimeAssemblyName.ToAssemblyName();
        }

        [RequiresUnreferencedCode("Types might be removed")]
        public sealed override Type[] GetForwardedTypes()
        {
            List<Type> types = new List<Type>();
            List<Exception>? exceptions = null;

            foreach (TypeForwardInfo typeForwardInfo in TypeForwardInfos)
            {
                string fullTypeName = typeForwardInfo.NamespaceName.Length == 0 ? typeForwardInfo.TypeName : typeForwardInfo.NamespaceName + "." + typeForwardInfo.TypeName;
                RuntimeAssemblyName redirectedAssemblyName = typeForwardInfo.RedirectedAssemblyName;

                Type? type = null;
                RuntimeAssemblyInfo redirectedAssembly;
                Exception exception = TryGetRuntimeAssembly(redirectedAssemblyName, out redirectedAssembly);
                if (exception == null)
                {
                    type = redirectedAssembly.GetTypeCore(fullTypeName, ignoreCase: false); // GetTypeCore() will follow any further type-forwards if needed.
                    if (type == null)
                        exception = Helpers.CreateTypeLoadException(fullTypeName.EscapeTypeNameIdentifier(), redirectedAssembly);
                }

                Debug.Assert((type != null) != (exception != null)); // Exactly one of these must be non-null.

                if (type != null)
                {
                    types.Add(type);
                    AddPublicNestedTypes(type, types);
                }
                else
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(exception);
                }
            }

            if (exceptions != null)
            {
                int numTypes = types.Count;
                int numExceptions = exceptions.Count;
                types.AddRange(new Type[numExceptions]); // add one null Type for each exception.
                exceptions.InsertRange(0, new Exception[numTypes]); // align the Exceptions with the null Types.
                throw new ReflectionTypeLoadException(types.ToArray(), exceptions.ToArray());
            }

            return types.ToArray();
        }

        /// <summary>
        /// Intentionally excludes forwards to nested types.
        /// </summary>
        protected abstract IEnumerable<TypeForwardInfo> TypeForwardInfos { get; }

        [RequiresUnreferencedCode("Types might be removed")]
        private static void AddPublicNestedTypes(Type type, List<Type> types)
        {
            foreach (Type nestedType in type.GetNestedTypes(BindingFlags.Public))
            {
                types.Add(nestedType);
                AddPublicNestedTypes(nestedType, types);
            }
        }

        /// <summary>
        /// Helper routine for the more general Type.GetType() family of apis.
        ///
        /// Resolves top-level named types only. No nested types. No constructed types.
        ///
        /// Returns null if the type does not exist. Throws for all other error cases.
        /// </summary>
        internal RuntimeTypeInfo GetTypeCore(string fullName, bool ignoreCase)
        {
            if (ignoreCase)
                return GetTypeCoreCaseInsensitive(fullName);
            else
                return GetTypeCoreCaseSensitive(fullName);
        }

        // Types that derive from RuntimeAssembly must implement the following public surface area members
        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }
        public abstract override IEnumerable<TypeInfo> DefinedTypes
        {
            [RequiresUnreferencedCode("Types might be removed")]
            get;
        }
        public abstract override MethodInfo EntryPoint { get; }
        public abstract override IEnumerable<Type> ExportedTypes
        {
            [RequiresUnreferencedCode("Types might be removed")]
            get;
        }
        public abstract override ManifestResourceInfo GetManifestResourceInfo(string resourceName);
        public abstract override string[] GetManifestResourceNames();
        public abstract override Stream GetManifestResourceStream(string name);
        public abstract override string ImageRuntimeVersion { get; }
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();

        /// <summary>
        /// Ensures a module is loaded and that its module constructor is executed. If the module is fully
        /// loaded and its constructor already ran, we do not run it again.
        /// </summary>
        internal abstract void RunModuleConstructor();

        /// <summary>
        /// Perform a lookup for a type based on a name. Overriders are expected to
        /// have a non-cached implementation, as the result is expected to be cached by
        /// callers of this method. Should be implemented by every format specific
        /// RuntimeAssembly implementor
        /// </summary>
        internal abstract RuntimeTypeInfo UncachedGetTypeCoreCaseSensitive(string fullName);


        /// <summary>
        /// Perform a lookup for a type based on a name. Overriders may or may not
        /// have a cached implementation, as the result is not expected to be cached by
        /// callers of this method, but it is also a rarely used api. Should be
        /// implemented by every format specific RuntimeAssembly implementor
        /// </summary>
        internal abstract RuntimeTypeInfo GetTypeCoreCaseInsensitive(string fullName);

        internal RuntimeTypeInfo GetTypeCoreCaseSensitive(string fullName)
        {
            return this.CaseSensitiveTypeTable.GetOrAdd(fullName);
        }

        private CaseSensitiveTypeCache CaseSensitiveTypeTable
        {
            get
            {
                return _lazyCaseSensitiveTypeTable ??= new CaseSensitiveTypeCache(this);
            }
        }

#pragma warning disable 0672  // GlobalAssemblyCache is Obsolete.
        public sealed override bool GlobalAssemblyCache
        {
            get
            {
                return false;
            }
        }
#pragma warning restore 0672

        public sealed override long HostContext
        {
            get
            {
                return 0;
            }
        }

        [RequiresUnreferencedCode("Types and members the loaded module depends on might be removed")]
        public sealed override Module LoadModule(string moduleName, byte[] rawModule, byte[] rawSymbolStore)
        {
            throw new PlatformNotSupportedException();
        }

        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public sealed override FileStream GetFile(string name)
        {
            throw new FileNotFoundException();
        }

        [RequiresAssemblyFiles(ThrowingMessageInRAF)]
        public sealed override FileStream[] GetFiles(bool getResourceModules)
        {
            throw new FileNotFoundException();
        }

        public sealed override SecurityRuleSet SecurityRuleSet
        {
            get
            {
                return SecurityRuleSet.None;
            }
        }

        /// <summary>
        /// Returns a *freshly allocated* array of loaded Assemblies.
        /// </summary>
        internal static Assembly[] GetLoadedAssemblies()
        {
            // Important: The result of this method is the return value of the AppDomain.GetAssemblies() api so
            // so it must return a freshly allocated array on each call.

            AssemblyBinder binder = ReflectionCoreExecution.ExecutionDomain.ReflectionDomainSetup.AssemblyBinder;
            IList<AssemblyBindResult> bindResults = binder.GetLoadedAssemblies();
            Assembly[] results = new Assembly[bindResults.Count];
            for (int i = 0; i < bindResults.Count; i++)
            {
                Assembly assembly = GetRuntimeAssembly(bindResults[i]);
                results[i] = assembly;
            }
            return results;
        }

        private volatile CaseSensitiveTypeCache _lazyCaseSensitiveTypeTable;

        private sealed class CaseSensitiveTypeCache : ConcurrentUnifier<string, RuntimeTypeInfo>
        {
            public CaseSensitiveTypeCache(RuntimeAssemblyInfo runtimeAssembly)
            {
                _runtimeAssembly = runtimeAssembly;
            }

            protected sealed override RuntimeTypeInfo Factory(string key)
            {
                return _runtimeAssembly.UncachedGetTypeCoreCaseSensitive(key);
            }

            private readonly RuntimeAssemblyInfo _runtimeAssembly;
        }
    }
}
