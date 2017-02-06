// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    exception/signal.hpp

Abstract:
    Private signal handling utilities for SEH



--*/

#ifndef _PAL_SIGNAL_HPP_
#define _PAL_SIGNAL_HPP_

#if !HAVE_MACH_EXCEPTIONS

/*++
Function :
    SEHInitializeSignals

    Set-up signal handlers to catch signals and translate them to exceptions

Parameters :
    flags: PAL initialization flags

Return :
    TRUE in case of a success, FALSE otherwise
--*/
BOOL SEHInitializeSignals(DWORD flags);

/*++
Function :
    SEHCleanupSignals

    Restore default signal handlers

    (no parameters, no return value)
--*/
void SEHCleanupSignals();

#endif // !HAVE_MACH_EXCEPTIONS

#endif /* _PAL_SIGNAL_HPP_ */

