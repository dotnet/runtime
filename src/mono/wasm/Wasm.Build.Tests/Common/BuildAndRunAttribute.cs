// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    /// <summary>
    /// Example usage:
    ///     [BuildAndRun(aot: true, parameters: new object[] { arg1, arg2 })]
    ///     public void Test(ProjectInfo, arg1, arg2, RunHost, id)
    /// </summary>
    [DataDiscoverer("Xunit.Sdk.DataDiscoverer", "xunit.core")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class BuildAndRunAttribute : DataAttribute
    {
        private readonly IEnumerable<object?[]> _data;

        public BuildAndRunAttribute(bool aot=false, Configuration? config=null, params object?[] parameters)
        {
            _data = BuildTestBase.ConfigWithAOTData(aot, config)
                    .Multiply(parameters)
                    .UnwrapItemsAsArrays().ToList();
        }

        public override IEnumerable<object?[]> GetData(MethodInfo testMethod) => _data;
    }
}
