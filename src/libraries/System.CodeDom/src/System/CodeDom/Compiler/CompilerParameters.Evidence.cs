// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Additional implementation to keep public API in Mono (it has a reference to this file)
// https://github.com/dotnet/corefx/pull/15945

using System.Security.Policy;

namespace System.CodeDom.Compiler
{
    public partial class CompilerParameters
    {
        private Evidence _evidence;

        [Obsolete("Code Access Security is not supported or honored by the runtime")]
        public Evidence Evidence
        {
            get { return _evidence?.Clone(); }
            set { _evidence = value?.Clone(); }
         }
    }
}
