//using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class MarshalComDisabledTests
    {
        // internal static Type? GetTypeFromCLSID(Guid clsid, string? server, bool throwOnError)
        // public static IntPtr CreateAggregatedObject(IntPtr pOuter, object o)
        // public static IntPtr CreateAggregatedObject<T>(IntPtr pOuter, T o) where T : notnull
        // public static int ReleaseComObject(object o)
        // public static int FinalReleaseComObject(object o)
        // public static object? GetComObjectData(object obj, object key)
        // public static bool SetComObjectData(object obj, object key, object? data)
        // public static object? CreateWrapperOfType(object? o, Type t)
        // public static TWrapper CreateWrapperOfType<T, TWrapper>(T? o)
        // public static void GetNativeVariantForObject(object? obj, /* VARIANT * */ IntPtr pDstNativeVariant)
        // public static void GetNativeVariantForObject<T>(T? obj, IntPtr pDstNativeVariant)
        // public static object? GetObjectForNativeVariant(/* VARIANT * */ IntPtr pSrcNativeVariant)
        // public static T? GetObjectForNativeVariant<T>(IntPtr pSrcNativeVariant)
        // public static object?[] GetObjectsForNativeVariants(/* VARIANT * */ IntPtr aSrcNativeVariant, int cVars)
        // public static T[] GetObjectsForNativeVariants<T>(IntPtr aSrcNativeVariant, int cVars)
        // public static object BindToMoniker(string monikerName)

        // public static IntPtr /* IUnknown* */ GetIUnknownForObject(object o)
        // public static IntPtr /* IDispatch */ GetIDispatchForObject(object o)
        // public static object GetObjectForIUnknown(IntPtr /* IUnknown* */ pUnk)

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypeFromCLSID_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetTypeFromCLSID(Guid.Empty));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CreateAggregatedObject_ThrowsNotSupportedException()
        {
            object value = new object();
            Assert.Throws<NotSupportedException>(() => Marshal.CreateAggregatedObject(IntPtr.Zero, value));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CreateAggregatedObject_T_ThrowsNotSupportedException()
        {
            object value = new object();
            Assert.Throws<NotSupportedException>(() => Marshal.CreateAggregatedObject<object>(IntPtr.Zero, value));
        }


        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ReleaseComObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.ReleaseComObject(new object()));
        }
        
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FinalReleaseComObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.FinalReleaseComObject(new object()));
        }        

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComObjectData_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetComObjectData("key", "value"));
        }        

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SetComObjectData_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.SetComObjectData(new object(), "key", "value"));
        }        

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CreateWrapperOfType_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.CreateWrapperOfType(new object(), typeof(object)));
        }        

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CreateWrapperOfType_T_TWrapper_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.CreateWrapperOfType<object, object>(new object()));
        }        

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariantForObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetNativeVariantForObject(99, IntPtr.Zero));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetNativeVariantForObject_T_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetNativeVariantForObject<double>(99, IntPtr.Zero));
        }


        public struct NativeVariant{}

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
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
        [PlatformSpecific(TestPlatforms.Windows)]
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
        [PlatformSpecific(TestPlatforms.Windows)]
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
        [PlatformSpecific(TestPlatforms.Windows)]
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
        [PlatformSpecific(TestPlatforms.Windows)]
        public void BindToMoniker_ThrowsNotSupportedException()
        {            
            Assert.Throws<NotSupportedException>(() => Marshal.BindToMoniker("test"));
        }        

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetIUnknownForObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetIUnknownForObject(new object()));
        }        

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetIDispatchForObject_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Marshal.GetIDispatchForObject(new object()));
        }        

        public struct StructForIUnknown{}

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
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
