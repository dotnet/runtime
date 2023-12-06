#include "regnative.hpp"

#include <array>
#include <vector>
#include <algorithm>
#include <functional>
#include <string>
#include <sstream>
#include <limits>

namespace
{
    IMetaDataDispenser* g_baselineDisp;
    IMetaDataDispenser* g_deltaImageBuilder;
    IMetaDataDispenser* g_currentDisp;

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

    HRESULT CreateEmit(IMetaDataDispenser* disp, void const* data, uint32_t dataLen, IMetaDataEmit2** emit)
    {
        assert(disp != nullptr && data != nullptr && dataLen > 0 && emit != nullptr);
        return disp->OpenScopeOnMemory(
            data,
            dataLen,
            CorOpenFlags::ofWrite,
            IID_IMetaDataEmit2,
            reinterpret_cast<IUnknown**>(emit));
    }
}

EXPORT
HRESULT UnitInitialize(IMetaDataDispenser* baseline, IMetaDataDispenserEx* deltaBuilder)
{
    HRESULT hr;
    if (baseline == nullptr)
        return E_INVALIDARG;
    
    if (deltaBuilder == nullptr)
        return E_INVALIDARG;

    (void)baseline->AddRef();
    g_baselineDisp = baseline;

    (void)deltaBuilder->AddRef();
    
    // We need to set the ENC mode on the delta builder to get it to apply EnC deltas
    // and produce images with the indirection tables.
    VARIANT vt;
    V_VT(&vt) = VT_UI4;
    V_UI4(&vt) = MDUpdateExtension;
    if (FAILED(hr = deltaBuilder->SetOption(MetaDataSetENC, &vt)))
        return hr;

    g_deltaImageBuilder = deltaBuilder;

    if (FAILED(hr = GetDispenser(IID_IMetaDataDispenser, reinterpret_cast<void**>(&g_currentDisp))))
        return hr;

    return S_OK;
}

