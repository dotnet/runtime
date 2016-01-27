// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// StringLexer.cpp
//


//
// Implements the StringLexer class
//
// ============================================================

#define DISABLE_BINDER_DEBUG_LOGGING

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
        BINDER_LOG_ENTER(L"StringLexer::GetNextLexeme");

        // Remove any white spaces
        do
        {
            wcCurrentChar = PopCharacter(&fIsEscaped);
        }
        while (IsWhitespace(wcCurrentChar));

        // Determine lexeme type
        LEXEME_TYPE kLexemeType = LEXEME_TYPE_INVALID;
        if (!fIsEscaped)
        {
            kLexemeType = GetLexemeType(wcCurrentChar);

            if (kLexemeType != LEXEME_TYPE_STRING)
            {
                return kLexemeType;
            }
        }

        // First character of string lexeme; push it back
        PushCharacter(wcCurrentChar, fIsEscaped);
        kLexemeType = ParseString(currentString, fPermitUnescapedQuotes);
        if (kLexemeType == LEXEME_TYPE_STRING)
        {
            BINDER_LOG_LEAVE_HR(L"StringLexer::GetNextLexeme(LEXEME_TYPE_STRING)", S_OK);
        }
        else
        {
            BINDER_LOG_LEAVE_HR(L"StringLexer::GetNextLexeme(LEXEME_TYPE_INVALID)",
                                S_FALSE);
        }

        return kLexemeType;
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
                BINDER_LOG(L"StringLexer::ParseString: Invalid character encoding");
                return LEXEME_TYPE_INVALID;
            }

            if (IsEOS(wcCurrentChar))
            {
                if (IsQuoteCharacter(wcOpeningQuote))
                {
                    // EOS and unclosed quotes is an error
                    BINDER_LOG(L"StringLexer::ParseString: EOS and unclosed quotes");
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
                BINDER_LOG(L"StringLexer::ParseString: Quote in the middle of a string");
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
            BINDER_LOG(L"StringLexer::ParseString: Trimming string");
            TrimTrailingWhiteSpaces(currentString);
        }

        BINDER_LOG_STRING(L"string", currentString);

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
