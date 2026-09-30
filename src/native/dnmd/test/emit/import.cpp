// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"

#include <array>
#include <atomic>
#include <cstring>
#include <thread>
#include <vector>

namespace
{
    void SaveScopeImage(IMetaDataEmit* emit, std::vector<uint8_t>& image)
    {
        DWORD size;
        ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
        image.resize(size);
        ASSERT_EQ(S_OK, emit->SaveToMemory(image.data(), size));
    }
}

TEST(Import, TypeDefWithoutAssemblyManifestInSameModule)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

    mdToken implements = mdTokenNil;
    mdTypeDef typeDef;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("SourceType"), tdPublic, mdTypeDefNil, &implements, &typeDef));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    mdTypeRef imported = mdTypeRefNil;
    ASSERT_EQ(S_OK, emit->DefineImportType(nullptr, nullptr, 0, import.p, typeDef, nullptr, &imported));
    EXPECT_EQ(typeDef, imported);
}

TEST(Import, TypeDefWithoutAssemblyManifestInDifferentModule)
{
    minipal::com_ptr<IMetaDataEmit> source;
    minipal::com_ptr<IMetaDataEmit> target;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(source));
    ASSERT_NO_FATAL_FAILURE(CreateEmit(target));

    mdToken implements = mdTokenNil;
    mdTypeDef typeDef;
    ASSERT_EQ(S_OK, source->DefineTypeDef(W("SourceType"), tdPublic, mdTypeDefNil, &implements, &typeDef));

    minipal::com_ptr<IMetaDataImport> sourceImport;
    minipal::com_ptr<IMetaDataAssemblyEmit> targetAssemblyEmit;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&sourceImport));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&targetAssemblyEmit));

    mdTypeRef imported = mdTypeRefNil;
    EXPECT_EQ(E_UNEXPECTED, target->DefineImportType(nullptr, nullptr, 0, sourceImport.p, typeDef, targetAssemblyEmit.p, &imported));
}

TEST(Import, TypeRefWithoutAssemblyManifestInDifferentModule)
{
    minipal::com_ptr<IMetaDataEmit> source;
    minipal::com_ptr<IMetaDataEmit> target;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(source));
    ASSERT_NO_FATAL_FAILURE(CreateEmit(target));

    mdTypeRef typeRef;
    ASSERT_EQ(S_OK, source->DefineTypeRefByName(mdTokenNil, W("SourceType"), &typeRef));
    ASSERT_EQ(1u, RidFromToken(typeRef));

    minipal::com_ptr<IMetaDataImport> sourceImport;
    minipal::com_ptr<IMetaDataAssemblyEmit> targetAssemblyEmit;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&sourceImport));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&targetAssemblyEmit));

    // The TypeDefOrRef coded index for TypeRef RID 1 is 5.
    std::array<uint8_t, 3> signature = {IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 5};
    std::array<uint8_t, 16> translated{};
    ULONG translatedLength = 0;
    EXPECT_EQ(E_UNEXPECTED, target->TranslateSigWithScope(nullptr, nullptr, 0, sourceImport.p,
        signature.data(), (ULONG)signature.size(), targetAssemblyEmit.p, target.p,
        translated.data(), (ULONG)translated.size(), &translatedLength));
}

