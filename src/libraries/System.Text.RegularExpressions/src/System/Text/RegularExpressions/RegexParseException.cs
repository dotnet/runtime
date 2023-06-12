// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// An exception as a result of a parse error in a regular expression <see cref="RegularExpressions"/>, with
    /// detailed information in the <see cref="Error"/> and <see cref="Offset"/> properties.
    /// </summary>
    [Serializable]
#if SYSTEM_TEXT_REGULAREXPRESSIONS
    public
#else
    internal
#endif
    sealed class RegexParseException : ArgumentException
    {
        /// <summary>Gets the error that happened during parsing.</summary>
        public RegexParseError Error { get; }

        /// <summary>Gets the zero-based character offset in the regular expression pattern where the parse error occurs.</summary>
        public int Offset { get; }

        // No need for a serialization ctor: we swap the active type during serialization.

        internal RegexParseException(RegexParseError error, int offset, string message) : base(message)
        {
            Error = error;
            Offset = offset;
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.SetType(typeof(ArgumentException)); // To maintain serialization support with .NET Framework.
        }
    }
}
