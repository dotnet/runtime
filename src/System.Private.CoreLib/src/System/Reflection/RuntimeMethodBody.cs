// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection
{
    internal sealed class RuntimeMethodBody : MethodBody
    {
        // This class can only be created from inside the EE.
        private RuntimeMethodBody() { }

        private byte[] _IL = null!;
        private ExceptionHandlingClause[] _exceptionHandlingClauses = null!;
        private LocalVariableInfo[] _localVariables = null!;
        internal MethodBase _methodBase = null!;
        private int _localSignatureMetadataToken;
        private int _maxStackSize;
        private bool _initLocals;

        public override int LocalSignatureMetadataToken => _localSignatureMetadataToken;
        public override IList<LocalVariableInfo> LocalVariables => Array.AsReadOnly(_localVariables);
        public override int MaxStackSize => _maxStackSize;
        public override bool InitLocals => _initLocals;
        public override byte[] GetILAsByteArray() => _IL;
        public override IList<ExceptionHandlingClause> ExceptionHandlingClauses => Array.AsReadOnly(_exceptionHandlingClauses);
    }
}

