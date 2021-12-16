// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // When applied to a type this custom attribute will force use of statically precompiled dictionary looks that
    // do not depend on lazy resolution by the template type loader
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class ForceDictionaryLookupsAttribute : Attribute
    {
    }
}
