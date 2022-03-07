// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

void StringLexer::Init(SString &inputString)
{
    m_cursor = inputString.Begin();
    m_end = inputString.End();
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
    if (wcCurrentChar != INVALID_CHARACTER)
    {
        m_wcCurrentChar = INVALID_CHARACTER;
        *pfIsEscaped = m_fCurrentCharIsEscaped;
        m_cursor++;
    }
    else
    {
        wcCurrentChar = GetNextCharacter(pfIsEscaped);
    }

    return wcCurrentChar;
}

void StringLexer::PushCharacter(WCHAR wcCurrentChar,
                                BOOL  fIsEscaped)
{
    _ASSERTE(m_wcCurrentChar == INVALID_CHARACTER);

    m_wcCurrentChar = wcCurrentChar;
    m_fCurrentCharIsEscaped = fIsEscaped;
    m_cursor--;
}

WCHAR StringLexer::GetRawCharacter()
{
    WCHAR wcCurrentChar = 0;

    if (m_cursor < m_end)
    {
        wcCurrentChar = m_cursor[0];
        m_cursor++;

        // do not allow \0 anywhere in the string.
        if (wcCurrentChar == 0)
        {
            wcCurrentChar = INVALID_CHARACTER;
        }
    }
    else
    {
        // EOS
        wcCurrentChar = 0;
    }

    return wcCurrentChar;
}

WCHAR StringLexer::GetNextCharacter(BOOL *pfIsEscaped)
{
    *pfIsEscaped = FALSE;

    WCHAR wcCurrentChar = GetRawCharacter();
    if (wcCurrentChar == L'\\')
    {
        WCHAR wcTempChar = GetRawCharacter();

        // Handle standard escapes
        switch (wcTempChar)
        {
        case L'"':
        case L'\'':
        case L',':
        case L'\\':
        case L'=':
        case L't':
        case L'n':
        case L'r':
            break;
        default:
            return INVALID_CHARACTER;
        }

        *pfIsEscaped = TRUE;
        wcCurrentChar = wcTempChar;
    }

    return wcCurrentChar;
}

#endif
