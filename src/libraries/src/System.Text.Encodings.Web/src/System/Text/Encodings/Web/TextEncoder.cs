// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Text.Encodings.Web
{
    public abstract class TextEncoder
    {
        [CLSCompliant(false)]
        public unsafe abstract bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);

        // all subclasses have the same implementation of this method.
        // but this cannot be made virtual, because it will cause a virtual call to Encodes, and it destroys perf, i.e. makes common scenario 2x slower 
        [CLSCompliant(false)]
        public unsafe abstract int FindFirstCharacterToEncode(char* text, int textLength);

        public abstract bool Encodes(int unicodeScalar);

        // this could be a field, but I am trying to make the abstraction pure.
        public abstract int MaxOutputCharactersPerInputCharacter { get; }
    }
}
