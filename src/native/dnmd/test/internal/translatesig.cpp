// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cstddef>
#include <minipal_com.h>
#include <minipal/rwlock.h>
#include <cor.h>
#include <quickbytes.h>
#include <metadata.h>
#include <dnmd_interfaces.hpp>

#include <gtest/gtest.h>
#include <array>
#include <chrono>
#include <cstring>
#include <future>
#include <limits>
#include <vector>

namespace
{
    HRESULT CreateScope(bool threadSafe, minipal::com_ptr<IMetaDataEmit>& emit)
    {
        minipal::com_ptr<IMetaDataDispenserEx> dispenser;
        HRESULT hr = GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser);
        if (FAILED(hr))
            return hr;

        if (threadSafe)
        {
            VARIANT option{};
            V_VT(&option) = VT_UI4;
            V_UI4(&option) = MDThreadSafetyOn;
            hr = dispenser->SetOption(MetaDataThreadSafetyOptions, &option);
            if (FAILED(hr))
                return hr;
        }

        return dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&emit);
    }

    constexpr std::array<BYTE, 3> TypeRefSignature =
        { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 0x05 };
}

TEST(InternalTranslateSig, SameScopePreservesTypeRefTokenAndOwnsBuffer)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, CreateScope(true, emit));
    mdTypeRef typeRef;
    ASSERT_EQ(S_OK, emit->DefineTypeRefByName(TokenFromRid(1, mdtModule), W("N.Same"), &typeRef));
    ASSERT_EQ(TokenFromRid(1, mdtTypeRef), typeRef);

    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalImport, (void**)&internal));
    std::array<BYTE, 3> signature = TypeRefSignature;
    CQuickBytes output;
    ASSERT_NE(nullptr, output.AllocNoThrow(6));
    std::memset(output.Ptr(), 0xCC, output.Size());
    ULONG length = UINT32_MAX;

    ASSERT_EQ(S_OK, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        signature.data(), (ULONG)signature.size(), nullptr, emit.p, &output, &length));
    EXPECT_EQ(signature.size(), length);
    EXPECT_EQ(signature.size(), output.Size());
    EXPECT_EQ(0, std::memcmp(TypeRefSignature.data(), output.Ptr(), length));
    EXPECT_EQ(1u, internal->GetCountWithTokenKind(mdtTypeRef));

    signature.fill(0);
    internal.Release();
    emit.Release();
    EXPECT_EQ(0, std::memcmp(TypeRefSignature.data(), output.Ptr(), length));
}

TEST(InternalTranslateSig, LongSignatureUsesOwnedHeapBuffer)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, CreateScope(false, emit));
    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalImport, (void**)&internal));

    constexpr size_t localCount = 520;
    std::vector<BYTE> signature(3 + localCount, ELEMENT_TYPE_I4);
    signature[0] = IMAGE_CEE_CS_CALLCONV_LOCAL_SIG;
    signature[1] = 0x82;
    signature[2] = 0x08;
    std::vector<BYTE> expected = signature;
    CQuickBytes output;
    ULONG length = 0;

    ASSERT_EQ(S_OK, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        signature.data(), (ULONG)signature.size(), nullptr, emit.p, &output, &length));
    EXPECT_EQ(expected.size(), length);
    EXPECT_EQ(expected.size(), output.Size());
    EXPECT_EQ(0, std::memcmp(expected.data(), output.Ptr(), length));

    std::memset(signature.data(), 0, signature.size());
    signature.clear();
    internal.Release();
    emit.Release();
    EXPECT_EQ(0, std::memcmp(expected.data(), output.Ptr(), length));
}

