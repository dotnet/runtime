// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Serialization;
using System.Threading;

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
