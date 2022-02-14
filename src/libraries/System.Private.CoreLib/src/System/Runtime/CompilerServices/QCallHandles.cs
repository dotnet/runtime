// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Wrappers used to pass objects to and from QCalls.

using System.Threading;
using Internal.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    // Wrapper for address of a string variable on stack
    internal unsafe ref struct StringHandleOnStack
    {
        private void* _ptr;

        internal StringHandleOnStack(ref string? s)
        {
            _ptr = Unsafe.AsPointer(ref s);
        }
    }

    // Wrapper for address of a object variable on stack
    internal unsafe ref struct ObjectHandleOnStack
    {
        private void* _ptr;

        private ObjectHandleOnStack(void* pObject)
        {
            _ptr = pObject;
        }

        internal static ObjectHandleOnStack Create<T>(ref T o) where T : class?
        {
            return new ObjectHandleOnStack(Unsafe.AsPointer(ref o));
        }
    }

    // Wrapper for StackCrawlMark
    internal unsafe ref struct StackCrawlMarkHandle
    {
        private void* _ptr;

        internal StackCrawlMarkHandle(ref StackCrawlMark stackMark)
        {
            _ptr = Unsafe.AsPointer(ref stackMark);
        }
    }

    // Wraps RuntimeModule into a handle. Used to pass RuntimeModule to native code without letting it be collected
    internal unsafe ref struct QCallModule
    {
        private void* _ptr;
        private IntPtr _module;

        internal QCallModule(ref System.Reflection.RuntimeModule module)
        {
            _ptr = Unsafe.AsPointer(ref module);
            _module = module.GetUnderlyingNativeHandle();
        }

        internal QCallModule(ref System.Reflection.Emit.ModuleBuilder module)
        {
            _ptr = Unsafe.AsPointer(ref module);
            _module = module.InternalModule.GetUnderlyingNativeHandle();
        }
    }

    // Wraps RuntimeAssembly into a handle. Used to pass RuntimeAssembly to native code without letting it be collected
    internal unsafe ref struct QCallAssembly
    {
        private void* _ptr;
        private IntPtr _assembly;

        internal QCallAssembly(ref System.Reflection.RuntimeAssembly assembly)
        {
            _ptr = Unsafe.AsPointer(ref assembly);
            _assembly = assembly?.GetUnderlyingNativeHandle() ?? IntPtr.Zero;
        }
    }

    // Wraps RuntimeType into a handle. Used to pass RuntimeType to native code without letting it be collected
    internal unsafe ref struct QCallTypeHandle
    {
        private void* _ptr;
        private IntPtr _handle;

        internal QCallTypeHandle(ref System.RuntimeType type)
        {
            _ptr = Unsafe.AsPointer(ref type);
            _handle = type?.GetUnderlyingNativeHandle() ?? IntPtr.Zero;
        }

        internal QCallTypeHandle(ref System.RuntimeTypeHandle rth)
        {
            _ptr = Unsafe.AsPointer(ref rth);
            _handle = rth.Value;
        }
    }
}
