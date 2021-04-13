// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    internal static partial class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentException_NodeValueNotAllowed(string argumentName)
        {
            throw new ArgumentException(SR.NodeValueNotAllowed, argumentName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentNullException_ValueCannotBeNull(string argumentName)
        {
            throw new ArgumentNullException(SR.ValueCannotBeNull, argumentName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_NodeAlreadyHasParent()
        {
            throw new InvalidOperationException(SR.NodeAlreadyHasParent);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_NodeCycleDetected()
        {
            throw new InvalidOperationException(SR.NodeCycleDetected);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidOperationException_NodeElementCannotBeObjectOrArray()
        {
            throw new InvalidOperationException(SR.NodeElementCannotBeObjectOrArray);
        }
    }
}
