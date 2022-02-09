// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public partial class MarshalComDisabledTests
    {
        [Fact]
        public void GetTypeFromCLSID_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetTypeFromCLSID(Guid.Empty));
        }

        [Fact]
        public void CreateAggregatedObject_ThrowsPlatformNotSupportedException()
        {
            object value = new object();
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.CreateAggregatedObject(IntPtr.Zero, value));
        }

        [Fact]
        public void CreateAggregatedObject_T_ThrowsPlatformNotSupportedException()
        {
            object value = new object();
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.CreateAggregatedObject<object>(IntPtr.Zero, value));
        }


        [Fact]
        public void ReleaseComObject_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.ReleaseComObject(new object()));
        }
        
        [Fact]
        public void FinalReleaseComObject_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.FinalReleaseComObject(new object()));
        }        

        [Fact]
        public void GetComObjectData_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetComObjectData("key", "value"));
        }        

        [Fact]
        public void SetComObjectData_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.SetComObjectData(new object(), "key", "value"));
        }        

        [Fact]
        public void CreateWrapperOfType_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.CreateWrapperOfType(new object(), typeof(object)));
        }        

        [Fact]
        public void CreateWrapperOfType_T_TWrapper_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.CreateWrapperOfType<object, object>(new object()));
        }        

        [Fact]
        public void GetNativeVariantForObject_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetNativeVariantForObject(99, IntPtr.Zero));
        }

        [Fact]
        public void GetNativeVariantForObject_T_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetNativeVariantForObject<double>(99, IntPtr.Zero));
        }

        public struct NativeVariant{}

        [Fact]
        public void GetObjectForNativeVariant_ThrowsPlatformNotSupportedException()
        {
            NativeVariant variant = new NativeVariant();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeVariant>());
            try
            {
                Marshal.StructureToPtr(variant, ptr, fDeleteOld: false);
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectForNativeVariant(ptr));
            }
            finally
            {
                Marshal.DestroyStructure<NativeVariant>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }        

        public struct NativeVariant_T{}

        [Fact]
        public void GetObjectForNativeVariant_T_ThrowsPlatformNotSupportedException()
        {
            NativeVariant_T variant = new NativeVariant_T();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeVariant_T>());
            try
            {
                Marshal.StructureToPtr(variant, ptr, fDeleteOld: false);
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectForNativeVariant<NativeVariant_T>(ptr));
            }
            finally
            {
                Marshal.DestroyStructure<NativeVariant_T>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }        

        [Fact]
        public void GetObjectsForNativeVariants_ThrowsPlatformNotSupportedException()
        {
            IntPtr ptr = Marshal.AllocHGlobal(2 * Marshal.SizeOf<NativeVariant>());
            try
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectsForNativeVariants(ptr, 2));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }        

        [Fact]
        public void GetObjectsForNativeVariants_T_ThrowsPlatformNotSupportedException()
        {
            IntPtr ptr = Marshal.AllocHGlobal(2 * Marshal.SizeOf<NativeVariant_T>());
            try
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectsForNativeVariants<sbyte>(ptr, 2));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }        

        [Fact]
        public void BindToMoniker_ThrowsPlatformNotSupportedException()
        {            
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.BindToMoniker("test"));
        }        

        [Fact]
        public void GetIUnknownForObject_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIUnknownForObject(new object()));
        }        

        [Fact]
        public void GetIDispatchForObject_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIDispatchForObject(new object()));
        }        

        public struct StructForIUnknown{}

        [Fact]
        public void GetObjectForIUnknown_ThrowsPlatformNotSupportedException()
        {
            StructForIUnknown test = new StructForIUnknown();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<StructForIUnknown>());
            try
            {
                Marshal.StructureToPtr(test, ptr, fDeleteOld: false);
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetObjectForIUnknown(ptr));
            }
            finally
            {
                Marshal.DestroyStructure<StructForIUnknown>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }        
    }
}
