/*
 * collection.c:  Garbage collection for handles
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2004 Novell, Inc.
 */

#include <config.h>
#include <glib.h>
#include <pthread.h>
#include <signal.h>
#include <sys/types.h>
#include <unistd.h>

#include <mono/io-layer/wapi.h>
#include <mono/io-layer/collection.h>
#include <mono/io-layer/handles-private.h>

#undef DEBUG

volatile guint32 _wapi_collection_unsafe = 0;
volatile guint32 _wapi_collecting = 0;

static void collection_signal (int sig, siginfo_t *info, void *context)
{
	union sigval val;
	sigset_t sig_mask;
	
#ifdef DEBUG
	/* This is certainly not reentrant, but as it's just my debug
	 * code it doesn't really matter
	 */
	g_message ("Collection signal received.");
	g_message ("signo: %d", info->si_signo);
	g_message ("errno: %d", info->si_errno);
	g_message ("code : %d (SI_QUEUE = %d)", info->si_code, SI_QUEUE);
	g_message ("pid  : %d", info->si_pid);
	g_message ("uid  : %d", info->si_uid);
	g_message ("value: %d", info->si_value.sival_int);
	g_message ("int  : %d", info->si_int);
#endif

	switch(info->si_int) {
	case COLLECTION_READY:
		/* PREPARE, sent by master */
#ifdef DEBUG
		g_message ("%s: (%d) Been told to get ready for a collection",
			   __func__, getpid());
#endif
		
		/* Set the global flag so that other threads know not
		 * to enter unsafe code
		 */
		while ((InterlockedCompareExchange (&_wapi_collection_unsafe,
						    1, 0)) != 0) {
			/* We can't sleep, we're in a signal handler,
			 * so we'll just have to spin and hope the
			 * unsafe code finishes soon :-(
			 */
		}
		
#ifdef DEBUG
		g_message ("%s: (%d) Ack ready to collect", __func__,
			   getpid());
#endif
		
		/* Tell the master process that we're ready */
		val.sival_int = COLLECTION_ACK;
		sigqueue (info->si_pid, _WAPI_COLLECTION_SIGNAL, val);

		sigfillset (&sig_mask);
		sigdelset (&sig_mask, _WAPI_COLLECTION_PREPARE);
		sigdelset (&sig_mask, _WAPI_COLLECTION_SIGNAL);

		/* Wait for the next signal, which should be START.
		 *
		 * This should really be a timed wait (there's a
		 * window here for the master process to crash and
		 * leave this process stuck waiting), but
		 * sigtimedwait() is not safe to call in a signal
		 * handler.  The best way to deal with this (as
		 * recommended by Butenhof) would be to use a
		 * dedicated thread to run all handle GC activities,
		 * then I wouldn't need a signal handler at all - the
		 * combination of pthread_sigmask and
		 * sigwaitinfo()/sigtimedwait() would ensure that the
		 * thread received the signals
		 */
		sigsuspend (&sig_mask);

#ifdef DEBUG
		g_message ("%s: (%d) Ack suspend done", __func__, getpid());
#endif
		
		break;
		
	case COLLECTION_ACK:
		/* SIGNAL, sent to master */
		/* This is only here for completeness and
		 * documentation; the master has this signal masked
		 * globally, and the collection function calls
		 * sigtimedwait() to receive it.
		 */
		g_error ("%s: _WAPI_COLLECTION_SIGNAL wasn't properly masked!",
			 __func__);
		break;
		
	case COLLECTION_START:
		/* SIGNAL, sent by master */
#ifdef DEBUG
		g_message ("%s: (%d) Been told to start a collection",
			   __func__, getpid());
#endif

		/* Add all our refs */
		_wapi_handle_update_refs ();
		
		/* Tell the master process that we've finished */
		InterlockedDecrement (&_wapi_shared_layout->collection_signal_done);

#ifdef DEBUG
		g_message ("%s: (%d) Refs update done", __func__, getpid());
#endif

		sigfillset (&sig_mask);
		sigdelset (&sig_mask, _WAPI_COLLECTION_PREPARE);
		sigdelset (&sig_mask, _WAPI_COLLECTION_SIGNAL);

		/* Wait for the next signal, which should be DONE */
		sigsuspend (&sig_mask);

#ifdef DEBUG
		g_message ("%s: (%d) Start suspend done", __func__, getpid());
#endif
		break;
		
	case COLLECTION_DONE:
		/* SIGNAL, sent by master */

#ifdef DEBUG
		g_message ("%s: (%d) Collection is complete",
			   __func__, getpid());
#endif

		/* Clear the global flag so that other threads know
		 * it's now OK to enter unsafe code
		 */
		InterlockedDecrement (&_wapi_collection_unsafe);
		
		break;
		
	case COLLECTION_DUMP:
		/* Debugging aid */
#ifdef DEBUG
		_wapi_handle_dump ();
#endif
		break;

	case COLLECTION_FORCE:
		/* Debugging aid */
#ifdef DEBUG
		_wapi_handle_collect ();
#endif
		break;
	}
}

