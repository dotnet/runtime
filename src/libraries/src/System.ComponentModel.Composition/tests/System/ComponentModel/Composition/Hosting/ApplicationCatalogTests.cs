// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ApplicationCatalogTests
    {
        // This is a glorious do nothing ReflectionContext
        public class ApplicationCatalogTestsReflectionContext : ReflectionContext
        {
            public override Assembly MapAssembly(Assembly assembly)
            {
                return assembly;
            }

            public override TypeInfo MapType(TypeInfo type)

            {
                return type;
            }
        }

        public class Worker : MarshalByRefObject
        {
            internal void DoWork(Action work)
            {
                work();
            }
        }

        [Fact]
        public void Constructor1_NullReflectionContextArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("reflectionContext", () =>
            {
                new ApplicationCatalog((ReflectionContext)null);
            });
        }

        [Fact]
        public void Constructor3_NullBothArguments_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("reflectionContext", () =>
            {
                new ApplicationCatalog((ReflectionContext)null, (ICompositionElement)null);
            });
        }

        [Fact]
        public void Constructor2_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("definitionOrigin", () =>
            {
                new ApplicationCatalog((ICompositionElement)null);
            });
        }

        [Fact]
        public void Constructor3_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("definitionOrigin", () =>
            {
                new ApplicationCatalog((ICompositionElement)null);
            });
        }

        [Fact]
        public void ExecuteOnCreationThread()
        {
            // Add a proper test for event notification on caller thread
        }
    }
}
