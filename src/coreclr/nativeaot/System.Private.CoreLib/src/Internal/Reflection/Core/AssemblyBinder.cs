// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using Internal.Metadata.NativeFormat;
using System.Reflection.Runtime.General;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Reflection.Core
{
    // Auto StructLayout used to suppress warning that order of fields is not guaranteed in partial structs
    [StructLayout(LayoutKind.Auto)]
    [ReflectionBlocked]
    [CLSCompliant(false)]
    public partial struct AssemblyBindResult
    {
        public MetadataReader Reader;
        public ScopeDefinitionHandle ScopeDefinitionHandle;
        public IEnumerable<QScopeDefinition> OverflowScopes;
    }

    //
    // Implements the assembly binding policy Reflection domain. This gets called any time the domain needs
    // to resolve an assembly name.
    //
    // If the binder cannot locate an assembly, it must return null and set "exception" to an exception object.
    //
    [ReflectionBlocked]
    [CLSCompliant(false)]
    public abstract class AssemblyBinder
    {
        public const string DefaultAssemblyNameForGetType = "System.Private.CoreLib";

        public abstract bool Bind(RuntimeAssemblyName refName, bool cacheMissedLookups, out AssemblyBindResult result, out Exception exception);

        public abstract bool Bind(byte[] rawAssembly, byte[] rawSymbolStore, out AssemblyBindResult result, out Exception exception);

        public abstract bool Bind(string assemblyPath, out AssemblyBindResult bindResult, out Exception exception);

        public abstract IList<AssemblyBindResult> GetLoadedAssemblies();
    }
}
