// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class AsyncMethodBuilderOverrideAttributeTests
    {
        [Theory]
        [InlineData(typeof(AsyncTaskMethodBuilder))]
        [InlineData(typeof(AsyncTaskMethodBuilder<>))]
        [InlineData(typeof(AsyncTaskMethodBuilder<int>))]
        [InlineData(typeof(AsyncTaskMethodBuilder<string>))]
        [InlineData(typeof(AsyncValueTaskMethodBuilder))]
        [InlineData(typeof(AsyncValueTaskMethodBuilder<>))]
        [InlineData(typeof(AsyncValueTaskMethodBuilder<int>))]
        [InlineData(typeof(AsyncValueTaskMethodBuilder<string>))]
        [InlineData(typeof(PoolingAsyncValueTaskMethodBuilder))]
        [InlineData(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        [InlineData(typeof(PoolingAsyncValueTaskMethodBuilder<int>))]
        [InlineData(typeof(PoolingAsyncValueTaskMethodBuilder<string>))]
        public void Ctor_BuilderType_Roundtrip(Type builderType)
        {
            var amba = new AsyncMethodBuilderOverrideAttribute(builderType);
            Assert.Same(builderType, amba.BuilderType);
        }
    }
}
