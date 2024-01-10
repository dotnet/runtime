#include "importhelpers.hpp"
#include "signatures.hpp"
#include "pal.hpp"
#include <cassert>
#include <stack>
#include <cctype>
#include <array>
#include <algorithm>
#include <cstring>

// Macros from wincrypt.h that we need avaliable on all platforms
// for strong-name parsing.
// Get the class (hash, signature, encryption, etc) of an algorithm from an ALG_ID
#define GET_ALG_CLASS(x)                (x & (7 << 13))
// Get the sub-identifier of an algorithm (like SHA1) from an ALG_ID
#define GET_ALG_SID(x)                  (x & (511))

#define ALG_CLASS_SIGNATURE             (1 << 13)
#define ALG_CLASS_HASH                  (4 << 13)

#define ALG_SID_SHA1                    4

// Blob definitions from wincrypt.h
#define PUBLICKEYBLOB           0x6

#define RETURN_IF_FAILED(exp) \
{ \
    hr = (exp); \
    if (FAILED(hr)) \
    { \
        return hr; \
    } \
}

namespace
{
    HRESULT GetMvid(mdhandle_t image, mdguid_t* mvid)
    {
        mdcursor_t c;
        uint32_t count;
        if (!md_create_cursor(image, mdtid_Module, &c, &count))
            return CLDB_E_FILE_CORRUPT;
        
        if (1 != md_get_column_value_as_guid(c, mdtModule_Mvid, 1, mvid))
            return CLDB_E_FILE_CORRUPT;
        
        return S_OK;
    }

    CorTokenType GetTokenTypeFromCursor(mdcursor_t cursor)
    {
        mdToken token = mdTokenNil;
        if (!md_cursor_to_token(cursor, &token))
            assert(false);
        
        return (CorTokenType)TypeFromToken(token);
    }

    // The strong name token is the last 8 bytes of the SHA1 hash of the public key.
    // See II.6.3
    constexpr size_t StrongNameTokenSize = 8;
    
    using StrongNameToken = std::array<uint8_t, StrongNameTokenSize>;

    namespace StrongNameKeys
    {
        // The byte values of the real public keys and their corresponding tokens
        // for assemblies the .NET SDK ships.
        // These blobs allow us to skip the token calculation for these assemblies.
        // Each of these keys corresponds to the public key in a file in the .NET Arcade SDK.

        // The byte values of the ECMA pseudo public key and its token.
        // Arcade SDK StrongNameKeyId: ECMA
        // See II.6.2.1.3 for th definition of this key.
        uint8_t const EcmaPublicKey[] = { 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0 };
        const StrongNameToken EcmaToken = { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 };

        // Arcade SDK StrongNameKeyId: Microsoft
        uint8_t const Microsoft[] =
        {
            0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
            0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
            0x07,0xd1,0xfa,0x57,0xc4,0xae,0xd9,0xf0,0xa3,0x2e,0x84,0xaa,0x0f,0xae,0xfd,0x0d,
            0xe9,0xe8,0xfd,0x6a,0xec,0x8f,0x87,0xfb,0x03,0x76,0x6c,0x83,0x4c,0x99,0x92,0x1e,
            0xb2,0x3b,0xe7,0x9a,0xd9,0xd5,0xdc,0xc1,0xdd,0x9a,0xd2,0x36,0x13,0x21,0x02,0x90,
            0x0b,0x72,0x3c,0xf9,0x80,0x95,0x7f,0xc4,0xe1,0x77,0x10,0x8f,0xc6,0x07,0x77,0x4f,
            0x29,0xe8,0x32,0x0e,0x92,0xea,0x05,0xec,0xe4,0xe8,0x21,0xc0,0xa5,0xef,0xe8,0xf1,
            0x64,0x5c,0x4c,0x0c,0x93,0xc1,0xab,0x99,0x28,0x5d,0x62,0x2c,0xaa,0x65,0x2c,0x1d,
            0xfa,0xd6,0x3d,0x74,0x5d,0x6f,0x2d,0xe5,0xf1,0x7e,0x5e,0xaf,0x0f,0xc4,0x96,0x3d,
            0x26,0x1c,0x8a,0x12,0x43,0x65,0x18,0x20,0x6d,0xc0,0x93,0x34,0x4d,0x5a,0xd2,0x93
        };

        StrongNameToken const MicrosoftToken = {0xb0,0x3f,0x5f,0x7f,0x11,0xd5,0x0a,0x3a};

        // Arcade SDK StrongNameKeyId: SilverlightPlatform
        uint8_t const SilverlightPlatform[] =
        {
            0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
            0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
            0x8d,0x56,0xc7,0x6f,0x9e,0x86,0x49,0x38,0x30,0x49,0xf3,0x83,0xc4,0x4b,0xe0,0xec,
            0x20,0x41,0x81,0x82,0x2a,0x6c,0x31,0xcf,0x5e,0xb7,0xef,0x48,0x69,0x44,0xd0,0x32,
            0x18,0x8e,0xa1,0xd3,0x92,0x07,0x63,0x71,0x2c,0xcb,0x12,0xd7,0x5f,0xb7,0x7e,0x98,
            0x11,0x14,0x9e,0x61,0x48,0xe5,0xd3,0x2f,0xba,0xab,0x37,0x61,0x1c,0x18,0x78,0xdd,
            0xc1,0x9e,0x20,0xef,0x13,0x5d,0x0c,0xb2,0xcf,0xf2,0xbf,0xec,0x3d,0x11,0x58,0x10,
            0xc3,0xd9,0x06,0x96,0x38,0xfe,0x4b,0xe2,0x15,0xdb,0xf7,0x95,0x86,0x19,0x20,0xe5,
            0xab,0x6f,0x7d,0xb2,0xe2,0xce,0xef,0x13,0x6a,0xc2,0x3d,0x5d,0xd2,0xbf,0x03,0x17,
            0x00,0xae,0xc2,0x32,0xf6,0xc6,0xb1,0xc7,0x85,0xb4,0x30,0x5c,0x12,0x3b,0x37,0xab
        };

        StrongNameToken const SilverlightPlatformToken = {0x7c,0xec,0x85,0xd7,0xbe,0xa7,0x79,0x8e};

        // Arcade SDK StrongNameKeyId: MicrosoftShared
        uint8_t const Silverlight[] =
        {
            0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
            0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
            0xb5,0xfc,0x90,0xe7,0x02,0x7f,0x67,0x87,0x1e,0x77,0x3a,0x8f,0xde,0x89,0x38,0xc8,
            0x1d,0xd4,0x02,0xba,0x65,0xb9,0x20,0x1d,0x60,0x59,0x3e,0x96,0xc4,0x92,0x65,0x1e,
            0x88,0x9c,0xc1,0x3f,0x14,0x15,0xeb,0xb5,0x3f,0xac,0x11,0x31,0xae,0x0b,0xd3,0x33,
            0xc5,0xee,0x60,0x21,0x67,0x2d,0x97,0x18,0xea,0x31,0xa8,0xae,0xbd,0x0d,0xa0,0x07,
            0x2f,0x25,0xd8,0x7d,0xba,0x6f,0xc9,0x0f,0xfd,0x59,0x8e,0xd4,0xda,0x35,0xe4,0x4c,
            0x39,0x8c,0x45,0x43,0x07,0xe8,0xe3,0x3b,0x84,0x26,0x14,0x3d,0xae,0xc9,0xf5,0x96,
            0x83,0x6f,0x97,0xc8,0xf7,0x47,0x50,0xe5,0x97,0x5c,0x64,0xe2,0x18,0x9f,0x45,0xde,
            0xf4,0x6b,0x2a,0x2b,0x12,0x47,0xad,0xc3,0x65,0x2b,0xf5,0xc3,0x08,0x05,0x5d,0xa9
        };

        StrongNameToken const SilverlightToken = {0x31,0xBF,0x38,0x56,0xAD,0x36,0x4E,0x35};

        // Arcade SDK StrongNameKeyId: MicrosoftAspNetCore
        uint8_t const AspNetCore[] =
        {
            0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
            0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
            0xF3,0x3A,0x29,0x04,0x4F,0xA9,0xD7,0x40,0xC9,0xB3,0x21,0x3A,0x93,0xE5,0x7C,0x84,
            0xB4,0x72,0xC8,0x4E,0x0B,0x8A,0x0E,0x1A,0xE4,0x8E,0x67,0xA9,0xF8,0xF6,0xDE,0x9D,
            0x5F,0x7F,0x3D,0x52,0xAC,0x23,0xE4,0x8A,0xC5,0x18,0x01,0xF1,0xDC,0x95,0x0A,0xBE,
            0x90,0x1D,0xA3,0x4D,0x2A,0x9E,0x3B,0xAA,0xDB,0x14,0x1A,0x17,0xC7,0x7E,0xF3,0xC5,
            0x65,0xDD,0x5E,0xE5,0x05,0x4B,0x91,0xCF,0x63,0xBB,0x3C,0x6A,0xB8,0x3F,0x72,0xAB,
            0x3A,0xAF,0xE9,0x3D,0x0F,0xC3,0xC2,0x34,0x8B,0x76,0x4F,0xAF,0xB0,0xB1,0xC0,0x73,
            0x3D,0xE5,0x14,0x59,0xAE,0xAB,0x46,0x58,0x03,0x84,0xBF,0x9D,0x74,0xC4,0xE2,0x81,
            0x64,0xB7,0xCD,0xE2,0x47,0xF8,0x91,0xBA,0x07,0x89,0x1C,0x9D,0x87,0x2A,0xD2,0xBB
        };

        StrongNameToken const AspNetCoreToken = {0xad, 0xb9, 0x79, 0x38, 0x29, 0xdd, 0xae, 0x60};

