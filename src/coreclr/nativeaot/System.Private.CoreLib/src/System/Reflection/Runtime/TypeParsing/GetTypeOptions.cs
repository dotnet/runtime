// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Diagnostics;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.Assemblies;

namespace System.Reflection.Runtime.TypeParsing
{
    /// <summary>
    /// Return the assembly matching the refName if one exists. If a matching assembly doesn't exist, return null. Throw for all other errors.
    /// </summary>
    internal delegate Assembly CoreAssemblyResolver(RuntimeAssemblyName refName);

    /// <summary>
    /// Look for a type matching the name inside the provided assembly. If "containingAssemblyIfAny" is null, look in a set of default assemblies. For example, if
    /// this resolver is for the Type.GetType() api, the default assemblies are the assembly that invoked Type.GetType() and mscorlib in that order.
    /// If this resolver is for Assembly.GetType(), the default is that assembly. Third-party resolvers can do whatever they want. If no type exists for that name,
    /// return null. Throw for all other errors. The name will be for a top-level named type only. No nested types. No constructed types.
    /// </summary>
    ///
    /// <remarks>
    /// This delegate "should" take an "ignoreCase" parameter too, but pragmatically, every resolver we create is a closure for other reasons so
    /// it's more convenient to let "ignoreCase" be just another variable that's captured in that closure.
    /// </remarks>
    internal delegate Type CoreTypeResolver(Assembly containingAssemblyIfAny, string name);

    //
    // Captures the various options passed to the Type.GetType() family of apis.
    //
    internal sealed class GetTypeOptions
    {
        public GetTypeOptions(CoreAssemblyResolver coreAssemblyResolver, CoreTypeResolver coreTypeResolver, bool throwOnError, bool ignoreCase)
        {
            Debug.Assert(coreAssemblyResolver != null);
            Debug.Assert(coreTypeResolver != null);

            _coreAssemblyResolver = coreAssemblyResolver;
            _coreTypeResolver = coreTypeResolver;
            ThrowOnError = throwOnError;
            IgnoreCase = ignoreCase;
        }

        public Assembly CoreResolveAssembly(RuntimeAssemblyName name)
        {
            Assembly assembly = _coreAssemblyResolver(name);
            if (assembly == null && ThrowOnError)
                throw new FileNotFoundException(SR.Format(SR.FileNotFound_AssemblyNotFound, name.FullName));
            return assembly;
        }

        public Type CoreResolveType(Assembly containingAssemblyIfAny, string name)
        {
            Type type = _coreTypeResolver(containingAssemblyIfAny, name);
            if (type == null && ThrowOnError)
                throw Helpers.CreateTypeLoadException(name.EscapeTypeNameIdentifier(), containingAssemblyIfAny);
            return type;
        }

        public bool ThrowOnError { get; }
        public bool IgnoreCase { get; }

        private readonly CoreAssemblyResolver _coreAssemblyResolver;
        private readonly CoreTypeResolver _coreTypeResolver;
    }
}
