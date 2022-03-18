// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsageAttribute(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event, Inherited = false, AllowMultiple = false)]
    public sealed class ExcludeFromCodeCoverageAttribute : Attribute
    {
        public ExcludeFromCodeCoverageAttribute() { }

        /// <summary>Gets or sets the justification for excluding the member from code coverage.</summary>
        public string? Justification { get; set; }
    }
}
