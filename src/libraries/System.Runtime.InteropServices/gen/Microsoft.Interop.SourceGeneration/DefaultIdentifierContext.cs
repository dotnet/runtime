// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
namespace Microsoft.Interop
{
    public sealed record DefaultIdentifierContext : StubIdentifierContext
    {
        private const string InvokeReturnIdentifier = "__invokeRetVal";
        private const string InvokeReturnIdentifierNative = "__invokeRetValUnmanaged";
        private readonly string _returnIdentifier;
        private readonly string _nativeReturnIdentifier;
        private readonly MarshalDirection _direction;

        public DefaultIdentifierContext(
            string returnIdentifier,
            string nativeReturnIdentifier,
            MarshalDirection direction)
        {
            _returnIdentifier = returnIdentifier;
            _nativeReturnIdentifier = nativeReturnIdentifier;
            _direction = direction;
        }

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            // If the info is in the stub return position, then we need to generate a name to use
            // for both the managed and native values since there is no name in the signature for the return value.
            if (MarshallerHelpers.IsInStubReturnPosition(info, _direction))
            {
                // If the info is in the native exception position,
                // then we're going to return using name of the native return identifier.
                // We use the provided instance identifier as that represents
                // the name of the exception variable specified in the catch clause.
                if (info.IsManagedExceptionPosition)
                {
                    return (info.InstanceIdentifier, _nativeReturnIdentifier);
                }
                return (_returnIdentifier, _nativeReturnIdentifier);
            }
            // If the info is in the invocation return position but is not in the stub return position,
            // then that means that the stub is introducing an additional info for the return position.
            // This means that there is no name in source for this info, so we must provide one here.
            else if (MarshallerHelpers.IsInInvocationReturnPosition(info, _direction))
            {
                return (InvokeReturnIdentifier, InvokeReturnIdentifierNative);
            }
            else
            {
                // If the info isn't in either the managed or native return position,
                // then we can use the base implementation since we have an identifier name provided
                // in the original metadata.
                return base.GetIdentifiers(info);
            }
        }
    }
}
