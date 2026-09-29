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

    uint32_t HashString(char const* arr)
    {
        uint32_t hash = Seed;
        auto curr = arr;
        for (; *curr != '\0'; ++curr)
        {
            hash = fnv1a(*curr, hash);
        }
        return hash;
    }

    // APIs on the public interfaces to enable enumerating tokens
    // that can be passed to the internal APIs.
    void ValidateAndCloseEnum(IMetaDataImport2* import, HCORENUM hcorenum, ULONG expectedCount)
    {
        ULONG count;
        ASSERT_HRESULT_SUCCEEDED(import->CountEnum(hcorenum, &count));
        ASSERT_EQ(count, expectedCount);
        import->CloseEnum(hcorenum);
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

    std::vector<uint32_t> GetCustomAttributeByName(IMDInternalImport* import, LPCSTR customAttr, mdToken tkObj)
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

    std::vector<uint32_t> GetCustomAttribute_Nullable(IMDInternalImport* import, mdToken tkObj)
    {
        auto NullableAttrName = "System.Runtime.CompilerServices.NullableAttribute";
        return GetCustomAttributeByName(import, NullableAttrName, tkObj);
    }

    std::vector<uint32_t> GetCustomAttribute_CompilerGenerated(IMDInternalImport* import, mdToken tkObj)
    {
        auto CompilerGeneratedAttrName = "System.Runtime.CompilerServices.CompilerGeneratedAttribute";
        return GetCustomAttributeByName(import, CompilerGeneratedAttrName, tkObj);
    }

    void ValidateAndCloseEnum(IMDInternalImport* import, HENUMInternal* hcorenum, ULONG expectedCount)
    {
        ASSERT_EQ(expectedCount, import->EnumGetCount(hcorenum));
        import->EnumClose(hcorenum);
    }

    std::vector<uint32_t> EnumTypeDefs(IMDInternalImport* import)
    {
        std::vector<uint32_t> tokens;
        HENUMInternal hcorenum{};
        EXPECT_HRESULT_SUCCEEDED(import->EnumTypeDefInit(&hcorenum));
        mdToken tok;
        while (import->EnumNext(&hcorenum, &tok))
        {
            tokens.push_back(tok);
        }
        EXPECT_NO_FATAL_FAILURE(ValidateAndCloseEnum(import, &hcorenum, (ULONG)tokens.size()));
        return tokens;
    }

    std::vector<uint32_t> EnumTokens(IMDInternalImport* import, mdToken tokenKind)
    {
        std::vector<uint32_t> tokens;
        HENUMInternal hcorenum{};
        EXPECT_HRESULT_SUCCEEDED(import->EnumAllInit(tokenKind, &hcorenum));
        mdToken tok;
        while (import->EnumNext(&hcorenum, &tok))
        {
            tokens.push_back(tok);
        }
        EXPECT_NO_FATAL_FAILURE(ValidateAndCloseEnum(import, &hcorenum, (ULONG)tokens.size()));
        return tokens;
    }


    std::vector<uint32_t> EnumTokens(IMDInternalImport* import, CorTokenType tokenKind, mdToken parent)
    {
        std::vector<uint32_t> tokens;
        HENUMInternal hcorenum{};
        EXPECT_HRESULT_SUCCEEDED(import->EnumInit(tokenKind, parent, &hcorenum));
        mdToken tok;
        while (import->EnumNext(&hcorenum, &tok))
        {
            tokens.push_back(tok);
        }
        EXPECT_NO_FATAL_FAILURE(ValidateAndCloseEnum(import, &hcorenum, (ULONG)tokens.size()));
        return tokens;
    }

    std::vector<uint32_t> EnumTypeRefs(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtTypeRef);
    }

    std::vector<uint32_t> EnumTypeSpecs(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtTypeSpec);
    }

    std::vector<uint32_t> EnumModuleRefs(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtModuleRef);
    }

    std::vector<uint32_t> EnumInterfaceImpls(IMDInternalImport* import, mdTypeDef typdef)
    {
        return EnumTokens(import, mdtInterfaceImpl, typdef);
    }

    std::vector<uint32_t> EnumMemberRefs(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtMemberRef);
    }

    std::vector<uint32_t> EnumMethods(IMDInternalImport* import, mdTypeDef typdef)
    {
        return EnumTokens(import, mdtMethodDef, typdef);
    }

    std::vector<uint32_t> EnumMethodImpls(IMDInternalImport* import, mdTypeDef typdef)
    {
        std::vector<uint32_t> tokens;
        HENUMInternal hcorenumBody{};
        HENUMInternal hcorenumDecl{};
        EXPECT_HRESULT_SUCCEEDED(import->EnumMethodImplInit(typdef, &hcorenumBody, &hcorenumDecl));
        mdToken tok;
        while (import->EnumNext(&hcorenumBody, &tok))
        {
            tokens.push_back(tok);
        }
        while (import->EnumNext(&hcorenumDecl, &tok))
        {
            tokens.push_back(tok);
        }
        EXPECT_NO_FATAL_FAILURE(ValidateAndCloseEnum(import, &hcorenumBody, (ULONG)tokens.size()));
        EXPECT_NO_FATAL_FAILURE(ValidateAndCloseEnum(import, &hcorenumDecl, 0));
        return tokens;
    }

    std::vector<uint32_t> EnumParams(IMDInternalImport* import, mdMethodDef methoddef)
    {
        return EnumTokens(import, mdtParamDef, methoddef);
    }

    std::vector<uint32_t> EnumMethodSpecs(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtMethodSpec);
    }

    std::vector<uint32_t> EnumEvents(IMDInternalImport* import, mdTypeDef tk)
    {
        return EnumTokens(import, mdtEvent, tk);
    }

    std::vector<uint32_t> EnumProperties(IMDInternalImport* import, mdTypeDef tk)
    {
        return EnumTokens(import, mdtProperty, tk);
    }

    std::vector<uint32_t> EnumFields(IMDInternalImport* import, mdTypeDef tk)
    {
        return EnumTokens(import, mdtFieldDef, tk);
    }

    std::vector<uint32_t> EnumGlobalFields(IMDInternalImport* import)
    {
        std::vector<uint32_t> tokens;
        HENUMInternal hcorenum{};
        EXPECT_HRESULT_SUCCEEDED(import->EnumGlobalFieldsInit(&hcorenum));
        mdToken tok;
        while (import->EnumNext(&hcorenum, &tok))
        {
            tokens.push_back(tok);
        }
        EXPECT_NO_FATAL_FAILURE(ValidateAndCloseEnum(import, &hcorenum, (ULONG)tokens.size()));
        return tokens;
    }

    std::vector<uint32_t> EnumGlobalFunctions(IMDInternalImport* import)
    {
        std::vector<uint32_t> tokens;
        HENUMInternal hcorenum{};
        EXPECT_HRESULT_SUCCEEDED(import->EnumGlobalFunctionsInit(&hcorenum));
        mdToken tok;
        while (import->EnumNext(&hcorenum, &tok))
        {
            tokens.push_back(tok);
        }
        EXPECT_NO_FATAL_FAILURE(ValidateAndCloseEnum(import, &hcorenum, (ULONG)tokens.size()));
        return tokens;
    }

    std::vector<uint32_t> EnumSignatures(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtSignature);
    }
    std::vector<uint32_t> EnumCustomAttributes(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtCustomAttribute);
    }
    std::vector<uint32_t> EnumCustomAttributes(IMDInternalImport* import, mdToken tk)
    {
        return EnumTokens(import, mdtCustomAttribute, tk);
    }
    std::vector<uint32_t> EnumCustomAttributesByName(IMDInternalImport* import, mdToken tk, LPCSTR name)
    {
        std::vector<uint32_t> tokens;
        HENUMInternal hcorenum{};
        EXPECT_HRESULT_SUCCEEDED(import->EnumCustomAttributeByNameInit(tk, name, &hcorenum));
        mdToken tok;
        while (import->EnumNext(&hcorenum, &tok))
        {
            tokens.push_back(tok);
        }
        EXPECT_NO_FATAL_FAILURE(ValidateAndCloseEnum(import, &hcorenum, (ULONG)tokens.size()));
        return tokens;
    }

    std::vector<uint32_t> EnumGenericParams(IMDInternalImport* import, mdToken tk)
    {
        return EnumTokens(import, mdtGenericParam, tk);
    }

    std::vector<uint32_t> EnumGenericParamConstraints(IMDInternalImport* import, mdGenericParam tk)
    {
        return EnumTokens(import, mdtGenericParamConstraint, tk);
    }

    std::vector<uint32_t> EnumAssemblyRefs(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtAssemblyRef);
    }

    std::vector<uint32_t> EnumFiles(IMDInternalImport* import)
    {
        return EnumTokens(import, mdtFile);
    }

    std::vector<uint32_t> GetParentToken(IMDInternalImport* import, mdToken tk)
    {
        std::vector<uint32_t> values;
        // The parent value must be left unchanged if there's no parent and the method return S_OK.
        // Callers in the runtime depend on that.
        // We'll verify that behavior.
        mdToken parent = 0xdeadbeef;
        HRESULT hr = import->GetParentToken(tk, &parent);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(parent);

        return values;
    }

    std::vector<uint32_t> FindTypeRef(IMDInternalImport* import)
    {
        std::vector<uint32_t> values;
        HRESULT hr;
        mdToken tk;

        // The first assembly ref token typically contains System.Object and Enumerator.
        mdToken const assemblyRefToken = 0x23000001;
        hr = import->FindTypeRefByName("System", "Object", assemblyRefToken, &tk);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(tk);

        // Look for a type that won't ever exist
        hr = import->FindTypeRefByName("DoesNotExist", "NotReal", assemblyRefToken, &tk);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(tk);
        return values;
    }

    std::vector<uint32_t> FindTypeDefByName(IMDInternalImport* import, LPCSTR ns, LPCSTR name, mdToken scope)
    {
        std::vector<uint32_t> values;

        mdTypeDef ptd;
        HRESULT hr = import->FindTypeDef(ns, name, scope, &ptd);

        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(ptd);
        return values;
    }

    std::vector<uint32_t> FindExportedTypeByName(IMDInternalImport* import, LPCSTR ns, LPCSTR name, mdToken tkImplementation)
    {
        std::vector<uint32_t> values;

        mdExportedType exported;
        HRESULT hr = import->FindExportedTypeByName(ns, name, tkImplementation, &exported);

        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(exported);
        return values;
    }

    std::vector<uint32_t> FindManifestResourceByName(IMDInternalImport* import, LPCSTR name)
    {
        std::vector<uint32_t> values;

        mdManifestResource resource;
        HRESULT hr = import->FindManifestResourceByName(name, &resource);

        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(resource);
        return values;
    }

    std::vector<uint32_t> GetTypeDefProps(IMDInternalImport* import, mdTypeDef typdef)
    {
        std::vector<uint32_t> values;
        DWORD pdwTypeDefFlags;
        mdToken ptkExtends;
        HRESULT hr = import->GetTypeDefProps(typdef,
            &pdwTypeDefFlags,
            &ptkExtends);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pdwTypeDefFlags);
            values.push_back(ptkExtends);
        }

        LPCSTR ns, name;
        hr = import->GetNameOfTypeDef(typdef, &name, &ns);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashByteArray(ns, (ULONG)strlen(ns));
            values.push_back(hash);
            hash = HashByteArray(name, (ULONG)strlen(name));
            values.push_back(hash);
        }
        return values;
    }

    std::vector<uint32_t> GetTypeRefProps(IMDInternalImport* import, mdTypeRef typeref)
    {
        std::vector<uint32_t> values;
        mdToken ptkResolutionScope;
        HRESULT hr = import->GetResolutionScopeOfTypeRef(typeref,
            &ptkResolutionScope);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(ptkResolutionScope);
        }

        LPCSTR ns, name;
        hr = import->GetNameOfTypeRef(typeref, &name, &ns);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashByteArray(ns, (ULONG)strlen(ns));
            values.push_back(hash);
            hash = HashByteArray(name, (ULONG)strlen(name));
            values.push_back(hash);
        }
        return values;
    }

    std::vector<uint32_t> GetScopeProps(IMDInternalImport* import)
    {
        std::vector<uint32_t> values;

        LPCSTR name;
        GUID mvid;
        HRESULT hr = import->GetScopeProps(
            &name,
            &mvid);

        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(name);
            values.push_back(hash);

            std::array<uint32_t, sizeof(GUID) / sizeof(uint32_t)> buffer{};
            memcpy(buffer.data(), &mvid, buffer.size());
            for (auto b : buffer)
                values.push_back(b);
        }
        return values;
    }

    std::vector<uint32_t> GetModuleRefProps(IMDInternalImport* import, mdModuleRef moduleref)
    {
        std::vector<uint32_t> values;

        LPCSTR name;
        HRESULT hr = import->GetModuleRefProps(moduleref,
            &name);

        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(name);
            values.push_back(hash);
        }
        return values;
    }

    std::vector<uint32_t> GetMethodProps(IMDInternalImport* import, mdToken tk, void const** sig = nullptr, ULONG* sigLen = nullptr)
    {
        std::vector<uint32_t> values;

        DWORD pdwAttr;
        HRESULT hr = import->GetMethodDefProps(tk, &pdwAttr);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pdwAttr);
        }
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        hr = import->GetSigOfMethodDef(tk, &pcbSigBlob, &ppvSigBlob);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(ppvSigBlob, pcbSigBlob));
            values.push_back(pcbSigBlob);
            if (sig != nullptr)
                *sig = ppvSigBlob;
            if (sigLen != nullptr)
                *sigLen = pcbSigBlob;
        }
        LPCSTR name;
        hr = import->GetNameOfMethodDef(tk, &name);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(name);
            values.push_back(hash);
        }
        return values;
    }

    std::vector<uint32_t> GetNameAndSigOfMethodDef(IMDInternalImport* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        LPCSTR name;
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        HRESULT hr = import->GetNameAndSigOfMethodDef(tk, &ppvSigBlob, &pcbSigBlob, &name);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(name);
            values.push_back(hash);
            values.push_back(HashByteArray(ppvSigBlob, pcbSigBlob));
            values.push_back(pcbSigBlob);
        }
        return values;
    }

    std::vector<uint32_t> GetParamProps(IMDInternalImport* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        USHORT pulSequence;
        LPCSTR name;
        DWORD pdwAttr;
        HRESULT hr = import->GetParamDefProps(tk,
            &pulSequence,
            &pdwAttr,
            &name);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pulSequence);
            uint32_t hash = HashString(name);
            values.push_back(hash);
            values.push_back(pdwAttr);
        }
        return values;
    }

    std::vector<uint32_t> GetMethodSpecProps(IMDInternalImport* import, mdMethodSpec methodSpec)
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

    std::vector<uint32_t> GetMemberRefProps(IMDInternalImport* import, mdMemberRef mr, PCCOR_SIGNATURE* sig = nullptr, ULONG* sigLen = nullptr)
    {
        std::vector<uint32_t> values;

        LPCSTR name;
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        HRESULT hr = import->GetNameAndSigOfMemberRef(mr,
            &ppvSigBlob,
            &pcbSigBlob,
            &name);

        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(name);
            values.push_back(hash);
            values.push_back(HashByteArray(ppvSigBlob, pcbSigBlob));
            values.push_back(pcbSigBlob);

            if (sig != nullptr)
                *sig = ppvSigBlob;
            if (sigLen != nullptr)
                *sigLen = pcbSigBlob;
        }

        mdToken ptk;
        hr = import->GetParentOfMemberRef(mr, &ptk);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(ptk);
        }
        return values;
    }

    std::vector<uint32_t> GetEventProps(IMDInternalImport* import, mdEvent tk)
    {
        std::vector<uint32_t> values;

        LPCSTR name;
        DWORD pdwEventFlags;
        mdToken ptkEventType;
        HRESULT hr = import->GetEventProps(tk,
            &name,
            &pdwEventFlags,
            &ptkEventType);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(name);
            values.push_back(hash);
            values.push_back(pdwEventFlags);
            values.push_back(ptkEventType);
        }
        return values;
    }

    std::vector<uint32_t> GetPropertyProps(IMDInternalImport* import, mdProperty tk)
    {
        std::vector<uint32_t> values;

        LPCSTR name;
        DWORD pdwPropFlags;
        PCCOR_SIGNATURE sig;
        ULONG sigLen;
        HRESULT hr = import->GetPropertyProps(tk,
            &name,
            &pdwPropFlags,
            &sig,
            &sigLen);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(name);
            values.push_back(hash);
            values.push_back(pdwPropFlags);
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
        }
        return values;
    }

    std::vector<uint32_t> GetFieldProps(IMDInternalImport* import, mdFieldDef tk, void const** sig = nullptr, ULONG* sigLen = nullptr)
    {
        std::vector<uint32_t> values;

        DWORD pdwAttr;
        HRESULT hr = import->GetFieldDefProps(tk, &pdwAttr);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pdwAttr);
        }
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        hr = import->GetSigOfFieldDef(tk, &pcbSigBlob, &ppvSigBlob);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(ppvSigBlob, pcbSigBlob));
            values.push_back(pcbSigBlob);
            if (sig != nullptr)
                *sig = ppvSigBlob;
            if (sigLen != nullptr)
                *sigLen = pcbSigBlob;
        }
        LPCSTR name;
        hr = import->GetNameOfFieldDef(tk, &name);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(name);
            values.push_back(hash);
        }
        return values;
    }

    std::vector<uint32_t> GetCustomAttributeProps(IMDInternalImport* import, mdCustomAttribute cv)
    {
        std::vector<uint32_t> values;

        mdToken ptkType;
        HRESULT hr = import->GetCustomAttributeProps(cv, &ptkType);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(ptkType);
        }
        void const* sig;
        ULONG sigLen;
        hr = import->GetCustomAttributeAsBlob(cv, &sig, &sigLen);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
        }
        return values;
    }

    std::vector<uint32_t> GetGenericParamProps(IMDInternalImport* import, mdGenericParam gp)
    {
        std::vector<uint32_t> values;

        ULONG pulParamSeq;
        DWORD pdwParamFlags;
        mdToken ptOwner;
        DWORD reserved;
        LPCSTR name;
        HRESULT hr = import->GetGenericParamProps(gp,
            &pulParamSeq,
            &pdwParamFlags,
            &ptOwner,
            &reserved,
            &name);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pulParamSeq);
            values.push_back(pdwParamFlags);
            values.push_back(ptOwner);
            values.push_back(reserved);
            uint32_t hash = HashString(name);
            values.push_back(hash);
        }
        return values;
    }

    std::vector<uint32_t> GetGenericParamConstraintProps(IMDInternalImport* import, mdGenericParamConstraint tk)
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

    std::vector<uint32_t> GetPinvokeMap(IMDInternalImport* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        DWORD pdwMappingFlags;
        LPCSTR name;
        mdModuleRef pmrImportDLL;
        HRESULT hr = import->GetPinvokeMap(tk,
            &pdwMappingFlags,
            &name,
            &pmrImportDLL);

        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pdwMappingFlags);
            uint32_t hash = HashString(name);
            values.push_back(hash);
            values.push_back(pmrImportDLL);
        }
        return values;
    }

    std::vector<uint32_t> GetTypeSpecFromToken(IMDInternalImport* import, mdTypeSpec typespec)
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

    std::vector<uint32_t> GetSigFromToken(IMDInternalImport* import, mdSignature tkSig)
    {
        std::vector<uint32_t> values;

        PCCOR_SIGNATURE sig;
        ULONG sigLen;
        HRESULT hr = import->GetSigFromToken(tkSig, &sigLen, &sig);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(sig, sigLen));
            values.push_back(sigLen);
        }
        return values;
    }

    std::vector<uint32_t> GetAllAssociates(IMDInternalImport* import, mdToken tkEventProp, std::vector<ASSOCIATE_RECORD>* associates = nullptr)
    {
        std::vector<uint32_t> values;

        HENUMInternal hcorenum;
        EXPECT_HRESULT_SUCCEEDED(import->EnumAssociateInit(tkEventProp, &hcorenum));
        ULONG count = import->EnumGetCount(&hcorenum);
        std::unique_ptr<ASSOCIATE_RECORD[]> recordsBuffer{ new ASSOCIATE_RECORD[count] };

        HRESULT hr = import->GetAllAssociates(&hcorenum, recordsBuffer.get(), count);

        values.push_back(hr);
        if (hr == S_OK)
        {
            for (ULONG i = 0; i < count; i++)
            {
                values.push_back(recordsBuffer[i].m_memberdef);
                values.push_back(recordsBuffer[i].m_dwSemantics);
            }

            if (associates != nullptr)
                *associates = std::vector<ASSOCIATE_RECORD>(recordsBuffer.get(), recordsBuffer.get() + count);
        }

        return values;
    }

    std::vector<uint32_t> GetUserString(IMDInternalImport* import, mdString tkStr)
    {
        std::vector<uint32_t> values;

        LPCWSTR name;
        ULONG pchString;
        HRESULT hr = import->GetUserString(tkStr, &pchString, &name);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashByteArray(name, pchString * sizeof(WCHAR));
            values.push_back(hash);
            values.push_back(pchString);
        }
        return values;
    }
    std::vector<uint32_t> GetFieldMarshal(IMDInternalImport* import, mdToken tk)
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

    std::vector<uint32_t> GetNestedClassProps(IMDInternalImport* import, mdTypeDef tk)
    {
        std::vector<uint32_t> values;

        mdTypeDef ptdEnclosingClass;
        HRESULT hr = import->GetNestedClassProps(tk, &ptdEnclosingClass);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(ptdEnclosingClass);

        return values;
    }

    std::vector<uint32_t> GetClassLayout(IMDInternalImport* import, mdTypeDef tk)
    {
        std::vector<uint32_t> values;
        DWORD pdwPackSize;
        HRESULT hr = import->GetClassPackSize(tk, &pdwPackSize);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(pdwPackSize);
        ULONG pulClassSize;
        hr = import->GetClassTotalSize(tk, &pulClassSize);
        values.push_back(hr);
        if (hr == S_OK)
            values.push_back(pulClassSize);

        MD_CLASS_LAYOUT layout;
        hr = import->GetClassLayoutInit(tk, &layout);
        values.push_back(hr);
        if (hr == S_OK)
        {
            mdFieldDef field;
            ULONG offset;
            while ((hr = import->GetClassLayoutNext(&layout, &field, &offset)) == S_OK)
            {
                values.push_back(layout.m_ridFieldCur);
                values.push_back(layout.m_ridFieldEnd);
                values.push_back(field);
                values.push_back(offset);
            }
        }
        return values;
    }

    std::vector<uint32_t> GetFieldRVA(IMDInternalImport* import, mdToken tk)
    {
        std::vector<uint32_t> values;

        ULONG pulCodeRVA;
        HRESULT hr = import->GetFieldRVA(tk, &pulCodeRVA);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(pulCodeRVA);
        }
        return values;
    }

    std::vector<uint32_t> GetVersionString(IMDInternalImport* import)
    {
        std::vector<uint32_t> values;

        LPCSTR version;
        HRESULT hr = import->GetVersionString(&version);
        values.push_back(hr);
        if (hr == S_OK)
        {
            uint32_t hash = HashString(version);
            values.push_back(hash);
        }

        return values;
    }

    std::vector<uint32_t> GetAssemblyFromScope(IMDInternalImport* import)
    {
        std::vector<uint32_t> values;

        mdAssembly mdAsm;
        HRESULT hr = import->GetAssemblyFromScope(&mdAsm);
        if (hr == S_OK)
            values.push_back(mdAsm);
        return values;
    }

    std::vector<size_t> GetAssemblyProps(IMDInternalImport* import, mdAssembly mda)
    {
        std::vector<size_t> values;
        LPCSTR name;
        std::vector<DWORD> processor(1);
        std::vector<OSINFO> osInfo(1);

        AssemblyMetaDataInternal metadata;

        void const* publicKey;
        ULONG publicKeyLength;
        ULONG hashAlgId;
        ULONG flags;
        HRESULT hr = import->GetAssemblyProps(mda, &publicKey, &publicKeyLength, &hashAlgId, &name, &metadata, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back((size_t)publicKey);
            values.push_back(publicKeyLength);
            values.push_back(hashAlgId);
            values.push_back(HashString(name));
            values.push_back(metadata.usMajorVersion);
            values.push_back(metadata.usMinorVersion);
            values.push_back(metadata.usBuildNumber);
            values.push_back(metadata.usRevisionNumber);
            values.push_back(HashString(metadata.szLocale));
            values.push_back(flags);
        }
        return values;
    }

    std::vector<size_t> GetAssemblyRefProps(IMDInternalImport* import, mdAssemblyRef mdar)
    {
        std::vector<size_t> values;
        LPCSTR name;
        std::vector<DWORD> processor(1);
        std::vector<OSINFO> osInfo(1);

        AssemblyMetaDataInternal metadata;

        void const* publicKeyOrToken;
        ULONG publicKeyOrTokenLength;
        void const* hash;
        ULONG hashLength;
        DWORD flags;
        HRESULT hr = import->GetAssemblyRefProps(mdar, &publicKeyOrToken, &publicKeyOrTokenLength, &name, &metadata, &hash, &hashLength, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(publicKeyOrTokenLength != 0 ? (size_t)publicKeyOrToken : 0);
            values.push_back(publicKeyOrTokenLength);
            values.push_back(HashString(name));
            values.push_back(metadata.usMajorVersion);
            values.push_back(metadata.usMinorVersion);
            values.push_back(metadata.usBuildNumber);
            values.push_back(metadata.usRevisionNumber);
            values.push_back(HashString(metadata.szLocale));
            values.push_back(hashLength != 0 ? (size_t)hash : 0);
            values.push_back(hashLength);
            values.push_back(flags);
        }
        return values;
    }

    std::vector<size_t> GetFileProps(IMDInternalImport* import, mdFile mdf)
    {
        std::vector<size_t> values;

        LPCSTR name;
        void const* hash;
        ULONG hashLength;
        DWORD flags;
        HRESULT hr = import->GetFileProps(mdf, &name, &hash, &hashLength, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(HashString(name));
            values.push_back(hashLength != 0 ? (size_t)hash : 0);
            values.push_back(hashLength);
            values.push_back(flags);
        }
        return values;
    }

    std::vector<uint32_t> GetExportedTypeProps(IMDInternalImport* import, mdFile mdf, LPCSTR* nsBuffer = nullptr, LPCSTR* nameBuffer = nullptr, uint32_t* implementationToken = nullptr)
    {
        std::vector<uint32_t> values;

        LPCSTR ns;
        LPCSTR name;
        mdToken implementation;
        mdTypeDef typeDef;
        DWORD flags;
        HRESULT hr = import->GetExportedTypeProps(mdf, &ns, &name, &implementation, &typeDef, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(HashString(ns));
            values.push_back(HashString(name));
            values.push_back(implementation);
            values.push_back(typeDef);
            values.push_back(flags);

            if (nsBuffer != nullptr)
                *nsBuffer = ns;
            if (nameBuffer != nullptr)
                *nameBuffer = name;
            if (implementationToken != nullptr)
                *implementationToken = implementation;
        }
        return values;
    }

    std::vector<uint32_t> GetManifestResourceProps(IMDInternalImport* import, mdManifestResource mmr, LPCSTR* nameBuffer = nullptr)
    {
        std::vector<uint32_t> values;

        LPCSTR name;
        ULONG offset;
        mdToken implementation;
        DWORD flags;
        HRESULT hr = import->GetManifestResourceProps(mmr, &name, &implementation, &offset, &flags);
        values.push_back(hr);

        if (hr == S_OK)
        {
            values.push_back(HashString(name));
            values.push_back(implementation);
            values.push_back(flags);

            if (nameBuffer != nullptr)
                *nameBuffer = name;
        }
        return values;
    }

    std::vector<uint32_t> ResetEnum(IMDInternalImport* import)
    {
        std::vector<uint32_t> tokens;
        auto typedefs = EnumTypeDefs(import);
        if (typedefs.size() == 0)
            return tokens;

        auto tk = typedefs[0];
        HENUMInternal henumInternal{};
        try
        {
            static auto ReadInMethods = [](IMDInternalImport* import, HENUMInternal& henumInternal, mdToken tk, std::vector<uint32_t>& tokens)
            {
                EXPECT_HRESULT_SUCCEEDED(import->EnumInit(mdtMethodDef, tk, &henumInternal));
                mdToken token;
                while (import->EnumNext(&henumInternal, &token))
                    tokens.push_back(token);
            };

            ReadInMethods(import, henumInternal, tk, tokens);

            // Fully reset the enum
            import->EnumReset(&henumInternal);
            ReadInMethods(import, henumInternal, tk, tokens);
        }
        catch (...)
        {
            import->EnumClose(&henumInternal);
            throw;
        }
        return tokens;
    }
}

