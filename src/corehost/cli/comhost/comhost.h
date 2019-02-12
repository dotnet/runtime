// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        pal::string_t assembly;
        pal::string_t type;
    };

    using clsid_map = std::map<CLSID, clsid_map_entry>;

    // Get the current CLSID map
    clsid_map get_clsid_map();
}

#endif /* _COREHOST_CLI_COMHOST_COMHOST_H_ */

