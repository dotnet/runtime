// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Composition.Hosting.Core.Tests
{
    public class CompositionOperationTests
    {
        [Fact]
        public void Run_ValidContextAndAction_ReturnsExpected()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                var results = new List<string>();
                void NonPrequisiteAction1() => results.Add("NonPrequisiteAction1");
                void NonPrequisiteAction2() => results.Add("NonPrequisiteAction2");

                void PostCompositionAction1() => results.Add("PostCompositionAction1");
                void PostCompositionAction2() => results.Add("PostCompositionAction2");

                object Activator(LifetimeContext activatorContext, CompositionOperation activatorOperation)
                {
                    Assert.Same(context, activatorContext);

                    activatorOperation.AddNonPrerequisiteAction(NonPrequisiteAction1);
                    activatorOperation.AddNonPrerequisiteAction(NonPrequisiteAction2);

                    activatorOperation.AddPostCompositionAction(PostCompositionAction1);
                    activatorOperation.AddPostCompositionAction(PostCompositionAction2);

                    return "Hi";
                }

                Assert.Equal("Hi", CompositionOperation.Run(context, Activator));
                Assert.Equal(new string[] { "NonPrequisiteAction1", "NonPrequisiteAction2", "PostCompositionAction1", "PostCompositionAction2" }, results);
            }
        }

        [Fact]
        public void Run_NullOutmostContext_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("outermostLifetimeContext", () => CompositionOperation.Run(null, Activator));
        }

        [Fact]
        public void Run_NullActivator_ThrowsArgumentNullException()
        {
            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                AssertExtensions.Throws<ArgumentNullException>("compositionRootActivator", () => CompositionOperation.Run(context, null));
            }
        }

        [Fact]
        public void AddNonPrequisiteAction_NullAction_ThrowsArgumentNullException()
        {
            object Activator(LifetimeContext context, CompositionOperation operation)
            {
                AssertExtensions.Throws<ArgumentNullException>("action", () => operation.AddNonPrerequisiteAction(null));
                return null;
            }

            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                CompositionOperation.Run(context, Activator);
            }
        }

        [Fact]
        public void AddPostCompositionAction_NullAction_ThrowsArgumentNullException()
        {
            object Activator(LifetimeContext context, CompositionOperation operation)
            {
                AssertExtensions.Throws<ArgumentNullException>("action", () => operation.AddPostCompositionAction(null));
                return null;
            }

            using (CompositionHost host = CompositionHost.CreateCompositionHost(new ExportDescriptorProvider[0]))
            {
                Assert.True(host.TryGetExport(new CompositionContract(typeof(CompositionContext)), out object export));
                LifetimeContext context = Assert.IsType<LifetimeContext>(export);

                CompositionOperation.Run(context, Activator);
            }
        }

        private static object Activator(LifetimeContext context, CompositionOperation operation) => null;
    }
}
