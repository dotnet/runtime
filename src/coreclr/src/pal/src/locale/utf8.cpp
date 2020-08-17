// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    unicode/utf8.c

Abstract:
    Functions to encode and decode UTF-8 strings. This is a port of the C# version from Utf8Encoding.cs.

Revision History:



--*/

#include "pal/utf8.h"
#include "pal/malloc.hpp"

using namespace CorUnix;

#define FASTLOOP

#ifndef COUNTOF
#define COUNTOF(x) (sizeof(x) / sizeof((x)[0]))
#endif

struct CharUnicodeInfo
{
    static const WCHAR HIGH_SURROGATE_START = 0xd800;
    static const WCHAR HIGH_SURROGATE_END = 0xdbff;
    static const WCHAR LOW_SURROGATE_START = 0xdc00;
    static const WCHAR LOW_SURROGATE_END = 0xdfff;
};

struct Char
{
    // Test if the wide character is a high surrogate
    static bool IsHighSurrogate(const WCHAR c)
    {
        return (c & 0xFC00) == CharUnicodeInfo::HIGH_SURROGATE_START;
    }

    // Test if the wide character is a low surrogate
    static bool IsLowSurrogate(const WCHAR c)
    {
        return (c & 0xFC00) == CharUnicodeInfo::LOW_SURROGATE_START;
    }

    // Test if the wide character is a low surrogate
    static bool IsSurrogate(const WCHAR c)
    {
        return (c & 0xF800) == CharUnicodeInfo::HIGH_SURROGATE_START;
    }

    // Test if the wide character is a high surrogate
    static bool IsHighSurrogate(const WCHAR* s, int index)
    {
        return IsHighSurrogate(s[index]);
    }

    // Test if the wide character is a low surrogate
    static bool IsLowSurrogate(const WCHAR* s, int index)
    {
        return IsLowSurrogate(s[index]);
    }

    // Test if the wide character is a low surrogate
    static bool IsSurrogate(const WCHAR* s, int index)
    {
        return IsSurrogate(s[index]);
    }
};

class ArgumentException
{

public:
    ArgumentException(LPCSTR message)
    {
    }

    ArgumentException(LPCSTR message, LPCSTR argName)
    {
    }
};

class ArgumentNullException : public ArgumentException
{
public:
    ArgumentNullException(LPCSTR argName)
        : ArgumentException("Argument is NULL", argName)
    {

    }
};

class ArgumentOutOfRangeException : public ArgumentException
{
public:
    ArgumentOutOfRangeException(LPCSTR argName, LPCSTR message)
        : ArgumentException(message, argName)
    {

    }
};

class InsufficientBufferException : public ArgumentException
{
public:
    InsufficientBufferException(LPCSTR message, LPCSTR argName)
        : ArgumentException(message, argName)
    {

    }
};

class Contract
{
public:
    static void Assert(bool cond, LPCSTR str)
    {
        if (!cond)
        {
            throw ArgumentException(str);
        }
    }

    static void EndContractBlock()
    {
    }
};

class DecoderFallbackException : public ArgumentException
{
    BYTE *bytesUnknown;
    int index;

public:
    DecoderFallbackException(
        LPCSTR message, BYTE bytesUnknown[], int index) : ArgumentException(message)
    {
        this->bytesUnknown = bytesUnknown;
        this->index = index;
    }

    BYTE *BytesUnknown()
    {
        return (bytesUnknown);
    }

    int GetIndex()
    {
        return index;
    }
};

class DecoderFallbackBuffer;

class DecoderFallback
{
public:

    // Fallback
    //
    // Return the appropriate unicode string alternative to the character that need to fall back.

    virtual DecoderFallbackBuffer* CreateFallbackBuffer() = 0;

    // Maximum number of characters that this instance of this fallback could return

    virtual int GetMaxCharCount() = 0;
};

class DecoderReplacementFallback : public DecoderFallback
{
    // Our variables
    WCHAR strDefault[2];
    int strDefaultLength;

public:
    // Construction.  Default replacement fallback uses no best fit and ? replacement string
    DecoderReplacementFallback() : DecoderReplacementFallback(W("?"))
    {
    }

    DecoderReplacementFallback(const WCHAR* replacement)
    {
        // Must not be null
        if (replacement == nullptr)
            throw ArgumentNullException("replacement");
        Contract::EndContractBlock();

        // Make sure it doesn't have bad surrogate pairs
        bool bFoundHigh = false;
        int replacementLength = PAL_wcslen((const WCHAR *)replacement);
        for (int i = 0; i < replacementLength; i++)
        {
            // Found a surrogate?
            if (Char::IsSurrogate(replacement, i))
            {
                // High or Low?
                if (Char::IsHighSurrogate(replacement, i))
                {
                    // if already had a high one, stop
                    if (bFoundHigh)
                        break;  // break & throw at the bFoundHIgh below
                    bFoundHigh = true;
                }
                else
                {
                    // Low, did we have a high?
                    if (!bFoundHigh)
                    {
                        // Didn't have one, make if fail when we stop
                        bFoundHigh = true;
                        break;
                    }

                    // Clear flag
                    bFoundHigh = false;
                }
            }
            // If last was high we're in trouble (not surrogate so not low surrogate, so break)
            else if (bFoundHigh)
                break;
        }
        if (bFoundHigh)
            throw ArgumentException("String 'replacement' contains invalid Unicode code points.", "replacement");

        wcscpy_s(strDefault, COUNTOF(strDefault), replacement);
        strDefaultLength = replacementLength;
    }

    WCHAR* GetDefaultString()
    {
        return strDefault;
    }

    virtual DecoderFallbackBuffer* CreateFallbackBuffer();

    // Maximum number of characters that this instance of this fallback could return
    virtual int GetMaxCharCount()
    {
        return strDefaultLength;
    }
};

class DecoderFallbackBuffer
{
    friend class UTF8Encoding;
    // Most implimentations will probably need an implimenation-specific constructor

    // internal methods that cannot be overriden that let us do our fallback thing
    // These wrap the internal methods so that we can check for people doing stuff that's incorrect

public:
    virtual ~DecoderFallbackBuffer() = default;

    virtual bool Fallback(BYTE bytesUnknown[], int index, int size) = 0;

    // Get next character
    virtual WCHAR GetNextChar() = 0;

    //Back up a character
    virtual bool MovePrevious() = 0;

    // How many chars left in this fallback?
    virtual int GetRemaining() = 0;

    // Clear the buffer
    virtual void Reset()
    {
        while (GetNextChar() != (WCHAR)0);
    }

    // Internal items to help us figure out what we're doing as far as error messages, etc.
    // These help us with our performance and messages internally
protected:
    BYTE*           byteStart;
    WCHAR*          charEnd;

    // Internal reset
    void InternalReset()
    {
        byteStart = nullptr;
        Reset();
    }

    // Set the above values
    // This can't be part of the constructor because EncoderFallbacks would have to know how to impliment these.
    void InternalInitialize(BYTE* byteStart, WCHAR* charEnd)
    {
        this->byteStart = byteStart;
        this->charEnd = charEnd;
    }

    // Fallback the current byte by sticking it into the remaining char buffer.
    // This can only be called by our encodings (other have to use the public fallback methods), so
    // we can use our DecoderNLS here too (except we don't).
    // Returns true if we are successful, false if we can't fallback the character (no buffer space)
    // So caller needs to throw buffer space if return false.
    // Right now this has both bytes and bytes[], since we might have extra bytes, hence the
    // array, and we might need the index, hence the byte*
    // Don't touch ref chars unless we succeed
    virtual bool InternalFallback(BYTE bytes[], BYTE* pBytes, WCHAR** chars, int size)
    {

        Contract::Assert(byteStart != nullptr, "[DecoderFallback.InternalFallback]Used InternalFallback without calling InternalInitialize");

        // See if there's a fallback character and we have an output buffer then copy our string.
        if (this->Fallback(bytes, (int)(pBytes - byteStart - size), size))
        {
            // Copy the chars to our output
            WCHAR ch;
            WCHAR* charTemp = *chars;
            bool bHighSurrogate = false;
            while ((ch = GetNextChar()) != 0)
            {
                // Make sure no mixed up surrogates
                if (Char::IsSurrogate(ch))
                {
                    if (Char::IsHighSurrogate(ch))
                    {
                        // High Surrogate
                        if (bHighSurrogate)
                            throw ArgumentException("String 'chars' contains invalid Unicode code points.");
                        bHighSurrogate = true;
                    }
                    else
                    {
                        // Low surrogate
                        if (!bHighSurrogate)
                            throw ArgumentException("String 'chars' contains invalid Unicode code points.");
                        bHighSurrogate = false;
                    }
                }

                if (charTemp >= charEnd)
                {
                    // No buffer space
                    return false;
                }

                *(charTemp++) = ch;
            }

            // Need to make sure that bHighSurrogate isn't true
            if (bHighSurrogate)
                throw ArgumentException("String 'chars' contains invalid Unicode code points.");

            // Now we aren't going to be false, so its OK to update chars
            *chars = charTemp;
        }

        return true;
    }

    // This version just counts the fallback and doesn't actually copy anything.
    virtual int InternalFallback(BYTE bytes[], BYTE* pBytes, int size)
        // Right now this has both bytes[] and BYTE* bytes, since we might have extra bytes, hence the
        // array, and we might need the index, hence the byte*
    {

        Contract::Assert(byteStart != nullptr, "[DecoderFallback.InternalFallback]Used InternalFallback without calling InternalInitialize");

        // See if there's a fallback character and we have an output buffer then copy our string.
        if (this->Fallback(bytes, (int)(pBytes - byteStart - size), size))
        {
            int count = 0;

            WCHAR ch;
            bool bHighSurrogate = false;
            while ((ch = GetNextChar()) != 0)
            {
                // Make sure no mixed up surrogates
                if (Char::IsSurrogate(ch))
                {
                    if (Char::IsHighSurrogate(ch))
                    {
                        // High Surrogate
                        if (bHighSurrogate)
                            throw ArgumentException("String 'chars' contains invalid Unicode code points.");
                        bHighSurrogate = true;
                    }
                    else
                    {
                        // Low surrogate
                        if (!bHighSurrogate)
                            throw ArgumentException("String 'chars' contains invalid Unicode code points.");
                        bHighSurrogate = false;
                    }
                }

                count++;
            }

            // Need to make sure that bHighSurrogate isn't true
            if (bHighSurrogate)
                throw ArgumentException("String 'chars' contains invalid Unicode code points.");

            return count;
        }

        // If no fallback return 0
        return 0;
    }

    // private helper methods
    void ThrowLastBytesRecursive(BYTE bytesUnknown[])
    {
        throw ArgumentException("Recursive fallback not allowed");
    }
};

class DecoderReplacementFallbackBuffer : public DecoderFallbackBuffer
{
    // Store our default string
    WCHAR strDefault[2];
    int strDefaultLength;
    int fallbackCount = -1;
    int fallbackIndex = -1;

public:
    // Construction
    DecoderReplacementFallbackBuffer(DecoderReplacementFallback* fallback)
    {
        wcscpy_s(strDefault, COUNTOF(strDefault), fallback->GetDefaultString());
        strDefaultLength = PAL_wcslen((const WCHAR *)fallback->GetDefaultString());
    }

