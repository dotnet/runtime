// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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
        public static void ThrowArgumentException_DuplicateKey(string paramName, object? propertyName)
        {
            throw new ArgumentException(SR.Format(SR.NodeDuplicateKey, propertyName), paramName);
        }

        [DoesNotReturn]
        public static void ThrowKeyNotFoundException()
        {
            throw new KeyNotFoundException();
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
        public static void ThrowNotSupportedException_CollectionIsReadOnly()
        {
            throw GetNotSupportedException_CollectionIsReadOnly();
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_NodeWrongType(params ReadOnlySpan<string> supportedTypeNames)
        {
            Debug.Assert(supportedTypeNames.Length > 0);
            string concatenatedNames = supportedTypeNames.Length == 1 ? supportedTypeNames[0] : string.Join(", ", supportedTypeNames.ToArray());
            throw new InvalidOperationException(SR.Format(SR.NodeWrongType, concatenatedNames));
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_NodeParentWrongType(string typeName)
        {
            throw new InvalidOperationException(SR.Format(SR.NodeParentWrongType, typeName));
        }

        public static NotSupportedException GetNotSupportedException_CollectionIsReadOnly()
        {
            return new NotSupportedException(SR.CollectionIsReadOnly);
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_NodeUnableToConvert(Type sourceType, Type destinationType)
        {
            throw new InvalidOperationException(SR.Format(SR.NodeUnableToConvert, sourceType, destinationType));
        }

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_NodeUnableToConvertElement(JsonValueKind valueKind, Type destinationType)
        {
            throw new InvalidOperationException(SR.Format(SR.NodeUnableToConvertElement, valueKind, destinationType));
        }
    }
}
