// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// StringLexer.inl
//


//
// Implements the inlined methods of StringLexer class
//
// ============================================================

#ifndef __BINDER__STRING_LEXER_INL__
#define __BINDER__STRING_LEXER_INL__

StringLexer::StringLexer()
{
    m_wcCurrentChar = INVALID_CHARACTER;
    m_fCurrentCharIsEscaped = FALSE;
}

StringLexer::~StringLexer()
{
    // Nothing to do here
}

void StringLexer::Init(SString &inputString, BOOL fSupportEscaping)
{
    m_cursor = inputString.Begin();
    m_end = inputString.End();
    m_fSupportEscaping = fSupportEscaping;
    m_fReadRawCharacter = FALSE;
}

BOOL StringLexer::IsWhitespace(WCHAR wcChar)
{
    return ((wcChar == L'\n') || (wcChar == L'\r') || (wcChar == L' ') || (wcChar == L'\t'));
}

BOOL StringLexer::IsEOS(WCHAR wcChar)
{
    return (wcChar == 0);
}
        
BOOL StringLexer::IsQuoteCharacter(WCHAR wcChar)
{
    return ((wcChar == L'\'') || (wcChar == L'"'));
}

WCHAR StringLexer::PopCharacter(BOOL *pfIsEscaped)
{
    WCHAR wcCurrentChar = m_wcCurrentChar;
    BINDER_LOG_ENTER(L"StringLexer::PopCharacter");

    if (wcCurrentChar != INVALID_CHARACTER)
    {
        BINDER_LOG(L"HAVE wcCurrentChar");
        m_wcCurrentChar = INVALID_CHARACTER;
        *pfIsEscaped = m_fCurrentCharIsEscaped;
    }
    else
    {
        BINDER_LOG(L"GET wcCurrentChar");
        wcCurrentChar = GetNextCharacter(pfIsEscaped);
    }

#ifdef BINDER_DEBUG_LOG
    PathString info;

    info.Printf(L"wcCurrentChar=%p", (void *) wcCurrentChar);
    BINDER_LOG((WCHAR *) info.GetUnicode());
#endif

    BINDER_LOG_LEAVE(L"StringLexer::PopCharacter");
    return wcCurrentChar;
}

void StringLexer::PushCharacter(WCHAR wcCurrentChar,
                                BOOL  fIsEscaped)
{
    BINDER_LOG_ENTER(L"StringLexer::PushCharacter");

#ifdef BINDER_DEBUG_LOG
    PathString info;

    info.Printf(L"wcCurrentChar=%p, fIsEscaped=%d", (void *) wcCurrentChar, fIsEscaped);
    BINDER_LOG((WCHAR *) info.GetUnicode());
#endif

    _ASSERTE(m_wcCurrentChar == INVALID_CHARACTER);

    m_wcCurrentChar = wcCurrentChar;
    m_fCurrentCharIsEscaped = fIsEscaped;

    BINDER_LOG_LEAVE(L"StringLexer::PushCharacter");
}

WCHAR StringLexer::GetRawCharacter()
{
    WCHAR wcCurrentChar = 0;

    if (m_cursor <= m_end)
    {
        wcCurrentChar = m_cursor[0];
        m_fReadRawCharacter = TRUE;
        m_cursor++;
    }
    else
    {
        m_fReadRawCharacter = FALSE;
    }

    return wcCurrentChar;
}

void StringLexer::PushRawCharacter()
{
    if (m_fReadRawCharacter)
    {
        m_cursor--;
        m_fReadRawCharacter = FALSE;
    }
}

WCHAR StringLexer::DecodeUTF16Character()
{
    // See http://www.ietf.org/rfc/rfc2781.txt for details on UTF-16 encoding.

    WCHAR wcCurrentChar = 0;
    SCOUNT_T nCharacters = m_end - m_cursor + 1;
    WCHAR wcChar1 = GetRawCharacter();

    if (wcChar1 < 0xd800)
    {
        wcCurrentChar = wcChar1;
    }
    else
    {
        // StringLexer is not designed to handle UTF-16 characters beyond the Basic Multilingual Plane,
        // since it stores all characters in 16-bit WCHARs.
        // However, since the vast majority of the time, we (Microsoft) produce the manifests,
        // this is likely a non-scenario, as the other Unicode planes would never be used in practice.

        if (wcChar1 <= 0xdbff) // 0xd800 - 0xdbff indicates the first WCHAR of a surrogate pair
        {
            if (nCharacters >= 2)
            {
                GetRawCharacter(); // Skip the second WCHAR of the surrogate pair
            }
        }
        // Otherwise, the character is either in the 0xdc00 - 0xdfff range, indicating the second WCHAR of a surrogate pair,
        // or in the 0xE000 - 0xFFFF range, which has within it ranges of invalid characters, and which we conservatively treat
        // as invalid.

        wcCurrentChar = INVALID_CHARACTER;
    }

    return wcCurrentChar;
}


WCHAR StringLexer::GetNextCharacter(BOOL *pfIsEscaped)
{
    *pfIsEscaped = FALSE;

    WCHAR wcCurrentChar = GetRawCharacter(); // DecodeUTF16Character()
    if (wcCurrentChar == L'\\')
    {
        WCHAR wcTempChar = GetRawCharacter(); // DecodeUTF16Character()

        if (m_fSupportEscaping)
        {
            // Handle standard escapes
            switch (wcTempChar)
            {
            case L'"':
            case L'\'':
            case L',':
            case L'\\':
            case L'/':
            case L'=':
                break;
            case L't':
                wcTempChar = 9;
                break;
            case L'n':
                wcTempChar = 10;
                break;
            case L'r':
                wcTempChar = 13;
                break;
            case L'u':
                wcTempChar = ParseUnicode();
                break;
            default:
                return INVALID_CHARACTER;
            }

            *pfIsEscaped = TRUE;
            wcCurrentChar = wcTempChar;
        }
        else
        {
            // Do not handle escapes except for quotes
            switch (wcTempChar)
            {
            case L'"':
            case L'\'':
                *pfIsEscaped = TRUE;
                wcCurrentChar = wcTempChar;
                break;
            default:
                PushRawCharacter();
                break;
            }
        }
    }

    return wcCurrentChar;
}

WCHAR StringLexer::ParseUnicode()
{
    int nCharacters = 0;
    WCHAR wcUnicodeChar = 0;

    for(;;)
    {
        WCHAR wcCurrentChar = DecodeUTF16Character();
        nCharacters++;

        if (wcCurrentChar == L';')
        {
            break;
        }
        else if ((wcCurrentChar == INVALID_CHARACTER) || (nCharacters >= 9))
        {
            return INVALID_CHARACTER;
        }

        wcUnicodeChar <<= 4;

        if ((wcCurrentChar >= L'0') && (wcCurrentChar <= L'9'))
        {
            wcUnicodeChar += (wcCurrentChar - L'0');
        }
        else if ((wcCurrentChar >= L'a') && (wcCurrentChar <= L'f'))
        {
            wcUnicodeChar += (wcCurrentChar - L'a') + 10;
        }
        else if ((wcCurrentChar >= L'A') && (wcCurrentChar <= L'F'))
        {
            wcUnicodeChar += (wcCurrentChar - L'A') + 10;
        }
        else
        {
            return INVALID_CHARACTER;
        }
    }
    
    return wcUnicodeChar;
}

#endif
