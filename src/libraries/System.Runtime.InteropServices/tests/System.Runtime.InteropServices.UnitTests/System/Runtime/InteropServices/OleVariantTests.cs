// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.Tests.Common;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    // NanoServer doesn't have any of the OLE Automation stack available, so we can't run these tests there.
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
    public class OleVariantTests
    {
        [Fact]
        public void DefaultVariantIsEmpty()
        {
            Assert.Equal(VarEnum.VT_EMPTY, default(OleVariant).VarType);
        }

        [Fact]
        public void NullVariantIsNull()
        {
            Assert.Equal(VarEnum.VT_NULL, OleVariant.Null.VarType);
        }

        [Fact]
        public void Short()
        {
            OleVariant variant = OleVariant.Create<short>(42);
            Assert.Equal(VarEnum.VT_I2, variant.VarType);
            Assert.Equal(42, variant.As<short>());
            Assert.Equal(42, variant.GetRawDataRef<short>());
        }

        [Fact]
        public void Int4()
        {
            OleVariant variant = OleVariant.Create(42);
            Assert.Equal(VarEnum.VT_I4, variant.VarType);
            Assert.Equal(42, variant.As<int>());
            Assert.Equal(42, variant.GetRawDataRef<int>());
        }

        [Fact]
        public void Float()
        {
            OleVariant variant = OleVariant.Create(42.0f);
            Assert.Equal(VarEnum.VT_R4, variant.VarType);
            Assert.Equal(42, variant.As<float>());
            Assert.Equal(42, variant.GetRawDataRef<float>());
        }

        [Fact]
        public void Double()
        {
            OleVariant variant = OleVariant.Create(42.0);
            Assert.Equal(VarEnum.VT_R8, variant.VarType);
            Assert.Equal(42, variant.As<double>());
            Assert.Equal(42, variant.GetRawDataRef<double>());
        }

#pragma warning disable CS0618 // Type or member is obsolete
        [Fact]
        public void Currency()
        {
            OleVariant variant = OleVariant.Create(new CurrencyWrapper(42.0m));
            Assert.Equal(VarEnum.VT_CY, variant.VarType);
            Assert.Equal(42.0m, variant.As<CurrencyWrapper>().WrappedObject);
            Assert.Equal(decimal.ToOACurrency(42.0m), variant.GetRawDataRef<long>());
        }
#pragma warning restore CS0618 // Type or member is obsolete

        [Fact]
        public void Date()
        {
            OleVariant variant = OleVariant.Create(new DateTime(2020, 1, 1));
            Assert.Equal(VarEnum.VT_DATE, variant.VarType);
            Assert.Equal(new DateTime(2020, 1, 1), variant.As<DateTime>());
            Assert.Equal(new DateTime(2020, 1, 1).ToOADate(), variant.GetRawDataRef<double>());
        }

        [Fact]
        public void BStrWrapper()
        {
            using OleVariant variant = OleVariant.Create(new BStrWrapper("Foo"));
            Assert.Equal(VarEnum.VT_BSTR, variant.VarType);
            Assert.Equal("Foo", variant.As<BStrWrapper>().WrappedObject);
            Assert.Equal("Foo", Marshal.PtrToStringBSTR(variant.GetRawDataRef<IntPtr>()));
        }

        [Fact]
        public void BStr_String()
        {
            using OleVariant variant = OleVariant.Create("Foo");
            Assert.Equal(VarEnum.VT_BSTR, variant.VarType);
            Assert.Equal("Foo", variant.As<string>());
            Assert.Equal("Foo", Marshal.PtrToStringBSTR(variant.GetRawDataRef<IntPtr>()));
        }

        [Fact]
        public void BStr_String_Null()
        {
            using OleVariant variant = OleVariant.Create<string>(null);
            Assert.Equal(VarEnum.VT_BSTR, variant.VarType);
            Assert.Null(variant.As<string>());
            Assert.Equal(IntPtr.Zero, variant.GetRawDataRef<IntPtr>());
        }

#if WINDOWS
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void Dispatch_NotSupported()
        {
           DispatchWrapper wrapper = new(new IDispatchComObject());
           Assert.Throws<ArgumentException>("T", () => OleVariant.Create(wrapper));
        }
#endif

        [Fact]
        public void Error()
        {
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_ERROR, 1);
            Assert.Equal(VarEnum.VT_ERROR, variant.VarType);
            Assert.Equal(1, variant.GetRawDataRef<int>());
            Assert.Equal(1, variant.As<ErrorWrapper>().ErrorCode);
            Assert.Equal(1, variant.As<int>());
        }

        [Fact]
        public void VariantBoolTrue()
        {
            OleVariant trueVariant = OleVariant.Create(true);
            Assert.Equal(VarEnum.VT_BOOL, trueVariant.VarType);
            Assert.True(trueVariant.As<bool>());
            Assert.Equal(0, trueVariant.GetRawDataRef<short>());
        }

        [Fact]
        public void VariantBoolFalse()
        {
            OleVariant falseVariant = OleVariant.Create(false);
            Assert.Equal(VarEnum.VT_BOOL, falseVariant.VarType);
            Assert.False(falseVariant.As<bool>());
            Assert.Equal(-1, falseVariant.GetRawDataRef<short>());
        }

        [Fact]
        public void VTVariantNotSupported()
        {
            Assert.Throws<ArgumentException>("vt", () => OleVariant.CreateRaw(VarEnum.VT_VARIANT, 1));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void Unknown_NotSupported()
        {
            UnknownWrapper wrapper = new(new TestObject());
            Assert.Throws<ArgumentException>("T", () => OleVariant.Create(wrapper));
        }

        [ComImport]
        [Guid("9FBB5303-ED8B-448D-8174-571D03E1D947")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IUnknownInterface
        {
        }

        private sealed class TestObject : IUnknownInterface
        {
        }

        [Fact]
        public void Decimal()
        {
            OleVariant variant = OleVariant.Create(42.0m);
            Assert.Equal(VarEnum.VT_DECIMAL, variant.VarType);
            Assert.Equal(42.0m, variant.As<decimal>());
            Assert.Throws<ArgumentException>("T", () => variant.GetRawDataRef<decimal>());
        }

        [Fact]
        public void SByte()
        {
            OleVariant variant = OleVariant.Create<sbyte>(42);
            Assert.Equal(VarEnum.VT_I1, variant.VarType);
            Assert.Equal(42, variant.As<sbyte>());
            Assert.Equal(42, variant.GetRawDataRef<sbyte>());
        }

        [Fact]
        public void Byte()
        {
            OleVariant variant = OleVariant.Create<byte>(42);
            Assert.Equal(VarEnum.VT_UI1, variant.VarType);
            Assert.Equal(42, variant.As<byte>());
            Assert.Equal(42, variant.GetRawDataRef<byte>());
        }

        [Fact]
        public void UShort()
        {
            OleVariant variant = OleVariant.Create<ushort>(42);
            Assert.Equal(VarEnum.VT_UI2, variant.VarType);
            Assert.Equal(42, variant.As<ushort>());
            Assert.Equal(42, variant.GetRawDataRef<ushort>());
        }

        [Fact]
        public void UInt4()
        {
            OleVariant variant = OleVariant.Create<uint>(42);
            Assert.Equal(VarEnum.VT_UI4, variant.VarType);
            Assert.Equal(42u, variant.As<uint>());
            Assert.Equal(42u, variant.GetRawDataRef<uint>());
        }

        [Fact]
        public void Long()
        {
            OleVariant variant = OleVariant.Create<long>(42);
            Assert.Equal(VarEnum.VT_I8, variant.VarType);
            Assert.Equal(42, variant.As<long>());
            Assert.Equal(42, variant.GetRawDataRef<long>());
        }

        [Fact]
        public void ULong()
        {
            OleVariant variant = OleVariant.Create<ulong>(42);
            Assert.Equal(VarEnum.VT_UI8, variant.VarType);
            Assert.Equal(42ul, variant.As<ulong>());
            Assert.Equal(42ul, variant.GetRawDataRef<ulong>());
        }

        [Fact]
        public void Int_Raw()
        {
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_INT, 42);
            Assert.Equal(VarEnum.VT_INT, variant.VarType);
            Assert.Equal(42, variant.As<int>());
            Assert.Equal(42, variant.GetRawDataRef<int>());
        }

        [Fact]
        public void UInt()
        {
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_UINT, 42u);
            Assert.Equal(VarEnum.VT_UINT, variant.VarType);
            Assert.Equal(42u, variant.As<uint>());
            Assert.Equal(42u, variant.GetRawDataRef<uint>());
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Record
        {
            private IntPtr _typeDesc;
            private IntPtr _data;
        }

        [Fact]
        public void Record_Raw()
        {
            // We do not support record types in the opinionated Create method.
            Assert.Throws<ArgumentException>("T", () => OleVariant.Create(new Record()));
            // We support creating a record-based variant with the CreateRaw method.
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_RECORD, new Record());
            Assert.Equal(VarEnum.VT_RECORD, variant.VarType);
            Assert.Equal(default, variant.GetRawDataRef<Record>());
        }

        [Fact]
        public void LPStr_Raw()
        {
            string str = "Foo";
            using OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_LPSTR, Marshal.StringToCoTaskMemAnsi(str));
            Assert.Equal(VarEnum.VT_LPSTR, variant.VarType);
            Assert.Throws<InvalidOperationException>(variant.As<string>);
            Assert.Equal(str, Marshal.PtrToStringAnsi(variant.GetRawDataRef<IntPtr>()));
        }

        [Fact]
        public void LPWStr_Raw()
        {
            string str = "Foo";
            using OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_LPWSTR, Marshal.StringToCoTaskMemUni(str));
            Assert.Equal(VarEnum.VT_LPWSTR, variant.VarType);
            Assert.Throws<InvalidOperationException>(variant.As<string>);
            Assert.Equal(str, Marshal.PtrToStringUni(variant.GetRawDataRef<IntPtr>()));
        }

        [Fact]
        public void FileTime_Raw()
        {
            long fileTime = 1039348523;
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_FILETIME, fileTime);
            Assert.Equal(VarEnum.VT_FILETIME, variant.VarType);
            Assert.Throws<InvalidOperationException>(() => variant.As<long>());
            Assert.Equal(fileTime, variant.GetRawDataRef<long>());
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Blob
        {
            public int Length;
            public IntPtr Data;
        }

        [Fact]
        public void Blob_Raw()
        {
            Blob blob = new Blob { Length = 3, Data = Marshal.AllocCoTaskMem(25) };
            using OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_BLOB, blob);
            Assert.Equal(VarEnum.VT_BLOB, variant.VarType);
            Assert.Throws<ArgumentException>("T", () => variant.As<Blob>());
            Assert.Equal(blob, variant.GetRawDataRef<Blob>());
        }

        [Fact]
        public void Stream_Raw()
        {
            IntPtr nativeStream = 42;
            // Using a fake value so we aren't disposing the OleVariant instance.
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_STREAM, nativeStream);
            Assert.Equal(VarEnum.VT_STREAM, variant.VarType);
            Assert.Equal(nativeStream, variant.GetRawDataRef<IntPtr>());
        }

        [Fact]
        public void Storage_Raw()
        {
            IntPtr nativeStorage = 42;
            // Using a fake value so we aren't disposing the OleVariant instance.
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_STORAGE, nativeStorage);
            Assert.Equal(VarEnum.VT_STORAGE, variant.VarType);
            Assert.Equal(nativeStorage, variant.GetRawDataRef<IntPtr>());
        }

        [Fact]
        public void StreamedObject_Raw()
        {
            IntPtr nativeStream = 42;
            // Using a fake value so we aren't disposing the OleVariant instance.
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_STREAMED_OBJECT, nativeStream);
            Assert.Equal(VarEnum.VT_STREAMED_OBJECT, variant.VarType);
            Assert.Equal(nativeStream, variant.GetRawDataRef<IntPtr>());
        }

        [Fact]
        public void StoredObject_Raw()
        {
            IntPtr nativeStorage = 42;
            // Using a fake value so we aren't disposing the OleVariant instance.
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_STORED_OBJECT, nativeStorage);
            Assert.Equal(VarEnum.VT_STORED_OBJECT, variant.VarType);
            Assert.Equal(nativeStorage, variant.GetRawDataRef<IntPtr>());
        }

        [Fact]
        public void VersionedStream_Raw()
        {
            IntPtr nativeStream = 42;
            // Using a fake value so we aren't disposing the OleVariant instance.
            OleVariant variant = OleVariant.CreateRaw((VarEnum)73, nativeStream);
            Assert.Equal((VarEnum)73, variant.VarType);
            Assert.Equal(nativeStream, variant.GetRawDataRef<IntPtr>());
        }

        [Fact]
        public void BlobObject_Raw()
        {
            Blob blob = new Blob { Length = 3, Data = Marshal.AllocCoTaskMem(10) };
            using OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_BLOB_OBJECT, blob);
            Assert.Equal(VarEnum.VT_BLOB_OBJECT, variant.VarType);
            Assert.Throws<ArgumentException>("T", () => variant.As<Blob>());
            Assert.Equal(blob, variant.GetRawDataRef<Blob>());
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct ClipboardData
        {
            public uint _size;
            public int _format;
            public IntPtr _data;
        }

        [Fact]
        public unsafe void ClipData_Raw()
        {
            // Construct a valid clipboard data structure
            // so we can validate the Clear/Dispose logic.
            IntPtr clipboardData = Marshal.AllocCoTaskMem(sizeof(ClipboardData));
            ((ClipboardData*)clipboardData)->_data = Marshal.AllocCoTaskMem(10);
            ((ClipboardData*)clipboardData)->_size = 10;
            ((ClipboardData*)clipboardData)->_format = 1;

            using OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_CF, clipboardData);
            Assert.Equal(VarEnum.VT_CF, variant.VarType);
            Assert.Equal(clipboardData, variant.GetRawDataRef<IntPtr>());
        }

        [Fact]
        public unsafe void Clsid_Raw()
        {
            // VT_CLSID is represented as a pointer to a GUID, not a GUID itself.
            IntPtr pClsid = Marshal.AllocCoTaskMem(sizeof(Guid));
            *(Guid*)pClsid = Guid.NewGuid();
            using OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_CLSID, pClsid);
            Assert.Equal(VarEnum.VT_CLSID, variant.VarType);
            Assert.Equal(pClsid, variant.GetRawDataRef<IntPtr>());
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Vector
        {
            public int Length;
            public IntPtr Data;
        }

        [Fact]
        public void Vector_Raw()
        {
            Vector vector = new Vector { Length = 3, Data = Marshal.AllocCoTaskMem(sizeof(int) * 3) };
            using OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_VECTOR | VarEnum.VT_I4, vector);
            Assert.Equal(VarEnum.VT_VECTOR | VarEnum.VT_I4, variant.VarType);
            Assert.Throws<ArgumentException>("T", () => variant.As<Vector>());
            Assert.Equal(vector, variant.GetRawDataRef<Vector>());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Array_Raw()
        {
            IntPtr safeArray = 42;
            // Using a fake value so we aren't disposing the OleVariant instance.
            OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_ARRAY | VarEnum.VT_I4, safeArray);
            Assert.Equal(VarEnum.VT_ARRAY | VarEnum.VT_I4, variant.VarType);
            Assert.Equal(safeArray, variant.GetRawDataRef<IntPtr>());
        }

        [Fact]
        [PlatformSpecific(~TestPlatforms.Windows)]
        public void Array_Raw_NonWindows()
        {
            Assert.Throws<PlatformNotSupportedException>(() => OleVariant.CreateRaw(VarEnum.VT_ARRAY | VarEnum.VT_I4, 0));
        }

        [Fact]
        public void ByRef_Raw()
        {
            // byref VARIANTs don't own the memory they point to.
            IntPtr byref = Marshal.AllocCoTaskMem(4);
            using OleVariant variant = OleVariant.CreateRaw(VarEnum.VT_BYREF | VarEnum.VT_I4, byref);
            Assert.Equal(VarEnum.VT_BYREF | VarEnum.VT_I4, variant.VarType);
            Assert.Equal(byref, variant.GetRawDataRef<IntPtr>());
            Marshal.FreeCoTaskMem(byref);
        }
    }
}
