// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNativeAot))]
    public partial class ThrowExceptionForHRTests
    {
        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15093", TestRuntimes.Mono)]
        [InlineData(unchecked((int)0x80020006))]
        [InlineData(unchecked((int)0x80020101))]
        public void ThrowExceptionForHR_NoErrorInfo_ReturnsValidException(int errorCode)
        {
            ClearCurrentIErrorInfo();

            bool calledCatch = false;
            try
            {
                Marshal.ThrowExceptionForHR(errorCode);
            }
            catch (Exception ex)
            {
                calledCatch = true;

                Assert.IsType<COMException>(ex);
                Assert.Equal(errorCode, ex.HResult);
                Assert.Null(ex.InnerException);
                Assert.Null(ex.HelpLink);
                Assert.NotEmpty(ex.Message);

                string sourceMaybe = "System.Private.CoreLib";

                // If the ThrowExceptionForHR is inlined by the JIT, the source could be the test assembly
                Assert.Contains(ex.Source, new string[] { sourceMaybe, Assembly.GetExecutingAssembly().GetName().Name });
                Assert.Contains(nameof(ThrowExceptionForHR_NoErrorInfo_ReturnsValidException), ex.StackTrace);
                Assert.Contains(nameof(Marshal.ThrowExceptionForHR), ex.TargetSite.Name);
            }

            Assert.True(calledCatch, "Expected an exception to be thrown.");
        }

        public static IEnumerable<object[]> ThrowExceptionForHR_ErrorInfo_TestData()
        {
            yield return new object[] { unchecked((int)0x80020006), IntPtr.Zero };
            yield return new object[] { unchecked((int)0x80020101), IntPtr.Zero };
            yield return new object[] { unchecked((int)0x80020006), (IntPtr)(-1) };
            yield return new object[] { unchecked((int)0x80020101), (IntPtr)(-1) };
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15093", TestRuntimes.Mono)]
        [MemberData(nameof(ThrowExceptionForHR_ErrorInfo_TestData))]
        public void ThrowExceptionForHR_ErrorInfo_ReturnsValidException(int errorCode, IntPtr errorInfo)
        {
            ClearCurrentIErrorInfo();

            bool calledCatch = false;
            try
            {
                Marshal.ThrowExceptionForHR(errorCode, errorInfo);
            }
            catch (Exception ex)
            {
                calledCatch = true;

                Assert.IsType<COMException>(ex);
                Assert.Equal(errorCode, ex.HResult);
                Assert.Null(ex.InnerException);
                Assert.Null(ex.HelpLink);
                Assert.NotEmpty(ex.Message);

                string sourceMaybe = "System.Private.CoreLib";

                // If the ThrowExceptionForHR is inlined by the JIT, the source could be the test assembly
                Assert.Contains(ex.Source, new string[] { sourceMaybe, Assembly.GetExecutingAssembly().GetName().Name });
                Assert.Contains(nameof(ThrowExceptionForHR_ErrorInfo_ReturnsValidException), ex.StackTrace);
                Assert.Contains(nameof(Marshal.ThrowExceptionForHR), ex.TargetSite.Name);
            }

            Assert.True(calledCatch, "Expected an exception to be thrown.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void ThrowExceptionForHR_InvalidHR_Nop(int errorCode)
        {
            Marshal.ThrowExceptionForHR(errorCode);
            Marshal.ThrowExceptionForHR(errorCode, IntPtr.Zero);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        public void ThrowExceptionForHR_BasedOnISupportErrorInfo()
        {
            var comWrappers = new StrategyBasedComWrappers();
            Guid iid = new Guid("999b8152-1e6f-4166-8b35-ac89475e96fa");
            var obj = new ConditionallySupportErrorInfo(iid);
            IntPtr pUnk = comWrappers.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
            try
            {
                var exception = new InvalidOperationException();
                ClearCurrentIErrorInfo();

                // Set the error info for the current thread to the exception.
                _ = Marshal.GetHRForException(exception);

                // The HResult from the IErrorInfo is used because the ISupportErrorInfo interface returned S_OK for the provided iid.
                Assert.IsType<InvalidOperationException>(Marshal.GetExceptionForHR(new ArgumentException().HResult, iid, pUnk));

                // Set the error info for the current thread to the exception.
                _ = Marshal.GetHRForException(exception);

                var otherIid = new Guid("65af44f4-fd4f-4a35-a6f5-a0c66878fa75");

                // The HResult from the IErrorInfo is ignored because the ISupportErrorInfo interface returned S_FALSE for the provided otherIid.
                Assert.IsType<ArgumentException>(Marshal.GetExceptionForHR(new ArgumentException().HResult, otherIid, pUnk));
            }
            finally
            {
                Marshal.Release(pUnk);
            }
        }

        private static void ClearCurrentIErrorInfo()
        {
            // Ensure that if the thread's current IErrorInfo
            // is set during a run that it is thrown away prior
            // to interpreting the HRESULT.
            Marshal.GetExceptionForHR(unchecked((int)0x80040001));
        }

        [GeneratedComClass]
        internal sealed partial class ConditionallySupportErrorInfo(Guid iid) : ISupportErrorInfo
        {
            public int InterfaceSupportsErrorInfo(in Guid riid)
            {
                return iid == riid ? 0 : 1; // S_OK or S_FALSE
            }
        }

        [GeneratedComInterface]
        [Guid("DF0B3D60-548F-101B-8E65-08002B2BD119")]
        internal partial interface ISupportErrorInfo
        {
            [PreserveSig]
            int InterfaceSupportsErrorInfo(in Guid riid);
        }
    }
}