void _wapi_collection_init (void)
{
	struct sigaction act;
	sigset_t procmask;
	
	/* Globally block the collection signal, so that it can be
	 * waited for with sigtimedwait() in a particular thread
	 */
	sigemptyset (&procmask);
	sigaddset (&procmask, _WAPI_COLLECTION_SIGNAL);
	sigprocmask (SIG_BLOCK, &procmask, NULL);

	/* And in all threads that we create.  ASSUMPTION: this is the
	 * main thread, and all new threads will be ancestors of this
	 * one.
	 */
	pthread_sigmask (SIG_BLOCK, &procmask, NULL);
	
	act.sa_sigaction = collection_signal;
	sigemptyset (&act.sa_mask);

	act.sa_flags = SA_SIGINFO | SA_NODEFER;
	
	sigaction (_WAPI_COLLECTION_PREPARE, &act, NULL);
	sigaction (_WAPI_COLLECTION_SIGNAL, &act, NULL);
}

struct _wapi_collection_pids
{
	pid_t pid;
	gboolean is_mono;
};

static void wait_for_signals (struct _wapi_collection_pids *pids,
			      guint32 batch_count, guint32 pid_high)
{
	guint32 now = (guint32)(time(NULL) & 0xFFFFFFFF);
	sigset_t sig_mask;
	siginfo_t info;
	struct timespec timeout;
	guint32 ack_count = 0;
	int i;
	
#ifdef DEBUG
	g_message ("%s: Checking pids to index %d", __func__, pid_high - 1);
#endif

	sigemptyset (&sig_mask);
	sigaddset (&sig_mask, _WAPI_COLLECTION_SIGNAL);
	
	/* This is a relative timeout, not absolute like some
	 * threading functions.  sigtimedwait() doesn't modify the
	 * timeout parameter like select() does, either.
	 */
	timeout.tv_sec = 2;
	timeout.tv_nsec = 0;
	
	do {
		int ret = sigtimedwait (&sig_mask, &info, &timeout);
		if (ret == -1 ) {
#ifdef DEBUG
			g_message ("%s: (%d) sigtimedwait error: %s", __func__,
				   getpid (), strerror(errno));
#endif
		} else if (info.si_int == COLLECTION_ACK) {
#ifdef DEBUG
			g_message ("%s: (%d) sigtimedwait ACK signal: %d (%d) from %d", __func__, getpid (), ret, _WAPI_COLLECTION_SIGNAL, info.si_pid);
#endif
			for (i = 0; i < pid_high; i++) {
				if (pids[i].pid == info.si_pid) {
					pids[i].is_mono = TRUE;
					break;
				}
			}

			ack_count++;
		} else {
			/* Bogus response, ignore this process */
#ifdef DEBUG
			g_message ("%s: (%d) sigtimedwait bogus (%d) signal: %d (%d) from %d", __func__, getpid (), info.si_int, ret, _WAPI_COLLECTION_SIGNAL, info.si_pid);
#endif
			ack_count++;
		}
#ifdef DEBUG
		g_message ("%s: (%d) batch_count: %d ack_count: %d", __func__,
			   getpid (), batch_count, ack_count);
#endif
	} while (batch_count > ack_count &&
		 now + 2 > (time(NULL) & 0xFFFFFFFF));
	
#ifdef DEBUG
	if (ack_count == batch_count) {
		g_message ("%s: (%d) All processes ACKed in this batch",
			   __func__, getpid ());
	} else {
		g_message ("%s: (%d) %d processes outstanding at ACK in this batch!!", __func__, getpid (), batch_count - ack_count);
	}
#endif
}