TEST(InternalTranslateSig, ReadOnlySourceCreatesReferencesInAnotherScope)
{
    minipal::com_ptr<IMetaDataEmit> sourceEmit;
    ASSERT_EQ(S_OK, CreateScope(false, sourceEmit));
    minipal::com_ptr<IMetaDataAssemblyEmit> sourceAssembly;
    ASSERT_EQ(S_OK, sourceEmit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&sourceAssembly));
    ASSEMBLYMETADATA metadata{};
    mdAssembly assembly;
    ASSERT_EQ(S_OK, sourceAssembly->DefineAssembly(nullptr, 0, 0, W("Source"),
        &metadata, 0, &assembly));
    mdAssemblyRef dependency;
    ASSERT_EQ(S_OK, sourceAssembly->DefineAssemblyRef(nullptr, 0, W("Dependency"),
        &metadata, nullptr, 0, 0, &dependency));
    mdTypeRef sourceRef;
    ASSERT_EQ(S_OK, sourceEmit->DefineTypeRefByName(dependency,
        W("N.External"), &sourceRef));
    ASSERT_EQ(TokenFromRid(1, mdtTypeRef), sourceRef);

    DWORD imageSize;
    ASSERT_EQ(S_OK, sourceEmit->GetSaveSize(cssAccurate, &imageSize));
    std::vector<BYTE> image(imageSize);
    ASSERT_EQ(S_OK, sourceEmit->SaveToMemory(image.data(), imageSize));
    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMDInternalImport> source;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), imageSize,
        ofReadOnly | ofCopyMemory, IID_IMDInternalImport, (IUnknown**)&source));

    minipal::com_ptr<IMetaDataEmit> target;
    ASSERT_EQ(S_OK, CreateScope(true, target));
    minipal::com_ptr<IMetaDataAssemblyEmit> targetAssembly;
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&targetAssembly));
    ASSERT_EQ(S_OK, targetAssembly->DefineAssembly(nullptr, 0, 0, W("Target"),
        &metadata, 0, &assembly));
    mdTypeRef existing;
    ASSERT_EQ(S_OK, target->DefineTypeRefByName(TokenFromRid(1, mdtModule),
        W("N.Existing"), &existing));
    ASSERT_EQ(TokenFromRid(1, mdtTypeRef), existing);

    CQuickBytes output;
    ULONG length = UINT32_MAX;
    ASSERT_EQ(S_OK, source->TranslateSigWithScope(source.p, nullptr, 0,
        TypeRefSignature.data(), (ULONG)TypeRefSignature.size(), targetAssembly.p,
        target.p, &output, &length));
    constexpr std::array<BYTE, 3> expected =
        { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 0x09 };
    ASSERT_EQ(expected.size(), length);
    EXPECT_EQ(expected.size(), output.Size());
    EXPECT_EQ(0, std::memcmp(expected.data(), output.Ptr(), length));

    minipal::com_ptr<IMDInternalImport> targetInternal;
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMDInternalImport, (void**)&targetInternal));
    EXPECT_EQ(2u, targetInternal->GetCountWithTokenKind(mdtTypeRef));
    EXPECT_EQ(1u, targetInternal->GetCountWithTokenKind(mdtAssemblyRef));
    mdToken resolutionScope;
    ASSERT_EQ(S_OK, targetInternal->GetResolutionScopeOfTypeRef(TokenFromRid(2, mdtTypeRef),
        &resolutionScope));
    EXPECT_EQ(TokenFromRid(1, mdtAssemblyRef), resolutionScope);
    mdTypeRef imported;
    ASSERT_EQ(S_OK, targetInternal->FindTypeRefByName("N", "External", resolutionScope, &imported));
    EXPECT_EQ(TokenFromRid(2, mdtTypeRef), imported);
    LPCSTR assemblyName = nullptr;
    ASSERT_EQ(S_OK, targetInternal->GetAssemblyRefProps(resolutionScope, nullptr, nullptr,
        &assemblyName, nullptr, nullptr, nullptr, nullptr));
    EXPECT_STREQ("Dependency", assemblyName);

    source.Release();
    sourceEmit.Release();
    sourceAssembly.Release();
    dispenser.Release();
    image.clear();
    EXPECT_EQ(0, std::memcmp(expected.data(), output.Ptr(), length));
}

