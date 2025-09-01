// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Declare an assembly that should be inspected during type map building.
    /// </summary>
    /// <typeparam name="TTypeMapGroup">Type map group</typeparam>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class TypeMapAssemblyTargetAttribute<TTypeMapGroup> : Attribute
    {
        /// <summary>
        /// Provide the assembly to look for type mapping attributes.
        /// </summary>
        /// <param name="assemblyName">Assembly to reference</param>
        public TypeMapAssemblyTargetAttribute(string assemblyName) { }
    }
}
