// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.SymbolStore;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security;
using System.Threading;

namespace System.Reflection.Emit
{
    public sealed partial class AssemblyBuilder : Assembly
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern RuntimeModule GetInMemoryAssemblyModule(RuntimeAssembly assembly);

        #region Internal Data Members

        internal readonly AssemblyBuilderAccess _access;
        private readonly RuntimeAssembly _internalAssembly;
        private readonly ModuleBuilder _manifestModuleBuilder;
        // Set to true if the manifest module was returned by code:DefineDynamicModule to the user
        private bool _isManifestModuleUsedAsDefinedModule;

        private const int AssemblyDefToken = 0x20000001;

        internal object SyncRoot => InternalAssembly.SyncRoot;

        internal RuntimeAssembly InternalAssembly => _internalAssembly;

        #endregion

        #region Constructor

        internal AssemblyBuilder(AssemblyName name!!,
                                 AssemblyBuilderAccess access,
                                 Assembly? callingAssembly,
                                 AssemblyLoadContext? assemblyLoadContext,
                                 IEnumerable<CustomAttributeBuilder>? unsafeAssemblyAttributes)
        {
            if (access != AssemblyBuilderAccess.Run && access != AssemblyBuilderAccess.RunAndCollect)
            {
                throw new ArgumentException(SR.Format(SR.Arg_EnumIllegalVal, (int)access), nameof(access));
            }
            if (callingAssembly == null)
            {
                // Called either from interop or async delegate invocation. Rejecting because we don't
                // know how to set the correct context of the new dynamic assembly.
                throw new InvalidOperationException();
            }
            if (assemblyLoadContext == null)
            {
                assemblyLoadContext = AssemblyLoadContext.GetLoadContext(callingAssembly);
            }

            // Clone the name in case the caller modifies it underneath us.
            name = (AssemblyName)name.Clone();

            // Scan the assembly level attributes for any attributes which modify how we create the
            // assembly. Currently, we look for any attribute which modifies the security transparency
            // of the assembly.
            List<CustomAttributeBuilder>? assemblyAttributes = null;
            if (unsafeAssemblyAttributes != null)
            {
                // Create a copy to ensure that it cannot be modified from another thread
                // as it is used further below.
                assemblyAttributes = new List<CustomAttributeBuilder>(unsafeAssemblyAttributes);
            }

            RuntimeAssembly? retAssembly = null;
            CreateDynamicAssembly(ObjectHandleOnStack.Create(ref name),
                                  (int)access,
                                  ObjectHandleOnStack.Create(ref assemblyLoadContext),
                                  ObjectHandleOnStack.Create(ref retAssembly));
            _internalAssembly = retAssembly!;

            _access = access;

            // Make sure that ManifestModule is properly initialized
            // We need to do this before setting any CustomAttribute
            RuntimeModule internalModule = (RuntimeModule)GetInMemoryAssemblyModule(InternalAssembly);

            // Note that this ModuleBuilder cannot be used for RefEmit yet
            // because it hasn't been initialized.
            // However, it can be used to set the custom attribute on the Assembly
            _manifestModuleBuilder = new ModuleBuilder(this, internalModule);

            if (assemblyAttributes != null)
            {
                foreach (CustomAttributeBuilder assemblyAttribute in assemblyAttributes)
                {
                    SetCustomAttribute(assemblyAttribute);
                }
            }
        }

        #endregion

        #region DefineDynamicAssembly

        [RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
        [DynamicSecurityMethod] // Required to make Assembly.GetCallingAssembly reliable.
        public static AssemblyBuilder DefineDynamicAssembly(AssemblyName name, AssemblyBuilderAccess access)
        {
            return InternalDefineDynamicAssembly(name,
                                                 access,
                                                 Assembly.GetCallingAssembly(),
                                                 AssemblyLoadContext.CurrentContextualReflectionContext,
                                                 null);
        }

        [RequiresDynamicCode("Defining a dynamic assembly requires dynamic code.")]
        [DynamicSecurityMethod] // Required to make Assembly.GetCallingAssembly reliable.
        public static AssemblyBuilder DefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            IEnumerable<CustomAttributeBuilder>? assemblyAttributes)
        {
            return InternalDefineDynamicAssembly(name,
                                                 access,
                                                 Assembly.GetCallingAssembly(),
                                                 AssemblyLoadContext.CurrentContextualReflectionContext,
                                                 assemblyAttributes);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AppDomain_CreateDynamicAssembly")]
        private static partial void CreateDynamicAssembly(ObjectHandleOnStack name,
                                                         int access,
                                                         ObjectHandleOnStack assemblyLoadContext,
                                                         ObjectHandleOnStack retAssembly);

        private static readonly object s_assemblyBuilderLock = new object();

        internal static AssemblyBuilder InternalDefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            Assembly? callingAssembly,
            AssemblyLoadContext? assemblyLoadContext,
            IEnumerable<CustomAttributeBuilder>? unsafeAssemblyAttributes)
        {
            lock (s_assemblyBuilderLock)
            {
                // We can only create dynamic assemblies in the current domain
                return new AssemblyBuilder(name,
                                           access,
                                           callingAssembly,
                                           assemblyLoadContext,
                                           unsafeAssemblyAttributes);
            }
        }
        #endregion

        #region DefineDynamicModule

