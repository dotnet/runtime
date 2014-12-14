// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using Xunit;

namespace Microsoft.Win32.Primitives.Tests
{
    public static class Win32ExceptionTestType
    {
        [Fact]
        public static void InstantiateException()
        {
            Win32Exception ex = new Win32Exception(5);
            Assert.True(ex.HResult == E_FAIL);
            Assert.True(ex.NativeErrorCode == 5);
        }

        public const int E_FAIL = unchecked((int)0x80004005);
    }
}