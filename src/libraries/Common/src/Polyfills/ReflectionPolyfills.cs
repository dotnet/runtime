// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection;

/// <summary>Provides downlevel polyfills for instance methods on <see cref="MethodInfo"/>.</summary>
internal static class ReflectionPolyfills
{
    extension(MethodInfo method)
    {
        public T CreateDelegate<T>() where T : Delegate =>
            (T)method.CreateDelegate(typeof(T));

        public T CreateDelegate<T>(object? target) where T : Delegate =>
            (T)method.CreateDelegate(typeof(T), target);
    }
}
