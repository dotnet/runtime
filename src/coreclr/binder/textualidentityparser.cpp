// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ============================================================
//
// TextualIdentityParser.cpp
//


//
// Implements the TextualIdentityParser class
//
// ============================================================

#include "textualidentityparser.hpp"
#include "assemblyidentity.hpp"

#include "ex.h"

#define GO_IF_SEEN(kAssemblyIdentityFlag)                       \
    if ((m_dwAttributesSeen & kAssemblyIdentityFlag) != 0)      \
    {                                                           \
        fIsValid = FALSE;                                       \
        goto Exit;                                              \
    }                                                           \
    else                                                        \
    {                                                           \
          m_dwAttributesSeen |= kAssemblyIdentityFlag;          \
    }

#define GO_IF_WILDCARD(valueString)             \
    {                                           \
        SmallStackSString wildCard(W("*"));       \
        if (valueString.Equals(wildCard))       \
        {                                       \
            goto Exit;                          \
        }                                       \
    }

#define GO_IF_VALIDATE_FAILED(validateProc, kIdentityFlag)      \
    if (!validateProc(valueString))                             \
    {                                                           \
        fIsValid = FALSE;                                       \
        goto Exit;                                              \
    }                                                           \
    else                                                        \
    {                                                           \
        m_pAssemblyIdentity->SetHave(kIdentityFlag);            \
   }

#define FROMHEX(a) ((a)>=W('a') ? a - W('a') + 10 : a - W('0'))
#define TOHEX(a) ((a)>=10 ? W('a')+(a)-10 : W('0')+(a))
#define TOLOWER(a) (((a) >= W('A') && (a) <= W('Z')) ? (W('a') + (a - W('A'))) : (a))

namespace BINDER_SPACE
{
    namespace
    {
        const int iPublicKeyTokenLength = 8;

        const int iVersionMax = 65535;
        const int iVersionParts = 4;

        inline void UnicodeHexToBin(LPCWSTR pSrc, UINT cSrc, LPBYTE pDest)
        {
            BYTE v;
            LPBYTE pd = pDest;
            LPCWSTR ps = pSrc;

            if (cSrc == 0)
                return;

            for (UINT i = 0; i < cSrc-1; i+=2)
            {
                v =  (BYTE)FROMHEX(TOLOWER(ps[i])) << 4;
                v |= FROMHEX(TOLOWER(ps[i+1]));
                *(pd++) = v;
            }
        }

        inline void BinToUnicodeHex(const BYTE *pSrc, UINT cSrc, __out_ecount(2*cSrc) LPWSTR pDst)
        {
            UINT x;
            UINT y;

            for (x = 0, y = 0 ; x < cSrc; ++x)
            {
                UINT v;

                v = pSrc[x]>>4;
                pDst[y++] = (WCHAR)TOHEX(v);
                v = pSrc[x] & 0x0f;
                pDst[y++] = (WCHAR)TOHEX(v);
            }
        }

        inline bool EqualsCaseInsensitive(const SString &a, LPCWSTR wzB)
        {
            return SString::_wcsicmp(a.GetUnicode(), wzB) == 0;
        }

        BOOL ValidateHex(SString &publicKeyOrToken)
        {
            if ((publicKeyOrToken.GetCount() == 0) || ((publicKeyOrToken.GetCount() % 2) != 0))
            {
                return FALSE;
            }

            SString::Iterator cursor = publicKeyOrToken.Begin();
            SString::Iterator end = publicKeyOrToken.End() - 1;

            while (cursor <= end)
            {
                WCHAR wcCurrentChar = cursor[0];

                if (((wcCurrentChar >= W('0')) && (wcCurrentChar <= W('9'))) ||
                    ((wcCurrentChar >= W('a')) && (wcCurrentChar <= W('f'))) ||
                    ((wcCurrentChar >= W('A')) && (wcCurrentChar <= W('F'))))
                {
                    cursor++;
                    continue;
                }

                return FALSE;
            }

            return TRUE;
        }

        inline BOOL ValidatePublicKeyToken(SString &publicKeyToken)
        {
            return ((publicKeyToken.GetCount() == (iPublicKeyTokenLength * 2)) &&
                    ValidateHex(publicKeyToken));
        }

        inline BOOL ValidatePublicKey(SString &publicKey)
        {
            return ValidateHex(publicKey);
        }

