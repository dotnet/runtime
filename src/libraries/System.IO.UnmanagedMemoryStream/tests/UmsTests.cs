// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    public class UmsTests : StandaloneStreamConformanceTests, IDisposable
    {
        private List<IntPtr> _pointers = new List<IntPtr>();

        protected override void Dispose(bool disposing)
        {
            List<IntPtr> pointers = _pointers;
            _pointers = null;
            pointers.ForEach(ptr => Marshal.FreeHGlobal(ptr));

            base.Dispose(disposing);
        }

        private unsafe Task<Stream> CreateStream(byte[] initialData, FileAccess access)
        {
            Stream stream = null;
            if (initialData is not null)
            {
                IntPtr ptr = Marshal.AllocHGlobal(initialData.Length);
                _pointers.Add(ptr);
                Marshal.Copy(initialData, 0, ptr, initialData.Length);
                stream =  new UnmanagedMemoryStream((byte*)ptr, initialData.Length, initialData.Length, access);
            }
            return Task.FromResult(stream);
        }

        protected override bool CanSetLengthGreaterThanCapacity => false;

        protected override Task<Stream> CreateReadOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Read);
        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Write);
        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.ReadWrite);
    }

    public class DerivedUmsTests : StandaloneStreamConformanceTests, IDisposable
    {
        private List<IntPtr> _pointers = new List<IntPtr>();

        protected override void Dispose(bool disposing)
        {
            List<IntPtr> pointers = _pointers;
            _pointers = null;
            pointers.ForEach(ptr => Marshal.FreeHGlobal(ptr));

            base.Dispose(disposing);
        }

        private unsafe Task<Stream> CreateStream(byte[] initialData, FileAccess access)
        {
            Stream stream = null;
            if (initialData is not null)
            {
                IntPtr ptr = Marshal.AllocHGlobal(initialData.Length);
                _pointers.Add(ptr);
                Marshal.Copy(initialData, 0, ptr, initialData.Length);
                stream = new DerivedUnmanagedMemoryStream((byte*)ptr, initialData.Length, initialData.Length, access);
            }
            return Task.FromResult(stream);
        }

        protected override bool CanSetLengthGreaterThanCapacity => false;

        protected override Task<Stream> CreateReadOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Read);
        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.Write);
        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) => CreateStream(initialData, FileAccess.ReadWrite);
    }
}
