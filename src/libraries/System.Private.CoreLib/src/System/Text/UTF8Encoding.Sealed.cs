// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text
{
    public partial class UTF8Encoding
    {
        /// <summary>
        /// A special instance of <see cref="UTF8Encoding"/> that is initialized with "don't throw on invalid sequences;
        /// perform <see cref="Rune.ReplacementChar"/> substitution instead" semantics. This type allows for devirtualization
        /// of calls made directly off of <see cref="Encoding.UTF8"/>. See https://github.com/dotnet/coreclr/pull/9230.
        /// </summary>
        internal sealed class UTF8EncodingSealed : UTF8Encoding
        {
            /// <summary>
            /// Maximum number of input elements we'll allow for going through the fast one-pass stackalloc code paths.
            /// </summary>
            private const int MaxSmallInputElementCount = 32;

            public UTF8EncodingSealed(bool encoderShouldEmitUTF8Identifier) : base(encoderShouldEmitUTF8Identifier) { }

            public override ReadOnlySpan<byte> Preamble => _emitUTF8Identifier ? PreambleSpan : default;

            public override object Clone()
            {
                // The base implementation of Encoding.Clone calls object.MemberwiseClone and marks the new object mutable.
                // We don't want to do this because it violates the invariants we have set for the sealed type.
                // Instead, we'll create a new instance of the base UTF8Encoding type and mark it mutable.

                return new UTF8Encoding(_emitUTF8Identifier)
                {
                    IsReadOnly = false
                };
            }

            public override byte[] GetBytes(string s)
            {
                // This method is short and can be inlined, meaning that the null check below
                // might be elided if the JIT can prove not-null at the call site.

                if (s?.Length <= MaxSmallInputElementCount)
                {
                    return GetBytesForSmallInput(s);
                }
                else
                {
                    return base.GetBytes(s!); // make the base method responsible for the null check
                }
            }

            private unsafe byte[] GetBytesForSmallInput(string s)
            {
                Debug.Assert(s != null);
                Debug.Assert(s.Length <= MaxSmallInputElementCount);

                byte* pDestination = stackalloc byte[MaxSmallInputElementCount * MaxUtf8BytesPerChar];

                int sourceLength = s.Length; // hoist this to avoid having the JIT auto-insert null checks
                int bytesWritten;

                fixed (char* pSource = s)
                {
                    bytesWritten = GetBytesCommon(pSource, sourceLength, pDestination, MaxSmallInputElementCount * MaxUtf8BytesPerChar);
                    Debug.Assert(0 <= bytesWritten && bytesWritten <= s.Length * MaxUtf8BytesPerChar);
                }

                return new Span<byte>(ref *pDestination, bytesWritten).ToArray(); // this overload of Span ctor doesn't validate length
            }

            public override string GetString(byte[] bytes)
            {
                // This method is short and can be inlined, meaning that the null check below
                // might be elided if the JIT can prove not-null at the call site.

                if (bytes?.Length <= MaxSmallInputElementCount)
                {
                    return GetStringForSmallInput(bytes);
                }
                else
                {
                    return base.GetString(bytes!); // make the base method responsible for the null check
                }
            }

            private unsafe string GetStringForSmallInput(byte[] bytes)
            {
                Debug.Assert(bytes != null);
                Debug.Assert(bytes.Length <= MaxSmallInputElementCount);

                char* pDestination = stackalloc char[MaxSmallInputElementCount]; // each byte produces at most one char

                int sourceLength = bytes.Length; // hoist this to avoid having the JIT auto-insert null checks
                int charsWritten;

                fixed (byte* pSource = bytes)
                {
                    charsWritten = GetCharsCommon(pSource, sourceLength, pDestination, MaxSmallInputElementCount);
                    Debug.Assert(0 <= charsWritten && charsWritten <= sourceLength); // should never have more output chars than input bytes
                }

                return new string(new ReadOnlySpan<char>(ref *pDestination, charsWritten)); // this overload of ROS ctor doesn't validate length
            }
        }
    }
}
