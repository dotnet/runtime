// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;

namespace Mono.Linker.Tests.Tests
{
    [TestClass]
    public class AnnotationStoreTest
    {
        AnnotationStore store;

        [TestInitialize]
        public void Setup()
        {
            var ctx = new LinkContext(null, new ConsoleLogger(), string.Empty);
            store = new AnnotationStore(ctx);
        }

        [TestMethod]
        public void CustomAnnotations()
        {
            var td = new TypeDefinition("ns", "name", TypeAttributes.Public);

            Assert.IsNull(store.GetCustomAnnotation("k", td));

            store.SetCustomAnnotation("k", td, "value");
            Assert.AreEqual("value", store.GetCustomAnnotation("k", td));
        }
    }
}