        // Arcade SDK StrongNameKeyId: Open
        uint8_t const Open[] =
        {
            0x00,0x24,0x00,0x00,0x04,0x80,0x00,0x00,0x94,0x00,0x00,0x00,0x06,0x02,0x00,0x00,
            0x00,0x24,0x00,0x00,0x52,0x53,0x41,0x31,0x00,0x04,0x00,0x00,0x01,0x00,0x01,0x00,
            0x4B,0x86,0xC4,0xCB,0x78,0x54,0x9B,0x34,0xBA,0xB6,0x1A,0x3B,0x18,0x00,0xE2,0x3B,
            0xFE,0xB5,0xB3,0xEC,0x39,0x00,0x74,0x04,0x15,0x36,0xA7,0xE3,0xCB,0xD9,0x7F,0x5F,
            0x04,0xCF,0x0F,0x85,0x71,0x55,0xA8,0x92,0x8E,0xAA,0x29,0xEB,0xFD,0x11,0xCF,0xBB,
            0xAD,0x3B,0xA7,0x0E,0xFE,0xA7,0xBD,0xA3,0x22,0x6C,0x6A,0x8D,0x37,0x0A,0x4C,0xD3,
            0x03,0xF7,0x14,0x48,0x6B,0x6E,0xBC,0x22,0x59,0x85,0xA6,0x38,0x47,0x1E,0x6E,0xF5,
            0x71,0xCC,0x92,0xA4,0x61,0x3C,0x00,0xB8,0xFA,0x65,0xD6,0x1C,0xCE,0xE0,0xCB,0xE5,
            0xF3,0x63,0x30,0xC9,0xA0,0x1F,0x41,0x83,0x55,0x9F,0x1B,0xEF,0x24,0xCC,0x29,0x17,
            0xC6,0xD9,0x13,0xE3,0xA5,0x41,0x33,0x3A,0x1D,0x05,0xD9,0xBE,0xD2,0x2B,0x38,0xCB
        };

        StrongNameToken const OpenToken = {0xcc, 0x7b, 0x13, 0xff, 0xcd, 0x2d, 0xdd, 0x51};

        struct WellKnownKey final
        {
            uint8_t const* const PublicKey;
            size_t const PublicKeyLen;
            StrongNameToken const& Token;
        };

        static const WellKnownKey WellKnownKeys[] =
        {
            { EcmaPublicKey, sizeof(EcmaPublicKey), EcmaToken },
            { Microsoft, sizeof(Microsoft), MicrosoftToken },
            { SilverlightPlatform, sizeof(SilverlightPlatform), SilverlightPlatformToken },
            { Silverlight, sizeof(Silverlight), SilverlightToken },
            { AspNetCore, sizeof(AspNetCore), AspNetCoreToken },
            { Open, sizeof(Open), OpenToken },
        };

        bool GetTokenForWellKnownKey(uint8_t const* key, size_t keyLength, StrongNameToken* token)
        {
            for (size_t i = 0; i < ARRAY_SIZE(WellKnownKeys); i++)
            {
                if (keyLength == WellKnownKeys[i].PublicKeyLen
                    && std::memcmp(key, WellKnownKeys[i].PublicKey, keyLength) == 0)
                {
                    *token = WellKnownKeys[i].Token;
                    return true;
                }
            }

            return false;
        }
    }

    struct PublicKeyBlob final
    {
        uint32_t SigAlgID;
        uint32_t HashAlgID;
        uint32_t PublicKeyLength;
        uint8_t  PublicKey[];
    };

    HRESULT StrongNameTokenFromPublicKey(span<uint8_t const> publicKeyBlob, StrongNameToken& strongNameTokenBuffer)
    {
        if (publicKeyBlob.size() < sizeof(PublicKeyBlob))
            return CORSEC_E_INVALID_PUBLICKEY;
        
        PublicKeyBlob const* publicKey = reinterpret_cast<PublicKeyBlob const*>((uint8_t const*)publicKeyBlob);

        if (publicKey->PublicKeyLength != publicKeyBlob.size() - sizeof(PublicKeyBlob))
            return CORSEC_E_INVALID_PUBLICKEY;
        
        if (publicKeyBlob.size() == sizeof(StrongNameKeys::EcmaPublicKey)
            && std::memcmp(publicKeyBlob, StrongNameKeys::EcmaPublicKey, sizeof(StrongNameKeys::EcmaPublicKey)) == 0)
        {
            return S_OK;
        }

        if (publicKey->HashAlgID != 0)
        {
            if (GET_ALG_CLASS(publicKey->HashAlgID) != ALG_CLASS_HASH)
                return CORSEC_E_INVALID_PUBLICKEY;
            
            if (GET_ALG_SID(publicKey->HashAlgID) < ALG_SID_SHA1)
                return CORSEC_E_INVALID_PUBLICKEY;
        }

        if (publicKey->SigAlgID != 0 && GET_ALG_CLASS(publicKey->SigAlgID) != ALG_CLASS_SIGNATURE)
            return CORSEC_E_INVALID_PUBLICKEY;
        
        if (publicKey->PublicKeyLength == 0 || publicKey->PublicKey[0] != PUBLICKEYBLOB)
            return CORSEC_E_INVALID_PUBLICKEY;
        
        // Check well-known keys first.
        if (StrongNameKeys::GetTokenForWellKnownKey(publicKey->PublicKey, publicKey->PublicKeyLength, &strongNameTokenBuffer))
            return S_OK;

        std::array<uint8_t, pal::SHA1_HASH_SIZE> hash;
        if (!pal::ComputeSha1Hash(publicKeyBlob, hash))
            return CORSEC_E_INVALID_PUBLICKEY;

        // Take the last few bytes of the hash value for our token.
        // These are the low order bytes from a big-endian point of view.
        // Reverse the order of these bytes in the output buffer to get little-endian byte order.
        // The byte order of the strong name token is not specified in ECMA-335, but is what CLR, CoreCLR, and Mono Desktop have always done.
        std::reverse_copy(hash.begin() + pal::SHA1_HASH_SIZE - StrongNameTokenSize, hash.end(), strongNameTokenBuffer.begin());

        return S_OK;
    }
}

namespace
{
    struct AssemblyVersionMatcher
    {
        bool(*IsApplicable)(char const* name);
        HRESULT(*Match)(mdcursor_t c, uint32_t majorVersion, uint32_t minorVersion, uint32_t buildNumber, uint32_t revisionNumber);
    };

    std::array<AssemblyVersionMatcher, 2> const AssemblyVersionMatchers =
    {
        {
            // COMPAT: CoreCLR resolves all references to mscorlib and Microsoft.VisualC to the same assembly ref ignoring the build and revision version.
            {
                [](char const* name) -> bool
                {
                    auto AsciiCaseInsensitiveEquals = [](char const* a, char const* b)
                    {
                        while (*a != '\0' && *b != '\0')
                        {
                            if (std::tolower(*a) != std::tolower(*b))
                                return false;
                            
                            a++;
                            b++;
                        }

                        return *a == '\0' && *b == '\0';
                    };

                    return AsciiCaseInsensitiveEquals(name, "mscorlib")
                        || AsciiCaseInsensitiveEquals(name, "microsoft.visualc");
                },
                [](mdcursor_t c, uint32_t majorVersion, uint32_t minorVersion, uint32_t buildNumber, uint32_t revisionNumber)
                {
                    UNREFERENCED_PARAMETER(buildNumber);
                    UNREFERENCED_PARAMETER(revisionNumber);
                    uint32_t temp;
                    if (1 != md_get_column_value_as_constant(c, mdtAssemblyRef_MajorVersion, 1, &temp))
                        return CLDB_E_FILE_CORRUPT;
                    
                    if (temp != majorVersion)
                        return S_FALSE;
                    
                    if (1 != md_get_column_value_as_constant(c, mdtAssemblyRef_MinorVersion, 1, &temp))
                        return CLDB_E_FILE_CORRUPT;
                    
                    if (temp != minorVersion)
                        return S_FALSE;

                    return S_OK;
                }
            },
            // Otherwise, we'll compare all of the version components.
            {
                [](char const* name)
                {
                    UNREFERENCED_PARAMETER(name);
                    return true;
                },
                [](mdcursor_t c, uint32_t majorVersion, uint32_t minorVersion, uint32_t buildNumber, uint32_t revisionNumber)
                {
                    uint32_t temp;
                    if (1 != md_get_column_value_as_constant(c, mdtAssemblyRef_MajorVersion, 1, &temp))
                        return CLDB_E_FILE_CORRUPT;
                    
                    if (temp != majorVersion)
                        return S_FALSE;
                    
                    if (1 != md_get_column_value_as_constant(c, mdtAssemblyRef_MinorVersion, 1, &temp))
                        return CLDB_E_FILE_CORRUPT;
                    
                    if (temp != minorVersion)
                        return S_FALSE;
                    if (1 != md_get_column_value_as_constant(c, mdtAssemblyRef_BuildNumber, 1, &temp))
                        return CLDB_E_FILE_CORRUPT;
                    
                    if (temp != buildNumber)
                        return S_FALSE;
                    
                    if (1 != md_get_column_value_as_constant(c, mdtAssemblyRef_RevisionNumber, 1, &temp))
                        return CLDB_E_FILE_CORRUPT;
                    
                    if (temp != revisionNumber)
                        return S_FALSE;

                    return S_OK;
                }
            }
        }
    };
    
    AssemblyVersionMatcher const& GetAssemblyVersionMatcher(char const* name)
    {
        for (AssemblyVersionMatcher const& matcher : AssemblyVersionMatchers)
        {
            if (matcher.IsApplicable(name))
                return matcher;
        }

        // The final matcher should always be applicable.
        // If it isn't, we have a bug in our code.
        assert(false);
        return AssemblyVersionMatchers[AssemblyVersionMatchers.size() - 1];
    }