        const struct {
            LPCWSTR strValue;
            PEKIND enumValue;
        } wszKnownArchitectures[] = { { W("x86"), peI386 },
                                      { W("IA64"), peIA64 },
                                      { W("AMD64"), peAMD64 },
                                      { W("ARM"), peARM },
                                      { W("MSIL"), peMSIL } };

        BOOL ValidateAndConvertProcessorArchitecture(SString &processorArchitecture,
                                                     PEKIND *pkProcessorArchitecture)
        {
            for (int i = LENGTH_OF(wszKnownArchitectures); i--;)
            {
                if (EqualsCaseInsensitive(processorArchitecture, wszKnownArchitectures[i].strValue))
                {
                    *pkProcessorArchitecture = wszKnownArchitectures[i].enumValue;
                    return TRUE;
                }
            }

            return FALSE;
        }

        LPCWSTR PeKindToString(PEKIND kProcessorArchitecture)
        {
            _ASSERTE(kProcessorArchitecture != peNone);

            for (int i = LENGTH_OF(wszKnownArchitectures); i--;)
            {
                if (wszKnownArchitectures[i].enumValue == kProcessorArchitecture)
                {
                    return wszKnownArchitectures[i].strValue;
                }
            }

            return NULL;
        }

        LPCWSTR ContentTypeToString(AssemblyContentType kContentType)
        {
            _ASSERTE(kContentType != AssemblyContentType_Default);

            if (kContentType == AssemblyContentType_WindowsRuntime)
            {
                return W("WindowsRuntime");
            }

            return NULL;
        }
    };  // namespace (anonymous)

    TextualIdentityParser::TextualIdentityParser(AssemblyIdentity *pAssemblyIdentity)
    {
        m_pAssemblyIdentity = pAssemblyIdentity;
        m_dwAttributesSeen = AssemblyIdentity::IDENTITY_FLAG_EMPTY;
    }

    TextualIdentityParser::~TextualIdentityParser()
    {
        // Nothing to do here
    }

    BOOL TextualIdentityParser::IsSeparatorChar(WCHAR wcChar)
    {
        return ((wcChar == W(',')) || (wcChar == W('=')));
    }

    StringLexer::LEXEME_TYPE TextualIdentityParser::GetLexemeType(WCHAR wcChar)
    {
        switch (wcChar)
        {
        case W('='):
            return LEXEME_TYPE_EQUALS;
        case W(','):
            return LEXEME_TYPE_COMMA;
        case 0:
            return LEXEME_TYPE_END_OF_STREAM;
        default:
            return LEXEME_TYPE_STRING;
        }
    }

    /* static */
    HRESULT TextualIdentityParser::Parse(SString          &textualIdentity,
                                         AssemblyIdentity *pAssemblyIdentity,
                                         BOOL              fPermitUnescapedQuotes)
    {
        HRESULT hr = S_OK;

        IF_FALSE_GO(pAssemblyIdentity != NULL);

        EX_TRY
        {
            TextualIdentityParser identityParser(pAssemblyIdentity);

            if (!identityParser.Parse(textualIdentity, fPermitUnescapedQuotes))
            {
                IF_FAIL_GO(FUSION_E_INVALID_NAME);
            }
        }
        EX_CATCH_HRESULT(hr);

    Exit:
        return hr;
    }

