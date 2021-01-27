// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.ObjectiveC
{
    /// <summary>
    /// An Objective-C object instance.
    /// </summary>
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct Instance
    {
        public IntPtr Isa;

        /// <summary>
        /// Given an instance, return the underlying type.
        /// </summary>
        /// <typeparam name="T">Managed type of the instance.</typeparam>
        /// <param name="instancePtr">Instance pointer</param>
        /// <returns>The managed instance</returns>
        public static unsafe T GetInstance<T>(Instance* instancePtr) where T : class
        {
            // Access to the Objective-C object_getIndexedIvars API is needed here.
            // var lifetimePtr = (ManagedObjectWrapperLifetime**)object_getIndexedIvars((nint)instancePtr);
            // var gcHandle = GCHandle.FromIntPtr((*lifetimePtr)->GCHandle);
            // return (T)gcHandle.Target;

            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// An Objective-C block instance.
    /// </summary>
    /// <remarks>
    /// See http://clang.llvm.org/docs/Block-ABI-Apple.html#high-level for a
    /// description of the ABI represented by this data structure.
    /// </remarks>
    [SupportedOSPlatform("macos")]
    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct BlockLiteral
    {
        internal IntPtr Isa;
        internal int Flags;
        internal int Reserved;
        internal IntPtr Invoke; // delegate* unmanaged[Cdecl]<BlockLiteral* , ...args, ret>
        internal unsafe BlockDescriptor* BlockDescriptor;

        // Extension of ABI to handle .NET lifetime.
        internal unsafe BlockLifetime* Lifetime;

        /// <summary>
        /// Get <typeparamref name="T"/> type from the supplied Block.
        /// </summary>
        /// <typeparam name="T">The delegate type the block is associated with.</typeparam>
        /// <param name="block">The block instance</param>
        /// <returns>A delegate</returns>
        public static unsafe T GetDelegate<T>(BlockLiteral* block) where T : Delegate
        {
            var gcHandle = GCHandle.FromIntPtr(block->Lifetime->GCHandle);
            return (T)gcHandle.Target!;
        }
    }

    // Internal Block Descriptor data structure.
    // http://clang.llvm.org/docs/Block-ABI-Apple.html#high-level
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct BlockDescriptor
    {
        public nint Reserved;
        public nint Size;
        public delegate* unmanaged[Cdecl]<BlockLiteral*, BlockLiteral*, void> Copy_helper;
        public delegate* unmanaged[Cdecl]<BlockLiteral*, void> Dispose_helper;
        public void* Signature;
    }

    // Internal data structure for managing Block lifetime.
    [StructLayout(LayoutKind.Sequential)]
    internal struct BlockLifetime
    {
        public IntPtr GCHandle;
        public int RefCount;
    }
}