TEST(InternalTranslateSig, ModuleScopedTypeRefCreatesSourceAssemblyRef)
{
    minipal::com_ptr<IMetaDataEmit> sourceEmit, target;
    ASSERT_EQ(S_OK, CreateScope(false, sourceEmit));
    ASSERT_EQ(S_OK, CreateScope(false, target));
    minipal::com_ptr<IMDInternalImport> source, targetInternal;
    ASSERT_EQ(S_OK, sourceEmit->QueryInterface(IID_IMDInternalImport, (void**)&source));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMDInternalImport, (void**)&targetInternal));
    ASSERT_EQ(0u, targetInternal->GetCountWithTokenKind(mdtAssemblyRef));
    minipal::com_ptr<IMetaDataAssemblyEmit> sourceAssembly, targetAssembly;
    ASSERT_EQ(S_OK, sourceEmit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&sourceAssembly));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&targetAssembly));

    ASSEMBLYMETADATA metadata{};
    mdAssembly assembly;
    ASSERT_EQ(S_OK, sourceAssembly->DefineAssembly(nullptr, 0, 0, W("Source"),
        &metadata, 0, &assembly));
    ASSERT_EQ(S_OK, targetAssembly->DefineAssembly(nullptr, 0, 0, W("Target"),
        &metadata, 0, &assembly));
    mdTypeRef typeRef, existing;
    ASSERT_EQ(S_OK, sourceEmit->DefineTypeRefByName(TokenFromRid(1, mdtModule),
        W("N.Local"), &typeRef));
    ASSERT_EQ(TokenFromRid(1, mdtTypeRef), typeRef);
    ASSERT_EQ(S_OK, target->DefineTypeRefByName(TokenFromRid(1, mdtModule),
        W("N.Existing"), &existing));
    ASSERT_EQ(TokenFromRid(1, mdtTypeRef), existing);

    CQuickBytes output;
    ULONG length = 0;
    ASSERT_EQ(S_OK, source->TranslateSigWithScope(source.p, nullptr, 0,
        TypeRefSignature.data(), (ULONG)TypeRefSignature.size(),
        targetAssembly.p, target.p, &output, &length));
    constexpr std::array<BYTE, 3> expected =
        { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 0x09 };
    ASSERT_EQ(expected.size(), length);
    EXPECT_EQ(0, std::memcmp(expected.data(), output.Ptr(), length));
    EXPECT_EQ(2u, targetInternal->GetCountWithTokenKind(mdtTypeRef));
    EXPECT_EQ(1u, targetInternal->GetCountWithTokenKind(mdtAssemblyRef));

    mdToken scope;
    ASSERT_EQ(S_OK, targetInternal->GetResolutionScopeOfTypeRef(TokenFromRid(2, mdtTypeRef), &scope));
    ASSERT_EQ(TokenFromRid(1, mdtAssemblyRef), scope);
    LPCSTR assemblyName = nullptr;
    ASSERT_EQ(S_OK, targetInternal->GetAssemblyRefProps(scope, nullptr, nullptr,
        &assemblyName, nullptr, nullptr, nullptr, nullptr));
    EXPECT_STREQ("Source", assemblyName);
}

