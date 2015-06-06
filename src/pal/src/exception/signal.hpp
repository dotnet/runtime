//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
    None

Return :
    TRUE in case of a success, FALSE otherwise
--*/
BOOL SEHInitializeSignals();

/*++
Function :
    SEHCleanupSignals

    Restore default signal handlers

    (no parameters, no return value)
--*/
void SEHCleanupSignals();

#if (__GNUC__ > 3 ||                                            \
     (__GNUC__ == 3 && __GNUC_MINOR__ > 2))
// For gcc > 3.2, sjlj exceptions semantics are no longer available
// Therefore we need to hijack out of signal handlers before second pass
#define HIJACK_ON_SIGNAL 1
#endif

#endif // !HAVE_MACH_EXCEPTIONS

#endif /* _PAL_SIGNAL_HPP_ */

