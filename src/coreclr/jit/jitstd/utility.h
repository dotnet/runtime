// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



#pragma once

#include <type_traits>

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
} // end of namespace utility.

} // end of namespace jitstd.
