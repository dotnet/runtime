// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Internal.TypeSystem
{
    public abstract partial class ModuleDesc : TypeSystemEntity
    {
        public override TypeSystemContext Context
        {
            get;
        }

        /// <summary>
        /// Gets the assembly this module is part of (the assembly manifest module).
        /// </summary>
        public virtual IAssemblyDesc Assembly
        {
            get;
        }

        public ModuleDesc(TypeSystemContext context, IAssemblyDesc assembly)
        {
            Context = context;
            Assembly = assembly;
        }

        /// <summary>
        /// Gets a type in this module or null.
        /// </summary>
        public MetadataType GetType(string nameSpace, string name, bool throwIfNotFound = true)
        {
            return (MetadataType)GetType(nameSpace, name, throwIfNotFound ? NotFoundBehavior.Throw : NotFoundBehavior.ReturnNull);
        }

        /// <summary>
        /// Gets a type in this module with the specified name, a resolution failure object, or null.
        /// </summary>
        public abstract object GetType(string nameSpace, string name, NotFoundBehavior notFoundBehavior);

        /// <summary>
        /// Gets the global &lt;Module&gt; type.
        /// </summary>
        public abstract MetadataType GetGlobalModuleType();

        /// <summary>
        /// Retrieves a collection of all types defined in the current module. This includes nested types.
        /// </summary>
        public abstract IEnumerable<MetadataType> GetAllTypes();
    }
}
