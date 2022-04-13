// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices
{
    public readonly ref struct VirtualMethodTableInfo
    {
        public VirtualMethodTableInfo(IntPtr thisPointer, ReadOnlySpan<IntPtr> virtualMethodTable)
        {
            ThisPointer = thisPointer;
            VirtualMethodTable = virtualMethodTable;
        }

        public IntPtr ThisPointer { get; }
        public ReadOnlySpan<IntPtr> VirtualMethodTable { get; }

        public void Deconstruct(out IntPtr thisPointer, out ReadOnlySpan<IntPtr> virtualMethodTable)
        {
            thisPointer = ThisPointer;
            virtualMethodTable = VirtualMethodTable;
        }

        public void Deconstruct(out ReadOnlySpan<IntPtr> virtualMethodTable)
        {
            if (ThisPointer != IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }
            virtualMethodTable = VirtualMethodTable;
        }
    }

    public interface IUnmanagedVirtualMethodTableProvider<T> where T : IEquatable<T>
    {
        VirtualMethodTableInfo GetFunctionPointerForIndex(T typeKey);
    }

    // Below here is scratch
    /*
    readonly record struct NoCasting { }

    //Example using IUnmanagedVirtualMethodTableProvider
    public partial class MyWrapper : IUnmanagedVirtualMethodTableProvider<NoCasting>, MyNativeAPI.Native
    //public partial class MyWrapper : IUnmanagedVirtualMethodTableProvider, IDIC
    {
        public VirtualMethodTableInfo GetFunctionPointerForIndex(NoCasting type) => throw new NotImplementedException();
    }

    partial interface MyNativeAPI
    {
        public readonly static NoCasting TypeKey;

        [VirtualMethodIndex(0)]
        void Foo();
    }

    // Generated:
    partial interface MyNativeAPI
    {
        [DynamicInterfaceCastableImplementation]
        internal interface Native : MyNativeAPI
        {
            unsafe void MyNativeAPI.Foo()
            {
                var (thisPtr, vtable) = ((IUnmanagedVirtualMethodTableProvider<NoCasting>)this).GetFunctionPointerForIndex(MyNativeAPI.TypeKey);

                ((delegate* unmanaged<void>)vtable[0])();
            }
        }
    }

    public abstract class GenericComWrappers<T> : ComWrappers
        where T: IComObjectWrapper<T>
    {
    }

    public interface IComObjectWrapper<T>
        where T : IComObjectWrapper<T>
    {
        static abstract T CreateFromIUnknown(IntPtr iUnknown);
    }


    public partial class M : GenericComWrappers<ComObject>
    { }

    // Generated
    public partial class M
    {
        protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags) => ComObject.CreateFromIUnknown(externalComObject);
    }


    public class ComObject : IUnmanagedVirtualMethodTableProvider, IDynamicInterfaceCastable, IComObjectWrapper<ComObject>
    {
        private IntPtr _iUnknown;

        private Dictionary<Type, IntPtr> _vtable;

        public static ComObject CreateFromIUnknown(IntPtr iUnknown) => new ComObject { _iUnknown = iUnknown };
        public virtual VirtualMethodTableInfo GetFunctionPointerForIndex(Type type) => QI;
        public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType) => throw new NotImplementedException();
        public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented) => throw new NotImplementedException();
    }

    public class MyComObject : ComObject, IComObjectWrapper<MyComObject>
    {
        public static new MyComObject CreateFromIUnknown(IntPtr iUnknown) => new MyComObject { _iUnknown = iUnknown };
    }
    */
}
