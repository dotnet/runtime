// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Text.Encodings.Web
{    
    public abstract class TextEncoder
    {
        // The following pragma disables a warning complaining about non-CLS compliant members being abstract, 
        // and wants me to mark the type as non-CLS compliant. 
        // It is true that this type cannot be extended by all CLS compliant languages. 
        // Having said that, if I marked the type as non-CLS all methods that take it as parameter will now have to be marked CLSCompliant(false), 
        // yet consumption of concrete encoders is totally CLS compliant, 
        // as it’s mainly to be done by calling helper methods in TextEncoderExtensions class, 
        // and so I think the warning is a bit too aggressive.  
        #pragma warning disable 3011
        [CLSCompliant(false)]
        public unsafe abstract bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);

        // all subclasses have the same implementation of this method.
        // but this cannot be made virtual, because it will cause a virtual call to Encodes, and it destroys perf, i.e. makes common scenario 2x slower 
        [CLSCompliant(false)]
        public unsafe abstract int FindFirstCharacterToEncode(char* text, int textLength);
        #pragma warning restore

        public abstract bool Encodes(int unicodeScalar);

        // this could be a field, but I am trying to make the abstraction pure.
        public abstract int MaxOutputCharactersPerInputCharacter { get; }
    }
}
