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
#if defined(DNMD_ENABLE_INTERNAL_INTERFACES)
    virtual void const* MetaDataHandleSlot() = 0;
#endif // DNMD_ENABLE_INTERNAL_INTERFACES
    virtual bool IsReadWrite() = 0;
    virtual uint32_t DuplicateChecks() = 0;
    virtual uint32_t UpdateMode() = 0;
    virtual HRESULT SetUpdateMode(uint32_t mode) = 0;
    virtual HRESULT ReplaceMetaData(mdhandle_ptr replacement, malloc_ptr<void> backing) = 0;
    virtual HRESULT GetPEKind(DWORD* kind, DWORD* machine) = 0;
    virtual void SetPEKind(DWORD kind, DWORD machine) = 0;
    virtual void ClearPEKind() = 0;
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
#if defined(DNMD_ENABLE_INTERNAL_INTERFACES)
    void const* MetaDataHandleSlot() const;
#endif // DNMD_ENABLE_INTERNAL_INTERFACES
    bool IsReadWrite() const;
    uint32_t DuplicateChecks() const;
    uint32_t UpdateMode() const;
    HRESULT SetUpdateMode(uint32_t mode) const;
    HRESULT ReplaceMetaData(mdhandle_ptr replacement, malloc_ptr<void> backing) const;
    HRESULT GetPEKind(DWORD* kind, DWORD* machine) const;
    void SetPEKind(DWORD kind, DWORD machine) const;
    void ClearPEKind() const;

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
    std::atomic<mdhandle_t> _currentHandle;
    std::unique_ptr<PreviousVersion> _previous;
    std::atomic<bool> _hasPEKind{ false };
    DWORD _peKind = 0;
    DWORD _machine = 0;
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
        , _currentHandle{ _handle.get() }
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
        , _currentHandle{ _handle.get() }
        , _duplicateChecks{ duplicateChecks }
        , _updateMode{ updateMode }
        , _readWrite{ readWrite }
    { }

    virtual ~DNMDOwner() noexcept = default;

public: // IDNMDOwner
    mdhandle_t MetaData() override
    {
        return _currentHandle.load(std::memory_order_acquire);
    }

#if defined(DNMD_ENABLE_INTERNAL_INTERFACES)
    void const* MetaDataHandleSlot() override
    {
        static_assert(sizeof(_currentHandle) == sizeof(mdhandle_t), "cDAC requires a pointer-sized metadata handle slot");
        return &_currentHandle;
    }
#endif // DNMD_ENABLE_INTERNAL_INTERFACES

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
        _currentHandle.store(_handle.get(), std::memory_order_release);
        return S_OK;
    }

    HRESULT GetPEKind(DWORD* kind, DWORD* machine) override
    {
        bool found = _hasPEKind.load(std::memory_order_acquire);
        if (kind != nullptr)
            *kind = found ? _peKind : 0;
        if (machine != nullptr)
            *machine = found ? _machine : 0;
        return found ? S_OK : S_FALSE;
    }

    void SetPEKind(DWORD kind, DWORD machine) override
    {
        _peKind = kind;
        _machine = machine;
        _hasPEKind.store(true, std::memory_order_release);
    }

    void ClearPEKind() override
    {
        _hasPEKind.store(false, std::memory_order_release);
    }
};

inline mdhandle_t mdhandle_view::get() const
{
    return _owner->MetaData();
}

#if defined(DNMD_ENABLE_INTERNAL_INTERFACES)
inline void const* mdhandle_view::MetaDataHandleSlot() const
{
    return _owner->MetaDataHandleSlot();
}
#endif // DNMD_ENABLE_INTERNAL_INTERFACES

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

inline HRESULT mdhandle_view::GetPEKind(DWORD* kind, DWORD* machine) const
{
    return _owner->GetPEKind(kind, machine);
}

inline void mdhandle_view::SetPEKind(DWORD kind, DWORD machine) const
{
    _owner->SetPEKind(kind, machine);
}

inline void mdhandle_view::ClearPEKind() const
{
    _owner->ClearPEKind();
}

#endif // !_SRC_INTERFACES_DNMDOWNER_HPP_