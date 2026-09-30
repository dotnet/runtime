// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal_com.h>
#include <cor.h>
#include <metadata.h>
#include <dnmd_interfaces.hpp>
#include <mdinternalemit.h>
#include <minipal/rwlock.h>
#include "dnmdowner.hpp"

#include <gtest/gtest.h>
#include <atomic>
#include <chrono>
#include <future>
#include <thread>
#include <vector>

namespace
{
    void CreateImage(std::vector<uint8_t>& image)
    {
        minipal::com_ptr<IMetaDataDispenser> dispenser;
        ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));

        minipal::com_ptr<IMetaDataEmit> emit;
        ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
            IID_IMetaDataEmit, (IUnknown**)&emit));
        mdTypeDef existing;
        ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Existing"), tdPublic, mdTypeDefNil, nullptr, &existing));
        ASSERT_EQ(TokenFromRid(2, mdtTypeDef), existing);

        DWORD size;
        ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
        image.resize(size);
        ASSERT_EQ(S_OK, emit->SaveToMemory(image.data(), size));
    }

    void OpenReadOnly(IMetaDataDispenser* dispenser, std::vector<uint8_t> const& image,
                      minipal::com_ptr<IMDInternalImport>& import)
    {
        ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), (ULONG)image.size(),
            ofReadOnly | ofCopyMemory, IID_IMDInternalImport, (IUnknown**)&import));
    }

    void ExpectSameComIdentity(IUnknown* left, IUnknown* right)
    {
        minipal::com_ptr<IUnknown> leftIdentity, rightIdentity;
        ASSERT_EQ(S_OK, left->QueryInterface(IID_IUnknown, (void**)&leftIdentity));
        ASSERT_EQ(S_OK, right->QueryInterface(IID_IUnknown, (void**)&rightIdentity));
        EXPECT_EQ(leftIdentity.p, rightIdentity.p);
    }

    class BlockingStream final : public IStream
    {
        std::promise<void>& _entered;
        std::shared_future<void> _release;
    public:
        BlockingStream(std::promise<void>& entered, std::shared_future<void> release)
            : _entered(entered), _release(std::move(release)) {}

        HRESULT STDMETHODCALLTYPE QueryInterface(REFIID, void** result) override
        {
            *result = nullptr;
            return E_NOINTERFACE;
        }
        ULONG STDMETHODCALLTYPE AddRef() override { return 1; }
        ULONG STDMETHODCALLTYPE Release() override { return 1; }
        HRESULT STDMETHODCALLTYPE Read(void*, ULONG, ULONG*) override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE Write(void const*, ULONG, ULONG*) override
        {
            _entered.set_value();
            _release.wait();
            return E_FAIL;
        }
        HRESULT STDMETHODCALLTYPE Seek(LARGE_INTEGER, DWORD, ULARGE_INTEGER*) override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE SetSize(ULARGE_INTEGER) override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE CopyTo(IStream*, ULARGE_INTEGER, ULARGE_INTEGER*, ULARGE_INTEGER*) override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE Commit(DWORD) override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE Revert() override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE LockRegion(ULARGE_INTEGER, ULARGE_INTEGER, DWORD) override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE UnlockRegion(ULARGE_INTEGER, ULARGE_INTEGER, DWORD) override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE Stat(STATSTG*, DWORD) override { return E_NOTIMPL; }
        HRESULT STDMETHODCALLTYPE Clone(IStream**) override { return E_NOTIMPL; }
    };

    void ExpectInternalReadWaitsForPublicWrite(IMDInternalImport* internal, mdTypeDef token)
    {
        minipal::com_ptr<IMetaDataEmit> emit;
        ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMetaDataEmit, (void**)&emit));

        std::promise<void> entered, release;
        auto enteredFuture = entered.get_future();
        BlockingStream stream(entered, release.get_future().share());
        HRESULT saveResult = S_OK;
        std::thread writer([&] { saveResult = emit->SaveToStream(&stream, 0); });

        if (enteredFuture.wait_for(std::chrono::seconds(5)) != std::future_status::ready)
        {
            release.set_value();
            writer.join();
            FAIL() << "SaveToStream did not enter the blocking stream";
            return;
        }

        std::promise<void> started;
        auto startedFuture = started.get_future();
        auto reader = std::async(std::launch::async, [&]
        {
            started.set_value();
            char const* name = nullptr;
            char const* nameSpace = nullptr;
            HRESULT hr = internal->GetNameOfTypeDef(token, &name, &nameSpace);
            return hr == S_OK && name != nullptr && name[0] != '\0';
        });
        if (startedFuture.wait_for(std::chrono::seconds(5)) != std::future_status::ready)
        {
            release.set_value();
            writer.join();
            FAIL() << "Internal reader did not start";
            return;
        }

        bool blocked = reader.wait_for(std::chrono::milliseconds(250)) == std::future_status::timeout;
        release.set_value();
        writer.join();
        EXPECT_EQ(E_FAIL, saveResult);
        EXPECT_TRUE(blocked);
        EXPECT_TRUE(reader.get());
    }
}

