// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;
using System.Reflection;

namespace System.CodeDom.Compiler
{
    public partial class CompilerResults
    {
        private Assembly _compiledAssembly;

        public CompilerResults(TempFileCollection tempFiles)
        {
            TempFiles = tempFiles;
        }

        public TempFileCollection TempFiles { get; set; }

        public Assembly CompiledAssembly
        {
            get
            {
                if (_compiledAssembly == null && PathToAssembly != null)
                {
                    _compiledAssembly = Assembly.Load(new AssemblyName() { CodeBase = PathToAssembly });
                }
                return _compiledAssembly;
            }
            set => _compiledAssembly = value;
        }

        public CompilerErrorCollection Errors { get; } = new CompilerErrorCollection();

        public StringCollection Output { get; } = new StringCollection();
        public string PathToAssembly { get; set; }

        public int NativeCompilerReturnValue { get; set; }
    }
}
