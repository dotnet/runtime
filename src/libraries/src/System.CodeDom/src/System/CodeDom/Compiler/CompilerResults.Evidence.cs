// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Policy;

namespace System.CodeDom.Compiler
{
    public partial class CompilerResults
    {
    	private Evidence _evidence;

        [Obsolete("CAS policy is obsolete and will be removed in a future release of the .NET Framework. Please see http://go2.microsoft.com/fwlink/?LinkId=131738 for more information.")]
        public Evidence Evidence
        {
            get { return _evidence?.Clone(); }
            set { _evidence = value?.Clone(); }
        }
    }
}