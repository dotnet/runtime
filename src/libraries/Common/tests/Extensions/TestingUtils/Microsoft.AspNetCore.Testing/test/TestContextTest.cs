// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class TestContextTest : ITestMethodLifecycle
    {
        public TestContext Context { get; private set; }

        [Fact]
        public void FullName_IsUsed_ByDefault()
        {
            Assert.Equal(GetType().FullName, Context.FileOutput.TestClassName);
        }

        Task ITestMethodLifecycle.OnTestStartAsync(TestContext context, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.CompletedTask;
        }

        Task ITestMethodLifecycle.OnTestEndAsync(TestContext context, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

namespace Microsoft.AspNetCore.Testing.Tests
{
    public class TestContextNameShorteningTest : ITestMethodLifecycle
    {
        public TestContext Context { get; private set; }

        [Fact]
        public void NameIsShortenedWhenAssemblyNameIsAPrefix()
        {
            Assert.Equal(GetType().Name, Context.FileOutput.TestClassName);
        }

        Task ITestMethodLifecycle.OnTestStartAsync(TestContext context, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.CompletedTask;
        }

        Task ITestMethodLifecycle.OnTestEndAsync(TestContext context, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}

namespace Microsoft.AspNetCore.Testing
{
    [ShortClassName]
    public class TestContextTestClassShortNameAttributeTest : ITestMethodLifecycle
    {
        public TestContext Context { get; private set; }

        [Fact]
        public void ShortClassNameUsedWhenShortClassNameAttributeSpecified()
        {
            Assert.Equal(GetType().Name, Context.FileOutput.TestClassName);
        }

        Task ITestMethodLifecycle.OnTestStartAsync(TestContext context, CancellationToken cancellationToken)
        {
            Context = context;
            return Task.CompletedTask;
        }

        Task ITestMethodLifecycle.OnTestEndAsync(TestContext context, Exception exception, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
