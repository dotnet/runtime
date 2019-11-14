// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionServiceExtensionsTests
    {
        [Fact]
        public void SatisfyImports_BooleanOverride_PartAndFalseHaveBeenPassed()
        {
            MockCompositionService compositionService = new MockCompositionService();
            ComposablePart part = PartFactory.Create();

            bool importsSatisfiedCalled = false;
            compositionService.ImportsSatisfied += delegate (object sender, SatisfyImportsEventArgs e)
            {
                Assert.False(importsSatisfiedCalled);
                Assert.Equal(part, e.Part);
                importsSatisfiedCalled = true;
            };

            compositionService.SatisfyImportsOnce(part);
            Assert.True(importsSatisfiedCalled);
        }

        [Fact]
        public void SatisfyImports_AttributedOverride_NullAsCompositionService()
        {
            ICompositionService compositionService = null;
            Assert.Throws<ArgumentNullException>("compositionService", () =>
            {
                compositionService.SatisfyImportsOnce(new MockAttributedPart());
            });
        }

        [Fact]
        public void SatisfyImports_AttributedOverride_NullAsAttributedPart()
        {
            MockCompositionService compositionService = new MockCompositionService();
            Assert.Throws<ArgumentNullException>("attributedPart", () =>
            {
                compositionService.SatisfyImportsOnce((object)null);
            });
        }

        [Fact]
        public void SatisfyImports_AttributedOverride_PartAndFalseHaveBeenPassed()
        {
            MockCompositionService compositionService = new MockCompositionService();
            object attributedPart = new MockAttributedPart();

            bool importsSatisfiedCalled = false;
            compositionService.ImportsSatisfied += delegate (object sender, SatisfyImportsEventArgs e)
            {
                Assert.False(importsSatisfiedCalled);
                Assert.True(e.Part is ReflectionComposablePart);
                Assert.True(((ReflectionComposablePart)e.Part).Definition.GetPartType() == typeof(MockAttributedPart));
                importsSatisfiedCalled = true;
            };

            compositionService.SatisfyImportsOnce(attributedPart);
            Assert.True(importsSatisfiedCalled);
        }

        [Fact]
        public void SatisfyImports_AttributedAndBooleanOverride_NullAsCompositionService()
        {
            ICompositionService compositionService = null;
            Assert.Throws<ArgumentNullException>("compositionService", () =>
            {
                compositionService.SatisfyImportsOnce(new MockAttributedPart());
            });
        }

        [Fact]
        public void SatisfyImports_AttributedAndBooleanOverride_NullAsAttributedPart()
        {
            MockCompositionService compositionService = new MockCompositionService();
            Assert.Throws<ArgumentNullException>("attributedPart", () =>
            {
                compositionService.SatisfyImportsOnce((object)null);
            });
        }

        [Fact]
        public void SatisfyImports_AttributedAndBooleanOverride_PartAndFalseHaveBeenPassed()
        {
            MockCompositionService compositionService = new MockCompositionService();
            object attributedPart = new MockAttributedPart();

            bool importsSatisfiedCalled = false;
            compositionService.ImportsSatisfied += delegate (object sender, SatisfyImportsEventArgs e)
            {
                Assert.False(importsSatisfiedCalled);
                Assert.True(e.Part is ReflectionComposablePart);
                Assert.True(((ReflectionComposablePart)e.Part).Definition.GetPartType() == typeof(MockAttributedPart));
                importsSatisfiedCalled = true;
            };

            compositionService.SatisfyImportsOnce(attributedPart);
            Assert.True(importsSatisfiedCalled);
        }

        internal class SatisfyImportsEventArgs : EventArgs
        {
            public SatisfyImportsEventArgs(ComposablePart part)
            {
                this.Part = part;
            }

            public ComposablePart Part { get; private set; }
        }

        internal class MockCompositionService : ICompositionService
        {
            public MockCompositionService()
            {
            }

            public void SatisfyImportsOnce(ComposablePart part)
            {
                if (this.ImportsSatisfied != null)
                {
                    this.ImportsSatisfied.Invoke(this, new SatisfyImportsEventArgs(part));
                }
            }

            public event EventHandler<SatisfyImportsEventArgs> ImportsSatisfied;
        }

        public class MockAttributedPart
        {
            public MockAttributedPart()
            {
            }
        }
    }
}
