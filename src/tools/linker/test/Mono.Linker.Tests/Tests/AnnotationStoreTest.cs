// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;
using NUnit.Framework;

namespace Mono.Linker.Tests.Tests
{
    public class AnnotationStoreTest
    {
        AnnotationStore store;

        [SetUp]
        public void Setup()
        {
            var ctx = new LinkContext(null, new ConsoleLogger(), string.Empty);
            store = new AnnotationStore(ctx);
        }

        [Test]
        public void CustomAnnotations()
        {
            var td = new TypeDefinition("ns", "name", TypeAttributes.Public);

            Assert.IsNull(store.GetCustomAnnotation("k", td));

            store.SetCustomAnnotation("k", td, "value");
            Assert.AreEqual("value", store.GetCustomAnnotation("k", td));
        }
    }
}