TEST(InternalConversion, NewWritableScopeSharesPublicAndInternalIdentity)
{
    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));

    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMDInternalImport, (IUnknown**)&internal));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMetaDataEmit, (void**)&emit));
    minipal::com_ptr<IMDInternalImport> bridged;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalImport, (void**)&bridged));
    EXPECT_EQ(internal.p, bridged.p);
    ExpectSameComIdentity(internal.p, emit.p);
    minipal::com_ptr<IMetaDataImport2> import;
    ASSERT_EQ(S_OK, GetDNMDPublicInterfaceFromInternal(internal.p,
        IID_IMetaDataImport2, (void**)&import));
    ExpectSameComIdentity(internal.p, import.p);

    minipal::com_ptr<IDNMDOwner> owner;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IDNMDOwner, (void**)&owner));
    EXPECT_TRUE(owner->IsReadWrite());
    EXPECT_EQ(nullptr, internal->GetReaderWriterLock());

    minipal_rwlock external{};
    ASSERT_TRUE(minipal_rwlock_init(&external));
    EXPECT_EQ(E_NOTIMPL, internal->SetReaderWriterLock(&external));
    minipal_rwlock_destroy(&external);

    mdTypeDef type;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Created"), tdPublic, mdTypeDefNil, nullptr, &type));
    mdTypeDef found;
    ASSERT_EQ(S_OK, internal->FindTypeDef("", "Created", mdTokenNil, &found));
    EXPECT_EQ(type, found);

    IMDInternalImport* alreadyWritable = nullptr;
    EXPECT_EQ(S_FALSE, ConvertDNMDInternalImport(internal.p, &alreadyWritable));
    EXPECT_EQ(internal.p, alreadyWritable);
}

