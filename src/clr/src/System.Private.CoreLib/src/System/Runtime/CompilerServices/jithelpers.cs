// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
// JitHelpers
//    Low-level Jit Helpers
////////////////////////////////////////////////////////////////////////////////

using System.Threading;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    // Wrapper for address of a string variable on stack
    internal struct StringHandleOnStack
    {
        private IntPtr m_ptr;

        internal StringHandleOnStack(IntPtr pString)
        {
            m_ptr = pString;
        }
    }

    // Wrapper for address of a object variable on stack
    internal struct ObjectHandleOnStack
    {
        private IntPtr m_ptr;

        internal ObjectHandleOnStack(IntPtr pObject)
        {
            m_ptr = pObject;
        }
    }

    // Wrapper for StackCrawlMark
    internal struct StackCrawlMarkHandle
    {
        private IntPtr m_ptr;

        internal StackCrawlMarkHandle(IntPtr stackMark)
        {
            m_ptr = stackMark;
        }
    }

    internal struct QCallModule
    {
        private IntPtr m_ptr;
        private IntPtr m_module;
        internal QCallModule(IntPtr pObject, System.Reflection.RuntimeModule module)
        {
            m_ptr = pObject;
            m_module = module.GetUnderlyingNativeHandle();
        }
        internal QCallModule(IntPtr pObject, System.Reflection.Emit.ModuleBuilder module)
        {
            m_ptr = pObject;
            m_module = module.GetNativeHandle().GetUnderlyingNativeHandle();
        }
    }

    internal struct QCallAssembly
    {
        private IntPtr m_ptr;
        private IntPtr m_assembly;
        internal QCallAssembly(IntPtr pObject, System.Reflection.RuntimeAssembly assembly)
        {
            m_ptr = pObject;
            m_assembly = assembly.GetUnderlyingNativeHandle();
        }
    }

    internal struct QCallTypeHandle
    {
        private IntPtr m_ptr;
        private IntPtr m_handle;
        internal QCallTypeHandle(IntPtr pObject, RuntimeType type)
        {
            m_ptr = pObject;
            if (type != null)
                m_handle = type.m_handle;
            else
                m_handle = IntPtr.Zero;
        }
    }

    // Helper class to assist with unsafe pinning of arbitrary objects.
    // It's used by VM code.
    internal class RawData
    {
        public byte Data;
    }

    internal class RawSzArrayData
    {
        public IntPtr Count; // Array._numComponents padded to IntPtr
        public byte Data;
    }

    internal static unsafe class JitHelpers
    {
        // The special dll name to be used for DllImport of QCalls
        internal const string QCall = "QCall";

        // Wraps object variable into a handle. Used to return managed strings from QCalls.
        // s has to be a local variable on the stack.
        internal static StringHandleOnStack GetStringHandleOnStack(ref string? s)
        {
            return new StringHandleOnStack((IntPtr)Unsafe.AsPointer(ref s));
        }

        // Wraps object variable into a handle. Used to pass managed object references in and out of QCalls.
        // o has to be a local variable on the stack.
        internal static ObjectHandleOnStack GetObjectHandleOnStack<T>(ref T o) where T : class?
        {
            return new ObjectHandleOnStack((IntPtr)Unsafe.AsPointer(ref o));
        }

        // Wraps RuntimeModule into a handle. Used to pass RuntimeModule to native code wihtout letting it be collected
        internal static QCallModule GetQCallModuleOnStack(ref System.Reflection.RuntimeModule module)
        {
            return new QCallModule((IntPtr)Unsafe.AsPointer(ref module), module);
        }

        internal static QCallModule GetQCallModuleOnStack(ref System.Reflection.Emit.ModuleBuilder module)
        {
            return new QCallModule((IntPtr)Unsafe.AsPointer(ref module), module);
        }

        // Wraps RuntimeAssembly into a handle. Used to pass RuntimeAssembly to native code wihtout letting it be collected
        internal static QCallAssembly GetQCallAssemblyOnStack(ref System.Reflection.RuntimeAssembly assembly)
        {
            return new QCallAssembly((IntPtr)Unsafe.AsPointer(ref assembly), assembly);
        }

        // Wraps RuntimeTypeHandle into a handle. Used to pass RuntimeAssembly to native code wihtout letting it be collected
        internal static QCallTypeHandle GetQCallTypeHandleOnStack(ref System.RuntimeTypeHandle rth)
        {
            return new QCallTypeHandle((IntPtr)Unsafe.AsPointer(ref rth.m_type), rth.m_type);
        }

        // Wraps RuntimeTypeHandle into a handle. Used to pass RuntimeAssembly to native code wihtout letting it be collected
        internal static QCallTypeHandle GetQCallTypeHandleOnStack(ref System.RuntimeType type)
        {
            return new QCallTypeHandle((IntPtr)Unsafe.AsPointer(ref type), type);
        }

        // Wraps StackCrawlMark into a handle. Used to pass StackCrawlMark to QCalls.
        // stackMark has to be a local variable on the stack.
        internal static StackCrawlMarkHandle GetStackCrawlMarkHandle(ref StackCrawlMark stackMark)
        {
            return new StackCrawlMarkHandle((IntPtr)Unsafe.AsPointer(ref stackMark));
        }

        internal static bool EnumEquals<T>(T x, T y) where T : struct, Enum
        {
            // The body of this function will be replaced by the EE with unsafe code
            // See getILIntrinsicImplementation for how this happens.
            return x.Equals(y);
        }

        internal static int EnumCompareTo<T>(T x, T y) where T : struct, Enum
        {
            // The body of this function will be replaced by the EE with unsafe code
            // See getILIntrinsicImplementation for how this happens.
            return x.CompareTo(y);
        }

        internal static ref byte GetRawData(this object obj) =>
            ref Unsafe.As<RawData>(obj).Data;

        internal static ref byte GetRawSzArrayData(this Array array) =>
            ref Unsafe.As<RawSzArrayData>(array).Data;
    }
}