namespace
{
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
        ASSERT_EQUAL(S_OK, import->CountEnum(hcorenum, &count));
        ASSERT_EQUAL(count, expectedCount);
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
        dncp::com_ptr<IMetaDataImport2> mdImport;
        HRESULT hr = import->QueryInterface(IID_IMetaDataImport2, (void**)&mdImport);
        ASSERT_EQUAL(S_OK, hr);
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
        dncp::com_ptr<IMetaDataImport2>  mdImport;
        HRESULT hr = import->QueryInterface(IID_IMetaDataImport2, (void**)&mdImport);
        ASSERT_EQUAL(S_OK, hr);
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
        dncp::com_ptr<IMetaDataImport2>  mdImport;
        HRESULT hr = import->QueryInterface(IID_IMetaDataImport2, (void**)&mdImport);
        ASSERT_EQUAL(S_OK, hr);
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
        dncp::com_ptr<IMetaDataImport2>  mdImport;
        HRESULT hr = import->QueryInterface(IID_IMetaDataImport2, (void**)&mdImport);
        ASSERT_EQUAL(S_OK, hr);
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
            values.push_back(reserved);
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
            values.push_back(hashLength != 0 ? (size_t)hash : 0);
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
            ASSERT_EQUAL(S_OK, import->CountEnum(hcorenum, &count));
            if (count != 0)
            {
                ASSERT_EQUAL(S_OK, import->ResetEnum(hcorenum, count - 1));
                ReadInMembers(import, hcorenum, tk, tokens);

                // Fully reset the enum
                ASSERT_EQUAL(S_OK, import->ResetEnum(hcorenum, 0));
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

EXPORT
TestResult UnitImportAPIs(void const* data, uint32_t dataLen)
{
    BEGIN_TEST();

    // Load metadata
    dncp::com_ptr<IMetaDataImport2> baselineImport;
    ASSERT_EQUAL(S_OK, CreateImport(g_baselineDisp, data, dataLen, &baselineImport));
    dncp::com_ptr<IMetaDataImport2> currentImport;
    ASSERT_EQUAL(S_OK, CreateImport(g_currentDisp, data, dataLen, &currentImport));

    // Verify APIs
    ASSERT_EQUAL(ResetEnum(baselineImport), ResetEnum(currentImport));
    ASSERT_EQUAL(GetScopeProps(baselineImport), GetScopeProps(currentImport));
    ASSERT_EQUAL(GetVersionString(baselineImport), GetVersionString(currentImport));

    auto sigs = ASSERT_AND_RETURN(EnumSignatures(baselineImport), EnumSignatures(currentImport));
    for (auto sig : sigs)
    {
        ASSERT_EQUAL(GetSigFromToken(baselineImport, sig), GetSigFromToken(currentImport, sig));
    }

    auto userStrings = ASSERT_AND_RETURN(EnumUserStrings(baselineImport), EnumUserStrings(currentImport));
    for (auto us : userStrings)
    {
        ASSERT_EQUAL(GetUserString(baselineImport, us), GetUserString(currentImport, us));
    }

    auto custAttrs = ASSERT_AND_RETURN(EnumCustomAttributes(baselineImport), EnumCustomAttributes(currentImport));
    for (auto ca : custAttrs)
    {
        ASSERT_EQUAL(GetCustomAttributeProps(baselineImport, ca), GetCustomAttributeProps(currentImport, ca));
    }

    auto modulerefs = ASSERT_AND_RETURN(EnumModuleRefs(baselineImport), EnumModuleRefs(currentImport));
    for (auto moduleref : modulerefs)
    {
        ASSERT_EQUAL(GetModuleRefProps(baselineImport, moduleref), GetModuleRefProps(currentImport, moduleref));
        ASSERT_EQUAL(GetNameFromToken(baselineImport, moduleref), GetNameFromToken(currentImport, moduleref));
    }

    ASSERT_EQUAL(FindTypeRef(baselineImport), FindTypeRef(currentImport));
    auto typerefs = ASSERT_AND_RETURN(EnumTypeRefs(baselineImport), EnumTypeRefs(currentImport));
    for (auto typeref : typerefs)
    {
        ASSERT_EQUAL(GetTypeRefProps(baselineImport, typeref), GetTypeRefProps(currentImport, typeref));
        ASSERT_EQUAL(GetCustomAttribute_CompilerGenerated(baselineImport, typeref), GetCustomAttribute_CompilerGenerated(currentImport, typeref));
        ASSERT_EQUAL(GetNameFromToken(baselineImport, typeref), GetNameFromToken(currentImport, typeref));
    }

    auto typespecs = ASSERT_AND_RETURN(EnumTypeSpecs(baselineImport), EnumTypeSpecs(currentImport));
    for (auto typespec : typespecs)
    {
        ASSERT_EQUAL(GetTypeSpecFromToken(baselineImport, typespec), GetTypeSpecFromToken(currentImport, typespec));
        ASSERT_EQUAL(GetCustomAttribute_CompilerGenerated(baselineImport, typespec), GetCustomAttribute_CompilerGenerated(currentImport, typespec));
    }

    auto typedefs = ASSERT_AND_RETURN(EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
    for (auto typdef : typedefs)
    {
        ASSERT_EQUAL(GetTypeDefProps(baselineImport, typdef), GetTypeDefProps(currentImport, typdef));
        ASSERT_EQUAL(GetNameFromToken(baselineImport, typdef), GetNameFromToken(currentImport, typdef));
        ASSERT_EQUAL(IsGlobal(baselineImport, typdef), IsGlobal(currentImport, typdef));
        ASSERT_EQUAL(EnumInterfaceImpls(baselineImport, typdef), EnumInterfaceImpls(currentImport, typdef));
        ASSERT_EQUAL(EnumPermissionSetsAndGetProps(baselineImport, typdef), EnumPermissionSetsAndGetProps(currentImport, typdef));
        ASSERT_EQUAL(EnumMembers(baselineImport, typdef), EnumMembers(currentImport, typdef));
        ASSERT_EQUAL(EnumMembersWithName(baselineImport, typdef), EnumMembersWithName(currentImport, typdef));
        ASSERT_EQUAL(EnumMethodsWithName(baselineImport, typdef), EnumMethodsWithName(currentImport, typdef));
        ASSERT_EQUAL(EnumMethodImpls(baselineImport, typdef), EnumMethodImpls(currentImport, typdef));
        ASSERT_EQUAL(GetNestedClassProps(baselineImport, typdef), GetNestedClassProps(currentImport, typdef));
        ASSERT_EQUAL(GetClassLayout(baselineImport, typdef), GetClassLayout(currentImport, typdef));
        ASSERT_EQUAL(GetCustomAttribute_CompilerGenerated(baselineImport, typdef), GetCustomAttribute_CompilerGenerated(currentImport, typdef));

        auto methoddefs = ASSERT_AND_RETURN(EnumMethods(baselineImport, typdef), EnumMethods(currentImport, typdef));
        for (auto methoddef : methoddefs)
        {
            void const* sig = nullptr;
            ULONG sigLen = 0;
            ASSERT_EQUAL(GetMethodProps(baselineImport, methoddef), GetMethodProps(currentImport, methoddef, &sig, &sigLen));
            ASSERT_EQUAL(GetNativeCallConvFromSig(baselineImport, sig, sigLen), GetNativeCallConvFromSig(currentImport, sig, sigLen));
            ASSERT_EQUAL(GetNameFromToken(baselineImport, methoddef), GetNameFromToken(currentImport, methoddef));
            ASSERT_EQUAL(IsGlobal(baselineImport, methoddef), IsGlobal(currentImport, methoddef));
            ASSERT_EQUAL(GetCustomAttribute_CompilerGenerated(baselineImport, methoddef), GetCustomAttribute_CompilerGenerated(currentImport, methoddef));

            auto paramdefs = ASSERT_AND_RETURN(EnumParams(baselineImport, methoddef), EnumParams(currentImport, methoddef));
            for (auto paramdef : paramdefs)
            {
                ASSERT_EQUAL(GetParamProps(baselineImport, paramdef), GetParamProps(currentImport, paramdef));
                ASSERT_EQUAL(GetFieldMarshal(baselineImport, paramdef), GetFieldMarshal(currentImport, paramdef));
                ASSERT_EQUAL(GetCustomAttribute_Nullable(baselineImport, paramdef), GetCustomAttribute_Nullable(currentImport, paramdef));
                ASSERT_EQUAL(GetNameFromToken(baselineImport, paramdef), GetNameFromToken(currentImport, paramdef));
            }

            ASSERT_EQUAL(GetParamForMethodIndex(baselineImport, methoddef), GetParamForMethodIndex(currentImport, methoddef));
            ASSERT_EQUAL(EnumPermissionSetsAndGetProps(baselineImport, methoddef), EnumPermissionSetsAndGetProps(currentImport, methoddef));
            ASSERT_EQUAL(GetPinvokeMap(baselineImport, methoddef), GetPinvokeMap(currentImport, methoddef));
            ASSERT_EQUAL(GetRVA(baselineImport, methoddef), GetRVA(currentImport, methoddef));

            auto methodspecs = ASSERT_AND_RETURN(EnumMethodSpecs(baselineImport, methoddef), EnumMethodSpecs(currentImport, methoddef));
            for (auto methodspec : methodspecs)
            {
                ASSERT_EQUAL(GetMethodSpecProps(baselineImport, methodspec), GetMethodSpecProps(currentImport, methodspec));
            }
        }

        auto eventdefs = ASSERT_AND_RETURN(EnumEvents(baselineImport, typdef), EnumEvents(currentImport, typdef));
        for (auto eventdef : eventdefs)
        {
            std::vector<mdMethodDef> mds;
            ASSERT_EQUAL(GetEventProps(baselineImport, eventdef), GetEventProps(currentImport, eventdef, &mds));
            for (auto md : mds)
            {
                ASSERT_EQUAL(GetMethodSemantics(baselineImport, eventdef, md), GetMethodSemantics(currentImport, eventdef, md));
            }

            ASSERT_EQUAL(GetNameFromToken(baselineImport, eventdef), GetNameFromToken(currentImport, eventdef));
            ASSERT_EQUAL(IsGlobal(baselineImport, eventdef), IsGlobal(currentImport, eventdef));
        }

        auto properties = ASSERT_AND_RETURN(EnumProperties(baselineImport, typdef), EnumProperties(currentImport, typdef));
        for (auto props : properties)
        {
            std::vector<mdMethodDef> mds;
            ASSERT_EQUAL(GetPropertyProps(baselineImport, props), GetPropertyProps(currentImport, props, &mds));
            for (auto md : mds)
            {
                ASSERT_EQUAL(GetMethodSemantics(baselineImport, props, md), GetMethodSemantics(currentImport, props, md));
            }

            ASSERT_EQUAL(GetNameFromToken(baselineImport, props), GetNameFromToken(currentImport, props));
            ASSERT_EQUAL(IsGlobal(baselineImport, props), IsGlobal(currentImport, props));
        }

        ASSERT_EQUAL(EnumFieldsWithName(baselineImport, typdef), EnumFieldsWithName(currentImport, typdef));
        auto fielddefs = ASSERT_AND_RETURN(EnumFields(baselineImport, typdef), EnumFields(currentImport, typdef));
        for (auto fielddef : fielddefs)
        {
            ASSERT_EQUAL(GetFieldProps(baselineImport, fielddef), GetFieldProps(currentImport, fielddef));
            ASSERT_EQUAL(GetNameFromToken(baselineImport, fielddef), GetNameFromToken(currentImport, fielddef));
            ASSERT_EQUAL(IsGlobal(baselineImport, fielddef), IsGlobal(currentImport, fielddef));
            ASSERT_EQUAL(GetPinvokeMap(baselineImport, fielddef), GetPinvokeMap(currentImport, fielddef));
            ASSERT_EQUAL(GetRVA(baselineImport, fielddef), GetRVA(currentImport, fielddef));
            ASSERT_EQUAL(GetFieldMarshal(baselineImport, fielddef), GetFieldMarshal(currentImport, fielddef));
            ASSERT_EQUAL(GetCustomAttribute_Nullable(baselineImport, fielddef), GetCustomAttribute_Nullable(currentImport, fielddef));
        }

        auto genparams = ASSERT_AND_RETURN(EnumGenericParams(baselineImport, typdef), EnumGenericParams(currentImport, typdef));
        for (auto genparam : genparams)
        {
            ASSERT_EQUAL(GetGenericParamProps(baselineImport, genparam), GetGenericParamProps(currentImport, genparam));
            auto genparamconsts = ASSERT_AND_RETURN(EnumGenericParamConstraints(baselineImport, genparam), EnumGenericParamConstraints(currentImport, genparam));
            for (auto genparamconst : genparamconsts)
            {
                ASSERT_EQUAL(GetGenericParamConstraintProps(baselineImport, genparamconst), GetGenericParamConstraintProps(currentImport, genparamconst));
            }
        }
    }

    dncp::com_ptr<IMetaDataAssemblyImport> baselineAssembly;
    ASSERT_EQUAL(S_OK, baselineImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&baselineAssembly));
    dncp::com_ptr<IMetaDataAssemblyImport> currentAssembly;
    ASSERT_EQUAL(S_OK, currentImport->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&currentAssembly));

    auto assemblyTokens = ASSERT_AND_RETURN(GetAssemblyFromScope(baselineAssembly), GetAssemblyFromScope(currentAssembly));
    for (auto assembly : assemblyTokens)
    {
        ASSERT_EQUAL(GetAssemblyProps(baselineAssembly, assembly), GetAssemblyProps(currentAssembly, assembly));
    }

    auto assemblyRefs = ASSERT_AND_RETURN(EnumAssemblyRefs(baselineAssembly), EnumAssemblyRefs(currentAssembly));
    for (auto assemblyRef : assemblyRefs)
    {
        ASSERT_EQUAL(GetAssemblyRefProps(baselineAssembly, assemblyRef), GetAssemblyRefProps(currentAssembly, assemblyRef));
    }

    auto files = ASSERT_AND_RETURN(EnumFiles(baselineAssembly), EnumFiles(currentAssembly));
    for (auto file : files)
    {
        ASSERT_EQUAL(GetFileProps(baselineAssembly, file), GetFileProps(currentAssembly, file));
    }

    auto exports = ASSERT_AND_RETURN(EnumExportedTypes(baselineAssembly), EnumExportedTypes(currentAssembly));
    for (auto exportedType : exports)
    {
        std::vector<WCHAR> name;
        uint32_t implementation = mdTokenNil;
        ASSERT_EQUAL(GetExportedTypeProps(baselineAssembly, exportedType), GetExportedTypeProps(currentAssembly, exportedType, &name, &implementation));
        ASSERT_EQUAL(
            FindExportedTypeByName(baselineAssembly, name.data(), implementation),
            FindExportedTypeByName(currentAssembly, name.data(), implementation));
    }

    auto resources = ASSERT_AND_RETURN(EnumManifestResources(baselineAssembly), EnumManifestResources(currentAssembly));
    for (auto resource : resources)
    {
        std::vector<WCHAR> name;
        ASSERT_EQUAL(GetManifestResourceProps(baselineAssembly, resource), GetManifestResourceProps(currentAssembly, resource, &name));
        ASSERT_EQUAL(FindManifestResourceByName(baselineAssembly, name.data()), FindManifestResourceByName(currentAssembly, name.data()));
    }

    END_TEST();
}

EXPORT
TestResult UnitLongRunningAPIs(void const* data, uint32_t dataLen)
{
    BEGIN_TEST();

    // Load metadata
    dncp::com_ptr<IMetaDataImport2> baselineImport;
    ASSERT_EQUAL(S_OK, CreateImport(g_baselineDisp, data, dataLen, &baselineImport));
    dncp::com_ptr<IMetaDataImport2> currentImport;
    ASSERT_EQUAL(S_OK, CreateImport(g_currentDisp, data, dataLen, &currentImport));

    static auto VerifyFindMemberRef = [](IMetaDataImport2* import, mdToken memberRef) -> std::vector<uint32_t>
    {
        std::vector<uint32_t> values;

        mdToken ptk;
        static_char_buffer<WCHAR> name{};
        ULONG pchMember;
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        HRESULT hr = import->GetMemberRefProps(memberRef,
            &ptk,
            name.data(),
            (ULONG)name.size(),
            &pchMember,
            &ppvSigBlob,
            &pcbSigBlob);
        values.push_back(hr);
        if (hr == S_OK)
        {
            // We were able to get the name, now try looking up a memberRef by name and by sig
            mdMemberRef lookup = mdTokenNil;
            hr = import->FindMemberRef(ptk, name.data(), ppvSigBlob, pcbSigBlob, &lookup);
            values.push_back(hr);
            values.push_back(lookup);
            lookup = mdTokenNil;
            hr = import->FindMemberRef(ptk, name.data(), nullptr, 0, &lookup);
            values.push_back(hr);
            values.push_back(lookup);
            lookup = mdTokenNil;
            hr = import->FindMemberRef(ptk, nullptr, ppvSigBlob, pcbSigBlob, &lookup);
            values.push_back(hr);
            values.push_back(lookup);
        }
        return values;
    };

    size_t stride;
    size_t count;

    auto typedefs = ASSERT_AND_RETURN(EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
    count = 0;
    stride = std::max(typedefs.size() / 128, (size_t)16);
    for (auto typdef : typedefs)
    {
        if (count++ % stride != 0)
            continue;

        ASSERT_EQUAL(EnumMemberRefs(baselineImport, typdef), EnumMemberRefs(currentImport, typdef));

        auto methoddefs = ASSERT_AND_RETURN(EnumMethods(baselineImport, typdef), EnumMethods(currentImport, typdef));
        for (auto methoddef : methoddefs)
        {
            ASSERT_EQUAL(EnumMethodSemantics(baselineImport, methoddef), EnumMethodSemantics(currentImport, methoddef));
        }

        ASSERT_EQUAL(EnumCustomAttributes(baselineImport, typdef), EnumCustomAttributes(currentImport, typdef));
    }

    auto typespecs = ASSERT_AND_RETURN(EnumTypeSpecs(baselineImport), EnumTypeSpecs(currentImport));
    count = 0;
    stride = std::max(typespecs.size() / 128, (size_t)16);
    for (auto typespec : typespecs)
    {
        if (count++ % stride != 0)
            continue;

        auto memberrefs = ASSERT_AND_RETURN(EnumMemberRefs(baselineImport, typespec), EnumMemberRefs(currentImport, typespec));
        for (auto memberref : memberrefs)
        {
            ASSERT_EQUAL(GetMemberRefProps(baselineImport, memberref), GetMemberRefProps(currentImport, memberref));
            ASSERT_EQUAL(VerifyFindMemberRef(baselineImport, memberref), VerifyFindMemberRef(currentImport, memberref));
        }
    }

    END_TEST();
}

EXPORT
TestResult UnitFindAPIs(void const* data, uint32_t dataLen)
{
    BEGIN_TEST();

    // Load metadata
    dncp::com_ptr<IMetaDataImport2> baselineImport;
    ASSERT_EQUAL(S_OK, CreateImport(g_baselineDisp, data, dataLen, &baselineImport));
    dncp::com_ptr<IMetaDataImport2> currentImport;
    ASSERT_EQUAL(S_OK, CreateImport(g_currentDisp, data, dataLen, &currentImport));

    static auto FindTokenByName = [](IMetaDataImport2* import, LPCWSTR name, mdToken enclosing = mdTokenNil) -> mdToken
    {
        mdTypeDef ptd;
        ASSERT_EQUAL(S_OK, import->FindTypeDefByName(name, enclosing, &ptd));
        return ptd;
    };

    static auto GetTypeDefBaseToken = [](IMetaDataImport2* import, mdTypeDef tk) -> mdToken
    {
        static_char_buffer<WCHAR> name{};
        ULONG pchTypeDef;
        DWORD pdwTypeDefFlags;
        mdToken ptkExtends;
        ASSERT_EQUAL(S_OK, import->GetTypeDefProps(tk,
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
        ASSERT_TRUE(!methoddefs.empty());
        return methoddefs[0];
    };

    static auto FindMemberRef = [](IMetaDataImport2* import, mdTypeDef type, LPCWSTR methodName) -> mdToken
    {
        auto methodDef = FindMethodDef(import, type, methodName);
        mdMemberRef pmr;
        ASSERT_EQUAL(S_OK, import->FindMemberRef(methodDef, methodName, nullptr, 0, &pmr));
        return pmr;
    };

    static auto FindMethod = [](IMetaDataImport2* import, mdTypeDef td, LPCWSTR name, void const* pvSigBlob, ULONG cbSigBlob) -> uint32_t
    {
        mdMethodDef tkMethod;
        mdToken tkMember;
        ASSERT_EQUAL(S_OK, import->FindMethod(td, name, (PCCOR_SIGNATURE)pvSigBlob, cbSigBlob, &tkMethod));
        ASSERT_EQUAL(S_OK, import->FindMember(td, name, (PCCOR_SIGNATURE)pvSigBlob, cbSigBlob, &tkMember));
        ASSERT_EQUAL(tkMethod, tkMember);
        return tkMethod;
    };

    static auto FindField = [](IMetaDataImport2* import, mdTypeDef td, LPCWSTR name, void const* pvSigBlob, ULONG cbSigBlob) -> uint32_t
    {
        mdFieldDef tkField;
        mdToken tkMember;
        ASSERT_EQUAL(S_OK, import->FindField(td, name, (PCCOR_SIGNATURE)pvSigBlob, cbSigBlob, &tkField));
        ASSERT_EQUAL(S_OK, import->FindMember(td, name, (PCCOR_SIGNATURE)pvSigBlob, cbSigBlob, &tkMember));
        ASSERT_EQUAL(tkField, tkMember);
        return tkField;
    };

    auto tgt = W("C");

    auto baseTypeDef = W("B1");
    auto tkB1 = ASSERT_AND_RETURN(FindTokenByName(baselineImport, baseTypeDef), FindTokenByName(currentImport, baseTypeDef));
    auto tkB1Base = ASSERT_AND_RETURN(GetTypeDefBaseToken(baselineImport, tkB1), GetTypeDefBaseToken(currentImport, tkB1));
    ASSERT_EQUAL(FindTypeDefByName(baselineImport, tgt, tkB1Base), FindTypeDefByName(currentImport, tgt, tkB1Base));

    auto baseTypeRef = W("B2");
    auto tkB2 = ASSERT_AND_RETURN(FindTokenByName(baselineImport, baseTypeRef), FindTokenByName(currentImport, baseTypeRef));
    auto tkB2Base = ASSERT_AND_RETURN(GetTypeDefBaseToken(baselineImport, tkB2), GetTypeDefBaseToken(currentImport, tkB2));
    ASSERT_EQUAL(FindTypeDefByName(baselineImport, tgt, tkB2Base), FindTypeDefByName(currentImport, tgt, tkB2Base));

    auto methodDefName = W("MethodDef");
    auto tkMethodDef = ASSERT_AND_RETURN(FindMethodDef(baselineImport, tkB1Base, methodDefName), FindMethodDef(currentImport, tkB1Base, methodDefName));

    void const* defSigBlob;
    ULONG defSigBlobLength;
    ASSERT_EQUAL(
        GetMethodProps(baselineImport, tkMethodDef),
        GetMethodProps(currentImport, tkMethodDef, &defSigBlob, &defSigBlobLength));
    ASSERT_EQUAL(
        FindMethod(baselineImport, tkB1Base, methodDefName, defSigBlob, defSigBlobLength),
        FindMethod(currentImport, tkB1Base, methodDefName, defSigBlob, defSigBlobLength));

    auto methodRef1Name = W("MethodRef1");
    auto tkMemberRefNoVarArgsBase = ASSERT_AND_RETURN(
        FindMemberRef(baselineImport, tkB1Base, methodRef1Name),
        FindMemberRef(currentImport, tkB1Base, methodRef1Name));

    PCCOR_SIGNATURE ref1Blob;
    ULONG ref1BlobLength;
    ASSERT_EQUAL(
        GetMemberRefProps(baselineImport, tkMemberRefNoVarArgsBase),
        GetMemberRefProps(currentImport, tkMemberRefNoVarArgsBase, &ref1Blob, &ref1BlobLength));
    ASSERT_EQUAL(
        FindMethod(baselineImport, tkB1Base, methodRef1Name, ref1Blob, ref1BlobLength),
        FindMethod(currentImport, tkB1Base, methodRef1Name, ref1Blob, ref1BlobLength));

    auto methodRef2Name = W("MethodRef2");
    auto tkMemberRefVarArgsBase = ASSERT_AND_RETURN(
        FindMemberRef(baselineImport, tkB1Base, methodRef2Name),
        FindMemberRef(currentImport, tkB1Base, methodRef2Name));

    PCCOR_SIGNATURE ref2Blob;
    ULONG ref2BlobLength;
    ASSERT_EQUAL(
        GetMemberRefProps(baselineImport, tkMemberRefVarArgsBase),
        GetMemberRefProps(currentImport, tkMemberRefVarArgsBase, &ref2Blob, &ref2BlobLength));
    ASSERT_EQUAL(
        FindMethod(baselineImport, tkB1Base, methodRef2Name, ref2Blob, ref2BlobLength),
        FindMethod(currentImport, tkB1Base, methodRef2Name, ref2Blob, ref2BlobLength));

    auto fieldName = W("Field1");
    auto tkFields = ASSERT_AND_RETURN(
        EnumFieldsWithName(baselineImport, tkB2, fieldName),
        EnumFieldsWithName(currentImport, tkB2, fieldName));
    ASSERT_TRUE(!tkFields.empty());
    mdToken tkField = tkFields[0];

    void const* sigBlob;
    ULONG sigBlobLength;
    ASSERT_EQUAL(
        GetFieldProps(baselineImport, tkField),
        GetFieldProps(currentImport, tkField, &sigBlob, &sigBlobLength));
    ASSERT_EQUAL(
        FindField(baselineImport, tkB2, fieldName, sigBlob, sigBlobLength),
        FindField(currentImport, tkB2, fieldName, sigBlob, sigBlobLength));

    END_TEST();
}

EXPORT
TestResult UnitImportAPIsIndirectionTables(void const* data, uint32_t dataLen, void const** deltaImages, uint32_t* deltaImageLengths, uint32_t numDeltaImages)
{
    BEGIN_TEST();

    dncp::com_ptr<IMetaDataEmit2> baseImageEmit;
    ASSERT_EQUAL(S_OK, CreateEmit(g_deltaImageBuilder, data, dataLen, &baseImageEmit));

    for (uint32_t i = 0; i < numDeltaImages; ++i)
    {
        dncp::com_ptr<IMetaDataImport2> deltaImport;
        ASSERT_EQUAL(S_OK, CreateImport(g_deltaImageBuilder, deltaImages[i], deltaImageLengths[i], &deltaImport));
        ASSERT_EQUAL(S_OK, baseImageEmit->ApplyEditAndContinue(deltaImport));
    }

    DWORD compositeImageSize;
    ASSERT_EQUAL(S_OK, baseImageEmit->GetSaveSize(CorSaveSize::cssAccurate, &compositeImageSize));

    std::unique_ptr<uint8_t[]> compositeImage = std::make_unique<uint8_t[]>(compositeImageSize);
    ASSERT_EQUAL(S_OK, baseImageEmit->SaveToMemory(compositeImage.get(), compositeImageSize));

    return UnitImportAPIs(compositeImage.get(), compositeImageSize);

    END_DELEGATING_TEST();
}
