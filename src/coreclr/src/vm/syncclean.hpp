// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#ifndef _SYNCCLEAN_HPP_
#define _SYNCCLEAN_HPP_

// We keep a list of memory blocks to be freed at the end of GC, but before we resume EE.
// To make this work, we need to make sure that these data are accessed in cooperative GC
// mode.

class Bucket;
struct EEHashEntry;
class Crst;
class CrstStatic;

class SyncClean {
public:
    static void Terminate ();

    static void AddHashMap (Bucket *bucket);
    static void AddEEHashTable (EEHashEntry** entry);
    static void CleanUp ();

private:
    static VolatilePtr<Bucket> m_HashMap;               // Cleanup list for HashMap
    static VolatilePtr<EEHashEntry *> m_EEHashTable;    // Cleanup list for EEHashTable
};
#endif
