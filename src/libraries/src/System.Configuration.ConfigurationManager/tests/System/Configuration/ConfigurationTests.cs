// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using System.IO;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConfigurationTests
    {
        [Fact]
        public void UnspecifiedTypeStringTransformer()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.Null(config.TypeStringTransformer);
            }
        }

        [Fact]
        public void SpecifiedTypeStringTransformerReturned()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Func<string, string> transformer = s => s;
                config.TypeStringTransformer = transformer;
                Assert.Same(transformer, config.TypeStringTransformer);
            }
        }

        [Fact]
        public void UnspecifiedAssemblyStringTransformer()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Assert.Null(config.AssemblyStringTransformer);
            }
        }

        [Fact]
        public void SpecifiedAssemblyStringTransformerReturned()
        {
            using (var temp = new TempConfig(TestData.EmptyConfig))
            {
                var config = ConfigurationManager.OpenExeConfiguration(temp.ExePath);
                Func<string, string> transformer = s => s;
                config.AssemblyStringTransformer = transformer;
                Assert.Same(transformer, config.AssemblyStringTransformer);
            }
        }
    }
}
