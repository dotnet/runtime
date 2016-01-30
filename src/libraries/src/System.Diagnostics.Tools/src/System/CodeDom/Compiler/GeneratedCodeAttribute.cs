// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.CodeDom.Compiler
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class GeneratedCodeAttribute : Attribute
    {
        private readonly string _tool;
        private readonly string _version;

        public GeneratedCodeAttribute(string tool, string version)
        {
            _tool = tool;
            _version = version;
        }

        public string Tool
        {
            get { return _tool; }
        }

        public string Version
        {
            get { return _version; }
        }
    }
}
