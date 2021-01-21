/**
 * \file
 * Runtime support for managed Semaphore on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32semaphore.h"

#include <windows.h>
#include <winbase.h>
#include <mono/metadata/object-internals.h>
#include <mono/utils/w32subset.h>
#include "icall-decl.h"

void
mono_w32semaphore_init (void)
{
}