    HRESULT FindAssemblyRef(
        mdhandle_t targetModule,
        uint32_t majorVersion,
        uint32_t minorVersion,
        uint32_t buildNumber,
        uint32_t revisionNumber,
        uint32_t flags,
        char const* name,
        char const* culture,
        span<const uint8_t> publicKeyOrToken,
        mdcursor_t* assemblyRef)
    {
        HRESULT hr;
    
        bool calculatedPublicKeyToken = false;
        StrongNameToken publicKeyToken{};
        if (IsAfPublicKeyToken(flags) && publicKeyOrToken.size() == StrongNameTokenSize)
        {
            std::copy(publicKeyOrToken.begin(), publicKeyOrToken.end(), publicKeyToken.begin());
            calculatedPublicKeyToken = true;
        }

        // Search the assembly ref table for a matching row.
        mdcursor_t c;
        uint32_t count;
        if (!md_create_cursor(targetModule, mdtid_AssemblyRef, &c, &count))
            return E_FAIL;
        
        AssemblyVersionMatcher const& matcher = GetAssemblyVersionMatcher(name);
        
        for (uint32_t i = 0; i < count; i++, md_cursor_next(&c))
        {
            // Search the table linearly by manually reading the columns.
            hr = matcher.Match(c, majorVersion, minorVersion, buildNumber, revisionNumber);
            RETURN_IF_FAILED(hr);
            if (hr == S_FALSE)
                continue;
            
            char const* tempString;
            if (1 != md_get_column_value_as_utf8(c, mdtAssemblyRef_Name, 1, &tempString))
                return CLDB_E_FILE_CORRUPT;
            
            if (std::strcmp(tempString, name) != 0)
                continue;
            
            if (1 != md_get_column_value_as_utf8(c, mdtAssemblyRef_Culture, 1, &tempString))
                return CLDB_E_FILE_CORRUPT;
            
            if (std::strcmp(tempString, culture) != 0)
                continue;
            
            uint8_t const* tempBlob;
            uint32_t tempBlobLength;
            if (1 != md_get_column_value_as_blob(c, mdtAssemblyRef_PublicKeyOrToken, 1, &tempBlob, &tempBlobLength))
                return CLDB_E_FILE_CORRUPT;
            
            // If our source has a public key or token, we can only match against an AssemblyRef that has a public key or token.
            // If our source doesn't have a public key or token, we can only match against an AssemblyRef that doesn't have a public key or token.
            if ((publicKeyOrToken.size() == 0) != (tempBlobLength == 0))
                continue;

            if (tempBlobLength != 0)
            {
                // Handle the case when a ref may be using a full public key instead of a token.
                StrongNameToken refPublicKeyToken;

                uint32_t assemblyRefFlags;
                if (1 != md_get_column_value_as_constant(c, mdtAssemblyRef_Flags, 1, &assemblyRefFlags))
                    return CLDB_E_FILE_CORRUPT;
                
                if (IsAfPublicKey(flags) == IsAfPublicKey(assemblyRefFlags))
                {
                    // If the source and destination either both have a full key or both have a key token, we can compare them directly.
                    if (tempBlobLength != publicKeyOrToken.size() || !std::equal(publicKeyOrToken.begin(), publicKeyOrToken.end(), tempBlob))
                        continue;
                }
                else if (IsAfPublicKey(assemblyRefFlags))
                {
                    // This AssemblyRef row has a full public key and our source has a token.
                    // We need to get the token from the key.
                    RETURN_IF_FAILED(StrongNameTokenFromPublicKey({ tempBlob, tempBlobLength }, refPublicKeyToken));
                }
                else
                {
                    // This AssemblyRef row has a token and our source has a full public key.
                    // We need to get the token from the key.
                    if (!calculatedPublicKeyToken)
                    {
                        RETURN_IF_FAILED(StrongNameTokenFromPublicKey(publicKeyOrToken, publicKeyToken));
                        calculatedPublicKeyToken = true;
                    }
                }

                // At this point, we have a token for both our source and the AssemblyRef we are checking against.

                // If our source started with a public key token, we should have initialized publicKeyToken to it
                // and set calculatedPublicKeyToken to true.
                // If our source started with a public key, then we should have calculated the token above and
                // set calculatedPublicKeyToken to true.
                assert(calculatedPublicKeyToken);
                if (publicKeyToken != refPublicKeyToken)
                    continue;
            }
            
            *assemblyRef = c;
            return S_OK;
        }

        return S_FALSE;
    }

    HRESULT ImportReferenceToAssemblyRef(
        mdcursor_t sourceAssemblyRef,
        mdhandle_t targetModule,
        std::function<void(mdcursor_t)> onRowAdded,
        mdcursor_t* targetAssembly
    )
    {
        HRESULT hr;
        uint32_t flags;
        if (1 != md_get_column_value_as_constant(sourceAssemblyRef, mdtAssemblyRef_Flags, 1, &flags))
            return E_FAIL;

        uint8_t const* publicKey;
        uint32_t publicKeyLength;
        if (1 != md_get_column_value_as_blob(sourceAssemblyRef, mdtAssemblyRef_PublicKeyOrToken, 1, &publicKey, &publicKeyLength))
            return E_FAIL;

        uint32_t majorVersion;
        if (1 != md_get_column_value_as_constant(sourceAssemblyRef, mdtAssemblyRef_MajorVersion, 1, &majorVersion))
            return E_FAIL;
        
        uint32_t minorVersion;
        if (1 != md_get_column_value_as_constant(sourceAssemblyRef, mdtAssemblyRef_MinorVersion, 1, &minorVersion))
            return E_FAIL;
        
        uint32_t buildNumber;
        if (1 != md_get_column_value_as_constant(sourceAssemblyRef, mdtAssemblyRef_BuildNumber, 1, &buildNumber))
            return E_FAIL;
        
        uint32_t revisionNumber;
        if (1 != md_get_column_value_as_constant(sourceAssemblyRef, mdtAssemblyRef_RevisionNumber, 1, &revisionNumber))
            return E_FAIL;
        
        char const* assemblyName;
        if (1 != md_get_column_value_as_utf8(sourceAssemblyRef, mdtAssemblyRef_Name, 1, &assemblyName))
            return E_FAIL;
        
        char const* assemblyCulture;
        if (1 != md_get_column_value_as_utf8(sourceAssemblyRef, mdtAssemblyRef_Culture, 1, &assemblyCulture))
            return E_FAIL;

        RETURN_IF_FAILED(FindAssemblyRef(
            targetModule,
            majorVersion,
            minorVersion,
            buildNumber,
            revisionNumber,
            flags,
            assemblyName,
            assemblyCulture,
            { publicKey, publicKeyLength },
            targetAssembly));

        if (hr == S_OK)
        {
            return S_OK;
        }

        md_added_row_t assemblyRef;
        if (!md_append_row(targetModule, mdtid_AssemblyRef, &assemblyRef))
            return E_FAIL;
        
        onRowAdded(assemblyRef);
  
        if (1 != md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_MajorVersion, 1, &majorVersion))
            return E_FAIL;

