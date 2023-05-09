// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Diagnostics.Contracts
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed class ContractException : Exception
    {
        private readonly ContractFailureKind _kind;
        private readonly string? _userMessage;
        private readonly string? _condition;

        public ContractFailureKind Kind => _kind;
        public string Failure => this.Message;
        public string? UserMessage => _userMessage;
        public string? Condition => _condition;

        // Called by COM Interop, if we see COR_E_CODECONTRACTFAILED as an HRESULT.
        private ContractException()
        {
            HResult = HResults.COR_E_CODECONTRACTFAILED;
        }

        public ContractException(ContractFailureKind kind, string? failure, string? userMessage, string? condition, Exception? innerException)
            : base(failure, innerException)
        {
            HResult = HResults.COR_E_CODECONTRACTFAILED;
            _kind = kind;
            _userMessage = userMessage;
            _condition = condition;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private ContractException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _kind = (ContractFailureKind)info.GetInt32("Kind");
            _userMessage = info.GetString("UserMessage");
            _condition = info.GetString("Condition");
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Kind", _kind);
            info.AddValue("UserMessage", _userMessage);
            info.AddValue("Condition", _condition);
        }
    }
}
