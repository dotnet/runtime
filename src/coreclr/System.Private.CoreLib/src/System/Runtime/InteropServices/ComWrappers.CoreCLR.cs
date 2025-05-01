// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Concurrent;

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
        public static unsafe void GetIUnknownImpl(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease)
            => GetIUnknownImplInternal(out fpQueryInterface, out fpAddRef, out fpRelease);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_GetIUnknownImpl")]
        [SuppressGCTransition]
        private static partial void GetIUnknownImplInternal(out IntPtr fpQueryInterface, out IntPtr fpAddRef, out IntPtr fpRelease);

        internal static IntPtr DefaultIUnknownVftblPtr { get; } = CreateDefaultIUnknownVftbl();
        internal static IntPtr TaggedImplVftblPtr { get; } = CreateTaggedImplVftbl();
        internal static IntPtr DefaultIReferenceTrackerTargetVftblPtr { get; } = CreateDefaultIReferenceTrackerTargetVftbl();

        private static unsafe IntPtr CreateDefaultIUnknownVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappers), 3 * sizeof(IntPtr));
            GetIUnknownImpl(out vftbl[0], out vftbl[1], out vftbl[2]);
            return (IntPtr)vftbl;
        }

        private static unsafe IntPtr CreateTaggedImplVftbl()
        {
            IntPtr* vftbl = (IntPtr*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(ComWrappers), 4 * sizeof(IntPtr));
            GetIUnknownImpl(out vftbl[0], out vftbl[1], out vftbl[2]);
            vftbl[3] = GetTaggedImplCurrentVersion();
            return (IntPtr)vftbl;
        }

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
        private static partial IntPtr GetDefaultIReferenceTrackerTargetVftbl();

        private static IntPtr CreateDefaultIReferenceTrackerTargetVftbl()
            => GetDefaultIReferenceTrackerTargetVftbl();

        private static IntPtr GetTaggedImplCurrentVersion()
        {
            return GetTaggedImpl();
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_GetTaggedImpl")]
        [SuppressGCTransition]
        private static partial IntPtr GetTaggedImpl();

        internal sealed partial class ManagedObjectWrapperHolder
        {
            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_RegisterIsRootedCallback")]
            private static partial void RegisterIsRootedCallback();

            private static IntPtr AllocateRefCountedHandle(ManagedObjectWrapperHolder holder)
            {
                return AllocateRefCountedHandle(ObjectHandleOnStack.Create(ref holder));
            }

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ComWrappers_AllocateRefCountedHandle")]
            private static partial IntPtr AllocateRefCountedHandle(ObjectHandleOnStack obj);
        }
    }
}