        if (1 != md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_MinorVersion, 1, &minorVersion))
            return E_FAIL;

        if (1 != md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_BuildNumber, 1, &buildNumber))
            return E_FAIL;

        if (md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_RevisionNumber, 1, &revisionNumber))
            return E_FAIL;

        if (1 != md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_Flags, 1, &flags))
            return E_FAIL;
        
        if (1 != md_set_column_value_as_utf8(assemblyRef, mdtAssemblyRef_Name, 1, &assemblyName))
            return E_FAIL;

        if (1 != md_set_column_value_as_utf8(assemblyRef, mdtAssemblyRef_Culture, 1, &assemblyCulture))
            return E_FAIL;

        if (1 != md_set_column_value_as_blob(assemblyRef, mdtAssemblyRef_PublicKeyOrToken, 1, &publicKey, &publicKeyLength))
            return E_FAIL;
        
        *targetAssembly = assemblyRef;
        return S_OK;
    }

    // Add a reference to sourceAssembly
    // in the AssemblyRef tables in targetModule and targetAssembly.
    // Returns the resulting cursor into targetModule's AssemblyRef table.
    HRESULT ImportReferenceToAssemblyRef(
        mdcursor_t sourceAssemblyRef,
        mdhandle_t targetModule,
        mdhandle_t targetAssembly,
        std::function<void(mdcursor_t)> onRowAdded,
        mdcursor_t* assemblyRefInTargetModule)
    {
        HRESULT hr;
        
        // Add a reference to the assembly in the target module.
        RETURN_IF_FAILED(ImportReferenceToAssemblyRef(sourceAssemblyRef, targetModule, onRowAdded, assemblyRefInTargetModule));

        // Also add a reference to the assembly in the target assembly.
        // In most cases, the target module will be the same as the target assembly, so this will be a no-op.
        // However, if the target module is a netmodule, then the target assembly will be the main assembly.
        // CoreCLR doesn't support multi-module assemblies, but they're still valid in ECMA-335.
        if (targetModule != targetAssembly)
        {
            mdcursor_t ignored;
            RETURN_IF_FAILED(ImportReferenceToAssemblyRef(sourceAssemblyRef, targetAssembly, onRowAdded, &ignored));
        }

        return S_OK;
    }

    HRESULT ImportReferenceToAssembly(
        mdcursor_t sourceAssembly,
        span<const uint8_t> sourceAssemblyHash,
        mdhandle_t targetModule,
        std::function<void(mdcursor_t)> onRowAdded,
        mdcursor_t* targetAssembly)
    {
        HRESULT hr;
        uint32_t flags;
        if (1 != md_get_column_value_as_constant(sourceAssembly, mdtAssembly_Flags, 1, &flags))
            return E_FAIL;

        uint8_t const* publicKey;
        uint32_t publicKeyLength;
        if (1 != md_get_column_value_as_blob(sourceAssembly, mdtAssembly_PublicKey, 1, &publicKey, &publicKeyLength))
            return E_FAIL;
        
        StrongNameToken publicKeyToken;
        if (publicKey != nullptr)
        {
            assert(IsAfPublicKey(flags));
            flags &= ~afPublicKey;
            RETURN_IF_FAILED(StrongNameTokenFromPublicKey({ publicKey, publicKeyLength }, publicKeyToken));
        }
        else
        {
            assert(!IsAfPublicKey(flags));
        }

        uint32_t majorVersion;
        if (1 != md_get_column_value_as_constant(sourceAssembly, mdtAssembly_MajorVersion, 1, &majorVersion))
            return E_FAIL;
        
        uint32_t minorVersion;
        if (1 != md_get_column_value_as_constant(sourceAssembly, mdtAssembly_MinorVersion, 1, &minorVersion))
            return E_FAIL;
        
        uint32_t buildNumber;
        if (1 != md_get_column_value_as_constant(sourceAssembly, mdtAssembly_BuildNumber, 1, &buildNumber))
            return E_FAIL;
        
        uint32_t revisionNumber;
        if (1 != md_get_column_value_as_constant(sourceAssembly, mdtAssembly_RevisionNumber, 1, &revisionNumber))
            return E_FAIL;
        
        char const* assemblyName;
        if (1 != md_get_column_value_as_utf8(sourceAssembly, mdtAssembly_Name, 1, &assemblyName))
            return E_FAIL;
        
        char const* assemblyCulture;
        if (1 != md_get_column_value_as_utf8(sourceAssembly, mdtAssembly_Culture, 1, &assemblyCulture))
            return E_FAIL;

        RETURN_IF_FAILED(FindAssemblyRef(
            targetModule,
            majorVersion,
            minorVersion,
            buildNumber,
            revisionNumber,
            flags,
            assemblyName,
            assemblyCulture,
            { publicKeyToken.data(), publicKeyToken.size() },
            targetAssembly));

        if (hr == S_OK)
        {
            return S_OK;
        }

        md_added_row_t assemblyRef;
        if (!md_append_row(targetModule, mdtid_AssemblyRef, &assemblyRef))
            return E_FAIL;
        
        onRowAdded(assemblyRef);
  
        if (1 != md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_MajorVersion, 1, &majorVersion))
            return E_FAIL;

        if (1 != md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_MinorVersion, 1, &minorVersion))
            return E_FAIL;

        if (1 != md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_BuildNumber, 1, &buildNumber))
            return E_FAIL;

        if (md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_RevisionNumber, 1, &revisionNumber))
            return E_FAIL;

        if (1 != md_set_column_value_as_constant(assemblyRef, mdtAssemblyRef_Flags, 1, &flags))
            return E_FAIL;
        
        if (1 != md_set_column_value_as_utf8(assemblyRef, mdtAssemblyRef_Name, 1, &assemblyName))
            return E_FAIL;

        if (1 != md_set_column_value_as_utf8(assemblyRef, mdtAssemblyRef_Culture, 1, &assemblyCulture))
            return E_FAIL;

        uint8_t const* hash = sourceAssemblyHash;
        uint32_t hashLength = (uint32_t)sourceAssemblyHash.size();
        if (1 != md_set_column_value_as_blob(assemblyRef, mdtAssemblyRef_HashValue, 1, &hash, &hashLength))
            return E_FAIL;

        uint8_t const* publicKeyTokenBlob = publicKeyToken.data();
        uint32_t publicKeyTokenLength = (uint32_t)publicKeyToken.size();
        if (1 != md_set_column_value_as_blob(assemblyRef, mdtAssemblyRef_PublicKeyOrToken, 1, &publicKeyTokenBlob, &publicKeyTokenLength))
            return E_FAIL;
        
        *targetAssembly = assemblyRef;
        return S_OK;
    }

    // Add a reference to sourceAssembly
    // in the AssemblyRef tables in targetModule and targetAssembly.
    // Returns the resulting cursor into targetModule's AssemblyRef table.
    HRESULT ImportReferenceToAssembly(
        mdhandle_t sourceAssembly,
        span<const uint8_t> sourceAssemblyHash,
        mdhandle_t targetModule,
        mdhandle_t targetAssembly,
        std::function<void(mdcursor_t)> onRowAdded,
        mdcursor_t* assemblyRefInTargetModule)
    {
        HRESULT hr;
        mdcursor_t importAssembly;
        if (!md_token_to_cursor(sourceAssembly, TokenFromRid(1, mdtAssembly), &importAssembly))
            return E_FAIL;
        
        // Add a reference to the assembly in the target module.
        RETURN_IF_FAILED(ImportReferenceToAssembly(importAssembly, sourceAssemblyHash, targetModule, onRowAdded, assemblyRefInTargetModule));

        // Also add a reference to the assembly in the target assembly.
        // In most cases, the target module will be the same as the target assembly, so this will be a no-op.
        // However, if the target module is a netmodule, then the target assembly will be the main assembly.
        // CoreCLR doesn't support multi-module assemblies, but they're still valid in ECMA-335.
        if (targetModule != targetAssembly)
        {
            mdcursor_t ignored;
            RETURN_IF_FAILED(ImportReferenceToAssembly(importAssembly, sourceAssemblyHash, targetAssembly, onRowAdded, &ignored));
        }

        return S_OK;
    }
}

HRESULT ImportReferenceToTypeDef(
    mdcursor_t sourceTypeDef,
    mdhandle_t sourceAssembly,
    span<const uint8_t> sourceAssemblyHash,
    mdhandle_t targetAssembly,
    mdhandle_t targetModule,
    bool alwaysImport,
    std::function<void(mdcursor_t)> onRowAdded,
    mdcursor_t* targetTypeDef)
{
    HRESULT hr;
    mdhandle_t sourceModule = md_extract_handle_from_cursor(sourceTypeDef);

    mdguid_t targetModuleMvid = {};
    mdguid_t targetAssemblyMvid = {};
    mdguid_t sourceAssemblyMvid = {};
    mdguid_t sourceModuleMvid = {};
    RETURN_IF_FAILED(GetMvid(targetModule, &targetModuleMvid));
    RETURN_IF_FAILED(GetMvid(targetAssembly, &targetAssemblyMvid));
    RETURN_IF_FAILED(GetMvid(sourceModule, &sourceModuleMvid));
    RETURN_IF_FAILED(GetMvid(sourceAssembly, &sourceAssemblyMvid));

    bool sameModuleMvid = std::memcmp(&targetModuleMvid, &sourceModuleMvid, sizeof(mdguid_t)) == 0;
    bool sameAssemblyMvid = std::memcmp(&targetAssemblyMvid, &sourceAssemblyMvid, sizeof(mdguid_t)) == 0;

    mdcursor_t resolutionScope;
    if (sameAssemblyMvid && sameModuleMvid)
    {
        if (!alwaysImport)
        {
            // If we don't need to always import the TypeDef,
            // we can resolve it to an existing TypeDef.
            mdToken token;
            if (!md_cursor_to_token(sourceTypeDef, &token))
                return E_FAIL;

            // All images with the same MVID should have the same metadata tables.
            if (!md_token_to_cursor(targetModule, token, targetTypeDef))
                return CLDB_E_FILE_CORRUPT;
            
            return S_OK;
        }
        uint32_t count;
        if (!md_create_cursor(targetModule, mdtid_Module, &resolutionScope, &count))
            return E_FAIL;
    }
    else if (sameAssemblyMvid && !sameModuleMvid)
    {
        char const* importName;
        mdcursor_t importModule;
        uint32_t count;
        if (!md_create_cursor(sourceModule, mdtid_Module, &importModule, &count)
            || 1 != md_get_column_value_as_utf8(importModule, mdtModule_Name, 1, &importName))
        {
            return E_FAIL;
        }

        md_added_row_t moduleRef;
        if (!md_append_row(targetModule, mdtid_ModuleRef, &moduleRef)
            || 1 != md_set_column_value_as_utf8(moduleRef, mdtModuleRef_Name, 1, &importName))
        {
            return E_FAIL;
        }

        resolutionScope = moduleRef;
        onRowAdded(moduleRef);
    }
    else if (sameModuleMvid)
    {
        // The import can't be the same module and different assemblies.
        // COMPAT-BREAK: CoreCLR allows this for cases where there is no source assembly open, with a TODO from FX-era
        // relating to using a sample compiler from the .NET Framework SDK from before VS6.0.
        // This tool never shipped, so we don't need to account for this bug here.
        return E_INVALIDARG;
    }
    else
    {
        RETURN_IF_FAILED(ImportReferenceToAssembly(sourceAssembly, sourceAssemblyHash, targetModule, targetAssembly, onRowAdded, &resolutionScope));
    }

    try
    {
        std::stack<mdcursor_t> typesForTypeRefs;

        mdcursor_t importType;
        if (!md_token_to_cursor(sourceModule, tdImport, &importType))
            return CLDB_E_FILE_CORRUPT;
        
        typesForTypeRefs.push(importType);
        
        mdcursor_t nestedClasses;
        uint32_t nestedClassCount;
        if (!md_create_cursor(sourceModule, mdtid_NestedClass, &nestedClasses, &nestedClassCount))
            return E_FAIL;
        
        mdToken nestedTypeToken = tdImport;
        mdcursor_t nestedClass;
        while (md_find_row_from_cursor(nestedClasses, mdtNestedClass_NestedClass, RidFromToken(nestedTypeToken), &nestedClass))
        {
            mdcursor_t enclosingClass;
            if (1 != md_get_column_value_as_cursor(nestedClass, mdtNestedClass_EnclosingClass, 1, &enclosingClass))
                return E_FAIL;
            
            typesForTypeRefs.push(enclosingClass);
            if (!md_cursor_to_token(enclosingClass, &nestedTypeToken))
                return E_FAIL;
        }

        for (; !typesForTypeRefs.empty(); typesForTypeRefs.pop())
        {
            mdcursor_t typeDef = typesForTypeRefs.top();
            md_added_row_t typeRef;
            if (!md_append_row(targetModule, mdtid_TypeRef, &typeRef))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_cursor(typeRef, mdtTypeRef_ResolutionScope, 1, &resolutionScope))
                return E_FAIL;
            
            char const* typeName;
            if (1 != md_get_column_value_as_utf8(typeDef, mdtTypeDef_TypeName, 1, &typeName)
                || 1 != md_set_column_value_as_utf8(typeRef, mdtTypeRef_TypeName, 1, &typeName))
            {
                return E_FAIL;
            }
            
            char const* typeNamespace;
            if (1 != md_get_column_value_as_utf8(typeDef, mdtTypeDef_TypeNamespace, 1, &typeNamespace)
                || 1 != md_set_column_value_as_utf8(typeRef, mdtTypeRef_TypeNamespace, 1, &typeNamespace))
            {
                return E_FAIL;
            }

            resolutionScope = typeRef;
            onRowAdded(typeRef);
        }

        *targetTypeDef = resolutionScope;
    }
    catch (std::bad_alloc const&)
    {
        return E_OUTOFMEMORY;
    }

    return S_OK;
}

