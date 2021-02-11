// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamic
{
    using System;
    using System.Runtime.InteropServices;
    using TestLibrary;

    internal class BasicTest
    {
        private Random rand;
        private dynamic obj;

        public BasicTest(int seed = 123)
        {
            Type t = Type.GetTypeFromCLSID(Guid.Parse(ServerGuids.BasicTest));
            obj = Activator.CreateInstance(t);
            rand = new Random(seed);
        }

        public void Run()
        {
            Console.WriteLine($"Running {nameof(BasicTest)}");

            DefaultMember();

            Boolean();
            SByte();
            Byte();
            Short();
            UShort();
            Int();
            UInt();
            Int64();
            UInt64();

            Float();
            Double();

            IntPtr();
            UIntPtr();

            String();
            Date();
            ComObject();
            Null();

            ErrorWrapper();
            CurrencyWrapper();
            VariantWrapper();

            Fail();
        }

        private void DefaultMember()
        {
            int val = (int)rand.Next(int.MinValue / 2, int.MaxValue / 2);
            int expected = val * 2;

            // Invoke default member
            Assert.AreEqual(expected, obj(val));
            Assert.AreEqual(expected, obj.Default(val));
        }

        private void Boolean()
        {
            // Get and set property
            obj.Boolean_Property = true;
            Assert.IsTrue(obj.Boolean_Property);

            // Call method with return value
            Assert.IsFalse(obj.Boolean_Inverse_Ret(true));

            // Call method passing by ref
            bool inout = true;
            obj.Boolean_Inverse_InOut(ref inout);
            Assert.IsFalse(inout);

            // Pass as variant
            Variant<bool>(true, false);
        }

        private void SByte()
        {
            sbyte val = (sbyte)rand.Next(sbyte.MinValue / 2, sbyte.MaxValue / 2);
            sbyte expected = (sbyte)(val * 2);

            // Get and set property
            obj.SByte_Property = val;
            Assert.AreEqual(val, obj.SByte_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.SByte_Doubled_Ret(val));

            // Call method passing by ref
            sbyte inout = val;
            obj.SByte_Doubled_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<sbyte>(val, expected);
        }

        private void Byte()
        {
            byte val = (byte)rand.Next(byte.MaxValue / 2);
            byte expected = (byte)(val * 2);

            // Get and set property
            obj.Byte_Property = val;
            Assert.AreEqual(val, obj.Byte_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.Byte_Doubled_Ret(val));

            // Call method passing by ref
            byte inout = val;
            obj.Byte_Doubled_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<byte>(val, expected);
        }

        private void Short()
        {
            short val = (short)rand.Next(short.MinValue / 2, short.MaxValue / 2);
            short expected = (short)(val * 2);

            // Get and set property
            obj.Short_Property = val;
            Assert.AreEqual(val, obj.Short_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.Short_Doubled_Ret(val));

            // Call method passing by ref
            short inout = val;
            obj.Short_Doubled_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<short>(val, expected);
        }

        private void UShort()
        {
            ushort val = (ushort)rand.Next(ushort.MaxValue / 2);
            ushort expected = (ushort)(val * 2);

            // Get and set property
            obj.UShort_Property = val;
            Assert.AreEqual(val, obj.UShort_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.UShort_Doubled_Ret(val));

            // Call method passing by ref
            ushort inout = val;
            obj.UShort_Doubled_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<ushort>(val, expected);
        }

        private void Int()
        {
            int val = (int)rand.Next(int.MinValue / 2, int.MaxValue / 2);
            int expected = val * 2;

            // Get and set property
            obj.Int_Property = val;
            Assert.AreEqual(val, obj.Int_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.Int_Doubled_Ret(val));

            // Call method passing by ref
            int inout = val;
            obj.Int_Doubled_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<int>(val, expected);
        }

        private void UInt()
        {
            uint val = (uint)rand.Next();
            uint expected = val * 2;

            // Get and set property
            obj.UInt_Property = val;
            Assert.AreEqual(val, obj.UInt_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.UInt_Doubled_Ret(val));

            // Call method passing by ref
            uint inout = val;
            obj.UInt_Doubled_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<uint>(val, expected);
        }

        private void Int64()
        {
            long val = (long)rand.Next();
            long expected = val * 2;

            // Get and set property
            obj.Int64_Property = val;
            Assert.AreEqual(val, obj.Int64_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.Int64_Doubled_Ret(val));

            // Call method passing by ref
            long inout = val;
            obj.Int64_Doubled_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<long>(val, expected);
        }

        private void UInt64()
        {
            ulong val = (ulong)rand.Next();
            ulong expected = val * 2;

            // Get and set property
            obj.UInt64_Property = val;
            Assert.AreEqual(val, obj.UInt64_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.UInt64_Doubled_Ret(val));

            // Call method passing by ref
            ulong inout = val;
            obj.UInt64_Doubled_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<ulong>(val, expected);
        }

        private void Float()
        {
            float val = rand.Next() / 10f;
            float expected = (float)Math.Ceiling(val);

            // Get and set property
            obj.Float_Property = val;
            Assert.AreEqual(val, obj.Float_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.Float_Ceil_Ret(val));

            // Call method passing by ref
            float inout = val;
            obj.Float_Ceil_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<float>(val, expected);
        }

        private void Double()
        {
            double val = rand.Next() / 10.0;
            double expected = Math.Ceiling(val);

            // Get and set property
            obj.Double_Property = val;
            Assert.AreEqual(val, obj.Double_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.Double_Ceil_Ret(val));

            // Call method passing by ref
            double inout = val;
            obj.Double_Ceil_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<double>(val, expected);
        }

        private void IntPtr()
        {
            // IntPtr as a variant is converted to int in the runtime. Dynamic COM binding matches this behaviour.
            // Runtime variant: OleVariant::MarshalOleVariantForObject conversion from ELEMENT_TYPE_I to VT_INT
            // Dynamic COM binding: VarEnumSelector.TryGetPrimitiveComType conversion from IntPtr to VT_INT
            int valRaw = (int)rand.Next(int.MinValue / 2, int.MaxValue / 2);
            int expectedRaw = valRaw * 2;

            IntPtr val = (IntPtr)valRaw;
            IntPtr expected = (IntPtr)expectedRaw;

            // Get and set property
            obj.Variant_Property = val;
            Assert.AreEqual(valRaw, obj.Variant_Property);

            // Call method with return value
            Assert.AreEqual(expectedRaw, obj.Variant_Ret(val));

            // Call method passing by ref
            IntPtr inout = val;
            obj.Variant_InOut(ref inout);
            Assert.AreEqual(expected, inout);
        }

        private void UIntPtr()
        {
            // UIntPtr as a variant is converted to uint in the runtime. Dynamic COM binding matches this behaviour.
            // Runtime variant: OleVariant::MarshalOleVariantForObject conversion from ELEMENT_TYPE_U to VT_UINT
            // Dynamic COM binding: VarEnumSelector.TryGetPrimitiveComType conversion from UIntPtr to VT_UINT
            uint valRaw = (uint)rand.Next();
            uint expectedRaw = valRaw * 2;

            UIntPtr val = (UIntPtr)valRaw;
            UIntPtr expected = (UIntPtr)expectedRaw;

            // Get and set property
            obj.Variant_Property = val;
            Assert.AreEqual(valRaw, obj.Variant_Property);

            // Call method with return value
            Assert.AreEqual(expectedRaw, obj.Variant_Ret(val));

            // Call method passing by ref
            UIntPtr inout = val;
            obj.Variant_InOut(ref inout);
            Assert.AreEqual(expected, inout);
        }

        private void String()
        {
            string val = System.IO.Path.GetRandomFileName();
            char[] chars = val.ToCharArray();
            Array.Reverse(chars);
            string expected = new string(chars);

            // Get and set property
            obj.String_Property = val;
            Assert.AreEqual(val, obj.String_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.String_Reverse_Ret(val));

            // Call method passing by ref
            string inout = val;
            obj.String_Reverse_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<string>(val, expected);

            StringWrapper(val, expected);
        }

        private void Date()
        {
            DateTime val = new DateTime(rand.Next(DateTime.MinValue.Year, DateTime.Now.Year), rand.Next(1, 12), rand.Next(1, 28));
            DateTime expected = val.AddDays(1);

            // Get and set property
            obj.Date_Property = val;
            Assert.AreEqual(val, obj.Date_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.Date_AddDay_Ret(val));

            // Call method passing by ref
            DateTime inout = val;
            obj.Date_AddDay_InOut(ref inout);
            Assert.AreEqual(expected, inout);

            // Pass as variant
            Variant<DateTime>(val, expected);
        }

        private void ComObject()
        {
            Type t = Type.GetTypeFromCLSID(Guid.Parse(ServerGuids.BasicTest));
            dynamic val = Activator.CreateInstance(t);

            // Get and set property
            obj.Dispatch_Property = val;
            Assert.AreEqual(val, obj.Dispatch_Property);

            // Update dispatch object
            obj.Dispatch_Property.Boolean_Property = false;
            Assert.IsFalse(obj.Dispatch_Property.Boolean_Property);
            Assert.IsFalse(val.Boolean_Property);

            // Call method with return value
            dynamic ret = obj.Dispatch_Ret(val);
            Assert.IsTrue(ret.Boolean_Property);
            Assert.IsFalse(val.Boolean_Property);

            // Call method passing by ref
            obj.Dispatch_InOut(ref val);
            Assert.IsTrue(val.Boolean_Property);

            val.Boolean_Property = false;
            Variant(val, new Action<dynamic>(d => Assert.IsTrue(d.Boolean_Property)));
            Assert.IsTrue(val.Boolean_Property);

            val.Boolean_Property = false;
            UnknownWrapper(val);
        }

        private void Null()
        {
            obj.Variant_Property = null;
            Assert.IsNull(obj.Variant_Property);

            obj.String_Property = null;
            Assert.AreEqual(string.Empty, obj.String_Property);
        }

        private void StringWrapper(string toWrap, string expected)
        {
            var val = new BStrWrapper(toWrap);

            // Get and set property
            obj.String_Property = val;
            Assert.AreEqual(val.WrappedObject, obj.String_Property);

            // Call method with return value
            Assert.AreEqual(expected, obj.String_Reverse_Ret(val));

            // Call method passing by ref
            BStrWrapper inout = new BStrWrapper(val.WrappedObject);
            obj.String_Reverse_InOut(ref inout);
            Assert.AreEqual(expected, inout.WrappedObject);
        }

        private void UnknownWrapper(dynamic toWrap)
        {
            var val = new UnknownWrapper(toWrap);

            // Get and set property
            obj.Variant_Property = val;
            Assert.AreEqual(val.WrappedObject, obj.Variant_Property);

            // Call method with return value
            dynamic ret = obj.Variant_Ret(val);
            Assert.IsTrue(ret.Boolean_Property);
            Assert.IsTrue(toWrap.Boolean_Property);

            // Call method passing by ref
            obj.Variant_InOut(ref val);
            Assert.IsTrue(toWrap.Boolean_Property);
        }

        private void ErrorWrapper()
        {
            const int E_NOTIMPL = unchecked((int)0X80004001);
            var val = new ErrorWrapper(E_NOTIMPL);

            // Get and set property
            obj.Variant_Property = val;
            Assert.AreEqual(val.ErrorCode, obj.Variant_Property);
        }

#pragma warning disable 618 // CurrencyWrapper is marked obsolete
        private void CurrencyWrapper()
        {
            decimal toWrap = rand.Next() / 10.0m;
            var val = new CurrencyWrapper(toWrap);

            // Get and set property
            obj.Variant_Property = val;
            Assert.AreEqual(val.WrappedObject, obj.Variant_Property);
        }
#pragma warning restore 618

        private void VariantWrapper()
        {
            long toWrap = (long)rand.Next();
            var val = new VariantWrapper(toWrap);
            long expected = toWrap * 2;

            // Get and set property
            obj.Variant_Property = val;
            Assert.AreEqual(val.WrappedObject, obj.Variant_Property);

            // Call method with return value
            dynamic ret = obj.Variant_Ret(val);
            Assert.AreEqual(expected, ret);

            // Call method passing by ref
            obj.Variant_InOut(ref val);
            Assert.AreEqual(expected, val.WrappedObject);
        }

        private void Variant<T>(T val, Action<T> validate)
        {
            // Get and set property
            obj.Variant_Property = val;
            Assert.AreEqual(val, obj.Variant_Property);

            // Call method with return value
            validate(obj.Variant_Ret(val));

            // Call method passing by ref
            T inout = val;
            obj.Variant_InOut(ref inout);
            validate(inout);
        }

        private void Variant<T>(T val, T expected)
        {
            Variant<T>(val, v => Assert.AreEqual(expected, v));
        }

        private void Fail()
        {
            const int E_ABORT = unchecked((int)0x80004004);
            string message = "CUSTOM ERROR MESSAGE";
            COMException comException = Assert.Throws<COMException>(() => obj.Fail(E_ABORT, message));
            Assert.AreEqual(E_ABORT, comException.HResult, "Unexpected HRESULT on COMException");
            Assert.AreEqual(message, comException.Message, "Unexpected message on COMException");

            Assert.Throws<SEHException>(() => obj.Throw());
        }
    }
}