    /* static */
    HRESULT TextualIdentityParser::ToString(AssemblyIdentity *pAssemblyIdentity,
                                            DWORD             dwIdentityFlags,
                                            SString          &textualIdentity)
    {
        HRESULT hr = S_OK;

        IF_FALSE_GO(pAssemblyIdentity != NULL);

        EX_TRY
        {
            SmallStackSString tmpString;

            textualIdentity.Clear();

            if (pAssemblyIdentity->m_simpleName.IsEmpty())
            {
                goto Exit;
            }

            EscapeString(pAssemblyIdentity->m_simpleName, tmpString);
            textualIdentity.Append(tmpString);

            if (AssemblyIdentity::Have(dwIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_VERSION))
            {
                tmpString.Clear();
                tmpString.Printf(W("%d.%d.%d.%d"),
                                 (DWORD)(USHORT)pAssemblyIdentity->m_version.GetMajor(),
                                 (DWORD)(USHORT)pAssemblyIdentity->m_version.GetMinor(),
                                 (DWORD)(USHORT)pAssemblyIdentity->m_version.GetBuild(),
                                 (DWORD)(USHORT)pAssemblyIdentity->m_version.GetRevision());

                textualIdentity.Append(W(", Version="));
                textualIdentity.Append(tmpString);
            }

            if (AssemblyIdentity::Have(dwIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_CULTURE))
            {
                textualIdentity.Append(W(", Culture="));
                if (pAssemblyIdentity->m_cultureOrLanguage.IsEmpty())
                {
                    textualIdentity.Append(W("neutral"));
                }
                else
                {
                    EscapeString(pAssemblyIdentity->m_cultureOrLanguage, tmpString);
                    textualIdentity.Append(tmpString);
                }
            }

            if (AssemblyIdentity::Have(dwIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY))
            {
                textualIdentity.Append(W(", PublicKey="));
                tmpString.Clear();
                BlobToHex(pAssemblyIdentity->m_publicKeyOrTokenBLOB, tmpString);
                textualIdentity.Append(tmpString);
            }
            else if (AssemblyIdentity::Have(dwIdentityFlags,
                                            AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN))
            {
                textualIdentity.Append(W(", PublicKeyToken="));
                tmpString.Clear();
                BlobToHex(pAssemblyIdentity->m_publicKeyOrTokenBLOB, tmpString);
                textualIdentity.Append(tmpString);
            }
            else if (AssemblyIdentity::Have(dwIdentityFlags,
                                            AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL))
            {
                textualIdentity.Append(W(", PublicKeyToken=null"));
            }

            if (AssemblyIdentity::Have(dwIdentityFlags,
                                       AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE))
            {
                textualIdentity.Append(W(", processorArchitecture="));
                textualIdentity.Append(PeKindToString(pAssemblyIdentity->m_kProcessorArchitecture));
            }

            if (AssemblyIdentity::Have(dwIdentityFlags,
                                       AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE))
            {
                textualIdentity.Append(W(", Retargetable=Yes"));
            }

            if (AssemblyIdentity::Have(dwIdentityFlags,
                                       AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE))
            {
                textualIdentity.Append(W(", ContentType="));
                textualIdentity.Append(ContentTypeToString(pAssemblyIdentity->m_kContentType));
            }

            if (AssemblyIdentity::Have(dwIdentityFlags, AssemblyIdentity::IDENTITY_FLAG_CUSTOM))
            {
                textualIdentity.Append(W(", Custom="));
                tmpString.Clear();
                BlobToHex(pAssemblyIdentity->m_customBLOB, tmpString);
                textualIdentity.Append(tmpString);
            }
            else if (AssemblyIdentity::Have(dwIdentityFlags,
                                            AssemblyIdentity::IDENTITY_FLAG_CUSTOM_NULL))
            {
                textualIdentity.Append(W(", Custom=null"));
            }
        }
        EX_CATCH_HRESULT(hr);

    Exit:
        return hr;
    }