namespace
{
    bool FindModuleRef(mdhandle_t image, char const* moduleName, mdcursor_t* existingModuleRef)
    {
        mdcursor_t c;
        uint32_t count;
        if (!md_create_cursor(image, mdtid_ModuleRef, &c, &count))
            return false;
        
        for (uint32_t i = 0; i < count; i++, md_cursor_next(&c))
        {
            char const* name;
            if (1 != md_get_column_value_as_utf8(c, mdtModuleRef_Name, 1, &name))
                return false;
            
            if (std::strcmp(name, moduleName) == 0)
            {
                *existingModuleRef = c;
                return true;
            }
        }

        return false;
    }

    // Given a type name and type namespace for a type in a (possibly multi-module) assembly,
    // import a reference to the type into the target module.
    // This function also handles type forwards into the target assembly.
    HRESULT ImportScopeForTypeByNameInAssembly(
        char const* typeName,
        char const* typeNamespace,
        mdhandle_t module,
        mdhandle_t assembly,
        std::function<void(mdcursor_t)> onRowAdded,
        mdcursor_t* importedScope
    )
    {
        // Search the ExportedType table in the targetAssembly for a type with the given name or namespace.
        // An empty ExportedType table is okay.
        mdcursor_t exportedType;
        uint32_t count;
        bool foundExportedType = false;
        if (md_create_cursor(assembly, mdtid_ExportedType, &exportedType, &count))
        {
            for (uint32_t i = 0; i < count; ++i, md_cursor_next(&exportedType))
            {
                char const* exportedTypeName;
                if (1 != md_get_column_value_as_utf8(exportedType, mdtExportedType_TypeName, 1, &exportedTypeName))
                    return E_FAIL;
                
                char const* exportedTypeNamespace;
                if (1 != md_get_column_value_as_utf8(exportedType, mdtExportedType_TypeNamespace, 1, &exportedTypeNamespace))
                    return E_FAIL;
                
                if (std::strcmp(typeName, exportedTypeName) == 0 && std::strcmp(typeNamespace, exportedTypeNamespace) == 0)
                {
                    foundExportedType = true;
                    break;
                }
            }
        }

        if (foundExportedType)
        {
            // If we found an ExportedType, then the type is defined in another module or is forwarded to another assembly.
            // We need to find the imported scope for the type.
            mdcursor_t implementation;
            if (1 != md_get_column_value_as_cursor(exportedType, mdtExportedType_Implementation, 1, &implementation))
                return E_FAIL;
            
            switch (GetTokenTypeFromCursor(implementation))
            {
                // If the ExportedType.Implementation is a File:
                // - If the File refers to module's module, then we can use the module cursor in module as the imported scope. 
                // - If the File refers to another module, then we'll create a ModuleRef to that module.
                case mdtFile:
                {
                    char const* fileName;
                    if (1 != md_get_column_value_as_utf8(implementation, mdtFile_Name, 1, &fileName))
                        return E_FAIL;
                    
                    mdcursor_t moduleCursor;
                    if (!md_token_to_cursor(module, TokenFromRid(1, mdtModule), &moduleCursor))
                        return E_FAIL;

                    char const* moduleName;
                    if (1 != md_get_column_value_as_utf8(moduleCursor, mdtModule_Name, 1, &moduleName))
                        return E_FAIL;
                    
                    if (std::strcmp(fileName, moduleName) == 0)
                    {
                        *importedScope = moduleCursor;
                    }
                    else
                    {
                        if (!FindModuleRef(module, fileName, importedScope))
                        {
                            md_added_row_t moduleRef;
                            if (!md_append_row(module, mdtid_ModuleRef, &moduleRef))
                                return E_FAIL;
                            
                            if (1 != md_set_column_value_as_utf8(moduleRef, mdtModuleRef_Name, 1, &fileName))
                                return E_FAIL;
                            
                            *importedScope = moduleRef;
                            onRowAdded(moduleRef);
                        }
                    }
                    return S_OK;
                }
                // If the ExportedType.Implementation is an AssemblyRef, then we'll use that as the imported scope.
                // COMPAT-BREAK: CoreCLR does not support this case (it assumes that this ExportedType entry is never a type forwarder).
                case mdtAssemblyRef:
                    return ImportReferenceToAssemblyRef(implementation, module, assembly, onRowAdded, importedScope);

                // If the ExportedType.Implementation is an ExportedType, then we're in an error scenario.
                case mdtExportedType:
                    return E_FAIL;
                default:
                    assert(false);
                    return E_FAIL;
            }
        }

        // If we couldn't find an ExportedType, then we need to search the TypeDef table in the assembly.
        // We must be able to find the type here, otherwise the metadata is invalid as we can't make a reference to a type we can't find.
        mdcursor_t typeDef;
        if (!md_create_cursor(assembly, mdtid_TypeDef, &typeDef, &count))
            return E_FAIL;
        
        for (uint32_t i = 0; i < count; ++i, md_cursor_next(&typeDef))
        {
            char const* typeDefName;
            if (1 != md_get_column_value_as_utf8(typeDef, mdtTypeDef_TypeName, 1, &typeDefName))
                return E_FAIL;
            
            char const* typeDefNamespace;
            if (1 != md_get_column_value_as_utf8(typeDef, mdtTypeDef_TypeNamespace, 1, &typeDefNamespace))
                return E_FAIL;

            if (std::strcmp(typeName, typeDefName) == 0 && std::strcmp(typeNamespace, typeDefNamespace) == 0)
            {
                // Make sure that this type is not nested.
                // For this to be the same type, it must not be a nested type.
                mdcursor_t nestedType;
                uint32_t nestedTypeCount;
                if (!md_create_cursor(assembly, mdtid_NestedClass, &nestedType, &nestedTypeCount))
                    return E_FAIL;
                
                mdToken typeDefToken;
                if (!md_cursor_to_token(typeDef, &typeDefToken))
                    return CLDB_E_FILE_CORRUPT;

                if (md_find_row_from_cursor(nestedType, mdtNestedClass_NestedClass, RidFromToken(typeDefToken), &nestedType))
                    return E_FAIL;

                // If we found the type defined in the assembly, then the correct imported scope is the assembly module.
                // COMPAT-BREAK: CLR and CoreCLR always use the module token as the ResolutionScope here,
                // which is invalid as the type lives in the Assembly manifest module, which may not be the current module.
                // When the assembly module is the manifest module, it ends up being correct,
                // but when the assembly manifest module is a different module, the TypeRef will not resolve.
                mdcursor_t assemblyModule;
                if (!md_token_to_cursor(assembly, TokenFromRid(1, mdtModule), &assemblyModule))
                    return CLDB_E_FILE_CORRUPT;
                
                char const* assemblyModuleName;
                if (1 != md_get_column_value_as_utf8(assemblyModule, mdtModule_Name, 1, &assemblyModuleName))
                    return E_FAIL;
                
                mdcursor_t moduleCursor;
                if (!md_token_to_cursor(module, TokenFromRid(1, mdtModule), &moduleCursor))
                    return CLDB_E_FILE_CORRUPT;
                
                char const* moduleName;
                if (1 != md_get_column_value_as_utf8(moduleCursor, mdtModule_Name, 1, &moduleName))
                    return E_FAIL;
                
                if (std::strcmp(assemblyModuleName, moduleName) == 0)
                {
                    // If the assembly module has the same name as the current module,
                    // assume that the assembly manifest module is the same module as the current module.
                    *importedScope = moduleCursor;
                }
                else if (!FindModuleRef(module, assemblyModuleName, importedScope))
                {
                    md_added_row_t moduleRef;
                    if (!md_append_row(module, mdtid_ModuleRef, &moduleRef))
                        return E_FAIL;
                    
                    if (1 != md_set_column_value_as_utf8(moduleRef, mdtModuleRef_Name, 1, &assemblyModuleName))
                        return E_FAIL;
                    
                    *importedScope = moduleRef;
                    onRowAdded(moduleRef);
                }
                return S_OK;
            }
        }
        return CLDB_E_RECORD_NOTFOUND;
    }