TEST(InternalConversion, ReadOnlyConversionClonesDataAndBridgesIdentity)
{
    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(CreateImage(image));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMDInternalImport> old;
    ASSERT_NO_FATAL_FAILURE(OpenReadOnly(dispenser.p, image, old));

    minipal::com_ptr<IDNMDOwner> oldOwner;
    ASSERT_EQ(S_OK, old->QueryInterface(IID_IDNMDOwner, (void**)&oldOwner));
    EXPECT_FALSE(oldOwner->IsReadWrite());
    minipal::com_ptr<IMetaDataImport2> oldPublic;
    ASSERT_EQ(S_OK, old->QueryInterface(IID_IMetaDataImport2, (void**)&oldPublic));
    ExpectSameComIdentity(old.p, oldPublic.p);
    minipal::com_ptr<IMDInternalImport> oldBridged;
    ASSERT_EQ(S_OK, oldPublic->QueryInterface(IID_IMDInternalImport, (void**)&oldBridged));
    EXPECT_EQ(old.p, oldBridged.p);
    minipal::com_ptr<IUnknown> oldCached;
    oldCached.Attach(old->GetCachedPublicInterface(TRUE));
    ASSERT_NE(nullptr, oldCached.p);
    ExpectSameComIdentity(old.p, oldCached.p);
    minipal::com_ptr<IMetaDataEmit> oldEmit;
    EXPECT_EQ(E_NOINTERFACE, old->QueryInterface(IID_IMetaDataEmit, (void**)&oldEmit));

    IMDInternalImport* result = nullptr;
    ASSERT_EQ(S_OK, ConvertDNMDInternalImport(old.p, &result));
    minipal::com_ptr<IMDInternalImport> writable;
    writable.Attach(result);
    ASSERT_NE(old.p, writable.p);

    minipal::com_ptr<IDNMDOwner> writableOwner;
    ASSERT_EQ(S_OK, writable->QueryInterface(IID_IDNMDOwner, (void**)&writableOwner));
    EXPECT_TRUE(writableOwner->IsReadWrite());
    EXPECT_NE(oldOwner->MetaData(), writableOwner->MetaData());

    minipal::com_ptr<IMetaDataEmit> emit;
    minipal::com_ptr<IMetaDataImport2> import;
    ASSERT_EQ(S_OK, writable->QueryInterface(IID_IMetaDataEmit, (void**)&emit));
    ASSERT_EQ(S_OK, writable->QueryInterface(IID_IMetaDataImport2, (void**)&import));
    ExpectSameComIdentity(writable.p, emit.p);
    ExpectSameComIdentity(writable.p, import.p);
    minipal::com_ptr<IMDInternalImport> bridged;
    ASSERT_EQ(S_OK, import->QueryInterface(IID_IMDInternalImport, (void**)&bridged));
    EXPECT_EQ(writable.p, bridged.p);

    mdTypeDef existing;
    ASSERT_EQ(S_OK, writable->FindTypeDef("", "Existing", mdTokenNil, &existing));
    EXPECT_EQ(TokenFromRid(2, mdtTypeDef), existing);
    mdTypeDef added;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Added"), tdPublic, mdTypeDefNil, nullptr, &added));
    mdTypeDef found;
    ASSERT_EQ(S_OK, writable->FindTypeDef("", "Added", mdTokenNil, &found));
    EXPECT_EQ(added, found);
    ASSERT_EQ(S_OK, import->FindTypeDefByName(W("Added"), mdTokenNil, &found));
    EXPECT_EQ(added, found);
    EXPECT_EQ(CLDB_E_RECORD_NOTFOUND, old->FindTypeDef("", "Added", mdTokenNil, &found));
    ASSERT_EQ(S_OK, old->FindTypeDef("", "Existing", mdTokenNil, &found));
    EXPECT_EQ(existing, found);

    minipal::com_ptr<IUnknown> cached;
    cached.Attach(writable->GetCachedPublicInterface(TRUE));
    ASSERT_NE(nullptr, cached.p);
    ExpectSameComIdentity(writable.p, cached.p);
    EXPECT_EQ(S_OK, writable->SetCachedPublicInterface(import.p));
    EXPECT_EQ(E_INVALIDARG, writable->SetCachedPublicInterface(oldPublic.p));

    minipal::com_ptr<IMDInternalImport> transferred;
    ASSERT_EQ(S_OK, old->QueryInterface(IID_IMDInternalImport, (void**)&transferred));
    ASSERT_EQ(S_OK, writable->SetUserContextData(transferred.Detach()));
    EXPECT_EQ(E_UNEXPECTED, writable->SetUserContextData(old.p));
    oldBridged.Release();
    oldCached.Release();
    old.Release();
    oldPublic.Release();
    oldOwner.Release();
    ASSERT_EQ(S_OK, writable->FindTypeDef("", "Added", mdTokenNil, &found));
    EXPECT_EQ(added, found);

    IMDInternalImport* alreadyWritable = nullptr;
    EXPECT_EQ(S_FALSE, ConvertDNMDInternalImport(writable.p, &alreadyWritable));
    EXPECT_EQ(writable.p, alreadyWritable);
    EXPECT_EQ(E_NOTIMPL, writable->ApplyEditAndContinue(nullptr, 0, nullptr));
}

