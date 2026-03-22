// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

#nullable enable

namespace Wasm.Build.Tests
{
    /// <summary>
    /// Example usage:
    ///     [BuildAndRun(aot: true)]
    ///     public void Test(ProjectInfo, RunHost, id)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class BuildAndRunAttribute : DataAttribute
    {
        private readonly List<ITheoryDataRow> _data;

#if TARGET_WASI
        // remove when wasi is refactored and use Configuration
        public BuildAndRunAttribute(bool aot=false, string? config=null, params object?[] parameters)
        {
            _data = BuildTestBase.ConfigWithAOTData(aot, config)
                    .Multiply(parameters)
                    .UnwrapItemsAsArrays()
                    .Select(row => (ITheoryDataRow)new TheoryDataRow(row))
                    .ToList();
        }
#else
        public BuildAndRunAttribute(bool aot=false, Configuration config=Configuration.Undefined, params object?[] parameters)
        {
            _data = BuildTestBase.ConfigWithAOTData(aot, config)
                    .Multiply(parameters)
                    .UnwrapItemsAsArrays()
                    .Select(row => (ITheoryDataRow)new TheoryDataRow(row))
                    .ToList();
        }
#endif

        public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
            => new(_data);

        public override bool SupportsDiscoveryEnumeration() => true;
    }
}