    HRESULT AssemblyRefPointsToAssembly(
        mdcursor_t assemblyRef,
        mdcursor_t assembly)
    {
        HRESULT hr;
        // Compare version, Name, Locale, and PublicKeyOrToken (possibly creating token from the assembly's key if needed)
        uint32_t refMajorVersion;
        if (1 != md_get_column_value_as_constant(assemblyRef, mdtAssemblyRef_MajorVersion, 1, &refMajorVersion))
            return CLDB_E_FILE_CORRUPT;
        
        uint32_t majorVersion;
        if (1 != md_get_column_value_as_constant(assembly, mdtAssembly_MajorVersion, 1, &majorVersion))
            return CLDB_E_FILE_CORRUPT;
        
        if (refMajorVersion != majorVersion)
            return S_FALSE;

        uint32_t refMinorVersion;
        if (1 != md_get_column_value_as_constant(assemblyRef, mdtAssemblyRef_MinorVersion, 1, &refMinorVersion))
            return CLDB_E_FILE_CORRUPT;
        
        uint32_t minorVersion;
        if (1 != md_get_column_value_as_constant(assembly, mdtAssembly_MinorVersion, 1, &minorVersion))
            return CLDB_E_FILE_CORRUPT;
        
        if (refMinorVersion != minorVersion)
            return S_FALSE;
        
        uint32_t refBuildNumber;
        if (1 != md_get_column_value_as_constant(assemblyRef, mdtAssemblyRef_BuildNumber, 1, &refBuildNumber))
            return CLDB_E_FILE_CORRUPT;
        
        uint32_t buildNumber;
        if (1 != md_get_column_value_as_constant(assembly, mdtAssembly_BuildNumber, 1, &buildNumber))
            return CLDB_E_FILE_CORRUPT;
        
        if (refBuildNumber != buildNumber)
            return S_FALSE;

        uint32_t refRevisionNumber;
        if (1 != md_get_column_value_as_constant(assemblyRef, mdtAssemblyRef_RevisionNumber, 1, &refRevisionNumber))
            return CLDB_E_FILE_CORRUPT;
        
        uint32_t revisionNumber;
        if (1 != md_get_column_value_as_constant(assembly, mdtAssembly_RevisionNumber, 1, &revisionNumber))
            return CLDB_E_FILE_CORRUPT;
        
        if (refRevisionNumber != revisionNumber)
            return S_FALSE;
        
        char const* refName;
        if (1 != md_get_column_value_as_utf8(assemblyRef, mdtAssemblyRef_Name, 1, &refName))
            return CLDB_E_FILE_CORRUPT;
        
        char const* name;
        if (1 != md_get_column_value_as_utf8(assembly, mdtAssembly_Name, 1, &name))
            return CLDB_E_FILE_CORRUPT;
        
        if (std::strcmp(refName, name) != 0)
            return S_FALSE;
        
        char const* refCulture;
        if (1 != md_get_column_value_as_utf8(assemblyRef, mdtAssemblyRef_Culture, 1, &refCulture))
            return CLDB_E_FILE_CORRUPT;
        
        char const* culture;
        if (1 != md_get_column_value_as_utf8(assembly, mdtAssembly_Culture, 1, &culture))
            return CLDB_E_FILE_CORRUPT;
        
        if (std::strcmp(refCulture, culture) != 0)
            return S_FALSE;
        
        uint8_t const* refPublicKeyOrToken;
        uint32_t refPublicKeyOrTokenLength;
        if (1 != md_get_column_value_as_blob(assemblyRef, mdtAssemblyRef_PublicKeyOrToken, 1, &refPublicKeyOrToken, &refPublicKeyOrTokenLength))
            return CLDB_E_FILE_CORRUPT;
        
        uint8_t const* publicKey;
        uint32_t publicKeyLength;
        if (1 != md_get_column_value_as_blob(assembly, mdtAssembly_PublicKey, 1, &publicKey, &publicKeyLength))
            return CLDB_E_FILE_CORRUPT;
        
        if ((refPublicKeyOrTokenLength == 0) != (publicKeyLength == 0))
            return S_FALSE;
        
        if (refPublicKeyOrTokenLength != 0)
        {
            uint32_t refFlags;
            if (1 != md_get_column_value_as_constant(assemblyRef, mdtAssemblyRef_Flags, 1, &refFlags))
                return CLDB_E_FILE_CORRUPT;
            
            if (IsAfPublicKey(refFlags))
            {
                // If we have a full public key for the reference, then we can compare the full public key.
                if (refPublicKeyOrTokenLength != publicKeyLength || std::memcmp(refPublicKeyOrToken, publicKey, publicKeyLength) != 0)
                    return S_FALSE;
                
                return S_OK;
            }

            StrongNameToken asmPublicKeyToken;
            RETURN_IF_FAILED(StrongNameTokenFromPublicKey({ publicKey, publicKeyLength }, asmPublicKeyToken));

            if (refPublicKeyOrTokenLength != asmPublicKeyToken.size() || !std::equal(asmPublicKeyToken.begin(), asmPublicKeyToken.end(), refPublicKeyOrToken))
                return S_FALSE;
            
            return S_OK;
        }
        return S_OK;
    }