        /// <summary>
        /// Defines a named dynamic module. It is an error to define multiple
        /// modules within an Assembly with the same name. This dynamic module is
        /// a transient module.
        /// </summary>
        public ModuleBuilder DefineDynamicModule(string name)
        {
            lock (SyncRoot)
            {
                return DefineDynamicModuleInternalNoLock(name);
            }
        }

        private ModuleBuilder DefineDynamicModuleInternalNoLock(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            if (name[0] == '\0')
            {
                throw new ArgumentException(SR.Argument_InvalidName, nameof(name));
            }

            // Create the dynamic module- only one ModuleBuilder per AssemblyBuilder can be created.
            if (_isManifestModuleUsedAsDefinedModule)
            {
                throw new InvalidOperationException(SR.InvalidOperation_NoMultiModuleAssembly);
            }

            // We are reusing manifest module as user-defined dynamic module
            _isManifestModuleUsedAsDefinedModule = true;

            return _manifestModuleBuilder;
        }

        #endregion

        /// <summary>
        /// Helper to ensure the type name is unique underneath assemblyBuilder.
        /// </summary>
        internal void CheckTypeNameConflict(string strTypeName, TypeBuilder? enclosingType)
        {
            _manifestModuleBuilder.CheckTypeNameConflict(strTypeName, enclosingType);
        }

        internal static void CheckContext(params Type[]?[]? typess)
        {
            if (typess == null)
            {
                return;
            }

            foreach (Type[]? types in typess)
            {
                if (types != null)
                {
                    CheckContext(types);
                }
            }
        }

        internal static void CheckContext(params Type?[]? types)
        {
            if (types == null)
            {
                return;
            }

            foreach (Type? type in types)
            {
                if (type == null)
                {
                    continue;
                }

                if (type.Module == null || type.Module.Assembly == null)
                {
                    throw new ArgumentException(SR.Argument_TypeNotValid);
                }

                if (type.Module.Assembly == typeof(object).Module.Assembly)
                {
                    continue;
                }
            }
        }

        public override bool Equals(object? obj) => base.Equals(obj);

        public override int GetHashCode() => base.GetHashCode();

        #region ICustomAttributeProvider Members
        public override object[] GetCustomAttributes(bool inherit) =>
            InternalAssembly.GetCustomAttributes(inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) =>
            InternalAssembly.GetCustomAttributes(attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) =>
            InternalAssembly.IsDefined(attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData() =>
            InternalAssembly.GetCustomAttributesData();

        #endregion

        #region Assembly overrides

        public override AssemblyName GetName(bool copiedName) => InternalAssembly.GetName(copiedName);

        public override string? FullName => InternalAssembly.FullName;

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type? GetType(string name, bool throwOnError, bool ignoreCase) =>
            InternalAssembly.GetType(name, throwOnError, ignoreCase);

        public override Module ManifestModule => _manifestModuleBuilder.InternalModule;

        public override bool ReflectionOnly => InternalAssembly.ReflectionOnly;

        public override Module? GetModule(string name) => InternalAssembly.GetModule(name);

        [RequiresUnreferencedCode("Assembly references might be removed")]
        public override AssemblyName[] GetReferencedAssemblies() =>
            InternalAssembly.GetReferencedAssemblies();

        public override long HostContext => InternalAssembly.HostContext;

        public override Module[] GetModules(bool getResourceModules) =>
            InternalAssembly.GetModules(getResourceModules);

        public override Module[] GetLoadedModules(bool getResourceModules) =>
            InternalAssembly.GetLoadedModules(getResourceModules);

        public override Assembly GetSatelliteAssembly(CultureInfo culture) =>
            InternalAssembly.GetSatelliteAssembly(culture, null);

        /// <sumary>
        /// Useful for binding to a very specific version of a satellite assembly
        /// </sumary>
        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version) =>
            InternalAssembly.GetSatelliteAssembly(culture, version);

        public override bool IsCollectible => InternalAssembly.IsCollectible;

        #endregion

        /// <param name="name">The name of module for the look up.</param>
        /// <returns>Dynamic module with the specified name.</returns>
        public ModuleBuilder? GetDynamicModule(string name)
        {
            lock (SyncRoot)
            {
                return GetDynamicModuleNoLock(name);
            }
        }

        /// <param name="name">The name of module for the look up.</param>
        private ModuleBuilder? GetDynamicModuleNoLock(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            if (_isManifestModuleUsedAsDefinedModule)
            {
                if (ModuleBuilder.ManifestModuleName == name)
                {
                    return _manifestModuleBuilder;
                }
            }
            return null;
        }

        /// <summary>
        /// Use this function if client decides to form the custom attribute blob themselves.
        /// </summary>
        public void SetCustomAttribute(ConstructorInfo con!!, byte[] binaryAttribute!!)
        {
            lock (SyncRoot)
            {
                TypeBuilder.DefineCustomAttribute(
                    _manifestModuleBuilder,     // pass in the in-memory assembly module
                    AssemblyDefToken,
                    _manifestModuleBuilder.GetConstructorToken(con),
                    binaryAttribute);
            }
        }

        /// <summary>
        /// Use this function if client wishes to build CustomAttribute using CustomAttributeBuilder.
        /// </summary>
        public void SetCustomAttribute(CustomAttributeBuilder customBuilder!!)
        {
            lock (SyncRoot)
            {
                customBuilder.CreateCustomAttribute(_manifestModuleBuilder, AssemblyDefToken);
            }
        }
    }
}
