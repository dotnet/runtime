#ifndef __DIAGNOSTICS_SERVER_H__
#define __DIAGNOSTICS_SERVER_H__

#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#include "ds-types.h"
#include "ds-rt.h"

/*
 * DiagnosticsServer.
 */

void
ds_server_disable (void);

// Initialize the event pipe (Creates the EventPipe IPC server).
bool
ds_server_init (void);

// Shutdown the event pipe.
bool
ds_server_shutdown (void);

// Pauses runtime startup after the Diagnostics Server has been started
// allowing a Diagnostics Monitor to attach perform tasks before
// Startup is completed
EP_NEVER_INLINE
void
ds_server_pause_for_diagnostics_monitor (void);

// Sets event to resume startup in runtime
// This is a no-op if not configured to pause or runtime has already resumed
void
ds_server_resume_runtime_startup (void);

bool
ds_server_is_paused_in_startup (void);

#endif /* ENABLE_PERFTRACING */
#endif /* __DIAGNOSTICS_SERVER_H__ */
