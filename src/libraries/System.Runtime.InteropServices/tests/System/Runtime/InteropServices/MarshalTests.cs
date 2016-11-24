// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Security;

namespace System.Runtime.InteropServices
{
    public static class MarshalTests
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
        [PlatformSpecific(TestPlatforms.Windows)]
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

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Net46)]
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.Net46)]
        public static void GenerateProgIdForType()
        {
             Assert.Throws<ArgumentNullException>(() => Marshal.GenerateProgIdForType(null));
             Assert.Equal("TestProgID", Marshal.GenerateProgIdForType(typeof(ClassWithProgID)));       
        }        

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Net46)]
        public static void GetComObjectData()
        {
             Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetComObjectData(null, null));        
        }        

        [Fact]
        public static void GetHINSTANCE()
        {
             Assert.Throws<ArgumentNullException>(() => Marshal.GetHINSTANCE(null));
             IntPtr ptr = Marshal.GetHINSTANCE(typeof(int).Module);
             IntPtr ptr2 = Marshal.GetHINSTANCE(typeof(string).Module);
             Assert.Equal(ptr, ptr2);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Net46)]
        public static void GetIDispatchForObject()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetIDispatchForObject(null));   
        }
                
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Net46)]
        public static void GetTypedObjectForIUnknown()
        {
            if(PlatformDetection.IsWindows)
            {
                Assert.Throws<ArgumentNullException>(() => Marshal.GetTypedObjectForIUnknown(IntPtr.Zero, typeof(int)));
            }  
            else 
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetTypedObjectForIUnknown(IntPtr.Zero, typeof(int)));
            }
        }

        [Fact]
        public static void Prelink()
        {
            Assert.Throws<ArgumentNullException>(() => Marshal.Prelink(null));
        }

        [Fact]
        public static void PrelinkAll()
        {
            Assert.Throws<ArgumentNullException>(() => Marshal.PrelinkAll(null));
        }
        
        [Fact]
        public static void PtrToStringAuto()
        {
            Assert.Null(Marshal.PtrToStringAuto(IntPtr.Zero));
        }

        [Fact]
        public static void PtrToStringAutoWithLength()
        {
            Assert.Throws<ArgumentNullException>(() => Marshal.PtrToStringAuto(IntPtr.Zero, 0));

            String s = "Hello World";
            int len = 5;
            IntPtr ptr = Marshal.StringToCoTaskMemAuto(s);

            
            String s2 = Marshal.PtrToStringAuto(ptr, len);
            Assert.Equal(s.Substring(0, len), s2);

            Marshal.FreeCoTaskMem(ptr);  
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Net46)]
        public static void SetComObjectData()
        {
             Assert.Throws<PlatformNotSupportedException>(() => Marshal.SetComObjectData(null, null, null));        
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.Net46)]
        public static void BindToMoniker()
        {
            String monikerName = null;
            if(PlatformDetection.IsWindows)
            {
                Assert.Throws<ArgumentException>(() => Marshal.BindToMoniker(monikerName));
            }  
            else 
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.BindToMoniker(monikerName));
            }        
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Net46)]
        public static void ChangeWrapperHandleStrength() 
        {
            if(PlatformDetection.IsWindows)
            {
                Assert.Throws<ArgumentNullException>(() => Marshal.ChangeWrapperHandleStrength(null, true));
            }  
            else 
            {
                Assert.Throws<PlatformNotSupportedException>(() => Marshal.ChangeWrapperHandleStrength(null, true));
            }        
        }    
        
    }
}
