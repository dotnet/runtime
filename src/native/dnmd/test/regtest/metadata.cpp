#include "asserts.h"
#include "fixtures.h"
#include "baseline.h"

#include <dnmd_interfaces.hpp>

#include <gtest/gtest.h>
#include <gmock/gmock.h>

#include <array>
#include <utility>

#ifndef BUILD_WINDOWS
#define EXPECT_HRESULT_SUCCEEDED(hr) EXPECT_THAT((hr), testing::Ge(S_OK))
#define EXPECT_HRESULT_FAILED(hr) EXPECT_THAT((hr), testing::Lt(0))
#define ASSERT_HRESULT_SUCCEEDED(hr) ASSERT_THAT((hr), testing::Ge(S_OK))
#define ASSERT_HRESULT_FAILED(hr) ASSERT_THAT((hr), testing::Lt(0))
#endif

namespace
{
    HRESULT CreateImport(IMetaDataDispenser* disp, void const* data, uint32_t dataLen, IMetaDataImport2** import)
    {
        assert(disp != nullptr && data != nullptr && dataLen > 0 && import != nullptr);
        return disp->OpenScopeOnMemory(
            data,
            dataLen,
            CorOpenFlags::ofReadOnly,
            IID_IMetaDataImport2,
            reinterpret_cast<IUnknown**>(import));
    }

    template<typename T>
    using static_enum_buffer = std::array<T, 32>;

    template<typename T>
    using static_char_buffer = std::array<T, 64>;

    // default values recommended by http://isthe.com/chongo/tech/comp/fnv/
    uint32_t const Prime = 0x01000193; //   16777619
    uint32_t const Seed = 0x811C9DC5; // 2166136261
    // hash a single byte
    uint32_t fnv1a(uint8_t oneByte, uint32_t hash = Seed)
    {
        return (oneByte ^ hash) * Prime;
    }

    // Based on https://create.stephan-brumme.com/fnv-hash/
    uint32_t HashCharArray(static_char_buffer<WCHAR> const& arr, uint32_t written)
    {
        uint32_t hash = Seed;
        auto curr = std::begin(arr);
        auto end = curr + written;
        for (; curr < end; ++curr)
        {
            WCHAR c = *curr;
            std::array<uint8_t, sizeof(c)> r;
            memcpy(r.data(), &c, r.size());
            for (uint8_t b : r)
                hash = fnv1a(b, hash);
        }
        return hash;
    }

    // Based on https://create.stephan-brumme.com/fnv-hash/
    uint32_t HashByteArray(void const* arr, size_t byteLength)
    {
        uint32_t hash = Seed;
        auto curr = (uint8_t const*)arr;
        auto end = curr + byteLength;
        for (; curr < end; ++curr)
        {
            hash = fnv1a(*curr, hash);
        }
        return hash;
    }

