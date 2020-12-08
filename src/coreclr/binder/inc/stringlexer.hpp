// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// StringLexer.hpp
//


//
// Defines the StringLexer class
//
// ============================================================

#ifndef __BINDER__STRING_LEXER_HPP__
#define __BINDER__STRING_LEXER_HPP__

#include "bindertypes.hpp"

#define GO_IF_NOT_EXPECTED(expr, kRequiredLexemeType)   \
    if ((expr) != kRequiredLexemeType)                  \
    {                                                   \
        fIsValid = FALSE;                               \
        goto Exit;                                      \
    }

#define GO_IF_END_OR_NOT_EXPECTED(expr, kRequiredLexemeType)            \
    {                                                                   \
        LEXEME_TYPE kGotLexemeType = (expr);                            \
        if (kGotLexemeType == LEXEME_TYPE_END_OF_STREAM)                \
        {                                                               \
            goto Exit;                                                  \
        }                                                               \
        else                                                            \
        {                                                               \
            GO_IF_NOT_EXPECTED(kGotLexemeType, kRequiredLexemeType);    \
        }                                                               \
    }

namespace BINDER_SPACE
{
    class StringLexer
    {
    public:
        typedef enum
        {
            LEXEME_TYPE_INVALID,
            LEXEME_TYPE_EQUALS,
            LEXEME_TYPE_COMMA,
            LEXEME_TYPE_COLON,
            LEXEME_TYPE_SEMICOLON,
            LEXEME_TYPE_STRING,
            LEXEME_TYPE_END_OF_STREAM
        } LEXEME_TYPE;

        inline StringLexer();
        inline ~StringLexer();

        inline void Init(SString &inputString, BOOL fSupportEscaping);

        static inline BOOL IsWhitespace(WCHAR wcChar);
        static inline BOOL IsEOS(WCHAR wcChar);
        static inline BOOL IsQuoteCharacter(WCHAR wcChar);

        virtual BOOL IsSeparatorChar(WCHAR wcChar) = NULL;
        virtual LEXEME_TYPE GetLexemeType(WCHAR wcChar) = NULL;

    protected:
        static const WCHAR INVALID_CHARACTER = -1;

        LEXEME_TYPE GetNextLexeme(SString &currentString, BOOL fPermitUnescapedQuotes = FALSE);

        inline WCHAR PopCharacter(BOOL *pfIsEscaped);
        inline void PushCharacter(WCHAR wcCurrentChar,
                                  BOOL fIsEscaped);

        inline WCHAR GetRawCharacter();
        inline void PushRawCharacter();
        inline WCHAR DecodeUTF16Character();
        inline WCHAR GetNextCharacter(BOOL *pfIsEscaped);

        inline WCHAR ParseUnicode();
        LEXEME_TYPE ParseString(SString &currentString,
                                BOOL     fPermitUnescapeQuotes);

        void TrimTrailingWhiteSpaces(SString &currentString);

        SString::Iterator m_cursor;
        SString::Iterator m_end;

        WCHAR m_wcCurrentChar;
        BOOL m_fCurrentCharIsEscaped;
        BOOL m_fSupportEscaping;
        BOOL m_fReadRawCharacter;
    };

#include "stringlexer.inl"
};

#endif
