// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices;

/// <summary>Provides downlevel polyfills for static methods on <see cref="RuntimeHelpers"/>.</summary>
internal static class RuntimeHelpersPolyfills
{
    extension(RuntimeHelpers)
    {
        public static bool TryEnsureSufficientExecutionStack()
        {
            try
            {
                RuntimeHelpers.EnsureSufficientExecutionStack();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object GetUninitializedObject(Type type)
        {
#pragma warning disable SYSLIB0050 // FormatterServices.GetUninitializedObject is obsolete
            return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
#pragma warning restore SYSLIB0050
        }
    }
}
