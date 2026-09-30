#ifndef _SRC_INTERFACES_DNMDOWNER_HPP_
#define _SRC_INTERFACES_DNMDOWNER_HPP_

#include <internal/dnmd_platform.hpp>
#include "tearoffbase.hpp"
#include "controllingiunknown.hpp"

#include <cor.h>
#include <corhdr.h>

#include <cstdint>
#include <atomic>

EXTERN_GUID(IID_IDNMDOwner, 0x250ebc02, 0x1a92, 0x4638, 0xaa, 0x6c, 0x3d, 0x0f, 0x98, 0xb3, 0xa6, 0xfb);

// This interface is an IUnknown interface for the purposes of easy discovery.
struct IDNMDOwner : IUnknown
{
    virtual mdhandle_t MetaData() = 0;
    virtual bool IsReadWrite() = 0;
    virtual uint32_t DuplicateChecks() = 0;
};

class DNMDOwner;

// We use a reference wrapper around the handle to allow the handle to be swapped out.
// We plan to use swapping to implement table sorting as DNMD itself does not support
// sorting tables or remapping tokens.
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
    mdhandle_ptr _handle;
    malloc_ptr<void> _malloc_to_free;
    minipal::cotaskmem_ptr<void> _cotaskmem_to_free;
    uint32_t _duplicateChecks;
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
    DNMDOwner(IUnknown* controllingUnknown, mdhandle_ptr md_ptr, uint32_t duplicateChecks, bool readWrite)
        : TearOffBase(controllingUnknown)
        , _handle{ std::move(md_ptr) }
        , _malloc_to_free{ nullptr }
        , _cotaskmem_to_free{ nullptr }
        , _duplicateChecks{ duplicateChecks }
        , _readWrite{ readWrite }
    { }

    DNMDOwner(IUnknown* controllingUnknown, mdhandle_ptr md_ptr, malloc_ptr<void> mallocMem,
              minipal::cotaskmem_ptr<void> cotaskmemMem, uint32_t duplicateChecks, bool readWrite)
        : TearOffBase(controllingUnknown)
        , _handle{ std::move(md_ptr) }
        , _malloc_to_free{ std::move(mallocMem) }
        , _cotaskmem_to_free{ std::move(cotaskmemMem) }
        , _duplicateChecks{ duplicateChecks }
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

#endif // !_SRC_INTERFACES_DNMDOWNER_HPP_