// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>Indicates the language version of the ref safety rules used when the module was compiled.</summary>
    [AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
    public sealed class RefSafetyRulesAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="RefSafetyRulesAttribute"/> class.</summary>
        /// <param name="version">The language version of the ref safety rules used when the module was compiled.</param>
        public RefSafetyRulesAttribute(int version) => Version = version;

        /// <summary>Gets the language version of the ref safety rules used when the module was compiled.</summary>
        public int Version { get; }
    }
}
