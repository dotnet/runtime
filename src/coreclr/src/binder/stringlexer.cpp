// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// StringLexer.cpp
//


//
// Implements the StringLexer class
//
// ============================================================

#include "stringlexer.hpp"
#include "utils.hpp"

#include "ex.h"

namespace BINDER_SPACE
{
    StringLexer::LEXEME_TYPE
    StringLexer::GetNextLexeme(SString &currentString, BOOL fPermitUnescapedQuotes)
    {
        BOOL fIsEscaped = FALSE;
        WCHAR wcCurrentChar = INVALID_CHARACTER;

        // Remove any white spaces
        do
        {
            wcCurrentChar = PopCharacter(&fIsEscaped);
        }
        while (IsWhitespace(wcCurrentChar));

        // Determine lexeme type
        if (!fIsEscaped)
        {
            LEXEME_TYPE kLexemeType = GetLexemeType(wcCurrentChar);
            if (kLexemeType != LEXEME_TYPE_STRING)
            {
                return kLexemeType;
            }
        }

        // First character of string lexeme; push it back
        PushCharacter(wcCurrentChar, fIsEscaped);
        return ParseString(currentString, fPermitUnescapedQuotes);
    }

    StringLexer::LEXEME_TYPE
    StringLexer::ParseString(SString &currentString, BOOL fPermitUnescapedQuotes)
    {
        BOOL fIsFirstCharacter = TRUE;
        WCHAR wcCurrentChar = INVALID_CHARACTER;
        WCHAR wcOpeningQuote = INVALID_CHARACTER;

        currentString.Clear();

        // Read until we find another lexeme that's not a string character
        for (;;)
        {
            BOOL fIsEscaped = FALSE;
            wcCurrentChar = PopCharacter(&fIsEscaped);

            if (wcCurrentChar == INVALID_CHARACTER)
            {
                // Found invalid character encoding
                return LEXEME_TYPE_INVALID;
            }

            if (IsEOS(wcCurrentChar))
            {
                if (IsQuoteCharacter(wcOpeningQuote))
                {
                    // EOS and unclosed quotes is an error
                    return LEXEME_TYPE_INVALID;
                }
                else
                {
                    // Reached end of input and therefore of string
                    break;
                }
            }

            if (fIsFirstCharacter)
            {
                fIsFirstCharacter = FALSE;

                // If first character is quote, then record its quoteness
                if (IsQuoteCharacter(wcCurrentChar))
                {
                    wcOpeningQuote = wcCurrentChar;
                    continue;
                }
            }

            if (wcCurrentChar == wcOpeningQuote)
            {
                // We've found the closing quote for a quoted string
                break;
            }

            if (!fPermitUnescapedQuotes && !fIsEscaped && IsQuoteCharacter(wcCurrentChar) && !IsQuoteCharacter(wcOpeningQuote))
            {
                // Unescaped quotes in the middle of the string are an error
                return LEXEME_TYPE_INVALID;
            }

            if (IsSeparatorChar(wcCurrentChar) && !IsQuoteCharacter(wcOpeningQuote) && !fIsEscaped)
            {
                // Unescaped separator char terminates the string
                PushCharacter(wcCurrentChar, fIsEscaped);
                break;
            }

            // Add character to current string
            currentString.Append(wcCurrentChar);
        }

        if (!IsQuoteCharacter(wcOpeningQuote))
        {
            // Remove trailing white spaces from unquoted string
            TrimTrailingWhiteSpaces(currentString);
        }

        return LEXEME_TYPE_STRING;
    }

    void StringLexer::TrimTrailingWhiteSpaces(SString &currentString)
    {
        SString::Iterator begin = currentString.Begin();
        SString::Iterator cursor = currentString.End() - 1;
        BOOL fFoundWhiteSpace = FALSE;

        for (;;)
        {
            if ((cursor >= begin) && IsWhitespace(cursor[0]))
            {
                fFoundWhiteSpace = TRUE;
                cursor--;
                continue;
            }
            break;
        }

        if (fFoundWhiteSpace)
        {
            currentString.Truncate(cursor + 1);
        }
    }
};
