// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when there is an attempt to dynamically access a method that does not exist.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MissingMethodException : MissingMemberException
    {
        public MissingMethodException()
            : base(SR.Arg_MissingMethodException)
        {
            HResult = HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string? message)
            : base(message ?? SR.Arg_MissingMethodException)
        {
            HResult = HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string? message, Exception? inner)
            : base(message ?? SR.Arg_MissingMethodException, inner)
        {
            HResult = HResults.COR_E_MISSINGMETHOD;
        }

        public MissingMethodException(string? className, string? methodName)
        {
            ClassName = className;
            MemberName = methodName;
            HResult = HResults.COR_E_MISSINGMETHOD;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected MissingMethodException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public override string Message =>
            ClassName == null ?
                base.Message :
                SR.Format(SR.MissingMethod_Name, ClassName, MemberName);
    }
}
