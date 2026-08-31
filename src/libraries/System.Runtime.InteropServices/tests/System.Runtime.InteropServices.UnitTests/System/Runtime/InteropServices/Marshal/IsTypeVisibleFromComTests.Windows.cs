// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class IsTypeVisibleFromComTests
    {
        public static IEnumerable<object[]> IsTypeVisibleFromCom_Windows_TestData()
        {
            yield return new object[] { typeof(ComImportObject), true };
            yield return new object[] { typeof(InterfaceOnComImportObject), true };
            yield return new object[] { typeof(InterfaceComImportObject), true };

            yield return new object[] { typeof(IsTypeVisibleFromComTests), true };
            yield return new object[] { typeof(PrivateType), false };
            yield return new object[] { typeof(ProtectedType), false };
            yield return new object[] { typeof(InternalType), false };
            yield return new object[] { typeof(InnerManagedInterface), false};
            yield return new object[] { typeof(INonGenericInterface), true };
            yield return new object[] { typeof(NonGenericStruct), true };
            yield return new object[] { typeof(ManagedClassWithComVisibleFalse), false };
            yield return new object[] { typeof(ManagedClassWithComVisibleTrue), true };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabled))]
        [MemberData(nameof(IsTypeVisibleFromCom_Windows_TestData))]
        public void IsTypeVisibleFromCom_Windows_ReturnsExpected(Type value, bool expected)
        {
            Assert.Equal(expected, Marshal.IsTypeVisibleFromCom(value));
        }

        private class PrivateType
        {
        }

        protected class ProtectedType
        {
        }

        internal class InternalType
        {
        }

        interface InnerManagedInterface
        {
        }
    }

    [ComVisibleAttribute(false)]
    public class ManagedClassWithComVisibleFalse
    {
    }

    [ComVisibleAttribute(true)]
    public class ManagedClassWithComVisibleTrue
    {
    }
}
