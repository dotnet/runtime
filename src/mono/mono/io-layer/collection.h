/*
 * collection.h:  Garbage collection for handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2004 Novell, Inc.
 */

#ifndef _WAPI_COLLECTION_H_
#define _WAPI_COLLECTION_H_

#include <glib.h>
#include <signal.h>

/* This needs to be a signal that is ignored by default, as we might
 * be sending it accidentally to non-mono processes
 */
#define _WAPI_COLLECTION_PREPARE SIGWINCH

/* This needs to be a real-time signal, because several will be queued
 * up
 */
#define _WAPI_COLLECTION_SIGNAL SIGRTMIN+4 /* LinuxThreads uses the first 3 */

/* The collection protocol:
 *
 * 1) A process decides that a handle collection is necessary, and
 * atomically assigns the master slot in the shared memory.  If it
 * can't get the master slot, it saves the current collection count
 * and retries until it can, or until n seconds have elapsed (to cope
 * with a master that crashed.)  If the collection count hasn't
 * increased, it goes ahead.  Otherwise it returns, as someone else
 * has done a collection.
 *
 * 2) It sends every process with a Process handle entry a
 * WAPI_COLLECTION_PREPARE signal with COLLECTION_START in the
 * siginfo_t intval.  Other processes that are in unsafe code will
 * have the signal delayed until it is safe to respond.  Processes
 * respond by sending back a WAPI_COLLECTION_SIGNAL signal with
 * COLLECTION_ACK in the siginfo_t intval.
 *
 * 3) The master waits for the ACK signals, or times out after n
 * seconds.  It then zeros every shared handle refcount and every
 * fileshare refcount and signals every process that responded (now
 * known to be capable of receiving the signal) with COLLECTION_START
 * in the siginfo_t intval.
 *
 * 4) All processes (including the master) atomically add their own
 * ref count to each referenced shared handle
 *
 * 5) All other processes inform the master that they have finished by
 * decrementing the "signal_done" field in the shared memory
 * structure.  The master will time out after n seconds, to cope with
 * other processes quitting mid-process.  Other processes that are in
 * unsafe code will have the START signal delayed until it is safe to
 * respond, so unsafe areas should be kept small.
 *
 * 6) If the collection count hasn't increased (someone else might
 * have decided the master took too long and assumed control), the
 * master deletes all shared slots with zero refs, and atomically
 * increments the collection count slot.  The master then signals
 * every process with COLLECTION_DONE.
 */

typedef enum {
	COLLECTION_READY = 1,
	COLLECTION_ACK = 2,
	COLLECTION_START = 3,
	COLLECTION_DONE = 4,
	COLLECTION_DUMP = 5,
	COLLECTION_FORCE = 6
} _wapi_collection_command;

extern volatile guint32 _wapi_collection_unsafe;

/* Prevent a handle collection from happening in unsafe code **in this
 * thread**.  Another thread will get this signal, if it happens.  The
 * collection code will wait for the unsafe flag to be 0 before
 * starting a collection.
 *
 * Using just one flag for "unsafe code" and "collecting" means that
 * unsafe sections are serialised, and therefore a bottleneck, but it
 * avoids the race condition between checking two flags.
 */

#define _WAPI_HANDLE_BLOCK_IF_COLLECTING			\
	if (_wapi_collection_unsafe) {				\
		do {						\
			_wapi_handle_spin (100);		\
		} while (_wapi_collection_unsafe);		\
	}
	
#define _WAPI_HANDLE_COLLECTION_UNSAFE				\
	{							\
		sigset_t _wapi_new_procmask;			\
		sigset_t _wapi_old_procmask;			\
		guint32 _wapi_save_start;			\
								\
		_WAPI_HANDLE_BLOCK_IF_COLLECTING;		\
								\
		_wapi_save_start = (guint32)(time(NULL) & 0xFFFFFFFF);	\
								\
		sigemptyset (&_wapi_new_procmask);		\
		sigaddset (&_wapi_new_procmask, _WAPI_COLLECTION_PREPARE); \
		pthread_sigmask (SIG_BLOCK, &_wapi_new_procmask,	\
				 &_wapi_old_procmask);			\
								\
		while ((InterlockedCompareExchange (&_wapi_collection_unsafe, 1, 0)) != 0) { \
			_wapi_handle_spin (10);			\
		}
		
		
#define _WAPI_HANDLE_COLLECTION_SAFE				\
		InterlockedDecrement (&_wapi_collection_unsafe);	\
		pthread_sigmask (SIG_SETMASK, &_wapi_old_procmask, NULL); \
								\
		if (_wapi_save_start + 2 < (time(NULL) & 0xFFFFFFFF)) {	\
			g_warning ("%s: Took more than 2 seconds in unsafe code, shared handle collection might break", __func__); \
		}							\
	}
	

extern void _wapi_collection_init (void);
extern void _wapi_handle_collect (void);

#endif /* _WAPI_COLLECTION_H_ */
