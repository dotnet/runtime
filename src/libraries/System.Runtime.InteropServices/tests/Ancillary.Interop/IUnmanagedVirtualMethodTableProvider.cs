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
    }

    public interface IUnmanagedVirtualMethodTableProvider<T> where T : IEquatable<T>
    {
        protected VirtualMethodTableInfo GetVirtualMethodTableInfoForKey(T typeKey);

        public sealed VirtualMethodTableInfo GetVirtualMethodTableInfoForKey<TUnmanagedInterfaceType>()
            where TUnmanagedInterfaceType : IUnmanagedInterfaceType<T>
        {
            return GetVirtualMethodTableInfoForKey(TUnmanagedInterfaceType.TypeKey);
        }
    }


    public interface IUnmanagedInterfaceType<T> where T : IEquatable<T>
    {
        public abstract static T TypeKey { get; }
    }
}