    std::vector<uint32_t> GetCustomAttributeByName(IMetaDataImport2* import, LPCWSTR customAttr, mdToken tkObj)
    {
        std::vector<uint32_t> values;

        void const* ppData;
        ULONG pcbData;
        HRESULT hr = import->GetCustomAttributeByName(tkObj,
            customAttr,
            &ppData,
            &pcbData);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(ppData, pcbData));
            values.push_back(pcbData);
        }
        return values;
    }

    std::vector<uint32_t> GetCustomAttribute_Nullable(IMetaDataImport2* import, mdToken tkObj)
    {
        auto NullableAttrName = W("System.Runtime.CompilerServices.NullableAttribute");
        return GetCustomAttributeByName(import, NullableAttrName, tkObj);
    }

    std::vector<uint32_t> GetCustomAttribute_CompilerGenerated(IMetaDataImport2* import, mdToken tkObj)
    {
        auto CompilerGeneratedAttrName = W("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        return GetCustomAttributeByName(import, CompilerGeneratedAttrName, tkObj);
    }

    void ValidateAndCloseEnum(IMetaDataImport2* import, HCORENUM hcorenum, ULONG expectedCount)
    {
        ULONG count;
        ASSERT_HRESULT_SUCCEEDED(import->CountEnum(hcorenum, &count));
        ASSERT_EQ(count, expectedCount);
        import->CloseEnum(hcorenum);
    }

    std::vector<uint32_t> EnumTypeDefs(IMetaDataImport2* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumTypeDefs(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
            {
                tokens.push_back(tokensBuffer[i]);
            }
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumTypeRefs(IMetaDataImport2* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumTypeRefs(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumTypeSpecs(IMetaDataImport2* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumTypeSpecs(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumModuleRefs(IMetaDataImport2* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumModuleRefs(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumInterfaceImpls(IMetaDataImport2* import, mdTypeDef typdef)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumInterfaceImpls(&hcorenum, typdef, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumMembers(IMetaDataImport2* import, mdTypeDef typdef)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumMembers(&hcorenum, typdef, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumMembersWithName(IMetaDataImport2* import, mdTypeDef typdef, LPCWSTR memberName = W(".ctor"))
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumMembersWithName(&hcorenum, typdef, memberName, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumMemberRefs(IMetaDataImport2* import, mdToken tkParent)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumMemberRefs(&hcorenum, tkParent, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumMethods(IMetaDataImport2* import, mdTypeDef typdef)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumMethods(&hcorenum, typdef, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumMethodsWithName(IMetaDataImport2* import, mdToken typdef, LPCWSTR methodName = W(".ctor"))
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumMethodsWithName(&hcorenum, typdef, methodName, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumMethodImpls(IMetaDataImport2* import, mdTypeDef typdef)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer1{};
        static_enum_buffer<uint32_t> tokensBuffer2{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumMethodImpls(&hcorenum, typdef, tokensBuffer1.data(), tokensBuffer2.data(), (ULONG)tokensBuffer1.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
            {
                tokens.push_back(tokensBuffer1[i]);
                tokens.push_back(tokensBuffer2[i]);
            }
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)(tokens.size() / 2));
        return tokens;
    }

    std::vector<uint32_t> EnumMethodSemantics(IMetaDataImport2* import, mdMethodDef mb)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumMethodSemantics(&hcorenum, mb, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumParams(IMetaDataImport2* import, mdMethodDef methoddef)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumParams(&hcorenum, methoddef, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumMethodSpecs(IMetaDataImport2* import, mdToken tk)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumMethodSpecs(&hcorenum, tk, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumEvents(IMetaDataImport2* import, mdTypeDef tk)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumEvents(&hcorenum, tk, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumProperties(IMetaDataImport2* import, mdTypeDef tk)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumProperties(&hcorenum, tk, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumFields(IMetaDataImport2* import, mdTypeDef tk)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumFields(&hcorenum, tk, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumFieldsWithName(IMetaDataImport2* import, mdTypeDef tk, LPCWSTR name = W("_name"))
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumFieldsWithName(&hcorenum, tk, name, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumSignatures(IMetaDataImport2* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumSignatures(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumUserStrings(IMetaDataImport2* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumUserStrings(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumCustomAttributes(IMetaDataImport2* import, mdToken tk = mdTokenNil, mdToken tkType = mdTokenNil)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumCustomAttributes(&hcorenum, tk, tkType, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumGenericParams(IMetaDataImport2* import, mdToken tk)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumGenericParams(&hcorenum, tk, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumGenericParamConstraints(IMetaDataImport2* import, mdGenericParam tk)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumGenericParamConstraints(&hcorenum, tk, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumPermissionSetsAndGetProps(IMetaDataImport2* import, mdToken permTk)
    {
        std::vector<uint32_t> values;
        static_enum_buffer<uint32_t> tokensBuffer{};

        // See CorDeclSecurity for actions definitions
        for (int32_t action = (int32_t)dclActionNil; action <= dclMaximumValue; ++action)
        {
            std::vector<uint32_t> tokens;
            HCORENUM hcorenum{};
            {
                ULONG returned;
                while (0 == import->EnumPermissionSets(&hcorenum, permTk, action, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
                    && returned != 0)
                {
                    for (ULONG j = 0; j < returned; ++j)
                    {
                        tokens.push_back(tokensBuffer[j]);
                    }
                }
                ValidateAndCloseEnum(import, hcorenum, (ULONG)tokens.size());
            }

            for (uint32_t pk : tokens)
            {
                DWORD a;
                void const* ppvPermission;
                ULONG pcbPermission;
                HRESULT hr = import->GetPermissionSetProps(pk, &a, &ppvPermission, &pcbPermission);
                values.push_back(hr);
                if (hr == S_OK)
                {
                    values.push_back(a);
                    values.push_back(HashByteArray(ppvPermission, pcbPermission));
                    values.push_back(pcbPermission);
                }
            }
        }
        return values;
    }

    std::vector<uint32_t> EnumAssemblyRefs(IMetaDataAssemblyImport* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumAssemblyRefs(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        minipal::com_ptr<IMetaDataImport2> mdImport;
        HRESULT hr = import->QueryInterface(IID_IMetaDataImport2, (void**)&mdImport);
        EXPECT_HRESULT_SUCCEEDED(hr);
        ValidateAndCloseEnum(mdImport, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumFiles(IMetaDataAssemblyImport* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumFiles(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        minipal::com_ptr<IMetaDataImport2>  mdImport;
        HRESULT hr = import->QueryInterface(IID_IMetaDataImport2, (void**)&mdImport);
        EXPECT_HRESULT_SUCCEEDED(hr);
        ValidateAndCloseEnum(mdImport, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumExportedTypes(IMetaDataAssemblyImport* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumExportedTypes(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        minipal::com_ptr<IMetaDataImport2>  mdImport;
        HRESULT hr = import->QueryInterface(IID_IMetaDataImport2, (void**)&mdImport);
        EXPECT_HRESULT_SUCCEEDED(hr);
        ValidateAndCloseEnum(mdImport, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> EnumManifestResources(IMetaDataAssemblyImport* import)
    {
        std::vector<uint32_t> tokens;
        static_enum_buffer<uint32_t> tokensBuffer{};
        HCORENUM hcorenum{};
        ULONG returned;
        while (0 == import->EnumManifestResources(&hcorenum, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
            && returned != 0)
        {
            for (ULONG i = 0; i < returned; ++i)
                tokens.push_back(tokensBuffer[i]);
        }
        minipal::com_ptr<IMetaDataImport2>  mdImport;
        HRESULT hr = import->QueryInterface(IID_IMetaDataImport2, (void**)&mdImport);
        EXPECT_HRESULT_SUCCEEDED(hr);
        ValidateAndCloseEnum(mdImport, hcorenum, (ULONG)tokens.size());
        return tokens;
    }

    std::vector<uint32_t> FindTypeRef(IMetaDataImport2* import)
    {
        std::vector<uint32_t> values;
        HRESULT hr;
        mdToken tk;

        // The first assembly ref token typically contains System.Object and Enumerator.
        mdToken const assemblyRefToken = 0x23000001;
        hr = import->FindTypeRef(assemblyRefToken, W("System.Object"), &tk);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(tk);

        // Look for a type that won't ever exist
        hr = import->FindTypeRef(assemblyRefToken, W("DoesntExist"), &tk);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(tk);
        return values;
    }

    std::vector<uint32_t> FindTypeDefByName(IMetaDataImport2* import, LPCWSTR name, mdToken scope)
    {
        std::vector<uint32_t> values;

        mdTypeDef ptd;
        HRESULT hr = import->FindTypeDefByName(name, scope, &ptd);

        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(ptd);
        return values;
    }

    std::vector<uint32_t> FindExportedTypeByName(IMetaDataAssemblyImport* import, LPCWSTR name, mdToken tkImplementation)
    {
        std::vector<uint32_t> values;

        mdExportedType exported;
        HRESULT hr = import->FindExportedTypeByName(name, tkImplementation, &exported);

        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(exported);
        return values;
    }

    std::vector<uint32_t> FindManifestResourceByName(IMetaDataAssemblyImport* import, LPCWSTR name)
    {
        std::vector<uint32_t> values;

        mdManifestResource resource;
        HRESULT hr = import->FindManifestResourceByName(name, &resource);

        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(resource);
        return values;
    }

    std::vector<uint32_t> GetTypeDefProps(IMetaDataImport2* import, mdTypeDef typdef)
    {
        std::vector<uint32_t> values;

        static_char_buffer<WCHAR> name{};
        ULONG pchTypeDef;
        DWORD pdwTypeDefFlags;
        mdToken ptkExtends;
        HRESULT hr = import->GetTypeDefProps(typdef,
            name.data(),
            (ULONG)name.size(),
            &pchTypeDef,
            &pdwTypeDefFlags,
            &ptkExtends);

        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashCharArray(name, pchTypeDef);
            values.push_back(hash);
            values.push_back(pchTypeDef);
            values.push_back(pdwTypeDefFlags);
            values.push_back(ptkExtends);
        }
        return values;
    }

    std::vector<uint32_t> GetTypeRefProps(IMetaDataImport2* import, mdTypeRef typeref)
    {
        std::vector<uint32_t> values;

        static_char_buffer<WCHAR> name{};
        mdToken tkResolutionScope;
        ULONG pchTypeRef;
        HRESULT hr = import->GetTypeRefProps(typeref,
            &tkResolutionScope,
            name.data(),
            (ULONG)name.size(),
            &pchTypeRef);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(tkResolutionScope);
            uint32_t hash = HashCharArray(name, pchTypeRef);
            values.push_back(hash);
            values.push_back(pchTypeRef);
        }
        return values;
    }

    std::vector<uint32_t> GetScopeProps(IMetaDataImport2* import)
    {
        std::vector<uint32_t> values;

        static_char_buffer<WCHAR> name{};
        ULONG pchName;
        GUID mvid;
        HRESULT hr = import->GetScopeProps(
            name.data(),
            (ULONG)name.size(),
            &pchName,
            &mvid);

        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashCharArray(name, pchName);
            values.push_back(hash);
            values.push_back(pchName);

            std::array<uint32_t, sizeof(GUID) / sizeof(uint32_t)> buffer{};
            memcpy(buffer.data(), &mvid, buffer.size());
            for (auto b : buffer)
                values.push_back(b);
        }
        return values;
    }

    std::vector<uint32_t> GetModuleRefProps(IMetaDataImport2* import, mdModuleRef moduleref)
    {
        std::vector<uint32_t> values;

        static_char_buffer<WCHAR> name{};
        ULONG pchModuleRef;
        HRESULT hr = import->GetModuleRefProps(moduleref,
            name.data(),
            (ULONG)name.size(),
            &pchModuleRef);

        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashCharArray(name, pchModuleRef);
            values.push_back(hash);
            values.push_back(pchModuleRef);
        }
        return values;
    }

    std::vector<uint32_t> GetMethodProps(IMetaDataImport2* import, mdToken tk, void const** sig = nullptr, ULONG* sigLen = nullptr)
    {
        std::vector<uint32_t> values;

        mdTypeDef pClass;
        static_char_buffer<WCHAR> name{};
        ULONG pchMethod;
        DWORD pdwAttr;
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        ULONG pulCodeRVA;
        DWORD pdwImplFlags;
        HRESULT hr = import->GetMethodProps(tk,
            &pClass,
            name.data(),
            (ULONG)name.size(),
            &pchMethod,
            &pdwAttr,
            &ppvSigBlob,
            &pcbSigBlob,
            &pulCodeRVA,
            &pdwImplFlags);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pClass);
            uint32_t hash = HashCharArray(name, pchMethod);
            values.push_back(hash);
            values.push_back(pchMethod);
            values.push_back(pdwAttr);
            values.push_back(HashByteArray(ppvSigBlob, pcbSigBlob));
            values.push_back(pcbSigBlob);
            values.push_back(pulCodeRVA);
            values.push_back(pdwImplFlags);

            if (sig != nullptr)
                *sig = ppvSigBlob;
            if (sigLen != nullptr)
                *sigLen = pcbSigBlob;
        }
        return values;
    }

    std::vector<uint32_t> GetParamProps(IMetaDataImport2* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        mdMethodDef pmd;
        ULONG pulSequence;
        static_char_buffer<WCHAR> name{};
        ULONG pchName;
        DWORD pdwAttr;
        DWORD pdwCPlusTypeFlag;
        UVCP_CONSTANT ppValue;
        ULONG pcchValue;
        HRESULT hr = import->GetParamProps(tk,
            &pmd,
            &pulSequence,
            name.data(),
            (ULONG)name.size(),
            &pchName,
            &pdwAttr,
            &pdwCPlusTypeFlag,
            &ppValue,
            &pcchValue);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pmd);
            values.push_back(pulSequence);
            uint32_t hash = HashCharArray(name, pchName);
            values.push_back(hash);
            values.push_back(pchName);
            values.push_back(pdwAttr);
            values.push_back(pdwCPlusTypeFlag);
            values.push_back(HashByteArray(ppValue, pcchValue));
            values.push_back(pcchValue);
        }
        return values;
    }

    std::vector<uint32_t> GetMethodSpecProps(IMetaDataImport2* import, mdMethodSpec methodSpec)
    {
        std::vector<uint32_t> values;

        mdToken parent;
        PCCOR_SIGNATURE sig;
        ULONG sigLen;
        HRESULT hr = import->GetMethodSpecProps(methodSpec,
            &parent,
            &sig,
            &sigLen);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(parent);
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
        }
        return values;
    }

    std::vector<uint32_t> GetMemberRefProps(IMetaDataImport2* import, mdMemberRef mr, PCCOR_SIGNATURE* sig = nullptr, ULONG* sigLen = nullptr)
    {
        std::vector<uint32_t> values;

        mdToken ptk;
        static_char_buffer<WCHAR> name{};
        ULONG pchMember;
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        HRESULT hr = import->GetMemberRefProps(mr,
            &ptk,
            name.data(),
            (ULONG)name.size(),
            &pchMember,
            &ppvSigBlob,
            &pcbSigBlob);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(ptk);
            uint32_t hash = HashCharArray(name, pchMember);
            values.push_back(hash);
            values.push_back(pchMember);
            values.push_back(HashByteArray(ppvSigBlob, pcbSigBlob));
            values.push_back(pcbSigBlob);

            if (sig != nullptr)
                *sig = ppvSigBlob;
            if (sigLen != nullptr)
                *sigLen = pcbSigBlob;
        }
        return values;
    }

    std::vector<uint32_t> GetEventProps(IMetaDataImport2* import, mdEvent tk, std::vector<mdMethodDef>* methoddefs = nullptr)
    {
        std::vector<uint32_t> values;

        mdTypeDef pClass;
        static_char_buffer<WCHAR> name{};
        ULONG pchEvent;
        DWORD pdwEventFlags;
        mdToken ptkEventType;
        mdMethodDef pmdAddOn;
        mdMethodDef pmdRemoveOn;
        mdMethodDef pmdFire;
        static_enum_buffer<mdMethodDef> rmdOtherMethod;
        ULONG pcOtherMethod;
        HRESULT hr = import->GetEventProps(tk,
            &pClass,
            name.data(),
            (ULONG)name.size(),
            &pchEvent,
            &pdwEventFlags,
            &ptkEventType,
            &pmdAddOn,
            &pmdRemoveOn,
            &pmdFire,
            rmdOtherMethod.data(),
            (ULONG)rmdOtherMethod.size(),
            &pcOtherMethod);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pClass);
            uint32_t hash = HashCharArray(name, pchEvent);
            values.push_back(hash);
            values.push_back(pchEvent);
            values.push_back(pdwEventFlags);
            values.push_back(ptkEventType);
            values.push_back(pmdAddOn);
            values.push_back(pmdRemoveOn);
            values.push_back(pmdFire);

            std::vector<mdMethodDef> retMaybe;
            for (ULONG i = 0; i < std::min(pcOtherMethod, (ULONG)rmdOtherMethod.size()); ++i)
            {
                values.push_back(rmdOtherMethod[i]);
                retMaybe.push_back(rmdOtherMethod[i]);
            }

            retMaybe.push_back(pmdAddOn);
            retMaybe.push_back(pmdRemoveOn);
            retMaybe.push_back(pmdFire);

            if (methoddefs != nullptr)
                *methoddefs = std::move(retMaybe);
        }
        return values;
    }

    std::vector<uint32_t> GetPropertyProps(IMetaDataImport2* import, mdProperty tk, std::vector<mdMethodDef>* methoddefs = nullptr)
    {
        std::vector<uint32_t> values;

        mdTypeDef pClass;
        static_char_buffer<WCHAR> name{};
        ULONG pchProperty;
        DWORD pdwPropFlags;
        PCCOR_SIGNATURE sig;
        ULONG sigLen;
        DWORD pdwCPlusTypeFlag;
        UVCP_CONSTANT ppDefaultValue;
        ULONG pcchDefaultValue;
        mdMethodDef pmdSetter;
        mdMethodDef pmdGetter;
        static_enum_buffer<mdMethodDef> rmdOtherMethod{};
        ULONG pcOtherMethod;
        HRESULT hr = import->GetPropertyProps(tk,
            &pClass,
            name.data(),
            (ULONG)name.size(),
            &pchProperty,
            &pdwPropFlags,
            &sig,
            &sigLen,
            &pdwCPlusTypeFlag,
            &ppDefaultValue,
            &pcchDefaultValue,
            &pmdSetter,
            &pmdGetter,
            rmdOtherMethod.data(),
            (ULONG)rmdOtherMethod.size(),
            &pcOtherMethod);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pClass);
            uint32_t hash = HashCharArray(name, pchProperty);
            values.push_back(hash);
            values.push_back(pchProperty);
            values.push_back(pdwPropFlags);
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
            values.push_back(pdwCPlusTypeFlag);
            values.push_back(HashByteArray(ppDefaultValue, pcchDefaultValue));
            values.push_back(pcchDefaultValue);
            values.push_back(pmdSetter);
            values.push_back(pmdGetter);

            std::vector<mdMethodDef> retMaybe;
            for (ULONG i = 0; i < std::min(pcOtherMethod, (ULONG)rmdOtherMethod.size()); ++i)
            {
                values.push_back(rmdOtherMethod[i]);
                retMaybe.push_back(rmdOtherMethod[i]);
            }

            retMaybe.push_back(pmdSetter);
            retMaybe.push_back(pmdGetter);

            if (methoddefs != nullptr)
                *methoddefs = std::move(retMaybe);
        }
        return values;
    }

    std::vector<uint32_t> GetFieldProps(IMetaDataImport2* import, mdFieldDef tk, void const** sig = nullptr, ULONG* sigLen = nullptr)
    {
        std::vector<uint32_t> values;

        mdTypeDef pClass;
        static_char_buffer<WCHAR> name{};
        ULONG pchField;
        DWORD pdwAttr;
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        DWORD pdwCPlusTypeFlag;
        UVCP_CONSTANT ppValue = nullptr;
        ULONG pcchValue = 0;
        HRESULT hr = import->GetFieldProps(tk,
            &pClass,
            name.data(),
            (ULONG)name.size(),
            &pchField,
            &pdwAttr,
            &ppvSigBlob,
            &pcbSigBlob,
            &pdwCPlusTypeFlag,
            &ppValue,
            &pcchValue);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pClass);
            uint32_t hash = HashCharArray(name, pchField);
            values.push_back(hash);
            values.push_back(pchField);
            values.push_back(pdwAttr);
            values.push_back(HashByteArray(ppvSigBlob, pcbSigBlob));
            values.push_back(pcbSigBlob);
            values.push_back(pdwCPlusTypeFlag);
            values.push_back(HashByteArray(ppValue, pcchValue));
            values.push_back(pcchValue);

            if (sig != nullptr)
                *sig = ppvSigBlob;
            if (sigLen != nullptr)
                *sigLen = pcbSigBlob;
        }
        return values;
    }

    std::vector<uint32_t> GetCustomAttributeProps(IMetaDataImport2* import, mdCustomAttribute cv)
    {
        std::vector<uint32_t> values;

        mdToken ptkObj;
        mdToken ptkType;
        void const* sig;
        ULONG sigLen;
        HRESULT hr = import->GetCustomAttributeProps(cv,
            &ptkObj,
            &ptkType,
            &sig,
            &sigLen);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(ptkObj);
            values.push_back(ptkType);
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
        }
        return values;
    }

    std::vector<uint32_t> GetGenericParamProps(IMetaDataImport2* import, mdGenericParam gp)
    {
        std::vector<uint32_t> values;

        ULONG pulParamSeq;
        DWORD pdwParamFlags;
        mdToken ptOwner;
        DWORD reserved;
        static_char_buffer<WCHAR> name{};
        ULONG pchName;
        HRESULT hr = import->GetGenericParamProps(gp,
            &pulParamSeq,
            &pdwParamFlags,
            &ptOwner,
            &reserved,
            name.data(),
            (ULONG)name.size(),
            &pchName);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pulParamSeq);
            values.push_back(pdwParamFlags);
            values.push_back(ptOwner);
            // We don't care about reserved
            // as its value is unspecified
            uint32_t hash = HashCharArray(name, pchName);
            values.push_back(hash);
            values.push_back(pchName);
        }
        return values;
    }

    std::vector<uint32_t> GetGenericParamConstraintProps(IMetaDataImport2* import, mdGenericParamConstraint tk)
    {
        std::vector<uint32_t> values;

        mdGenericParam ptGenericParam;
        mdToken ptkConstraintType;
        HRESULT hr = import->GetGenericParamConstraintProps(tk,
            &ptGenericParam,
            &ptkConstraintType);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(ptGenericParam);
            values.push_back(ptkConstraintType);
        }
        return values;
    }

    std::vector<uint32_t> GetPinvokeMap(IMetaDataImport2* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        DWORD pdwMappingFlags;
        static_char_buffer<WCHAR> name{};
        ULONG pchImportName;
        mdModuleRef pmrImportDLL;
        HRESULT hr = import->GetPinvokeMap(tk,
            &pdwMappingFlags,
            name.data(),
            (ULONG)name.size(),
            &pchImportName,
            &pmrImportDLL);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pdwMappingFlags);
            uint32_t hash = HashCharArray(name, pchImportName);
            values.push_back(hash);
            values.push_back(pchImportName);
            values.push_back(pmrImportDLL);
        }
        return values;
    }

    std::vector<uint32_t> GetNativeCallConvFromSig(IMetaDataImport2* import, void const* sig, ULONG sigLen)
    {
        std::vector<uint32_t> values;

        // .NET 2,4 and CoreCLR metadata imports do not handle null signatures.
        if (sigLen != 0)
        {
            ULONG pCallConv;
            HRESULT hr = import->GetNativeCallConvFromSig(sig, sigLen, &pCallConv);

            values.push_back(hr);
            if (hr == S_OK)
                values.push_back(pCallConv);
        }

        return values;
    }

    std::vector<uint32_t> GetTypeSpecFromToken(IMetaDataImport2* import, mdTypeSpec typespec)
    {
        std::vector<uint32_t> values;

        PCCOR_SIGNATURE sig;
        ULONG sigLen;
        HRESULT hr = import->GetTypeSpecFromToken(typespec, &sig, &sigLen);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
        }
        return values;
    }

    std::vector<uint32_t> GetSigFromToken(IMetaDataImport2* import, mdSignature tkSig)
    {
        std::vector<uint32_t> values;

        PCCOR_SIGNATURE sig;
        ULONG sigLen;
        HRESULT hr = import->GetSigFromToken(tkSig, &sig, &sigLen);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
        }
        return values;
    }

    std::vector<uint32_t> GetMethodSemantics(IMetaDataImport2* import, mdToken tkEventProp, mdMethodDef methodDef)
    {
        std::vector<uint32_t> values;

        DWORD pdwSemanticsFlags;
        HRESULT hr = import->GetMethodSemantics(methodDef, tkEventProp, &pdwSemanticsFlags);

        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(pdwSemanticsFlags);

        return values;
    }

    std::vector<uint32_t> GetUserString(IMetaDataImport2* import, mdString tkStr)
    {
        std::vector<uint32_t> values;

        static_char_buffer<WCHAR> name{};
        ULONG pchString;
        HRESULT hr = import->GetUserString(tkStr, name.data(), (ULONG)name.size(), &pchString);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashCharArray(name, pchString);
            values.push_back(hash);
            values.push_back(pchString);
        }
        return values;
    }

    std::vector<uint32_t> GetNameFromToken(IMetaDataImport2* import, mdToken tkObj)
    {
        std::vector<uint32_t> values;

        MDUTF8CSTR pszUtf8NamePtr;
        HRESULT hr = import->GetNameFromToken(tkObj, &pszUtf8NamePtr);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(pszUtf8NamePtr, ::strlen(pszUtf8NamePtr) + 1));
        }
        return values;
    }

    std::vector<uint32_t> GetFieldMarshal(IMetaDataImport2* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        PCCOR_SIGNATURE sig;
        ULONG sigLen;
        HRESULT hr = import->GetFieldMarshal(tk, &sig, &sigLen);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
        }
        return values;
    }

    std::vector<uint32_t> GetNestedClassProps(IMetaDataImport2* import, mdTypeDef tk)
    {
        std::vector<uint32_t> values;

        mdTypeDef ptdEnclosingClass;
        HRESULT hr = import->GetNestedClassProps(tk, &ptdEnclosingClass);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(ptdEnclosingClass);

        return values;
    }

    std::vector<uint32_t> GetClassLayout(IMetaDataImport2* import, mdTypeDef tk)
    {
        std::vector<uint32_t> values;

        DWORD pdwPackSize;
        std::vector<COR_FIELD_OFFSET> offsets(24);
        ULONG pcFieldOffset;
        ULONG pulClassSize;
        HRESULT hr = import->GetClassLayout(tk,
            &pdwPackSize,
            offsets.data(),
            (ULONG)offsets.size(),
            &pcFieldOffset,
            &pulClassSize);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pdwPackSize);
            for (ULONG i = 0; i < std::min(pcFieldOffset, (ULONG)offsets.size()); ++i)
            {
                COR_FIELD_OFFSET const& o = offsets[i];
                values.push_back(o.ridOfField);
                values.push_back(o.ulOffset);
            }
            values.push_back(pcFieldOffset);
            values.push_back(pulClassSize);
        }

        return values;
    }

    std::vector<uint32_t> GetRVA(IMetaDataImport2* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        ULONG pulCodeRVA;
        DWORD pdwImplFlags;
        HRESULT hr = import->GetRVA(tk, &pulCodeRVA, &pdwImplFlags);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pulCodeRVA);
            values.push_back(pdwImplFlags);
        }
        return values;
    }

    std::vector<uint32_t> GetParamForMethodIndex(IMetaDataImport2* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        mdParamDef def;
        for (uint32_t i = 0; i < std::numeric_limits<uint32_t>::max(); ++i)
        {
            HRESULT hr = import->GetParamForMethodIndex(tk, i, &def);
            values.push_back(hr);
            if (hr != S_OK)
                break;
            values.push_back(def);
        }
        return values;
    }

    int32_t IsGlobal(IMetaDataImport2* import, mdToken tk)
    {
        int32_t pbGlobal;
        HRESULT hr = import->IsGlobal(tk, &pbGlobal);
        if (hr != S_OK)
            return hr;
        return pbGlobal;
    }

    std::vector<uint32_t> GetVersionString(IMetaDataImport2* import)
    {
        std::vector<uint32_t> values;

        static_char_buffer<WCHAR> name{};
        ULONG pccBufSize;
        HRESULT hr = import->GetVersionString(name.data(), (DWORD)name.size(), &pccBufSize);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashCharArray(name, pccBufSize);
            values.push_back(hash);
            values.push_back(pccBufSize);
        }

        return values;
    }

    std::vector<uint32_t> GetAssemblyFromScope(IMetaDataAssemblyImport* import)
    {
        std::vector<uint32_t> values;

        mdAssembly mdAsm;
        HRESULT hr = import->GetAssemblyFromScope(&mdAsm);
        if (hr == S_OK)
            values.push_back(mdAsm);
        return values;
    }

    std::vector<size_t> GetAssemblyProps(IMetaDataAssemblyImport* import, mdAssembly mda)
    {
        std::vector<size_t> values;
        static_char_buffer<WCHAR> name{};
        static_char_buffer<WCHAR> locale{};
        std::vector<DWORD> processor(1);
        std::vector<OSINFO> osInfo(1);

        ASSEMBLYMETADATA metadata;
        metadata.szLocale = locale.data();
        metadata.cbLocale = (ULONG)locale.size();
        metadata.rProcessor = processor.data();
        metadata.ulProcessor = (ULONG)processor.size();
        metadata.rOS = osInfo.data();
        metadata.ulOS = (ULONG)osInfo.size();

        void const* publicKey;
        ULONG publicKeyLength;
        ULONG hashAlgId;
        ULONG nameLength;
        ULONG flags;
        HRESULT hr = import->GetAssemblyProps(mda, &publicKey, &publicKeyLength, &hashAlgId, name.data(), (ULONG)name.size(), &nameLength, &metadata, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(HashByteArray(publicKey, publicKeyLength));
            values.push_back(publicKeyLength);
            values.push_back(hashAlgId);
            values.push_back(HashCharArray(name, nameLength));
            values.push_back((size_t)nameLength);
            values.push_back(metadata.usMajorVersion);
            values.push_back(metadata.usMinorVersion);
            values.push_back(metadata.usBuildNumber);
            values.push_back(metadata.usRevisionNumber);
            values.push_back(HashCharArray(locale, metadata.cbLocale));
            values.push_back(metadata.cbLocale);
            values.push_back(metadata.ulProcessor);
            values.push_back(metadata.ulOS);
            values.push_back(flags);
        }
        return values;
    }

    std::vector<size_t> GetAssemblyRefProps(IMetaDataAssemblyImport* import, mdAssemblyRef mdar)
    {
        std::vector<size_t> values;
        static_char_buffer<WCHAR> name{};
        static_char_buffer<WCHAR> locale{};
        std::vector<DWORD> processor(1);
        std::vector<OSINFO> osInfo(1);

        ASSEMBLYMETADATA metadata;
        metadata.szLocale = locale.data();
        metadata.cbLocale = (ULONG)locale.size();
        metadata.rProcessor = processor.data();
        metadata.ulProcessor = (ULONG)processor.size();
        metadata.rOS = osInfo.data();
        metadata.ulOS = (ULONG)osInfo.size();

        void const* publicKeyOrToken;
        ULONG publicKeyOrTokenLength;
        ULONG nameLength;
        void const* hash;
        ULONG hashLength;
        DWORD flags;
        HRESULT hr = import->GetAssemblyRefProps(mdar, &publicKeyOrToken, &publicKeyOrTokenLength, name.data(), (ULONG)name.size(), &nameLength, &metadata, &hash, &hashLength, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(HashByteArray(publicKeyOrToken, publicKeyOrTokenLength));
            values.push_back(publicKeyOrTokenLength);
            values.push_back(HashCharArray(name, nameLength));
            values.push_back((size_t)nameLength);
            values.push_back(metadata.usMajorVersion);
            values.push_back(metadata.usMinorVersion);
            values.push_back(metadata.usBuildNumber);
            values.push_back(metadata.usRevisionNumber);
            values.push_back(HashCharArray(locale, metadata.cbLocale));
            values.push_back(metadata.cbLocale);
            values.push_back(metadata.ulProcessor);
            values.push_back(metadata.ulOS);
            values.push_back(HashByteArray(hash, hashLength));
            values.push_back(hashLength);
            values.push_back(flags);
        }
        return values;
    }

    std::vector<size_t> GetFileProps(IMetaDataAssemblyImport* import, mdFile mdf)
    {
        std::vector<size_t> values;
        static_char_buffer<WCHAR> name{};

        ULONG nameLength;
        void const* hash;
        ULONG hashLength;
        DWORD flags;
        HRESULT hr = import->GetFileProps(mdf, name.data(), (ULONG)name.size(), &nameLength, &hash, &hashLength, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(HashCharArray(name, nameLength));
            values.push_back((size_t)nameLength);
            values.push_back(HashByteArray(hash, hashLength));
            values.push_back(hashLength);
            values.push_back(flags);
        }
        return values;
    }

    std::vector<uint32_t> GetExportedTypeProps(IMetaDataAssemblyImport* import, mdFile mdf, std::vector<WCHAR>* nameBuffer = nullptr, uint32_t* implementationToken = nullptr)
    {
        std::vector<uint32_t> values;
        static_char_buffer<WCHAR> name{};

        ULONG nameLength;
        mdToken implementation;
        mdTypeDef typeDef;
        DWORD flags;
        HRESULT hr = import->GetExportedTypeProps(mdf, name.data(), (ULONG)name.size(), &nameLength, &implementation, &typeDef, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(HashCharArray(name, nameLength));
            values.push_back(nameLength);
            values.push_back(implementation);
            values.push_back(typeDef);
            values.push_back(flags);

            if (nameBuffer != nullptr)
                *nameBuffer = { std::begin(name), std::begin(name) + nameLength };
            if (implementationToken != nullptr)
                *implementationToken = implementation;
        }
        return values;
    }

    std::vector<uint32_t> GetManifestResourceProps(IMetaDataAssemblyImport* import, mdManifestResource mmr, std::vector<WCHAR>* nameBuffer = nullptr)
    {
        std::vector<uint32_t> values;
        static_char_buffer<WCHAR> name{};

        ULONG nameLength;
        ULONG offset;
        mdToken implementation;
        DWORD flags;
        HRESULT hr = import->GetManifestResourceProps(mmr, name.data(), (ULONG)name.size(), &nameLength, &implementation, &offset, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(HashCharArray(name, nameLength));
            values.push_back(nameLength);
            values.push_back(implementation);
            values.push_back(flags);

            if (nameBuffer != nullptr)
                *nameBuffer = { std::begin(name), std::begin(name) + nameLength };
        }
        return values;
    }

    std::vector<uint32_t> ResetEnum(IMetaDataImport2* import)
    {
        // We are going to test the ResetEnum() API using the
        // EnumMembers() API because it enumerates more than one table.
        std::vector<uint32_t> tokens;
        auto typedefs = EnumTypeDefs(import);
        if (typedefs.size() == 0)
            return tokens;

        auto tk = typedefs[0];
        HCORENUM hcorenum{};
        try
        {
            static auto ReadInMembers = [](IMetaDataImport2* import, HCORENUM& hcorenum, mdToken tk, std::vector<uint32_t>& tokens)
            {
                static_enum_buffer<uint32_t> tokensBuffer{};
                ULONG returned;
                if (0 == import->EnumMembers(&hcorenum, tk, tokensBuffer.data(), (ULONG)tokensBuffer.size(), &returned)
                    && returned != 0)
                {
                    for (ULONG i = 0; i < returned; ++i)
                        tokens.push_back(tokensBuffer[i]);
                }
            };

            ReadInMembers(import, hcorenum, tk, tokens);

            // Determine how many we have and move to right before end
            ULONG count;
            EXPECT_HRESULT_SUCCEEDED(import->CountEnum(hcorenum, &count));
            if (count != 0)
            {
                EXPECT_HRESULT_SUCCEEDED(import->ResetEnum(hcorenum, count - 1));
                ReadInMembers(import, hcorenum, tk, tokens);

                // Fully reset the enum
                EXPECT_HRESULT_SUCCEEDED(import->ResetEnum(hcorenum, 0));
                ReadInMembers(import, hcorenum, tk, tokens);
            }
        }
        catch (...)
        {
            import->CloseEnum(hcorenum);
            throw;
        }
        return tokens;
    }
}

TEST(FindTest, FindAPIs)
{
    malloc_span<uint8_t> metadata = GetRegressionAssemblyMetadata();

    minipal::com_ptr<IMetaDataImport2> baselineImport;
    ASSERT_HRESULT_SUCCEEDED(CreateImport(TestBaseline::Metadata, metadata, (uint32_t)metadata.size(), &baselineImport));
    // Load metadata
    minipal::com_ptr<IMetaDataImport2> currentImport;

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_HRESULT_SUCCEEDED(GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));

    ASSERT_HRESULT_SUCCEEDED(CreateImport(dispenser, metadata, (uint32_t)metadata.size(), &currentImport));

    static auto FindTokenByName = [](IMetaDataImport2* import, LPCWSTR name, mdToken enclosing = mdTokenNil) -> mdToken
    {
        mdTypeDef ptd;
        EXPECT_HRESULT_SUCCEEDED(import->FindTypeDefByName(name, enclosing, &ptd));
        return ptd;
    };

    static auto GetTypeDefBaseToken = [](IMetaDataImport2* import, mdTypeDef tk) -> mdToken
    {
        static_char_buffer<WCHAR> name{};
        ULONG pchTypeDef;
        DWORD pdwTypeDefFlags;
        mdToken ptkExtends;
        EXPECT_HRESULT_SUCCEEDED(import->GetTypeDefProps(tk,
            name.data(),
            (ULONG)name.size(),
            &pchTypeDef,
            &pdwTypeDefFlags,
            &ptkExtends));
        return ptkExtends;
    };

    static auto FindMethodDef = [](IMetaDataImport2* import, mdTypeDef type, LPCWSTR methodName) -> mdToken
    {
        std::vector<uint32_t> methoddefs = EnumMembersWithName(import, type, methodName);
        EXPECT_TRUE(!methoddefs.empty());
        return methoddefs[0];
    };

    static auto FindMemberRef = [](IMetaDataImport2* import, mdTypeDef type, LPCWSTR methodName) -> mdToken
    {
        auto methodDef = FindMethodDef(import, type, methodName);
        mdMemberRef pmr;
        EXPECT_HRESULT_SUCCEEDED(import->FindMemberRef(methodDef, methodName, nullptr, 0, &pmr));
        return pmr;
    };

    static auto FindMethod = [](IMetaDataImport2* import, mdTypeDef td, LPCWSTR name, void const* pvSigBlob, ULONG cbSigBlob) -> uint32_t
    {
        mdMethodDef tkMethod;
        mdToken tkMember;
        EXPECT_HRESULT_SUCCEEDED(import->FindMethod(td, name, (PCCOR_SIGNATURE)pvSigBlob, cbSigBlob, &tkMethod));
        EXPECT_HRESULT_SUCCEEDED(import->FindMember(td, name, (PCCOR_SIGNATURE)pvSigBlob, cbSigBlob, &tkMember));
        EXPECT_EQ(tkMethod, tkMember);
        return tkMethod;
    };

    static auto FindField = [](IMetaDataImport2* import, mdTypeDef td, LPCWSTR name, void const* pvSigBlob, ULONG cbSigBlob) -> uint32_t
    {
        mdFieldDef tkField;
        mdToken tkMember;
        EXPECT_HRESULT_SUCCEEDED(import->FindField(td, name, (PCCOR_SIGNATURE)pvSigBlob, cbSigBlob, &tkField));
        EXPECT_HRESULT_SUCCEEDED(import->FindMember(td, name, (PCCOR_SIGNATURE)pvSigBlob, cbSigBlob, &tkMember));
        EXPECT_EQ(tkField, tkMember);
        return tkField;
    };

    auto tgt = W("C");

    auto baseTypeDef = W("B1");
    auto tkB1 = EXPECT_THAT_AND_RETURN(FindTokenByName(baselineImport, baseTypeDef), testing::Eq(FindTokenByName(currentImport, baseTypeDef)));
    auto tkB1Base = EXPECT_THAT_AND_RETURN(GetTypeDefBaseToken(baselineImport, tkB1), testing::Eq(GetTypeDefBaseToken(currentImport, tkB1)));
    ASSERT_EQ(FindTypeDefByName(baselineImport, tgt, tkB1Base), FindTypeDefByName(currentImport, tgt, tkB1Base));

    auto baseTypeRef = W("B2");
    auto tkB2 = EXPECT_THAT_AND_RETURN(FindTokenByName(baselineImport, baseTypeRef), testing::Eq(FindTokenByName(currentImport, baseTypeRef)));
    auto tkB2Base = EXPECT_THAT_AND_RETURN(GetTypeDefBaseToken(baselineImport, tkB2), testing::Eq(GetTypeDefBaseToken(currentImport, tkB2)));
    ASSERT_THAT(FindTypeDefByName(baselineImport, tgt, tkB2Base), testing::Eq(FindTypeDefByName(currentImport, tgt, tkB2Base)));

    auto methodDefName = W("MethodDef");
    auto tkMethodDef = EXPECT_THAT_AND_RETURN(FindMethodDef(baselineImport, tkB1Base, methodDefName), testing::Eq(FindMethodDef(currentImport, tkB1Base, methodDefName)));

    void const* defSigBlob;
    ULONG defSigBlobLength;
    ASSERT_EQ(
        GetMethodProps(baselineImport, tkMethodDef),
        GetMethodProps(currentImport, tkMethodDef, &defSigBlob, &defSigBlobLength));
    ASSERT_EQ(
        FindMethod(baselineImport, tkB1Base, methodDefName, defSigBlob, defSigBlobLength),
        FindMethod(currentImport, tkB1Base, methodDefName, defSigBlob, defSigBlobLength));

    auto methodRef1Name = W("MethodRef1");
    auto tkMemberRefNoVarArgsBase = EXPECT_THAT_AND_RETURN(
        FindMemberRef(baselineImport, tkB1Base, methodRef1Name),
        testing::Eq(FindMemberRef(currentImport, tkB1Base, methodRef1Name)));

    PCCOR_SIGNATURE ref1Blob;
    ULONG ref1BlobLength;
    ASSERT_EQ(
        GetMemberRefProps(baselineImport, tkMemberRefNoVarArgsBase),
        GetMemberRefProps(currentImport, tkMemberRefNoVarArgsBase, &ref1Blob, &ref1BlobLength));
    ASSERT_EQ(
        FindMethod(baselineImport, tkB1Base, methodRef1Name, ref1Blob, ref1BlobLength),
        FindMethod(currentImport, tkB1Base, methodRef1Name, ref1Blob, ref1BlobLength));

    auto methodRef2Name = W("MethodRef2");
    auto tkMemberRefVarArgsBase = EXPECT_THAT_AND_RETURN(
        FindMemberRef(baselineImport, tkB1Base, methodRef2Name),
        testing::Eq(FindMemberRef(currentImport, tkB1Base, methodRef2Name)));

    PCCOR_SIGNATURE ref2Blob;
    ULONG ref2BlobLength;
    ASSERT_EQ(
        GetMemberRefProps(baselineImport, tkMemberRefVarArgsBase),
        GetMemberRefProps(currentImport, tkMemberRefVarArgsBase, &ref2Blob, &ref2BlobLength));
    EXPECT_EQ(
        FindMethod(baselineImport, tkB1Base, methodRef2Name, ref2Blob, ref2BlobLength),
        FindMethod(currentImport, tkB1Base, methodRef2Name, ref2Blob, ref2BlobLength));

    auto fieldName = W("Field1");
    auto tkFields = EXPECT_THAT_AND_RETURN(
        EnumFieldsWithName(baselineImport, tkB2, fieldName),
        testing::ElementsAreArray(EnumFieldsWithName(currentImport, tkB2, fieldName)));
    ASSERT_FALSE(tkFields.empty());
    mdToken tkField = tkFields[0];

    void const* sigBlob;
    ULONG sigBlobLength;
    ASSERT_EQ(
        GetFieldProps(baselineImport, tkField),
        GetFieldProps(currentImport, tkField, &sigBlob, &sigBlobLength));
    EXPECT_EQ(
        FindField(baselineImport, tkB2, fieldName, sigBlob, sigBlobLength),
        FindField(currentImport, tkB2, fieldName, sigBlob, sigBlobLength));
}

class MetadataImportTest : public RegressionTest
{
};

TEST_P(MetadataImportTest, ImportAPIs)
{
    auto param = GetParam();
    span<uint8_t> blob = GetMetadataForFile(param);
    void const* data = blob;
    uint32_t dataLen = (uint32_t)blob.size();

    // Load metadata
    minipal::com_ptr<IMetaDataImport2> baselineImport;
    ASSERT_HRESULT_SUCCEEDED(CreateImport(TestBaseline::Metadata, data, dataLen, &baselineImport));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_HRESULT_SUCCEEDED(GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataImport2> currentImport;
    ASSERT_HRESULT_SUCCEEDED(CreateImport(dispenser, data, dataLen, &currentImport));

    // Verify APIs
    ASSERT_THAT(ResetEnum(currentImport), testing::ElementsAreArray(ResetEnum(baselineImport)));
    ASSERT_THAT(GetScopeProps(currentImport), testing::ElementsAreArray(GetScopeProps(baselineImport)));
    ASSERT_THAT(GetVersionString(currentImport), testing::ElementsAreArray(GetVersionString(baselineImport)));

    TokenList sigs;
    ASSERT_EQUAL_AND_SET(sigs, EnumSignatures(baselineImport), EnumSignatures(currentImport));
    for (auto sig : sigs)
    {
        ASSERT_THAT(GetSigFromToken(currentImport, sig), testing::ElementsAreArray(GetSigFromToken(baselineImport, sig)));
    }

    TokenList userStrings;
    ASSERT_EQUAL_AND_SET(userStrings, EnumUserStrings(baselineImport), EnumUserStrings(currentImport));
    for (auto us : userStrings)
    {
        ASSERT_THAT(GetUserString(currentImport, us), testing::ElementsAreArray(GetUserString(baselineImport, us)));
    }

    TokenList custAttrs;
    ASSERT_EQUAL_AND_SET(custAttrs, EnumCustomAttributes(baselineImport), EnumCustomAttributes(currentImport));
    for (auto ca : custAttrs)
    {
        ASSERT_THAT(GetCustomAttributeProps(currentImport, ca), testing::ElementsAreArray(GetCustomAttributeProps(baselineImport, ca)));
    }

    TokenList modulerefs;
    ASSERT_EQUAL_AND_SET(modulerefs, EnumModuleRefs(baselineImport), EnumModuleRefs(currentImport));
    for (auto moduleref : modulerefs)
    {
        ASSERT_THAT(GetModuleRefProps(currentImport, moduleref), testing::ElementsAreArray(GetModuleRefProps(baselineImport, moduleref)));
        ASSERT_THAT(GetNameFromToken(currentImport, moduleref), testing::ElementsAreArray(GetNameFromToken(baselineImport, moduleref)));
    }

    ASSERT_THAT(FindTypeRef(currentImport), testing::ElementsAreArray(FindTypeRef(baselineImport)));
    TokenList typerefs;
    ASSERT_EQUAL_AND_SET(typerefs, EnumTypeRefs(baselineImport), EnumTypeRefs(currentImport));
    for (auto typeref : typerefs)
    {
        ASSERT_THAT(GetTypeRefProps(currentImport, typeref), testing::ElementsAreArray(GetTypeRefProps(baselineImport, typeref)));
        ASSERT_THAT(GetCustomAttribute_CompilerGenerated(currentImport, typeref), testing::ElementsAreArray(GetCustomAttribute_CompilerGenerated(baselineImport, typeref)));
        ASSERT_THAT(GetNameFromToken(currentImport, typeref), testing::ElementsAreArray(GetNameFromToken(baselineImport, typeref)));
    }

    TokenList typespecs;
    ASSERT_EQUAL_AND_SET(typespecs, EnumTypeSpecs(baselineImport), EnumTypeSpecs(currentImport));
    for (auto typespec : typespecs)
    {
        ASSERT_THAT(GetTypeSpecFromToken(currentImport, typespec), testing::ElementsAreArray(GetTypeSpecFromToken(baselineImport, typespec)));
        ASSERT_THAT(GetCustomAttribute_CompilerGenerated(currentImport, typespec), testing::ElementsAreArray(GetCustomAttribute_CompilerGenerated(baselineImport, typespec)));
    }

    TokenList typedefs;
    ASSERT_EQUAL_AND_SET(typedefs, EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
    for (auto typdef : typedefs)
    {
        ASSERT_THAT(GetTypeDefProps(currentImport, typdef), testing::ElementsAreArray(GetTypeDefProps(baselineImport, typdef)));
        ASSERT_THAT(GetNameFromToken(currentImport, typdef), testing::ElementsAreArray(GetNameFromToken(baselineImport, typdef)));
        ASSERT_THAT(IsGlobal(currentImport, typdef), testing::Eq(IsGlobal(baselineImport, typdef)));
        ASSERT_THAT(EnumInterfaceImpls(currentImport, typdef), testing::ElementsAreArray(EnumInterfaceImpls(baselineImport, typdef)));
        ASSERT_THAT(EnumPermissionSetsAndGetProps(currentImport, typdef), testing::ElementsAreArray(EnumPermissionSetsAndGetProps(baselineImport, typdef)));
        ASSERT_THAT(EnumMembers(currentImport, typdef), testing::ElementsAreArray(EnumMembers(baselineImport, typdef)));
        ASSERT_THAT(EnumMembersWithName(currentImport, typdef), testing::ElementsAreArray(EnumMembersWithName(baselineImport, typdef)));
        ASSERT_THAT(EnumMethodsWithName(currentImport, typdef), testing::ElementsAreArray(EnumMethodsWithName(baselineImport, typdef)));
        ASSERT_THAT(EnumMethodImpls(currentImport, typdef), testing::ElementsAreArray(EnumMethodImpls(baselineImport, typdef)));
        ASSERT_THAT(GetNestedClassProps(currentImport, typdef), testing::ElementsAreArray(GetNestedClassProps(baselineImport, typdef)));
        ASSERT_THAT(GetClassLayout(currentImport, typdef), testing::ElementsAreArray(GetClassLayout(baselineImport, typdef)));
        ASSERT_THAT(GetCustomAttribute_CompilerGenerated(currentImport, typdef), testing::ElementsAreArray(GetCustomAttribute_CompilerGenerated(baselineImport, typdef)));

        TokenList methoddefs;
        ASSERT_EQUAL_AND_SET(methoddefs, EnumMethods(baselineImport, typdef), EnumMethods(currentImport, typdef));
        for (auto methoddef : methoddefs)
        {
            void const* sig = nullptr;
            ULONG sigLen = 0;
            ASSERT_THAT(GetMethodProps(currentImport, methoddef, &sig, &sigLen), testing::ElementsAreArray(GetMethodProps(baselineImport, methoddef)));
            ASSERT_THAT(GetNativeCallConvFromSig(currentImport, sig, sigLen), testing::ElementsAreArray(GetNativeCallConvFromSig(baselineImport, sig, sigLen)));
            ASSERT_THAT(GetNameFromToken(currentImport, methoddef), testing::ElementsAreArray(GetNameFromToken(baselineImport, methoddef)));
            ASSERT_THAT(IsGlobal(currentImport, methoddef), testing::Eq(IsGlobal(baselineImport, methoddef)));
            ASSERT_THAT(GetCustomAttribute_CompilerGenerated(currentImport, methoddef), testing::ElementsAreArray(GetCustomAttribute_CompilerGenerated(baselineImport, methoddef)));

            TokenList paramdefs;
            ASSERT_EQUAL_AND_SET(paramdefs, EnumParams(baselineImport, methoddef), EnumParams(currentImport, methoddef));
            for (auto paramdef : paramdefs)
            {
                ASSERT_THAT(GetParamProps(currentImport, paramdef), testing::ElementsAreArray(GetParamProps(baselineImport, paramdef)));
                ASSERT_THAT(GetFieldMarshal(currentImport, paramdef), testing::ElementsAreArray(GetFieldMarshal(baselineImport, paramdef)));
                ASSERT_THAT(GetCustomAttribute_Nullable(currentImport, paramdef), testing::ElementsAreArray(GetCustomAttribute_Nullable(baselineImport, paramdef)));
                ASSERT_THAT(GetNameFromToken(currentImport, paramdef), testing::ElementsAreArray(GetNameFromToken(baselineImport, paramdef)));
            }

            ASSERT_THAT(GetParamForMethodIndex(currentImport, methoddef), testing::ElementsAreArray(GetParamForMethodIndex(baselineImport, methoddef)));
            ASSERT_THAT(EnumPermissionSetsAndGetProps(currentImport, methoddef), testing::ElementsAreArray(EnumPermissionSetsAndGetProps(baselineImport, methoddef)));
            ASSERT_THAT(GetPinvokeMap(currentImport, methoddef), testing::ElementsAreArray(GetPinvokeMap(baselineImport, methoddef)));
            ASSERT_THAT(GetRVA(currentImport, methoddef), testing::ElementsAreArray(GetRVA(baselineImport, methoddef)));

            TokenList methodspecs;
            ASSERT_EQUAL_AND_SET(methodspecs, EnumMethodSpecs(baselineImport, methoddef), EnumMethodSpecs(currentImport, methoddef));
            for (auto methodspec : methodspecs)
            {
                ASSERT_THAT(GetMethodSpecProps(currentImport, methodspec), testing::ElementsAreArray(GetMethodSpecProps(baselineImport, methodspec)));
            }
        }

        TokenList eventdefs;
        ASSERT_EQUAL_AND_SET(eventdefs, EnumEvents(baselineImport, typdef), EnumEvents(currentImport, typdef));
        for (auto eventdef : eventdefs)
        {
            std::vector<mdMethodDef> mds;
            ASSERT_THAT(GetEventProps(currentImport, eventdef, &mds), testing::ElementsAreArray(GetEventProps(baselineImport, eventdef)));
            for (auto md : mds)
            {
                ASSERT_THAT(GetMethodSemantics(currentImport, eventdef, md), testing::ElementsAreArray(GetMethodSemantics(baselineImport, eventdef, md)));
            }

            ASSERT_THAT(GetNameFromToken(currentImport, eventdef), testing::ElementsAreArray(GetNameFromToken(baselineImport, eventdef)));
            ASSERT_THAT(IsGlobal(currentImport, eventdef), testing::Eq(IsGlobal(baselineImport, eventdef)));
        }

        TokenList properties;
        ASSERT_EQUAL_AND_SET(properties, EnumProperties(baselineImport, typdef), EnumProperties(currentImport, typdef));
        for (auto props : properties)
        {
            std::vector<mdMethodDef> mds;
            ASSERT_THAT(GetPropertyProps(currentImport, props, &mds), testing::ElementsAreArray(GetPropertyProps(baselineImport, props)));
            for (auto md : mds)
            {
                ASSERT_THAT(GetMethodSemantics(currentImport, props, md), testing::ElementsAreArray(GetMethodSemantics(baselineImport, props, md)));
            }

            ASSERT_THAT(GetNameFromToken(currentImport, props), testing::ElementsAreArray(GetNameFromToken(baselineImport, props)));
            ASSERT_THAT(IsGlobal(currentImport, props), testing::Eq(IsGlobal(baselineImport, props)));
        }

        ASSERT_THAT(EnumFieldsWithName(baselineImport, typdef), EnumFieldsWithName(currentImport, typdef));
        TokenList fielddefs;
        ASSERT_EQUAL_AND_SET(fielddefs, EnumFields(baselineImport, typdef), EnumFields(currentImport, typdef));
        for (auto fielddef : fielddefs)
        {
            ASSERT_THAT(GetFieldProps(currentImport, fielddef), testing::ElementsAreArray(GetFieldProps(baselineImport, fielddef)));
            ASSERT_THAT(GetNameFromToken(currentImport, fielddef), testing::ElementsAreArray(GetNameFromToken(baselineImport, fielddef)));
            ASSERT_THAT(IsGlobal(currentImport, fielddef), testing::Eq(IsGlobal(baselineImport, fielddef)));
            ASSERT_THAT(GetPinvokeMap(currentImport, fielddef), testing::ElementsAreArray(GetPinvokeMap(baselineImport, fielddef)));
            ASSERT_THAT(GetRVA(currentImport, fielddef), testing::ElementsAreArray(GetRVA(baselineImport, fielddef)));
            ASSERT_THAT(GetFieldMarshal(currentImport, fielddef), testing::ElementsAreArray(GetFieldMarshal(baselineImport, fielddef)));
            ASSERT_THAT(GetCustomAttribute_Nullable(currentImport, fielddef), testing::ElementsAreArray(GetCustomAttribute_Nullable(baselineImport, fielddef)));
        }

        TokenList genparams;
        ASSERT_EQUAL_AND_SET(genparams, EnumGenericParams(baselineImport, typdef), EnumGenericParams(currentImport, typdef));
        for (auto genparam : genparams)
        {
            ASSERT_THAT(GetGenericParamProps(currentImport, genparam), testing::ElementsAreArray(GetGenericParamProps(baselineImport, genparam)));
            TokenList genparamconsts;
            ASSERT_EQUAL_AND_SET(genparamconsts, EnumGenericParamConstraints(baselineImport, genparam), EnumGenericParamConstraints(currentImport, genparam));
            for (auto genparamconst : genparamconsts)
            {
                ASSERT_THAT(GetGenericParamConstraintProps(currentImport, genparamconst), testing::ElementsAreArray(GetGenericParamConstraintProps(baselineImport, genparamconst)));
            }
        }
    }

    minipal::com_ptr<IMetaDataAssemblyImport> baselineAssembly;
    ASSERT_THAT(S_OK, baselineImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&baselineAssembly));
    minipal::com_ptr<IMetaDataAssemblyImport> currentAssembly;
    ASSERT_THAT(S_OK, currentImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&currentAssembly));

    TokenList assemblyTokens;
    ASSERT_EQUAL_AND_SET(assemblyTokens, GetAssemblyFromScope(baselineAssembly), GetAssemblyFromScope(currentAssembly));
    for (auto assembly : assemblyTokens)
    {
        ASSERT_THAT(GetAssemblyProps(currentAssembly, assembly), testing::ElementsAreArray(GetAssemblyProps(baselineAssembly, assembly)));
    }

    TokenList assemblyRefs;
    ASSERT_EQUAL_AND_SET(assemblyRefs, EnumAssemblyRefs(baselineAssembly), EnumAssemblyRefs(currentAssembly));
    for (auto assemblyRef : assemblyRefs)
    {
        ASSERT_THAT(GetAssemblyRefProps(currentAssembly, assemblyRef), testing::ElementsAreArray(GetAssemblyRefProps(baselineAssembly, assemblyRef)));
    }

    TokenList files;
    ASSERT_EQUAL_AND_SET(files, EnumFiles(baselineAssembly), EnumFiles(currentAssembly));
    for (auto file : files)
    {
        ASSERT_THAT(GetFileProps(currentAssembly, file), testing::ElementsAreArray(GetFileProps(baselineAssembly, file)));
    }

    TokenList exports;
    ASSERT_EQUAL_AND_SET(exports, EnumExportedTypes(baselineAssembly), EnumExportedTypes(currentAssembly));
    for (auto exportedType : exports)
    {
        std::vector<WCHAR> name;
        uint32_t implementation = mdTokenNil;
        ASSERT_THAT(GetExportedTypeProps(currentAssembly, exportedType, &name, &implementation), testing::ElementsAreArray(GetExportedTypeProps(baselineAssembly, exportedType)));
        ASSERT_THAT(
            FindExportedTypeByName(currentAssembly, name.data(), implementation),
            testing::ElementsAreArray(FindExportedTypeByName(baselineAssembly, name.data(), implementation)));
    }

    TokenList resources;
    ASSERT_EQUAL_AND_SET(resources, EnumManifestResources(baselineAssembly), EnumManifestResources(currentAssembly));
    for (auto resource : resources)
    {
        std::vector<WCHAR> name;
        ASSERT_THAT(GetManifestResourceProps(currentAssembly, resource, &name), testing::ElementsAreArray(GetManifestResourceProps(baselineAssembly, resource)));
        ASSERT_THAT(FindManifestResourceByName(currentAssembly, name.data()), testing::ElementsAreArray(FindManifestResourceByName(baselineAssembly, name.data())));
    }
}

INSTANTIATE_TEST_SUITE_P(MetaDataImportTestCore, MetadataImportTest, testing::ValuesIn(MetadataFilesInDirectory(GetBaselineDirectory())), PrintName);

INSTANTIATE_TEST_SUITE_P(MetaDataImportTestFx4_0, MetadataImportTest, testing::ValuesIn(MetadataFilesInDirectory(FindFrameworkInstall(X("v4.0.30319")))), PrintName);
INSTANTIATE_TEST_SUITE_P(MetaDataImportTestFx2_0, MetadataImportTest, testing::ValuesIn(MetadataFilesInDirectory(FindFrameworkInstall(X("v2.0.50727")))), PrintName);

INSTANTIATE_TEST_SUITE_P(MetaDataImportTest_IndirectionTables, MetadataImportTest, testing::Values(MetadataFile{ MetadataFile::Kind::Generated, IndirectionTablesKey }), PrintName);

class MetaDataLongRunningTest : public RegressionTest
{
};

TEST_P(MetaDataLongRunningTest, ImportAPIs)
{
    auto param = GetParam();
    span<uint8_t> blob = GetMetadataForFile(param);
    void const* data = blob;
    uint32_t dataLen = (uint32_t)blob.size();

    // Load metadata
    minipal::com_ptr<IMetaDataImport2> baselineImport;
    ASSERT_HRESULT_SUCCEEDED(CreateImport(TestBaseline::Metadata, data, dataLen, &baselineImport));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_HRESULT_SUCCEEDED(GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataImport2> currentImport;
    ASSERT_HRESULT_SUCCEEDED(CreateImport(dispenser, data, dataLen, &currentImport));

    static auto VerifyFindMemberRef = [](IMetaDataImport2 * import, mdToken memberRef) -> std::vector<uint32_t>
        {
            std::vector<uint32_t> values;

            mdToken ptk;
            static_char_buffer<WCHAR> name{};
            ULONG pchMember;
            PCCOR_SIGNATURE ppvSigBlob;
            ULONG pcbSigBlob;
            HRESULT hr = import->GetMemberRefProps(memberRef,
                & ptk,
                name.data(),
                (ULONG)name.size(),
                & pchMember,
                & ppvSigBlob,
                & pcbSigBlob);
            values.push_back(hr);
            if (hr == S_OK)
            {
                // We were able to get the name, now try looking up a memberRef by name and by sig
                mdMemberRef lookup = mdTokenNil;
                hr = import->FindMemberRef(ptk, name.data(), ppvSigBlob, pcbSigBlob, & lookup);
                values.push_back(hr);
                values.push_back(lookup);
                lookup = mdTokenNil;
                hr = import->FindMemberRef(ptk, name.data(), nullptr, 0, & lookup);
                values.push_back(hr);
                values.push_back(lookup);
                lookup = mdTokenNil;
                hr = import->FindMemberRef(ptk, nullptr, ppvSigBlob, pcbSigBlob, & lookup);
                values.push_back(hr);
                values.push_back(lookup);
            }
            return values;
        };

    size_t stride;
    size_t count;

    TokenList typedefs;
    ASSERT_EQUAL_AND_SET(typedefs, EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
    count = 0;
    stride = std::max(typedefs.size() / 128, (size_t)16);
    for (auto typdef : typedefs)
    {
        if (count++ % stride != 0)
            continue;

        EXPECT_THAT(EnumMemberRefs(currentImport, typdef), testing::ElementsAreArray(EnumMemberRefs(baselineImport, typdef)));

        TokenList methoddefs;
        ASSERT_EQUAL_AND_SET(methoddefs, EnumMethods(baselineImport, typdef), EnumMethods(currentImport, typdef));
        for (auto methoddef : methoddefs)
        {
            EXPECT_THAT(EnumMethodSemantics(currentImport, methoddef), testing::ElementsAreArray(EnumMethodSemantics(baselineImport, methoddef)));
        }

        EXPECT_THAT(EnumCustomAttributes(currentImport, typdef), testing::ElementsAreArray(EnumCustomAttributes(baselineImport, typdef)));
    }

    TokenList typespecs;
    ASSERT_EQUAL_AND_SET(typespecs, EnumTypeSpecs(baselineImport), EnumTypeSpecs(currentImport));
    count = 0;
    stride = std::max(typespecs.size() / 128, (size_t)16);
    for (auto typespec : typespecs)
    {
        if (count++ % stride != 0)
            continue;

        TokenList memberrefs;
        ASSERT_EQUAL_AND_SET(memberrefs, EnumMemberRefs(baselineImport, typespec), EnumMemberRefs(currentImport, typespec));
        for (auto memberref : memberrefs)
        {
            EXPECT_THAT(GetMemberRefProps(currentImport, memberref), testing::ElementsAreArray(GetMemberRefProps(baselineImport, memberref)));
            EXPECT_THAT(VerifyFindMemberRef(currentImport, memberref), testing::ElementsAreArray(VerifyFindMemberRef(baselineImport, memberref)));
        }
    }
}

INSTANTIATE_TEST_SUITE_P(MetaDataLongRunningTest_CoreLibs, MetaDataLongRunningTest, testing::ValuesIn(CoreLibFiles()), PrintName);
