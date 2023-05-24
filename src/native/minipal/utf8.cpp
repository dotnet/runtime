// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    unicode/utf8.c

Abstract:
    Functions to encode and decode UTF-8 strings. This is a port of the C# version from Utf8Encoding.cs.

Revision History:

--*/

#include <minipal/utf8.h>

#include <errno.h>
#include <limits.h>
#include <string.h>
#include <new>

#define FASTLOOP

#ifdef TARGET_WINDOWS
#define W(str) L ## str
#else
#define W(str) u##str
#endif

struct CharUnicodeInfo
{
    static const char16_t HIGH_SURROGATE_START = 0xd800;
    static const char16_t HIGH_SURROGATE_END = 0xdbff;
    static const char16_t LOW_SURROGATE_START = 0xdc00;
    static const char16_t LOW_SURROGATE_END = 0xdfff;
};

struct Char
{
    // Test if the wide character is a high surrogate
    static bool IsHighSurrogate(const char16_t c)
    {
        return (c & 0xFC00) == CharUnicodeInfo::HIGH_SURROGATE_START;
    }

    // Test if the wide character is a low surrogate
    static bool IsLowSurrogate(const char16_t c)
    {
        return (c & 0xFC00) == CharUnicodeInfo::LOW_SURROGATE_START;
    }

    // Test if the wide character is a surrogate half
    static bool IsSurrogate(const char16_t c)
    {
        return (c & 0xF800) == CharUnicodeInfo::HIGH_SURROGATE_START;
    }

    // Test if the wide character is a high surrogate
    static bool IsHighSurrogate(const char16_t* s, int index)
    {
        return IsHighSurrogate(s[index]);
    }

    // Test if the wide character is a low surrogate
    static bool IsLowSurrogate(const char16_t* s, int index)
    {
        return IsLowSurrogate(s[index]);
    }

    // Test if the wide character is a surrogate half
    static bool IsSurrogate(const char16_t* s, int index)
    {
        return IsSurrogate(s[index]);
    }
};

size_t wcslen(const char16_t* str)
{
    size_t nChar = 0;
    while (*str++) nChar++;
    return nChar;
}

int wcscpy_s(char16_t *_Dst, size_t _SizeInWords, const char16_t *_Src)
{

    char16_t* p = _Dst;
    size_t available = _SizeInWords;

    if (!_Src || !_Dst || _SizeInWords == 0) return EINVAL;

    while ((*p++ = *_Src++) != 0 && --available > 0);

    if (available == 0)
    {
        _Dst = 0;
        return ERANGE;
    }

#ifdef DEBUG
    size_t offset = _SizeInWords - available + 1;
    if (offset < _SizeInWords)
    {
        memset((_Dst) + (offset), 0xFD, ((_SizeInWords) - (offset)) * sizeof(*(_Dst)));
    }
#endif

    return 0;
}

int wcscat_s(char16_t *_Dst, size_t _SizeInWords, const char16_t *_Src)
{
    char16_t* p = _Dst;
    size_t available = _SizeInWords;

    if (!_Src || !_Dst || _SizeInWords == 0) return EINVAL;

    while (available > 0 && *p != 0)
    {
        p++;
        available--;
    }

    if (available == 0)
    {
        _Dst = 0;
        return EINVAL;
    }

    while ((*p++ = *_Src++) != 0 && --available > 0)
    {
    }

    if (available == 0)
    {
        _Dst = 0;
        return ERANGE;
    }

#ifdef DEBUG
    size_t offset = _SizeInWords - available + 1;
    if (offset < _SizeInWords)
    {
        memset((_Dst) + (offset), 0xFD, ((_SizeInWords) - (offset)) * sizeof(*(_Dst)));
    }
#endif
    return 0;
}

#define ContractAssert(cond)             \
    if (!(cond))                         \
    {                                    \
        errno = ERROR_INVALID_PARAMETER; \
        return 0;                        \
    }

#define ContractAssertVoid(cond)         \
    if (!(cond))                         \
    {                                    \
        errno = ERROR_INVALID_PARAMETER; \
        return;                          \
    }

#define ContractAssertFreeFallback(cond) \
    if (!(cond))                         \
    {                                    \
        errno = ERROR_INVALID_PARAMETER; \
        if (fallback) free(fallback);    \
        return 0;                        \
    }

#define RETURN_ON_ERROR               \
    if (errno)                        \
    {                                 \
        if (fallback) free(fallback); \
        return 0;                     \
    }

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
    char16_t strDefault[2];
    int strDefaultLength;

public:
    // Construction.  Default replacement fallback uses no best fit and ? replacement string
    DecoderReplacementFallback() : DecoderReplacementFallback(W("?"))
    {
    }

    DecoderReplacementFallback(const char16_t* replacement)
    {
        // Must not be null
        ContractAssertVoid(replacement != nullptr)

        // Make sure it doesn't have bad surrogate pairs
        bool bFoundHigh = false;
        int replacementLength = wcslen((const char16_t *)replacement);
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
        ContractAssertVoid(!bFoundHigh)

        wcscpy_s(strDefault, ARRAY_SIZE(strDefault), replacement);
        strDefaultLength = replacementLength;
    }

    char16_t* GetDefaultString()
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
    // Most implementations will probably need an implementation-specific constructor

    // internal methods that cannot be overridden that let us do our fallback thing
    // These wrap the internal methods so that we can check for people doing stuff that's incorrect

public:
    virtual bool Fallback(unsigned char bytesUnknown[], int index, int size) = 0;

    // Get next character
    virtual char16_t GetNextChar() = 0;

    //Back up a character
    virtual bool MovePrevious() = 0;

    // How many chars left in this fallback?
    virtual int GetRemaining() = 0;

    // Clear the buffer
    virtual void Reset()
    {
        while (GetNextChar() != (char16_t)0);
    }

    // Internal items to help us figure out what we're doing as far as error messages, etc.
    // These help us with our performance and messages internally
