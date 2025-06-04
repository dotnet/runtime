// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Wrappers used to pass objects to and from QCalls.

using System.Threading;

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

    internal ref struct ByteRef
    {
        private ref byte _ref;

        internal ByteRef(ref byte byteReference)
        {
            _ref = ref byteReference;
        }

        internal ref byte Get()
        {
            return ref _ref;
        }
    }

    // Wrapper for address of a byref to byte variable on stack
    internal unsafe ref struct ByteRefOnStack
    {
        private readonly void* _pByteRef;
        private ByteRefOnStack(void* pByteRef)
        {
            _pByteRef = pByteRef;
        }

        internal static ByteRefOnStack Create(ref ByteRef byteRef)
        {
            // This is valid because the ByteRef is ByRefLike (stack allocated)
            // and the ByteRefOnStack is expected to have a shorter lifetime
            // than the ByteRef instance.
            return new ByteRefOnStack(Unsafe.AsPointer(ref byteRef));
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

        internal QCallModule(ref Reflection.RuntimeModule module)
        {
            _ptr = Unsafe.AsPointer(ref module);
            _module = module.GetUnderlyingNativeHandle();
        }

        internal QCallModule(ref Reflection.Emit.RuntimeModuleBuilder module)
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

        internal QCallAssembly(ref Reflection.RuntimeAssembly assembly)
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

        internal QCallTypeHandle(ref RuntimeType type)
        {
            _ptr = Unsafe.AsPointer(ref type);
            _handle = type?.GetUnderlyingNativeHandle() ?? IntPtr.Zero;
        }

        internal QCallTypeHandle(ref RuntimeTypeHandle rth)
        {
            _ptr = Unsafe.AsPointer(ref rth);
            _handle = rth.Value;
        }
    }
}