    /* static */
    BOOL TextualIdentityParser::ParseVersion(SString         &versionString,
                                             AssemblyVersion *pAssemblyVersion)
    {
        BOOL fIsValid = FALSE;
        DWORD dwFoundNumbers = 0;
        bool foundUnspecifiedComponent = false;
        const DWORD UnspecifiedNumber = (DWORD)-1;
        DWORD dwCurrentNumber = UnspecifiedNumber;
        DWORD dwNumbers[iVersionParts] = {UnspecifiedNumber, UnspecifiedNumber, UnspecifiedNumber, UnspecifiedNumber};

        if (versionString.GetCount() > 0) {
            SString::Iterator cursor = versionString.Begin();
            SString::Iterator end = versionString.End();

            while (cursor <= end)
            {
                WCHAR wcCurrentChar = cursor[0];

                if (dwFoundNumbers >= static_cast<DWORD>(iVersionParts))
                {
                    goto Exit;
                }
                else if (wcCurrentChar == W('.') || wcCurrentChar == W('\0'))
                {
                    if (dwCurrentNumber == UnspecifiedNumber)
                    {
                        // Difference from .NET Framework, compat with Version(string) constructor: A missing version component
                        // is considered invalid.
                        //
                        // Examples:
                        //   "MyAssembly, Version=."
                        //   "MyAssembly, Version=1."
                        //   "MyAssembly, Version=.1"
                        //   "MyAssembly, Version=1..1"
                        goto Exit;
                    }

                    // Compat with .NET Framework: A value of iVersionMax is considered unspecified. Once an unspecified
                    // component is found, validate the remaining components but consider them as unspecified as well.
                    if (dwCurrentNumber == iVersionMax)
                    {
                        foundUnspecifiedComponent = true;
                        dwCurrentNumber = UnspecifiedNumber;
                    }
                    else if (!foundUnspecifiedComponent)
                    {
                        dwNumbers[dwFoundNumbers] = dwCurrentNumber;
                        dwCurrentNumber = UnspecifiedNumber;
                    }

                    dwFoundNumbers++;
                }
                else if ((wcCurrentChar >= W('0')) && (wcCurrentChar <= W('9')))
                {
                    if (dwCurrentNumber == UnspecifiedNumber)
                    {
                        dwCurrentNumber = 0;
                    }
                    dwCurrentNumber = (dwCurrentNumber * 10) + (wcCurrentChar - W('0'));

                    if (dwCurrentNumber > static_cast<DWORD>(iVersionMax))
                    {
                        goto Exit;
                    }
                }
                else
                {
                    goto Exit;
                }

                cursor++;
            }

            // Difference from .NET Framework: If the major or minor version are unspecified, the version is considered invalid.
            //
            // Examples:
            //   "MyAssembly, Version="
            //   "MyAssembly, Version=1"
            //   "MyAssembly, Version=65535.1"
            //   "MyAssembly, Version=1.65535"
            if (dwFoundNumbers < 2 || dwNumbers[0] == UnspecifiedNumber || dwNumbers[1] == UnspecifiedNumber)
            {
                goto Exit;
            }

            pAssemblyVersion->SetFeatureVersion(dwNumbers[0], dwNumbers[1]);
            pAssemblyVersion->SetServiceVersion(dwNumbers[2], dwNumbers[3]);
            fIsValid = TRUE;
        }

    Exit:
        return fIsValid;
    }

    /* static */
    BOOL TextualIdentityParser::HexToBlob(SString &publicKeyOrToken,
                                          BOOL     fValidateHex,
                                          BOOL     fIsToken,
                                          SBuffer &publicKeyOrTokenBLOB)
    {
        // Optional input verification
        if (fValidateHex)
        {
            if ((fIsToken && !ValidatePublicKeyToken(publicKeyOrToken)) ||
                (!fIsToken && !ValidatePublicKey(publicKeyOrToken)))
            {
                return FALSE;
            }
        }

        UINT ccPublicKeyOrToken = publicKeyOrToken.GetCount();
        BYTE *pByteBLOB = publicKeyOrTokenBLOB.OpenRawBuffer(ccPublicKeyOrToken / 2);

        UnicodeHexToBin(publicKeyOrToken.GetUnicode(), ccPublicKeyOrToken, pByteBLOB);
        publicKeyOrTokenBLOB.CloseRawBuffer();

        return TRUE;
    }

    /* static */
    void TextualIdentityParser::BlobToHex(SBuffer &publicKeyOrTokenBLOB,
                                          SString &publicKeyOrToken)
    {
        UINT cbPublicKeyOrTokenBLOB = publicKeyOrTokenBLOB.GetSize();
        WCHAR *pwzpublicKeyOrToken =
            publicKeyOrToken.OpenUnicodeBuffer(cbPublicKeyOrTokenBLOB * 2);

        BinToUnicodeHex(publicKeyOrTokenBLOB, cbPublicKeyOrTokenBLOB, pwzpublicKeyOrToken);
        publicKeyOrToken.CloseBuffer(cbPublicKeyOrTokenBLOB * 2);
    }