protected:
    unsigned char*           byteStart;
    char16_t*          charEnd;

    // Internal reset
    void InternalReset()
    {
        byteStart = nullptr;
        Reset();
    }

    // Set the above values
    // This can't be part of the constructor because EncoderFallbacks would have to know how to implement these.
    void InternalInitialize(unsigned char* byteStart, char16_t* charEnd)
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
    virtual bool InternalFallback(unsigned char bytes[], unsigned char* pBytes, char16_t** chars, int size)
    {

        ContractAssert(byteStart != nullptr)

        bool fallbackResult = this->Fallback(bytes, (int)(pBytes - byteStart - size), size);
        if (errno) return false;

        // See if there's a fallback character and we have an output buffer then copy our string.
        if (fallbackResult)
        {
            // Copy the chars to our output
            char16_t ch;
            char16_t* charTemp = *chars;
            bool bHighSurrogate = false;
            while ((ch = GetNextChar()) != 0)
            {
                // Make sure no mixed up surrogates
                if (Char::IsSurrogate(ch))
                {
                    if (Char::IsHighSurrogate(ch))
                    {
                        // High Surrogate
                        ContractAssert(!bHighSurrogate)
                        bHighSurrogate = true;
                    }
                    else
                    {
                        // Low surrogate
                        ContractAssert(bHighSurrogate)
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
            ContractAssert(!bHighSurrogate)

            // Now we aren't going to be false, so its OK to update chars
            *chars = charTemp;
        }

        return true;
    }

    // This version just counts the fallback and doesn't actually copy anything.
    virtual int InternalFallback(unsigned char bytes[], unsigned char* pBytes, int size)
        // Right now this has both bytes[] and unsigned char* bytes, since we might have extra bytes, hence the
        // array, and we might need the index, hence the byte*
    {

        ContractAssert(byteStart != nullptr)

        bool fallbackResult = this->Fallback(bytes, (int)(pBytes - byteStart - size), size);
        if (errno) return 0;

        // See if there's a fallback character and we have an output buffer then copy our string.
        if (fallbackResult)
        {
            int count = 0;

            char16_t ch;
            bool bHighSurrogate = false;
            while ((ch = GetNextChar()) != 0)
            {
                // Make sure no mixed up surrogates
                if (Char::IsSurrogate(ch))
                {
                    if (Char::IsHighSurrogate(ch))
                    {
                        // High Surrogate
                        ContractAssert(!bHighSurrogate)
                        bHighSurrogate = true;
                    }
                    else
                    {
                        // Low surrogate
                        ContractAssert(bHighSurrogate)
                        bHighSurrogate = false;
                    }
                }

                count++;
            }

            // Need to make sure that bHighSurrogate isn't true
            ContractAssert(!bHighSurrogate)

            return count;
        }

        // If no fallback return 0
        return 0;
    }
};

class DecoderReplacementFallbackBuffer : public DecoderFallbackBuffer
{
    // Store our default string
    char16_t strDefault[2];
    int strDefaultLength;
    int fallbackCount = -1;
    int fallbackIndex = -1;

public:
    // Construction
    DecoderReplacementFallbackBuffer(DecoderReplacementFallback* fallback)
    {
        wcscpy_s(strDefault, ARRAY_SIZE(strDefault), fallback->GetDefaultString());
        strDefaultLength = wcslen((const char16_t *)fallback->GetDefaultString());
    }

    // Fallback Methods
    virtual bool Fallback(unsigned char bytesUnknown[], int index, int size)
    {
        // We expect no previous fallback in our buffer
        // We can't call recursively but others might (note, we don't test on last char!!!)
        ContractAssert(fallbackCount < 1)

        // Go ahead and get our fallback
        if (strDefaultLength == 0)
            return false;

        fallbackCount = strDefaultLength;
        fallbackIndex = -1;

        return true;
    }

    virtual char16_t GetNextChar()
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
        ContractAssert(fallbackIndex < strDefaultLength && fallbackIndex >= 0)

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
    virtual int InternalFallback(unsigned char bytes[], unsigned char* pBytes, int size)
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

    virtual bool Fallback(unsigned char bytesUnknown[], int index, int size)
    {
        ContractAssert(false)
    }

    virtual char16_t GetNextChar()
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
        DecoderExceptionFallbackBuffer* pMem = (DecoderExceptionFallbackBuffer*)malloc(sizeof(DecoderExceptionFallbackBuffer));
        if (pMem == nullptr)
        {
            errno = ERROR_INSUFFICIENT_BUFFER;
            return nullptr;
        }
        return new (pMem) DecoderExceptionFallbackBuffer();
    }

    // Maximum number of characters that this instance of this fallback could return
    virtual int GetMaxCharCount()
    {
        return 0;
    }
};

DecoderFallbackBuffer* DecoderReplacementFallback::CreateFallbackBuffer()
{
    DecoderReplacementFallbackBuffer* pMem = (DecoderReplacementFallbackBuffer*)malloc(sizeof(DecoderReplacementFallbackBuffer));
    if (pMem == nullptr)
    {
        errno = ERROR_INSUFFICIENT_BUFFER;
        return nullptr;
    }
    pMem = new (pMem) DecoderReplacementFallbackBuffer(this);
    if (errno)
    {
        free(pMem);
        return nullptr;
    }
    return pMem;
}

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
    char16_t strDefault[2];
    int strDefaultLength;

public:
    // Construction.  Default replacement fallback uses no best fit and ? replacement string
    EncoderReplacementFallback() : EncoderReplacementFallback(W("?"))
    {
    }

    EncoderReplacementFallback(const char16_t* replacement)
    {
        // Must not be null
        ContractAssertVoid(replacement != nullptr)

        // Make sure it doesn't have bad surrogate pairs
        bool bFoundHigh = false;
        int replacementLength = wcslen((const char16_t *)replacement);
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
        ContractAssertVoid(!bFoundHigh)

        wcscpy_s(strDefault, ARRAY_SIZE(strDefault), replacement);
        strDefaultLength = replacementLength;
    }

    char16_t* GetDefaultString()
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
    // Most implementations will probably need an implementation-specific constructor

    // Public methods that cannot be overridden that let us do our fallback thing
    // These wrap the internal methods so that we can check for people doing stuff that is incorrect

public:
    virtual bool Fallback(char16_t charUnknown, int index) = 0;

    virtual bool Fallback(char16_t charUnknownHigh, char16_t charUnknownLow, int index) = 0;

    // Get next character
    virtual char16_t GetNextChar() = 0;

    // Back up a character
    virtual bool MovePrevious() = 0;

    // How many chars left in this fallback?
    virtual int GetRemaining() = 0;

    // Not sure if this should be public or not.
    // Clear the buffer
    virtual void Reset()
    {
        while (GetNextChar() != (char16_t)0);
    }

    // Internal items to help us figure out what we're doing as far as error messages, etc.
    // These help us with our performance and messages internally
protected:
    char16_t*          charStart;
    char16_t*          charEnd;
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
    // This can't be part of the constructor because EncoderFallbacks would have to know how to implement these.
    void InternalInitialize(char16_t* charStart, char16_t* charEnd, bool setEncoder)
    {
        this->charStart = charStart;
        this->charEnd = charEnd;
        this->setEncoder = setEncoder;
        this->bUsedEncoder = false;
        this->bFallingBack = false;
        this->iRecursionCount = 0;
    }

    char16_t InternalGetNextChar()
    {
        char16_t ch = GetNextChar();
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
    virtual bool InternalFallback(char16_t ch, char16_t** chars)
    {
        // Shouldn't have null charStart
        ContractAssert(charStart != nullptr)

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
                char16_t cNext = **chars;
                if (Char::IsLowSurrogate(cNext))
                {
                    // If already falling back then fail
                    ContractAssert(!bFallingBack || iRecursionCount++ <= iMaxRecursion)

                    // Next is a surrogate, add it as surrogate pair, and increment chars
                    (*chars)++;
                    bFallingBack = Fallback(ch, cNext, index);
                    return bFallingBack;
                }

                // Next isn't a low surrogate, just fallback the high surrogate
            }
        }

        // If already falling back then fail
        ContractAssert(!bFallingBack || iRecursionCount++ <= iMaxRecursion)

        // Fall back our char
        bFallingBack = Fallback(ch, index);

        return bFallingBack;
    }
};

