// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Runtime;

using Internal.Metadata.NativeFormat;
using Internal.NativeFormat;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;
using Internal.Runtime.TypeLoader;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.TypeLoader
{
    public sealed partial class TypeLoaderEnvironment
    {
        public static bool IsStaticMethodSignature(MethodNameAndSignature signature)
        {
            var method = signature.Handle.GetMethod(signature.Reader);
            return (method.Flags & MethodAttributes.Static) != 0;
        }

        public uint GetGenericArgumentCountFromMethodNameAndSignature(MethodNameAndSignature signature)
        {
            var metadataReader = signature.Reader;
            var method = signature.Handle.GetMethod(metadataReader);
            var methodSignature = method.Signature.GetMethodSignature(metadataReader);
            return checked((uint)methodSignature.GenericParameterCount);
        }
    }
}