    // Fallback Methods
    virtual bool Fallback(BYTE bytesUnknown[], int index, int size)
    {
        // We expect no previous fallback in our buffer
        // We can't call recursively but others might (note, we don't test on last char!!!)
        if (fallbackCount >= 1)
        {
            ThrowLastBytesRecursive(bytesUnknown);
        }

        // Go ahead and get our fallback
        if (strDefaultLength == 0)
            return false;

        fallbackCount = strDefaultLength;
        fallbackIndex = -1;

        return true;
    }

    virtual WCHAR GetNextChar()
    {
        // We want it to get < 0 because == 0 means that the current/last character is a fallback
        // and we need to detect recursion.  We could have a flag but we already have this counter.
        fallbackCount--;
        fallbackIndex++;

        // Do we have anything left? 0 is now last fallback char, negative is nothing left
        if (fallbackCount < 0)
            return '\0';

        // Need to get it out of the buffer.
        // Make sure it didn't wrap from the fast count-- path
        if (fallbackCount == INT_MAX)
        {
            fallbackCount = -1;
            return '\0';
        }

        // Now make sure its in the expected range
        Contract::Assert(fallbackIndex < strDefaultLength && fallbackIndex >= 0,
            "Index exceeds buffer range");

        return strDefault[fallbackIndex];
    }

    virtual bool MovePrevious()
    {
        // Back up one, only if we just processed the last character (or earlier)
        if (fallbackCount >= -1 && fallbackIndex >= 0)
        {
            fallbackIndex--;
            fallbackCount++;
            return true;
        }

        // Return false 'cause we couldn't do it.
        return false;
    }

    // How many characters left to output?
    virtual int GetRemaining()
    {
        // Our count is 0 for 1 character left.
        return (fallbackCount < 0) ? 0 : fallbackCount;
    }

    // Clear the buffer
    virtual void Reset()
    {
        fallbackCount = -1;
        fallbackIndex = -1;
        byteStart = nullptr;
    }

    // This version just counts the fallback and doesn't actually copy anything.
    virtual int InternalFallback(BYTE bytes[], BYTE* pBytes, int size)
        // Right now this has both bytes and bytes[], since we might have extra bytes, hence the
        // array, and we might need the index, hence the byte*
    {
        // return our replacement string Length
        return strDefaultLength;
    }
};

class DecoderExceptionFallbackBuffer : public DecoderFallbackBuffer
{
public:
    DecoderExceptionFallbackBuffer()
    {
    }

    virtual bool Fallback(BYTE bytesUnknown[], int index, int size)
    {
        throw DecoderFallbackException(
            "Unable to translate UTF-8 character to Unicode", bytesUnknown, index);
    }

    virtual WCHAR GetNextChar()
    {
        return 0;
    }

    virtual bool MovePrevious()
    {
        // Exception fallback doesn't have anywhere to back up to.
        return false;
    }

    // Exceptions are always empty
    virtual int GetRemaining()
    {
        return 0;
    }

};

class DecoderExceptionFallback : public DecoderFallback
{
    // Construction
public:
    DecoderExceptionFallback()
    {
    }

    virtual DecoderFallbackBuffer* CreateFallbackBuffer()
    {
        return InternalNew<DecoderExceptionFallbackBuffer>();
    }

    // Maximum number of characters that this instance of this fallback could return
    virtual int GetMaxCharCount()
    {
        return 0;
    }
};

DecoderFallbackBuffer* DecoderReplacementFallback::CreateFallbackBuffer()
{
    return InternalNew<DecoderReplacementFallbackBuffer>(this);
}

class EncoderFallbackException : public ArgumentException
{
    WCHAR   charUnknown;
    WCHAR   charUnknownHigh;
    WCHAR   charUnknownLow;
    int     index;

public:
    EncoderFallbackException(
        LPCSTR message, WCHAR charUnknown, int index) : ArgumentException(message)
    {
        this->charUnknown = charUnknown;
        this->index = index;
    }

    EncoderFallbackException(
        LPCSTR message, WCHAR charUnknownHigh, WCHAR charUnknownLow, int index) : ArgumentException(message)
    {
        if (!Char::IsHighSurrogate(charUnknownHigh))
        {
            throw ArgumentOutOfRangeException("charUnknownHigh",
                "Argument out of range 0xD800..0xDBFF");
        }
        if (!Char::IsLowSurrogate(charUnknownLow))
        {
            throw ArgumentOutOfRangeException("charUnknownLow",
                "Argument out of range 0xDC00..0xDFFF");
        }
        Contract::EndContractBlock();

        this->charUnknownHigh = charUnknownHigh;
        this->charUnknownLow = charUnknownLow;
        this->index = index;
    }

    WCHAR GetCharUnknown()
    {
        return (charUnknown);
    }

    WCHAR GetCharUnknownHigh()
    {
        return (charUnknownHigh);
    }

    WCHAR GetCharUnknownLow()
    {
        return (charUnknownLow);
    }

    int GetIndex()
    {
        return index;
    }

    // Return true if the unknown character is a surrogate pair.
    bool IsUnknownSurrogate()
    {
        return (charUnknownHigh != '\0');
    }
};

class EncoderFallbackBuffer;

class EncoderFallback
{
public:

    // Fallback
    //
    // Return the appropriate unicode string alternative to the character that need to fall back.

    virtual EncoderFallbackBuffer* CreateFallbackBuffer() = 0;

    // Maximum number of characters that this instance of this fallback could return
    virtual int GetMaxCharCount() = 0;
};

class EncoderReplacementFallback : public EncoderFallback
{
    // Our variables
    WCHAR strDefault[2];
    int strDefaultLength;

public:
    // Construction.  Default replacement fallback uses no best fit and ? replacement string
    EncoderReplacementFallback() : EncoderReplacementFallback(W("?"))
    {
    }

    EncoderReplacementFallback(const WCHAR* replacement)
    {
        // Must not be null
        if (replacement == nullptr)
            throw ArgumentNullException("replacement");
        Contract::EndContractBlock();

        // Make sure it doesn't have bad surrogate pairs
        bool bFoundHigh = false;
        int replacementLength = PAL_wcslen((const WCHAR *)replacement);
        for (int i = 0; i < replacementLength; i++)
        {
            // Found a surrogate?
            if (Char::IsSurrogate(replacement, i))
            {
                // High or Low?
                if (Char::IsHighSurrogate(replacement, i))
                {
                    // if already had a high one, stop
                    if (bFoundHigh)
                        break;  // break & throw at the bFoundHIgh below
                    bFoundHigh = true;
                }
                else
                {
                    // Low, did we have a high?
                    if (!bFoundHigh)
                    {
                        // Didn't have one, make if fail when we stop
                        bFoundHigh = true;
                        break;
                    }

                    // Clear flag
                    bFoundHigh = false;
                }
            }
            // If last was high we're in trouble (not surrogate so not low surrogate, so break)
            else if (bFoundHigh)
                break;
        }
        if (bFoundHigh)
            throw ArgumentException("String 'replacement' contains invalid Unicode code points.", "replacement");

        wcscpy_s(strDefault, COUNTOF(strDefault), replacement);
        strDefaultLength = replacementLength;
    }

    WCHAR* GetDefaultString()
    {
        return strDefault;
    }

    virtual EncoderFallbackBuffer* CreateFallbackBuffer();

    // Maximum number of characters that this instance of this fallback could return
    virtual int GetMaxCharCount()
    {
        return strDefaultLength;
    }
};

class EncoderFallbackBuffer
{
    friend class UTF8Encoding;
    // Most implementations will probably need an implemenation-specific constructor

    // Public methods that cannot be overriden that let us do our fallback thing
    // These wrap the internal methods so that we can check for people doing stuff that is incorrect

public:
    virtual ~EncoderFallbackBuffer() = default;

    virtual bool Fallback(WCHAR charUnknown, int index) = 0;

    virtual bool Fallback(WCHAR charUnknownHigh, WCHAR charUnknownLow, int index) = 0;

    // Get next character
    virtual WCHAR GetNextChar() = 0;

    // Back up a character
    virtual bool MovePrevious() = 0;

    // How many chars left in this fallback?
    virtual int GetRemaining() = 0;

    // Not sure if this should be public or not.
    // Clear the buffer
    virtual void Reset()
    {
        while (GetNextChar() != (WCHAR)0);
    }

    // Internal items to help us figure out what we're doing as far as error messages, etc.
    // These help us with our performance and messages internally
protected:
    WCHAR*          charStart;
    WCHAR*          charEnd;
    bool            setEncoder;
    bool            bUsedEncoder;
    bool            bFallingBack = false;
    int             iRecursionCount = 0;
    static const int iMaxRecursion = 250;

    // Internal Reset
    // For example, what if someone fails a conversion and wants to reset one of our fallback buffers?
    void InternalReset()
    {
        charStart = nullptr;
        bFallingBack = false;
        iRecursionCount = 0;
        Reset();
    }

    // Set the above values
    // This can't be part of the constructor because EncoderFallbacks would have to know how to impliment these.
    void InternalInitialize(WCHAR* charStart, WCHAR* charEnd, bool setEncoder)
    {
        this->charStart = charStart;
        this->charEnd = charEnd;
        this->setEncoder = setEncoder;
        this->bUsedEncoder = false;
        this->bFallingBack = false;
        this->iRecursionCount = 0;
    }

    WCHAR InternalGetNextChar()
    {
        WCHAR ch = GetNextChar();
        bFallingBack = (ch != 0);
        if (ch == 0) iRecursionCount = 0;
        return ch;
    }

    // Fallback the current character using the remaining buffer and encoder if necessary
    // This can only be called by our encodings (other have to use the public fallback methods), so
    // we can use our EncoderNLS here too.
    // setEncoder is true if we're calling from a GetBytes method, false if we're calling from a GetByteCount
    //
    // Note that this could also change the contents of this->encoder, which is the same
    // object that the caller is using, so the caller could mess up the encoder for us
    // if they aren't careful.
    virtual bool InternalFallback(WCHAR ch, WCHAR** chars)
    {
        // Shouldn't have null charStart
        Contract::Assert(charStart != nullptr,
            "[EncoderFallback.InternalFallbackBuffer]Fallback buffer is not initialized");

        // Get our index, remember chars was preincremented to point at next char, so have to -1
        int index = (int)(*chars - charStart) - 1;

        // See if it was a high surrogate
        if (Char::IsHighSurrogate(ch))
        {
            // See if there's a low surrogate to go with it
            if (*chars >= this->charEnd)
            {
                // Nothing left in input buffer
                // No input, return 0
            }
            else
            {
                // Might have a low surrogate
                WCHAR cNext = **chars;
                if (Char::IsLowSurrogate(cNext))
                {
                    // If already falling back then fail
                    if (bFallingBack && iRecursionCount++ > iMaxRecursion)
                        ThrowLastCharRecursive(ch, cNext);

                    // Next is a surrogate, add it as surrogate pair, and increment chars
                    (*chars)++;
                    bFallingBack = Fallback(ch, cNext, index);
                    return bFallingBack;
                }

                // Next isn't a low surrogate, just fallback the high surrogate
            }
        }

        // If already falling back then fail
        if (bFallingBack && iRecursionCount++ > iMaxRecursion)
            ThrowLastCharRecursive((int)ch);

        // Fall back our char
        bFallingBack = Fallback(ch, index);

        return bFallingBack;
    }