TEST(InternalTranslateSig, NestedTypeRefPreservesItsEnclosingScope)
{
    minipal::com_ptr<IMetaDataEmit> sourceEmit, target;
    ASSERT_EQ(S_OK, CreateScope(false, sourceEmit));
    ASSERT_EQ(S_OK, CreateScope(false, target));
    minipal::com_ptr<IMetaDataAssemblyEmit> sourceAssembly;
    ASSERT_EQ(S_OK, sourceEmit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&sourceAssembly));
    ASSEMBLYMETADATA metadata{};
    mdAssemblyRef dependency;
    ASSERT_EQ(S_OK, sourceAssembly->DefineAssemblyRef(nullptr, 0, W("Dependency"),
        &metadata, nullptr, 0, 0, &dependency));
    mdTypeRef enclosing, nested, existing;
    ASSERT_EQ(S_OK, sourceEmit->DefineTypeRefByName(dependency, W("N.Outer"), &enclosing));
    ASSERT_EQ(S_OK, sourceEmit->DefineTypeRefByName(enclosing, W("Nested"), &nested));
    ASSERT_EQ(TokenFromRid(2, mdtTypeRef), nested);
    ASSERT_EQ(S_OK, target->DefineTypeRefByName(TokenFromRid(1, mdtModule),
        W("N.Existing"), &existing));

    minipal::com_ptr<IMDInternalImport> source, targetInternal;
    ASSERT_EQ(S_OK, sourceEmit->QueryInterface(IID_IMDInternalImport, (void**)&source));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMDInternalImport, (void**)&targetInternal));
    ASSERT_EQ(0u, targetInternal->GetCountWithTokenKind(mdtAssemblyRef));
    constexpr std::array<BYTE, 3> nestedSignature =
        { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 0x09 };
    CQuickBytes output;
    ULONG length = 0;
    ASSERT_EQ(S_OK, source->TranslateSigWithScope(nullptr, nullptr, 0,
        nestedSignature.data(), (ULONG)nestedSignature.size(),
        nullptr, target.p, &output, &length));
    constexpr std::array<BYTE, 3> expected =
        { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 0x0D };
    ASSERT_EQ(expected.size(), length);
    EXPECT_EQ(0, std::memcmp(expected.data(), output.Ptr(), length));
    EXPECT_EQ(3u, targetInternal->GetCountWithTokenKind(mdtTypeRef));
    EXPECT_EQ(1u, targetInternal->GetCountWithTokenKind(mdtAssemblyRef));

    mdToken scope;
    ASSERT_EQ(S_OK, targetInternal->GetResolutionScopeOfTypeRef(TokenFromRid(2, mdtTypeRef), &scope));
    EXPECT_EQ(TokenFromRid(1, mdtAssemblyRef), scope);
    ASSERT_EQ(S_OK, targetInternal->GetResolutionScopeOfTypeRef(TokenFromRid(3, mdtTypeRef), &scope));
    EXPECT_EQ(TokenFromRid(2, mdtTypeRef), scope);
    mdTypeRef found;
    ASSERT_EQ(S_OK, targetInternal->FindTypeRefByName("", "Nested", scope, &found));
    EXPECT_EQ(TokenFromRid(3, mdtTypeRef), found);
}

TEST(InternalTranslateSig, ExpandedTypeRefTokenReportsExactLength)
{
    minipal::com_ptr<IMetaDataEmit> sourceEmit, target;
    ASSERT_EQ(S_OK, CreateScope(false, sourceEmit));
    ASSERT_EQ(S_OK, CreateScope(false, target));
    minipal::com_ptr<IMetaDataAssemblyEmit> sourceAssembly;
    ASSERT_EQ(S_OK, sourceEmit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&sourceAssembly));
    ASSEMBLYMETADATA metadata{};
    mdAssemblyRef dependency;
    ASSERT_EQ(S_OK, sourceAssembly->DefineAssemblyRef(nullptr, 0, W("Dependency"),
        &metadata, nullptr, 0, 0, &dependency));
    mdTypeRef sourceRef;
    ASSERT_EQ(S_OK, sourceEmit->DefineTypeRefByName(dependency, W("N.Expanded"), &sourceRef));
    ASSERT_EQ(TokenFromRid(1, mdtTypeRef), sourceRef);

    for (unsigned i = 0; i < 31; ++i)
    {
        WCHAR name[] = { 'F', 'i', 'l', 'l', 'e', 'r', static_cast<WCHAR>('A' + i), 0 };
        mdTypeRef filler;
        ASSERT_EQ(S_OK, target->DefineTypeRefByName(TokenFromRid(1, mdtModule), name, &filler));
        ASSERT_EQ(TokenFromRid(i + 1, mdtTypeRef), filler);
    }

    minipal::com_ptr<IMDInternalImport> source, targetInternal;
    ASSERT_EQ(S_OK, sourceEmit->QueryInterface(IID_IMDInternalImport, (void**)&source));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMDInternalImport, (void**)&targetInternal));
    CQuickBytes output;
    ULONG length = 0;
    ASSERT_EQ(S_OK, source->TranslateSigWithScope(nullptr, nullptr, 0,
        TypeRefSignature.data(), (ULONG)TypeRefSignature.size(),
        nullptr, target.p, &output, &length));
    constexpr std::array<BYTE, 4> expected =
        { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_CLASS, 0x80, 0x81 };
    ASSERT_EQ(expected.size(), length);
    EXPECT_EQ(expected.size(), output.Size());
    EXPECT_EQ(0, std::memcmp(expected.data(), output.Ptr(), length));
    EXPECT_EQ(32u, targetInternal->GetCountWithTokenKind(mdtTypeRef));
}

