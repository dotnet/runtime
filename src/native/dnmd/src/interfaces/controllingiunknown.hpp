#ifndef _SRC_INTERFACES_CONTROLLINGIUNKNOWN_HPP_
#define _SRC_INTERFACES_CONTROLLINGIUNKNOWN_HPP_

#include "tearoffbase.hpp"
#include <dncp.h>
#include <atomic>
#include <vector>
#include <new>
#include <utility>

class ControllingIUnknown final : public IUnknown
{
private:
    std::atomic<int32_t> _refCount{ 1 };
    std::vector<std::unique_ptr<TearOffUnknown>> _tearOffs;
public:
    ControllingIUnknown() = default;

    template<typename T, typename... Ts>
    void CreateAndAddTearOff(Ts&&... args)
    {
        _tearOffs.push_back(std::make_unique<T>(this, std::forward<Ts>(args)...));
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
            delete this;
        }
        return c;
    }
};

#endif