class EncoderReplacementFallbackBuffer : public EncoderFallbackBuffer
{
    // Store our default string
    char16_t strDefault[4];
    int strDefaultLength;
    int fallbackCount = -1;
    int fallbackIndex = -1;
public:
    // Construction
    EncoderReplacementFallbackBuffer(EncoderReplacementFallback* fallback)
    {
        // 2X in case we're a surrogate pair
        wcscpy_s(strDefault, ARRAY_SIZE(strDefault), fallback->GetDefaultString());
        wcscat_s(strDefault, ARRAY_SIZE(strDefault), fallback->GetDefaultString());
        strDefaultLength = 2 * wcslen((const char16_t *)fallback->GetDefaultString());

    }

    // Fallback Methods
    virtual bool Fallback(char16_t charUnknown, int index)
    {
        // If we had a buffer already we're being recursive, throw, it's probably at the suspect
        // character in our array.
        ContractAssert(fallbackCount < 1)

        // Go ahead and get our fallback
        // Divide by 2 because we aren't a surrogate pair
        fallbackCount = strDefaultLength / 2;
        fallbackIndex = -1;

        return fallbackCount != 0;
    }

    virtual bool Fallback(char16_t charUnknownHigh, char16_t charUnknownLow, int index)
    {
        // Double check input surrogate pair
        ContractAssert(Char::IsHighSurrogate(charUnknownHigh))
        ContractAssert(Char::IsLowSurrogate(charUnknownLow))

        // If we had a buffer already we're being recursive, throw, it's probably at the suspect
        // character in our array.
        ContractAssert(fallbackCount < 1)

        // Go ahead and get our fallback
        fallbackCount = strDefaultLength;
        fallbackIndex = -1;

        return fallbackCount != 0;
    }

    virtual char16_t GetNextChar()
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
        ContractAssert(fallbackIndex < strDefaultLength && fallbackIndex >= 0)

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

    virtual bool Fallback(char16_t charUnknown, int index)
    {
        // Fall back our char
        ContractAssert(false)
    }

    virtual bool Fallback(char16_t charUnknownHigh, char16_t charUnknownLow, int index)
    {
        ContractAssert(Char::IsHighSurrogate(charUnknownHigh))
        ContractAssert(Char::IsLowSurrogate(charUnknownLow))

        //int iTemp = Char::ConvertToUtf32(charUnknownHigh, charUnknownLow);

        // Fall back our char
        ContractAssert(false)
    }

    virtual char16_t GetNextChar()
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
        EncoderExceptionFallbackBuffer* pMem = (EncoderExceptionFallbackBuffer*)malloc(sizeof(EncoderExceptionFallbackBuffer));
        if (pMem == nullptr)
            return nullptr;
        return new (pMem) EncoderExceptionFallbackBuffer();
    }

    // Maximum number of characters that this instance of this fallback could return
    virtual int GetMaxCharCount()
    {
        return 0;
    }
};

