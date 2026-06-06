// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/procobj.hpp

Abstract:
    Header file for process structures



--*/

#ifndef _PAL_PROCOBJ_HPP_
#define _PAL_PROCOBJ_HPP_

#include "corunix.hpp"

namespace CorUnix
{
    PAL_ERROR
    CreateInitialProcessAndThreadObjects(
        CPalThread *pThread
        );
}

#endif // _PAL_PROCOBJ_HPP_

