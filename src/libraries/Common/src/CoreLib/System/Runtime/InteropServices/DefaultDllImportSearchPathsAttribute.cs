// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DefaultDllImportSearchPathsAttribute : Attribute
    {
        public DefaultDllImportSearchPathsAttribute(DllImportSearchPath paths)
        {
            Paths = paths;
        }

        public DllImportSearchPath Paths { get; }
    }
}
