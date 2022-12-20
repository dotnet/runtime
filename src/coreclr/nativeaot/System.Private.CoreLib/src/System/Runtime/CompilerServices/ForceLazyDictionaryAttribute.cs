// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    //
    // When applied to a generic type or a method, this custom attribute forces use of lazy dictionaries. This allows static compilation
    // to succeed in the presence of constructs that trigger infinite generic expansion.
    //
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ForceLazyDictionaryAttribute : Attribute
    {
        public ForceLazyDictionaryAttribute() { }
    }
}
