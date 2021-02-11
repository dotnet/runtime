// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Factories;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.Primitives
{
    public class ComposablePartCatalogDebuggerProxyTests
    {
        [Fact]
        public void Constructor_NullAsCatalogArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("catalog", () =>
            {
                new ComposablePartCatalogDebuggerProxy((ComposablePartCatalog)null);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void Constructor_ValueAsCatalogArgument_ShouldSetPartsProperty()
        {
            var expectations = Expectations.GetCatalogs();
            foreach (var e in expectations)
            {
                var proxy = new ComposablePartCatalogDebuggerProxy(e);

                EqualityExtensions.CheckEquals(e.Parts, proxy.Parts);
            }
        }
   }
}