static struct _wapi_collection_pids *find_pids (guint32 *proc_count)
{
	int i;
	struct _wapi_collection_pids *pids;
	guint32 pid_count, batch_count;
	
	pid_t self = getpid();
	union sigval val;
	
	/* Find the upper bound of the process count (some of these
	 * might be dead or non-mono if the pid has been reused)
	 */
	*proc_count = 0;
	for (i = 0; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
		struct _WapiHandleShared *shared;
		
		shared = &_wapi_shared_layout->handles[i];
		if (shared->type == WAPI_HANDLE_PROCESS) {
			(*proc_count)++;
		}
	}
	
	pids = g_new0 (struct _wapi_collection_pids, *proc_count);

#ifdef DEBUG
	g_message ("%s: Finding pids in %d chunks", __func__,
		   (*proc_count / _POSIX_SIGQUEUE_MAX) + 1);
#endif
	
	/* Scan the list for processes to signal.  Do this in chunks
	 * of _POSIX_SIGQUEUE_MAX, because the reply signals have to
	 * be queued up by this process.
	 */
	pid_count = 0;
	batch_count = 0;
	for (i = 0; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
		struct _WapiHandleShared *shared;
		
		shared = &_wapi_shared_layout->handles[i];
		if (shared->type == WAPI_HANDLE_PROCESS) {
			struct _WapiHandle_process *proc = &shared->u.process;
				
#ifdef DEBUG
			g_message ("%s: (%d) Found process %d", __func__, self,
				   proc->id);
#endif

			if (proc->id == self) {
				/* Don't signal ourselves! */
				continue;
			}
			
			if (kill (proc->id, 0) == -1) {
				/* This process no longer exists, or
				 * we can't signal it anyway, so don't
				 * bother.
				 */
				continue;
			}
			
			pids[pid_count].pid = proc->id;
				
			val.sival_int = COLLECTION_READY;
			sigqueue (proc->id, _WAPI_COLLECTION_PREPARE, val);

#ifdef DEBUG
			g_message ("%s: (%d) Signalled %d", __func__, self,
				   proc->id);
#endif

			batch_count++;
				
			if (++pid_count % _POSIX_SIGQUEUE_MAX == 0) {
				/* Wait for this batch to check in */
				wait_for_signals (pids, batch_count,
						  pid_count);
				batch_count = 0;
			}
		}
	}

	if (batch_count > 0) {
		wait_for_signals (pids, batch_count, pid_count);
	}

	return(pids);
}

