// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Xunit;

namespace Mono.Linker.Tests.Tests
{
    public class AnnotationStoreTest
    {
        readonly AnnotationStore store;

        public AnnotationStoreTest()
        {
            var ctx = new LinkContext(null, new ConsoleLogger(), string.Empty);
            store = new AnnotationStore(ctx);
        }

        [Fact]
        public void CustomAnnotations()
        {
            var td = new TypeDefinition("ns", "name", TypeAttributes.Public);

            Assert.Null(store.GetCustomAnnotation("k", td));

            store.SetCustomAnnotation("k", td, "value");
            Assert.Equal("value", store.GetCustomAnnotation("k", td));
        }
    }
}
