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
    public partial class ComVariantMarshallerTests
    {
        [Fact]
        public void Null_Marshals_To_Empty()
        {
            Assert.Equal(VarEnum.VT_EMPTY, ComVariantMarshaller.ConvertToUnmanaged(null).VarType);
            Assert.Null(ComVariantMarshaller.ConvertToManaged(default));
        }

        [Fact]
        public void DBNull_Marshals_To_Null()
        {
            Assert.Equal(VarEnum.VT_NULL, ComVariantMarshaller.ConvertToUnmanaged(DBNull.Value).VarType);
            Assert.Same(DBNull.Value, ComVariantMarshaller.ConvertToManaged(ComVariant.Null));
        }

        [Fact]
        public void String_Marshals_To_BStr()
        {
            string value = "Hello";
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_BSTR, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void BStrWrapper_Marshals_To_BStr()
        {
            string value = "Hello";
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(new BStrWrapper(value));
            Assert.Equal(VarEnum.VT_BSTR, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void Int32_Marshals_To_I4()
        {
            int value = 42;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_I4, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void UInt32_Marshals_To_UI4()
        {
            uint value = 42;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_UI4, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void Int16_Marshals_To_I2()
        {
            short value = 42;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_I2, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void UInt16_Marshals_To_UI2()
        {
            ushort value = 42;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_UI2, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void Byte_Marshals_To_UI1()
        {
            byte value = 42;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_UI1, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void SByte_Marshals_To_I1()
        {
            sbyte value = 42;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_I1, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void Double_Marshals_To_R8()
        {
            double value = 42.0;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_R8, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void Single_Marshals_To_R4()
        {
            float value = 42.0f;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_R4, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [InlineData(true)]
        [InlineData(false)]
        [Theory]
        public void Boolean_Marshals_To_BOOL(bool value)
        {
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_BOOL, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void ErrorWrapper_Maps_To_VT_ERROR()
        {
            ErrorWrapper errorWrapper = new ErrorWrapper(42);
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(errorWrapper);
            Assert.Equal(VarEnum.VT_ERROR, variant.VarType);
            Assert.Equal(errorWrapper.ErrorCode, Assert.IsType<int>(ComVariantMarshaller.ConvertToManaged(variant)));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void VariantWrapper_Throws()
        {
            VariantWrapper wrapper = new VariantWrapper(42);
            Assert.Throws<ArgumentException>("managed", () => ComVariantMarshaller.ConvertToUnmanaged(wrapper));
        }

        [Fact]
        public void Decimal_Marshals_To_DECIMAL()
        {
            decimal value = 42.0m;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_DECIMAL, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void Date_Marshals_To_DATE()
        {
            // OLE dates do not have time zones and do not support sub-millisecond precision.
            // Select a date format that includes the maximum precision that OLE supports.
            DateTime value = DateTime.Parse("2023-10-17T14:47:32.6390000");
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(value);
            Assert.Equal(VarEnum.VT_DATE, variant.VarType);
            Assert.Equal(value, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

#pragma warning disable CS0618 // Type or member is obsolete
        [Fact]
        public void CurrentyWrapper_Marshals_To_CY()
        {
            decimal value = 42.0m;
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(new CurrencyWrapper(value));
            Assert.Equal(VarEnum.VT_CY, variant.VarType);
            Assert.Equal(value, Assert.IsType<decimal>(ComVariantMarshaller.ConvertToManaged(variant)));
            ComVariantMarshaller.Free(variant);
        }
#pragma warning restore CS0618 // Type or member is obsolete

        [GeneratedComInterface]
        [Guid("ADD9E468-1503-48E5-AA18-B6B6BD1FF34A")]
        internal partial interface IGeneratedComInterface
        {
            void Method();
        }

        [GeneratedComClass]
        internal sealed partial class ComExposedType : IGeneratedComInterface
        {
            public void Method() { }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55742", TestRuntimes.Mono)]
        public unsafe void GeneratedComInterfaceType_Marshals_To_UNKNOWN()
        {
            var obj = new ComExposedType();
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(obj);
            Assert.Equal(VarEnum.VT_UNKNOWN, variant.VarType);
            // Validate that the correct object is wrapped.
            Assert.True(ComWrappers.TryGetObject(variant.GetRawDataRef<nint>(), out object wrappedObj));
            Assert.Same(obj, wrappedObj);
            // Validate that we use the same ComWrappers instance as ComInterfaceMarshaller<T>.
            Assert.Same(obj, ComInterfaceMarshaller<object>.ConvertToManaged((void*)variant.GetRawDataRef<nint>()));
            Assert.Same(obj, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/55742", TestRuntimes.Mono)]
        public void UnknownWrapper_Of_GeneratedComInterfaceType_Marshals_To_UNKNOWN()
        {
            var obj = new ComExposedType();
            ComVariant variant = ComVariantMarshaller.ConvertToUnmanaged(new UnknownWrapper(obj));
            Assert.Equal(VarEnum.VT_UNKNOWN, variant.VarType);
            Assert.True(ComWrappers.TryGetObject(variant.GetRawDataRef<nint>(), out object wrappedObj));
            Assert.Same(obj, wrappedObj);
            Assert.Same(obj, ComVariantMarshaller.ConvertToManaged(variant));
            ComVariantMarshaller.Free(variant);
        }

        [Fact]
        public void INT_Marshals_as_Int()
        {
            ComVariant variant = ComVariant.CreateRaw(VarEnum.VT_INT, 42);
            Assert.Equal(42, Assert.IsType<int>(ComVariantMarshaller.ConvertToManaged(variant)));
        }

        [Fact]
        public void UINT_Marshals_as_UInt()
        {
            ComVariant variant = ComVariant.CreateRaw(VarEnum.VT_UINT, 42u);
            Assert.Equal(42u, Assert.IsType<uint>(ComVariantMarshaller.ConvertToManaged(variant)));
        }

        [InlineData(VarEnum.VT_I1, (byte)42)]
        [InlineData(VarEnum.VT_I1, (sbyte)42)]
        [InlineData(VarEnum.VT_UI1, (byte)42)]
        [InlineData(VarEnum.VT_UI1, (sbyte)42)]
        [InlineData(VarEnum.VT_I2, (short)42)]
        [InlineData(VarEnum.VT_I2, (ushort)42)]
        [InlineData(VarEnum.VT_UI2, (short)42)]
        [InlineData(VarEnum.VT_UI2, (ushort)42)]
        [InlineData(VarEnum.VT_I4, 42)]
        [InlineData(VarEnum.VT_I4, (uint)42)]
        [InlineData(VarEnum.VT_UI4, 42)]
        [InlineData(VarEnum.VT_UI4, (uint)42)]
        [InlineData(VarEnum.VT_ERROR, (uint)42)]
        [InlineData(VarEnum.VT_ERROR, 42)]
        [InlineData(VarEnum.VT_I8, 42L)]
        [InlineData(VarEnum.VT_I8, 42UL)]
        [InlineData(VarEnum.VT_UI8, 42L)]
        [InlineData(VarEnum.VT_UI8, 42UL)]
        [InlineData(VarEnum.VT_R4, 42.0f)]
        [InlineData(VarEnum.VT_R8, 42.0)]
        [InlineData(VarEnum.VT_BOOL, true)]
        [InlineData(VarEnum.VT_BOOL, false)]
        [Theory]
        public unsafe void ByRef_Primitives(VarEnum elementType, object valueToSet)
        {
            long storage = 0;
            ComVariant variant = ComVariant.CreateRaw(VarEnum.VT_BYREF | elementType, (nint)(&storage));
            // Set up the marshaller
            ComVariantMarshaller.RefPropagate marshaller = default;
            marshaller.FromUnmanaged(variant);

            // Marshal back the new value
            marshaller.FromManaged(valueToSet);

            ComVariant updated = marshaller.ToUnmanaged();

            // Make sure we didn't change the pointer.
            Assert.Equal(variant.GetRawDataRef<nint>(), updated.GetRawDataRef<nint>());

            // Validate that the new value of the variant is the same as the value we set.
            // Go through IConvertible to handle the case of "same size, different signedness" (e.g. int and uint)
            Assert.Equal(valueToSet, ((IConvertible)ComVariantMarshaller.ConvertToManaged(variant)).ToType(valueToSet.GetType(), null));
        }
    }
}
