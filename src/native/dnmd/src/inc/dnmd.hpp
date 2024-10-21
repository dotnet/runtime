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

struct md_added_row_t final
{
private:
    mdcursor_t new_row;
public:
    md_added_row_t() = default;
    explicit md_added_row_t(mdcursor_t row) : new_row{ row } {}
    md_added_row_t(md_added_row_t const& other) = delete;
    md_added_row_t(md_added_row_t&& other)
    {
        *this = std::move(other);
    }

    md_added_row_t& operator=(md_added_row_t const& other) = delete;
    md_added_row_t& operator=(md_added_row_t&& other)
    {
        new_row = other.new_row;
        other.new_row = {}; // Clear the other's row so we don't double-commit.
        return *this;
    }

    ~md_added_row_t()
    {
        md_commit_row_add(new_row);
    }

    operator mdcursor_t()
    {
        return new_row;
    }

    mdcursor_t* operator&()
    {
        return &new_row;
    }
};

#endif // _SRC_INC_DNMD_HPP_
