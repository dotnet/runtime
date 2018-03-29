// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ConstraintTests : ContainerTests
    {
        public interface IThing { }
        public interface IUnrelatedThings<TC, TP> : IList<TC>, IThing { }
        public interface IInheritedThings<TC, TP> : IList<TC>, IThing where TC : TP { }
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

        [Export(typeof(IInheritedThings<,>))]
        public class InheritedThings<TC, TP> : ObservableCollection<TC>, IInheritedThings<TC, TP> 
            where TC : TP
        {
        }

        [Export(typeof(IUnrelatedThings<,>))]
        public class UnrelatedThings<TC, TP> : ObservableCollection<TC>, IUnrelatedThings<TC, TP>
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

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void GetExport_ComplexConstraint_ExportSuccessful()
        {
            CompositionContext container = CreateContainer(typeof(UnrelatedThings<,>));
            var exports = container.GetExports<IUnrelatedThings<IBook, ICar>>();
            var types = exports.Select(h => h.GetType());

            Assert.Equal(1, exports.Count());
            Assert.Contains(typeof(UnrelatedThings<IBook, ICar>), types);
        }

        [Fact]
        [ActiveIssue(23607)]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void GetExport_WhereClause_ExportSuccessful()
        {
            CompositionContext container = CreateContainer(typeof(InheritedThings<,>));
            var exports = container.GetExports<IInheritedThings<IBook, IThing>>();
            var types = exports.Select(h => h.GetType());

            Assert.Equal(1, exports.Count());
            Assert.Contains(typeof(InheritedThings<IBook, IThing>), types);
        }
    }
}
