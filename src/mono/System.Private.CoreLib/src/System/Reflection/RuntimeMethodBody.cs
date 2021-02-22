// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed partial class RuntimeMethodBody : MethodBody
    {
        // Called by the runtime
        internal RuntimeMethodBody(ExceptionHandlingClause[] clauses, LocalVariableInfo[] locals,
                                    byte[] il, bool init_locals, int sig_token, int max_stack)
        {
            _exceptionHandlingClauses = clauses;
            _localVariables = locals;
            _IL = il;
            _initLocals = init_locals;
            _localSignatureMetadataToken = sig_token;
            _maxStackSize = max_stack;
        }
    }
}