EncoderFallbackBuffer* EncoderReplacementFallback::CreateFallbackBuffer()
{
    EncoderReplacementFallbackBuffer* pMem = (EncoderReplacementFallbackBuffer*)malloc(sizeof(EncoderReplacementFallbackBuffer));
    if (pMem == nullptr)
    {
        errno = ERROR_INSUFFICIENT_BUFFER;
        return nullptr;
    }
    return new (pMem) EncoderReplacementFallbackBuffer(this);
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

#if BIGENDIAN
    bool treatAsLE;
#endif

    bool InRange(int c, int begin, int end)
    {
        return begin <= c && c <= end;
    }

    size_t PtrDiff(char16_t* ptr1, char16_t* ptr2)
    {
        return ptr1 - ptr2;
    }

    size_t PtrDiff(unsigned char* ptr1, unsigned char* ptr2)
    {
        return ptr1 - ptr2;
    }

    // During GetChars we had an invalid byte sequence
    // pSrc is backed up to the start of the bad sequence if we didn't have room to
    // fall it back.  Otherwise pSrc remains where it is.
    bool FallbackInvalidByteSequence(unsigned char** pSrc, int ch, DecoderFallbackBuffer* fallback, char16_t** pTarget)
    {
        // Get our byte[]
        unsigned char* pStart = *pSrc;
        unsigned char bytesUnknown[3];
        int size = GetBytesUnknown(pStart, ch, bytesUnknown);
        bool fallbackResult = fallback->InternalFallback(bytesUnknown, *pSrc, pTarget, size);
        RETURN_ON_ERROR

        // Do the actual fallback
        if (!fallbackResult)
        {
            // Oops, it failed, back up to pStart
            *pSrc = pStart;
            return false;
        }

        // It worked
        return true;
    }

    int FallbackInvalidByteSequence(unsigned char* pSrc, int ch, DecoderFallbackBuffer *fallback)
    {
        // Get our byte[]
        unsigned char bytesUnknown[3];
        int size = GetBytesUnknown(pSrc, ch, bytesUnknown);

        // Do the actual fallback
        int count = fallback->InternalFallback(bytesUnknown, pSrc, size);

        // # of fallback chars expected.
        // Note that we only get here for "long" sequences, and have already unreserved
        // the count that we prereserved for the input bytes
        return count;
    }

    int GetBytesUnknown(unsigned char* pSrc, int ch, unsigned char* bytesUnknown)
    {
        int size;

        // See if it was a plain char
        // (have to check >= 0 because we have all sorts of weird bit flags)
        if (ch < 0x100 && ch >= 0)
        {
            pSrc--;
            bytesUnknown[0] = (unsigned char)ch;
            size =  1;
        }
        // See if its an unfinished 2 byte sequence
        else if ((ch & (SupplimentarySeq | ThreeByteSeq)) == 0)
        {
            pSrc--;
            bytesUnknown[0] = (unsigned char)((ch & 0x1F) | 0xc0);
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
                bytesUnknown[0] = (unsigned char)(((ch >> 12) & 0x07) | 0xF0);
                bytesUnknown[1] = (unsigned char)(((ch >> 6) & 0x3F) | 0x80);
                bytesUnknown[2] = (unsigned char)(((ch)& 0x3F) | 0x80);
                size = 3;
            }
            else if ((ch & (FinalByte >> 12)) != 0)
            {
                // 2nd byte of a 4 byte sequence
                pSrc -= 2;
                bytesUnknown[0] = (unsigned char)(((ch >> 6) & 0x07) | 0xF0);
                bytesUnknown[1] = (unsigned char)(((ch)& 0x3F) | 0x80);
                size = 2;
            }
            else
            {
                // 4th byte of a 4 byte sequence
                pSrc--;
                bytesUnknown[0] = (unsigned char)(((ch)& 0x07) | 0xF0);
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
                bytesUnknown[0] = (unsigned char)(((ch >> 6) & 0x0F) | 0xE0);
                bytesUnknown[1] = (unsigned char)(((ch)& 0x3F) | 0x80);
                size = 2;
            }
            else
            {
                // 1st byte of a 3 byte sequence
                pSrc--;
                bytesUnknown[0] = (unsigned char)(((ch)& 0x0F) | 0xE0);
                size = 1;
            }
        }

        return size;
    }

public:

    UTF8Encoding(bool isThrowException, bool treatAsLE)
        : encoderReplacementFallback(W("\xFFFD")), decoderReplacementFallback(W("\xFFFD"))
#if BIGENDIAN
        , treatAsLE(treatAsLE)
