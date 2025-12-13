// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __ASYNCSAFETHREADMAP_H__
#define __ASYNCSAFETHREADMAP_H__

#if defined(TARGET_UNIX) && !defined(TARGET_WASM)

//  Insert a thread into the async-safe map.
//  * osThread - The OS thread ID to insert.
//  * pThread - A pointer to the thread object to associate with the OS thread ID.
//  * return true if the insertion was successful, false otherwise (OOM).
bool InsertThreadIntoAsyncSafeMap(size_t osThread, void* pThread);

// Remove a thread from the async-safe map.
// * osThread - The OS thread ID to remove.
// * pThread - A pointer to the thread object associated with the OS thread ID.
void RemoveThreadFromAsyncSafeMap(size_t osThread, void* pThread);

// Find a thread in the async-safe map.
// * osThread - The OS thread ID to search for.
// * return - A pointer to the thread object associated with the OS thread ID, or NULL if not found.
void* FindThreadInAsyncSafeMap(size_t osThread);

#endif // TARGET_UNIX && !TARGET_WASM

#endif // __ASYNCSAFETHREADMAP_H__