TEST(Import, ReopenReplacesMetadataWithoutChangingScopeIdentity)
{
    minipal::com_ptr<IMetaDataEmit> first, second;
    ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(first));
    ASSERT_NO_FATAL_FAILURE(CreateEmit(second));

    mdTypeDef token, replacement;
    ASSERT_EQ(S_OK, first->DefineTypeDef(W("Before"), tdPublic, mdTypeDefNil, nullptr, &token));
    ASSERT_EQ(S_OK, second->DefineTypeDef(W("After"), tdPublic, mdTypeDefNil, nullptr, &replacement));
    ASSERT_EQ(token, replacement);
    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(SaveScopeImage(second.p, image));

    minipal::com_ptr<IMetaDataImport> importer;
    ASSERT_EQ(S_OK, first->QueryInterface(IID_IMetaDataImport, (void**)&importer));
    minipal::com_ptr<IUnknown> identityBefore, identityAfter;
    ASSERT_EQ(S_OK, importer->QueryInterface(IID_IUnknown, (void**)&identityBefore));
    MDUTF8CSTR oldName;
    ASSERT_EQ(S_OK, importer->GetNameFromToken(token, &oldName));
    EXPECT_STREQ("Before", oldName);

    BYTE invalid[] = { 0, 1, 2, 3 };
    EXPECT_EQ(E_INVALIDARG, ReOpenDNMDMetaDataWithMemory(importer.p, image.data(),
        (ULONG)image.size(), ofReadWriteMask));
    EXPECT_EQ(CLDB_E_FILE_CORRUPT, ReOpenDNMDMetaDataWithMemory(importer.p,
        invalid, sizeof(invalid), 0));
    EXPECT_STREQ("Before", oldName);

    ASSERT_EQ(S_OK, ReOpenDNMDMetaDataWithMemory(importer.p, image.data(),
        (ULONG)image.size(), 0));
    EXPECT_STREQ("Before", oldName);
    ASSERT_EQ(S_OK, importer->QueryInterface(IID_IUnknown, (void**)&identityAfter));
    EXPECT_EQ(identityBefore.p, identityAfter.p);
    mdTypeDef found;
    ASSERT_EQ(S_OK, importer->FindTypeDefByName(W("After"), mdTokenNil, &found));
    EXPECT_EQ(token, found);
    WCHAR name[16];
    ULONG nameLength;
    DWORD flags;
    mdToken extends;
    ASSERT_EQ(S_OK, importer->GetTypeDefProps(token, name, 16, &nameLength, &flags, &extends));
    EXPECT_EQ(W("After"), WSTR_string(name));
}

TEST(Import, ReopenTakesOwnershipOnlyAfterSuccess)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(SaveScopeImage(emit.p, image));

    minipal::cotaskmem_ptr<void> owned{ CoTaskMemAlloc(image.size()) };
    ASSERT_NE(nullptr, owned.get());
    std::memcpy(owned.get(), image.data(), image.size());

    minipal::com_ptr<IMetaDataImport> importer;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&importer));
    HRESULT hr = ReOpenDNMDMetaDataWithMemory(importer.p, owned.get(),
        (ULONG)image.size(), ofTakeOwnership);
    if (SUCCEEDED(hr))
        owned.release();
    ASSERT_EQ(S_OK, hr);
    mdModule module;
    ASSERT_EQ(S_OK, importer->GetModuleFromScope(&module));
    EXPECT_EQ(TokenFromRid(1, mdtModule), module);
}

TEST(Import, ReadOnlyReopenPreservesConcurrentReaders)
{
    minipal::com_ptr<IMetaDataEmit> first, second;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(first));
    ASSERT_NO_FATAL_FAILURE(CreateEmit(second));
    mdTypeDef before, after;
    ASSERT_EQ(S_OK, first->DefineTypeDef(W("Before"), tdPublic, mdTypeDefNil, nullptr, &before));
    ASSERT_EQ(S_OK, second->DefineTypeDef(W("After"), tdPublic, mdTypeDefNil, nullptr, &after));
    ASSERT_EQ(before, after);

    std::vector<uint8_t> firstImage, secondImage;
    ASSERT_NO_FATAL_FAILURE(SaveScopeImage(first.p, firstImage));
    ASSERT_NO_FATAL_FAILURE(SaveScopeImage(second.p, secondImage));
    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(firstImage.data(), (ULONG)firstImage.size(),
        ofReadOnly | ofCopyMemory, IID_IMetaDataImport, (IUnknown**)&import));

    std::atomic<bool> start{ false }, succeeded{ true };
    std::thread writer([&]
    {
        while (!start.load())
            std::this_thread::yield();
        for (int i = 0; i < 128; ++i)
        {
            std::vector<uint8_t> const& image = i % 2 == 0 ? secondImage : firstImage;
            if (ReOpenDNMDMetaDataWithMemory(import.p, image.data(), (ULONG)image.size(), 0) != S_OK)
            {
                succeeded = false;
                break;
            }
        }
    });
    start = true;
    for (int i = 0; i < 256; ++i)
    {
        WCHAR name[16];
        ULONG length;
        DWORD flags;
        mdToken extends;
        HRESULT hr = import->GetTypeDefProps(before, name, 16, &length, &flags, &extends);
        if (hr != S_OK || (WSTR_string(name) != W("Before") && WSTR_string(name) != W("After")))
        {
            succeeded = false;
            break;
        }
    }
    writer.join();
    EXPECT_TRUE(succeeded);
}