TEST(InternalConversion, CompressedInternalReadDefaultsToReadOnly)
{
    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(CreateImage(image));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), (ULONG)image.size(),
        ofRead | ofCopyMemory, IID_IMDInternalImport, (IUnknown**)&internal));
    minipal::com_ptr<IDNMDOwner> owner;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IDNMDOwner, (void**)&owner));
    EXPECT_FALSE(owner->IsReadWrite());
    minipal::com_ptr<IMetaDataEmit> emit;
    EXPECT_EQ(E_NOINTERFACE, internal->QueryInterface(IID_IMetaDataEmit, (void**)&emit));

    minipal::com_ptr<IMetaDataImport2> promotedPublic;
    ASSERT_EQ(S_OK, GetDNMDPublicInterfaceFromInternal(internal.p,
        IID_IMetaDataImport2, (void**)&promotedPublic));
    minipal::com_ptr<IMDInternalImport> promotedInternal;
    ASSERT_EQ(S_OK, promotedPublic->QueryInterface(IID_IMDInternalImport, (void**)&promotedInternal));
    minipal::com_ptr<IDNMDOwner> promotedOwner;
    ASSERT_EQ(S_OK, promotedInternal->QueryInterface(IID_IDNMDOwner, (void**)&promotedOwner));
    EXPECT_TRUE(promotedOwner->IsReadWrite());
    EXPECT_NE(owner->MetaData(), promotedOwner->MetaData());
    ExpectSameComIdentity(promotedPublic.p, promotedInternal.p);

    IMDInternalImport* result = nullptr;
    ASSERT_EQ(S_OK, ConvertDNMDInternalImport(internal.p, &result));
    minipal::com_ptr<IMDInternalImport> writable;
    writable.Attach(result);
    ASSERT_EQ(S_OK, writable->QueryInterface(IID_IMetaDataEmit, (void**)&emit));
    mdTypeDef added;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Writable"), tdPublic, mdTypeDefNil, nullptr, &added));
    mdTypeDef found;
    ASSERT_EQ(S_OK, writable->FindTypeDef("", "Writable", mdTokenNil, &found));
    EXPECT_EQ(added, found);
    EXPECT_EQ(CLDB_E_RECORD_NOTFOUND, internal->FindTypeDef("", "Writable", mdTokenNil, &found));

    minipal::com_ptr<IMetaDataEmit> publicEmit;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), (ULONG)image.size(),
        ofRead | ofCopyMemory, IID_IMetaDataEmit, (IUnknown**)&publicEmit));
    minipal::com_ptr<IDNMDOwner> publicOwner;
    ASSERT_EQ(S_OK, publicEmit->QueryInterface(IID_IDNMDOwner, (void**)&publicOwner));
    EXPECT_TRUE(publicOwner->IsReadWrite());
}

TEST(InternalConversion, UncompressedInternalReadIsAlreadyWritable)
{
    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(CreateImage(image));

    bool renamed = false;
    for (size_t i = 0; i + 3 < image.size(); ++i)
    {
        if (image[i] == '#' && image[i + 1] == '~' && image[i + 2] == 0 && image[i + 3] == 0)
        {
            image[i + 1] = '-';
            renamed = true;
            break;
        }
    }
    ASSERT_TRUE(renamed);

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(image.data(), (ULONG)image.size(),
        ofRead | ofCopyMemory, IID_IMDInternalImport, (IUnknown**)&internal));
    minipal::com_ptr<IDNMDOwner> owner;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IDNMDOwner, (void**)&owner));
    EXPECT_TRUE(owner->IsReadWrite());
    EXPECT_EQ(nullptr, internal->GetReaderWriterLock());
    IMDInternalImport* unchanged = nullptr;
    EXPECT_EQ(S_FALSE, ConvertDNMDInternalImport(internal.p, &unchanged));
    EXPECT_EQ(internal.p, unchanged);
}

