// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ConstraintTests : ContainerTests
    {
        public interface IThing { }
        public interface ICar : IThing { }
        public interface IBook : IThing { }
        public interface IHandler<T> where T : IThing { }

        [Export(typeof(IHandler<>))]
        public class ThingHandler<T> : IHandler<T>
            where T : IThing
        {
        }

        [Export(typeof(IHandler<>))]
        public class BookHandler<T> : IHandler<T>
            where T : IBook
        {
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void GenericPartDiscoveryIgnoresAPartAndDoesntThrowAnExceptionWhenItsConstraintOnTypeParameterIsNotAssignableFromTheExportTarget()
        {
            var container = CreateContainer(typeof(ThingHandler<>), typeof(BookHandler<>));
            var carHandlers = container.GetExports<IHandler<ICar>>();
            var handlerTypes = carHandlers.Select(h => h.GetType());

            Assert.Equal(1, carHandlers.Count());
            Assert.Contains<Type>(typeof(ThingHandler<ICar>), handlerTypes);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void GenericPartDiscoveryIncludesAPartWhenItsConstraintOnTypeParameterIsAssignableFromTheExportTarget()
        {
            var container = CreateContainer(typeof(ThingHandler<>), typeof(BookHandler<>));
            var bookHandlers = container.GetExports<IHandler<IBook>>();
            var handlerTypes = bookHandlers.Select(h => h.GetType());

            Assert.Equal(2, bookHandlers.Count());
            Assert.Contains<Type>(typeof(ThingHandler<IBook>), handlerTypes);
            Assert.Contains<Type>(typeof(BookHandler<IBook>), handlerTypes);
        }
    }
}
