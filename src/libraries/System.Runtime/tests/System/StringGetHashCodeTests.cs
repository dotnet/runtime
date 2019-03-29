// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class StringGetHashCodeTests
    {
        /// <summary>
        /// Ensure that hash codes are randomized by getting the hash in two processes
        /// and confirming it is different (modulo possible values of int).
        /// If the legacy hash codes are being returned, it will not be different.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetHashCode_TestData))]
        public void GetHashCodeWithStringComparer_UseSameStringInTwoProcesses_ReturnsDifferentHashCodes(int getHashCodeIndex)
        {
            Func<string, string, int> method = (parentHash, i) => int.Parse(parentHash) != s_GetHashCodes[int.Parse(i)]() ? RemoteExecutor.SuccessExitCode : -1;
            int parentHashCode = s_GetHashCodes[getHashCodeIndex]();
            int exitCode, retry = 0;
            do
            {
                // very small chance the child and parent hashcode are the same. To further reduce chance of collision we try up to 3 times
                using (RemoteInvokeHandle handle = RemoteExecutor.Invoke(method, parentHashCode.ToString(), getHashCodeIndex.ToString(), new RemoteInvokeOptions { CheckExitCode = false }))
                {
                    exitCode = handle.ExitCode;
                    retry++;
                }
            } while (exitCode != RemoteExecutor.SuccessExitCode && retry < 3);
            Assert.Equal(RemoteExecutor.SuccessExitCode, exitCode);
        }

        public static IEnumerable<object[]> GetHashCode_TestData()
        {
            for (int i = 0; i < s_GetHashCodes.Length; i++)
            {
                yield return new object[] { i };
            }
        }

        private static readonly Func<int>[] s_GetHashCodes = {
            () => { return StringComparer.CurrentCulture.GetHashCode("abc"); },
            () => { return StringComparer.CurrentCultureIgnoreCase.GetHashCode("abc"); },
            () => { return StringComparer.InvariantCulture.GetHashCode("abc"); },
            () => { return StringComparer.InvariantCultureIgnoreCase.GetHashCode("abc"); },
            () => { return StringComparer.Ordinal.GetHashCode("abc"); },
            () => { return StringComparer.OrdinalIgnoreCase.GetHashCode("abc"); },
            () => { return "abc".GetHashCode(); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreCase); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreKanaType); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreNonSpace); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreSymbols); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.IgnoreWidth); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.None); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.Ordinal); },
            () => { return CultureInfo.CurrentCulture.CompareInfo.GetHashCode("abc", CompareOptions.OrdinalIgnoreCase); }
        };
    }
}
