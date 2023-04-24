// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MissingFieldException : MissingMemberException, ISerializable
    {
        public MissingFieldException()
            : base(SR.Arg_MissingFieldException)
        {
            HResult = HResults.COR_E_MISSINGFIELD;
        }

        public MissingFieldException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_MISSINGFIELD;
        }

        public MissingFieldException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_MISSINGFIELD;
        }

        public MissingFieldException(string? className, string? fieldName)
        {
            ClassName = className;
            MemberName = fieldName;
            HResult = HResults.COR_E_MISSINGFIELD;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected MissingFieldException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public override string Message
        {
            get
            {
                if (ClassName == null)
                {
                    return base.Message;
                }
                else
                {
                    // do any desired fixups to classname here.
                    return SR.Format(SR.MissingField_Name, ClassName, MemberName);
                }
            }
        }
    }
}