class InternalMetadataImportTest : public RegressionTest
{
    protected:
    void SetUp() override
    {
        if (TestBaseline::InternalMetadata == nullptr)
        {
            GTEST_SKIP() << "Baseline internal metadata implementation not available.";
        }
    }
};

TEST_P(InternalMetadataImportTest, ImportAPIs)
{
    auto param = GetParam();
    span<uint8_t> blob = GetMetadataForFile(param);
    void const* data = blob.data();
    uint32_t dataLen = (uint32_t)blob.size();

    // Load metadata
    minipal::com_ptr<IMDInternalImport> baselineImport;
    minipal::com_ptr<IMetaDataImport2> baselinePublic;
    ASSERT_HRESULT_SUCCEEDED(TestBaseline::InternalMetadata(data, dataLen, ofRead, IID_IMDInternalImport, (void**)&baselineImport));
    ASSERT_HRESULT_SUCCEEDED(CreateImport(TestBaseline::Metadata, data, dataLen, &baselinePublic));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_HRESULT_SUCCEEDED(GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataImport2> currentPublic;
    ASSERT_HRESULT_SUCCEEDED(CreateImport(dispenser, data, dataLen, &currentPublic));
    minipal::com_ptr<IMDInternalImport> currentImport;
    ASSERT_HRESULT_SUCCEEDED(currentPublic->QueryInterface(IID_IMDInternalImport, (void**)&currentImport));

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
    ASSERT_EQUAL_AND_SET(userStrings, EnumUserStrings(baselinePublic), EnumUserStrings(currentPublic));
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
    }

    ASSERT_THAT(FindTypeRef(currentImport), testing::ElementsAreArray(FindTypeRef(baselineImport)));
    TokenList typerefs;
    ASSERT_EQUAL_AND_SET(typerefs, EnumTypeRefs(baselineImport), EnumTypeRefs(currentImport));
    for (auto typeref : typerefs)
    {
        ASSERT_THAT(GetTypeRefProps(currentImport, typeref), testing::ElementsAreArray(GetTypeRefProps(baselineImport, typeref)));
        ASSERT_THAT(GetCustomAttribute_CompilerGenerated(currentImport, typeref), testing::ElementsAreArray(GetCustomAttribute_CompilerGenerated(baselineImport, typeref)));
    }

    TokenList typespecs;
    ASSERT_EQUAL_AND_SET(typespecs, EnumTypeSpecs(baselineImport), EnumTypeSpecs(currentImport));
    for (auto typespec : typespecs)
    {
        ASSERT_THAT(GetTypeSpecFromToken(currentImport, typespec), testing::ElementsAreArray(GetTypeSpecFromToken(baselineImport, typespec)));
        ASSERT_THAT(GetCustomAttribute_CompilerGenerated(currentImport, typespec), testing::ElementsAreArray(GetCustomAttribute_CompilerGenerated(baselineImport, typespec)));
    }

    TokenList globalFunctions;
    ASSERT_EQUAL_AND_SET(globalFunctions, EnumGlobalFunctions(baselineImport), EnumGlobalFunctions(currentImport));
    for (auto methoddef : globalFunctions)
    {
        void const* sig = nullptr;
        ULONG sigLen = 0;
        ASSERT_THAT(GetMethodProps(currentImport, methoddef, &sig, &sigLen), testing::ElementsAreArray(GetMethodProps(baselineImport, methoddef)));
        ASSERT_THAT(GetNameAndSigOfMethodDef(currentImport, methoddef), testing::ElementsAreArray(GetNameAndSigOfMethodDef(baselineImport, methoddef)));
        ASSERT_THAT(GetCustomAttribute_CompilerGenerated(currentImport, methoddef), testing::ElementsAreArray(GetCustomAttribute_CompilerGenerated(baselineImport, methoddef)));
        ASSERT_EQ(GetParentToken(baselineImport, methoddef), GetParentToken(currentImport, methoddef));

        TokenList paramdefs;
        ASSERT_EQUAL_AND_SET(paramdefs, EnumParams(baselineImport, methoddef), EnumParams(currentImport, methoddef));
        for (auto paramdef : paramdefs)
        {
            ASSERT_THAT(GetParamProps(currentImport, paramdef), testing::ElementsAreArray(GetParamProps(baselineImport, paramdef)));
            ASSERT_THAT(GetFieldMarshal(currentImport, paramdef), testing::ElementsAreArray(GetFieldMarshal(baselineImport, paramdef)));
            ASSERT_THAT(GetCustomAttribute_Nullable(currentImport, paramdef), testing::ElementsAreArray(GetCustomAttribute_Nullable(baselineImport, paramdef)));
            ASSERT_EQ(GetParentToken(baselineImport, paramdef), GetParentToken(currentImport, paramdef));
        }

        ASSERT_THAT(GetPinvokeMap(currentImport, methoddef), testing::ElementsAreArray(GetPinvokeMap(baselineImport, methoddef)));
    }

    TokenList globalFields;
    ASSERT_EQUAL_AND_SET(globalFields, EnumGlobalFields(baselineImport), EnumGlobalFields(currentImport));
    for (auto fielddef : globalFields)
    {
        ASSERT_THAT(GetFieldProps(currentImport, fielddef), testing::ElementsAreArray(GetFieldProps(baselineImport, fielddef)));
        ASSERT_THAT(GetPinvokeMap(currentImport, fielddef), testing::ElementsAreArray(GetPinvokeMap(baselineImport, fielddef)));
        ASSERT_THAT(GetFieldRVA(currentImport, fielddef), testing::ElementsAreArray(GetFieldRVA(baselineImport, fielddef)));
        ASSERT_THAT(GetFieldMarshal(currentImport, fielddef), testing::ElementsAreArray(GetFieldMarshal(baselineImport, fielddef)));
        ASSERT_THAT(GetCustomAttribute_Nullable(currentImport, fielddef), testing::ElementsAreArray(GetCustomAttribute_Nullable(baselineImport, fielddef)));
        ASSERT_EQ(GetParentToken(baselineImport, fielddef), GetParentToken(currentImport, fielddef));
    }

    // TODO: GetPermissionSetProps (there's no mechanism to enumerate these on the internal interface, and it's not used)
    // TODO: GetParentToken (we're missing some cases here)

    TokenList typedefs;
    ASSERT_EQUAL_AND_SET(typedefs, EnumTypeDefs(baselineImport), EnumTypeDefs(currentImport));
    for (auto typdef : typedefs)
    {
        ASSERT_THAT(GetTypeDefProps(currentImport, typdef), testing::ElementsAreArray(GetTypeDefProps(baselineImport, typdef)));
        ASSERT_THAT(EnumInterfaceImpls(currentImport, typdef), testing::ElementsAreArray(EnumInterfaceImpls(baselineImport, typdef)));
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
            ASSERT_THAT(GetNameAndSigOfMethodDef(currentImport, methoddef), testing::ElementsAreArray(GetNameAndSigOfMethodDef(baselineImport, methoddef)));
            ASSERT_THAT(GetCustomAttribute_CompilerGenerated(currentImport, methoddef), testing::ElementsAreArray(GetCustomAttribute_CompilerGenerated(baselineImport, methoddef)));
            ASSERT_EQ(GetParentToken(baselineImport, methoddef), GetParentToken(currentImport, methoddef));

            TokenList paramdefs;
            ASSERT_EQUAL_AND_SET(paramdefs, EnumParams(baselineImport, methoddef), EnumParams(currentImport, methoddef));
            for (auto paramdef : paramdefs)
            {
                ASSERT_THAT(GetParamProps(currentImport, paramdef), testing::ElementsAreArray(GetParamProps(baselineImport, paramdef)));
                ASSERT_THAT(GetFieldMarshal(currentImport, paramdef), testing::ElementsAreArray(GetFieldMarshal(baselineImport, paramdef)));
                ASSERT_THAT(GetCustomAttribute_Nullable(currentImport, paramdef), testing::ElementsAreArray(GetCustomAttribute_Nullable(baselineImport, paramdef)));
                ASSERT_EQ(GetParentToken(baselineImport, paramdef), GetParentToken(currentImport, paramdef));
            }

            ASSERT_THAT(GetPinvokeMap(currentImport, methoddef), testing::ElementsAreArray(GetPinvokeMap(baselineImport, methoddef)));
        }

        TokenList methodspecs;
        ASSERT_EQUAL_AND_SET(methodspecs, EnumMethodSpecs(baselineImport), EnumMethodSpecs(currentImport));
        for (auto methodspec : methodspecs)
        {
            ASSERT_THAT(GetMethodSpecProps(currentImport, methodspec), testing::ElementsAreArray(GetMethodSpecProps(baselineImport, methodspec)));
            ASSERT_EQ(GetParentToken(baselineImport, methodspec), GetParentToken(currentImport, methodspec));
        }

        TokenList eventdefs;
        ASSERT_EQUAL_AND_SET(eventdefs, EnumEvents(baselineImport, typdef), EnumEvents(currentImport, typdef));
        for (auto eventdef : eventdefs)
        {
            ASSERT_THAT(GetEventProps(currentImport, eventdef), testing::ElementsAreArray(GetEventProps(baselineImport, eventdef)));
            // We explicitly don't test enumerating associates with the regular enumerator
            // as it's never used. The Associates enumerator is only used with GetAllAssociates.
            ASSERT_THAT(GetAllAssociates(currentImport, eventdef), testing::ElementsAreArray(GetAllAssociates(baselineImport, eventdef)));
            ASSERT_EQ(GetParentToken(baselineImport, eventdef), GetParentToken(currentImport, eventdef));
        }

        TokenList properties;
        ASSERT_EQUAL_AND_SET(properties, EnumProperties(baselineImport, typdef), EnumProperties(currentImport, typdef));
        for (auto prop : properties)
        {
            ASSERT_THAT(GetPropertyProps(currentImport, prop), testing::ElementsAreArray(GetPropertyProps(baselineImport, prop)));
            // We explicitly don't test enumerating associates with the regular enumerator
            // as it's never used. The Associates enumerator is only used with GetAllAssociates.
            ASSERT_THAT(GetAllAssociates(currentImport, prop), testing::ElementsAreArray(GetAllAssociates(baselineImport, prop)));
            ASSERT_EQ(GetParentToken(baselineImport, prop), GetParentToken(currentImport, prop));
        }

        TokenList fielddefs;
        ASSERT_EQUAL_AND_SET(fielddefs, EnumFields(baselineImport, typdef), EnumFields(currentImport, typdef));
        for (auto fielddef : fielddefs)
        {
            ASSERT_THAT(GetFieldProps(currentImport, fielddef), testing::ElementsAreArray(GetFieldProps(baselineImport, fielddef)));
            ASSERT_THAT(GetPinvokeMap(currentImport, fielddef), testing::ElementsAreArray(GetPinvokeMap(baselineImport, fielddef)));
            ASSERT_THAT(GetFieldRVA(currentImport, fielddef), testing::ElementsAreArray(GetFieldRVA(baselineImport, fielddef)));
            ASSERT_THAT(GetFieldMarshal(currentImport, fielddef), testing::ElementsAreArray(GetFieldMarshal(baselineImport, fielddef)));
            ASSERT_THAT(GetCustomAttribute_Nullable(currentImport, fielddef), testing::ElementsAreArray(GetCustomAttribute_Nullable(baselineImport, fielddef)));
            ASSERT_EQ(GetParentToken(baselineImport, fielddef), GetParentToken(currentImport, fielddef));
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
    ASSERT_THAT(S_OK, baselinePublic->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&baselineAssembly));
    minipal::com_ptr<IMetaDataAssemblyImport> currentAssembly;
    ASSERT_THAT(S_OK, currentPublic->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&currentAssembly));

    TokenList assemblyTokens;
    ASSERT_EQUAL_AND_SET(assemblyTokens, GetAssemblyFromScope(baselineImport), GetAssemblyFromScope(currentImport));
    for (auto assembly : assemblyTokens)
    {
        ASSERT_THAT(GetAssemblyProps(currentImport, assembly), testing::ElementsAreArray(GetAssemblyProps(baselineImport, assembly)));
    }

    TokenList assemblyRefs;
    ASSERT_EQUAL_AND_SET(assemblyRefs, EnumAssemblyRefs(baselineImport), EnumAssemblyRefs(currentImport));
    for (auto assemblyRef : assemblyRefs)
    {
        ASSERT_THAT(GetAssemblyRefProps(currentImport, assemblyRef), testing::ElementsAreArray(GetAssemblyRefProps(baselineImport, assemblyRef)));
    }

    TokenList files;
    ASSERT_EQUAL_AND_SET(files, EnumFiles(baselineImport), EnumFiles(currentImport));
    for (auto file : files)
    {
        ASSERT_THAT(GetFileProps(currentImport, file), testing::ElementsAreArray(GetFileProps(baselineImport, file)));
    }

    TokenList exports;
    ASSERT_EQUAL_AND_SET(exports, EnumExportedTypes(baselineAssembly), EnumExportedTypes(currentAssembly));
    for (auto exportedType : exports)
    {
        LPCSTR ns;
        LPCSTR name;
        uint32_t implementation = mdTokenNil;
        ASSERT_THAT(GetExportedTypeProps(currentImport, exportedType, &ns, &name, &implementation), testing::ElementsAreArray(GetExportedTypeProps(baselineImport, exportedType)));
        ASSERT_THAT(
            FindExportedTypeByName(currentImport, ns, name, implementation),
            testing::ElementsAreArray(FindExportedTypeByName(baselineImport, ns, name, implementation)));
    }

    TokenList resources;
    ASSERT_EQUAL_AND_SET(resources, EnumManifestResources(baselineAssembly), EnumManifestResources(currentAssembly));
    for (auto resource : resources)
    {
        LPCSTR name;
        ASSERT_THAT(GetManifestResourceProps(currentImport, resource, &name), testing::ElementsAreArray(GetManifestResourceProps(baselineImport, resource)));
        ASSERT_THAT(FindManifestResourceByName(currentImport, name), testing::ElementsAreArray(FindManifestResourceByName(baselineImport, name)));
    }
}

