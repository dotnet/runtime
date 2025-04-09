// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Class for managing wrappers of COM IUnknown types.
    /// </summary>
    public abstract partial class ComWrappers
    {
        /// <summary>
        /// Get the runtime provided IUnknown implementation.
        /// </summary>
        /// <param name="fpQueryInterface">Function pointer to QueryInterface.</param>
        /// <param name="fpAddRef">Function pointer to AddRef.</param>
        /// <param name="fpRelease">Function pointer to Release.</param>
        public static unsafe partial void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
            => GetIUnknownImplInternal(out fpQueryInterface, out fpAddRef, out fpRelease);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_GetIUnknownImpl")]
        private static partial void GetIUnknownImplInternal(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);

        static unsafe partial void RegisterManagedObjectWrapperForDiagnostics(object instance, ManagedObjectWrapper* wrapper)
        {
            RegisterManagedObjectWrapperForDiagnosticsInternal(ObjectHandleOnStack.Create(ref instance), wrapper);
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_RegisterManagedObjectWrapperForDiagnostics")]
        private static unsafe partial void RegisterManagedObjectWrapperForDiagnosticsInternal(ObjectHandleOnStack instance, ManagedObjectWrapper* wrapper);

        static partial void RegisterNativeObjectWrapperForDiagnostics(NativeObjectWrapper registeredWrapper)
        {
            object target = registeredWrapper.ProxyHandle.Target!;
            RegisterNativeObjectWrapperForDiagnosticsInternal(ObjectHandleOnStack.Create(ref target), ObjectHandleOnStack.Create(ref registeredWrapper));
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_RegisterNativeObjectWrapperForDiagnostics")]
        private static unsafe partial void RegisterNativeObjectWrapperForDiagnosticsInternal(ObjectHandleOnStack target, ObjectHandleOnStack wrapper);

        internal static int CallICustomQueryInterface(ManagedObjectWrapperHolder holder, ref Guid iid, out IntPtr ppObject)
        {
            if (holder.WrappedObject is ICustomQueryInterface customQueryInterface)
            {
                return (int)customQueryInterface.GetInterface(ref iid, out ppObject);
            }

            ppObject = IntPtr.Zero;
            return -1; // See TryInvokeICustomQueryInterfaceResult
        }

        internal static IntPtr GetOrCreateComInterfaceForObjectWithGlobalMarshallingInstance(object obj)
        {
            try
            {
                return s_globalInstanceForMarshalling is null
                    ? IntPtr.Zero
                    : s_globalInstanceForMarshalling.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.TrackerSupport);
            }
            catch (ArgumentException)
            {
                // We've failed to create a COM interface for the object.
                // Fallback to built-in COM.
                return IntPtr.Zero;
            }
        }

        internal static object? GetOrCreateObjectForComInstanceWithGlobalMarshallingInstance(IntPtr comObject, CreateObjectFlags flags)
        {
            try
            {
                return s_globalInstanceForMarshalling?.GetOrCreateObjectForComInstance(comObject, flags);
            }
            catch (ArgumentNullException)
            {
                // We've failed to create a managed object for the COM instance.
                // Fallback to built-in COM.
                return null;
            }
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_GetIReferenceTrackerTargetVftbl")]
        [SuppressGCTransition]
        private static unsafe partial IntPtr GetDefaultIReferenceTrackerTargetVftbl();

        private static unsafe partial IntPtr CreateDefaultIReferenceTrackerTargetVftbl()
            => GetDefaultIReferenceTrackerTargetVftbl();

        private static partial IntPtr GetTaggedImplCurrentVersion()
        {
            GetTaggedImpl(out IntPtr fpCurrentVersion);
            return fpCurrentVersion;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_GetTaggedImpl")]
        [SuppressGCTransition]
        private static partial void GetTaggedImpl(out IntPtr fpCurrentVersion);

        internal sealed unsafe partial class ManagedObjectWrapperHolder
        {
            static partial void RegisterIsRootedCallback()
            {
                RegisterIsRootedCallbackInternal();
            }

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_RegisterIsRootedCallback")]
            private static partial void RegisterIsRootedCallbackInternal();

            private static partial IntPtr AllocateRefCountedHandle(ManagedObjectWrapperHolder holder)
            {
                return AllocateRefCountedHandle(ObjectHandleOnStack.Create(ref holder));
            }

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_AllocateRefCountedHandle")]
            private static partial IntPtr AllocateRefCountedHandle(ObjectHandleOnStack obj);
        }
    }
}
