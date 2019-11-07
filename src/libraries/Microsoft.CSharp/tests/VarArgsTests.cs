// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class VarArgsTests
    {
        // The inability to cast __arglist to object means it can't be used with dynamic bounding
        // but we still need to be sure that the binder doesn't choke when it encounters a varargs
        // method, or attempt to bind to it.

        public class HasVarargs
        {
            public void OnlyVarargs(__arglist)
            {
            }

            // Overloads where the varargs form could perhaps be confused with another
            public int Nullary(__arglist) => 0;

            public int Nullary() => 1;

            public int Unary0(int i, __arglist) => 0;

            public int Unary0(int i) => i;

            public int Unary1(int i, __arglist) => 0;

            public int Unary1() => 1;

            public int Unary1(long l) => (int)l;

            public int Unary1(long l, string s) => 2;

            public int Binary(int i, __arglist) => 0;

            public int Binary(int i, int j, __arglist) => 0;

            public int Binary(int i, int j) => i + j;
        }

        public class VarArgCtorOption
        {
            public int Value { get; }
            public VarArgCtorOption(__arglist)
            {
            }

            public VarArgCtorOption(int i, __arglist)
            {
            }

            public VarArgCtorOption(int i) => Value = i;
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void FailBindOnlyVarargsAvailable()
        {
            dynamic d = new HasVarargs();
            string errorMessage = Assert.Throws<RuntimeBinderException>(() => d.OnlyVarargs()).Message;
            // No overload for method 'OnlyVarargs' takes '0' arguments
            // Localized forms should contain the name and count.
            Assert.Contains("OnlyVarargs", errorMessage);
            Assert.Contains("0", errorMessage);
            errorMessage = Assert.Throws<RuntimeBinderException>(() => d.OnlyVarargs(1)).Message;
            // "The best overloaded method match for 'Microsoft.CSharp.RuntimeBinder.Tests.VarArgsTests.HasVarargs.OnlyVarargs(__arglist)' has some invalid arguments"
            // Localized form should contain the name,
            Assert.Contains("Microsoft.CSharp.RuntimeBinder.Tests.VarArgsTests.HasVarargs.OnlyVarargs(__arglist)", errorMessage);
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void CorrectNullaryOverload()
        {
            dynamic d = new HasVarargs();
            Assert.Equal(1, d.Nullary());
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void CorrectUnaryOverload()
        {
            dynamic d = new HasVarargs();
            Assert.Equal(32, d.Unary0(32));
        }

        [Fact]
        public void CorrectUnaryOverloadNeedingImplicitConversion()
        {
            dynamic d = new HasVarargs();
            Assert.Equal(9392, d.Unary1(9392));
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void CorrectBinaryOverload()
        {
            dynamic d = new HasVarargs();
            Assert.Equal(7, d.Binary(3, 4));
            Assert.Equal(8, d.Binary((byte)2, (byte)6));
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void CorrectCtor()
        {
            dynamic d = 19;
            VarArgCtorOption vars = new VarArgCtorOption(d);
            Assert.Equal(19, vars.Value);
        }
    }
}