    // private helper methods
    void ThrowLastCharRecursive(WCHAR highSurrogate, WCHAR lowSurrogate)
    {
        // Throw it, using our complete character
        throw ArgumentException("Recursive fallback not allowed", "chars");
    }

    void ThrowLastCharRecursive(int utf32Char)
    {
        throw ArgumentException("Recursive fallback not allowed", "chars");
    }

};

class EncoderReplacementFallbackBuffer : public EncoderFallbackBuffer
{
    // Store our default string
    WCHAR strDefault[4];
    int strDefaultLength;
    int fallbackCount = -1;
    int fallbackIndex = -1;
public:
    // Construction
    EncoderReplacementFallbackBuffer(EncoderReplacementFallback* fallback)
    {
        // 2X in case we're a surrogate pair
        wcscpy_s(strDefault, COUNTOF(strDefault), fallback->GetDefaultString());
        wcscat_s(strDefault, COUNTOF(strDefault), fallback->GetDefaultString());
        strDefaultLength = 2 * PAL_wcslen((const WCHAR *)fallback->GetDefaultString());

    }

    // Fallback Methods
    virtual bool Fallback(WCHAR charUnknown, int index)
    {
        // If we had a buffer already we're being recursive, throw, it's probably at the suspect
        // character in our array.
        if (fallbackCount >= 1)
        {
            // If we're recursive we may still have something in our buffer that makes this a surrogate
            if (Char::IsHighSurrogate(charUnknown) && fallbackCount >= 0 &&
                Char::IsLowSurrogate(strDefault[fallbackIndex + 1]))
                ThrowLastCharRecursive(charUnknown, strDefault[fallbackIndex + 1]);

            // Nope, just one character
            ThrowLastCharRecursive((int)charUnknown);
        }

        // Go ahead and get our fallback
        // Divide by 2 because we aren't a surrogate pair
        fallbackCount = strDefaultLength / 2;
        fallbackIndex = -1;

        return fallbackCount != 0;
    }

    virtual bool Fallback(WCHAR charUnknownHigh, WCHAR charUnknownLow, int index)
    {
        // Double check input surrogate pair
        if (!Char::IsHighSurrogate(charUnknownHigh))
            throw ArgumentOutOfRangeException("charUnknownHigh",
            "Argument out of range 0xD800..0xDBFF");

        if (!Char::IsLowSurrogate(charUnknownLow))
            throw ArgumentOutOfRangeException("charUnknownLow",
            "Argument out of range 0xDC00..0xDFFF");
        Contract::EndContractBlock();

        // If we had a buffer already we're being recursive, throw, it's probably at the suspect
        // character in our array.
        if (fallbackCount >= 1)
            ThrowLastCharRecursive(charUnknownHigh, charUnknownLow);

        // Go ahead and get our fallback
        fallbackCount = strDefaultLength;
        fallbackIndex = -1;

        return fallbackCount != 0;
    }

    virtual WCHAR GetNextChar()
    {
        // We want it to get < 0 because == 0 means that the current/last character is a fallback
        // and we need to detect recursion.  We could have a flag but we already have this counter.
        fallbackCount--;
        fallbackIndex++;

        // Do we have anything left? 0 is now last fallback char, negative is nothing left
        if (fallbackCount < 0)
            return '\0';

        // Need to get it out of the buffer.
        // Make sure it didn't wrap from the fast count-- path
        if (fallbackCount == INT_MAX)
        {
            fallbackCount = -1;
            return '\0';
        }

        // Now make sure its in the expected range
        Contract::Assert(fallbackIndex < strDefaultLength && fallbackIndex >= 0,
            "Index exceeds buffer range");

        return strDefault[fallbackIndex];
    }

    virtual bool MovePrevious()
    {
        // Back up one, only if we just processed the last character (or earlier)
        if (fallbackCount >= -1 && fallbackIndex >= 0)
        {
            fallbackIndex--;
            fallbackCount++;
            return true;
        }

        // Return false 'cause we couldn't do it.
        return false;
    }

    // How many characters left to output?
    virtual int GetRemaining()
    {
        // Our count is 0 for 1 character left.
        return (fallbackCount < 0) ? 0 : fallbackCount;
    }

    // Clear the buffer
    virtual void Reset()
    {
        fallbackCount = -1;
        fallbackIndex = 0;
        charStart = nullptr;
        bFallingBack = false;
    }
};

class EncoderExceptionFallbackBuffer : public EncoderFallbackBuffer
{
public:
    EncoderExceptionFallbackBuffer()
    {
    }

    virtual bool Fallback(WCHAR charUnknown, int index)
    {
        // Fall back our char
        throw EncoderFallbackException("Unable to translate Unicode character to UTF-8", charUnknown, index);
    }

    virtual bool Fallback(WCHAR charUnknownHigh, WCHAR charUnknownLow, int index)
    {
        if (!Char::IsHighSurrogate(charUnknownHigh))
        {
            throw ArgumentOutOfRangeException("charUnknownHigh",
                "Argument out of range 0xD800..0xDBFF");
        }
        if (!Char::IsLowSurrogate(charUnknownLow))
        {
            throw ArgumentOutOfRangeException("charUnknownLow",
                "Argument out of range 0xDC00..0xDFFF");
        }
        Contract::EndContractBlock();

        //int iTemp = Char::ConvertToUtf32(charUnknownHigh, charUnknownLow);

        // Fall back our char
        throw EncoderFallbackException(
            "Unable to translate Unicode character to UTF-8", charUnknownHigh, charUnknownLow, index);
    }

    virtual WCHAR GetNextChar()
    {
        return 0;
    }

    virtual bool MovePrevious()
    {
        // Exception fallback doesn't have anywhere to back up to.
        return false;
    }

    // Exceptions are always empty
    virtual int GetRemaining()
    {
        return 0;
    }
};

class EncoderExceptionFallback : public EncoderFallback
{
    // Construction
public:
    EncoderExceptionFallback()
    {
    }

    virtual EncoderFallbackBuffer* CreateFallbackBuffer()
    {
        return InternalNew<EncoderExceptionFallbackBuffer>();
    }

    // Maximum number of characters that this instance of this fallback could return
    virtual int GetMaxCharCount()
    {
        return 0;
    }
};

EncoderFallbackBuffer* EncoderReplacementFallback::CreateFallbackBuffer()
{
    return InternalNew<EncoderReplacementFallbackBuffer>(this);
}

class UTF8Encoding
{
    EncoderFallback* encoderFallback;
    // Instances of the two possible fallbacks. The constructor parameter
    // determines which one to use.
    EncoderReplacementFallback encoderReplacementFallback;
    EncoderExceptionFallback encoderExceptionFallback;

    DecoderFallback* decoderFallback;
    // Instances of the two possible fallbacks. The constructor parameter
    // determines which one to use.
    DecoderReplacementFallback decoderReplacementFallback;
    DecoderExceptionFallback decoderExceptionFallback;

    bool InRange(WCHAR c, WCHAR begin, WCHAR end)
    {
        return begin <= c && c <= end;
    }

    size_t PtrDiff(WCHAR* ptr1, WCHAR* ptr2)
    {
        return ptr1 - ptr2;
    }

    size_t PtrDiff(BYTE* ptr1, BYTE* ptr2)
    {
        return ptr1 - ptr2;
    }

    void ThrowBytesOverflow()
    {
        // Special message to include fallback type in case fallback's GetMaxCharCount is broken
        // This happens if user has implimented an encoder fallback with a broken GetMaxCharCount
        throw InsufficientBufferException("The output byte buffer is too small to contain the encoded data", "bytes");
    }

    void ThrowBytesOverflow(bool nothingEncoded)
    {
        // Special message to include fallback type in case fallback's GetMaxCharCount is broken
        // This happens if user has implimented an encoder fallback with a broken GetMaxCharCount
        if (nothingEncoded){
            ThrowBytesOverflow();
        }
    }

    void ThrowCharsOverflow()
    {
        // Special message to include fallback type in case fallback's GetMaxCharCount is broken
        // This happens if user has implimented a decoder fallback with a broken GetMaxCharCount
        throw InsufficientBufferException("The output char buffer is too small to contain the encoded data", "chars");
    }

    void ThrowCharsOverflow(bool nothingEncoded)
    {
        // Special message to include fallback type in case fallback's GetMaxCharCount is broken
        // This happens if user has implimented an decoder fallback with a broken GetMaxCharCount
        if (nothingEncoded){
            ThrowCharsOverflow();
        }
    }

    // During GetChars we had an invalid byte sequence
    // pSrc is backed up to the start of the bad sequence if we didn't have room to
    // fall it back.  Otherwise pSrc remains where it is.
    bool FallbackInvalidByteSequence(BYTE** pSrc, int ch, DecoderFallbackBuffer* fallback, WCHAR** pTarget)
    {
        // Get our byte[]
        BYTE* pStart = *pSrc;
        BYTE bytesUnknown[3];
        int size = GetBytesUnknown(pStart, ch, bytesUnknown);

        // Do the actual fallback
        if (!fallback->InternalFallback(bytesUnknown, *pSrc, pTarget, size))
        {
            // Oops, it failed, back up to pStart
            *pSrc = pStart;
            return false;
        }

        // It worked
        return true;
    }

    int FallbackInvalidByteSequence(BYTE* pSrc, int ch, DecoderFallbackBuffer *fallback)
    {
        // Get our byte[]
        BYTE bytesUnknown[3];
        int size = GetBytesUnknown(pSrc, ch, bytesUnknown);

        // Do the actual fallback
        int count = fallback->InternalFallback(bytesUnknown, pSrc, size);

        // # of fallback chars expected.
        // Note that we only get here for "long" sequences, and have already unreserved
        // the count that we prereserved for the input bytes
        return count;
    }

    int GetBytesUnknown(BYTE* pSrc, int ch, BYTE* bytesUnknown)
    {
        int size;

        // See if it was a plain char
        // (have to check >= 0 because we have all sorts of wierd bit flags)
        if (ch < 0x100 && ch >= 0)
        {
            pSrc--;
            bytesUnknown[0] = (BYTE)ch;
            size =  1;
        }
        // See if its an unfinished 2 byte sequence
        else if ((ch & (SupplimentarySeq | ThreeByteSeq)) == 0)
        {
            pSrc--;
            bytesUnknown[0] = (BYTE)((ch & 0x1F) | 0xc0);
            size = 1;
        }
        // So now we're either 2nd byte of 3 or 4 byte sequence or
        // we hit a non-trail byte or we ran out of space for 3rd byte of 4 byte sequence
        // 1st check if its a 4 byte sequence
        else if ((ch & SupplimentarySeq) != 0)
        {
            //  3rd byte of 4 byte sequence?
            if ((ch & (FinalByte >> 6)) != 0)
            {
                // 3rd byte of 4 byte sequence
                pSrc -= 3;
                bytesUnknown[0] = (BYTE)(((ch >> 12) & 0x07) | 0xF0);
                bytesUnknown[1] = (BYTE)(((ch >> 6) & 0x3F) | 0x80);
                bytesUnknown[2] = (BYTE)(((ch)& 0x3F) | 0x80);
                size = 3;
            }
            else if ((ch & (FinalByte >> 12)) != 0)
            {
                // 2nd byte of a 4 byte sequence
                pSrc -= 2;
                bytesUnknown[0] = (BYTE)(((ch >> 6) & 0x07) | 0xF0);
                bytesUnknown[1] = (BYTE)(((ch)& 0x3F) | 0x80);
                size = 2;
            }
            else
            {
                // 4th byte of a 4 byte sequence
                pSrc--;
                bytesUnknown[0] = (BYTE)(((ch)& 0x07) | 0xF0);
                size = 1;
            }
        }
        else
        {
            // 2nd byte of 3 byte sequence?
            if ((ch & (FinalByte >> 6)) != 0)
            {
                // So its 2nd byte of a 3 byte sequence
                pSrc -= 2;
                bytesUnknown[0] = (BYTE)(((ch >> 6) & 0x0F) | 0xE0);
                bytesUnknown[1] = (BYTE)(((ch)& 0x3F) | 0x80);
                size = 2;
            }
            else
            {
                // 1st byte of a 3 byte sequence
                pSrc--;
                bytesUnknown[0] = (BYTE)(((ch)& 0x0F) | 0xE0);
                size = 1;
            }
        }

        return size;
    }

public:

