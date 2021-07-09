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
    ///     public void Test(BuildArgs, arg1, arg2, RunHost, id)
    /// </summary>
    [DataDiscoverer("Xunit.Sdk.DataDiscoverer", "xunit.core")]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class BuildAndRunAttribute : DataAttribute
    {
        private bool _aot;
        private RunHost _host;
        private object?[] _parameters;

        public BuildAndRunAttribute(bool aot=false, RunHost host = RunHost.All, params object?[] parameters)
        {
            this._aot = aot;
            this._host = host;
            this._parameters = parameters;
        }

        public override IEnumerable<object?[]> GetData(MethodInfo testMethod)
            => BuildTestBase.ConfigWithAOTData(_aot)
                    .Multiply(_parameters)
                    .WithRunHosts(_host)
                    .UnwrapItemsAsArrays().ToList().Dump();
    }
}
