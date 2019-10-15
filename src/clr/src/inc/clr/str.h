// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// This header provides general standard string services.
//

#ifndef _clr_str_h_
#define _clr_str_h_

namespace clr
{
    namespace str
    {
        //-----------------------------------------------------------------------------------------
        // Returns true if the provided string is a null pointer or the empty string.
        static inline bool
        IsNullOrEmpty(LPCWSTR wzStr)
        {
            return wzStr == nullptr || *wzStr == W('\0');
        }
    }
}

#endif // _clr_str_h_