    BOOL TextualIdentityParser::Parse(SString &textualIdentity, BOOL fPermitUnescapedQuotes)
    {
        BOOL fIsValid = TRUE;
        SString unicodeTextualIdentity;

        // Lexer modifies input string
        textualIdentity.ConvertToUnicode(unicodeTextualIdentity);
        Init(unicodeTextualIdentity, TRUE /* fSupportEscaping */);

        SmallStackSString currentString;

        // Identity format is simple name (, attr = value)*
        GO_IF_NOT_EXPECTED(GetNextLexeme(currentString, fPermitUnescapedQuotes), LEXEME_TYPE_STRING);
        m_pAssemblyIdentity->m_simpleName.Set(currentString);
        m_pAssemblyIdentity->m_simpleName.Normalize();
        m_pAssemblyIdentity->SetHave(AssemblyIdentity::IDENTITY_FLAG_SIMPLE_NAME);

        for (;;)
        {
            SmallStackSString attributeString;
            SmallStackSString valueString;

            GO_IF_END_OR_NOT_EXPECTED(GetNextLexeme(currentString), LEXEME_TYPE_COMMA);
            GO_IF_NOT_EXPECTED(GetNextLexeme(attributeString), LEXEME_TYPE_STRING);
            GO_IF_NOT_EXPECTED(GetNextLexeme(currentString), LEXEME_TYPE_EQUALS);
            GO_IF_NOT_EXPECTED(GetNextLexeme(valueString), LEXEME_TYPE_STRING);

            if (!PopulateAssemblyIdentity(attributeString, valueString))
            {
                fIsValid = FALSE;
                break;
            }
        }

    Exit:
        return fIsValid;
    }

    BOOL TextualIdentityParser::ParseString(SString &textualString,
                                            SString &contentString)
    {
        BOOL fIsValid = TRUE;
        SString unicodeTextualString;

        // Lexer modifies input string
        textualString.ConvertToUnicode(unicodeTextualString);
        Init(unicodeTextualString, TRUE /* fSupportEscaping */);

        SmallStackSString currentString;
        GO_IF_NOT_EXPECTED(GetNextLexeme(currentString), LEXEME_TYPE_STRING);

        contentString.Set(currentString);
        currentString.Normalize();

    Exit:
        return fIsValid;
    }

    BOOL TextualIdentityParser::PopulateAssemblyIdentity(SString &attributeString,
                                                         SString &valueString)
    {
        BOOL fIsValid = TRUE;

        if (EqualsCaseInsensitive(attributeString, W("culture")) ||
            EqualsCaseInsensitive(attributeString, W("language")))
        {
            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_CULTURE);
            GO_IF_WILDCARD(valueString);

            if (!EqualsCaseInsensitive(valueString, W("neutral")))
            {
                // culture/language is preserved as is
                m_pAssemblyIdentity->m_cultureOrLanguage.Set(valueString);
                m_pAssemblyIdentity->m_cultureOrLanguage.Normalize();
            }

            m_pAssemblyIdentity->SetHave(AssemblyIdentity::IDENTITY_FLAG_CULTURE);
        }
        else if (EqualsCaseInsensitive(attributeString, W("version")))
        {
            AssemblyVersion *pAssemblyVersion = &(m_pAssemblyIdentity->m_version);

            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_VERSION);
            GO_IF_WILDCARD(valueString);

