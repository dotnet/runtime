/**
 * \file
 * Support for interop with the Microsoft Error Reporting tool (header)
 *
 * Author:
 *   Alexander Kyte (alkyte@microsoft.com)
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#ifndef __MONO_UTILS_MERP__
#define __MONO_UTILS_MERP__

#include <config.h>
#include <glib.h>
#include <mono/metadata/threads-types.h>

#ifdef TARGET_OSX

/**
 * Unregister the MERP-based handler
 */
void mono_merp_disable (void);

/**
 *
 * Enable the MERP-based handler and set application-specific information
 *
 * See MERP documentation for information on the bundle ID, signature, and version fields
 */
void
mono_merp_enable (const char *appBundleID, const char *appSignature, const char *appVersion, const char *merpGUIPath, const char *appPath, const char *configDir);

/**
 * Whether the MERP-based handler is registered
 */
gboolean mono_merp_enabled (void);

/**
 * Create the MERP config file and invoke the merp agent
 *
 * \arg crashed_pid the PID of the thread that encountered the native fault
 * \arg thread_pointer the address of the stack pointer when the native fault occurred
 *
 * returns: Success of sending message
 *
 * This either returns after the MERP handler has successfully uploaded crashed_pid's
 * crash dump (leaving the caller to call exit), or terminates the runtime
 * when the registered telemetry application does not respond.
 */
gboolean
mono_merp_invoke (const intptr_t crashed_pid, const char *signal, const char *dump_file, MonoStackHash *hashes);

void
mono_merp_add_annotation (const char *key, const char *value);

#endif // TARGET_OSX

#endif // MONO_UTILS_MERP