INSTANTIATE_TEST_SUITE_P(InternalMetaDataImportTestCore, InternalMetadataImportTest, testing::ValuesIn(MetadataFilesInDirectory(GetBaselineDirectory())), PrintName);

INSTANTIATE_TEST_SUITE_P(InternalMetaDataImportTestFx4_0, InternalMetadataImportTest, testing::ValuesIn(MetadataFilesInDirectory(FindFrameworkInstall(X("v4.0.30319")))), PrintName);
INSTANTIATE_TEST_SUITE_P(InternalMetaDataImportTestFx2_0, InternalMetadataImportTest, testing::ValuesIn(MetadataFilesInDirectory(FindFrameworkInstall(X("v2.0.50727")))), PrintName);

INSTANTIATE_TEST_SUITE_P(InternalMetaDataImportTest_IndirectionTables, InternalMetadataImportTest, testing::Values(MetadataFile{ MetadataFile::Kind::Generated, IndirectionTablesKey }), PrintName);

class InternalMetaDataLongRunningTest : public RegressionTest
{
protected:
    void SetUp() override
    {
        if (TestBaseline::InternalMetadata == nullptr)
        {
            GTEST_SKIP() << "Baseline metadata implementation not available.";
        }
    }
};

TEST_P(InternalMetaDataLongRunningTest, ImportAPIs)
{
    auto param = GetParam();
    span<uint8_t> blob = GetMetadataForFile(param);
    void const* data = blob.data();
    uint32_t dataLen = (uint32_t)blob.size();

    // Load metadata
    minipal::com_ptr<IMDInternalImport> baselineImport;
    ASSERT_HRESULT_SUCCEEDED(TestBaseline::InternalMetadata(data, dataLen, ofRead, IID_IMDInternalImport, (void**)&baselineImport));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_HRESULT_SUCCEEDED(GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataImport2> currentPublic;
    ASSERT_HRESULT_SUCCEEDED(CreateImport(dispenser, data, dataLen, &currentPublic));
    minipal::com_ptr<IMDInternalImport> currentImport;
    ASSERT_HRESULT_SUCCEEDED(currentPublic->QueryInterface(IID_IMDInternalImport, (void**)&currentImport));

    static auto VerifyMemberRef = [](IMDInternalImport* import, mdToken memberRef) -> std::vector<uint32_t>
    {
        std::vector<uint32_t> values;

        LPCSTR name;
        PCCOR_SIGNATURE ppvSigBlob;
        ULONG pcbSigBlob;
        HRESULT hr = import->GetNameAndSigOfMemberRef(memberRef,
            &ppvSigBlob,
            &pcbSigBlob,
            &name);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(HashByteArray(ppvSigBlob, pcbSigBlob));
            values.push_back(pcbSigBlob);
            values.push_back(HashString(name));
        }
        mdToken parent;
        hr = import->GetParentOfMemberRef(memberRef, &parent);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(parent);
        }
        hr = import->GetParentToken(memberRef, &parent);
        values.push_back(hr);
        if (hr == S_OK)
        {
            values.push_back(parent);
        }

        return values;
    };

    size_t stride;
    size_t count;

    TokenList memberrefs;
    ASSERT_EQUAL_AND_SET(memberrefs, EnumMemberRefs(baselineImport), EnumMemberRefs(currentImport));
    count = 0;
    stride = std::max(memberrefs.size() / 128, (size_t)16);
    for (auto memberref : memberrefs)
    {
        if (count++ % stride != 0)
            continue;

        ASSERT_THAT(VerifyMemberRef(currentImport, memberref), testing::ElementsAreArray(VerifyMemberRef(baselineImport, memberref)));
    }
}

INSTANTIATE_TEST_SUITE_P(InternalMetaDataLongRunningTest_CoreLibs, InternalMetaDataLongRunningTest, testing::ValuesIn(CoreLibFiles()), PrintName);
