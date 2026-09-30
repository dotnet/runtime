// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"

#include <array>
#include <atomic>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <thread>
#include <vector>
#include <minipal/guid.h>

namespace
{
    void SaveScopeImage(IMetaDataEmit* emit, std::vector<uint8_t>& image)
    {
        DWORD size;
        ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
        image.resize(size);
        ASSERT_EQ(S_OK, emit->SaveToMemory(image.data(), size));
    }

    struct TempMetadataFile
    {
        std::filesystem::path path;

        ~TempMetadataFile()
        {
            std::error_code ignored;
            std::filesystem::remove(path, ignored);
        }

        bool Write(std::vector<uint8_t> const& bytes) const
        {
            std::ofstream file(path, std::ios::binary | std::ios::trunc);
            if (!file)
                return false;
            file.write(reinterpret_cast<char const*>(bytes.data()), static_cast<std::streamsize>(bytes.size()));
            return file.good();
        }
    };

    std::vector<uint8_t> WrapMetadataInPE(std::vector<uint8_t> const& metadata, bool pe64)
    {
        constexpr size_t ntOffset = 0x80;
        constexpr size_t sectionOffset = 0x200;
        constexpr size_t metadataOffset = 0x300;
        std::vector<uint8_t> image(metadataOffset + metadata.size());

        IMAGE_DOS_HEADER dos{};
        dos.e_magic = IMAGE_DOS_SIGNATURE;
        dos.e_lfanew = static_cast<LONG>(ntOffset);
        std::memcpy(image.data(), &dos, sizeof(dos));

        size_t ntSize;
        if (pe64)
        {
            IMAGE_NT_HEADERS64 nt{};
            nt.Signature = 0x00004550;
            nt.FileHeader.Machine = IMAGE_FILE_MACHINE_AMD64;
            nt.FileHeader.NumberOfSections = 1;
            nt.FileHeader.SizeOfOptionalHeader = static_cast<WORD>(sizeof(IMAGE_OPTIONAL_HEADER64));
            nt.OptionalHeader.Magic = 0x20b;
            nt.OptionalHeader.NumberOfRvaAndSizes = 16;
            nt.OptionalHeader.SizeOfHeaders = static_cast<DWORD>(sectionOffset);
            nt.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress = 0x2000;
            nt.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size = sizeof(IMAGE_COR20_HEADER);
            ntSize = sizeof(nt);
            std::memcpy(image.data() + ntOffset, &nt, ntSize);
        }
        else
        {
            IMAGE_NT_HEADERS32 nt{};
            nt.Signature = 0x00004550;
            nt.FileHeader.Machine = IMAGE_FILE_MACHINE_I386;
            nt.FileHeader.NumberOfSections = 1;
            nt.FileHeader.SizeOfOptionalHeader = static_cast<WORD>(sizeof(IMAGE_OPTIONAL_HEADER32));
            nt.OptionalHeader.Magic = 0x10b;
            nt.OptionalHeader.NumberOfRvaAndSizes = 16;
            nt.OptionalHeader.SizeOfHeaders = static_cast<DWORD>(sectionOffset);
            nt.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].VirtualAddress = 0x2000;
            nt.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Size = sizeof(IMAGE_COR20_HEADER);
            ntSize = sizeof(nt);
            std::memcpy(image.data() + ntOffset, &nt, ntSize);
        }

        IMAGE_SECTION_HEADER section{};
        section.VirtualAddress = 0x2000;
        section.Misc.VirtualSize = static_cast<DWORD>(image.size() - sectionOffset);
        section.PointerToRawData = static_cast<DWORD>(sectionOffset);
        section.SizeOfRawData = static_cast<DWORD>(image.size() - sectionOffset);
        std::memcpy(image.data() + ntOffset + ntSize, &section, sizeof(section));

        IMAGE_COR20_HEADER cor{};
        cor.cb = sizeof(cor);
        cor.MetaData.VirtualAddress = 0x2100;
        cor.MetaData.Size = static_cast<DWORD>(metadata.size());
        std::memcpy(image.data() + sectionOffset, &cor, sizeof(cor));
        std::memcpy(image.data() + metadataOffset, metadata.data(), metadata.size());
        return image;
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

TEST(Import, OpenScopeReadsMetadataAndManagedPEFiles)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdTypeDef expected;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("FromFile"), tdPublic, mdTypeDefNil, nullptr, &expected));
    std::vector<uint8_t> metadata;
    ASSERT_NO_FATAL_FAILURE(SaveScopeImage(emit.p, metadata));

    GUID identifier;
    ASSERT_TRUE(minipal_guid_v4_create(&identifier));
#ifdef BUILD_WINDOWS
    TempMetadataFile file{ std::filesystem::temp_directory_path() /
        (std::wstring(L"dnmd-") + std::to_wstring(identifier.Data1) +
            std::to_wstring(identifier.Data2) + L"-\u00e9.dll") };
    WSTR_string path = file.path.wstring();
#else
    TempMetadataFile file{ std::filesystem::temp_directory_path() /
        std::filesystem::u8path("dnmd-" + std::to_string(identifier.Data1) +
            std::to_string(identifier.Data2) + "-\u00e9.dll") };
    std::u16string utf16Path = file.path.u16string();
    WSTR_string path(utf16Path.begin(), utf16Path.end());
#endif

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    for (std::vector<uint8_t> const& contents : { metadata, WrapMetadataInPE(metadata, false),
        WrapMetadataInPE(metadata, true) })
    {
        ASSERT_TRUE(file.Write(contents));
        minipal::com_ptr<IMetaDataImport> import;
        ASSERT_EQ(S_OK, dispenser->OpenScope(path.c_str(), ofRead,
            IID_IMetaDataImport, (IUnknown**)&import));
        mdTypeDef actual;
        ASSERT_EQ(S_OK, import->FindTypeDefByName(W("FromFile"), mdTokenNil, &actual));
        EXPECT_EQ(expected, actual);
    }

    ASSERT_TRUE(file.Write(WrapMetadataInPE(metadata, false)));
    WSTR_string uri = W("file:");
    uri += path;
    minipal::com_ptr<IMetaDataImport> prefixed;
    ASSERT_EQ(S_OK, dispenser->OpenScope(uri.c_str(), ofRead,
        IID_IMetaDataImport, (IUnknown**)&prefixed));

    ASSERT_TRUE(file.Write({ 'M', 'Z', 0, 0 }));
    IUnknown* invalid = reinterpret_cast<IUnknown*>(1);
    EXPECT_EQ(COR_E_BADIMAGEFORMAT, dispenser->OpenScope(path.c_str(), ofRead,
        IID_IMetaDataImport, &invalid));
    EXPECT_EQ(nullptr, invalid);
    EXPECT_EQ(E_INVALIDARG, dispenser->OpenScope(path.c_str(), ofTakeOwnership,
        IID_IMetaDataImport, &invalid));
    EXPECT_EQ(nullptr, invalid);
}
