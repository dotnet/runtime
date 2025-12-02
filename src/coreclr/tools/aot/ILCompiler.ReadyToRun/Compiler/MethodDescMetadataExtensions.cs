// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

#nullable enable

namespace ILCompiler
{
    internal static class MethodDescMetadataExtensions
    {
        public static EcmaMethod? GetMetadataMethodDefinition(this MethodDesc method)
        {
            return method.GetTypicalMethodDefinition() switch
            {
                EcmaMethod em => em,
                AsyncMethodVariant amv => amv.GetAsyncVariantDefinition(),
                UnboxingMethodDesc umd => (EcmaMethod)umd.GetUnboxedMethod(),
                _ => null,
            };
        }
    }
}

#nullable restore
