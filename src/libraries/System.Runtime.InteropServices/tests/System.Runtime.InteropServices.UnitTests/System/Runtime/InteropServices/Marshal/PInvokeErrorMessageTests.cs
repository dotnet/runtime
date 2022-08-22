// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class PInvokeErrorMessageTests
    {
        public static IEnumerable<object[]> GetErrorCode_TestData()
        {
            yield return new object[] { 0 };
            yield return new object[] { 1 };

            // errno values
            yield return new object[] { 0x10002 };
            yield return new object[] { 0x10003 };
            yield return new object[] { 0x10014 };
            yield return new object[] { 0x1001D };

            // HRESULT values
            yield return new object[] { unchecked((int)0x80004001) };
            yield return new object[] { unchecked((int)0x80004005) };
            yield return new object[] { unchecked((int)0x80070057) };
            yield return new object[] { unchecked((int)0x8000FFFF) };
        }

        [Theory]
        [MemberData(nameof(GetErrorCode_TestData))]
        public void PInvokeErrorMessage_Returns_Win32Exception_Message(int error)
        {
            // The Win32Exception represents the canonical system exception on
            // all platforms. The GetPInvokeErrorMessage API is about providing
            // this message in a manner that avoid the instantiation of a Win32Exception
            // instance and querying for the message.
            string expected = new Win32Exception(error).Message;
            Assert.Equal(expected, Marshal.GetPInvokeErrorMessage(error));
        }

        [Theory]
        [MemberData(nameof(GetErrorCode_TestData))]
        public void LastPInvokeErrorMessage_Returns_Correct_Message(int error)
        {
            string expected = Marshal.GetPInvokeErrorMessage(error);

            Marshal.SetLastPInvokeError(error);
            Assert.Equal(expected, Marshal.GetLastPInvokeErrorMessage());
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void PInvokeErrorMessage_Returns_UniqueMessage_Windows()
        {
            var err1 = Marshal.GetPInvokeErrorMessage(unchecked((int)0x80070057)); // E_INVALIDARG
            var err2 = Marshal.GetPInvokeErrorMessage(unchecked((int)0x80004001)); // E_NOTIMPL
            Assert.NotNull(err1);
            Assert.NotNull(err2);
            Assert.NotEqual(err1, err2);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void PInvokeErrorMessage_Returns_UniqueMessage_Unix()
        {
            var err1 = Marshal.GetPInvokeErrorMessage(0x10002); // EACCES
            var err2 = Marshal.GetPInvokeErrorMessage(0x10014); // EEXIST
            Assert.NotNull(err1);
            Assert.NotNull(err2);
            // It is difficult to determine which error codes do or do not have
            // translations on non-Windows. For now, we will just confirm the
            // message is non-null.
        }
    }
}
