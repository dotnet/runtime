// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentException_NodeValueNotAllowed(string paramName)
        {
            throw new ArgumentException(SR.NodeValueNotAllowed, paramName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentException_NodeArrayTooSmall(string paramName)
        {
            throw new ArgumentException(SR.NodeArrayTooSmall, paramName);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName, SR.NodeArrayIndexNegative);
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowArgumentException_DuplicateKey(string propertyName)
        {
            throw new ArgumentException(SR.NodeDuplicateKey, propertyName);
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

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNotSupportedException_NodeCollectionIsReadOnly()
        {
            throw NotSupportedException_NodeCollectionIsReadOnly();
        }

        public static NotSupportedException NotSupportedException_NodeCollectionIsReadOnly()
        {
            return new NotSupportedException(SR.NodeCollectionIsReadOnly);
        }
    }
}
