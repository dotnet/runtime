// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _COREHOST_CLI_COMHOST_COMHOST_H_
#define _COREHOST_CLI_COMHOST_COMHOST_H_

#include <pal.h>
#include <map>
#include <cassert>

#define RETURN_IF_FAILED(exp) { hr = (exp); if (FAILED(hr)) { assert(false && #exp); return hr; } }

struct HResultException
{
    HRESULT hr;
};

#define RETURN_HRESULT_IF_EXCEPT(exp) try { exp; } catch (const HResultException &e) { return e.hr; } catch (const std::bad_alloc&) { return E_OUTOFMEMORY; }

// Should be shared with core-sdk for tooling support
#define RESOURCEID_CLSIDMAP 64
#define RESOURCETYPE_CLSIDMAP 1024

namespace std
{
    template<>
    struct less<CLSID>
    {
        bool operator()(const CLSID& l, const CLSID& r) const
        {
            return ::memcmp(&l, &r, sizeof(CLSID)) < 0;
        }
    };
}

namespace comhost
{
    struct clsid_map_entry
    {
        CLSID clsid;
        pal::string_t assembly;
        pal::string_t type;
        pal::string_t progid;
    };

    using clsid_map = std::map<CLSID, clsid_map_entry>;

    // Get the current CLSID map
    clsid_map get_clsid_map();
}

#endif /* _COREHOST_CLI_COMHOST_COMHOST_H_ */

