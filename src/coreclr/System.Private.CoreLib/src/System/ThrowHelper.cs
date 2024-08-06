// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static unsafe partial class ThrowHelper
    {
        [DoesNotReturn]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ExceptionNative_ThrowAmbiguousResolutionException")]
        private static partial void ThrowAmbiguousResolutionException(MethodTable* targetType, MethodTable* interfaceType, void* methodDesc);

        [DoesNotReturn]
        internal static void ThrowAmbiguousResolutionException(
            void* method,           // MethodDesc*
            void* interfaceType,    // MethodTable*
            void* targetType)       // MethodTable*
        {
            ThrowAmbiguousResolutionException((MethodTable*)targetType, (MethodTable*)interfaceType, method);
        }

        [DoesNotReturn]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ExceptionNative_ThrowEntryPointNotFoundException")]
        private static partial void ThrowEntryPointNotFoundException(MethodTable* targetType, MethodTable* interfaceType, void* methodDesc);

        [DoesNotReturn]
        internal static void ThrowEntryPointNotFoundException(
            void* method,           // MethodDesc*
            void* interfaceType,    // MethodTable*
            void* targetType)       // MethodTable*
        {
            ThrowEntryPointNotFoundException((MethodTable*)targetType, (MethodTable*)interfaceType, method);
        }
    }
}
