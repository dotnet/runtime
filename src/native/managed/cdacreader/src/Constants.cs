// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

internal static class Constants
{
    internal static class Globals
    {
        // See src/coreclr/debug/runtimeinfo/datadescriptor.h
        internal const string AppDomain = nameof(AppDomain);
        internal const string ThreadStore = nameof(ThreadStore);
        internal const string FinalizerThread = nameof(FinalizerThread);
        internal const string GCThread = nameof(GCThread);

        internal const string FeatureCOMInterop = nameof(FeatureCOMInterop);
        internal const string FeatureEHFunclets = nameof(FeatureEHFunclets);

        internal const string ObjectToMethodTableUnmask = nameof(ObjectToMethodTableUnmask);
        internal const string SOSBreakingChangeVersion = nameof(SOSBreakingChangeVersion);

        internal const string ExceptionMethodTable = nameof(ExceptionMethodTable);
        internal const string FreeObjectMethodTable = nameof(FreeObjectMethodTable);
        internal const string ObjectMethodTable = nameof(ObjectMethodTable);
        internal const string ObjectArrayMethodTable = nameof(ObjectArrayMethodTable);
        internal const string StringMethodTable = nameof(StringMethodTable);

        internal const string MiniMetaDataBuffAddress = nameof(MiniMetaDataBuffAddress);
        internal const string MiniMetaDataBuffMaxSize = nameof(MiniMetaDataBuffMaxSize);

        internal const string MethodDescAlignment = nameof(MethodDescAlignment);
        internal const string ObjectHeaderSize = nameof(ObjectHeaderSize);

        internal const string ArrayBoundsZero = nameof(ArrayBoundsZero);
    }
}
