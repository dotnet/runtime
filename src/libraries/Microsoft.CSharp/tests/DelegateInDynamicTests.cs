// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class DelegateInDynamicTests
    {
        [Fact]
        public void DelegateInDynamicExplicitInvoke()
        {
            Func<int, int> doubleIt = x => x * 2;
            dynamic d = doubleIt;
            int result = d.Invoke(9);
            Assert.Equal(18, result);
        }

        [Fact]
        public void DelegateInDynamicImplicitInvoke()
        {
            Func<int, int> doubleIt = x => x * 2;
            dynamic d = doubleIt;
            int result = d(9);
            Assert.Equal(18, result);
        }

        [Fact]
        public void DelegateInDynamicExplicitInvokeWithBadArgument()
        {
            Func<int, int> doubleIt = x => x * 2;
            dynamic d = doubleIt;
            Assert.Throws<RuntimeBinderException>(() => d.Invoke("nine"));
        }

        [Fact]
        public void DelegateInDynamicImplicitInvokeWithBadArgument()
        {
            Func<int, int> doubleIt = x => x * 2;
            dynamic d = doubleIt;
            Assert.Throws<RuntimeBinderException>(() => d("nine"));
        }

        delegate void ActionWithOut<in TIn, TOut>(TIn input, out TOut output);

        [Fact]
        public void DelegateWithOutParameterInDynamic()
        {
            ActionWithOut<int, string> act = (int input, out string output) => output = input.ToString();
            dynamic d = act;
            d(23, out string res);
            Assert.Equal("23", res);
        }

        [Fact]
        public void DelegateWithOutParametersInDynamicNamedArgumentInvocation()
        {
            ActionWithOut<int, string> act = (int input, out string output) => output = input.ToString();
            dynamic d = act;
            d(output: out string res, input: 23);
            Assert.Equal("23", res);
        }
    }
}
