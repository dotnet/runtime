#ifndef _SRC_INTERFACES_CONTROLLINGIUNKNOWN_HPP_
#define _SRC_INTERFACES_CONTROLLINGIUNKNOWN_HPP_

#include "tearoffbase.hpp"
#include <minipal_com.h>
#include <atomic>
#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
#include <limits>
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE
#include <vector>
#include <new>
#include <utility>

#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
class ControllingIUnknown;

class MetadataScopeRegistry
{
public:
    static void RegisterScope(ControllingIUnknown* scope);
    static void UnregisterScope(ControllingIUnknown* scope) noexcept;
    static std::vector<minipal::com_ptr<IUnknown>> AcquireScopes();
};
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE

class ControllingIUnknown final : public IUnknown
{
#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
    friend class MetadataScopeRegistry;
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE

    std::atomic<int32_t> _refCount{ 1 };
#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
    std::atomic<bool> _registeredScope{ false };
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE
    std::vector<std::unique_ptr<TearOffUnknown>> _tearOffs;

#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
    // Called only under the registry lock, before a final Release can remove and delete this scope.
    bool TryAddRef() noexcept
    {
        int32_t count = _refCount.load(std::memory_order_relaxed);
        while (count > 0 && count < std::numeric_limits<int32_t>::max())
        {
            if (_refCount.compare_exchange_weak(count, count + 1, std::memory_order_acquire, std::memory_order_relaxed))
                return true;
        }
        return false;
    }
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE

public:
    ControllingIUnknown() = default;

    template<typename T, typename... Ts>
    T* CreateAndAddTearOff(Ts&&... args)
    {
        auto tear_off = std::make_unique<T>(this, std::forward<Ts>(args)...);
        T* tear_off_ptr = tear_off.get();
        _tearOffs.push_back(std::move(tear_off));
        return tear_off_ptr;
    }

public: // IUnknown
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
        /* [in] */ REFIID riid,
        /* [iid_is][out] */ _COM_Outptr_ void __RPC_FAR* __RPC_FAR* ppvObject) override
    {
        if (ppvObject == nullptr)
            return E_POINTER;

        if (riid == IID_IUnknown)
        {
            *ppvObject = static_cast<IUnknown*>(this);
            (void)AddRef();
            return S_OK;
        }

        for (std::unique_ptr<TearOffUnknown> const& tearOff: _tearOffs)
        {
            if (tearOff->TryGetInterfaceOnThis(riid, ppvObject))
            {
                (void)AddRef();
                return S_OK;
            }
        }
        
        *ppvObject = nullptr;
        return E_NOINTERFACE;
    }

    virtual ULONG STDMETHODCALLTYPE AddRef(void) override
    {
        return ++_refCount;
    }

    virtual ULONG STDMETHODCALLTYPE Release(void) override
    {
        uint32_t c = --_refCount;
        if (c == 0)
        {
#if defined(DNMD_ENABLE_LOADED_MODULES_CACHE)
            if (_registeredScope.load(std::memory_order_acquire))
                MetadataScopeRegistry::UnregisterScope(this);
#endif // DNMD_ENABLE_LOADED_MODULES_CACHE
            delete this;
        }
        return c;
    }
};

#endif // _SRC_INTERFACES_CONTROLLINGIUNKNOWN_HPP_