TEST(InternalConversion, SeparateConversionsHaveIndependentMetadata)
{
    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(CreateImage(image));

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMDInternalImport> original;
    ASSERT_NO_FATAL_FAILURE(OpenReadOnly(dispenser.p, image, original));

    IMDInternalImport *firstRaw = nullptr, *secondRaw = nullptr;
    ASSERT_EQ(S_OK, ConvertDNMDInternalImport(original.p, &firstRaw));
    ASSERT_EQ(S_OK, ConvertDNMDInternalImport(original.p, &secondRaw));
    minipal::com_ptr<IMDInternalImport> first, second;
    first.Attach(firstRaw);
    second.Attach(secondRaw);
    minipal::com_ptr<IUnknown> firstIdentity, secondIdentity;
    ASSERT_EQ(S_OK, first->QueryInterface(IID_IUnknown, (void**)&firstIdentity));
    ASSERT_EQ(S_OK, second->QueryInterface(IID_IUnknown, (void**)&secondIdentity));
    EXPECT_NE(firstIdentity.p, secondIdentity.p);

    minipal::com_ptr<IMetaDataEmit> firstEmit;
    ASSERT_EQ(S_OK, first->QueryInterface(IID_IMetaDataEmit, (void**)&firstEmit));
    mdTypeDef added;
    ASSERT_EQ(S_OK, firstEmit->DefineTypeDef(W("OnlyFirst"), tdPublic, mdTypeDefNil, nullptr, &added));
    mdTypeDef found;
    ASSERT_EQ(S_OK, first->FindTypeDef("", "OnlyFirst", mdTokenNil, &found));
    EXPECT_EQ(added, found);
    EXPECT_EQ(CLDB_E_RECORD_NOTFOUND, second->FindTypeDef("", "OnlyFirst", mdTokenNil, &found));
    EXPECT_EQ(CLDB_E_RECORD_NOTFOUND, original->FindTypeDef("", "OnlyFirst", mdTokenNil, &found));
}

TEST(InternalConversion, ReadOnlyConversionPreservesInitialUpdateMode)
{
    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(CreateImage(image));

    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateExtension;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));

    minipal::com_ptr<IMDInternalImport> readOnly;
    ASSERT_NO_FATAL_FAILURE(OpenReadOnly(dispenser.p, image, readOnly));
    IMDInternalImport* converted = nullptr;
    ASSERT_EQ(S_OK, ConvertDNMDInternalImport(readOnly.p, &converted));
    minipal::com_ptr<IMDInternalImport> writable;
    writable.Attach(converted);
    minipal::com_ptr<IMDInternalEmit> emitter;
    ASSERT_EQ(S_OK, writable->QueryInterface(IID_IMDInternalEmit, (void**)&emitter));
    ULONG previous = UINT32_MAX;
    ASSERT_EQ(S_OK, emitter->SetMDUpdateMode(MDUpdateFull, &previous));
    EXPECT_EQ(MDUpdateExtension, previous);
}

TEST(InternalConversion, ThreadSafeScopeSerializesInternalReadsWithWrites)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDThreadSafetyOn;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));

    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMDInternalImport, (IUnknown**)&internal));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMetaDataEmit, (void**)&emit));
    ExpectSameComIdentity(internal.p, emit.p);
    minipal_rwlock* lock = internal->GetReaderWriterLock();
    ASSERT_NE(nullptr, lock);
    EXPECT_EQ(S_OK, internal->SetReaderWriterLock(lock));
    EXPECT_EQ(E_NOTIMPL, internal->SetReaderWriterLock(nullptr));
    mdTypeDef type;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("ThreadSafe"), tdPublic, mdTypeDefNil, nullptr, &type));
    mdTypeDef nested, found;
    ASSERT_EQ(S_OK, emit->DefineNestedType(W("Nested"), tdNestedPublic, mdTypeDefNil,
        nullptr, type, &nested));
    ASSERT_EQ(S_OK, internal->FindTypeDef("", "Nested", type, &found));
    EXPECT_EQ(nested, found);
    ULONG isDual = 0;
    EXPECT_EQ(CLDB_E_RECORD_NOTFOUND, internal->GetIsDualOfTypeDef(type, &isDual));
    EXPECT_EQ(1u, isDual);
    ExpectInternalReadWaitsForPublicWrite(internal.p, type);
}

