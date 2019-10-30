// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CeeSectionString.h
// 
// ===========================================================================

#ifndef CeeSectionString_H
#define CeeSectionString_H

#include <ole2.h>
#include "ceegen.h"

// This class is responsible for managing all the strings that have
// been emitted for the PE file.

// This class manages the strings that are added to the .rdata section.
// It keeps track of each string that has been added using a hashtable.
// The hash table is effectively 2-dimensional. There is a large "virtual
// hash space" that is used to get a wide hash code distribution. The
// virtual hash space is mapped into a real hash table where each n
// hash values in the virtual space fall into a given hash bucket for
// real hash table size n. Within the bucket, elements are stored in a linked
// list in-order. When an virtual hash entry corresponds to a given bucket,
// that bucket is searched for the matching hash id. If not found, it is
// inserted, otherwise, the value is returned. The idea is that for smaller
// apps, there won't be a large number of strings, so that collisions are
// minimal and the length of each bucket's chain is small. For larger
// numbers of strings, having a large hash space also reduces numbers
// of collisions, avoiding string compares unless the hash codes match.

struct StringTableEntry;

class CeeSectionString : public CeeSection {
	enum { MaxRealEntries = 100, MaxVirtualEntries = 10000 };
	StringTableEntry *stringTable[MaxRealEntries];

	StringTableEntry *createEntry(__in_z LPWSTR target, ULONG hashId);
	StringTableEntry *findStringInsert(
				StringTableEntry *&entry, __in_z LPWSTR targetValue, ULONG hashId);
	void deleteEntries(StringTableEntry *e);
#ifdef RDATA_STATS
	int dumpEntries(StringTableEntry *e);
	void dumpTable();
#endif

  public:
	~CeeSectionString();
	CeeSectionString(CCeeGen &ceeFile, CeeSectionImpl &impl);
	virtual HRESULT getEmittedStringRef(__in_z LPWSTR targetValue, StringRef *ref);
};

#endif

