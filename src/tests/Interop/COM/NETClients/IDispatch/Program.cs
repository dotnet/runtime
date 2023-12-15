// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace NetClient
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;

    using TestLibrary;
    using Xunit;
    using Server.Contract;
    using Server.Contract.Servers;

    public class Program
    {
        static void Validate_Numeric_In_ReturnByRef()
        {
            var dispatchTesting = (DispatchTesting)new DispatchTestingClass();

            byte b1 = 1;
            byte b2 = b1;
            short s1 = -1;
            short s2 = s1;
            ushort us1 = 1;
            ushort us2 = us1;
            int i1 = -1;
            int i2 = i1;
            uint ui1 = 1;
            uint ui2 = ui1;
            long l1 = -1;
            long l2 = l1;
            ulong ul1 = 1;
            ulong ul2 = ul1;

            Console.WriteLine($"Calling {nameof(DispatchTesting.DoubleNumeric_ReturnByRef)} ...");
            dispatchTesting.DoubleNumeric_ReturnByRef (
                b1, ref b2,
                s1, ref s2,
                us1, ref us2,
                i1, ref i2,
                ui1, ref ui2,
                l1, ref l2,
                ul1, ref ul2);
            Console.WriteLine($"Call to {nameof(DispatchTesting.DoubleNumeric_ReturnByRef)} complete");

            Assert.Equal(b1 * 2, b2);
            Assert.Equal(s1 * 2, s2);
            Assert.Equal(us1 * 2, us2);
            Assert.Equal(i1 * 2, i2);
            Assert.Equal(ui1 * 2, ui2);
            Assert.Equal(l1 * 2, l2);
            Assert.Equal(ul1 * 2, ul2);
        }

        static private bool EqualByBound(float expected, float actual)
        {
            float low = expected - 0.0001f;
            float high = expected + 0.0001f;
            float eps = Math.Abs(expected - actual);
            return eps < float.Epsilon || (low < actual && actual < high);
        }

        static private bool EqualByBound(double expected, double actual)
        {
            double low = expected - 0.00001;
            double high = expected + 0.00001;
            double eps = Math.Abs(expected - actual);
            return eps < double.Epsilon || (low < actual && actual < high);
        }

        static void Validate_Float_In_ReturnAndUpdateByRef()
        {
            var dispatchTesting = (DispatchTesting)new DispatchTestingClass();

            float a = .1f;
            float b = .2f;
            float expected = a + b;

            Console.WriteLine($"Calling {nameof(DispatchTesting.Add_Float_ReturnAndUpdateByRef)} ...");
            float c = b;
            float d = dispatchTesting.Add_Float_ReturnAndUpdateByRef (a, ref c);

            Console.WriteLine($"Call to {nameof(DispatchTesting.Add_Float_ReturnAndUpdateByRef)} complete: {a} + {b} = {d}; {c} == {d}");
            Assert.True(EqualByBound(expected, c));
            Assert.True(EqualByBound(expected, d));
        }

        static void Validate_Double_In_ReturnAndUpdateByRef()
        {
            var dispatchTesting = (DispatchTesting)new DispatchTestingClass();

            double a = .1;
            double b = .2;
            double expected = a + b;

            Console.WriteLine($"Calling {nameof(DispatchTesting.Add_Double_ReturnAndUpdateByRef)} ...");
            double c = b;
            double d = dispatchTesting.Add_Double_ReturnAndUpdateByRef (a, ref c);

            Console.WriteLine($"Call to {nameof(DispatchTesting.Add_Double_ReturnAndUpdateByRef)} complete: {a} + {b} = {d}; {c} == {d}");
            Assert.True(EqualByBound(expected, c));
            Assert.True(EqualByBound(expected, d));
        }

        static int GetErrorCodeFromHResult(int hresult)
        {
            // https://msdn.microsoft.com/en-us/library/cc231198.aspx
            return hresult & 0xffff;
        }

        static void Validate_Exception()
        {
            var dispatchTesting = (DispatchTesting)new DispatchTestingClass();

            int errorCode = 127;
            string resultString = errorCode.ToString("x");
            try
            {
                Console.WriteLine($"Calling {nameof(DispatchTesting.TriggerException)} with {nameof(IDispatchTesting_Exception.Disp)} {errorCode}...");
                dispatchTesting.TriggerException(IDispatchTesting_Exception.Disp, errorCode);
                Assert.Fail("DISP exception not thrown properly");
            }
            catch (COMException e)
            {
                Assert.Equal(GetErrorCodeFromHResult(e.HResult), errorCode);
                Assert.Equal(e.Message, resultString);
            }

            try
            {
                Console.WriteLine($"Calling {nameof(DispatchTesting.TriggerException)} with {nameof(IDispatchTesting_Exception.HResult)} {errorCode}...");
                dispatchTesting.TriggerException(IDispatchTesting_Exception.HResult, errorCode);
                Assert.Fail("HRESULT exception not thrown properly");
            }
            catch (COMException e)
            {
                Assert.Equal(GetErrorCodeFromHResult(e.HResult), errorCode);
                // Failing HRESULT exceptions contain CLR generated messages
            }

            // Calling methods through IDispatch::Invoke() (i.e., late-bound) doesn't
            // propagate the HRESULT when marked with PreserveSig. It is always 0.
            {
                Console.WriteLine($"Calling {nameof(DispatchTesting.TriggerException)} (PreserveSig) with {nameof(IDispatchTesting_Exception.Int)} {errorCode}...");
                var dispatchTesting2 = (IDispatchTestingPreserveSig1)dispatchTesting;
                Assert.Equal(0, dispatchTesting2.TriggerException(IDispatchTesting_Exception.Int, errorCode));
            }

            {
                // Validate the HRESULT as a value type construct works for IDispatch.
                Console.WriteLine($"Calling {nameof(DispatchTesting.TriggerException)} (PreserveSig, ValueType) with {nameof(IDispatchTesting_Exception.Int)} {errorCode}...");
                var dispatchTesting3 = (IDispatchTestingPreserveSig2)dispatchTesting;
                Assert.Equal(0, dispatchTesting3.TriggerException(IDispatchTesting_Exception.Int, errorCode).Value);
            }
        }

        static void Validate_StructNotSupported()
        {
            Console.WriteLine($"IDispatch with structs not supported...");
            var dispatchTesting = (DispatchTesting)new DispatchTestingClass();

            var input = new HFA_4() { x = 1f, y = 2f, z = 3f, w = 4f };
            Assert.Throws<NotSupportedException>(() => dispatchTesting.DoubleHVAValues(ref input));
        }

        static void Validate_LCID_Marshaled()
        {
            var dispatchTesting = (DispatchTesting)new DispatchTestingClass();
            CultureInfo oldCulture = CultureInfo.CurrentCulture;
            CultureInfo newCulture = new CultureInfo("es-ES", false);
            try
            {
                CultureInfo englishCulture = new CultureInfo("en-US", false);
                CultureInfo.CurrentCulture = newCulture;
                int lcid = dispatchTesting.PassThroughLCID();
                Assert.Equal(englishCulture.LCID, lcid);  // CLR->Dispatch LCID marshalling is explicitly hardcoded to en-US instead of passing the current culture.
            }
            finally
            {
                CultureInfo.CurrentCulture = oldCulture;
            }
        }

        static void Validate_Enumerator()
        {
            var dispatchTesting = (DispatchTesting)new DispatchTestingClass();
            var expected = System.Linq.Enumerable.Range(0, 10);

            Console.WriteLine($"Calling {nameof(DispatchTesting.GetEnumerator)} ...");
            var enumerator = dispatchTesting.GetEnumerator();
            AssertExtensions.CollectionEqual(expected, GetEnumerable(enumerator));

            enumerator.Reset();
            AssertExtensions.CollectionEqual(expected, GetEnumerable(enumerator));

            Console.WriteLine($"Calling {nameof(DispatchTesting.ExplicitGetEnumerator)} ...");
            var enumeratorExplicit = dispatchTesting.ExplicitGetEnumerator();
            AssertExtensions.CollectionEqual(expected, GetEnumerable(enumeratorExplicit));

            enumeratorExplicit.Reset();
            AssertExtensions.CollectionEqual(expected, GetEnumerable(enumeratorExplicit));

            System.Collections.Generic.IEnumerable<int> GetEnumerable(System.Collections.IEnumerator e)
            {
               var list = new System.Collections.Generic.List<int>();
               while (e.MoveNext())
               {
                   list.Add((int)e.Current);
               }

               return list;
            }
        }

        [Fact]
        public static int TestEntryPoint()
        {
            // RegFree COM is not supported on Windows Nano
            if (Utilities.IsWindowsNanoServer)
            {
                return 100;
            }

            try
            {
                Validate_Numeric_In_ReturnByRef();
                Validate_Float_In_ReturnAndUpdateByRef();
                Validate_Double_In_ReturnAndUpdateByRef();
                Validate_Exception();
                Validate_StructNotSupported();
                Validate_LCID_Marshaled();
                Validate_Enumerator();
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
