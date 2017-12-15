// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace Tests.Integration
{
    public class CompositionContainerAttributedModelCycleTests
    {
        // There are nine possible scenarios that cause a part to have a dependency on another part, some of which
        // are legal and some not. For example, below, is not legal for a part, A, to have a prerequisite dependency
        // on a part, B, which has also has a prerequisite dependency on A. In contrast, however, it is legal for 
        // part A and B to have a non-prerequisite (Post) dependency on each other.
        // 
        // ------------------------------
        // |        |         B         |
        // |        | Pre | Post | None |
        // |--------|-----|------|------|
        // |   Pre  |  X  |    X |    v |
        // | A Post |  X  |    v |    v |
        // |   None |  v  |    v |    v |
        // ------------------------------
        //

        [Fact]
        public void APrerequisiteDependsOnBPrerequisite_ShouldThrowComposition()
        {
            AssertCycle(typeof(APrerequisiteDependsOnBPrerequisite),
                        typeof(BPrerequisiteDependsOnAPrerequisite));
        }

        [Fact]
        public void APrerequisiteDependsOnBPost_ShouldThrowComposition()
        {
            AssertCycle(typeof(APrerequisiteDependsOnBPost),
                        typeof(BPostDependsOnAPrerequisite));
        }

        [Fact]
        [ActiveIssue(25498)]
        public void APrerequisiteDependsOnBNone_ShouldNotThrow()
        {
            AssertNotCycle(typeof(APrerequisiteDependsOnBNone),
                           typeof(BNone));
        }

        [Fact]
        public void APostDependsOnBPrerequisite_ShouldThrowComposition()
        {
            AssertCycle(typeof(APostDependsOnBPrerequisite),
                        typeof(BPrerequisiteDependsOnAPost));
        }

        [Fact]
        [ActiveIssue(25498)]
        public void APostDependsOnBPost_ShouldNotThrow()
        {
            AssertNotCycle(typeof(APostDependsOnBPost),
                           typeof(BPostDependsOnAPost));
        }

        [Fact]
        [ActiveIssue(25498)]
        public void APostDependsOnBNone_ShouldNotThrow()
        {
            AssertNotCycle(typeof(APostDependsOnBNone),
                           typeof(BNone));
        }

        [Fact]
        [ActiveIssue(25498)]
        public void BPrerequisiteDependsOnANone_ShouldNotThrow()
        {
            AssertNotCycle(typeof(ANone),
                           typeof(BPrerequisiteDependsOnANone));
        }

        [Fact]
        [ActiveIssue(25498)]
        public void BPostDependsOnANone_ShouldNotThrow()
        {
            AssertNotCycle(typeof(ANone),
                           typeof(BPostDependsOnANone));
        }

        [Fact]
        [ActiveIssue(25498)]
        public void ANoneWithBNone_ShouldNotThrow()
        {
            AssertNotCycle(typeof(ANone),
                           typeof(BNone));
        }

        [Fact]
        [ActiveIssue(25498)]
        public void PartWithHasPrerequisteImportThatIsInAPostCycle_ShouldNotThrow()
        {
            AssertNotCycle(typeof(PartWithHasPrerequisteImportThatIsInAPostCycle)
                , typeof(APostDependsOnBPost), typeof(BPostDependsOnAPost));
        }

        private static void AssertCycle(params Type[] types)
        {
            foreach (Type type in types)
            {
                var export = GetExport(type, types);

                CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
                {
                    var value = export.Value;
                });
            }
        }

        private static void AssertNotCycle(params Type[] types)
        {
            foreach (Type type in types)
            {
                var export = GetExport(type, types);

                Assert.IsType<Type>(export.Value);
            }
        }

        private static Lazy<object, object> GetExport(Type type, Type[] partTypes)
        {
            var container = ContainerFactory.CreateWithAttributedCatalog(partTypes);

            return container.GetExports(type, null, null).Single();
        }

        [Export]
        public class APrerequisiteDependsOnBPrerequisite
        {
            [ImportingConstructor]
            public APrerequisiteDependsOnBPrerequisite(BPrerequisiteDependsOnAPrerequisite b)
            {
            }
        }

        [Export]
        public class BPrerequisiteDependsOnAPrerequisite
        {
            [ImportingConstructor]
            public BPrerequisiteDependsOnAPrerequisite(APrerequisiteDependsOnBPrerequisite a)
            {
            }
        }

        [Export]
        public class APrerequisiteDependsOnBPost
        {
            [ImportingConstructor]
            public APrerequisiteDependsOnBPost(BPostDependsOnAPrerequisite b)
            {
            }
        }

        [Export]
        public class BPostDependsOnAPrerequisite
        {
            [Import]
            public APrerequisiteDependsOnBPost A
            {
                get;
                set;
            }
        }

        [Export]
        public class APrerequisiteDependsOnBNone
        {
            [ImportingConstructor]
            public APrerequisiteDependsOnBNone(BNone b)
            {
            }
        }

        [Export]
        public class BNone
        {
        }

        [Export]
        public class ANone
        {
        }

        [Export]
        public class APostDependsOnBPrerequisite
        {
            [Import]
            public BPrerequisiteDependsOnAPost B
            {
                get;
                set;
            }
        }

        [Export]
        public class BPrerequisiteDependsOnAPost
        {
            [ImportingConstructor]
            public BPrerequisiteDependsOnAPost(APostDependsOnBPrerequisite a)
            {
            }
        }

        [Export]
        public class APostDependsOnBPost
        {
            [Import]
            public BPostDependsOnAPost B
            {
                get;
                set;
            }
        }

        [Export]
        public class BPostDependsOnAPost
        {
            [Import]
            public APostDependsOnBPost A
            {
                get;
                set;
            }
        }

        [Export]
        public class APostDependsOnBNone
        {
            [Import]
            public BNone B
            {
                get;
                set;
            }
        }

        [Export]
        public class BPrerequisiteDependsOnANone
        {
            [ImportingConstructor]
            public BPrerequisiteDependsOnANone(ANone a)
            {
            }
        }

        [Export]
        public class BPostDependsOnANone
        {
            [Import]
            public ANone A
            {
                get;
                set;
            }
        }

        [Export]
        public class PartWithHasPrerequisteImportThatIsInAPostCycle
        {
            [ImportingConstructor]
            public PartWithHasPrerequisteImportThatIsInAPostCycle(APostDependsOnBPost a)
            {
            }
        }
    }
}
