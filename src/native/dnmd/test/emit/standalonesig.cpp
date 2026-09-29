#include "emit.hpp"
#include <array>
#include <chrono>
#include <future>
#include <thread>
#include <vector>
#include <gmock/gmock.h>

namespace
{
class BlockingStream final : public IStream
{
    std::promise<void>& _entered;
    std::shared_future<void> _release;

public:
    BlockingStream(std::promise<void>& entered, std::shared_future<void> release)
        : _entered(entered), _release(release) {}

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
}

TEST(StandaloneSig, Define)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdSignature sig;
    std::array<uint8_t, 3> signature = {0x01, 0x02, 0x03};
    ASSERT_EQ(S_OK, emit->GetTokenFromSig(signature.data(), (ULONG)signature.size(), &sig));
    ASSERT_EQ(1, RidFromToken(sig));
    ASSERT_EQ(mdtSignature, TypeFromToken(sig));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    PCCOR_SIGNATURE sigBlob;
    ULONG sigBlobLength;
    ASSERT_EQ(S_OK, import->GetSigFromToken(sig, &sigBlob, &sigBlobLength));
    EXPECT_THAT(std::vector<uint8_t>(sigBlob, sigBlob + sigBlobLength), testing::ContainerEq(std::vector<uint8_t>(signature.begin(), signature.end())));
}

TEST(StandaloneSig, TranslateAcrossThreadSafeScopes)
{
    minipal::com_ptr<IMetaDataEmit> source;
    minipal::com_ptr<IMetaDataEmit> target;
    ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(source));
    ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(target));

    minipal::com_ptr<IMetaDataImport> sourceImport;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&sourceImport));

    std::array<uint8_t, 2> signature = {IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4};
    std::array<uint8_t, 16> translated{};
    ULONG translatedLength = 0;
    ASSERT_EQ(S_OK, source->TranslateSigWithScope(nullptr, nullptr, 0, sourceImport.p,
        signature.data(), (ULONG)signature.size(), nullptr, target.p,
        translated.data(), (ULONG)translated.size(), &translatedLength));
    ASSERT_EQ(signature.size(), translatedLength);
    EXPECT_THAT(std::vector<uint8_t>(translated.begin(), translated.begin() + translatedLength),
        testing::ElementsAreArray(signature));

    minipal::com_ptr<IMetaDataImport> targetImport;
    ASSERT_EQ(S_OK, target->QueryInterface(IID_IMetaDataImport, (void**)&targetImport));
    ASSERT_EQ(S_OK, target->TranslateSigWithScope(nullptr, nullptr, 0, targetImport.p,
        signature.data(), (ULONG)signature.size(), nullptr, target.p,
        translated.data(), (ULONG)translated.size(), &translatedLength));
    EXPECT_EQ(signature.size(), translatedLength);
}

TEST(StandaloneSig, TranslateLongSignature)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    constexpr uint8_t localCount = 65;
    std::vector<uint8_t> signature(2 + localCount, ELEMENT_TYPE_I4);
    signature[0] = IMAGE_CEE_CS_CALLCONV_LOCAL_SIG;
    signature[1] = localCount;
    std::vector<uint8_t> translated(signature.size());
    ULONG translatedLength = 0;

    ASSERT_EQ(S_OK, emit->TranslateSigWithScope(nullptr, nullptr, 0, import.p,
        signature.data(), (ULONG)signature.size(), nullptr, emit.p,
        translated.data(), (ULONG)translated.size(), &translatedLength));
    ASSERT_EQ(signature.size(), translatedLength);
    EXPECT_THAT(translated, testing::ElementsAreArray(signature));
}

TEST(StandaloneSig, TranslateWaitsForDestinationWriteLock)
{
    minipal::com_ptr<IMetaDataEmit> source;
    minipal::com_ptr<IMetaDataEmit> target;
    ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(source));
    ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(target));

    minipal::com_ptr<IMetaDataImport> sourceImport;
    ASSERT_EQ(S_OK, source->QueryInterface(IID_IMetaDataImport, (void**)&sourceImport));

    std::promise<void> entered;
    std::promise<void> release;
    auto enteredFuture = entered.get_future();
    BlockingStream stream(entered, release.get_future().share());
    HRESULT saveResult = S_OK;
    std::thread saver([&] { saveResult = target->SaveToStream(&stream, 0); });

    if (enteredFuture.wait_for(std::chrono::seconds(5)) != std::future_status::ready)
    {
        release.set_value();
        saver.join();
        FAIL() << "SaveToStream did not enter the blocking stream";
        return;
    }

    std::array<uint8_t, 2> signature = {IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4};
    std::array<uint8_t, 16> translated{};
    ULONG translatedLength = 0;
    std::promise<void> started;
    auto startedFuture = started.get_future();
    auto translation = std::async(std::launch::async, [&]
    {
        started.set_value();
        return source->TranslateSigWithScope(nullptr, nullptr, 0, sourceImport.p,
            signature.data(), (ULONG)signature.size(), nullptr, target.p,
            translated.data(), (ULONG)translated.size(), &translatedLength);
    });

    if (startedFuture.wait_for(std::chrono::seconds(5)) != std::future_status::ready)
    {
        release.set_value();
        saver.join();
        FAIL() << "Signature translation did not start";
        return;
    }

    bool blocked = translation.wait_for(std::chrono::milliseconds(250)) == std::future_status::timeout;
    release.set_value();
    saver.join();

    EXPECT_EQ(E_FAIL, saveResult);
    EXPECT_TRUE(blocked);
    EXPECT_EQ(S_OK, translation.get());
    EXPECT_EQ(signature.size(), translatedLength);
}

TEST(ThreadSafety, SignatureReadWaitsForWriteLock)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateThreadSafeEmit(emit));

    std::array<uint8_t, 2> signature = {IMAGE_CEE_CS_CALLCONV_FIELD, ELEMENT_TYPE_I4};
    mdSignature token;
    ASSERT_EQ(S_OK, emit->GetTokenFromSig(signature.data(), (ULONG)signature.size(), &token));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    std::promise<void> entered;
    std::promise<void> release;
    auto enteredFuture = entered.get_future();
    BlockingStream stream(entered, release.get_future().share());
    HRESULT saveResult = S_OK;
    std::thread saver([&] { saveResult = emit->SaveToStream(&stream, 0); });

    if (enteredFuture.wait_for(std::chrono::seconds(5)) != std::future_status::ready)
    {
        release.set_value();
        saver.join();
        FAIL() << "SaveToStream did not enter the blocking stream";
        return;
    }

    PCCOR_SIGNATURE blob = nullptr;
    ULONG blobLength = 0;
    std::promise<void> started;
    auto startedFuture = started.get_future();
    auto reader = std::async(std::launch::async, [&]
    {
        started.set_value();
        return import->GetSigFromToken(token, &blob, &blobLength);
    });

    if (startedFuture.wait_for(std::chrono::seconds(5)) != std::future_status::ready)
    {
        release.set_value();
        saver.join();
        FAIL() << "Signature read did not start";
        return;
    }

    bool blocked = reader.wait_for(std::chrono::milliseconds(250)) == std::future_status::timeout;
    release.set_value();
    saver.join();

    EXPECT_EQ(E_FAIL, saveResult);
    EXPECT_TRUE(blocked);
    ASSERT_EQ(S_OK, reader.get());
    EXPECT_THAT(std::vector<uint8_t>(blob, blob + blobLength), testing::ElementsAreArray(signature));
}