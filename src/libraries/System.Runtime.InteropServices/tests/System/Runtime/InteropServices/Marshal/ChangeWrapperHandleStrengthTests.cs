// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class ChangeWrapperHandleStrengthTests
    {
        public static IEnumerable<object[]> ChangeWrapperHandleStrength_TestData()
        {
            yield return new object[] { new object() };
            yield return new object[] { 10 };
            yield return new object[] { "string" };

            yield return new object[] { new NonGenericClass() };
            yield return new object[] { new GenericClass<string>() };
            yield return new object[] { new Dictionary<string, int>() };
            yield return new object[] { new NonGenericStruct() };
            yield return new object[] { new GenericStruct<string>() };
            yield return new object[] { Int32Enum.Value1 };

            yield return new object[] { new int[] { 10 } };
            yield return new object[] { new int[][] { new int[] { 10 } } };
            yield return new object[] { new int[,] { { 10 } } };

            MethodInfo method = typeof(ChangeWrapperHandleStrengthTests).GetMethod(nameof(NonGenericMethod));
            Delegate d = method.CreateDelegate(typeof(NonGenericDelegate));
            yield return new object[] { d };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNative))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(ChangeWrapperHandleStrength_TestData))]
        public void ChangeWrapperHandleStrength_ValidObject_Success(object otp)
        {
            Marshal.ChangeWrapperHandleStrength(otp, fIsWeak: true);
            Marshal.ChangeWrapperHandleStrength(otp, fIsWeak: false);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotNetNative))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void ChangeWrapperHandleStrength_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("otp", () => Marshal.ChangeWrapperHandleStrength(null, fIsWeak: true));
            AssertExtensions.Throws<ArgumentNullException>("otp", () => Marshal.ChangeWrapperHandleStrength(null, fIsWeak: false));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void ChangeWrapperHandleStrength_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.ChangeWrapperHandleStrength(null, fIsWeak: true));
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.ChangeWrapperHandleStrength(null, fIsWeak: false));
        }

        public static void NonGenericMethod(int i) { }
        public delegate void NonGenericDelegate(int i);

        public enum Int32Enum : int { Value1, Value2 }
    }
}
