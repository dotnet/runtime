// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



#pragma once

namespace jitstd
{

namespace utility
{
    // Template class for scoped execution of a lambda.
    // Usage:
    //
    //  auto code = [&]
    //  {
    //      JITDUMP("finally()");
    //  };
    //  jitstd::utility::scoped_code<decltype(code)> finally(code);
    //  "code" will execute when "finally" goes out of scope.
    template <typename T>
    class scoped_code
    {
    public:
        const T& l;
        scoped_code(const T& l) : l(l) { }
        ~scoped_code() { l(); }
    }; 
    
 
    // Helper to allocate objects of any type, given an allocator of void type.
    //
    // @param alloc An allocator of void type used to create an allocator of type T.
    // @param count The number of objects of type T that need to be allocated.
    //
    // @return A pointer to an object or an array of objects that was allocated.
    template <typename T>
    inline
    static T* allocate(jitstd::allocator<void>& alloc, size_t count = 1)
    {
        return jitstd::allocator<T>(alloc).allocate(count);
    }

    // Ensures that "wset" is the union of the initial state of "wset" and "rset".
    // Elements from "rset" that were not in "wset" are added to "cset."
    template <typename Set>
    bool set_union(Set& wset, const Set& rset, Set& cset)
    {
        bool change = false;
        for (typename Set::const_iterator i = rset.begin(); i != rset.end(); ++i)
        {
            jitstd::pair<typename Set::iterator, bool> result = wset.insert(*i);
            if (result.second)
            {
                change = true;
                cset.insert(*i);
            }
        }
        return change;
    }

    template <typename Set>
    bool set_union(Set& wset, const Set& rset)
    {
        bool change = false;
        for (typename Set::const_iterator i = rset.begin(); i != rset.end(); ++i)
        {
            jitstd::pair<typename Set::iterator, bool> result = wset.insert(*i);
            change |= result.second;
        }
        return change;
    }

    template <typename Set>
    bool set_difference(Set& wset, const Set& rset)
    {
        bool change = false;
        for (typename Set::const_iterator i = rset.begin(); i != rset.end(); ++i)
        {
            if (wset.find(*i) != wset.end())
            {
                wset.erase(*i);
                change = true;
            }
        }

        return change;
    }
} // end of namespace utility.

} // end of namespace jitstd.
