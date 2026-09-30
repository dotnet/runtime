#ifndef _SRC_INTERFACES_DNMDOWNER_HPP_
#define _SRC_INTERFACES_DNMDOWNER_HPP_

#include <internal/dnmd_platform.hpp>
#include "tearoffbase.hpp"
#include "controllingiunknown.hpp"

#include <cor.h>
#include <corhdr.h>

#include <cstdint>
#include <atomic>
#include <memory>
#include <new>
#include <utility>

EXTERN_GUID(IID_IDNMDOwner, 0x250ebc02, 0x1a92, 0x4638, 0xaa, 0x6c, 0x3d, 0x0f, 0x98, 0xb3, 0xa6, 0xfb);

inline HRESULT ValidateDNMDUpdateMode(uint32_t mode)
{
    return mode == MDUpdateFull || mode == MDUpdateExtension || mode == MDUpdateENC
        ? S_OK : E_INVALIDARG;
}

// This interface is an IUnknown interface for the purposes of easy discovery.
struct IDNMDOwner : IUnknown
{
    virtual mdhandle_t MetaData() = 0;
    virtual bool IsReadWrite() = 0;
    virtual uint32_t DuplicateChecks() = 0;
    virtual uint32_t UpdateMode() = 0;
    virtual HRESULT SetUpdateMode(uint32_t mode) = 0;
    virtual HRESULT ReplaceMetaData(mdhandle_ptr replacement, malloc_ptr<void> backing) = 0;
};

class DNMDOwner;

// A reference wrapper lets EnC replace the handle without changing COM identity.
// This is explicitly a non-owning view as this view will be passed to other tear-offs of the same object,
// which would otherwise lead to memory leaks.
class mdhandle_view final
{
private:
    DNMDOwner* _owner;
public:
    explicit mdhandle_view(DNMDOwner* owner)
        : _owner{ owner }
    {
    }

    mdhandle_view(mdhandle_view const& other) = default;

    mdhandle_view(mdhandle_view&& other) = default;

    mdhandle_view& operator=(mdhandle_view const& other) = default;

    mdhandle_view& operator=(mdhandle_view&& other) = default;

    mdhandle_t get() const;
    bool IsReadWrite() const;
    uint32_t DuplicateChecks() const;
    uint32_t UpdateMode() const;
    HRESULT SetUpdateMode(uint32_t mode) const;
    HRESULT ReplaceMetaData(mdhandle_ptr replacement, malloc_ptr<void> backing) const;

    bool operator==(std::nullptr_t) const
    {
        return get() == nullptr;
    }
    bool operator!=(std::nullptr_t) const
    {
        return get() != nullptr;
    }
};

inline bool operator==(std::nullptr_t, mdhandle_view const& view)
{
    return view == nullptr;
}

inline bool operator!=(std::nullptr_t, mdhandle_view const& view)
{
    return view != nullptr;
}

class DNMDOwner final : public TearOffBase<IDNMDOwner>
{
private:
    // Keep old heap pointers and active cursor-backed enumerators valid after an EnC swap.
    struct PreviousVersion
    {
        std::unique_ptr<PreviousVersion> previous;
        malloc_ptr<void> mallocMemory;
        minipal::cotaskmem_ptr<void> cotaskmemMemory;
        mdhandle_ptr handle;
    };

    malloc_ptr<void> _malloc_to_free;
    minipal::cotaskmem_ptr<void> _cotaskmem_to_free;
    mdhandle_ptr _handle;
    std::unique_ptr<PreviousVersion> _previous;
    uint32_t _duplicateChecks;
    uint32_t _updateMode;
    bool _readWrite;

protected:
    virtual bool TryGetInterfaceOnThis(REFIID riid, void** ppvObject) override
    {
        assert(riid != IID_IUnknown);
        if (riid == IID_IDNMDOwner)
        {
            *ppvObject = static_cast<IDNMDOwner*>(this);
            return true;
        }
        return false;
    }

public:
    DNMDOwner(IUnknown* controllingUnknown, mdhandle_ptr md_ptr, uint32_t duplicateChecks, uint32_t updateMode, bool readWrite)
        : TearOffBase(controllingUnknown)
        , _malloc_to_free{ nullptr }
        , _cotaskmem_to_free{ nullptr }
        , _handle{ std::move(md_ptr) }
        , _duplicateChecks{ duplicateChecks }
        , _updateMode{ updateMode }
        , _readWrite{ readWrite }
    { }

    DNMDOwner(IUnknown* controllingUnknown, mdhandle_ptr md_ptr, malloc_ptr<void> mallocMem,
              minipal::cotaskmem_ptr<void> cotaskmemMem, uint32_t duplicateChecks, uint32_t updateMode, bool readWrite)
        : TearOffBase(controllingUnknown)
        , _malloc_to_free{ std::move(mallocMem) }
        , _cotaskmem_to_free{ std::move(cotaskmemMem) }
        , _handle{ std::move(md_ptr) }
        , _duplicateChecks{ duplicateChecks }
        , _updateMode{ updateMode }
        , _readWrite{ readWrite }
    { }

    virtual ~DNMDOwner() noexcept = default;

public: // IDNMDOwner
    mdhandle_t MetaData() override
    {
        return _handle.get();
    }

    bool IsReadWrite() override
    {
        return _readWrite;
    }

    uint32_t DuplicateChecks() override
    {
        return _duplicateChecks;
    }

    uint32_t UpdateMode() override
    {
        return _updateMode;
    }

    HRESULT SetUpdateMode(uint32_t mode) override
    {
        HRESULT hr = ValidateDNMDUpdateMode(mode);
        if (FAILED(hr))
            return hr;
        _updateMode = mode;
        return S_OK;
    }

    HRESULT ReplaceMetaData(mdhandle_ptr replacement, malloc_ptr<void> backing) override
    {
        if (replacement == nullptr || backing == nullptr)
            return E_INVALIDARG;

        std::unique_ptr<PreviousVersion> previous{ new (std::nothrow) PreviousVersion{} };
        if (previous == nullptr)
            return E_OUTOFMEMORY;

        previous->handle = std::move(_handle);
        previous->mallocMemory = std::move(_malloc_to_free);
        previous->cotaskmemMemory = std::move(_cotaskmem_to_free);
        previous->previous = std::move(_previous);
        _previous = std::move(previous);
        _malloc_to_free = std::move(backing);
        _handle = std::move(replacement);
        return S_OK;
    }
};

inline mdhandle_t mdhandle_view::get() const
{
    return _owner->MetaData();
}

inline bool mdhandle_view::IsReadWrite() const
{
    return _owner->IsReadWrite();
}

inline uint32_t mdhandle_view::DuplicateChecks() const
{
    return _owner->DuplicateChecks();
}

inline uint32_t mdhandle_view::UpdateMode() const
{
    return _owner->UpdateMode();
}

inline HRESULT mdhandle_view::SetUpdateMode(uint32_t mode) const
{
    return _owner->SetUpdateMode(mode);
}

inline HRESULT mdhandle_view::ReplaceMetaData(mdhandle_ptr replacement, malloc_ptr<void> backing) const
{
    return _owner->ReplaceMetaData(std::move(replacement), std::move(backing));
}

#endif // !_SRC_INTERFACES_DNMDOWNER_HPP_