    UTF8Encoding(bool isThrowException)
        : encoderReplacementFallback(W("\xFFFD")), decoderReplacementFallback(W("\xFFFD"))
    {
        if (isThrowException)
        {
            encoderFallback = &encoderExceptionFallback;
            decoderFallback = &decoderExceptionFallback;
        }
        else
        {
            encoderFallback = &encoderReplacementFallback;
            decoderFallback = &decoderReplacementFallback;
        }
    }

    // These are bitmasks used to maintain the state in the decoder. They occupy the higher bits
    // while the actual character is being built in the lower bits. They are shifted together
    // with the actual bits of the character.

    // bits 30 & 31 are used for pending bits fixup
    const int FinalByte = 1 << 29;
    const int SupplimentarySeq = 1 << 28;
    const int ThreeByteSeq = 1 << 27;

    int GetCharCount(BYTE* bytes, int count)
    {
        Contract::Assert(bytes != nullptr, "[UTF8Encoding.GetCharCount]bytes!=nullptr");
        Contract::Assert(count >= 0, "[UTF8Encoding.GetCharCount]count >=0");

        // Initialize stuff
        BYTE *pSrc = bytes;
        BYTE *pEnd = pSrc + count;

        // Start by assuming we have as many as count, charCount always includes the adjustment
        // for the character being decoded
        int charCount = count;
        int ch = 0;
        DecoderFallbackBuffer *fallback = nullptr;

        for (;;)
        {
            // SLOWLOOP: does all range checks, handles all special cases, but it is slow
            if (pSrc >= pEnd) {
                break;
            }

            // read next byte. The JIT optimization seems to be getting confused when
            // compiling "ch = *pSrc++;", so rather use "ch = *pSrc; pSrc++;" instead
            int cha = *pSrc;

            if (ch == 0) {
                // no pending bits
                goto ReadChar;
            }

            pSrc++;

            // we are expecting to see trailing bytes like 10vvvvvv
            if ((cha & 0xC0) != 0x80) {
                // This can be a valid starting byte for another UTF8 byte sequence, so let's put
                // the current byte back, and try to see if this is a valid byte for another UTF8 byte sequence
                pSrc--;
                charCount += (ch >> 30);
                goto InvalidByteSequence;
            }

            // fold in the new byte
            ch = (ch << 6) | (cha & 0x3F);

            if ((ch & FinalByte) == 0) {
                Contract::Assert((ch & (SupplimentarySeq | ThreeByteSeq)) != 0,
                    "[UTF8Encoding.GetChars]Invariant volation");

                if ((ch & SupplimentarySeq) != 0) {
                    if ((ch & (FinalByte >> 6)) != 0) {
                        // this is 3rd byte (of 4 byte supplimentary) - nothing to do
                        continue;
                    }

                    // 2nd byte, check for non-shortest form of supplimentary char and the valid
                    // supplimentary characters in range 0x010000 - 0x10FFFF at the same time
                    if (!InRange(ch & 0x1F0, 0x10, 0x100)) {
                        goto InvalidByteSequence;
                    }
                }
                else {
                    // Must be 2nd byte of a 3-byte sequence
                    // check for non-shortest form of 3 byte seq
                    if ((ch & (0x1F << 5)) == 0 ||                  // non-shortest form
                        (ch & (0xF800 >> 6)) == (0xD800 >> 6))     // illegal individually encoded surrogate
                    {
                        goto InvalidByteSequence;
                    }
                }
                continue;
            }

            // ready to punch

            // adjust for surrogates in non-shortest form
            if ((ch & (SupplimentarySeq | 0x1F0000)) == SupplimentarySeq) {
                charCount--;
            }
            goto EncodeChar;

        InvalidByteSequence:
            // this code fragment should be close to the gotos referencing it
            // Have to do fallback for invalid bytes
            if (fallback == nullptr)
            {
                fallback = decoderFallback->CreateFallbackBuffer();
                fallback->InternalInitialize(bytes, nullptr);
            }
            charCount += FallbackInvalidByteSequence(pSrc, ch, fallback);

            ch = 0;
            continue;

        ReadChar:
            ch = *pSrc;
            pSrc++;

        ProcessChar:
            if (ch > 0x7F) {
                // If its > 0x7F, its start of a new multi-byte sequence

                // Long sequence, so unreserve our char.
                charCount--;

                // bit 6 has to be non-zero for start of multibyte chars.
                if ((ch & 0x40) == 0) {
                    // Unexpected trail byte
                    goto InvalidByteSequence;
                }

                // start a new long code
                if ((ch & 0x20) != 0) {
                    if ((ch & 0x10) != 0) {
                        // 4 byte encoding - supplimentary character (2 surrogates)

                        ch &= 0x0F;

                        // check that bit 4 is zero and the valid supplimentary character
                        // range 0x000000 - 0x10FFFF at the same time
                        if (ch > 0x04) {
                            ch |= 0xf0;
                            goto InvalidByteSequence;
                        }

                        // Add bit flags so that when we check new characters & rotate we'll be flagged correctly.
                        // Final byte flag, count fix if we don't make final byte & supplimentary sequence flag.
                        ch |= (FinalByte >> 3 * 6) |  // Final byte is 3 more bytes from now
                            (1 << 30) |           // If it dies on next byte we'll need an extra char
                            (3 << (30 - 2 * 6)) |     // If it dies on last byte we'll need to subtract a char
                            (SupplimentarySeq) | (SupplimentarySeq >> 6) |
                            (SupplimentarySeq >> 2 * 6) | (SupplimentarySeq >> 3 * 6);

                        // Our character count will be 2 characters for these 4 bytes, so subtract another char
                        charCount--;
                    }
                    else {
                        // 3 byte encoding
                        // Add bit flags so that when we check new characters & rotate we'll be flagged correctly.
                        ch = (ch & 0x0F) | ((FinalByte >> 2 * 6) | (1 << 30) |
                            (ThreeByteSeq) | (ThreeByteSeq >> 6) | (ThreeByteSeq >> 2 * 6));

                        // We'll expect 1 character for these 3 bytes, so subtract another char.
                        charCount--;
                    }
                }
                else {
                    // 2 byte encoding

                    ch &= 0x1F;

                    // check for non-shortest form
                    if (ch <= 1) {
                        ch |= 0xc0;
                        goto InvalidByteSequence;
                    }

                    // Add bit flags so we'll be flagged correctly
                    ch |= (FinalByte >> 6);
                }
                continue;
            }

        EncodeChar:

#ifdef FASTLOOP
            int availableBytes = PtrDiff(pEnd, pSrc);

            // don't fall into the fast decoding loop if we don't have enough bytes
            if (availableBytes <= 13) {
                // try to get over the remainder of the ascii characters fast though
            BYTE* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                while (pSrc < pLocalEnd) {
                    ch = *pSrc;
                    pSrc++;

                    if (ch > 0x7F)
                        goto ProcessChar;
                }
                // we are done
                ch = 0;
                break;
            }

            // To compute the upper bound, assume that all characters are ASCII characters at this point,
            //  the boundary will be decreased for every non-ASCII character we encounter
            // Also, we need 7 chars reserve for the unrolled ansi decoding loop and for decoding of multibyte sequences
            BYTE *pStop = pSrc + availableBytes - 7;

            while (pSrc < pStop) {
                ch = *pSrc;
                pSrc++;

                if (ch > 0x7F) {
                    goto LongCode;
                }

                // get pSrc 2-byte aligned
                if (((size_t)pSrc & 0x1) != 0) {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F) {
                        goto LongCode;
                    }
                }

                // get pSrc 4-byte aligned
                if (((size_t)pSrc & 0x2) != 0) {
                    ch = *(USHORT*)pSrc;
                    if ((ch & 0x8080) != 0) {
                        goto LongCodeWithMask16;
                    }
                    pSrc += 2;
                }


                // Run 8 + 8 characters at a time!
                while (pSrc < pStop) {
                    ch = *(int*)pSrc;
                    int chb = *(int*)(pSrc + 4);
                    if (((ch | chb) & (int)0x80808080) != 0) {
                        goto LongCodeWithMask32;
                    }
                    pSrc += 8;

                    // This is a really small loop - unroll it
                    if (pSrc >= pStop)
                        break;

                    ch = *(int*)pSrc;
                    chb = *(int*)(pSrc + 4);
                    if (((ch | chb) & (int)0x80808080) != 0) {
                        goto LongCodeWithMask32;
                    }
                    pSrc += 8;
                }
                break;

#if BIGENDIAN
            LongCodeWithMask32 :
                // be careful about the sign extension
                ch = (int)(((uint)ch) >> 16);
            LongCodeWithMask16:
                ch = (int)(((uint)ch) >> 8);
#else // BIGENDIAN
            LongCodeWithMask32:
            LongCodeWithMask16:
                ch &= 0xFF;
#endif // BIGENDIAN
                pSrc++;
                if (ch <= 0x7F) {
                    continue;
                }

            LongCode:
                int chc = *pSrc;
                pSrc++;

                if (
                    // bit 6 has to be zero
                    (ch & 0x40) == 0 ||
                    // we are expecting to see trailing bytes like 10vvvvvv
                    (chc & 0xC0) != 0x80)
                {
                    goto BadLongCode;
                }

                chc &= 0x3F;

                // start a new long code
                if ((ch & 0x20) != 0) {

                    // fold the first two bytes together
                    chc |= (ch & 0x0F) << 6;

                    if ((ch & 0x10) != 0) {
                        // 4 byte encoding - surrogate
                        ch = *pSrc;
                        if (
                            // check that bit 4 is zero, the non-shortest form of surrogate
                            // and the valid surrogate range 0x000000 - 0x10FFFF at the same time
                            !InRange(chc >> 4, 0x01, 0x10) ||
                            // we are expecting to see trailing bytes like 10vvvvvv
                            (ch & 0xC0) != 0x80)
                        {
                            goto BadLongCode;
                        }

                        chc = (chc << 6) | (ch & 0x3F);

                        ch = *(pSrc + 1);
                        // we are expecting to see trailing bytes like 10vvvvvv
                        if ((ch & 0xC0) != 0x80) {
                            goto BadLongCode;
                        }
                        pSrc += 2;

                        // extra byte
                        charCount--;
                    }
                    else {
                        // 3 byte encoding
                        ch = *pSrc;
                        if (
                            // check for non-shortest form of 3 byte seq
                            (chc & (0x1F << 5)) == 0 ||
                            // Can't have surrogates here.
                            (chc & (0xF800 >> 6)) == (0xD800 >> 6) ||
                            // we are expecting to see trailing bytes like 10vvvvvv
                            (ch & 0xC0) != 0x80)
                        {
                            goto BadLongCode;
                        }
                        pSrc++;

                        // extra byte
                        charCount--;
                    }
                }
                else {
                    // 2 byte encoding

                    // check for non-shortest form
                    if ((ch & 0x1E) == 0) {
                        goto BadLongCode;
                    }
                }

                // extra byte
                charCount--;
            }
#endif // FASTLOOP

            // no pending bits at this point
            ch = 0;
            continue;

        BadLongCode:
            pSrc -= 2;
            ch = 0;
            continue;
        }