TEST(InternalTranslateSig, NullAssemblyImportWaitsForDestinationWriteLock)
{
    minipal::com_ptr<IMetaDataEmit> sourceEmit, target;
    ASSERT_EQ(S_OK, CreateScope(true, sourceEmit));
    ASSERT_EQ(S_OK, CreateScope(true, target));
    minipal::com_ptr<IMDInternalImport> source, targetInternal;
    ASSERT_EQ(S_OK, sourceEmit->QueryInterface(IID_IMDInternalImport, (void**)&source));
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMDInternalImport, (void**)&targetInternal));
    minipal_rwlock* destinationLock = targetInternal->GetReaderWriterLock();
    ASSERT_NE(nullptr, destinationLock);

    constexpr std::array<BYTE, 2> signature = { IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4 };
    CQuickBytes output;
    ULONG length = 0;
    ASSERT_TRUE(minipal_rwlock_enter_write(destinationLock));

    std::promise<void> started;
    std::future<void> startedFuture = started.get_future();
    std::future<HRESULT> translation = std::async(std::launch::async, [&]
    {
        started.set_value();
        return source->TranslateSigWithScope(nullptr, nullptr, 0,
            signature.data(), (ULONG)signature.size(), nullptr, target.p, &output, &length);
    });

    EXPECT_EQ(std::future_status::ready, startedFuture.wait_for(std::chrono::seconds(5)));
    bool blocked = translation.wait_for(std::chrono::milliseconds(250)) == std::future_status::timeout;
    minipal_rwlock_leave_write(destinationLock);

    EXPECT_TRUE(blocked);
    EXPECT_EQ(S_OK, translation.get());
    EXPECT_EQ(signature.size(), length);
    EXPECT_EQ(signature.size(), output.Size());
    EXPECT_EQ(0, std::memcmp(signature.data(), output.Ptr(), length));
}

TEST(InternalTranslateSig, RejectsInvalidArgumentsAndMissingTypeRef)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, CreateScope(false, emit));
    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalImport, (void**)&internal));

    CQuickBytes output;
    ULONG length = 42;
    EXPECT_EQ(E_INVALIDARG, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        TypeRefSignature.data(), (ULONG)TypeRefSignature.size(), nullptr, nullptr, &output, &length));
    EXPECT_EQ(E_INVALIDARG, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        TypeRefSignature.data(), (ULONG)TypeRefSignature.size(), nullptr, emit.p, nullptr, &length));
    EXPECT_EQ(E_INVALIDARG, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        TypeRefSignature.data(), (ULONG)TypeRefSignature.size(), nullptr, emit.p, &output, nullptr));
    EXPECT_EQ(E_INVALIDARG, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        nullptr, (ULONG)TypeRefSignature.size(), nullptr, emit.p, &output, &length));
    EXPECT_EQ(E_INVALIDARG, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        TypeRefSignature.data(), 0, nullptr, emit.p, &output, &length));
    EXPECT_EQ(E_INVALIDARG, internal->TranslateSigWithScope(nullptr, nullptr, 1,
        TypeRefSignature.data(), (ULONG)TypeRefSignature.size(), nullptr, emit.p, &output, &length));
    EXPECT_EQ(CLDB_E_TOO_BIG, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        TypeRefSignature.data(), std::numeric_limits<ULONG>::max(), nullptr, emit.p, &output, &length));
    EXPECT_EQ(42u, length);
    EXPECT_EQ(CLDB_E_FILE_CORRUPT, internal->TranslateSigWithScope(nullptr, nullptr, 0,
        TypeRefSignature.data(), (ULONG)TypeRefSignature.size(), nullptr, emit.p, &output, &length));
    EXPECT_EQ(0u, length);
}