    HRESULT ImportReferenceToTypeRef(
        mdcursor_t sourceTypeRef,
        mdhandle_t sourceAssembly,
        span<const uint8_t> sourceAssemblyHash,
        mdhandle_t targetAssembly,
        mdhandle_t targetModule,
        std::function<void(mdcursor_t)> onRowAdded,
        mdcursor_t* targetTypeRef)
    {
        assert(sourceAssembly != nullptr && targetAssembly != nullptr && targetModule != nullptr);

        HRESULT hr;
        std::stack<mdcursor_t> typesForTypeRefs;
        typesForTypeRefs.push(sourceTypeRef);
        
        mdcursor_t scope = sourceTypeRef;
        while (GetTokenTypeFromCursor(scope) == mdtTypeRef)
        {
            mdcursor_t resolutionScope;
            if (1 != md_get_column_value_as_cursor(scope, mdtTypeRef_ResolutionScope, 1, &resolutionScope))
                return E_FAIL;
            
            typesForTypeRefs.push(resolutionScope);
            scope = resolutionScope;
        }
        
        mdhandle_t sourceModule = md_extract_handle_from_cursor(sourceTypeRef);
        mdguid_t targetModuleMvid = {};
        mdguid_t targetAssemblyMvid = {};
        mdguid_t sourceAssemblyMvid = {};
        mdguid_t sourceModuleMvid = {};
        RETURN_IF_FAILED(GetMvid(targetModule, &targetModuleMvid));
        RETURN_IF_FAILED(GetMvid(targetAssembly, &targetAssemblyMvid));
        RETURN_IF_FAILED(GetMvid(sourceModule, &sourceModuleMvid));
        RETURN_IF_FAILED(GetMvid(sourceAssembly, &sourceAssemblyMvid));

        bool sameModuleMvid = std::memcmp(&targetModuleMvid, &sourceModuleMvid, sizeof(mdguid_t)) == 0;
        bool sameAssemblyMvid = std::memcmp(&targetAssemblyMvid, &sourceAssemblyMvid, sizeof(mdguid_t)) == 0;

        // II.22.38 1. Valid ResolutionScope values
        // - null
        // - TypeRef token
        // - ModuleRef token
        // - Module token
        // - AssemblyRef token
        mdcursor_t targetOutermostScope = {};
        if (sameAssemblyMvid && sameModuleMvid)
        {
            mdToken token;
            if (!md_cursor_to_token(sourceTypeRef, &token))
                return E_FAIL;
            
            if (!md_token_to_cursor(targetModule, token, targetTypeRef))
                return CLDB_E_FILE_CORRUPT;
            
            return S_OK;
        }
        else if (sameAssemblyMvid && !sameModuleMvid)
        {
            mdToken scopeToken;
            if (!md_cursor_to_token(scope, &scopeToken))
                return E_FAIL;

            if (IsNilToken(scopeToken))
            {
                // A Nil ResolutionScope means a reference to an ExportedType entry
                // in the assembly.
                // Since the source and target assemblies have the same identity,
                // we can use the Nil token and we don't have to resolve the ExportedType
                // as the target and source assemblies are the same.
                targetOutermostScope = {};
            }
            else if (TypeFromToken(scopeToken) == mdtModule)
            {
                // Create a ModuleRef from the target module to the source module.
                char const* moduleName;
                if (1 != md_get_column_value_as_utf8(scope, mdtModule_Name, 1, &moduleName))
                    return CLDB_E_FILE_CORRUPT;

                if (!FindModuleRef(targetModule, moduleName, &targetOutermostScope))
                {
                    md_added_row_t moduleRef;
                    if (!md_append_row(targetModule, mdtid_ModuleRef, &moduleRef))
                        return E_FAIL;
                    
                    if (1 != md_set_column_value_as_utf8(moduleRef, mdtModuleRef_Name, 1, &moduleName))
                        return E_FAIL;
                    
                    targetOutermostScope = moduleRef;
                    onRowAdded(moduleRef);
                }
            }
            else if (TypeFromToken(scopeToken) == mdtModuleRef)
            {
                // If this ModuleRef points from the source module into the target module,
                // then we can use the Module token as the outermost scope.
                // otherwise, create ModuleRef to the module that the source ModuleRef points to.
                char const* moduleName;
                if (1 != md_get_column_value_as_utf8(scope, mdtModuleRef_Name, 1, &moduleName))
                    return CLDB_E_FILE_CORRUPT;
                
                mdcursor_t targetModuleCursor;
                uint32_t count;
                if (!md_create_cursor(targetModule, mdtid_Module, &targetModuleCursor, &count))
                    return E_FAIL;
                
                char const* targetModuleName;
                if (1 != md_get_column_value_as_utf8(targetModuleCursor, mdtModule_Name, 1, &targetModuleName))
                    return E_FAIL;
                
                if (std::strcmp(moduleName, targetModuleName) == 0)
                {
                    targetOutermostScope = targetModuleCursor;
                }
                else if (!FindModuleRef(targetModule, moduleName, &targetOutermostScope))
                {
                    md_added_row_t moduleRef;
                    if (!md_append_row(targetModule, mdtid_ModuleRef, &moduleRef))
                        return E_FAIL;
                    
                    if (1 != md_set_column_value_as_utf8(moduleRef, mdtModuleRef_Name, 1, &moduleName))
                        return E_FAIL;
                    
                    targetOutermostScope = moduleRef;
                    onRowAdded(moduleRef);
                }
            }
            else if (TypeFromToken(scopeToken) == mdtAssemblyRef)
            {
                // Copy the AssemblyRef from the source module to the target module.
                RETURN_IF_FAILED(ImportReferenceToAssemblyRef(scope, targetModule, targetAssembly, onRowAdded, &targetOutermostScope));
            }
            else
            {
                return E_INVALIDARG;
            }
        }
        else if (!sameAssemblyMvid)
        {
            assert(!sameModuleMvid);

            mdToken scopeToken;
            if (!md_cursor_to_token(scope, &scopeToken))
                return E_FAIL;
            
            if (IsNilToken(scopeToken))
            {
                // Lookup ExportedType entry in the source assembly for this type.
                mdcursor_t exportedType;
                uint32_t count;
                bool foundExportedType = false;
                mdcursor_t implementation = {};
                if (md_create_cursor(sourceAssembly, mdtid_ExportedType, &exportedType, &count))
                {
                    mdcursor_t outermostTypeRef = typesForTypeRefs.top();
                    char const* typeName;
                    if (1 != md_get_column_value_as_utf8(outermostTypeRef, mdtTypeRef_TypeName, 1, &typeName))
                        return E_FAIL;
                    
                    char const* typeNamespace;
                    if (1 != md_get_column_value_as_utf8(outermostTypeRef, mdtTypeRef_TypeNamespace, 1, &typeNamespace))
                        return E_FAIL;
                    
                    // If we can't find an ExportedType entry for this type, we'll just move over the TypeRef with a Nil ResolutionScope.
                    for (uint32_t i = 0; i < count; ++i, md_cursor_next(&exportedType))
                    {
                        char const* exportedTypeName;
                        if (1 != md_get_column_value_as_utf8(exportedType, mdtExportedType_TypeName, 1, &exportedTypeName))
                            return E_FAIL;
                        
                        char const* exportedTypeNamespace;
                        if (1 != md_get_column_value_as_utf8(exportedType, mdtExportedType_TypeNamespace, 1, &exportedTypeNamespace))
                            return E_FAIL;
                        
                        if (std::strcmp(typeName, exportedTypeName) == 0 && std::strcmp(typeNamespace, exportedTypeNamespace) == 0)
                        {
                            if (1 != md_get_column_value_as_cursor(exportedType, mdtExportedType_Implementation, 1, &implementation))
                                return E_FAIL;
                            
                            foundExportedType = true;
                            break;
                        }
                    }
                }

                if (foundExportedType)
                {
                    switch (GetTokenTypeFromCursor(implementation))
                    {
                        case mdtFile:
                        {
                            // This type is from a file in the source assembly, so we need to create an AssemblyRef to the source assembly.
                            RETURN_IF_FAILED(ImportReferenceToAssembly(sourceAssembly, sourceAssemblyHash, targetModule, targetAssembly, onRowAdded, &targetOutermostScope));
                        }
                        case mdtAssemblyRef:
                        {
                            // This type is a type-forward from another assembly.
                            // Reset the scope and scopeToken variables to this AssemblyRef.
                            // If this is a type forward from the target assembly, we want to resolve it to the target assembly to avoid a self-referential
                            // AssemblyRef.
                            scope = implementation;
                            if (!md_cursor_to_token(scope, &scopeToken))
                                return CLDB_E_FILE_CORRUPT;
                            break;
                        }
                        case mdtExportedType:
                        {
                            assert(false && "We should be looking at the outermost type already. Therefore, the ExportedType entry for this type should not be enclosed in another type.");
                            return E_FAIL;
                        }
                        default:
                        {
                            assert(false && "Unexpected token type for ExportedType.Implementation");
                            return E_FAIL;
                        }
                    }
                }
                else
                {
                    // COMPAT-BREAK: CoreCLR and CLR treat a type that is not found in the ExportedType table as though it is an imported type from the target assembly.
                    // This is incorrect per the spec as the type is not defined in the target assembly.
                    // Early .NET compilers wouldn't always have an AssemblyRef to the core library (mscorlib), so we could end up in a situation where we'd be importing
                    // a TypeRef to mscorlib from a module that doesn't have a reference to mscorlib.
                    // In this case, this branch would treat the ResolutionScope as the Nil token.
                    // Nowadays, all of the managed code compilers correctly emit references to all types, including the core library (which in many cases now is not mscorlib).
                    // Additionally, multimodule assemblies aren't supported by CoreCLR, so we won't even reach this branch anyway (the whole IsNilToken(scopeToken) branch will only happen in multimodule scenarios).

                    // If we can't find the type in the source assembly, then we can't import it as we can't find the definition anywhere.
                    mdcursor_t sourceAssemblyTypeDef;
                    uint32_t sourceAssemblyTypeDefCount;
                    if (!md_create_cursor(sourceAssembly, mdtid_TypeDef, &sourceAssemblyTypeDef, &sourceAssemblyTypeDefCount))
                        return E_FAIL;
                    
                    mdcursor_t outermostTypeRef = typesForTypeRefs.top();
                    char const* typeName;
                    if (1 != md_get_column_value_as_utf8(outermostTypeRef, mdtTypeRef_TypeName, 1, &typeName))
                        return E_FAIL;
                    
                    char const* typeNamespace;
                    if (1 != md_get_column_value_as_utf8(outermostTypeRef, mdtTypeRef_TypeNamespace, 1, &typeNamespace))
                        return E_FAIL;

                    bool found = false;
                    for (uint32_t i = 0; i < sourceAssemblyTypeDefCount; ++i, md_cursor_next(&sourceAssemblyTypeDef))
                    {
                        char const* sourceAssemblyTypeDefName;
                        if (1 != md_get_column_value_as_utf8(sourceAssemblyTypeDef, mdtTypeDef_TypeName, 1, &sourceAssemblyTypeDefName))
                            return E_FAIL;
                        
                        char const* sourceAssemblyTypeDefNamespace;
                        if (1 != md_get_column_value_as_utf8(sourceAssemblyTypeDef, mdtTypeDef_TypeNamespace, 1, &sourceAssemblyTypeDefNamespace))
                            return E_FAIL;
                        
                        if (std::strcmp(typeName, sourceAssemblyTypeDefName) != 0 && std::strcmp(typeNamespace, sourceAssemblyTypeDefNamespace) != 0)
                            continue;
                        
                        mdcursor_t sourceAssemblyTypeDefEnclosingClass;
                        uint32_t ignored;
                        if (md_create_cursor(sourceAssembly, mdtid_NestedClass, &sourceAssemblyTypeDefEnclosingClass, &ignored)
                            && md_find_row_from_cursor(sourceAssemblyTypeDefEnclosingClass, mdtNestedClass_NestedClass, TokenFromRid(i + 1, mdtTypeDef), &sourceAssemblyTypeDefEnclosingClass))
                        {
                            // If the type is nested, then it can't be the right type as we're already at the outermost scope.
                            continue;
                        }
                        
                        // If we found the type defined in the source assembly, then the correct imported scope is the assembly module.
                        mdcursor_t importAssembly;
                        if (!md_token_to_cursor(sourceAssembly, TokenFromRid(1, mdtAssembly), &importAssembly))
                            return E_FAIL;
                        
                        // Add a reference to the assembly in the target module and assembly.
                        RETURN_IF_FAILED(ImportReferenceToAssembly(sourceAssembly, sourceAssemblyHash, targetModule, targetAssembly, onRowAdded, &targetOutermostScope));
                        found = true;
                        break;
                    }

                    if (!found)
                        return CLDB_E_RECORD_NOTFOUND;
                }
            }
            else if (TypeFromToken(scopeToken) == mdtModule)
            {
                // Create an AssemblyRef from the destination assembly to the source assembly.
                RETURN_IF_FAILED(ImportReferenceToAssembly(sourceAssembly, sourceAssemblyHash, targetModule, targetAssembly, onRowAdded, &targetOutermostScope));
            }
            
            // The IsNilToken case can resolve to an ExportedType entry whose scope is an AssemblyRef.
            // We want to catch that case here, so we split this out to a separate if instead of a chained else if.
            if (TypeFromToken(scopeToken) == mdtAssemblyRef)
            {
                // Convert from AssemblyRef to Assembly if the source AssemblyRef points to the target assembly.
                mdcursor_t targetAssemblyCursor;
                uint32_t count;
                if (!md_create_cursor(targetModule, mdtid_Assembly, &targetAssemblyCursor, &count))
                    return E_FAIL;

                RETURN_IF_FAILED(AssemblyRefPointsToAssembly(scope, targetAssemblyCursor));
                if (hr == S_OK)
                {
                    // The type is defined in the target assembly, so we need to correctly define its scope.
                    mdcursor_t outermostTypeRef = typesForTypeRefs.top();
                    char const* typeName;
                    if (1 != md_get_column_value_as_utf8(outermostTypeRef, mdtTypeRef_TypeName, 1, &typeName))
                        return E_FAIL;
                    
                    char const* typeNamespace;
                    if (1 != md_get_column_value_as_utf8(outermostTypeRef, mdtTypeRef_TypeNamespace, 1, &typeNamespace))
                        return E_FAIL;

                    RETURN_IF_FAILED(ImportScopeForTypeByNameInAssembly(
                        typeName,
                        typeNamespace,
                        targetModule,
                        targetAssembly,
                        onRowAdded,
                        &targetOutermostScope));
                }
                else
                {
                    // The type is defined in another assembly. We need to create an AssemblyRef to that assembly.
                    assert(hr == S_FALSE);
                    RETURN_IF_FAILED(ImportReferenceToAssemblyRef(scope, targetModule, targetAssembly, onRowAdded, &targetOutermostScope));
                }
            }
            else if (TypeFromToken(scopeToken) == mdtModuleRef)
            {
                // In this case, the type is from the source assembly, but a different module than the source module.
                // Since the source assembly and target assembly are different, we can't make a module reference to the type's module
                // as module references are only within assembly boundaries.
                // Make an AssemblyRef to the source assembly from the target assembly.
                RETURN_IF_FAILED(ImportReferenceToAssembly(sourceAssembly, sourceAssemblyHash, targetModule, targetAssembly, onRowAdded, &targetOutermostScope));
            }
            else
            {
                return E_FAIL;
            }
        }

        assert(md_extract_handle_from_cursor(targetOutermostScope) == targetModule);

        mdToken targetOutermostScopeToken;
        if (!md_cursor_to_token(targetOutermostScope, &targetOutermostScopeToken))
            return E_FAIL;
        
        if (TypeFromToken(targetOutermostScopeToken) == mdtModule && !IsNilToken(targetOutermostScopeToken))
        {
            // Find a nested TypeDef in the target module that matches the name and namespace of the source TypeRef.
            // We've resolved the TypeRef's outermost scope to be in the target module,
            // so the TypeDef must be in the target module.
            mdcursor_t enclosingScope = targetOutermostScope;

            mdcursor_t targetTypeDef;
            uint32_t targetTypeDefCount;
            if (!md_create_cursor(targetModule, mdtid_TypeDef, &targetTypeDef, &targetTypeDefCount))
                return E_FAIL;
            for (; !typesForTypeRefs.empty(); typesForTypeRefs.pop())
            {
                mdcursor_t sourceEnclosingTypeRef = typesForTypeRefs.top();

                char const* typeName;
                if (1 != md_get_column_value_as_utf8(sourceEnclosingTypeRef, mdtTypeRef_TypeName, 1, &typeName))
                    return E_FAIL;
                
                char const* typeNamespace;
                if (1 != md_get_column_value_as_utf8(sourceEnclosingTypeRef, mdtTypeRef_TypeNamespace, 1, &typeNamespace))
                    return E_FAIL;
                
                mdToken enclosingScopeToken;
                if (!md_cursor_to_token(enclosingScope, &enclosingScopeToken))
                    return E_FAIL;
                
                bool shouldHaveEnclosingType = !IsNilToken(enclosingScopeToken) && TypeFromToken(enclosingScopeToken) == mdtTypeDef;
                // The TypeDef table must be sorted such that enclosing types are defined before nesting types.
                // Therefore, we can search the table linearly.
                // See the commentary in II.22 before II.22.1 for more information.
                bool found = false;
                do
                {
                    char const* targetTypeName;
                    if (1 != md_get_column_value_as_utf8(targetTypeDef, mdtTypeDef_TypeName, 1, &targetTypeName))
                        return E_FAIL;
                    
                    char const* targetTypeNamespace;
                    if (1 != md_get_column_value_as_utf8(targetTypeDef, mdtTypeDef_TypeNamespace, 1, &targetTypeNamespace))
                        return E_FAIL;
                    
                    // Check the name of the type.
                    if (std::strcmp(typeName, targetTypeName) != 0 || std::strcmp(typeNamespace, targetTypeNamespace) != 0)
                        continue;

                    // Now that we've validated that the target TypeDef has an enclosing type,
                    // we need to validate that the enclosing type matches the source TypeRef's enclosing type.
                    mdToken targetTypeDefToken;
                    if (!md_cursor_to_token(targetTypeDef, &targetTypeDefToken))
                        return E_FAIL;

                    mdcursor_t targetNestedClass;
                    uint32_t targetNestedClassCount;
                    if (shouldHaveEnclosingType != 
                        (md_create_cursor(targetModule, mdtid_NestedClass, &targetNestedClass, &targetNestedClassCount)
                            && md_find_row_from_cursor(targetNestedClass, mdtNestedClass_NestedClass, RidFromToken(targetTypeDefToken), &targetNestedClass)))
                    {
                        // If the source TypeRef has an enclosing type, then the target TypeDef must have an enclosing type and vice versa.
                        continue;
                    }

                    if (shouldHaveEnclosingType)
                    {
                        mdToken targetEnclosingType;
                        if (1 != md_get_column_value_as_token(targetNestedClass, mdtNestedClass_EnclosingClass, 1, &targetEnclosingType))
                            return E_FAIL;

                        // If the enclosing type doesn't match, then we are in a failure state.
                        if (enclosingScopeToken != targetTypeDefToken)
                            return CLDB_E_RECORD_NOTFOUND;
                    }

                    found = true;
                    break;
                } while (md_cursor_next(&targetTypeDef));

                if (!found)
                    return CLDB_E_RECORD_NOTFOUND;

                enclosingScope = targetTypeDef;
            }
            *targetTypeRef = enclosingScope;
            return S_OK;
        }

        mdcursor_t resolutionScope = targetOutermostScope;
        for (; !typesForTypeRefs.empty(); typesForTypeRefs.pop())
        {
            mdcursor_t sourceEnclosingTypeRef = typesForTypeRefs.top();
            md_added_row_t targetEnclosingTypeRef;
            if (!md_append_row(targetModule, mdtid_TypeRef, &targetEnclosingTypeRef))
                return E_FAIL;
            
            if (1 != md_set_column_value_as_cursor(targetEnclosingTypeRef, mdtTypeRef_ResolutionScope, 1, &resolutionScope))
                return E_FAIL;
            
            char const* typeName;
            if (1 != md_get_column_value_as_utf8(sourceEnclosingTypeRef, mdtTypeRef_TypeName, 1, &typeName)
                || 1 != md_set_column_value_as_utf8(targetEnclosingTypeRef, mdtTypeRef_TypeName, 1, &typeName))
            {
                return E_FAIL;
            }
            
            char const* typeNamespace;
            if (1 != md_get_column_value_as_utf8(sourceEnclosingTypeRef, mdtTypeRef_TypeNamespace, 1, &typeNamespace)
                || 1 != md_set_column_value_as_utf8(targetEnclosingTypeRef, mdtTypeRef_TypeNamespace, 1, &typeNamespace))
            {
                return E_FAIL;
            }

            resolutionScope = targetEnclosingTypeRef;
            onRowAdded(targetEnclosingTypeRef);
        }

        *targetTypeRef = resolutionScope;

        return S_OK;
    }
}

