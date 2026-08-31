// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Runtime.InteropServices
{
    internal static partial class TrackerObjectManager
    {
        private static bool HasReferenceTrackerManager
            => HasReferenceTrackerManagerInternal();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TrackerObjectManager_HasReferenceTrackerManager")]
        [SuppressGCTransition]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool HasReferenceTrackerManagerInternal();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TrackerObjectManager_TryRegisterReferenceTrackerManager")]
        [SuppressGCTransition]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool TryRegisterReferenceTrackerManager(IntPtr referenceTrackerManager);

        internal static bool IsGlobalPeggingEnabled
            => IsGlobalPeggingEnabledInternal();

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TrackerObjectManager_IsGlobalPeggingEnabled")]
        [SuppressGCTransition]
        [return: MarshalAs(UnmanagedType.U1)]
        private static partial bool IsGlobalPeggingEnabledInternal();

        private static void RegisterGCCallbacks()
        {
            // CoreCLR doesn't have GC callbacks, but we do need to register the GC handle set with the runtime for enumeration
            // during GC.
            GCHandleSet handleSet = s_referenceTrackerNativeObjectWrapperCache;
            RegisterNativeObjectWrapperCache(ObjectHandleOnStack.Create(ref handleSet));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "TrackerObjectManager_RegisterNativeObjectWrapperCache")]
        private static partial void RegisterNativeObjectWrapperCache(ObjectHandleOnStack nativeObjectWrapperCache);
    }
}