TEST(InternalConversion, ThreadSafeInternalReadersObserveConcurrentEmission)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDThreadSafetyOn;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));

    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMDInternalImport, (IUnknown**)&internal));
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, internal->QueryInterface(IID_IMetaDataEmit, (void**)&emit));
    mdTypeDef stable;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("Stable"), tdPublic, mdTypeDefNil, nullptr, &stable));

    std::atomic<bool> start{ false }, succeeded{ true };
    std::thread writer([&]
    {
        while (!start.load())
            std::this_thread::yield();
        for (int i = 0; i < 128; ++i)
        {
            mdTypeDef added;
            if (emit->DefineTypeDef(W("Other"), tdPublic, mdTypeDefNil, nullptr, &added) != S_OK)
            {
                succeeded = false;
                break;
            }
        }
    });
    start = true;
    for (int i = 0; i < 128; ++i)
    {
        DWORD flags;
        mdToken extends;
        if (internal->GetTypeDefProps(stable, &flags, &extends) != S_OK || flags != tdPublic)
        {
            succeeded = false;
            break;
        }
    }
    writer.join();
    EXPECT_TRUE(succeeded);
    EXPECT_EQ(130u, internal->GetCountWithTokenKind(mdtTypeDef));
}

TEST(InternalConversion, ConvertingReadOnlyScopeKeepsOptionsAndSerializesReads)
{
    std::vector<uint8_t> image;
    ASSERT_NO_FATAL_FAILURE(CreateImage(image));

    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDDupTypeDef;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    minipal::com_ptr<IMDInternalImport> readOnly;
    ASSERT_NO_FATAL_FAILURE(OpenReadOnly(dispenser.p, image, readOnly));

    V_UI4(&option) = MDNoDupChecks;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataCheckDuplicatesFor, &option));
    IMDInternalImport* result = nullptr;
    ASSERT_EQ(S_OK, ConvertDNMDInternalImport(readOnly.p, &result));
    minipal::com_ptr<IMDInternalImport> writable;
    writable.Attach(result);

    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, writable->QueryInterface(IID_IMetaDataEmit, (void**)&emit));
    mdTypeDef existing;
    ASSERT_EQ(META_S_DUPLICATE, emit->DefineTypeDef(W("Existing"), tdPublic,
        mdTypeDefNil, nullptr, &existing));
    EXPECT_EQ(TokenFromRid(2, mdtTypeDef), existing);
    ExpectInternalReadWaitsForPublicWrite(writable.p, existing);
}

TEST(InternalConversion, RejectsMissingParameters)
{
    IMDInternalImport* converted = nullptr;
    EXPECT_EQ(E_INVALIDARG, ConvertDNMDInternalImport(nullptr, &converted));
    EXPECT_EQ(nullptr, converted);

    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    minipal::com_ptr<IMDInternalImport> internal;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMDInternalImport, (IUnknown**)&internal));
    EXPECT_EQ(E_INVALIDARG, ConvertDNMDInternalImport(internal.p, nullptr));
    converted = internal.p;
    EXPECT_EQ(E_INVALIDARG, ConvertDNMDInternalImport(nullptr, &converted));
    EXPECT_EQ(nullptr, converted);
    void* publicInterface = internal.p;
    EXPECT_EQ(E_INVALIDARG, GetDNMDPublicInterfaceFromInternal(nullptr,
        IID_IMetaDataImport2, &publicInterface));
    EXPECT_EQ(nullptr, publicInterface);
    EXPECT_EQ(E_INVALIDARG, GetDNMDPublicInterfaceFromInternal(internal.p,
        IID_IMetaDataImport2, nullptr));
    EXPECT_EQ(E_NOINTERFACE, GetDNMDPublicInterfaceFromInternal(internal.p,
        IID_IMetaDataDispenser, &publicInterface));
    EXPECT_EQ(nullptr, publicInterface);
}
