// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using Xunit;

namespace System.Composition.Tests
{
    public class CompositionContextTests
    {
        [Theory]
        [InlineData(true, null)]
        [InlineData(false, null)]
        [InlineData(true, "contractName")]
        [InlineData(false, "contractName")]
        public void GetExport_Invoke_ReturnsExpected(bool success, string contractName)
        {
            var context = new SubContext(contract =>
            {
                Assert.Equal(typeof(int), contract.ContractType);
                Assert.Equal(contractName, contract.ContractName);
                Assert.Null(contract.MetadataConstraints);

                return (success, 10);
            });
            if (success)
            {
                if (contractName == null)
                {
                    Assert.Equal(10, context.GetExport<int>());
                    Assert.Equal(10, context.GetExport(typeof(int)));

                    Assert.True(context.TryGetExport(out int export1));
                    Assert.Equal(10, export1);

                    Assert.True(context.TryGetExport(typeof(int), out object export2));
                    Assert.Equal(10, export2);
                }
                else
                {
                    Assert.Equal(10, context.GetExport<int>(contractName));
                    Assert.Equal(10, context.GetExport(typeof(int), contractName));

                    Assert.True(context.TryGetExport(contractName, out int export1));
                    Assert.Equal(10, export1);

                    Assert.True(context.TryGetExport(typeof(int), contractName, out object export2));
                    Assert.Equal(10, export2);
                }
            }
            else
            {
                if (contractName == null)
                {
                    Assert.Throws<CompositionFailedException>(() => context.GetExport<int>());
                    Assert.Throws<CompositionFailedException>(() => context.GetExport(typeof(int)));

                    Assert.False(context.TryGetExport(out int export1));
                    Assert.Equal(0, export1);

                    // Failure leaks through.
                    Assert.False(context.TryGetExport(typeof(int), out object export2));
                    Assert.Equal(10, export2);
                }
                else
                {
                    Assert.Throws<CompositionFailedException>(() => context.GetExport<int>(contractName));
                    Assert.Throws<CompositionFailedException>(() => context.GetExport(typeof(int), contractName));

                    Assert.False(context.TryGetExport(contractName, out int export1));
                    Assert.Equal(0, export1);

                    // Failure leaks through.
                    Assert.False(context.TryGetExport(typeof(int), contractName, out object export2));
                    Assert.Equal(10, export2);
                }
            }
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(false, null)]
        [InlineData(true, "contractName")]
        [InlineData(false, "contractName")]
        public void GetExports_Invoke_ReturnsExpected(bool success, string contractName)
        {
            var context = new SubContext(contract =>
            {
                Assert.Equal(typeof(object[]), contract.ContractType);
                Assert.Equal(contractName, contract.ContractName);
                Assert.Equal(new Dictionary<string, object> { { "IsImportMany", true } }, contract.MetadataConstraints);

                return (success, new object[] { 10 });
            });
            if (success)
            {

                if (contractName == null)
                {
                    Assert.Equal(new object[] { 10 }, context.GetExports<object>());
                    Assert.Equal(new object[] { 10 }, context.GetExports(typeof(object)));
                }
                else
                {
                    Assert.Equal(new object[] { 10 }, context.GetExports<object>(contractName));
                    Assert.Equal(new object[] { 10 }, context.GetExports(typeof(object), contractName));
                }
            }
            else
            {
                if (contractName == null)
                {
                    Assert.Throws<CompositionFailedException>(() => context.GetExports<object>());
                    Assert.Throws<CompositionFailedException>(() => context.GetExports(typeof(object)));
                }
                else
                {
                    Assert.Throws<CompositionFailedException>(() => context.GetExports<object>(contractName));
                    Assert.Throws<CompositionFailedException>(() => context.GetExports(typeof(object), contractName));
                }
            }
        }

        public class SubContext : CompositionContext
        {
            public Func<CompositionContract, (bool, object)> Handler { get; }

            public SubContext(Func<CompositionContract, (bool, object)> handler) => Handler = handler;

            public override bool TryGetExport(CompositionContract contract, out object export)
            {
                (bool result, object exportResult) = Handler(contract);
                export = exportResult;
                return result;
            }
        }
    }
}
