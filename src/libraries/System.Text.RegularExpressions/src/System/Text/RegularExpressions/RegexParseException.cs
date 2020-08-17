// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// An exception as a result of a parse error in a regular expression <see cref="RegularExpressions"/>, with
    /// detailed information in the <see cref="Error"/> and <see cref="Offset"/> properties.
    /// </summary>
    [Serializable]
    public sealed class RegexParseException : ArgumentException
    {
        /// <summary>Gets the error that happened during parsing.</summary>
        public RegexParseError Error { get; }

        /// <summary>Gets the offset in the supplied pattern.</summary>
        public int Offset { get; }

        internal RegexParseException(RegexParseError error, int offset, string message) : base(message)
        {
            Error = error;
            Offset = offset;
        }

        /// <summary>
        /// Construct a custom RegexParseException that creates a default message based on the given <see cref="RegexParseError"/> value.
        /// </summary>
        /// <param name="error">The <see cref="RegexParseError"/> value detailing the type of parse error.</param>
        /// <param name="offset">The zero-based offset in the regular expression where the parse error occurs.</param>
        public RegexParseException(RegexParseError error, int offset) : base(MakeMessage(error, offset))
        {
            Error = error;
            Offset = offset;
        }

        private static string MakeMessage(RegexParseError error, int offset)
        {
            return SR.Format(SR.Unknown, error.ToString(), offset);
        }

        private RegexParseException(SerializationInfo info, StreamingContext context)
        {
            // It means someone modified the payload.
            throw new NotImplementedException();
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.SetType(typeof(ArgumentException)); // To maintain serialization support with .NET Framework.
        }
    }
}
