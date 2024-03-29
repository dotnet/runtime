// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // When applied to a type this custom attribute will cause any static class constructor to be run eagerly
    // at module load time rather than deferred till just before the class is used.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal class EagerStaticClassConstructionAttribute : Attribute
    {
    }
}
