// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal static class MetadataPolyfills
    {
        extension(GenericParameterAttributes)
        {
            public static GenericParameterAttributes AllowByRefLike => (GenericParameterAttributes)0x0020;
        }

        extension(MethodImplAttributes)
        {
            public static MethodImplAttributes AggressiveOptimization => (MethodImplAttributes)0x0200;
            public static MethodImplAttributes Async => (MethodImplAttributes)0x2000;
        }

        extension(TypeAttributes)
        {
            public static TypeAttributes ExtendedLayout => (TypeAttributes)0x00000018;
        }
    }
}

namespace System.Runtime.InteropServices
{
    internal static class LayoutKindPolyfills
    {
        extension(LayoutKind)
        {
            public static LayoutKind Extended => (LayoutKind)1;
        }
    }
}
