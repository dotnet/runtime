// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Reflection;

/// <summary>Provides downlevel polyfills for reflection methods and metadata enum values.</summary>
internal static class ReflectionPolyfills
{
    extension(MethodInfo method)
    {
        public T CreateDelegate<T>() where T : Delegate =>
            (T)method.CreateDelegate(typeof(T));

        public T CreateDelegate<T>(object? target) where T : Delegate =>
            (T)method.CreateDelegate(typeof(T), target);
    }

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

    extension(LayoutKind)
    {
        public static LayoutKind Extended => (LayoutKind)1;
    }
}