        // May have a problem if we have to flush
        if (ch != 0)
        {
            // We were already adjusting for these, so need to unadjust
            charCount += (ch >> 30);
            // Have to do fallback for invalid bytes
            if (fallback == nullptr)
            {
                fallback = decoderFallback->CreateFallbackBuffer();
                fallback->InternalInitialize(bytes, nullptr);
            }
            charCount += FallbackInvalidByteSequence(pSrc, ch, fallback);
        }

        // Shouldn't have anything in fallback buffer for GetCharCount
        // (don't have to check m_throwOnOverflow for count)
        Contract::Assert(fallback == nullptr || fallback->GetRemaining() == 0,
            "[UTF8Encoding.GetCharCount]Expected empty fallback buffer at end");

        InternalDelete(fallback);

        return charCount;

    }

    int GetChars(BYTE* bytes, int byteCount, WCHAR* chars, int charCount)
    {
        Contract::Assert(chars != nullptr, "[UTF8Encoding.GetChars]chars!=nullptr");
        Contract::Assert(byteCount >= 0, "[UTF8Encoding.GetChars]byteCount >=0");
        Contract::Assert(charCount >= 0, "[UTF8Encoding.GetChars]charCount >=0");
        Contract::Assert(bytes != nullptr, "[UTF8Encoding.GetChars]bytes!=nullptr");

        BYTE *pSrc = bytes;
        WCHAR *pTarget = chars;

        BYTE *pEnd = pSrc + byteCount;
        WCHAR *pAllocatedBufferEnd = pTarget + charCount;

        int ch = 0;

        DecoderFallbackBuffer *fallback = nullptr;

        for (;;)
        {
            // SLOWLOOP: does all range checks, handles all special cases, but it is slow

            if (pSrc >= pEnd) {
                break;
            }

            // read next byte. The JIT optimization seems to be getting confused when
            // compiling "ch = *pSrc++;", so rather use "ch = *pSrc; pSrc++;" instead
            int cha = *pSrc;

            if (ch == 0) {
                // no pending bits
                goto ReadChar;
            }

            pSrc++;

            // we are expecting to see trailing bytes like 10vvvvvv
            if ((cha & 0xC0) != 0x80) {
                // This can be a valid starting byte for another UTF8 byte sequence, so let's put
                // the current byte back, and try to see if this is a valid byte for another UTF8 byte sequence
                pSrc--;
                goto InvalidByteSequence;
            }

            // fold in the new byte
            ch = (ch << 6) | (cha & 0x3F);

            if ((ch & FinalByte) == 0) {
                // Not at last byte yet
                Contract::Assert((ch & (SupplimentarySeq | ThreeByteSeq)) != 0,
                    "[UTF8Encoding.GetChars]Invariant volation");

                if ((ch & SupplimentarySeq) != 0) {
                    // Its a 4-byte supplimentary sequence
                    if ((ch & (FinalByte >> 6)) != 0) {
                        // this is 3rd byte of 4 byte sequence - nothing to do
                        continue;
                    }

                    // 2nd byte of 4 bytes
                    // check for non-shortest form of surrogate and the valid surrogate
                    // range 0x000000 - 0x10FFFF at the same time
                    if (!InRange(ch & 0x1F0, 0x10, 0x100)) {
                        goto InvalidByteSequence;
                    }
                }
                else {
                    // Must be 2nd byte of a 3-byte sequence
                    // check for non-shortest form of 3 byte seq
                    if ((ch & (0x1F << 5)) == 0 ||                  // non-shortest form
                        (ch & (0xF800 >> 6)) == (0xD800 >> 6))     // illegal individually encoded surrogate
                    {
                        goto InvalidByteSequence;
                    }
                }
                continue;
            }

            // ready to punch

            // surrogate in shortest form?
            // Might be possible to get rid of this?  Already did non-shortest check for 4-byte sequence when reading 2nd byte?
            if ((ch & (SupplimentarySeq | 0x1F0000)) > SupplimentarySeq) {
                // let the range check for the second char throw the exception
                if (pTarget < pAllocatedBufferEnd) {
                    *pTarget = (WCHAR)(((ch >> 10) & 0x7FF) +
                        (SHORT)((CharUnicodeInfo::HIGH_SURROGATE_START - (0x10000 >> 10))));
                    pTarget++;

                    ch = (ch & 0x3FF) +
                        (int)(CharUnicodeInfo::LOW_SURROGATE_START);
                }
            }

            goto EncodeChar;

        InvalidByteSequence:
            // this code fragment should be close to the gotos referencing it
            // Have to do fallback for invalid bytes
            if (fallback == nullptr)
            {
                fallback = decoderFallback->CreateFallbackBuffer();
                fallback->InternalInitialize(bytes, pAllocatedBufferEnd);
            }

            // That'll back us up the appropriate # of bytes if we didn't get anywhere
            if (!FallbackInvalidByteSequence(&pSrc, ch, fallback, &pTarget))
            {
                // Ran out of buffer space
                // Need to throw an exception?
                Contract::Assert(pSrc >= bytes || pTarget == chars,
                    "[UTF8Encoding.GetChars]Expected to throw or remain in byte buffer after fallback");
                fallback->InternalReset();
                ThrowCharsOverflow(pTarget == chars);
                ch = 0;
                break;
            }
            Contract::Assert(pSrc >= bytes,
                "[UTF8Encoding.GetChars]Expected invalid byte sequence to have remained within the byte array");
            ch = 0;
            continue;

        ReadChar:
            ch = *pSrc;
            pSrc++;

        ProcessChar:
            if (ch > 0x7F) {
                // If its > 0x7F, its start of a new multi-byte sequence

                // bit 6 has to be non-zero
                if ((ch & 0x40) == 0) {
                    goto InvalidByteSequence;
                }

                // start a new long code
                if ((ch & 0x20) != 0) {
                    if ((ch & 0x10) != 0) {
                        // 4 byte encoding - supplimentary character (2 surrogates)

                        ch &= 0x0F;

                        // check that bit 4 is zero and the valid supplimentary character
                        // range 0x000000 - 0x10FFFF at the same time
                        if (ch > 0x04) {
                            ch |= 0xf0;
                            goto InvalidByteSequence;
                        }

                        ch |= (FinalByte >> 3 * 6) | (1 << 30) | (3 << (30 - 2 * 6)) |
                            (SupplimentarySeq) | (SupplimentarySeq >> 6) |
                            (SupplimentarySeq >> 2 * 6) | (SupplimentarySeq >> 3 * 6);
                    }
                    else {
                        // 3 byte encoding
                        ch = (ch & 0x0F) | ((FinalByte >> 2 * 6) | (1 << 30) |
                            (ThreeByteSeq) | (ThreeByteSeq >> 6) | (ThreeByteSeq >> 2 * 6));
                    }
                }
                else {
                    // 2 byte encoding

                    ch &= 0x1F;

                    // check for non-shortest form
                    if (ch <= 1) {
                        ch |= 0xc0;
                        goto InvalidByteSequence;
                    }

                    ch |= (FinalByte >> 6);
                }
                continue;
            }

        EncodeChar:
            // write the pending character
            if (pTarget >= pAllocatedBufferEnd)
            {
                // Fix chars so we make sure to throw if we didn't output anything
                ch &= 0x1fffff;
                if (ch > 0x7f)
                {
                    if (ch > 0x7ff)
                    {
                        if (ch >= CharUnicodeInfo::LOW_SURROGATE_START &&
                            ch <= CharUnicodeInfo::LOW_SURROGATE_END)
                        {
                            pSrc--;     // It was 4 bytes
                            pTarget--;  // 1 was stored already, but we can't remember 1/2, so back up
                        }
                        else if (ch > 0xffff)
                        {
                            pSrc--;     // It was 4 bytes, nothing was stored
                        }
                        pSrc--;         // It was at least 3 bytes
                    }
                    pSrc--;             // It was at least 2 bytes
                }
                pSrc--;

                // Throw that we don't have enough room (pSrc could be < chars if we had started to process
                // a 4 byte sequence alredy)
                Contract::Assert(pSrc >= bytes || pTarget == chars,
                    "[UTF8Encoding.GetChars]Expected pSrc to be within input buffer or throw due to no output]");
                ThrowCharsOverflow(pTarget == chars);

                // Don't store ch in decoder, we already backed up to its start
                ch = 0;

                // Didn't throw, just use this buffer size.
                break;
            }
            *pTarget = (WCHAR)ch;
            pTarget++;

#ifdef FASTLOOP
            int availableChars = PtrDiff(pAllocatedBufferEnd, pTarget);
            int availableBytes = PtrDiff(pEnd, pSrc);

            // don't fall into the fast decoding loop if we don't have enough bytes
            // Test for availableChars is done because pStop would be <= pTarget.
            if (availableBytes <= 13) {
                // we may need as many as 1 character per byte
                if (availableChars < availableBytes) {
                    // not enough output room.  no pending bits at this point
                    ch = 0;
                    continue;
                }

                // try to get over the remainder of the ascii characters fast though
                BYTE* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                while (pSrc < pLocalEnd) {
                    ch = *pSrc;
                    pSrc++;

                    if (ch > 0x7F)
                        goto ProcessChar;

                    *pTarget = (WCHAR)ch;
                    pTarget++;
                }
                // we are done
                ch = 0;
                break;
            }

            // we may need as many as 1 character per byte, so reduce the byte count if necessary.
            // If availableChars is too small, pStop will be before pTarget and we won't do fast loop.
            if (availableChars < availableBytes) {
                availableBytes = availableChars;
            }

            // To compute the upper bound, assume that all characters are ASCII characters at this point,
            //  the boundary will be decreased for every non-ASCII character we encounter
            // Also, we need 7 chars reserve for the unrolled ansi decoding loop and for decoding of multibyte sequences
            WCHAR *pStop = pTarget + availableBytes - 7;

            while (pTarget < pStop) {
                ch = *pSrc;
                pSrc++;

                if (ch > 0x7F) {
                    goto LongCode;
                }
                *pTarget = (WCHAR)ch;
                pTarget++;

                // get pSrc to be 2-byte aligned
                if ((((size_t)pSrc) & 0x1) != 0) {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F) {
                        goto LongCode;
                    }
                    *pTarget = (WCHAR)ch;
                    pTarget++;
                }

                // get pSrc to be 4-byte aligned
                if ((((size_t)pSrc) & 0x2) != 0) {
                    ch = *(USHORT*)pSrc;
                    if ((ch & 0x8080) != 0) {
                        goto LongCodeWithMask16;
                    }

                    // Unfortunately, this is endianess sensitive
#if BIGENDIAN
                    *pTarget = (WCHAR)((ch >> 8) & 0x7F);
                    pSrc += 2;
                    *(pTarget + 1) = (WCHAR)(ch & 0x7F);
                    pTarget += 2;
#else // BIGENDIAN
                    *pTarget = (WCHAR)(ch & 0x7F);
                    pSrc += 2;
                    *(pTarget + 1) = (WCHAR)((ch >> 8) & 0x7F);
                    pTarget += 2;
#endif // BIGENDIAN
                }

                // Run 8 characters at a time!
                while (pTarget < pStop) {
                    ch = *(int*)pSrc;
                    int chb = *(int*)(pSrc + 4);
                    if (((ch | chb) & (int)0x80808080) != 0) {
                        goto LongCodeWithMask32;
                    }

                    // Unfortunately, this is endianess sensitive
#if BIGENDIAN
                    *pTarget = (WCHAR)((ch >> 24) & 0x7F);
                    *(pTarget + 1) = (WCHAR)((ch >> 16) & 0x7F);
                    *(pTarget + 2) = (WCHAR)((ch >> 8) & 0x7F);
                    *(pTarget + 3) = (WCHAR)(ch & 0x7F);
                    pSrc += 8;
                    *(pTarget + 4) = (WCHAR)((chb >> 24) & 0x7F);
                    *(pTarget + 5) = (WCHAR)((chb >> 16) & 0x7F);
                    *(pTarget + 6) = (WCHAR)((chb >> 8) & 0x7F);
                    *(pTarget + 7) = (WCHAR)(chb & 0x7F);
                    pTarget += 8;
#else // BIGENDIAN
                    *pTarget = (WCHAR)(ch & 0x7F);
                    *(pTarget + 1) = (WCHAR)((ch >> 8) & 0x7F);
                    *(pTarget + 2) = (WCHAR)((ch >> 16) & 0x7F);
                    *(pTarget + 3) = (WCHAR)((ch >> 24) & 0x7F);
                    pSrc += 8;
                    *(pTarget + 4) = (WCHAR)(chb & 0x7F);
                    *(pTarget + 5) = (WCHAR)((chb >> 8) & 0x7F);
                    *(pTarget + 6) = (WCHAR)((chb >> 16) & 0x7F);
                    *(pTarget + 7) = (WCHAR)((chb >> 24) & 0x7F);
                    pTarget += 8;
#endif // BIGENDIAN
                }
                break;

#if BIGENDIAN
                LongCodeWithMask32 :
                    // be careful about the sign extension
                    ch = (int)(((uint)ch) >> 16);
                LongCodeWithMask16:
                    ch = (int)(((uint)ch) >> 8);
#else // BIGENDIAN
            LongCodeWithMask32:
            LongCodeWithMask16:
                ch &= 0xFF;
#endif // BIGENDIAN
                pSrc++;
                if (ch <= 0x7F) {
                    *pTarget = (WCHAR)ch;
                    pTarget++;
                    continue;
                }

            LongCode:
                int chc = *pSrc;
                pSrc++;

                if (
                    // bit 6 has to be zero
                    (ch & 0x40) == 0 ||
                    // we are expecting to see trailing bytes like 10vvvvvv
                    (chc & 0xC0) != 0x80)
                {
                    goto BadLongCode;
                }

                chc &= 0x3F;

                // start a new long code
                if ((ch & 0x20) != 0) {

                    // fold the first two bytes together
                    chc |= (ch & 0x0F) << 6;

                    if ((ch & 0x10) != 0) {
                        // 4 byte encoding - surrogate
                        ch = *pSrc;
                        if (
                            // check that bit 4 is zero, the non-shortest form of surrogate
                            // and the valid surrogate range 0x000000 - 0x10FFFF at the same time
                            !InRange(chc >> 4, 0x01, 0x10) ||
                            // we are expecting to see trailing bytes like 10vvvvvv
                            (ch & 0xC0) != 0x80)
                        {
                            goto BadLongCode;
                        }

                        chc = (chc << 6) | (ch & 0x3F);

                        ch = *(pSrc + 1);
                        // we are expecting to see trailing bytes like 10vvvvvv
                        if ((ch & 0xC0) != 0x80) {
                            goto BadLongCode;
                        }
                        pSrc += 2;

                        ch = (chc << 6) | (ch & 0x3F);

                        *pTarget = (WCHAR)(((ch >> 10) & 0x7FF) +
                            (SHORT)(CharUnicodeInfo::HIGH_SURROGATE_START - (0x10000 >> 10)));
                        pTarget++;

                        ch = (ch & 0x3FF) +
                            (SHORT)(CharUnicodeInfo::LOW_SURROGATE_START);

                        // extra byte, we're already planning 2 chars for 2 of these bytes,
                        // but the big loop is testing the target against pStop, so we need
                        // to subtract 2 more or we risk overrunning the input.  Subtract
                        // one here and one below.
                        pStop--;
                    }
                    else {
                        // 3 byte encoding
                        ch = *pSrc;
                        if (
                            // check for non-shortest form of 3 byte seq
                            (chc & (0x1F << 5)) == 0 ||
                            // Can't have surrogates here.
                            (chc & (0xF800 >> 6)) == (0xD800 >> 6) ||
                            // we are expecting to see trailing bytes like 10vvvvvv
                            (ch & 0xC0) != 0x80)
                        {
                            goto BadLongCode;
                        }
                        pSrc++;

                        ch = (chc << 6) | (ch & 0x3F);

                        // extra byte, we're only expecting 1 char for each of these 3 bytes,
                        // but the loop is testing the target (not source) against pStop, so
                        // we need to subtract 2 more or we risk overrunning the input.
                        // Subtract 1 here and one more below
                        pStop--;
                    }
                }
                else {
                    // 2 byte encoding

                    ch &= 0x1F;

                    // check for non-shortest form
                    if (ch <= 1) {
                        goto BadLongCode;
                    }
                    ch = (ch << 6) | chc;
                }

                *pTarget = (WCHAR)ch;
                pTarget++;

                // extra byte, we're only expecting 1 char for each of these 2 bytes,
                // but the loop is testing the target (not source) against pStop.
                // subtract an extra count from pStop so that we don't overrun the input.
                pStop--;
            }
#endif // FASTLOOP

            Contract::Assert(pTarget <= pAllocatedBufferEnd, "[UTF8Encoding.GetChars]pTarget <= pAllocatedBufferEnd");

            // no pending bits at this point
            ch = 0;
            continue;

        BadLongCode:
            pSrc -= 2;
            ch = 0;
            continue;
        }

        if (ch != 0)
        {
            // Have to do fallback for invalid bytes
            if (fallback == nullptr)
            {
                fallback = decoderFallback->CreateFallbackBuffer();
                fallback->InternalInitialize(bytes, pAllocatedBufferEnd);
            }

            // This'll back us up the appropriate # of bytes if we didn't get anywhere
            if (!FallbackInvalidByteSequence(pSrc, ch, fallback))
            {
                Contract::Assert(pSrc >= bytes || pTarget == chars,
                    "[UTF8Encoding.GetChars]Expected to throw or remain in byte buffer while flushing");

                // Ran out of buffer space
                // Need to throw an exception?
                fallback->InternalReset();
                ThrowCharsOverflow(pTarget == chars);
            }
            Contract::Assert(pSrc >= bytes,
                "[UTF8Encoding.GetChars]Expected flushing invalid byte sequence to have remained within the byte array");
            ch = 0;
        }

        // Shouldn't have anything in fallback buffer for GetChars
        // (don't have to check m_throwOnOverflow for chars)
        Contract::Assert(fallback == nullptr || fallback->GetRemaining() == 0,
            "[UTF8Encoding.GetChars]Expected empty fallback buffer at end");

        InternalDelete(fallback);

        return PtrDiff(pTarget, chars);
    }

    int GetBytes(WCHAR* chars, int charCount, BYTE* bytes, int byteCount)
    {
        Contract::Assert(chars != nullptr, "[UTF8Encoding.GetBytes]chars!=nullptr");
        Contract::Assert(byteCount >= 0, "[UTF8Encoding.GetBytes]byteCount >=0");
        Contract::Assert(charCount >= 0, "[UTF8Encoding.GetBytes]charCount >=0");
        Contract::Assert(bytes != nullptr, "[UTF8Encoding.GetBytes]bytes!=nullptr");

        // For fallback we may need a fallback buffer.
        // We wait to initialize it though in case we don't have any broken input unicode
        EncoderFallbackBuffer* fallbackBuffer = nullptr;
        WCHAR *pSrc = chars;
        BYTE *pTarget = bytes;

        WCHAR *pEnd = pSrc + charCount;
        BYTE *pAllocatedBufferEnd = pTarget + byteCount;

        int ch = 0;

        // assume that JIT will enregister pSrc, pTarget and ch

        for (;;) {
            // SLOWLOOP: does all range checks, handles all special cases, but it is slow

            if (pSrc >= pEnd) {

                if (ch == 0) {
                    // Check if there's anything left to get out of the fallback buffer
                    ch = fallbackBuffer != nullptr ? fallbackBuffer->InternalGetNextChar() : 0;
                    if (ch > 0) {
                        goto ProcessChar;
                    }
                }
                else {
                    // Case of leftover surrogates in the fallback buffer
                    if (fallbackBuffer != nullptr && fallbackBuffer->bFallingBack) {
                        Contract::Assert(ch >= 0xD800 && ch <= 0xDBFF,
                            "[UTF8Encoding.GetBytes]expected high surrogate"); //, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));

                        int cha = ch;

                        ch = fallbackBuffer->InternalGetNextChar();

                        if (InRange(ch, CharUnicodeInfo::LOW_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END)) {
                            ch = ch + (cha << 10) + (0x10000 - CharUnicodeInfo::LOW_SURROGATE_START - (CharUnicodeInfo::HIGH_SURROGATE_START << 10));
                            goto EncodeChar;
                        }
                        else if (ch > 0){
                            goto ProcessChar;
                        }
                        else {
                            break;
                        }
                    }
                }

                // attempt to encode the partial surrogate (will fail or ignore)
                if (ch > 0)
                    goto EncodeChar;

                // We're done
                break;
            }

            if (ch > 0) {
                // We have a high surrogate left over from a previous loop.
                Contract::Assert(ch >= 0xD800 && ch <= 0xDBFF,
                    "[UTF8Encoding.GetBytes]expected high surrogate");//, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));

                // use separate helper variables for local contexts so that the jit optimizations
                // won't get confused about the variable lifetimes
                int cha = *pSrc;

                // In previous byte, we encountered a high surrogate, so we are expecting a low surrogate here.
                // if (IsLowSurrogate(cha)) {
                if (InRange(cha, CharUnicodeInfo::LOW_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END)) {
                    ch = cha + (ch << 10) +
                        (0x10000
                        - CharUnicodeInfo::LOW_SURROGATE_START
                        - (CharUnicodeInfo::HIGH_SURROGATE_START << 10));

                    pSrc++;
                }
                // else ch is still high surrogate and encoding will fail

                // attempt to encode the surrogate or partial surrogate
                goto EncodeChar;
            }

            // If we've used a fallback, then we have to check for it
            if (fallbackBuffer != nullptr)
            {
                ch = fallbackBuffer->InternalGetNextChar();
                if (ch > 0) goto ProcessChar;
            }

            // read next char. The JIT optimization seems to be getting confused when
            // compiling "ch = *pSrc++;", so rather use "ch = *pSrc; pSrc++;" instead
            ch = *pSrc;
            pSrc++;

        ProcessChar:
            if (InRange(ch, CharUnicodeInfo::HIGH_SURROGATE_START, CharUnicodeInfo::HIGH_SURROGATE_END)) {
                continue;
            }
            // either good char or partial surrogate

        EncodeChar:
            // throw exception on partial surrogate if necessary
            if (InRange(ch, CharUnicodeInfo::HIGH_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END))
            {
                // Lone surrogates aren't allowed, we have to do fallback for them
                // Have to make a fallback buffer if we don't have one
                if (fallbackBuffer == nullptr)
                {
                    // wait on fallbacks if we can
                    // For fallback we may need a fallback buffer
                    fallbackBuffer = encoderFallback->CreateFallbackBuffer();

                    // Set our internal fallback interesting things.
                    fallbackBuffer->InternalInitialize(chars, pEnd, true);
                }

                // Do our fallback.  Actually we already know its a mixed up surrogate,
                // so the ref pSrc isn't gonna do anything.
                fallbackBuffer->InternalFallback((WCHAR)ch, &pSrc);

                // Ignore it if we don't throw
                ch = 0;
                continue;
            }

            // Count bytes needed
            int bytesNeeded = 1;
            if (ch > 0x7F) {
                if (ch > 0x7FF) {
                    if (ch > 0xFFFF) {
                        bytesNeeded++;  // 4 bytes (surrogate pair)
                    }
                    bytesNeeded++;      // 3 bytes (800-FFFF)
                }
                bytesNeeded++;          // 2 bytes (80-7FF)
            }

            if (pTarget > pAllocatedBufferEnd - bytesNeeded) {
                // Left over surrogate from last time will cause pSrc == chars, so we'll throw
                if (fallbackBuffer != nullptr && fallbackBuffer->bFallingBack)
                {
                    fallbackBuffer->MovePrevious();              // Didn't use this fallback char
                    if (ch > 0xFFFF)
                        fallbackBuffer->MovePrevious();          // Was surrogate, didn't use 2nd part either
                }
                else
                {
                    pSrc--;                                     // Didn't use this char
                    if (ch > 0xFFFF)
                        pSrc--;                                 // Was surrogate, didn't use 2nd part either
                }
                Contract::Assert(pSrc >= chars || pTarget == bytes,
                    "[UTF8Encoding.GetBytes]Expected pSrc to be within buffer or to throw with insufficient room.");
                ThrowBytesOverflow(pTarget == bytes);  // Throw if we must
                ch = 0;                                         // Nothing left over (we backed up to start of pair if supplimentary)
                break;
            }

            if (ch <= 0x7F) {
                *pTarget = (BYTE)ch;
            }
            else {
                // use separate helper variables for local contexts so that the jit optimizations
                // won't get confused about the variable lifetimes
                int chb;
                if (ch <= 0x7FF) {
                    // 2 BYTE encoding
                    chb = (BYTE)(0xC0 | (ch >> 6));
                }
                else
                {
                    if (ch <= 0xFFFF) {
                        chb = (BYTE)(0xE0 | (ch >> 12));
                    }
                    else
                    {
                        *pTarget = (BYTE)(0xF0 | (ch >> 18));
                        pTarget++;

                        chb = 0x80 | ((ch >> 12) & 0x3F);
                    }
                    *pTarget = (BYTE)chb;
                    pTarget++;

                    chb = 0x80 | ((ch >> 6) & 0x3F);
                }
                *pTarget = (BYTE)chb;
                pTarget++;

                *pTarget = (BYTE)0x80 | (ch & 0x3F);
            }
            pTarget++;


#ifdef FASTLOOP
            // If still have fallback don't do fast loop
            if (fallbackBuffer != nullptr && (ch = fallbackBuffer->InternalGetNextChar()) != 0)
                goto ProcessChar;

            int availableChars = PtrDiff(pEnd, pSrc);
            int availableBytes = PtrDiff(pAllocatedBufferEnd, pTarget);

            // don't fall into the fast decoding loop if we don't have enough characters
            // Note that if we don't have enough bytes, pStop will prevent us from entering the fast loop.
            if (availableChars <= 13) {
                // we are hoping for 1 BYTE per char
                if (availableBytes < availableChars) {
                    // not enough output room.  no pending bits at this point
                    ch = 0;
                    continue;
                }

                // try to get over the remainder of the ascii characters fast though
                WCHAR* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                while (pSrc < pLocalEnd) {
                    ch = *pSrc;
                    pSrc++;

                    // Not ASCII, need more than 1 BYTE per char
                    if (ch > 0x7F)
                        goto ProcessChar;

                    *pTarget = (BYTE)ch;
                    pTarget++;
                }
                // we are done, let ch be 0 to clear encoder
                ch = 0;
                break;
            }

            // we need at least 1 BYTE per character, but Convert might allow us to convert
            // only part of the input, so try as much as we can.  Reduce charCount if necessary
            if (availableBytes < availableChars)
            {
                availableChars = availableBytes;
            }

            // FASTLOOP:
            // - optimistic range checks
            // - fallbacks to the slow loop for all special cases, exception throwing, etc.

            // To compute the upper bound, assume that all characters are ASCII characters at this point,
            //  the boundary will be decreased for every non-ASCII character we encounter
            // Also, we need 5 chars reserve for the unrolled ansi decoding loop and for decoding of surrogates
            // If there aren't enough bytes for the output, then pStop will be <= pSrc and will bypass the loop.
            WCHAR *pStop = pSrc + availableChars - 5;

            while (pSrc < pStop) {
                ch = *pSrc;
                pSrc++;

                if (ch > 0x7F) {
                    goto LongCode;
                }
                *pTarget = (BYTE)ch;
                pTarget++;

                // get pSrc aligned
                if (((size_t)pSrc & 0x2) != 0) {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F) {
                        goto LongCode;
                    }
                    *pTarget = (BYTE)ch;
                    pTarget++;
                }

                // Run 4 characters at a time!
                while (pSrc < pStop) {
                    ch = *(int*)pSrc;
                    int chc = *(int*)(pSrc + 2);
                    if (((ch | chc) & (int)0xFF80FF80) != 0) {
                        goto LongCodeWithMask;
                    }

                    // Unfortunately, this is endianess sensitive
#if BIGENDIAN
                    *pTarget = (BYTE)(ch >> 16);
                    *(pTarget + 1) = (BYTE)ch;
                    pSrc += 4;
                    *(pTarget + 2) = (BYTE)(chc >> 16);
                    *(pTarget + 3) = (BYTE)chc;
                    pTarget += 4;
#else // BIGENDIAN
                    *pTarget = (BYTE)ch;
                    *(pTarget + 1) = (BYTE)(ch >> 16);
                    pSrc += 4;
                    *(pTarget + 2) = (BYTE)chc;
                    *(pTarget + 3) = (BYTE)(chc >> 16);
                    pTarget += 4;
#endif // BIGENDIAN
                }
                continue;

            LongCodeWithMask:
#if BIGENDIAN
                // be careful about the sign extension
                ch = (int)(((uint)ch) >> 16);
#else // BIGENDIAN
                ch = (WCHAR)ch;
#endif // BIGENDIAN
                pSrc++;

                if (ch > 0x7F) {
                    goto LongCode;
                }
                *pTarget = (BYTE)ch;
                pTarget++;
                continue;

            LongCode:
                // use separate helper variables for slow and fast loop so that the jit optimizations
                // won't get confused about the variable lifetimes
                int chd;
                if (ch <= 0x7FF) {
                    // 2 BYTE encoding
                    chd = 0xC0 | (ch >> 6);
                }
                else {
                    if (!InRange(ch, CharUnicodeInfo::HIGH_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END)) {
                        // 3 BYTE encoding
                        chd = 0xE0 | (ch >> 12);
                    }
                    else
                    {
                        // 4 BYTE encoding - high surrogate + low surrogate
                        if (ch > CharUnicodeInfo::HIGH_SURROGATE_END) {
                            // low without high -> bad, try again in slow loop
                            pSrc -= 1;
                            break;
                        }

                        chd = *pSrc;
                        pSrc++;

                        // if (!IsLowSurrogate(chd)) {
                        if (!InRange(chd, CharUnicodeInfo::LOW_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END)) {
                            // high not followed by low -> bad, try again in slow loop
                            pSrc -= 2;
                            break;
                        }

                        ch = chd + (ch << 10) +
                            (0x10000
                            - CharUnicodeInfo::LOW_SURROGATE_START
                            - (CharUnicodeInfo::HIGH_SURROGATE_START << 10));

                        *pTarget = (BYTE)(0xF0 | (ch >> 18));
                        // pStop - this BYTE is compensated by the second surrogate character
                        // 2 input chars require 4 output bytes.  2 have been anticipated already
                        // and 2 more will be accounted for by the 2 pStop-- calls below.
                        pTarget++;

                        chd = 0x80 | ((ch >> 12) & 0x3F);
                    }
                    *pTarget = (BYTE)chd;
                    pStop--;                    // 3 BYTE sequence for 1 char, so need pStop-- and the one below too.
                    pTarget++;

                    chd = 0x80 | ((ch >> 6) & 0x3F);
                }
                *pTarget = (BYTE)chd;
                pStop--;                        // 2 BYTE sequence for 1 char so need pStop--.
                pTarget++;

                *pTarget = (BYTE)(0x80 | (ch & 0x3F));
                // pStop - this BYTE is already included
                pTarget++;
            }

            Contract::Assert(pTarget <= pAllocatedBufferEnd, "[UTF8Encoding.GetBytes]pTarget <= pAllocatedBufferEnd");

#endif // FASTLOOP

            // no pending char at this point
            ch = 0;
        }

        InternalDelete(fallbackBuffer);

        return (int)(pTarget - bytes);
    }

    int GetByteCount(WCHAR *chars, int count)
    {
        // For fallback we may need a fallback buffer.
        // We wait to initialize it though in case we don't have any broken input unicode
        EncoderFallbackBuffer* fallbackBuffer = nullptr;
        WCHAR *pSrc = chars;
        WCHAR *pEnd = pSrc + count;

        // Start by assuming we have as many as count
        int byteCount = count;

        int ch = 0;

        for (;;) {
            // SLOWLOOP: does all range checks, handles all special cases, but it is slow
            if (pSrc >= pEnd) {

                if (ch == 0) {
                    // Unroll any fallback that happens at the end
                    ch = fallbackBuffer != nullptr ? fallbackBuffer->InternalGetNextChar() : 0;
                    if (ch > 0) {
                        byteCount++;
                        goto ProcessChar;
                    }
                }
                else {
                    // Case of surrogates in the fallback.
                    if (fallbackBuffer != nullptr && fallbackBuffer->bFallingBack) {
                        Contract::Assert(ch >= 0xD800 && ch <= 0xDBFF,
                            "[UTF8Encoding.GetBytes]expected high surrogate");// , not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));

                        ch = fallbackBuffer->InternalGetNextChar();
                        byteCount++;

                        if (InRange(ch, CharUnicodeInfo::LOW_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END)) {
                            ch = 0xfffd;
                            byteCount++;
                            goto EncodeChar;
                        }
                        else if (ch > 0){
                            goto ProcessChar;
                        }
                        else {
                            byteCount--; // ignore last one.
                            break;
                        }
                    }
                }

                if (ch <= 0) {
                    break;
                }

                // attempt to encode the partial surrogate (will fallback or ignore it), it'll also subtract 1.
                byteCount++;
                goto EncodeChar;
            }

            if (ch > 0) {
                Contract::Assert(ch >= 0xD800 && ch <= 0xDBFF,
                    "[UTF8Encoding.GetBytes]expected high surrogate"); // , not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture));

                // use separate helper variables for local contexts so that the jit optimizations
                // won't get confused about the variable lifetimes
                int cha = *pSrc;

                // count the pending surrogate
                byteCount++;

                // In previous byte, we encountered a high surrogate, so we are expecting a low surrogate here.
                // if (IsLowSurrogate(cha)) {
                if (InRange(cha, CharUnicodeInfo::LOW_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END)) {
                    // Don't need a real # because we're just counting, anything > 0x7ff ('cept surrogate) will do.
                    ch = 0xfffd;
                    //                        ch = cha + (ch << 10) +
                    //                            (0x10000
                    //                            - CharUnicodeInfo::LOW_SURROGATE_START
                    //                            - (CharUnicodeInfo::HIGH_SURROGATE_START << 10) );

                    // Use this next char
                    pSrc++;
                }
                // else ch is still high surrogate and encoding will fail (so don't add count)

                // attempt to encode the surrogate or partial surrogate
                goto EncodeChar;
            }

            // If we've used a fallback, then we have to check for it
            if (fallbackBuffer != nullptr)
            {
                ch = fallbackBuffer->InternalGetNextChar();
                if (ch > 0)
                {
                    // We have an extra byte we weren't expecting.
                    byteCount++;
                    goto ProcessChar;
                }
            }

            // read next char. The JIT optimization seems to be getting confused when
            // compiling "ch = *pSrc++;", so rather use "ch = *pSrc; pSrc++;" instead
            ch = *pSrc;
            pSrc++;

        ProcessChar:
            if (InRange(ch, CharUnicodeInfo::HIGH_SURROGATE_START, CharUnicodeInfo::HIGH_SURROGATE_END)) {
                // we will count this surrogate next time around
                byteCount--;
                continue;
            }
            // either good char or partial surrogate

        EncodeChar:
            // throw exception on partial surrogate if necessary
            if (InRange(ch, CharUnicodeInfo::HIGH_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END))
            {
                // Lone surrogates aren't allowed
                // Have to make a fallback buffer if we don't have one
                if (fallbackBuffer == nullptr)
                {
                    // wait on fallbacks if we can
                    // For fallback we may need a fallback buffer
                    fallbackBuffer = encoderFallback->CreateFallbackBuffer();

                    // Set our internal fallback interesting things.
                    fallbackBuffer->InternalInitialize(chars, chars + count, false);
                }

                // Do our fallback.  Actually we already know its a mixed up surrogate,
                // so the ref pSrc isn't gonna do anything.
                fallbackBuffer->InternalFallback((WCHAR)ch, &pSrc);

                // Ignore it if we don't throw (we had preallocated this ch)
                byteCount--;
                ch = 0;
                continue;
            }

            // Count them
            if (ch > 0x7F) {
                if (ch > 0x7FF) {
                    // the extra surrogate byte was compensated by the second surrogate character
                    // (2 surrogates make 4 bytes.  We've already counted 2 bytes, 1 per char)
                    byteCount++;
                }
                byteCount++;
            }

#if WIN64
            // check for overflow
            if (byteCount < 0) {
                break;
            }
#endif

#ifdef FASTLOOP
            // If still have fallback don't do fast loop
            if (fallbackBuffer != nullptr && (ch = fallbackBuffer->InternalGetNextChar()) != 0)
            {
                // We're reserving 1 byte for each char by default
                byteCount++;
                goto ProcessChar;
            }

            int availableChars = PtrDiff(pEnd, pSrc);

            // don't fall into the fast decoding loop if we don't have enough characters
            if (availableChars <= 13) {
                // try to get over the remainder of the ascii characters fast though
                WCHAR* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                while (pSrc < pLocalEnd) {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F)
                        goto ProcessChar;
                }

                // we are done
                break;
            }

#if WIN64
            // make sure that we won't get a silent overflow inside the fast loop
            // (Fall out to slow loop if we have this many characters)
            availableChars &= 0x0FFFFFFF;
#endif

            // To compute the upper bound, assume that all characters are ASCII characters at this point,
            //  the boundary will be decreased for every non-ASCII character we encounter
            // Also, we need 3 + 4 chars reserve for the unrolled ansi decoding loop and for decoding of surrogates
            WCHAR *pStop = pSrc + availableChars - (3 + 4);

            while (pSrc < pStop) {
                ch = *pSrc;
                pSrc++;

                if (ch > 0x7F)                                                  // Not ASCII
                {
                    if (ch > 0x7FF)                                             // Not 2 Byte
                    {
                        if ((ch & 0xF800) == 0xD800)                            // See if its a Surrogate
                            goto LongCode;
                        byteCount++;
                    }
                    byteCount++;
                }

                // get pSrc aligned
                if (((size_t)pSrc & 0x2) != 0) {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F)                                              // Not ASCII
                    {
                        if (ch > 0x7FF)                                         // Not 2 Byte
                        {
                            if ((ch & 0xF800) == 0xD800)                        // See if its a Surrogate
                                goto LongCode;
                            byteCount++;
                        }
                        byteCount++;
                    }
                }

                // Run 2 * 4 characters at a time!
                while (pSrc < pStop) {
                    ch = *(int*)pSrc;
                    int chc = *(int*)(pSrc + 2);
                    if (((ch | chc) & (int)0xFF80FF80) != 0)         // See if not ASCII
                    {
                        if (((ch | chc) & (int)0xF800F800) != 0)     // See if not 2 Byte
                        {
                            goto LongCodeWithMask;
                        }


                        if ((ch & (int)0xFF800000) != 0)             // Actually 0x07800780 is all we care about (4 bits)
                            byteCount++;
                        if ((ch & (int)0xFF80) != 0)
                            byteCount++;
                        if ((chc & (int)0xFF800000) != 0)
                            byteCount++;
                        if ((chc & (int)0xFF80) != 0)
                            byteCount++;
                    }
                    pSrc += 4;

                    ch = *(int*)pSrc;
                    chc = *(int*)(pSrc + 2);
                    if (((ch | chc) & (int)0xFF80FF80) != 0)         // See if not ASCII
                    {
                        if (((ch | chc) & (int)0xF800F800) != 0)     // See if not 2 Byte
                        {
                            goto LongCodeWithMask;
                        }

                        if ((ch & (int)0xFF800000) != 0)
                            byteCount++;
                        if ((ch & (int)0xFF80) != 0)
                            byteCount++;
                        if ((chc & (int)0xFF800000) != 0)
                            byteCount++;
                        if ((chc & (int)0xFF80) != 0)
                            byteCount++;
                    }
                    pSrc += 4;
                }
                break;

            LongCodeWithMask:
#if BIGENDIAN
                // be careful about the sign extension
                ch = (int)(((uint)ch) >> 16);
#else // BIGENDIAN
                ch = (WCHAR)ch;
#endif // BIGENDIAN
                pSrc++;

                if (ch <= 0x7F) {
                    continue;
                }

            LongCode:
                // use separate helper variables for slow and fast loop so that the jit optimizations
                // won't get confused about the variable lifetimes
                if (ch > 0x7FF) {
                    if (InRange(ch, CharUnicodeInfo::HIGH_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END)) {
                        // 4 byte encoding - high surrogate + low surrogate

                        int chd = *pSrc;
                        if (
                            ch > CharUnicodeInfo::HIGH_SURROGATE_END ||
                            !InRange(chd, CharUnicodeInfo::LOW_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END))
                        {
                            // Back up and drop out to slow loop to figure out error
                            pSrc--;
                            break;
                        }
                        pSrc++;

                        // byteCount - this byte is compensated by the second surrogate character
                    }
                    byteCount++;
                }
                byteCount++;

                // byteCount - the last byte is already included
            }
