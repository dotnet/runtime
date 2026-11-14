// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class DelegateInDynamicTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData((short)0)]
        [InlineData((short)7)]
        public void ClosedStaticExpressionDelegateWithNullableArgument(short? value)
        {
            int bias = 37;
            Expression<Func<short?, int>> expression = argument => argument.GetValueOrDefault() + bias;
            Func<short?, int> compiled = expression.Compile();
            Func<short?, int> openStatic = ReadNullable;
            var holder = new NullableDelegateTarget<string>("target");
            Func<short?, int> closedInstance = holder.Read;

            Assert.Equal(value.GetValueOrDefault() + bias, compiled(value));
            Assert.Equal(value.GetValueOrDefault() + 11, openStatic(value));
            Assert.Equal(value.GetValueOrDefault() + 6, closedInstance(value));
        }

        private static int ReadNullable(short? value) => value.GetValueOrDefault() + 11;

        private sealed class NullableDelegateTarget<T>
        {
            private readonly T _target;

            public NullableDelegateTarget(T target) => _target = target;

            public int Read(short? value) => value.GetValueOrDefault() + _target.ToString().Length;
        }

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