            if (ParseVersion(valueString, pAssemblyVersion))
            {
                m_pAssemblyIdentity->SetHave(AssemblyIdentity::IDENTITY_FLAG_VERSION);
            }
            else
            {
                fIsValid = FALSE;
            }
        }
        else if (EqualsCaseInsensitive(attributeString, W("publickeytoken")))
        {
            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY);
            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN);
            GO_IF_WILDCARD(valueString);

            if (!EqualsCaseInsensitive(valueString, W("null")) &&
                !EqualsCaseInsensitive(valueString, W("neutral")))
            {
                GO_IF_VALIDATE_FAILED(ValidatePublicKeyToken,
                                      AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN);
                HexToBlob(valueString,
                          FALSE /* fValidateHex */,
                          TRUE /* fIsToken */,
                          m_pAssemblyIdentity->m_publicKeyOrTokenBLOB);
            }
            else
            {
                m_pAssemblyIdentity->SetHave(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN_NULL);
            }
        }
        else if (EqualsCaseInsensitive(attributeString, W("publickey")))
        {
            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY_TOKEN);
            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY);

            if (!EqualsCaseInsensitive(valueString, W("null")) &&
                !EqualsCaseInsensitive(valueString, W("neutral")))
            {
                GO_IF_VALIDATE_FAILED(ValidatePublicKey, AssemblyIdentity::IDENTITY_FLAG_PUBLIC_KEY);
                HexToBlob(valueString,
                          FALSE /* fValidateHex */,
                          FALSE /* fIsToken */,
                          m_pAssemblyIdentity->m_publicKeyOrTokenBLOB);
            }
        }
        else if (EqualsCaseInsensitive(attributeString, W("processorarchitecture")))
        {
            PEKIND kProcessorArchitecture = peNone;

            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE);
            GO_IF_WILDCARD(valueString);

            if (ValidateAndConvertProcessorArchitecture(valueString, &kProcessorArchitecture))
            {
                m_pAssemblyIdentity->m_kProcessorArchitecture = kProcessorArchitecture;
                m_pAssemblyIdentity->SetHave(AssemblyIdentity::IDENTITY_FLAG_PROCESSOR_ARCHITECTURE);
            }
            else
            {
                fIsValid = FALSE;
            }
        }
        else if (EqualsCaseInsensitive(attributeString, W("retargetable")))
        {
            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE);

            if (EqualsCaseInsensitive(valueString, W("yes")))
            {
                m_pAssemblyIdentity->SetHave(AssemblyIdentity::IDENTITY_FLAG_RETARGETABLE);
            }
            else if (!EqualsCaseInsensitive(valueString, W("no")))
            {
                fIsValid = FALSE;
            }
        }
        else if (EqualsCaseInsensitive(attributeString, W("contenttype")))
        {
            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE);
            GO_IF_WILDCARD(valueString);

            if (EqualsCaseInsensitive(valueString, W("windowsruntime")))
            {
                m_pAssemblyIdentity->m_kContentType = AssemblyContentType_WindowsRuntime;
                m_pAssemblyIdentity->SetHave(AssemblyIdentity::IDENTITY_FLAG_CONTENT_TYPE);
            }
            else
            {
                fIsValid = FALSE;
            }
        }
        else if (EqualsCaseInsensitive(attributeString, W("custom")))
        {
            GO_IF_SEEN(AssemblyIdentity::IDENTITY_FLAG_CUSTOM);

            if (EqualsCaseInsensitive(valueString, W("null")))
            {
                m_pAssemblyIdentity->SetHave(AssemblyIdentity::IDENTITY_FLAG_CUSTOM_NULL);
            }
            else
            {
                GO_IF_VALIDATE_FAILED(ValidateHex, AssemblyIdentity::IDENTITY_FLAG_CUSTOM);
                HexToBlob(valueString,
                          FALSE /* fValidateHex */,
                          FALSE /* fIsToken */,
                          m_pAssemblyIdentity->m_customBLOB);
            }
        }

    Exit:
        return fIsValid;
    }

    /* static */
    void TextualIdentityParser::EscapeString(SString &input,
                                             SString &result)
    {
        BOOL fNeedQuotes = FALSE;
        WCHAR wcQuoteCharacter = W('"');

        SmallStackSString tmpString;
        SString::Iterator cursor = input.Begin();
        SString::Iterator end = input.End() - 1;

        // Leading/Trailing white space require quotes
        if (IsWhitespace(cursor[0]) || IsWhitespace(end[0]))
        {
            fNeedQuotes = TRUE;
        }

        // Fusion textual identity compat: escape all non-quote characters even if quoted
        while (cursor <= end)
        {
            WCHAR wcCurrentChar = cursor[0];

            switch (wcCurrentChar)
            {
            case W('"'):
            case W('\''):
                if (fNeedQuotes && (wcQuoteCharacter != wcCurrentChar))
                {
                    tmpString.Append(wcCurrentChar);
                }
                else if (!fNeedQuotes)
                {
                    fNeedQuotes = TRUE;
                    wcQuoteCharacter = (wcCurrentChar == W('"') ? W('\'') : W('"'));
                    tmpString.Append(wcCurrentChar);
                }
                else
                {
                    tmpString.Append(W('\\'));
                    tmpString.Append(wcCurrentChar);
                }
                break;
            case W('='):
            case W(','):
            case W('\\'):
                tmpString.Append(W('\\'));
                tmpString.Append(wcCurrentChar);
                break;
            case 9:
                tmpString.Append(W("\\t"));
                break;
            case 10:
                tmpString.Append(W("\\n"));
                break;
            case 13:
                tmpString.Append(W("\\r"));
                break;
            default:
                tmpString.Append(wcCurrentChar);
                break;
            }

            cursor++;
        }

        if (fNeedQuotes)
        {
            result.Clear();
            result.Append(wcQuoteCharacter);
            result.Append(tmpString);
            result.Append(wcQuoteCharacter);
        }
        else
        {
            result.Set(tmpString);
        }
    }
};
