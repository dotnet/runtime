//using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public partial class MarshalComDisabledTests
    {
        [Fact]
        public void GetTypeFromCLSID_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetTypeFromCLSID(Guid.Empty));
        }

        [Fact]
        public void CreateAggregatedObject_ThrowsNotSupportedException()
        {
            object value = new object();
            Assert.Throws<NotSupportedException>(() => Marshal.CreateAggregatedObject(IntPtr.Zero, value));
        }

        [Fact]
        public void CreateAggregatedObject_T_ThrowsNotSupportedException()
        {
            object value = new object();
            Assert.Throws<NotSupportedException>(() => Marshal.CreateAggregatedObject<object>(IntPtr.Zero, value));
        }


        [Fact]
        public void ReleaseComObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.ReleaseComObject(new object()));
        }
        
        [Fact]
        public void FinalReleaseComObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.FinalReleaseComObject(new object()));
        }        

        [Fact]
        public void GetComObjectData_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetComObjectData("key", "value"));
        }        

        [Fact]
        public void SetComObjectData_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.SetComObjectData(new object(), "key", "value"));
        }        

        [Fact]
        public void CreateWrapperOfType_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.CreateWrapperOfType(new object(), typeof(object)));
        }        

        [Fact]
        public void CreateWrapperOfType_T_TWrapper_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.CreateWrapperOfType<object, object>(new object()));
        }        

        [Fact]
        public void GetNativeVariantForObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetNativeVariantForObject(99, IntPtr.Zero));
        }

        [Fact]
        public void GetNativeVariantForObject_T_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetNativeVariantForObject<double>(99, IntPtr.Zero));
        }

        public struct NativeVariant{}

        [Fact]
        public void GetObjectForNativeVariant_ThrowsNotSupportedException()
        {
            NativeVariant variant = new NativeVariant();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeVariant>());
            try
            {
                Marshal.StructureToPtr(variant, ptr, fDeleteOld: false);
                Assert.Throws<NotSupportedException>(() => Marshal.GetObjectForNativeVariant(ptr));
            }
            finally
            {
                Marshal.DestroyStructure<NativeVariant>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }        

        public struct NativeVariant_T{}

        [Fact]
        public void GetObjectForNativeVariant_T_ThrowsNotSupportedException()
        {
            NativeVariant_T variant = new NativeVariant_T();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeVariant_T>());
            try
            {
                Marshal.StructureToPtr(variant, ptr, fDeleteOld: false);
                Assert.Throws<NotSupportedException>(() => Marshal.GetObjectForNativeVariant<NativeVariant_T>(ptr));
            }
            finally
            {
                Marshal.DestroyStructure<NativeVariant_T>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }        

        [Fact]
        public void GetObjectsForNativeVariants_ThrowsNotSupportedException()
        {
            IntPtr ptr = Marshal.AllocHGlobal(2 * Marshal.SizeOf<NativeVariant>());
            try
            {
                Assert.Throws<NotSupportedException>(() => Marshal.GetObjectsForNativeVariants(ptr, 2));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }        

        [Fact]
        public void GetObjectsForNativeVariants_T_ThrowsNotSupportedException()
        {
            IntPtr ptr = Marshal.AllocHGlobal(2 * Marshal.SizeOf<NativeVariant_T>());
            try
            {
                Assert.Throws<NotSupportedException>(() => Marshal.GetObjectsForNativeVariants<sbyte>(ptr, 2));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }        

        [Fact]
        public void BindToMoniker_ThrowsNotSupportedException()
        {            
            Assert.Throws<NotSupportedException>(() => Marshal.BindToMoniker("test"));
        }        

        [Fact]
        public void GetIUnknownForObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetIUnknownForObject(new object()));
        }        

        [Fact]
        public void GetIDispatchForObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetIDispatchForObject(new object()));
        }        

        public struct StructForIUnknown{}

        [Fact]
        public void GetObjectForIUnknown_ThrowsNotSupportedException()
        {
            StructForIUnknown test = new StructForIUnknown();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<StructForIUnknown>());
            try
            {
                Marshal.StructureToPtr(test, ptr, fDeleteOld: false);
                Assert.Throws<NotSupportedException>(() => Marshal.GetObjectForIUnknown(ptr));
            }
            finally
            {
                Marshal.DestroyStructure<StructForIUnknown>(ptr);
                Marshal.FreeHGlobal(ptr);
            }
        }        
    }
}