#endif // FASTLOOP

            // no pending char at this point
            ch = 0;
        }

#if WIN64
        // check for overflow
        if (byteCount < 0) {
            throw ArgumentException("Conversion buffer overflow.");
        }
#endif

        Contract::Assert(fallbackBuffer == nullptr || fallbackBuffer->GetRemaining() == 0,
            "[UTF8Encoding.GetByteCount]Expected Empty fallback buffer");

        InternalDelete(fallbackBuffer);

        return byteCount;
    }

};


////////////////////////////////////////////////////////////////////////////
//
//  UTF8ToUnicode
//
//  Maps a UTF-8 character string to its wide character string counterpart.
//
////////////////////////////////////////////////////////////////////////////

int UTF8ToUnicode(
    LPCSTR lpSrcStr,
    int cchSrc,
    LPWSTR lpDestStr,
    int cchDest,
    DWORD dwFlags
    )
{
    int ret;
    UTF8Encoding enc(dwFlags & MB_ERR_INVALID_CHARS);
    try {
        ret = enc.GetCharCount((BYTE*)lpSrcStr, cchSrc);
        if (cchDest){
            if (ret > cchDest){
                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                ret = 0;
            }
            enc.GetChars((BYTE*)lpSrcStr, cchSrc, (WCHAR*)lpDestStr, ret);
        }
    }
    catch (const InsufficientBufferException& e){
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return 0;
    }
    catch (const DecoderFallbackException& e){
        SetLastError(ERROR_NO_UNICODE_TRANSLATION);
        return 0;
    }
    catch (const ArgumentException& e){
        SetLastError(ERROR_INVALID_PARAMETER);
        return 0;
    }
    return ret;
}

////////////////////////////////////////////////////////////////////////////
//
//  UnicodeToUTF8
//
//  Maps a Unicode character string to its UTF-8 string counterpart.
//
////////////////////////////////////////////////////////////////////////////

int UnicodeToUTF8(
    LPCWSTR lpSrcStr,
    int cchSrc,
    LPSTR lpDestStr,
    int cchDest)
{
    int ret;
    UTF8Encoding enc(false);
    try{
        ret = enc.GetByteCount((WCHAR*)lpSrcStr, cchSrc);
        if (cchDest){
            if (ret > cchDest){
                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                ret = 0;
            }
            enc.GetBytes((WCHAR*)lpSrcStr, cchSrc, (BYTE*)lpDestStr, ret);
        }
    }
    catch (const InsufficientBufferException& e){
        SetLastError(ERROR_INSUFFICIENT_BUFFER);
        return 0;
    }
    catch (const EncoderFallbackException& e){
        SetLastError(ERROR_NO_UNICODE_TRANSLATION);
        return 0;
    }
    catch (const ArgumentException& e){
        SetLastError(ERROR_INVALID_PARAMETER);
        return 0;
    }
    return ret;
}