#endif
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

    int GetCharCount(unsigned char* bytes, int count)
    {
        ContractAssert(bytes != nullptr)
        ContractAssert(count >= 0)

        // Initialize stuff
        unsigned char *pSrc = bytes;
        unsigned char *pEnd = pSrc + count;

        // Start by assuming we have as many as count, charCount always includes the adjustment
        // for the character being decoded
        int charCount = count;
        int ch = 0;
        DecoderFallbackBuffer *fallback = nullptr;

        while (true)
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
                ContractAssertFreeFallback((ch & (SupplimentarySeq | ThreeByteSeq)) != 0)

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
                RETURN_ON_ERROR
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
            unsigned char* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
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
            unsigned char *pStop = pSrc + availableBytes - 7;

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
                    ch = *(unsigned short*)pSrc;
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

            LongCodeWithMask32 :
#if BIGENDIAN
            // be careful about the sign extension
            if (!treatAsLE) ch = (int)(((unsigned int)ch) >> 16);
            else
#else
                ch &= 0xFF;
#endif

            LongCodeWithMask16:
#if BIGENDIAN
            if (!treatAsLE) ch = (int)(((unsigned int)ch) >> 8);
            else
#else
                ch &= 0xFF;
#endif

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
                RETURN_ON_ERROR
                fallback->InternalInitialize(bytes, nullptr);
            }
            charCount += FallbackInvalidByteSequence(pSrc, ch, fallback);
        }

        // Shouldn't have anything in fallback buffer for GetCharCount
        // (don't have to check m_throwOnOverflow for count)
        ContractAssertFreeFallback(fallback == nullptr || fallback->GetRemaining() == 0)

        free(fallback);

        return charCount;

    }

    int GetChars(unsigned char* bytes, int byteCount, char16_t* chars, int charCount)
    {
        ContractAssert(chars != nullptr)
        ContractAssert(byteCount >= 0)
        ContractAssert(charCount >= 0)
        ContractAssert(bytes != nullptr)

        unsigned char *pSrc = bytes;
        char16_t *pTarget = chars;

        unsigned char *pEnd = pSrc + byteCount;
        char16_t *pAllocatedBufferEnd = pTarget + charCount;

        int ch = 0;

        DecoderFallbackBuffer *fallback = nullptr;

        while (true)
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
                ContractAssertFreeFallback((ch & (SupplimentarySeq | ThreeByteSeq)) != 0)

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
                    *pTarget = (char16_t)(((ch >> 10) & 0x7FF) +
                        (short)((CharUnicodeInfo::HIGH_SURROGATE_START - (0x10000 >> 10))));
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
                RETURN_ON_ERROR
                fallback->InternalInitialize(bytes, pAllocatedBufferEnd);
            }

            // That'll back us up the appropriate # of bytes if we didn't get anywhere
            if (!FallbackInvalidByteSequence(&pSrc, ch, fallback, &pTarget))
            {
                // Ran out of buffer space
                // Need to throw an exception?
                ContractAssertFreeFallback(pSrc >= bytes || pTarget == chars)
                fallback->InternalReset();
                if (pTarget == chars)
                {
                    errno = ERROR_INSUFFICIENT_BUFFER;
                    if (fallback) free(fallback);
                    return 0;
                }
                ch = 0;
                break;
            }
            ContractAssert(pSrc >= bytes)
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
                // a 4 byte sequence already)
                ContractAssert(pSrc >= bytes || pTarget == chars)
                if (pTarget == chars)
                {
                    errno = ERROR_INSUFFICIENT_BUFFER;
                    if (fallback) free(fallback);
                    return 0;
                }

                // Don't store ch in decoder, we already backed up to its start
                ch = 0;

                // Didn't throw, just use this buffer size.
                break;
            }
            *pTarget = (char16_t)ch;
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
                unsigned char* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                while (pSrc < pLocalEnd) {
                    ch = *pSrc;
                    pSrc++;

                    if (ch > 0x7F)
                        goto ProcessChar;

                    *pTarget = (char16_t)ch;
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
            char16_t *pStop = pTarget + availableBytes - 7;

            while (pTarget < pStop) {
                ch = *pSrc;
                pSrc++;

                if (ch > 0x7F) {
                    goto LongCode;
                }
                *pTarget = (char16_t)ch;
                pTarget++;

                // get pSrc to be 2-byte aligned
                if ((((size_t)pSrc) & 0x1) != 0) {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F) {
                        goto LongCode;
                    }
                    *pTarget = (char16_t)ch;
                    pTarget++;
                }

                // get pSrc to be 4-byte aligned
                if ((((size_t)pSrc) & 0x2) != 0) {
                    ch = *(unsigned short*)pSrc;
                    if ((ch & 0x8080) != 0) {
                        goto LongCodeWithMask16;
                    }

                    // Unfortunately, this is endianness sensitive
#if BIGENDIAN
                    if (!treatAsLE)
                    {
                        *pTarget = (char16_t)((ch >> 8) & 0x7F);
                        pSrc += 2;
                        *(pTarget + 1) = (char16_t)(ch & 0x7F);
                        pTarget += 2;
                    }
                    else
#else
                    {
                        *pTarget = (char16_t)(ch & 0x7F);
                        pSrc += 2;
                        *(pTarget + 1) = (char16_t)((ch >> 8) & 0x7F);
                        pTarget += 2;
                    }
#endif
                }

                // Run 8 characters at a time!
                while (pTarget < pStop) {
                    ch = *(int*)pSrc;
                    int chb = *(int*)(pSrc + 4);
                    if (((ch | chb) & (int)0x80808080) != 0) {
                        goto LongCodeWithMask32;
                    }

                    // Unfortunately, this is endianness sensitive
#if BIGENDIAN
                    if (!treatAsLE)
                    {
                        *pTarget = (char16_t)((ch >> 24) & 0x7F);
                        *(pTarget + 1) = (char16_t)((ch >> 16) & 0x7F);
                        *(pTarget + 2) = (char16_t)((ch >> 8) & 0x7F);
                        *(pTarget + 3) = (char16_t)(ch & 0x7F);
                        pSrc += 8;
                        *(pTarget + 4) = (char16_t)((chb >> 24) & 0x7F);
                        *(pTarget + 5) = (char16_t)((chb >> 16) & 0x7F);
                        *(pTarget + 6) = (char16_t)((chb >> 8) & 0x7F);
                        *(pTarget + 7) = (char16_t)(chb & 0x7F);
                        pTarget += 8;
                    }
                    else
#else
                    {
                        *pTarget = (char16_t)(ch & 0x7F);
                        *(pTarget + 1) = (char16_t)((ch >> 8) & 0x7F);
                        *(pTarget + 2) = (char16_t)((ch >> 16) & 0x7F);
                        *(pTarget + 3) = (char16_t)((ch >> 24) & 0x7F);
                        pSrc += 8;
                        *(pTarget + 4) = (char16_t)(chb & 0x7F);
                        *(pTarget + 5) = (char16_t)((chb >> 8) & 0x7F);
                        *(pTarget + 6) = (char16_t)((chb >> 16) & 0x7F);
                        *(pTarget + 7) = (char16_t)((chb >> 24) & 0x7F);
                        pTarget += 8;
                    }
#endif
                }
                break;

                LongCodeWithMask32 :
#if BIGENDIAN
                // be careful about the sign extension
                if (!treatAsLE) ch = (int)(((unsigned int)ch) >> 16);
                else
#else
                ch &= 0xFF;
#endif

                LongCodeWithMask16:
#if BIGENDIAN
                if (!treatAsLE) ch = (int)(((unsigned int)ch) >> 8);
                else
#else
                ch &= 0xFF;
#endif

                pSrc++;
                if (ch <= 0x7F) {
                    *pTarget = (char16_t)ch;
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

                        *pTarget = (char16_t)(((ch >> 10) & 0x7FF) +
                            (short)(CharUnicodeInfo::HIGH_SURROGATE_START - (0x10000 >> 10)));
                        pTarget++;

                        ch = (ch & 0x3FF) +
                            (short)(CharUnicodeInfo::LOW_SURROGATE_START);

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

                *pTarget = (char16_t)ch;
                pTarget++;

                // extra byte, we're only expecting 1 char for each of these 2 bytes,
                // but the loop is testing the target (not source) against pStop.
                // subtract an extra count from pStop so that we don't overrun the input.
                pStop--;
            }
#endif // FASTLOOP

            ContractAssert(pTarget <= pAllocatedBufferEnd)

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
                RETURN_ON_ERROR
                fallback->InternalInitialize(bytes, pAllocatedBufferEnd);
            }

            // This'll back us up the appropriate # of bytes if we didn't get anywhere
            if (!FallbackInvalidByteSequence(pSrc, ch, fallback))
            {
                ContractAssertFreeFallback(pSrc >= bytes || pTarget == chars)

                // Ran out of buffer space
                // Need to throw an exception?
                fallback->InternalReset();
                if (pTarget == chars)
                {
                    errno = ERROR_INSUFFICIENT_BUFFER;
                    if (fallback) free(fallback);
                    return 0;
                }
            }
            ContractAssertFreeFallback(pSrc >= bytes)
            ch = 0;
        }

        // Shouldn't have anything in fallback buffer for GetChars
        // (don't have to check m_throwOnOverflow for chars)
        ContractAssert(fallback == nullptr || fallback->GetRemaining() == 0)

        free(fallback);

        return PtrDiff(pTarget, chars);
    }

    int GetBytes(char16_t* chars, int charCount, unsigned char* bytes, int byteCount)
    {
        ContractAssert(chars != nullptr)
        ContractAssert(byteCount >= 0)
        ContractAssert(charCount >= 0)
        ContractAssert(bytes != nullptr)

        // For fallback we may need a fallback buffer.
        // We wait to initialize it though in case we don't have any broken input unicode
        EncoderFallbackBuffer* fallback = nullptr;
        char16_t *pSrc = chars;
        unsigned char *pTarget = bytes;

        char16_t *pEnd = pSrc + charCount;
        unsigned char *pAllocatedBufferEnd = pTarget + byteCount;

        int ch = 0;

        // assume that JIT will enregister pSrc, pTarget and ch

        while (true) {
            // SLOWLOOP: does all range checks, handles all special cases, but it is slow

            if (pSrc >= pEnd) {

                if (ch == 0) {
                    // Check if there's anything left to get out of the fallback buffer
                    ch = fallback != nullptr ? fallback->InternalGetNextChar() : 0;
                    if (ch > 0) {
                        goto ProcessChar;
                    }
                }
                else {
                    // Case of leftover surrogates in the fallback buffer
                    if (fallback != nullptr && fallback->bFallingBack) {
                        ContractAssertFreeFallback(ch >= 0xD800 && ch <= 0xDBFF); //, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture))

                        int cha = ch;

                        ch = fallback->InternalGetNextChar();

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
                ContractAssertFreeFallback(ch >= 0xD800 && ch <= 0xDBFF);//, not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture))

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
            if (fallback != nullptr)
            {
                ch = fallback->InternalGetNextChar();
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
                if (fallback == nullptr)
                {
                    // wait on fallbacks if we can
                    // For fallback we may need a fallback buffer
                    fallback = encoderFallback->CreateFallbackBuffer();
                    RETURN_ON_ERROR

                    // Set our internal fallback interesting things.
                    fallback->InternalInitialize(chars, pEnd, true);
                }

                // Do our fallback.  Actually we already know its a mixed up surrogate,
                // so the ref pSrc isn't gonna do anything.
                fallback->InternalFallback((char16_t)ch, &pSrc);
                RETURN_ON_ERROR

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
                if (fallback != nullptr && fallback->bFallingBack)
                {
                    fallback->MovePrevious();              // Didn't use this fallback char
                    if (ch > 0xFFFF)
                        fallback->MovePrevious();          // Was surrogate, didn't use 2nd part either
                }
                else
                {
                    pSrc--;                                     // Didn't use this char
                    if (ch > 0xFFFF)
                        pSrc--;                                 // Was surrogate, didn't use 2nd part either
                }
                ContractAssertFreeFallback(pSrc >= chars || pTarget == bytes)
                if (pTarget == bytes)  // Throw if we must
                {
                    errno = ERROR_INSUFFICIENT_BUFFER;
                    if (fallback) free(fallback);
                    return 0;
                }
                ch = 0;                                         // Nothing left over (we backed up to start of pair if supplimentary)
                break;
            }

            if (ch <= 0x7F) {
                *pTarget = (unsigned char)ch;
            }
            else {
                // use separate helper variables for local contexts so that the jit optimizations
                // won't get confused about the variable lifetimes
                int chb;
                if (ch <= 0x7FF) {
                    // 2 unsigned char encoding
                    chb = (unsigned char)(0xC0 | (ch >> 6));
                }
                else
                {
                    if (ch <= 0xFFFF) {
                        chb = (unsigned char)(0xE0 | (ch >> 12));
                    }
                    else
                    {
                        *pTarget = (unsigned char)(0xF0 | (ch >> 18));
                        pTarget++;

                        chb = 0x80 | ((ch >> 12) & 0x3F);
                    }
                    *pTarget = (unsigned char)chb;
                    pTarget++;

                    chb = 0x80 | ((ch >> 6) & 0x3F);
                }
                *pTarget = (unsigned char)chb;
                pTarget++;

                *pTarget = (unsigned char)0x80 | (ch & 0x3F);
            }
            pTarget++;


#ifdef FASTLOOP
            // If still have fallback don't do fast loop
            if (fallback != nullptr && (ch = fallback->InternalGetNextChar()) != 0)
                goto ProcessChar;

            int availableChars = PtrDiff(pEnd, pSrc);
            int availableBytes = PtrDiff(pAllocatedBufferEnd, pTarget);

            // don't fall into the fast decoding loop if we don't have enough characters
            // Note that if we don't have enough bytes, pStop will prevent us from entering the fast loop.
            if (availableChars <= 13) {
                // we are hoping for 1 unsigned char per char
                if (availableBytes < availableChars) {
                    // not enough output room.  no pending bits at this point
                    ch = 0;
                    continue;
                }

                // try to get over the remainder of the ascii characters fast though
                char16_t* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
                while (pSrc < pLocalEnd) {
                    ch = *pSrc;
                    pSrc++;

                    // Not ASCII, need more than 1 unsigned char per char
                    if (ch > 0x7F)
                        goto ProcessChar;

                    *pTarget = (unsigned char)ch;
                    pTarget++;
                }
                // we are done, let ch be 0 to clear encoder
                ch = 0;
                break;
            }

            // we need at least 1 unsigned char per character, but Convert might allow us to convert
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
            char16_t *pStop = pSrc + availableChars - 5;

            while (pSrc < pStop) {
                ch = *pSrc;
                pSrc++;

                if (ch > 0x7F) {
                    goto LongCode;
                }
                *pTarget = (unsigned char)ch;
                pTarget++;

                // get pSrc aligned
                if (((size_t)pSrc & 0x2) != 0) {
                    ch = *pSrc;
                    pSrc++;
                    if (ch > 0x7F) {
                        goto LongCode;
                    }
                    *pTarget = (unsigned char)ch;
                    pTarget++;
                }

                // Run 4 characters at a time!
                while (pSrc < pStop) {
                    ch = *(int*)pSrc;
                    int chc = *(int*)(pSrc + 2);
                    if (((ch | chc) & (int)0xFF80FF80) != 0) {
                        goto LongCodeWithMask;
                    }

                    // Unfortunately, this is endianness sensitive
#if BIGENDIAN
                    if (!treatAsLE)
                    {
                        *pTarget = (unsigned char)(ch >> 16);
                        *(pTarget + 1) = (unsigned char)ch;
                        pSrc += 4;
                        *(pTarget + 2) = (unsigned char)(chc >> 16);
                        *(pTarget + 3) = (unsigned char)chc;
                        pTarget += 4;
                    }
                    else
#else
                    {
                        *pTarget = (unsigned char)ch;
                        *(pTarget + 1) = (unsigned char)(ch >> 16);
                        pSrc += 4;
                        *(pTarget + 2) = (unsigned char)chc;
                        *(pTarget + 3) = (unsigned char)(chc >> 16);
                        pTarget += 4;
                    }
#endif
                }
                continue;

            LongCodeWithMask:
#if BIGENDIAN
            // be careful about the sign extension
            if (!treatAsLE) ch = (int)(((unsigned int)ch) >> 16);
            else
#else
                ch = (char16_t)ch;
#endif

            pSrc++;

            if (ch > 0x7F) {
                goto LongCode;
            }
            *pTarget = (unsigned char)ch;
            pTarget++;
            continue;

            LongCode:
                // use separate helper variables for slow and fast loop so that the jit optimizations
                // won't get confused about the variable lifetimes
                int chd;
                if (ch <= 0x7FF) {
                    // 2 unsigned char encoding
                    chd = 0xC0 | (ch >> 6);
                }
                else {
                    if (!InRange(ch, CharUnicodeInfo::HIGH_SURROGATE_START, CharUnicodeInfo::LOW_SURROGATE_END)) {
                        // 3 unsigned char encoding
                        chd = 0xE0 | (ch >> 12);
                    }
                    else
                    {
                        // 4 unsigned char encoding - high surrogate + low surrogate
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

                        *pTarget = (unsigned char)(0xF0 | (ch >> 18));
                        // pStop - this unsigned char is compensated by the second surrogate character
                        // 2 input chars require 4 output bytes.  2 have been anticipated already
                        // and 2 more will be accounted for by the 2 pStop-- calls below.
                        pTarget++;

                        chd = 0x80 | ((ch >> 12) & 0x3F);
                    }
                    *pTarget = (unsigned char)chd;
                    pStop--;                    // 3 unsigned char sequence for 1 char, so need pStop-- and the one below too.
                    pTarget++;

                    chd = 0x80 | ((ch >> 6) & 0x3F);
                }
                *pTarget = (unsigned char)chd;
                pStop--;                        // 2 unsigned char sequence for 1 char so need pStop--.
                pTarget++;

                *pTarget = (unsigned char)(0x80 | (ch & 0x3F));
                // pStop - this unsigned char is already included
                pTarget++;
            }

            ContractAssertFreeFallback(pTarget <= pAllocatedBufferEnd)

#endif // FASTLOOP

            // no pending char at this point
            ch = 0;
        }

        free(fallback);

        return (int)(pTarget - bytes);
    }

    int GetByteCount(char16_t *chars, int count)
    {
        // For fallback we may need a fallback buffer.
        // We wait to initialize it though in case we don't have any broken input unicode
        EncoderFallbackBuffer* fallback = nullptr;
        char16_t *pSrc = chars;
        char16_t *pEnd = pSrc + count;

        // Start by assuming we have as many as count
        int byteCount = count;

        int ch = 0;

        while (true) {
            // SLOWLOOP: does all range checks, handles all special cases, but it is slow
            if (pSrc >= pEnd) {

                if (ch == 0) {
                    // Unroll any fallback that happens at the end
                    ch = fallback != nullptr ? fallback->InternalGetNextChar() : 0;
                    if (ch > 0) {
                        byteCount++;
                        goto ProcessChar;
                    }
                }
                else {
                    // Case of surrogates in the fallback.
                    if (fallback != nullptr && fallback->bFallingBack) {
                        ContractAssertFreeFallback(ch >= 0xD800 && ch <= 0xDBFF);// , not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture))

                        ch = fallback->InternalGetNextChar();
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
                ContractAssertFreeFallback(ch >= 0xD800 && ch <= 0xDBFF); // , not 0x" + ((int)ch).ToString("X4", CultureInfo.InvariantCulture))

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
            if (fallback != nullptr)
            {
                ch = fallback->InternalGetNextChar();
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
                if (fallback == nullptr)
                {
                    // wait on fallbacks if we can
                    // For fallback we may need a fallback buffer
                    fallback = encoderFallback->CreateFallbackBuffer();
                    RETURN_ON_ERROR

                    // Set our internal fallback interesting things.
                    fallback->InternalInitialize(chars, chars + count, false);
                }

                // Do our fallback.  Actually we already know its a mixed up surrogate,
                // so the ref pSrc isn't gonna do anything.
                fallback->InternalFallback((char16_t)ch, &pSrc);
                RETURN_ON_ERROR

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
            if (fallback != nullptr && (ch = fallback->InternalGetNextChar()) != 0)
            {
                // We're reserving 1 byte for each char by default
                byteCount++;
                goto ProcessChar;
            }

            int availableChars = PtrDiff(pEnd, pSrc);

            // don't fall into the fast decoding loop if we don't have enough characters
            if (availableChars <= 13) {
                // try to get over the remainder of the ascii characters fast though
                char16_t* pLocalEnd = pEnd; // hint to get pLocalEnd enregistered
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
            char16_t *pStop = pSrc + availableChars - (3 + 4);

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
            if (!treatAsLE) ch = (int)(((unsigned int)ch) >> 16);
            else
#else
                ch = (char16_t)ch;
#endif

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
        ContractAssertFreeFallback(byteCount >= 0)
#endif
        ContractAssertFreeFallback(fallback == nullptr || fallback->GetRemaining() == 0)

        free(fallback);

        return byteCount;
    }
};

int minipal_utf8_to_utf16_preallocated(
    const char* lpSrcStr,
    int cchSrc,
    char16_t** lpDestStr,
    int cchDest,
    unsigned int dwFlags,
    bool treatAsLE)
{
    int ret;
    errno = 0;

    if (cchSrc < 0)
        cchSrc = strlen(lpSrcStr) + 1;

    UTF8Encoding enc(dwFlags & MB_ERR_INVALID_CHARS, treatAsLE);
    ret = enc.GetCharCount((unsigned char*)lpSrcStr, cchSrc);
    if (cchDest)
    {
        if (ret > cchDest)
        {
            errno = ERROR_INSUFFICIENT_BUFFER;
            ret = 0;
        }
        enc.GetChars((unsigned char*)lpSrcStr, cchSrc, (char16_t*)*lpDestStr, ret);
        if (errno) ret = 0;
    }
    return ret;
}

static int utf16_to_utf8_preallocated(
    const char16_t* lpSrcStr,
    int cchSrc,
    char** lpDestStr,
    int cchDest,
    bool treatAsLE)
{
    int ret;
    errno = 0;

    if (cchSrc < 0)
        cchSrc = wcslen(lpSrcStr) + 1;

    UTF8Encoding enc(false, treatAsLE);
    ret = enc.GetByteCount((char16_t*)lpSrcStr, cchSrc);
    if (cchDest)
    {
        if (ret > cchDest)
        {
            errno = ERROR_INSUFFICIENT_BUFFER;
            ret = 0;
        }
        enc.GetBytes((char16_t*)lpSrcStr, cchSrc, (unsigned char*)*lpDestStr, ret);
        if (errno) ret = 0;
    }
    return ret;
}

int minipal_utf16_to_utf8_preallocated(
    const char16_t* lpSrcStr,
    int cchSrc,
    char** lpDestStr,
    int cchDest)
{
    return utf16_to_utf8_preallocated(lpSrcStr, cchSrc, lpDestStr, cchDest, false);
}

int minipal_utf8_to_utf16_allocate(
    const char* lpSrcStr,
    int cchSrc,
    char16_t** lpDestStr,
    unsigned int dwFlags,
    bool treatAsLE)
{
    int cchDest = minipal_utf8_to_utf16_preallocated(lpSrcStr, cchSrc, nullptr, 0, dwFlags, !treatAsLE);
    if (cchDest > 0)
    {
        *lpDestStr = (char16_t*)malloc((cchDest + 1) * sizeof(char16_t));
        cchDest = minipal_utf8_to_utf16_preallocated(lpSrcStr, cchSrc, lpDestStr, cchDest, dwFlags, !treatAsLE);
        (*lpDestStr)[cchDest] = '\0';
    }
    return cchDest;
}

int minipal_utf16_to_utf8_allocate(
    const char16_t* lpSrcStr,
    int cchSrc,
    char** lpDestStr,
    bool treatAsLE)
{
    int cchDest = utf16_to_utf8_preallocated(lpSrcStr, cchSrc, nullptr, 0, treatAsLE);
    if (cchDest > 0)
    {
        *lpDestStr = (char*)malloc((cchDest + 1) * sizeof(char));
        cchDest = utf16_to_utf8_preallocated(lpSrcStr, cchSrc, lpDestStr, cchDest, treatAsLE);
        (*lpDestStr)[cchDest] = '\0';
    }
    return cchDest;
}
