// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public sealed record NativeToManagedStubCodeContext : StubCodeContext
    {
        public override bool SingleFrameSpansNativeContext => false;

        public override bool AdditionalTemporaryStateLivesAcrossStages => true;

        private const string InvokeReturnIdentifier = "__invokeRetVal";
        private const string InvokeReturnIdentifierNative = "__invokeRetValUnmanaged";
        private readonly string _returnIdentifier;
        private readonly string _nativeReturnIdentifier;

        public NativeToManagedStubCodeContext(
            string returnIdentifier,
            string nativeReturnIdentifier)
        {
            _returnIdentifier = returnIdentifier;
            _nativeReturnIdentifier = nativeReturnIdentifier;
            Direction = MarshalDirection.UnmanagedToManaged;
        }

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            // If the info is in the native return position, then we need to generate a name to use
            // for both the managed and native values since there is no name in the signature for the return value.
            if (info.IsNativeReturnPosition)
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
            // If the info is in the managed return position but is not in the native return position,
            // then that means that the stub is introducing an additional info for the return position.
            // This element can be in any position in the native signature,
            // but since it isn't in the managed signature, there is no name in source for this info, so we must provide one here.
            // We can't use ReturnIdentifier or ReturnNativeIdentifier since that will be used by the return value of the stub itself.
            // As a result, we generate another name for the native return value.
            if (info.IsManagedReturnPosition)
            {
                return (InvokeReturnIdentifier, InvokeReturnIdentifierNative);
            }

            // If the info isn't in either the managed or native return position,
            // then we can use the base implementation since we have an identifier name provided
            // in the original metadata.
            return base.GetIdentifiers(info);
        }
    }
}
