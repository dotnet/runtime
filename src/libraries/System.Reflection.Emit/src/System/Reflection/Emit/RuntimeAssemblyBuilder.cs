// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

namespace System.Reflection.Emit
{
    public sealed class RuntimeAssemblyBuilder : AssemblyBuilder
    {
        private RuntimeAssemblyBuilder(
            AssemblyName name,
            AssemblyBuilderAccess access,
            IEnumerable<CustomAttributeBuilder>? assemblyAttributes,
            AssemblyLoadContext? asssemblyLoadContext)
        {
#if false
            // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.metadata.ecma335.metadatabuilder

            var peHeaderBuilder = new PEHeaderBuilder(
                imageCharacteristics: Characteristics.ExecutableImage
            );

            peHeaderBuilder.ToString();
#endif
        }

        public static AssemblyBuilder DefineDynamicAssembly(
            AssemblyName name,
            AssemblyBuilderAccess access,
            IEnumerable<CustomAttributeBuilder>? assemblyAttributes,
            Assembly? callingAssembly)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
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

            AssemblyLoadContext? assemblyLoadContext =
                AssemblyLoadContext.CurrentContextualReflectionContext ?? AssemblyLoadContext.GetLoadContext(callingAssembly);

            if (assemblyLoadContext == null)
            {
                throw new InvalidOperationException();
            }

            return new RuntimeAssemblyBuilder(name, access, assemblyAttributes, assemblyLoadContext);
        }

        internal Assembly InternalAssembly
        {
            get
            {
                throw new NotImplementedException(); // TODO
            }
        }

        #region Assembly overrides
        public override object[] GetCustomAttributes(bool inherit)
            => InternalAssembly.GetCustomAttributes(inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            => InternalAssembly.GetCustomAttributes(attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit)
            => InternalAssembly.IsDefined(attributeType, inherit);

        public override IList<CustomAttributeData> GetCustomAttributesData()
            => InternalAssembly.GetCustomAttributesData();

        public override AssemblyName GetName(bool copiedName)
            => InternalAssembly.GetName(copiedName);

        public override string? FullName
            => InternalAssembly.FullName;

        [RequiresUnreferencedCode("Types might be removed")]
        public override Type? GetType(string name, bool throwOnError, bool ignoreCase)
            => InternalAssembly.GetType(name, throwOnError, ignoreCase);

        public override bool ReflectionOnly
            => InternalAssembly.ReflectionOnly;

        public override Module? GetModule(string name)
            => InternalAssembly.GetModule(name);

        [RequiresUnreferencedCode("Assembly references might be removed")]
        public override AssemblyName[] GetReferencedAssemblies()
            => InternalAssembly.GetReferencedAssemblies();

        public override long HostContext
            => InternalAssembly.HostContext;

        public override Module[] GetModules(bool getResourceModules)
            => InternalAssembly.GetModules(getResourceModules);

        public override Module[] GetLoadedModules(bool getResourceModules)
            => InternalAssembly.GetLoadedModules(getResourceModules);

        public override Assembly GetSatelliteAssembly(CultureInfo culture)
            => InternalAssembly.GetSatelliteAssembly(culture, null);

        public override Assembly GetSatelliteAssembly(CultureInfo culture, Version? version)
            => InternalAssembly.GetSatelliteAssembly(culture, version);

        public override bool IsCollectible
            => InternalAssembly.IsCollectible;
        #endregion

        public override Module ManifestModule
            => throw new NotImplementedException(); // TODO _manifestModuleBuilder.InternalModule;

        public override ModuleBuilder DefineDynamicModule(string name)
        {
            throw new NotImplementedException(); // TODO
        }

        public override ModuleBuilder? GetDynamicModule(string name)
        {
            throw new NotImplementedException(); // TODO
        }

        public override void SetCustomAttribute(ConstructorInfo con, byte[] binaryAttribute)
        {
            if (con == null)
            {
                throw new ArgumentNullException(nameof(con));
            }
            if (binaryAttribute == null)
            {
                throw new ArgumentNullException(nameof(binaryAttribute));
            }

            throw new NotImplementedException(); // TODO
        }
        public override void SetCustomAttribute(CustomAttributeBuilder customBuilder)
        {
            if (customBuilder == null)
            {
                throw new ArgumentNullException(nameof(customBuilder));
            }

            throw new NotImplementedException(); // TODO
        }
    }
}
