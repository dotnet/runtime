// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
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

        public RegexParseException(RegexParseError error, int offset)
        {
            Error = error;
            Offset = offset;
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
