// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed partial class RuntimeMethodBody : MethodBody
    {
        // This class can only be created from inside the EE.
        private RuntimeMethodBody()
        {
            _IL = null!;
            _exceptionHandlingClauses = null!;
            _localVariables = null!;
            _methodBase = null!;
        }
    }
}
