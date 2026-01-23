// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       mytime.h
/// \brief      Time handling functions
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////


/// \brief      Number of milliseconds to between LZMA_SYNC_FLUSHes
///
/// If 0, timed flushing is disabled. Otherwise if no more input is available
/// and not at the end of the file and at least opt_flush_timeout milliseconds
/// has elapsed since the start of compression or the previous flushing
/// (LZMA_SYNC_FLUSH or LZMA_FULL_FLUSH), set LZMA_SYNC_FLUSH to flush
/// the pending data.
extern uint64_t opt_flush_timeout;


#ifdef USE_SIGTSTP_HANDLER
/// \brief      Signal handler for SIGTSTP
extern void mytime_sigtstp_handler(int sig);
#endif


/// \brief      Store the time when (de)compression was started
///
/// The start time is also stored as the time of the first flush.
extern void mytime_set_start_time(void);


/// \brief      Get the number of milliseconds since the operation started
extern uint64_t mytime_get_elapsed(void);


/// \brief      Store the time of when compressor was flushed
extern void mytime_set_flush_time(void);


/// \brief      Get the number of milliseconds until the next flush
///
/// This returns -1 if no timed flushing is used.
///
/// The return value is intended for use with poll().
extern int mytime_get_flush_timeout(void);
