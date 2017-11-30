// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ExportProviderTests
    {
        [Fact]
        public void GetExports2_NullAsDefinitionArgument_ShouldThrowArgumentNull()
        {
            var provider = ExportProviderFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                provider.GetExports((ImportDefinition)null);                
            });
        }

        [Fact]
        public void TryGetExports_NullAsDefinitionArgument_ShouldThrowArgumentNull()
        {
            var provider = ExportProviderFactory.Create();

            Assert.Throws<ArgumentNullException>(() =>
            {
                IEnumerable<Export> exports;
                provider.TryGetExports((ImportDefinition)null, null, out exports);
            });
        }

        [Fact]
        public void TryGetExports_NullAsDefinitionArgument_ShouldNotSetExportsArgument()
        {
            var provider = ExportProviderFactory.Create();

            IEnumerable<Export> exports = new Export[0];
            IEnumerable<Export> results = exports;
            
            Assert.Throws<ArgumentNullException>(() =>
            {
                provider.TryGetExports((ImportDefinition)null, null, out results);
            });

            Assert.Same(exports, results);
        }
    }
}
