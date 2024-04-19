// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;
using static TestLibrary.Utilities;

namespace System.Runtime.InteropServices.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public class ICustomMarshalerTests
    {
        // To avoid having to create a native test library to reference in tests that
        // interact with native libraries, we can use a simple method from the C standard
        // library. Unfortunately, the C standard library has different names on Windows
        // vs Unix.
#if Windows
        public const string LibcLibrary = "msvcrt.dll";
#else
        public const string LibcLibrary = "libc";
#endif

        public static void CustomMarshaler_StringType_Success()
        {
            int val = 64001;
            Assert.Equal(val, MarshalerOnStringTypeMethod(val.ToString()));
        }

        public class StringForwardingCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { Marshal.ZeroFreeCoTaskMemAnsi(pNativeData); }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi((string)ManagedObj);
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie) => new StringForwardingCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi")]
        public static extern int MarshalerOnStringTypeMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StringForwardingCustomMarshaler))] string str);

        public static void CustomMarshaler_ArrayType_Success()
        {
            int val = 64001;
            Assert.Equal(val, MarshalerOnArrayTypeMethod(new string[] { val.ToString() }));
        }

        public class ArrayForwardingCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { Marshal.ZeroFreeCoTaskMemAnsi(pNativeData); }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi(((string[])ManagedObj)[0]);
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie) => new ArrayForwardingCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi")]
        public static extern int MarshalerOnArrayTypeMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.Tests.ICustomMarshalerTests+ArrayForwardingCustomMarshaler")] string[] str);

        public static void CustomMarshaler_BoxedValueType_Success()
        {
            int val = 64001;
            Assert.Equal(val * 2, MarshalerOnBoxedValueTypeMethod(val));
        }

        public class BoxedValueTypeCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { Marshal.ZeroFreeCoTaskMemAnsi(pNativeData); }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj)
            {
                int unboxedValueType = (int)ManagedObj * 2;
                return Marshal.StringToCoTaskMemAnsi(unboxedValueType.ToString());
            }

            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie) => new BoxedValueTypeCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi")]
        public static extern int MarshalerOnBoxedValueTypeMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(BoxedValueTypeCustomMarshaler))] object i);

        public static void Parameter_CustomMarshalerProvidedOnClassType_ForwardsCorrectly()
        {
            int val = 64001;
            Assert.Equal((val * 2).ToString(), MarshalerOnClassTypeMethod(new StringContainer { Value = val.ToString() }).Value);
        }

        public class StringContainer
        {
            public string Value { get; set; }
        }

        public class ClassForwardingCustomMarshaler : ICustomMarshaler
        {
            private bool CleanedString { get; set; }

            public void CleanUpManagedData(object ManagedObj) {}

            public void CleanUpNativeData(IntPtr pNativeData)
            {
                if (CleanedString)
                {
                    return;
                }

                Marshal.ZeroFreeCoTaskMemAnsi(pNativeData);
                CleanedString = true;
            }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj)
            {
                return Marshal.StringToCoTaskMemAnsi(((StringContainer)ManagedObj).Value);
            }

            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                int doubleValue = pNativeData.ToInt32() * 2;
                return new StringContainer { Value = doubleValue.ToString() };
            }

            public static ICustomMarshaler GetInstance(string cookie) => new ClassForwardingCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ClassForwardingCustomMarshaler))]
        public static extern StringContainer MarshalerOnClassTypeMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ClassForwardingCustomMarshaler))] StringContainer str);

        public static void Parameter_CustomMarshalerProvided_CallsMethodsInCorrectOrdering()
        {
            Assert.Empty(OrderTrackingCustomMarshaler.Events);

            string val1 = "64001";
            Assert.Equal(val1, OrderTrackingMethod(val1));

            string[] expectedOrderingFirstCall = new string[]
            {
                "Called GetInstance",
                "Called MarshalManagedToNative",
                "Called MarshalNativeToManaged",
                "Called CleanUpNativeData"
            };
            Assert.Equal(expectedOrderingFirstCall, OrderTrackingCustomMarshaler.Events);

            // GetInstance is only called once.
            string val2 = "234";
            Assert.Equal(val2, OrderTrackingMethod(val2));
            IEnumerable<string> expectedOrderingSecondCall = expectedOrderingFirstCall.Concat(new string[]
            {
                "Called MarshalManagedToNative",
                "Called MarshalNativeToManaged",
                "Called CleanUpNativeData"
            });
            Assert.Equal(expectedOrderingSecondCall, OrderTrackingCustomMarshaler.Events);

            // GetInstance is only called once.
            string val3 = "7488";
            Assert.Equal(7488, OrderTrackingMethodRef(ref val3));
            IEnumerable<string> expectedOrderingThirdCall = expectedOrderingSecondCall.Concat(new string[]
            {
                "Called MarshalManagedToNative",
                "Called CleanUpManagedData",
                "Called MarshalNativeToManaged",
                "Called CleanUpNativeData",
            });
            Assert.Equal(expectedOrderingThirdCall.Skip(7), OrderTrackingCustomMarshaler.Events.Skip(7));

            OrderTrackingMethodOut(out var val4);
            Assert.Equal("2334", val4);
            IEnumerable<string> expectedOrderingForthCall = expectedOrderingThirdCall.Concat(new string[]
            {
                "Called MarshalNativeToManaged",
            });
            Assert.Equal(expectedOrderingForthCall.Skip(11), OrderTrackingCustomMarshaler.Events.Skip(11));

            var val5 = OrderTrackingMethodDelegate(439, (x) => x.ToString());
            Assert.Equal("439", val5);
            IEnumerable<string> expectedOrderingFifthCall = expectedOrderingForthCall.Concat(new string[]
            {
                "Called MarshalManagedToNative",
                "Called CleanUpManagedData",
                "Called MarshalNativeToManaged",
            });
            Assert.Equal(expectedOrderingFifthCall.Skip(12), OrderTrackingCustomMarshaler.Events.Skip(12));

            var val6 = OrderTrackingMethodReturn("726");
            Assert.Equal("726", val6);
            IEnumerable<string> expectedOrderingSixthCall = expectedOrderingFifthCall.Concat(new string[]
            {
                "Called MarshalNativeToManaged",
            });
            Assert.Equal(expectedOrderingSixthCall.Skip(15), OrderTrackingCustomMarshaler.Events.Skip(15));
        }

        // This should only be used *once*, as it uses static state.
        public class OrderTrackingCustomMarshaler : ICustomMarshaler
        {
            public static List<string> Events { get; } = new List<string>();
            public static IntPtr MarshaledNativeData { get; set; }

            public void CleanUpManagedData(object ManagedObj)
            {
                Events.Add("Called CleanUpManagedData");
            }

            public void CleanUpNativeData(IntPtr pNativeData)
            {
                Assert.Equal(MarshaledNativeData, pNativeData);
                Marshal.ZeroFreeCoTaskMemAnsi(pNativeData);

                Events.Add("Called CleanUpNativeData");
            }

            public int GetNativeDataSize()
            {
                Events.Add("Called GetNativeDataSize");
                return 0;
            }

            public IntPtr MarshalManagedToNative(object ManagedObj)
            {
                Events.Add("Called MarshalManagedToNative");
                MarshaledNativeData = Marshal.StringToCoTaskMemAnsi((string)ManagedObj);
                return MarshaledNativeData;
            }

            public object MarshalNativeToManaged(IntPtr pNativeData)
            {
                Events.Add("Called MarshalNativeToManaged");
                return pNativeData.ToInt64().ToString();
            }

            public static ICustomMarshaler GetInstance(string cookie)
            {
                Assert.Empty(cookie);
                Events.Add("Called GetInstance");
                return new OrderTrackingCustomMarshaler();
            }
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OrderTrackingCustomMarshaler))]
        public static extern string OrderTrackingMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OrderTrackingCustomMarshaler))] string str);

        [DllImport("CustomMarshalersPrimitives", EntryPoint = "NativeParseIntRef")]
        public static extern int OrderTrackingMethodRef([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OrderTrackingCustomMarshaler))] ref string str);

        [DllImport("CustomMarshalersPrimitives", EntryPoint = "NativeParseIntOut")]
        public static extern void OrderTrackingMethodOut([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OrderTrackingCustomMarshaler))] out string str);

        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OrderTrackingCustomMarshaler))]
        public delegate string TestDelegate(int val);

        [DllImport("CustomMarshalersPrimitives", EntryPoint = "NativeParseIntDelegate")]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OrderTrackingCustomMarshaler))]
        public static extern string OrderTrackingMethodDelegate(int val, TestDelegate dlg);

        [DllImport("CustomMarshalersPrimitives", EntryPoint = "NativeParseInt")]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OrderTrackingCustomMarshaler))]
        public static extern string OrderTrackingMethodReturn([MarshalAs(UnmanagedType.LPStr)] string str);

        public static void CustomMarshaler_BothMarshalTypeRefAndMarshalTypeProvided_PicksMarshalType()
        {
            Assert.Equal(2, BothTypeRefAndTypeMethod("64001"));
        }

        public class OverridingCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { Marshal.ZeroFreeCoTaskMemAnsi(pNativeData); }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi("2");
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie) => new OverridingCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int BothTypeRefAndTypeMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.Tests.ICustomMarshalerTests+OverridingCustomMarshaler", MarshalTypeRef = typeof(StringForwardingCustomMarshaler))] string str);

        public static void Parameter_CookieProvided_PassesCookieToGetInstance()
        {
            int val = 64001;
            Assert.Equal(val, CustomCookieMethod(val.ToString()));
            Assert.Equal("Cookie", CookieTrackingCustomMarshaler.Cookie);
        }

        public class CookieTrackingCustomMarshaler : ICustomMarshaler
        {
            public static string Cookie { get; set; }

            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { Marshal.ZeroFreeCoTaskMemAnsi(pNativeData); }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi((string)ManagedObj);
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie)
            {
                Cookie = cookie;
                return new CookieTrackingCustomMarshaler();
            }
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int CustomCookieMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CookieTrackingCustomMarshaler), MarshalCookie = "Cookie")] string str);

        public static void Parameter_NotCustomMarshalerType_UsesSpecifiedMarshaler()
        {
            int val = 64001;
            Assert.Equal(val, NonCustomMarshalerTypeMethod(val.ToString()));
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NonCustomMarshalerTypeMethod([MarshalAs(UnmanagedType.LPStr, MarshalTypeRef = typeof(OverridingCustomMarshaler))] string str);

        public static void CustomMarshaler_Generic_Success()
        {
            Assert.Equal(234, GenericGetInstanceCustomMarshalerMethod("64001"));
        }

        public class GenericCustomMarshaler<T> : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { Marshal.ZeroFreeCoTaskMemAnsi(pNativeData); }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi("234");
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie)
            {
                return new GenericCustomMarshaler<int>();
            }
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GenericGetInstanceCustomMarshalerMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(GenericCustomMarshaler<int>))] string str);

        public static void CustomMarshaler_ValueTypeWithStringType_Success()
        {
            Assert.Equal(234, ValueTypeMarshalerOnStringTypeMethod("64001"));
        }

        public struct CustomMarshalerValueType : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { Marshal.ZeroFreeCoTaskMemAnsi(pNativeData); }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi("234");
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie)
            {
                return new CustomMarshalerValueType();
            }
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ValueTypeMarshalerOnStringTypeMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CustomMarshalerValueType))] string str);

        public static void Parameter_MarshalerOnValueType_ThrowsMarshalDirectiveException()
        {
            Assert.Throws<MarshalDirectiveException>(() => MarshalerOnValueTypeMethod(0));
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int MarshalerOnValueTypeMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StringForwardingCustomMarshaler))] int str);

        public static unsafe void Parameter_MarshalerOnPointer_ThrowsMarshalDirectiveException()
        {
            Assert.Throws<MarshalDirectiveException>(() => MarshalerOnPointerMethod(null));
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern int MarshalerOnPointerMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StringForwardingCustomMarshaler))] int* str);

        public static void Parameter_NullICustomMarshaler_ThrowsTypeLoadException()
        {
            Assert.Throws<TypeLoadException>(() => NullCustomMarshalerMethod(""));
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NullCustomMarshalerMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = null)] string str);

        public static void Parameter_InvalidTypeICustomMarshaler_TypeLoadException()
        {
            Assert.Throws<TypeLoadException>(() => InvalidTypeCustomMarshalerMethod(""));
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int InvalidTypeCustomMarshalerMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "junk_type")] string str);

        public static void Parameter_NotICustomMarshaler_ThrowsApplicationException()
        {
            Assert.Throws<ApplicationException>(() => NonICustomMarshalerMethod(""));
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NonICustomMarshalerMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(string))] string str);

        public static void Parameter_OpenGenericICustomMarshaler_ThrowsTypeLoadException()
        {
            Assert.Throws<TypeLoadException>(() => OpenGenericICustomMarshalerMethod(""));
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int OpenGenericICustomMarshalerMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(GenericCustomMarshaler<>))] string str);

        public static void Parameter_GetInstanceMethodDoesntExist_ThrowsApplicationException()
        {
            Assert.Throws<ApplicationException>(() => NoGetInstanceMethod(""));
        }

        public class NoGetInstanceCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => IntPtr.Zero;
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NoGetInstanceMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NoGetInstanceCustomMarshaler))] string str);

        public static void Parameter_GetInstanceMethodInstanceMethod_ThrowsApplicationException()
        {
            Assert.Throws<ApplicationException>(() => InstanceGetInstanceMethod(""));
        }

        public class InstanceGetInstanceCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => IntPtr.Zero;
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;
            public ICustomMarshaler GetInstance(string cookie) => new InstanceGetInstanceCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int InstanceGetInstanceMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(InstanceGetInstanceCustomMarshaler))] string str);

        public static void Parameter_GetInstanceMethodNoParameters_ThrowsApplicationException()
        {
            Assert.Throws<ApplicationException>(() => NoParametersGetInstanceMethod(""));
        }

        public class NoParameterGetInstanceCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => IntPtr.Zero;
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance() => new NoParameterGetInstanceCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NoParametersGetInstanceMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NoParameterGetInstanceCustomMarshaler))] string str);

        public static void Parameter_GetInstanceMethodNonStringParameter_ThrowsApplicationException()
        {
            Assert.Throws<ApplicationException>(() => NonStringGetInstanceMethod(""));
        }

        public class NonStringGetInstanceCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => IntPtr.Zero;
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(int x) => new NonStringGetInstanceCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NonStringGetInstanceMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NonStringGetInstanceCustomMarshaler))] string str);

        public static void Parameter_GetInstanceMethodReturnsVoid_ThrowsApplicationException()
        {
            Assert.Throws<ApplicationException>(() => VoidGetInstanceMethod(""));
        }

        public class VoidGetInstanceCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => IntPtr.Zero;
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static void GetInstance(string cookie) { }
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int VoidGetInstanceMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(VoidGetInstanceCustomMarshaler))] string str);

        public static void Parameter_GetInstanceMethodReturnsNull_ThrowsApplicationException()
        {
            Assert.Throws<ApplicationException>(() => NullGetInstanceMethod(""));
        }

        public class NullGetInstanceCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => IntPtr.Zero;
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie) => null;
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int NullGetInstanceMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NullGetInstanceCustomMarshaler))] string str);

        public static void Parameter_GetInstanceMethodThrows_ThrowsActualException()
        {
            Assert.Throws<NotImplementedException>(() => ThrowingGetInstanceMethod(""));
        }

        public class ThrowingGetInstanceCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => IntPtr.Zero;
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie) => throw new NotImplementedException();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ThrowingGetInstanceMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ThrowingGetInstanceCustomMarshaler))] string str);

        public static void Parameter_MarshalManagedToNativeThrows_ThrowsActualException()
        {
            Assert.Throws<NotImplementedException>(() => ThrowingMarshalManagedToNativeMethod(""));
        }

        public class ThrowingMarshalManagedToNativeCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) { }

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => throw new NotImplementedException();
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie) => new ThrowingMarshalManagedToNativeCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ThrowingMarshalManagedToNativeMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ThrowingMarshalManagedToNativeCustomMarshaler))] string str);

        public static void Parameter_CleanUpNativeDataMethodThrows_ThrowsActualException()
        {
            Assert.Throws<NotImplementedException>(() => ThrowingCleanUpNativeDataMethod(""));
        }

        public class ThrowingCleanUpNativeDataCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) { }
            public void CleanUpNativeData(IntPtr pNativeData) => throw new NotImplementedException();

            public int GetNativeDataSize() => IntPtr.Size;

            public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi((string)ManagedObj);
            public object MarshalNativeToManaged(IntPtr pNativeData) => null;

            public static ICustomMarshaler GetInstance(string cookie) => new ThrowingMarshalManagedToNativeCustomMarshaler();
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ThrowingCleanUpNativeDataMethod([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ThrowingCleanUpNativeDataCustomMarshaler))] string str);

        public static void Field_ParentIsStruct_ThrowsTypeLoadException()
        {
            Assert.Throws<TypeLoadException>(() => StructWithCustomMarshalerFieldMethod(new StructWithCustomMarshalerField()));
        }

        public struct StructWithCustomMarshalerField
        {
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(StringForwardingCustomMarshaler))]
            public string Field;
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int StructWithCustomMarshalerFieldMethod(StructWithCustomMarshalerField c);


        public static void Parameter_DifferentCustomMarshalerType_MarshalsCorrectly()
        {
            Assert.Equal(234, DifferentCustomMarshalerType("5678"));
        }

        public class OuterCustomMarshaler : ICustomMarshaler
        {
            public void CleanUpManagedData(object ManagedObj) => throw new NotImplementedException();
            public void CleanUpNativeData(IntPtr pNativeData) => throw new NotImplementedException();

            public int GetNativeDataSize() => throw new NotImplementedException();

            public IntPtr MarshalManagedToNative(object ManagedObj) => throw new NotImplementedException();
            public object MarshalNativeToManaged(IntPtr pNativeData) => throw new NotImplementedException();

            public static ICustomMarshaler GetInstance(string cookie) => new InnerCustomMarshaler();

            private interface ILargeInterface
            {
                void Method1();
                void Method2();
                void Method3();
                void Method4();
                void Method5();
                void Method6();
            }

            private class InnerCustomMarshaler : ILargeInterface, ICustomMarshaler
            {
                public void Method1() => throw new InvalidOperationException();
                public void Method2() => throw new InvalidOperationException();
                public void Method3() => throw new InvalidOperationException();
                public void Method4() => throw new InvalidOperationException();
                public void Method5() => throw new InvalidOperationException();
                public void Method6() => throw new InvalidOperationException();

                public void CleanUpManagedData(object ManagedObj) { }
                public void CleanUpNativeData(IntPtr pNativeData) => Marshal.FreeCoTaskMem(pNativeData);

                public int GetNativeDataSize() => IntPtr.Size;

                public IntPtr MarshalManagedToNative(object ManagedObj) => Marshal.StringToCoTaskMemAnsi("234");
                public object MarshalNativeToManaged(IntPtr pNativeData) => null;
            }
        }

        [DllImport(LibcLibrary, EntryPoint = "atoi", CallingConvention = CallingConvention.Cdecl)]
        public static extern int DifferentCustomMarshalerType([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(OuterCustomMarshaler))] string str);

        public delegate string TestDelegateRef([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(BoxedValueTypeCustomMarshaler))] ref int val);

        [DllImport("CustomMarshalersPrimitives", EntryPoint = "NativeParseIntDelegateRef")]
        public static extern string CustomMarshallerWithDelegateRef(int val, TestDelegateRef dlg);

        public static void DelegateParameter_MarshalerOnRefInt_ThrowsMarshalDirectiveException()
        {
            Assert.Throws<MarshalDirectiveException>(() => CustomMarshallerWithDelegateRef(84664, (ref int x) => x.ToString()));
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                CustomMarshaler_StringType_Success();
                CustomMarshaler_ArrayType_Success();
                CustomMarshaler_BoxedValueType_Success();
                Parameter_CustomMarshalerProvidedOnClassType_ForwardsCorrectly();
                Parameter_CustomMarshalerProvided_CallsMethodsInCorrectOrdering();
                CustomMarshaler_BothMarshalTypeRefAndMarshalTypeProvided_PicksMarshalType();
                Parameter_CookieProvided_PassesCookieToGetInstance();
                Parameter_NotCustomMarshalerType_UsesSpecifiedMarshaler();
                CustomMarshaler_Generic_Success();
                CustomMarshaler_ValueTypeWithStringType_Success();
                Parameter_MarshalerOnValueType_ThrowsMarshalDirectiveException();
                Parameter_MarshalerOnPointer_ThrowsMarshalDirectiveException();
                Parameter_NullICustomMarshaler_ThrowsTypeLoadException();
                Parameter_InvalidTypeICustomMarshaler_TypeLoadException();
                Parameter_NotICustomMarshaler_ThrowsApplicationException();
                Parameter_OpenGenericICustomMarshaler_ThrowsTypeLoadException();
                Parameter_GetInstanceMethodDoesntExist_ThrowsApplicationException();
                Parameter_GetInstanceMethodInstanceMethod_ThrowsApplicationException();
                Parameter_GetInstanceMethodNoParameters_ThrowsApplicationException();
                Parameter_GetInstanceMethodNonStringParameter_ThrowsApplicationException();
                Parameter_GetInstanceMethodReturnsVoid_ThrowsApplicationException();
                Parameter_GetInstanceMethodReturnsNull_ThrowsApplicationException();
                Parameter_GetInstanceMethodThrows_ThrowsActualException();
                Parameter_MarshalManagedToNativeThrows_ThrowsActualException();
                Parameter_CleanUpNativeDataMethodThrows_ThrowsActualException();
                Field_ParentIsStruct_ThrowsTypeLoadException();
                Parameter_DifferentCustomMarshalerType_MarshalsCorrectly();
                if (SupportsExceptionInterop)
                {
                    // EH interop is not supported for NativeAOT.
                    DelegateParameter_MarshalerOnRefInt_ThrowsMarshalDirectiveException();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Test Failure: {e}");
                return 101;
            }

            return 100;
        }
    }
}
