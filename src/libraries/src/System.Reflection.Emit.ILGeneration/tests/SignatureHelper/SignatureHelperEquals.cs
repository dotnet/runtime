// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperEquals
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper1 = SignatureHelper.GetFieldSigHelper(module);
            SignatureHelper helper2 = SignatureHelper.GetFieldSigHelper(module);

            SignatureHelper helper3 = SignatureHelper.GetFieldSigHelper(module);
            helper3.AddArgument(typeof(string));
            SignatureHelper helper4 = SignatureHelper.GetFieldSigHelper(module);
            helper4.AddArgument(typeof(string));

            yield return new object[] { helper1, helper2, true };
            yield return new object[] { helper3, helper1, false };
            yield return new object[] { helper3, helper4, true };
            yield return new object[] { helper1, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(SignatureHelper helper, object obj, bool expected)
        {
            Assert.Equal(expected, helper.Equals(obj));
            if (obj is SignatureHelper && expected == true)
            {
                Assert.Equal(helper.GetHashCode(), obj.GetHashCode());
            }
        }
    }
}
