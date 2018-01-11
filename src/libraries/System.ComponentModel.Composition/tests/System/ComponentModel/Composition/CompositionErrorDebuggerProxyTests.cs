// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionErrorDebuggerProxyTests
    {
        [Fact]
        public void Constructor_NullAsErrorArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("error", () =>
            {
                new CompositionErrorDebuggerProxy((CompositionError)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsErrorArgument_ShouldSetExceptionProperty()
        {
            var expectations = Expectations.GetInnerExceptionsWithNull();

            foreach (var e in expectations)
            {
                var error = ErrorFactory.Create(e);

                var proxy = new CompositionErrorDebuggerProxy(error);

                Assert.Same(error.Exception, proxy.Exception);
            }            
        }

        [Fact]
        public void Constructor_ValueAsErrorArgument_ShouldSetMessageProperty()
        {
            var expectations = Expectations.GetExceptionMessages();

            foreach (var e in expectations)
            {
                var error = ErrorFactory.Create(e);

                var proxy = new CompositionErrorDebuggerProxy(error);

                Assert.Same(error.Description, proxy.Description);
            }
        }

        [Fact]
        public void Constructor_ValueAsErrorArgument_ShouldSetElementProperty()
        {
            var expectations = Expectations.GetCompositionElementsWithNull();

            foreach (var e in expectations)
            {
                var error = ErrorFactory.Create(e);

                var proxy = new CompositionErrorDebuggerProxy(error);

                Assert.Same(error.Element, proxy.Element);
            }
        }

   }
}
