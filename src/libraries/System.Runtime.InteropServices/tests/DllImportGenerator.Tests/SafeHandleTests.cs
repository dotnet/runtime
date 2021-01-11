using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public class NativeExportsSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private NativeExportsSafeHandle() : base(ownsHandle: true)
            { }

            protected override bool ReleaseHandle()
            {
                bool didRelease = NativeExportsNE.ReleaseHandle(handle);
                Assert.True(didRelease);
                return didRelease;
            }
        }

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "alloc_handle")]
        public static partial NativeExportsSafeHandle AllocateHandle();

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "alloc_handle_out")]
        public static partial void AllocateHandle(out NativeExportsSafeHandle handle);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "release_handle")]
        [return:MarshalAs(UnmanagedType.I1)]
        private static partial bool ReleaseHandle(nint handle);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "is_handle_alive")]
        [return:MarshalAs(UnmanagedType.I1)]
        public static partial bool IsHandleAlive(NativeExportsSafeHandle handle);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "modify_handle")]
        public static partial void ModifyHandle(ref NativeExportsSafeHandle handle, [MarshalAs(UnmanagedType.I1)] bool newHandle);
    }

    public class SafeHandleTests
    {
        [Fact]
        public void ReturnValue_CreatesSafeHandle()
        {
            using NativeExportsNE.NativeExportsSafeHandle handle = NativeExportsNE.AllocateHandle();
            Assert.False(handle.IsClosed);
            Assert.False(handle.IsInvalid);
        }

        [Fact]
        public void ByValue_CorrectlyUnwrapsHandle()
        {
            using NativeExportsNE.NativeExportsSafeHandle handle = NativeExportsNE.AllocateHandle();
            Assert.True(NativeExportsNE.IsHandleAlive(handle));
        }

        [Fact]
        public void ByRefOut_CreatesSafeHandle()
        {
            NativeExportsNE.NativeExportsSafeHandle handle;
            NativeExportsNE.AllocateHandle(out handle);
            Assert.False(handle.IsClosed);
            Assert.False(handle.IsInvalid);
            handle.Dispose();
        }

        [Fact]
        public void ByRefSameValue_UsesSameHandleInstance()
        {
            using NativeExportsNE.NativeExportsSafeHandle handleToDispose = NativeExportsNE.AllocateHandle();
            NativeExportsNE.NativeExportsSafeHandle handle = handleToDispose;
            NativeExportsNE.ModifyHandle(ref handle, newHandle: false);
            Assert.Same(handleToDispose, handle);
        }

        [Fact]
        public void ByRefDifferentValue_UsesNewHandleInstance()
        {
            using NativeExportsNE.NativeExportsSafeHandle handleToDispose = NativeExportsNE.AllocateHandle();
            NativeExportsNE.NativeExportsSafeHandle handle = handleToDispose;
            NativeExportsNE.ModifyHandle(ref handle, newHandle: true);
            Assert.NotSame(handleToDispose, handle);
            handle.Dispose();
        }
    }
}