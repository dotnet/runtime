// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Reflection.Metadata.Tests.Metadata
{
    public class HandleCollectionTests
    {
        [Fact(Skip = "subject to design change")]
        public void CollectionsCreatedFromDefaultDefinitionsThrow()
        {
            Assert.Throws<InvalidOperationException>(() => default(MethodDefinition).GetCustomAttributes());
            Assert.Throws<InvalidOperationException>(() => default(TypeDefinition).GetMethods());
            Assert.Throws<InvalidOperationException>(() => default(TypeDefinition).GetFields());
            Assert.Throws<InvalidOperationException>(() => default(TypeDefinition).GetProperties());
            Assert.Throws<InvalidOperationException>(() => default(TypeDefinition).GetEvents());
            Assert.Throws<InvalidOperationException>(() => default(AssemblyDefinition).GetDeclarativeSecurityAttributes());
        }
    }
}