void _wapi_handle_collect (void)
{
	guint32 now;
	guint32 count = _wapi_shared_layout->collection_count;
	union sigval val;
	int thr_ret, i;
	guint32 proc_count;
	struct _wapi_collection_pids *pids;
	
#ifdef DEBUG
	g_message ("%s: (%d) Starting a collection", __func__, getpid ());
#endif

	do {
		/* Become the collection master */
		now = (guint32)(time(NULL) & 0xFFFFFFFF);
		thr_ret = _wapi_timestamp_exclusion (&_wapi_shared_layout->master, now);
		if (thr_ret == EBUSY) {
			/* we will eventually usurp the master role,
			 * if the previous one is taking too long.
			 */
			_wapi_handle_spin (100);
		}
	} while (thr_ret == EBUSY);
	g_assert(thr_ret == 0);
	
#ifdef DEBUG
	g_message ("%s: (%d) Master set", __func__, getpid ());
#endif

	_wapi_shared_layout->collection_signal_done = 0;

	pids = find_pids(&proc_count);

	/* From here on we know exactly which other processes are
	 * really mono, because they've told us.
	 */

	/* Zero every shared refcount */
	for (i = 0; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
		struct _WapiHandleSharedMetadata *meta;
		
		meta = &_wapi_shared_layout->metadata[i];
		meta->ref = 0;
	}

	/* And every file share refcount */
	for (i = 0; i < _wapi_fileshare_layout->hwm; i++) {
		struct _WapiFileShare *share;
		
		share = &_wapi_fileshare_layout->share_info[i];
		share->handle_refs = 0;
	}

#ifdef DEBUG
	g_message ("%s: (%d) Zerod all refs", __func__, getpid ());
#endif
	
	/* Scan the list for processes to signal to start updating
	 */
	val.sival_int = COLLECTION_START;
	for (i = 0; i < proc_count; i++) {
		if (pids[i].is_mono) {
			sigqueue (pids[i].pid, _WAPI_COLLECTION_SIGNAL, val);
			InterlockedIncrement (&_wapi_shared_layout->collection_signal_done);

#ifdef DEBUG
			g_message ("%s: (%d) Signalled %d", __func__,
				   getpid (), pids[i].pid);
#endif
		}
	}

	/* Add our own refs */
	_wapi_handle_update_refs ();
	
	/* And wait for the other processes to signal that they've
	 * updated theirs
	 */
	do {
		/* Sleep for a bit, and then break out.  2 seconds
		 * ought to be major overkill
		 */
		_wapi_handle_spin (100);
	} while (_wapi_shared_layout->collection_signal_done > 0 &&
		 now + 2 > (time(NULL) & 0xFFFFFFFF));

#ifdef DEBUG
	if (_wapi_shared_layout->collection_signal_done == 0) {
		g_message ("%s: (%d) All processes reported in", __func__,
			   getpid ());
	} else {
		g_message ("%s: (%d) %d processes outstanding!!", __func__,
			   getpid (),
			   _wapi_shared_layout->collection_signal_done);
	}
#endif
	
	if (count == _wapi_shared_layout->collection_count) {
		for (i = 0; i < _WAPI_HANDLE_INITIAL_COUNT; i++) {
			struct _WapiHandleShared *shared;
			struct _WapiHandleSharedMetadata *meta;

			meta = &_wapi_shared_layout->metadata[i];
			if (meta->ref == 0 && meta->offset != 0) {
#ifdef DEBUG
				g_message ("%s: (%d) Deleting metadata slot 0x%x handle 0x%x", __func__, getpid (), i, meta->offset);
#endif
				memset (&_wapi_shared_layout->handles[meta->offset], '\0', sizeof(struct _WapiHandleShared));
				memset (&_wapi_shared_layout->metadata[i], '\0', sizeof(struct _WapiHandleSharedMetadata));
			}

			/* Need to blank any handles data that is no
			 * longer pointed to by a metadata entry too
			 */
			shared = &_wapi_shared_layout->handles[i];
			if (shared->stale == TRUE) {
#ifdef DEBUG
				g_message ("%s: (%d) Deleting stale handle 0x%x", __func__, getpid (), i);
#endif
				memset (&_wapi_shared_layout->handles[i], '\0',
					sizeof(struct _WapiHandleShared));
			}
		}

		for (i = 0; i < _wapi_fileshare_layout->hwm; i++) {
			struct _WapiFileShare *file_share = &_wapi_fileshare_layout->share_info[i];
			
			if (file_share->handle_refs == 0) {
				memset (file_share, '\0',
					sizeof(struct _WapiFileShare));
			}
		}

		InterlockedIncrement (&_wapi_shared_layout->collection_count);
	}
	
	/* Scan the list for processes to signal that collection is
	 * done.  Signal everyone, not just mono processes (with the
	 * _PREPARE signal) just in case a mono process failed to
	 * check in in time, and is now stuck suspended waiting for
	 * the _START signal. (If this does happen it probably wont
	 * survive much longer - chances are its handles were blown
	 * away cos it didn't get told to add the ref counts...)
	 */
	val.sival_int = COLLECTION_DONE;
	for (i = 0; i < proc_count; i++) {
		/* Some entries might have pid == 0, as the array
		 * length is the total number of entries in the shared
		 * memory, not just the valid processes.
		 */
		if (pids[i].pid) {
			sigqueue (pids[i].pid, _WAPI_COLLECTION_PREPARE, val);

#ifdef DEBUG
			g_message ("%s: (%d) Signalled %d", __func__,
				   getpid (), pids[i].pid);
#endif
		}
	}

	g_free (pids);
			
	thr_ret = _wapi_timestamp_release (&_wapi_shared_layout->master, now);
	g_assert (thr_ret == 0);

#ifdef DEBUG
	g_message ("%s: (%d) Collection done", __func__, getpid ());
#endif
}
