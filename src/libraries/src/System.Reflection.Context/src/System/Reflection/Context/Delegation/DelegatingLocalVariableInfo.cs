// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Reflection.Context.Delegation
{
    internal class DelegatingLocalVariableInfo : LocalVariableInfo
    {
        public DelegatingLocalVariableInfo(LocalVariableInfo variable)
        {
            Debug.Assert(variable != null);

            UnderlyingVariable = variable;
        }

        public override bool IsPinned
        {
            get { return UnderlyingVariable.IsPinned; }
        }

        public override int LocalIndex
        {
            get { return UnderlyingVariable.LocalIndex; }
        }

        public override Type LocalType
        {
            get { return UnderlyingVariable.LocalType; }
        }

        public LocalVariableInfo UnderlyingVariable { get; }

        public override string ToString()
        {
            return UnderlyingVariable.ToString();
        }
    }
}
