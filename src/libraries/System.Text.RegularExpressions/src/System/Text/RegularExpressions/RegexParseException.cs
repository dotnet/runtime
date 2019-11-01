// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Text.RegularExpressions
{
    [Serializable]
    internal sealed class RegexParseException : ArgumentException
    {
        private readonly RegexParseError _error;

        /// <summary>
        /// The error that happened during parsing.
        /// </summary>
        public RegexParseError Error => _error;

        /// <summary>
        /// The offset in the supplied pattern.
        /// </summary>
        public int Offset { get; }

        public RegexParseException(RegexParseError error, int offset, string message) : base(message)
        {
            _error = error;
            Offset = offset;
        }

        public RegexParseException() : base()
        {
        }

        public RegexParseException(string message) : base(message)
        {
        }

        public RegexParseException(string message, Exception inner) : base(message, inner)
        {
        }

        private RegexParseException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // To maintain serialization support with netfx.
            info.SetType(typeof(ArgumentException));
        }
    }
}
