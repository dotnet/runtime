﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentException_NodeValueNotAllowed(string paramName)
        {
            throw new ArgumentException(SR.NodeValueNotAllowed, paramName);
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_NodeArrayTooSmall(string paramName)
        {
            throw new ArgumentException(SR.NodeArrayTooSmall, paramName);
        }

        [DoesNotReturn]
        public static void ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName, SR.NodeArrayIndexNegative);
        }

        [DoesNotReturn]
        public static void ThrowArgumentException_DuplicateKey(string propertyName)
        {
            throw new ArgumentException(SR.NodeDuplicateKey, propertyName);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_NodeAlreadyHasParent()
        {
            throw new InvalidOperationException(SR.NodeAlreadyHasParent);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_NodeCycleDetected()
        {
            throw new InvalidOperationException(SR.NodeCycleDetected);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_NodeElementCannotBeObjectOrArray()
        {
            throw new InvalidOperationException(SR.NodeElementCannotBeObjectOrArray);
        }

        [DoesNotReturn]
        public static void ThrowNotSupportedException_NodeCollectionIsReadOnly()
        {
            throw GetNotSupportedException_NodeCollectionIsReadOnly();
        }

        public static NotSupportedException GetNotSupportedException_NodeCollectionIsReadOnly()
        {
            return new NotSupportedException(SR.NodeCollectionIsReadOnly);
        }
    }
}
