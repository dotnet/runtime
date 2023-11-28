#ifndef _SRC_INC_DNMD_HPP_
#define _SRC_INC_DNMD_HPP_

#include "dnmd.h"
#include <memory>

struct mdhandle_deleter_t final
{
    using pointer = mdhandle_t;
    void operator()(mdhandle_t handle)
    {
        ::md_destroy_handle(handle);
    }
};

// C++ lifetime wrapper for mdhandle_t type
using mdhandle_ptr = std::unique_ptr<mdhandle_t, mdhandle_deleter_t>;

#endif // _SRC_INC_DNMD_HPP_
