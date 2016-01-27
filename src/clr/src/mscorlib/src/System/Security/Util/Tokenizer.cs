// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** PURPOSE:  Tokenize "Elementary XML", that is, XML without 
**           attributes or DTDs, in other words, XML with 
**           elements only.
** 
** 
===========================================================*/
namespace System.Security.Util 
{
    using System.Text;
    using System;
    using System.IO;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    internal sealed class Tokenizer 
    {
        // There are five externally knowable token types: bras, kets,
        // slashes, cstrs, and equals.  
    
        internal const byte bra = 0;
        internal const byte ket = 1;
        internal const byte slash = 2;
        internal const byte cstr = 3;
        internal const byte equals = 4;
        internal const byte quest = 5;
        internal const byte bang = 6;
        internal const byte dash = 7;

        // these are the assorted text characters that the tokenizer must
        // understand to do its job

        internal const int intOpenBracket = (int) '<';
        internal const int intCloseBracket = (int) '>';
        internal const int intSlash = (int) '/';
        internal const int intEquals = (int) '=';
        internal const int intQuote = (int) '\"';
        internal const int intQuest = (int) '?';
        internal const int intBang = (int) '!';
        internal const int intDash = (int) '-';
        internal const int intTab = (int) '\t';
        internal const int intCR = (int) '\r';
        internal const int intLF = (int) '\n';
        internal const int intSpace = (int) ' ';

        // this tells us where we will be getting our input from
        // and what the encoding is (if any)
        
        private enum TokenSource
        {
            UnicodeByteArray,  // this is little-endian unicode (there are other encodings)
            UTF8ByteArray,
            ASCIIByteArray,
            CharArray,
            String,
            NestedStrings,
            Other                
        }

        // byte streams can come in 3 different flavors

        internal enum ByteTokenEncoding
        {
            UnicodeTokens,  // this is little-endian unicode (there are other encodings)
            UTF8Tokens,
            ByteTokens
        }
  
        public int  LineNo;

        // these variables represent the input state of the of the tokenizer

        private int _inProcessingTag;  
        private byte[] _inBytes;
        private char[] _inChars;
        private String _inString;
        private int _inIndex;
        private int _inSize;
        private int _inSavedCharacter;
        private TokenSource _inTokenSource;
        private ITokenReader _inTokenReader;        

        // these variables are used to build and deliver tokenizer output strings
                
        private StringMaker _maker = null;
        
        private String[] _searchStrings;
        private String[] _replaceStrings;
        
        private int _inNestedIndex;
        private int _inNestedSize;
        private String _inNestedString;

        
        //================================================================
        // Constructor uses given ICharInputStream
        //

        internal void BasicInitialization()
        {
            LineNo = 1 ;
            _inProcessingTag = 0;
            _inSavedCharacter = -1;
            _inIndex = 0;
            _inSize = 0;
            _inNestedSize = 0;
            _inNestedIndex = 0;
            _inTokenSource = TokenSource.Other;
            _maker = System.SharedStatics.GetSharedStringMaker();
        }
        
        public void Recycle()
        {
            System.SharedStatics.ReleaseSharedStringMaker(ref _maker); // will set _maker to null
        }

        internal Tokenizer (String input)
        {
            BasicInitialization();
            _inString = input;
            _inSize = input.Length;
            _inTokenSource = TokenSource.String;
        }        
        
        internal Tokenizer (String input, String[] searchStrings, String[] replaceStrings)
        {
            BasicInitialization();
            _inString = input;
            _inSize = _inString.Length;
            _inTokenSource = TokenSource.NestedStrings;
            _searchStrings = searchStrings;
            _replaceStrings = replaceStrings;

#if DEBUG
            Contract.Assert(searchStrings.Length == replaceStrings.Length, "different number of search/replace strings");
            Contract.Assert(searchStrings.Length != 0, "no search replace strings, shouldn't be using this ctor");
            
            for (int istr=0; istr<searchStrings.Length; istr++)
            {
                String str = searchStrings[istr];
                Contract.Assert( str != null, "XML Slug null");
                Contract.Assert( str.Length >= 3 , "XML Slug too small");
                Contract.Assert( str[0] == '{', "XML Slug doesn't start with '{'" );
                Contract.Assert( str[str.Length-1] == '}', "XML Slug doesn't end with '}'" );
                
                str = replaceStrings[istr];
                Contract.Assert( str != null, "XML Replacement null");
                Contract.Assert( str.Length >= 1, "XML Replacement empty");
            }
#endif            
        }

        internal Tokenizer (byte[] array, ByteTokenEncoding encoding, int startIndex)
           {
            BasicInitialization();
            _inBytes = array;
            _inSize = array.Length;
            _inIndex = startIndex;

            switch (encoding)
            {
            case ByteTokenEncoding.UnicodeTokens:
                _inTokenSource = TokenSource.UnicodeByteArray;
                break;

            case ByteTokenEncoding.UTF8Tokens:
                _inTokenSource = TokenSource.UTF8ByteArray;
                break;

            case ByteTokenEncoding.ByteTokens:
                _inTokenSource = TokenSource.ASCIIByteArray;
                break;

            default:
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)encoding));
            }
           }
        
        internal Tokenizer (char[] array)
        {
            BasicInitialization();
            _inChars = array;
            _inSize = array.Length;
            _inTokenSource = TokenSource.CharArray;
        }        
        
        internal Tokenizer (StreamReader input)
        {
            BasicInitialization();
            _inTokenReader = new StreamTokenReader(input);
        }
    
        internal void ChangeFormat( System.Text.Encoding encoding )
        {        
            if (encoding == null)
            {
                return;
            }

            Contract.Assert( _inSavedCharacter == -1, "There was a lookahead character at the stream change point, that means the parser is changing encodings too late" );

            switch (_inTokenSource)
            {
                case TokenSource.UnicodeByteArray:
                case TokenSource.UTF8ByteArray:
                case TokenSource.ASCIIByteArray:
                    // these are the ones we can change on the fly

                    if (encoding == System.Text.Encoding.Unicode)
                    {
                        _inTokenSource = TokenSource.UnicodeByteArray;
                        return;
                    }

                    if (encoding == System.Text.Encoding.UTF8)
                    {
                        _inTokenSource = TokenSource.UTF8ByteArray;
                        return;
                    }
#if FEATURE_ASCII
                    if (encoding == System.Text.Encoding.ASCII)
                    {
                        _inTokenSource = TokenSource.ASCIIByteArray;
                        return;
                    }
#endif              
                    break;

                case TokenSource.String:
                case TokenSource.CharArray:
                case TokenSource.NestedStrings:
                    // these are already unicode and encoding changes are moot
                    // they can't be further decoded 
                    return;
            }

            // if we're here it means we don't know how to change
            // to the desired encoding with the memory that we have
            // we'll have to do this the hard way -- that means
            // creating a suitable stream from what we've got

            // this is thankfully the rare case as UTF8 and unicode
            // dominate the scene 

            Stream stream = null;

            switch (_inTokenSource)
            {
                case TokenSource.UnicodeByteArray:
                case TokenSource.UTF8ByteArray:
                case TokenSource.ASCIIByteArray:
                    stream = new MemoryStream(_inBytes, _inIndex, _inSize - _inIndex);
                    break;

                case TokenSource.CharArray:
                case TokenSource.String:
                case TokenSource.NestedStrings:
                    Contract.Assert(false, "attempting to change encoding on a non-changable source, should have been prevented earlier" );
                    return;

                default:
                    StreamTokenReader reader = _inTokenReader as StreamTokenReader;

                    if (reader == null)
                    {
                        Contract.Assert(false, "A new input source type has been added to the Tokenizer but it doesn't support encoding changes");
                        return;
                    }

                    stream = reader._in.BaseStream;

                    Contract.Assert( reader._in.CurrentEncoding != null, "Tokenizer's StreamReader does not have an encoding" );

                    String fakeReadString = new String(' ', reader.NumCharEncountered);
                    stream.Position = reader._in.CurrentEncoding.GetByteCount( fakeReadString );
                    break;
            }

            Contract.Assert(stream != null, "The XML stream with new encoding was not properly initialized for kind of input we had");

            // we now have an initialized memory stream based on whatever source we had before
            _inTokenReader = new StreamTokenReader( new StreamReader( stream, encoding ) );
            _inTokenSource = TokenSource.Other;
        }

        internal void GetTokens( TokenizerStream stream, int maxNum, bool endAfterKet )
        {
            while (maxNum == -1 || stream.GetTokenCount() < maxNum)
            {
                int i = -1;
                byte ch;
                int cb = 0;
                bool inLiteral = false;
                bool inQuotedString = false;
            
                StringMaker m = _maker;
            
                m._outStringBuilder = null;
                m._outIndex = 0;
                        
            BEGINNING: 
            
                if (_inSavedCharacter != -1)
                {
                       i = _inSavedCharacter;
                       _inSavedCharacter = -1;
                }
                else switch (_inTokenSource)
                {
                case TokenSource.UnicodeByteArray:
                    if (_inIndex + 1 >= _inSize)
                    {
                        stream.AddToken( -1 );
                        return;
                    }

                     i = (int)((_inBytes[_inIndex+1]<<8) + _inBytes[_inIndex]);
                      _inIndex += 2;      
                    break;
                
                 case TokenSource.UTF8ByteArray:
                    if (_inIndex >= _inSize)
                    {
                        stream.AddToken( -1 );
                        return;
                    }

                    i = (int)(_inBytes[_inIndex++]);

                    // single byte -- case, early out as we're done already
                    if ((i & 0x80) == 0x00)
                        break;

                    // to decode the lead byte switch on the high nibble 
                    // shifted down so the switch gets dense integers
                    switch ((i & 0xf0) >>4)
                    {
                    case 0x8:   // 1000  (together these 4 make 10xxxxx)
                    case 0x9:   // 1001
                    case 0xa:   // 1010
                    case 0xb:   // 1011
                        // trail byte is an error
                        throw new XmlSyntaxException( LineNo );

                    case 0xc:   // 1100  (these two make 110xxxxx)
                    case 0xd:   // 1101
                        // two byte encoding (1 trail byte)
                        i &= 0x1f;
                        cb = 2;
                        break;

                    case 0xe:   // 1110 (this gets us 1110xxxx)
                        // three byte encoding (2 trail bytes)
                        i &= 0x0f;
                        cb = 3;
                        break;

                    case 0xf:   // 1111 (and finally 1111xxxx)
                        // 4 byte encoding is an error
                        throw new XmlSyntaxException( LineNo );
                    }

                    // at least one trail byte, fetch it
                    if (_inIndex >= _inSize)
                        throw new XmlSyntaxException (LineNo, Environment.GetResourceString( "XMLSyntax_UnexpectedEndOfFile" ));

                    ch = _inBytes[_inIndex++];

                    // must be trail byte encoding
                    if ((ch & 0xc0) != 0x80)
                        throw new XmlSyntaxException( LineNo );

                    i = (i<<6) | (ch & 0x3f);

                    // done now if 2 byte encoding, otherwise go for 3
                    if (cb == 2)
                        break;

                    if (_inIndex >= _inSize)
                        throw new XmlSyntaxException (LineNo, Environment.GetResourceString( "XMLSyntax_UnexpectedEndOfFile" ));

                    ch = _inBytes[_inIndex++];

                    // must be trail byte encoding
                    if ((ch & 0xc0) != 0x80)
                        throw new XmlSyntaxException( LineNo );

                    i = (i<<6) | (ch & 0x3f);
                    break;                
                
                case TokenSource.ASCIIByteArray:
                    if (_inIndex >= _inSize)
                    {
                        stream.AddToken( -1 );
                        return;
                    }

                    i = (int)(_inBytes[_inIndex++]);
                    break;

                case TokenSource.CharArray:
                    if (_inIndex >= _inSize)
                    {
                        stream.AddToken( -1 );
                        return;
                    }

                     i = (int)(_inChars[_inIndex++]);
                    break;
                
                case TokenSource.String:
                    if (_inIndex >= _inSize)
                    {
                        stream.AddToken( -1 );
                        return;
                    }

                    i = (int)(_inString[_inIndex++]);
                    break;
                            
                case TokenSource.NestedStrings:
                    if (_inNestedSize != 0)
                    {
                        if (_inNestedIndex < _inNestedSize)
                        {
                            i = _inNestedString[_inNestedIndex++];
                            break;
                        }
                    
                        _inNestedSize = 0;
                    }
                
                    if (_inIndex >= _inSize)
                    {
                        stream.AddToken( -1 );
                        return;
                    }

                    i = (int)(_inString[_inIndex++]);
                
                    if (i != '{')
                        break;
                                    
                    for (int istr=0; istr < _searchStrings.Length; istr++)
                    {
                        if (0==String.Compare(_searchStrings[istr], 0, _inString, _inIndex-1, _searchStrings[istr].Length, StringComparison.Ordinal))
                        {
                            _inNestedString = _replaceStrings[istr];
                            _inNestedSize = _inNestedString.Length;
                            _inNestedIndex = 1;
                            i = _inNestedString[0];
                            _inIndex += _searchStrings[istr].Length - 1;   
                            break;
                        }
                    }
                
                    break;
                    
                default:
                    i = _inTokenReader.Read();
                    if (i == -1) 
                    {
                        stream.AddToken( -1 );
                        return;
                    }
                    break;
                }

                if (!inLiteral)
                {
                    switch (i)
                    {
                    // skip whitespace
                    case intSpace:
                    case intTab:
                    case intCR:
                        goto BEGINNING;

                    // count linefeeds
                    case intLF:
                        LineNo++;
                        goto BEGINNING;
                                        
                    case intOpenBracket:
                        _inProcessingTag++;
                        stream.AddToken( bra );
                        continue;
                    
                    case intCloseBracket:
                        _inProcessingTag--;
                        stream.AddToken( ket );
                        if (endAfterKet)
                            return;
                        continue;
                    
                    case intEquals:
                        stream.AddToken( equals );
                        continue;
                    
                    case intSlash:
                        if (_inProcessingTag != 0)
                        {
                            stream.AddToken( slash );
                            continue;
                        }
                        break;

                    case intQuest:
                        if (_inProcessingTag != 0)
                        {
                            stream.AddToken( quest );
                            continue;
                        } 
                        break;

                    case intBang:
                        if (_inProcessingTag != 0)
                        {
                            stream.AddToken( bang );
                            continue;
                        } 
                        break;

                    case intDash:
                        if (_inProcessingTag != 0) 
                        {
                            stream.AddToken( dash );
                            continue;
                        }
                        break;

                    case intQuote:
                        inLiteral = true;
                        inQuotedString = true;
                        goto BEGINNING;
                    }
                }
                else
                   {
                    switch (i)
                    {
                    case intOpenBracket:
                        if (!inQuotedString)
                        {
                            _inSavedCharacter = i;
                            stream.AddToken( cstr );
                            stream.AddString( this.GetStringToken() );
                            continue;
                        }
                        break;
                        
                    case intCloseBracket:
                    case intEquals:
                    case intSlash:
                        if (!inQuotedString && _inProcessingTag != 0)
                        {
                            _inSavedCharacter = i;
                            stream.AddToken( cstr );
                            stream.AddString( this.GetStringToken() );
                            continue;
                        }
                        break;
                    
                    case intQuote:
                        if (inQuotedString)
                        {
                            stream.AddToken( cstr );
                            stream.AddString( this.GetStringToken() );
                            continue;
                        }
                        break;
                    
                    case intTab:
                    case intCR:
                    case intSpace:
                        if (!inQuotedString)
                        {
                            stream.AddToken( cstr );
                            stream.AddString( this.GetStringToken() );
                            continue;
                        }
                        break;

                    // count linefeeds
                    case intLF:
                        LineNo++;

                        if (!inQuotedString)
                        {
                            stream.AddToken( cstr );
                            stream.AddString( this.GetStringToken() );
                            continue;
                        }
                        break;                        
                    }
                }

                inLiteral = true;
                                       
                // add character  to the string
                if (m._outIndex < StringMaker.outMaxSize) 
                {
                    // easy case
                    m._outChars[m._outIndex++] = (char)i;
                } 
                else
                {
                    if (m._outStringBuilder == null) 
                    {
                       // OK, first check if we have to init the StringBuilder
                        m._outStringBuilder = new StringBuilder();
                    }
                
                    // OK, copy from _outChars to _outStringBuilder
                    m._outStringBuilder.Append(m._outChars, 0, StringMaker.outMaxSize);
                
                    // reset _outChars pointer
                    m._outChars[0] = (char)i;
                    m._outIndex = 1;
                }
                        
                goto BEGINNING;
            }
        }
        
        [Serializable]
        internal sealed class StringMaker
        {
            String[] aStrings;
            uint cStringsMax;
            uint cStringsUsed;
            
            public StringBuilder _outStringBuilder;
            public char[] _outChars;
            public int _outIndex;
            
            public const int outMaxSize = 512;            
            
            static uint HashString(String str)
            {
                uint hash = 0;
                
                int l = str.Length;
                
                // rotate in string character
                for (int i=0; i < l; i++)
                {
                    hash = (hash << 3) ^ (uint)str[i] ^ (hash >> 29);
                }
                
                return hash;
            }

            static uint HashCharArray(char[] a, int l)
            {
                uint hash = 0;
                
                // rotate in a character
                for (int i=0; i < l; i++)
                {
                    hash = (hash << 3) ^ (uint)a[i] ^ (hash >> 29);
                }
                    
                return hash;
            }
            
            public StringMaker()
            {
                cStringsMax = 2048;
                cStringsUsed = 0;
                aStrings = new String[cStringsMax];
                _outChars = new char[outMaxSize];
            }
            
            bool CompareStringAndChars(String str, char [] a, int l)
            {
                if (str.Length != l)
                    return false;
                
                for (int i=0; i<l; i++)
                    if (a[i] != str[i])
                        return false;
                        
                return true;
            }
            
            public String MakeString()
            {
                uint hash;
                char[] a = _outChars;
                int l = _outIndex;
                
                // if we have a stringbuilder then we have to append... slow case
                if (_outStringBuilder != null) 
                {                    
                    _outStringBuilder.Append(_outChars, 0, _outIndex);
                    return _outStringBuilder.ToString();
                }
                
                // no stringbuilder, fast case, shareable string
                
                if (cStringsUsed > (cStringsMax / 4) * 3)
                {
                    // we need to rehash
                    
                    uint cNewMax = cStringsMax * 2;
                    String [] aStringsNew = new String[cNewMax];
                    
                    for (int i=0; i < cStringsMax;i++)
                    {
                        if (aStrings[i] != null)
                        {
                            hash = HashString(aStrings[i]) % cNewMax;
                            
                            while (aStringsNew[hash] != null)
                            {
                                // slot full, skip
                                if (++hash >= cNewMax)
                                    hash = 0;                        
                            }
                
                            aStringsNew[hash] = aStrings[i];
                        }
                    }
                    
                    // all done, cutover to the new hash table
                    cStringsMax = cNewMax;
                    aStrings = aStringsNew;
                }
                       
                hash = HashCharArray(a, l) % cStringsMax;
                
                String str;
                
                while ((str = aStrings[hash]) != null)
                {
                    if (CompareStringAndChars(str, a, l))
                        return str;

                    if (++hash >= cStringsMax)
                        hash = 0;                        
                }
                
                str = new String(a,0,l);
                aStrings[hash] = str;
                cStringsUsed++;
                
                return str;
            }
        }

        //================================================================
        //
        //
        
        private String GetStringToken ()
        {
            return _maker.MakeString();
        }
    
        internal interface ITokenReader
        {
            int Read();
        }
                       
        internal class StreamTokenReader : ITokenReader 
        {
            
            internal StreamReader _in;
            internal int _numCharRead;
            
            internal StreamTokenReader(StreamReader input) 
            {
                _in = input;
                _numCharRead = 0;
            }
                       
            public virtual int Read()
            {
                int value = _in.Read();
                if (value != -1)
                    _numCharRead++;
                return value;
            }

            internal int NumCharEncountered
            {
                get
                {
                    return _numCharRead;
                }
            }
        }
    }

    internal sealed class TokenizerShortBlock
    {
        internal short[] m_block = new short[16];
        internal TokenizerShortBlock m_next = null;
    }

    internal sealed class TokenizerStringBlock
    {
        internal String[] m_block = new String[16];
        internal TokenizerStringBlock m_next = null;
    }


    internal sealed class TokenizerStream
    {
        private int m_countTokens;

        private TokenizerShortBlock m_headTokens;
        private TokenizerShortBlock m_lastTokens;
        private TokenizerShortBlock m_currentTokens;
        private int m_indexTokens;

        private TokenizerStringBlock m_headStrings;
        private TokenizerStringBlock m_currentStrings;
        private int m_indexStrings;

#if _DEBUG
        private bool m_bLastWasCStr;        
#endif

        internal TokenizerStream()
        {
            m_countTokens = 0;
            m_headTokens = new TokenizerShortBlock();
            m_headStrings = new TokenizerStringBlock();
            Reset();
        }

        internal void AddToken( short token )
        {
            if (m_currentTokens.m_block.Length <= m_indexTokens)
            {
                m_currentTokens.m_next = new TokenizerShortBlock();
                m_currentTokens = m_currentTokens.m_next;
                m_indexTokens = 0;
            }

            m_countTokens++;
            m_currentTokens.m_block[m_indexTokens++] = token;
        }

        internal void AddString( String str )
        {
            if (m_currentStrings.m_block.Length <= m_indexStrings)
            {
                m_currentStrings.m_next = new TokenizerStringBlock();
                m_currentStrings = m_currentStrings.m_next;
                m_indexStrings = 0;
            }

            m_currentStrings.m_block[m_indexStrings++] = str;
        }

        internal void Reset()
        {
            m_lastTokens = null;
            m_currentTokens = m_headTokens;
            m_currentStrings = m_headStrings;
            m_indexTokens = 0;
            m_indexStrings = 0;
#if _DEBUG
            m_bLastWasCStr = false;
#endif
        }

        internal short GetNextFullToken()
        {
            if (m_currentTokens.m_block.Length <= m_indexTokens)
            {
                m_lastTokens = m_currentTokens;
                m_currentTokens = m_currentTokens.m_next;
                m_indexTokens = 0;
            }

            return m_currentTokens.m_block[m_indexTokens++];
        }

        internal short GetNextToken()
        {
            short retval = (short)(GetNextFullToken() & 0x00FF);
#if _DEBUG
            Contract.Assert( !m_bLastWasCStr, "CStr token not followed by GetNextString()" );
            m_bLastWasCStr = (retval == Tokenizer.cstr);
#endif
            return retval;
        }
        
        internal String GetNextString()
        {
            if (m_currentStrings.m_block.Length <= m_indexStrings)
            {
                m_currentStrings = m_currentStrings.m_next;
                m_indexStrings = 0;
            }
#if _DEBUG
            m_bLastWasCStr = false;
#endif
            return m_currentStrings.m_block[m_indexStrings++];
        }

        internal void ThrowAwayNextString()
        {
            GetNextString();
        }

        internal void TagLastToken( short tag )
        {
            if (m_indexTokens == 0)
                m_lastTokens.m_block[m_lastTokens.m_block.Length-1] = (short)((ushort)m_lastTokens.m_block[m_lastTokens.m_block.Length-1] | (ushort)tag);
            else
                m_currentTokens.m_block[m_indexTokens-1] = (short)((ushort)m_currentTokens.m_block[m_indexTokens-1] | (ushort)tag);
        }

        internal int GetTokenCount()
        {
            return m_countTokens;
        }

        internal void GoToPosition( int position )
        {
            Reset();

            for (int count = 0; count < position; ++count)
            {
                if (GetNextToken() == Tokenizer.cstr)
                    ThrowAwayNextString();
            }
        }   
    }
}
