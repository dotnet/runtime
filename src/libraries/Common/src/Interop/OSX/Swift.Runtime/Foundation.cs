// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;

#pragma warning disable CS3016 // Arrays as attribute arguments are not CLS Compliant
#pragma warning disable SYSLIB1051
#pragma warning disable IDE0060

namespace Swift
{
    /// <summary>
    /// Represents a Swift type in C#.
    /// </summary>
    internal unsafe interface ISwiftObject
    {
        public static abstract void* Metadata { get; }
    }

    // <summary>
    // Represents Swift UnsafePointer in C#.
    // </summary>
    internal readonly unsafe struct UnsafePointer<T> where T : unmanaged
    {
        private readonly T* _pointee;
        public UnsafePointer(T* pointee)
        {
            this._pointee = pointee;
        }

        public T* Pointee => _pointee;

        public static implicit operator T*(UnsafePointer<T> pointer) => pointer.Pointee;

        public static implicit operator UnsafePointer<T>(T* pointee) => new(pointee);
    }

    // <summary>
    // Represents Swift UnsafeMutablePointer in C#.
    // </summary>
    internal readonly unsafe struct UnsafeMutablePointer<T> where T : unmanaged
    {
        private readonly T* _pointee;
        public UnsafeMutablePointer(T* pointee)
        {
            _pointee = pointee;
        }

        public T* Pointee => _pointee;

        public static implicit operator T*(UnsafeMutablePointer<T> pointer) => pointer.Pointee;

        public static implicit operator UnsafeMutablePointer<T>(T* pointee) => new(pointee);
    }

    // <summary>
    // Represents Swift UnsafeRawPointer in C#.
    // </summary>
    internal readonly unsafe struct UnsafeRawPointer
    {
        private readonly void* _pointee;
        public UnsafeRawPointer(void* pointee)
        {
            _pointee = pointee;
        }

        public void* Pointee => _pointee;

        public static implicit operator void*(UnsafeRawPointer pointer) => pointer.Pointee;

        public static implicit operator UnsafeRawPointer(void* pointee) => new(pointee);
    }

    // <summary>
    // Represents Swift UnsafeMutableRawPointer in C#.
    // </summary>
    internal readonly unsafe struct UnsafeMutableRawPointer
    {
        private readonly void* _pointee;
        public UnsafeMutableRawPointer(void* pointee)
        {
            _pointee = pointee;
        }

        public void* Pointee => _pointee;

        public static implicit operator void*(UnsafeMutableRawPointer pointer) => pointer.Pointee;

        public static implicit operator UnsafeMutableRawPointer(void* pointee) => new(pointee);
    }

    // <summary>
    // Represents Swift UnsafeBufferPointer in C#.
    // </summary>
    internal readonly unsafe struct UnsafeBufferPointer<T> where T : unmanaged
    {
        private readonly T* _baseAddress;
        private readonly nint _count;
        public UnsafeBufferPointer(T* baseAddress, nint count)
        {
            _baseAddress = baseAddress;
            _count = count;
        }

        public T* BaseAddress => _baseAddress;
        public nint Count => _count;
    }

    // <summary>
    // Represents Swift UnsafeMutableBufferPointer in C#.
    // </summary>
    internal readonly unsafe struct UnsafeMutableBufferPointer<T> where T : unmanaged
    {
        private readonly T* _baseAddress;
        private readonly nint _count;
        public UnsafeMutableBufferPointer(T* baseAddress, nint count)
        {
            _baseAddress = baseAddress;
            _count = count;
        }

        public T* BaseAddress => _baseAddress;
        public nint Count => _count;
    }

    // <summary>
    // Represents Swift Foundation.Data in C#.
    // </summary>
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    [InlineArray(16)]
    internal unsafe partial struct Data : ISwiftObject
    {
        internal byte payload;

        internal unsafe Data(UnsafeRawPointer pointer, nint count)
        {
            this = Foundation.PInvoke_Data_InitWithBytes(pointer, count);
        }

        internal readonly nint Count => Foundation.PInvoke_Data_GetCount(this);

        internal unsafe void CopyBytes(UnsafeMutablePointer<byte> buffer, nint count)
        {
            Foundation.PInvoke_Data_CopyBytes(buffer, count, this);
        }

        public static void* Metadata => Foundation.PInvoke_Data_GetMetadata();
    }

    /// <summary>
    /// Represents Swift Foundation.DataProtocol in C#.
    /// </summary>
    internal unsafe interface IDataProtocol
    {
        public static void* GetConformanceDescriptor => Runtime.GetConformanceDescriptor("$s10Foundation4DataVAA0B8ProtocolAAMc");
    }

    /// <summary>
    /// Represents Swift Foundation.ContiguousBytes in C#.
    /// </summary>
    internal unsafe interface IContiguousBytes
    {
        public static void* GetConformanceDescriptor => Runtime.GetConformanceDescriptor("$s10Foundation4DataVAA15ContiguousBytesAAMc");
    }

    /// <summary>
    /// Swift Foundation PInvoke methods in C#.
    /// </summary>
    internal static partial class Foundation
    {
        internal const string Path = "/System/Library/Frameworks/Foundation.framework/Foundation";

        [LibraryImport(Path, EntryPoint = "$s10Foundation4DataV5bytes5countACSV_SitcfC")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial Data PInvoke_Data_InitWithBytes(UnsafeRawPointer pointer, nint count);

        [LibraryImport(Path, EntryPoint = "$s10Foundation4DataV5countSivg")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial nint PInvoke_Data_GetCount(Data data);

        [LibraryImport(Path, EntryPoint = "$s10Foundation4DataV9copyBytes2to5countySpys5UInt8VG_SitF")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void PInvoke_Data_CopyBytes(UnsafeMutablePointer<byte> buffer, nint count, Data data);

        [LibraryImport(Path, EntryPoint = "swift_getWitnessTable")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void* PInvoke_Swift_GetWitnessTable(void* conformanceDescriptor, void* typeMetadata, void* instantiationArgs);

        [LibraryImport(Path, EntryPoint = "$s10Foundation4DataVMa")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static unsafe partial void* PInvoke_Data_GetMetadata();
    }

    /// <summary>
    /// Swift runtime helper methods in C#.
    /// </summary>
    internal static partial class Runtime
    {

        internal static unsafe void* GetMetadata<T>(T type) where T: ISwiftObject
        {
            return T.Metadata;
        }

        internal static unsafe void* GetConformanceDescriptor(string symbol)
        {
            IntPtr handle = NativeLibrary.Load(Foundation.Path);
            void* conformanceDescriptor = NativeLibrary.GetExport(handle, symbol).ToPointer();
            return conformanceDescriptor;
        }
    }
}