HRESULT ImportReferenceToTypeDefOrRefOrSpec(
    mdhandle_t sourceAssembly,
    mdhandle_t sourceModule,
    span<const uint8_t> sourceAssemblyHash,
    mdhandle_t targetAssembly,
    mdhandle_t targetModule,
    std::function<void(mdcursor_t)> onRowAdded,
    mdToken* importedToken)
{
    HRESULT hr;
    mdcursor_t sourceCursor;
    if (!md_token_to_cursor(sourceModule, *importedToken, &sourceCursor))
        return CLDB_E_FILE_CORRUPT;
    
    switch (GetTokenTypeFromCursor(sourceCursor))
    {
        case mdtTypeDef:
        {
            mdcursor_t targetCursor;
            RETURN_IF_FAILED(ImportReferenceToTypeDef(sourceCursor, sourceAssembly, sourceAssemblyHash, targetAssembly, targetModule, true, onRowAdded, &targetCursor));
            if (!md_cursor_to_token(targetCursor, importedToken))
                return E_FAIL;
            
            return S_OK;
        }
        case mdtTypeRef:
        {
            mdcursor_t targetCursor;
            RETURN_IF_FAILED(ImportReferenceToTypeRef(sourceCursor, sourceAssembly, sourceAssemblyHash, targetAssembly, targetModule, onRowAdded, &targetCursor));
            if (!md_cursor_to_token(targetCursor, importedToken))
                return E_FAIL;
            
            return S_OK;
        }
        case mdtTypeSpec:
        {
            uint8_t const* signature;
            uint32_t signatureLength;
            if (1 != md_get_column_value_as_blob(sourceCursor, mdtTypeSpec_Signature, 1, &signature, &signatureLength))
                return E_FAIL;
            
            malloc_span<uint8_t> importedSignature;
            RETURN_IF_FAILED(ImportTypeSpecBlob(sourceAssembly, sourceModule, sourceAssemblyHash, targetAssembly, targetModule, {signature, signatureLength}, onRowAdded, importedSignature));

            md_added_row_t typeSpec;
            if (!md_append_row(targetModule, mdtid_TypeSpec, &typeSpec))
                return E_FAIL;
            
            uint8_t const* importedSignatureData = importedSignature;
            uint32_t importedSignatureLength = (uint32_t)importedSignature.size();
            if (1 != md_set_column_value_as_blob(typeSpec, mdtTypeSpec_Signature, 1, &importedSignatureData, &importedSignatureLength))
                return E_FAIL;
            
            if (!md_cursor_to_token(typeSpec, importedToken))
                return E_FAIL;
        }
        default:
            return E_INVALIDARG;
    }
}
