// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILTrim
{
    internal static class TypeSystemExtensions
    {
        public static MethodDesc TryGetMethod(this EcmaModule module, EntityHandle handle)
        {
            return module.GetObject(handle, NotFoundBehavior.ReturnNull) as MethodDesc;
        }
    }
}
