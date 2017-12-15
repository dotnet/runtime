// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition.Hosting
{
    public class AssemblyCatalogDebuggerProxyTests
    {
        [Fact]
        [ActiveIssue(25498)]
        public void Constructor_NullAsCatalogArgument_ShouldThrowArgumentNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("catalog", () =>
            {
                new AssemblyCatalogDebuggerProxy((AssemblyCatalog)null);
            });
        }

        [Fact]
        [ActiveIssue(25498, TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void Constructor_ValueAsCatalogArgument_ShouldSetPartsProperty()
        {
            var expectations = Expectations.GetAssemblies();

            foreach (var e in expectations)
            {
                var catalog = CreateAssemblyCatalog(e);

                var proxy = new AssemblyCatalogDebuggerProxy(catalog);
                
                EqualityExtensions.CheckSequenceEquals(catalog.Parts, proxy.Parts);
            }
        }

        [Fact]
        public void Constructor_ValueAsCatalogArgument_ShouldSetAssemblyProperty()
        {
            var expectations = Expectations.GetAssemblies();

            foreach (var e in expectations)
            {
                var catalog = CreateAssemblyCatalog(e);

                var proxy = new AssemblyCatalogDebuggerProxy(catalog);

                Assert.Same(catalog.Assembly, proxy.Assembly);
            }
        }

        private AssemblyCatalogDebuggerProxy CreateAssemblyDebuggerProxy(AssemblyCatalog catalog)
        {
            return new AssemblyCatalogDebuggerProxy(catalog);
        }

        private AssemblyCatalog CreateAssemblyCatalog(Assembly assembly)
        {
            return new AssemblyCatalog(assembly);
        }
   }
}
