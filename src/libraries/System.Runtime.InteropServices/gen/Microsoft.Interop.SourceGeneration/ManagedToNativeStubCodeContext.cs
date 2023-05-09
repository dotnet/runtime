// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
namespace Microsoft.Interop
{
    public sealed record ManagedToNativeStubCodeContext : StubCodeContext
    {
        public override bool SingleFrameSpansNativeContext => true;

        public override bool AdditionalTemporaryStateLivesAcrossStages => true;

        private readonly TargetFramework _framework;
        private readonly Version _frameworkVersion;

        private const string InvokeReturnIdentifier = "__invokeRetVal";
        private const string InvokeReturnIdentifierNative = "__invokeRetValUnmanaged";
        private readonly string _returnIdentifier;
        private readonly string _nativeReturnIdentifier;

        public ManagedToNativeStubCodeContext(
            TargetFramework targetFramework,
            Version targetFrameworkVersion,
            string returnIdentifier,
            string nativeReturnIdentifier)
        {
            _framework = targetFramework;
            _frameworkVersion = targetFrameworkVersion;
            _returnIdentifier = returnIdentifier;
            _nativeReturnIdentifier = nativeReturnIdentifier;
        }

        public override (TargetFramework framework, Version version) GetTargetFramework()
            =>  (_framework, _frameworkVersion);

        public override (string managed, string native) GetIdentifiers(TypePositionInfo info)
        {
            // If the info is in the managed return position, then we need to generate a name to use
            // for both the managed and native values since there is no name in the signature for the return value.
            if (info.IsManagedReturnPosition)
            {
                return (_returnIdentifier, _nativeReturnIdentifier);
            }
            // If the info is in the native return position but is not in the managed return position,
            // then that means that the stub is introducing an additional info for the return position.
            // This means that there is no name in source for this info, so we must provide one here.
            // We can't use ReturnIdentifier or ReturnNativeIdentifier since that will be used by the managed return value.
            // Additionally, since all use cases today of a TypePositionInfo in the native position but not the managed
            // are for infos that aren't in the managed signature at all (PreserveSig scenario), we don't have a name
            // that we can use from source.
            // If this changes, the assert below will trigger and we will need to decide what to do.
            // As a result, we generate another name for the native return value
            // and use the same name for native and managed.
            else if (info.IsNativeReturnPosition)
            {
                Debug.Assert(info.ManagedIndex == TypePositionInfo.UnsetIndex);
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
