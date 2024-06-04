// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Indicates that a switch expression that was non-exhaustive failed to match its input
    /// at runtime, e.g. in the C# 8 expression <code>3 switch { 4 => 5 }</code>.
    /// The exception optionally contains an object representing the unmatched value.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("System.Runtime.Extensions, Version=4.2.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed class SwitchExpressionException : InvalidOperationException
    {
        public SwitchExpressionException()
            : base(SR.Arg_SwitchExpressionException) { }

        public SwitchExpressionException(Exception? innerException) :
            base(SR.Arg_SwitchExpressionException, innerException)
        {
        }

        public SwitchExpressionException(object? unmatchedValue) : this()
        {
            UnmatchedValue = unmatchedValue;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private SwitchExpressionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            UnmatchedValue = info.GetValue(nameof(UnmatchedValue), typeof(object));
        }

        public SwitchExpressionException(string? message) : base(message ?? SR.Arg_SwitchExpressionException) { }

        public SwitchExpressionException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_SwitchExpressionException, innerException) { }

        public object? UnmatchedValue { get; }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(UnmatchedValue), UnmatchedValue, typeof(object));
        }

        public override string Message
        {
            get
            {
                if (UnmatchedValue is null)
                {
                    return base.Message;
                }

                string valueMessage = SR.Format(SR.SwitchExpressionException_UnmatchedValue, UnmatchedValue);
                return base.Message + Environment.NewLine + valueMessage;
            }
        }
    }
}
