// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Security;

namespace System.Runtime.InteropServices
{
    public partial class MarshalTests
    {
        public static readonly object[][] StringData =
        {
            new object[] { "pizza" },
            new object[] { "pepperoni" },
            new object[] { "password" },
            new object[] { "P4ssw0rdAa1!" },
        };

        private static SecureString ToSecureString(string data)
        {
            var str = new SecureString();
            foreach (char c in data)
            {
                str.AppendChar(c);
            }
            str.MakeReadOnly();
            return str;
        }

        [Theory]
        [MemberData(nameof(StringData))]
        [PlatformSpecific(TestPlatforms.Windows)]  // SecureStringToBSTR not supported on Unix
        public static void SecureStringToBSTR(string data)
        {
            using (SecureString str = ToSecureString(data))
            {
                IntPtr bstr = Marshal.SecureStringToBSTR(str);
                try
                {
                    string actual = Marshal.PtrToStringBSTR(bstr);
                    Assert.Equal(data, actual);
                }
                finally
                {
                    Marshal.ZeroFreeBSTR(bstr);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StringData))]
        public static void SecureStringToCoTaskMemAnsi(string data)
        {
            using (var str = ToSecureString(data))
            {
                IntPtr ptr = Marshal.SecureStringToCoTaskMemAnsi(str);
                try
                {
                    string actual = Marshal.PtrToStringAnsi(ptr);
                    Assert.Equal(data, actual);
                }
                finally
                {
                    Marshal.ZeroFreeCoTaskMemAnsi(ptr);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StringData))]
        public static void SecureStringToCoTaskMemUnicode(string data)
        {
            using (var str = ToSecureString(data))
            {
                IntPtr ptr = Marshal.SecureStringToCoTaskMemUnicode(str);
                try
                {
                    string actual = Marshal.PtrToStringUni(ptr);
                    Assert.Equal(data, actual);
                }
                finally
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(ptr);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StringData))]
        public static void SecureStringToGlobalAllocAnsi(string data)
        {
            using (var str = ToSecureString(data))
            {
                IntPtr ptr = Marshal.SecureStringToGlobalAllocAnsi(str);
                try
                {
                    string actual = Marshal.PtrToStringAnsi(ptr);
                    Assert.Equal(data, actual);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocAnsi(ptr);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StringData))]
        public static void SecureStringToGlobalAllocUnicode(string data)
        {
            using (var str = ToSecureString(data))
            {
                IntPtr ptr = Marshal.SecureStringToGlobalAllocUnicode(str);
                try
                {
                    string actual = Marshal.PtrToStringUni(ptr);
                    Assert.Equal(data, actual);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(ptr);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void AllocHGlobal_Int32_ReadableWritable(int size)
        {
            IntPtr p = Marshal.AllocHGlobal(size);
            Assert.NotEqual(IntPtr.Zero, p);
            try
            {
                WriteBytes(p, size);
                VerifyBytes(p, size);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void AllocHGlobal_IntPtr_ReadableWritable(int size)
        {
            IntPtr p = Marshal.AllocHGlobal((IntPtr)size);
            Assert.NotEqual(IntPtr.Zero, p);
            try
            {
                WriteBytes(p, size);
                VerifyBytes(p, size);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        [Fact]
        public static void ReAllocHGlobal_DataCopied()
        {
            const int Size = 3;
            IntPtr p1 = Marshal.AllocHGlobal((IntPtr)Size);
            IntPtr p2 = p1;
            try
            {
                WriteBytes(p1, Size);
                int add = 1;
                do
                {
                    p2 = Marshal.ReAllocHGlobal(p2, (IntPtr)(Size + add));
                    VerifyBytes(p2, Size);
                    add++;
                }
                while (p2 == p1); // stop once we've validated moved case
            }
            finally
            {
                Marshal.FreeHGlobal(p2);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        public static void AllocCoTaskMem_Int32_ReadableWritable(int size)
        {
            IntPtr p = Marshal.AllocCoTaskMem(size);
            Assert.NotEqual(IntPtr.Zero, p);
            try
            {
                WriteBytes(p, size);
                VerifyBytes(p, size);
            }
            finally
            {
                Marshal.FreeCoTaskMem(p);
            }
        }

        [Fact]
        public static void ReAllocCoTaskMem_DataCopied()
        {
            const int Size = 3;
            IntPtr p1 = Marshal.AllocCoTaskMem(Size);
            IntPtr p2 = p1;
            try
            {
                WriteBytes(p1, Size);
                int add = 1;
                do
                {
                    p2 = Marshal.ReAllocCoTaskMem(p2, Size + add);
                    VerifyBytes(p2, Size);
                    add++;
                }
                while (p2 == p1); // stop once we've validated moved case
            }
            finally
            {
                Marshal.FreeCoTaskMem(p2);
            }
        }

        private static void WriteBytes(IntPtr p, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Marshal.WriteByte(p + i, (byte)i);
            }
        }

        private static void VerifyBytes(IntPtr p, int length)
        {
            for (int i = 0; i < length; i++)
            {
                Assert.Equal((byte)i, Marshal.ReadByte(p + i));
            }
        }

        [Fact]
        public static void ValidateExceptionForHRAPI()
        {
            int errorCode = -2146231029;
            COMException getHRException = Marshal.GetExceptionForHR(errorCode) as COMException;
            Assert.NotNull(getHRException);
            Assert.Equal(errorCode, getHRException.HResult);
            try
            {
                Marshal.ThrowExceptionForHR(errorCode);
            }
            catch (COMException e)
            {             
                Assert.Equal(errorCode, e.HResult);
                Assert.Equal(e.HResult, getHRException.HResult);
            }
        }

        [Fact]
        public static void GetHRForException()
        {
            Exception e = new Exception();

            try
            {
                Assert.Equal(0, Marshal.GetHRForException(null));
                
                Assert.InRange(Marshal.GetHRForException(e), int.MinValue, -1);
                Assert.Equal(e.HResult, Marshal.GetHRForException(e));
            }
            finally
            {
                // This GetExceptionForHR call is needed to 'eat' the IErrorInfo put to TLS by 
                // Marshal.GetHRForException call above. Otherwise other Marshal.GetExceptionForHR 
                // calls would randomly return previous exception objects passed to 
                // Marshal.GetHRForException.
                // The correct way is to use Marshal.GetHRForException at interop boundary only, but for the
                // time being we'll keep this code as-is.
                Marshal.GetExceptionForHR(e.HResult);
            }
        }

#if netcoreapp
        [Fact]
        public static void GenerateGuidForType()
        {
            Assert.Equal(typeof(int).GUID, Marshal.GenerateGuidForType(typeof(int)));
            Assert.Equal(typeof(string).GUID, Marshal.GenerateGuidForType(typeof(string)));
        }

        [ProgId("TestProgID")]
        public class ClassWithProgID
        {
        }

        [Fact]
        public static void GenerateProgIdForType()
        {
             AssertExtensions.Throws<ArgumentNullException>("type", () => Marshal.GenerateProgIdForType(null));
             Assert.Equal("TestProgID", Marshal.GenerateProgIdForType(typeof(ClassWithProgID)));       
        }        

        [Fact]
        public static void GetComObjectData()
        {
             Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetComObjectData(null, null));        
        }        

        [Fact]
        public static void GetHINSTANCE()
        {
            AssertExtensions.Throws<ArgumentNullException>("m", () => Marshal.GetHINSTANCE(null));
            IntPtr ptr = Marshal.GetHINSTANCE(typeof(int).Module);
            IntPtr ptr2 = Marshal.GetHINSTANCE(typeof(string).Module);
            Assert.Equal(ptr, ptr2);
        }

        [Fact]
        public static void GetIDispatchForObject()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIDispatchForObject(null));   
        }
                
        [Fact]
        public static void GetTypedObjectForIUnknown()
        {
            if(PlatformDetection.IsWindows)
            {
                AssertExtensions.Throws<ArgumentNullException>("pUnk", () => Marshal.GetTypedObjectForIUnknown(IntPtr.Zero, typeof(int)));
            }  
            else 
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetTypedObjectForIUnknown(IntPtr.Zero, typeof(int)));
            }
        }

        [Fact]
        public static void SetComObjectData()
        {
             Assert.Throws<PlatformNotSupportedException>(() => Marshal.SetComObjectData(null, null, null));        
        }

        [Theory]
        [MemberData(nameof(Encode_TestData))]
        public static void TestUTF8Marshalling(string chars, byte[] expected)
        {
            IntPtr pString = IntPtr.Zero;
            try
            {
                pString = Marshal.StringToCoTaskMemUTF8(chars);
                string utf8String = Marshal.PtrToStringUTF8(pString);
                Assert.Equal(chars, utf8String);
            }
            finally
            {
                if (pString != IntPtr.Zero)
                    Marshal.ZeroFreeCoTaskMemUTF8(pString);
            }
        }

        public static System.Collections.Generic.IEnumerable<object[]> Encode_TestData()
        {
            yield return new object[] { "FooBA\u0400R", new byte[] { 70, 111, 111, 66, 65, 208, 128, 82 } };

            yield return new object[] { "\u00C0nima\u0300l", new byte[] { 195, 128, 110, 105, 109, 97, 204, 128, 108 } };

            yield return new object[] { "Test\uD803\uDD75Test", new byte[] { 84, 101, 115, 116, 240, 144, 181, 181, 84, 101, 115, 116 } };

            yield return new object[] { "\u0130", new byte[] { 196, 176 } };

            yield return new object[] { "\uD803\uDD75\uD803\uDD75\uD803\uDD75", new byte[] { 240, 144, 181, 181, 240, 144, 181, 181, 240, 144, 181, 181 } };

            yield return new object[] { "za\u0306\u01FD\u03B2\uD8FF\uDCFF", new byte[] { 122, 97, 204, 134, 199, 189, 206, 178, 241, 143, 179, 191 } };

            yield return new object[] { "za\u0306\u01FD\u03B2\uD8FF\uDCFF", new byte[] { 206, 178, 241, 143, 179, 191 } };

            yield return new object[] { "\u0023\u0025\u03a0\u03a3", new byte[] { 37, 206, 160 } };

            yield return new object[] { "\u00C5", new byte[] { 0xC3, 0x85 } };

            yield return new object[] { "\u0065\u0065\u00E1\u0065\u0065\u8000\u00E1\u0065\uD800\uDC00\u8000\u00E1\u0065\u0065\u0065", new byte[] { 0x65, 0x65, 0xC3, 0xA1, 0x65, 0x65, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0x65, 0xF0, 0x90, 0x80, 0x80, 0xE8, 0x80, 0x80, 0xC3, 0xA1, 0x65, 0x65, 0x65 } };

            yield return new object[] { "\u00A4\u00D0aR|{AnGe\u00A3\u00A4", new byte[] { 0xC2, 0xA4, 0xC3, 0x90, 0x61, 0x52, 0x7C, 0x7B, 0x41, 0x6E, 0x47, 0x65, 0xC2, 0xA3, 0xC2, 0xA4 } };

            yield return new object[] { "\uD800\uDC00\uD800\uDC00\uD800\uDC00", new byte[] { 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80, 0xF0, 0x90, 0x80, 0x80 } };

            yield return new object[] { "\uD800\uDC00\u0065\uD800\uDC00", new byte[] { 0xF0, 0x90, 0x80, 0x80, 0x65, 0xF0, 0x90, 0x80, 0x80 } };

            yield return new object[] { string.Empty, new byte[] { } };
            
            // bug fixed in coreclr
            //yield return new object[] { null, new byte[] {}};
        }

#endif // netcoreapp

        [Fact]
        public static void Prelink()
        {
            AssertExtensions.Throws<ArgumentNullException>("m", () => Marshal.Prelink(null));
        }

        [Fact]
        public static void PrelinkAll()
        {
            AssertExtensions.Throws<ArgumentNullException>("c", () => Marshal.PrelinkAll(null));
        }
        
        [Fact]
        public static void PtrToStringAuto()
        {
            Assert.Null(Marshal.PtrToStringAuto(IntPtr.Zero));
        }

        [Fact]
        public static void StringToCoTaskMemAuto_PtrToStringAuto_ReturnsExpected()
        {
            string s = "Hello World";
            int len = 5;
            IntPtr ptr = Marshal.StringToCoTaskMemAuto(s);
            try
            {
                string actual = Marshal.PtrToStringAuto(ptr, len);
                Assert.Equal(s.Substring(0, len), actual);
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void PtrToStringAuto_ZeroPtr_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.PtrToStringAuto(IntPtr.Zero, 0));
        }

        [Fact]
        public static void StringToCoTaskMemAuto()
        {
            String s = null;

            // passing null string should return 0
            Assert.Equal(0, (long)Marshal.StringToCoTaskMemAuto(s));        
            
            s = "Hello World";
            IntPtr ptr = Marshal.StringToCoTaskMemAuto(s);

            // make sure the native memory is correctly laid out
            for (int i=0; i < s.Length; i++)
            {
                char c = (char)Marshal.ReadInt16(IntPtr.Add(ptr, i<<1));
                Assert.Equal(s[i], c);    
            }

            // make sure if we convert back to string we get the same value
            String s2 = Marshal.PtrToStringAuto(ptr);
            Assert.Equal(s, s2);

            // free the native memory
            Marshal.FreeCoTaskMem(ptr);  
        }

        [Fact]
        public static void StringToHGlobalAuto()
        {
            String s = null;

            // passing null string should return 0
            Assert.Equal(0, (long)Marshal.StringToHGlobalAuto(s));        
            
            s = "Hello World";
            IntPtr ptr = Marshal.StringToHGlobalAuto(s);

            // make sure the native memory is correctly laid out
            for (int i=0; i < s.Length; i++)
            {
                char c = (char)Marshal.ReadInt16(IntPtr.Add(ptr, i<<1));
                Assert.Equal(s[i], c);    
            }
            
            // make sure if we convert back to string we get the same value
            String s2 = Marshal.PtrToStringAuto(ptr);
            Assert.Equal(s, s2);

            // free the native memory
            Marshal.FreeCoTaskMem(ptr);                          
        }
        
        [Fact]        
        public static void BindToMoniker()
        {
            if(PlatformDetection.IsWindows && !PlatformDetection.IsNetNative)
            {
                if (PlatformDetection.IsNotWindowsNanoServer)
                {
                    AssertExtensions.Throws<ArgumentException>(null, () => Marshal.BindToMoniker(null));
                }
            }  
            else 
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.BindToMoniker(null));
            }        
        }

        [Fact]        
        public static void ChangeWrapperHandleStrength() 
        {
            if(PlatformDetection.IsWindows && !PlatformDetection.IsNetNative)
            {
                AssertExtensions.Throws<ArgumentNullException>("otp", () => Marshal.ChangeWrapperHandleStrength(null, true));
            }  
            else 
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.ChangeWrapperHandleStrength(null, true));
            }        
        }    
        
        [Theory]
        [InlineData(0)]
        [InlineData("String")]
        public void IsComObject_NonComObject_ReturnsFalse(object value)
        {
            Assert.False(Marshal.IsComObject(value));
        }

        [Fact]
        public void IsComObject_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.IsComObject(null));
        }
    }
}
