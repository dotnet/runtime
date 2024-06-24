// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;

using Internal.NativeFormat;

namespace Internal.TypeSystem
{
    public abstract partial class TypeSystemContext : IModuleResolver
    {
        public ModuleDesc SystemModule
        {
            get;
            private set;
        }

        protected void InitializeSystemModule(ModuleDesc systemModule)
        {
            Debug.Assert(SystemModule == null);
            SystemModule = systemModule;
        }

        public virtual ModuleDesc ResolveAssembly(AssemblyNameInfo name, bool throwIfNotFound = true)
        {
            if (throwIfNotFound)
                throw new NotSupportedException();
            return null;
        }

        internal virtual ModuleDesc ResolveModule(IAssemblyDesc referencingModule, string fileName, bool throwIfNotFound = true)
        {
            if (throwIfNotFound)
                throw new NotSupportedException();
            return null;
        }

        ModuleDesc IModuleResolver.ResolveModule(IAssemblyDesc referencingModule, string fileName, bool throwIfNotFound)
        {
            return ResolveModule(referencingModule, fileName, throwIfNotFound);
        }
    }
}
