/**
 * \file
 * Support for verbose unmanaged crash dumps
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#include <config.h>
#include <glib.h>

#include <mono/mini/debugger-state-machine.h>
#include <mono/metadata/object-internals.h>
#include <mono/mini/mini-runtime.h>

