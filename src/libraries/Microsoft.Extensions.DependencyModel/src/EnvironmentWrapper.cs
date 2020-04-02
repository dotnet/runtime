// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.DependencyModel
{
    internal class EnvironmentWrapper : IEnvironment
    {
        public static IEnvironment Default = new EnvironmentWrapper();

        public string GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

        public object GetAppContextData(string name) => AppDomain.CurrentDomain.GetData(name);

        public bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
