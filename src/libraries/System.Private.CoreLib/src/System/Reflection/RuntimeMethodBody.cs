// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection
{
    internal sealed partial class RuntimeMethodBody : MethodBody
    {
        private byte[] _IL;
        private ExceptionHandlingClause[] _exceptionHandlingClauses;
        private LocalVariableInfo[] _localVariables;
#if CORECLR
        internal MethodBase _methodBase;
#endif
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
