// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.CodeDom.Compiler
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    public sealed class GeneratedCodeAttribute : Attribute
    {
        private readonly string _tool;
        private readonly string _vversion;

        public GeneratedCodeAttribute(string tool, string version)
        {
            _tool = tool;
            _vversion = version;
        }

        public string Tool
        {
            get { return _tool; }
        }

        public string Version
        {
            get { return _vversion; }
        }
    }
}
