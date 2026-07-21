// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "unixasmmacros.inc"
#include "asmconstants.h"

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
    blr
    nop
LEAF_END JIT_PatchedCodeStart, _TEXT

// There is an even more optimized version of these helpers possible which takes
// advantage of knowledge of which way the ephemeral heap is growing to only do 1/2
// that check (this is more significant in the JIT_WriteBarrier case).
//
// Additionally we can look into providing helpers which will take the src/dest from
// specific registers (like x86) which _could_ (??) make for easier register allocation
// for the JIT64, however it might lead to having to have some nasty code that treats
// these guys really special like... :(.
//
// Version that does the move, checks whether or not it's in the GC and whether or not
// it needs to have it's card updated
//
// void JIT_CheckedWriteBarrier(Object** dst, Object* src)
// On entry:
//   r3  : the destination address (LHS of the assignment)
//   r4  : the object reference (RHS of the assignment)
//
// On exit:
//   r3  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//   r5  : trashed
//   r6  : trashed
//   r7  : trashed
//
LEAF_ENTRY JIT_CheckedWriteBarrier, _TEXT
	// Check if destination is in the GC heap
	// Load g_lowest_address
	addis %r5, %r2, g_lowest_address@toc@ha
	ld %r5, g_lowest_address@toc@l(%r5)
	cmpld %r3, %r5
	blt LOCAL_LABEL(NotInHeap)
	
	// Load g_highest_address
	addis %r5, %r2, g_highest_address@toc@ha
	ld %r5, g_highest_address@toc@l(%r5)
	cmpld %r3, %r5
	bge LOCAL_LABEL(NotInHeap)
	
	// In heap, call JIT_WriteBarrier
	b JIT_WriteBarrier

LOCAL_LABEL(NotInHeap):
	// Not in heap, just do the store
	std %r4, 0(%r3)
	addi %r3, %r3, 8
	blr
LEAF_END_MARKED JIT_CheckedWriteBarrier, _TEXT

// This is used by the mechanism to hold either the JIT_WriteBarrier_PreGrow
// or JIT_WriteBarrier_PostGrow code (depending on the state of the GC). It _WILL_
// change at runtime as the GC changes. Initially it should simply be a copy of the
// larger of the two functions (JIT_WriteBarrier_PostGrow) to ensure we have created
// enough space to copy that code in.
//
// void JIT_WriteBarrier(Object** dst, Object* src)
// On entry:
//   r3  : the destination address (LHS of the assignment)
//   r4  : the object reference (RHS of the assignment)
//
// On exit:
//   r3  : trashed (incremented by 8 to implement JIT_ByRefWriteBarrier contract)
//   r5  : trashed
//   r6  : trashed
//   r7  : trashed
//
LEAF_ENTRY JIT_WriteBarrier, _TEXT
	// Store the value
	std %r4, 0(%r3)

#ifdef WRITE_BARRIER_CHECK
	// Update GC Shadow Heap if enabled
	// Check if g_GCShadow is 0
	addis %r5, %r2, g_GCShadow@toc@ha
	ld %r5, g_GCShadow@toc@l(%r5)
	cmpdi %r5, 0
	beq LOCAL_LABEL(ShadowUpdateDisabled)
	
	// Compute shadow address: pShadow = g_GCShadow + (r3 - g_lowest_address)
	addis %r6, %r2, g_lowest_address@toc@ha
	ld %r6, g_lowest_address@toc@l(%r6)
	sub %r6, %r3, %r6
	add %r5, %r5, %r6
	
	// Check if pShadow >= g_GCShadowEnd
	addis %r6, %r2, g_GCShadowEnd@toc@ha
	ld %r6, g_GCShadowEnd@toc@l(%r6)
	cmpld %r5, %r6
	bge LOCAL_LABEL(ShadowUpdateDisabled)
	
	// *pShadow = r4
	std %r4, 0(%r5)
	
	// Memory barrier to ensure shadow write happens before GC heap read
	hwsync
	
	// Check if [r3] == r4
	ld %r7, 0(%r3)
	cmpld %r7, %r4
	beq LOCAL_LABEL(ShadowUpdateDisabled)
	
	// *pShadow = INVALIDGCVALUE (0xcccccccd)
	lis %r7, 0xcccc
	ori %r7, %r7, 0xcccd
	std %r7, 0(%r5)

LOCAL_LABEL(ShadowUpdateDisabled):
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
	// Update the write watch table if necessary
	addis %r5, %r2, g_sw_ww_enabled_for_gc_heap@toc@ha
	lbz %r5, g_sw_ww_enabled_for_gc_heap@toc@l(%r5)
	cmpdi %r5, 0
	beq LOCAL_LABEL(CheckCardTable)
	
	// Calculate write watch table entry
	srdi %r5, %r3, 12  // SoftwareWriteWatch::AddressToTableByteIndexShift
	addis %r6, %r2, g_sw_ww_table@toc@ha
	ld %r6, g_sw_ww_table@toc@l(%r6)
	add %r5, %r5, %r6
	
	// Check if already marked
	lbz %r6, 0(%r5)
	cmpdi %r6, 0
	bne LOCAL_LABEL(CheckCardTable)
	
	// Mark the entry
	li %r6, 0xFF
	stb %r6, 0(%r5)
#endif

LOCAL_LABEL(CheckCardTable):
	// Check if the reference is in the ephemeral generation
	addis %r5, %r2, g_ephemeral_low@toc@ha
	ld %r5, g_ephemeral_low@toc@l(%r5)
	cmpld %r4, %r5
	blt LOCAL_LABEL(Exit)
	
	addis %r5, %r2, g_ephemeral_high@toc@ha
	ld %r5, g_ephemeral_high@toc@l(%r5)
	cmpld %r4, %r5
	bge LOCAL_LABEL(Exit)
	
	// Update card table
	// Calculate card table entry: pCardByte = g_card_table + (r3 >> 11)
	srdi %r5, %r3, 11
	addis %r6, %r2, g_card_table@toc@ha
	ld %r6, g_card_table@toc@l(%r6)
	add %r5, %r5, %r6
	
	// Check if card is already dirty
	lbz %r6, 0(%r5)
	cmpdi %r6, 0xFF
	beq LOCAL_LABEL(Exit)
	
	// Mark card as dirty
	li %r6, 0xFF
	stb %r6, 0(%r5)

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
	// Update card bundle table
	// Calculate card bundle entry: pCardBundle = g_card_bundle_table + (r3 >> 21)
	srdi %r5, %r3, 21
	addis %r6, %r2, g_card_bundle_table@toc@ha
	ld %r6, g_card_bundle_table@toc@l(%r6)
	add %r5, %r5, %r6
	
	// Check if bundle is already dirty
	lbz %r6, 0(%r5)
	cmpdi %r6, 0xFF
	beq LOCAL_LABEL(Exit)
	
	// Mark bundle as dirty
	li %r6, 0xFF
	stb %r6, 0(%r5)
#endif

LOCAL_LABEL(Exit):
	// Increment destination pointer (for JIT_ByRefWriteBarrier contract)
	addi %r3, %r3, 8
	blr
LEAF_END_MARKED JIT_WriteBarrier, _TEXT

// Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
    blr
    nop
LEAF_END JIT_PatchedCodeLast, _TEXT

// JIT_ByRefWriteBarrier has weird semantics, see usage in StubLinkerX86.cpp
//
// Entry:
//   r3 - address of ref-field (assigned to)
//   r4 - address of the data  (source)
//
//   Note: RyuJIT assumes that all volatile registers can be trashed by
//   the CORINFO_HELP_ASSIGN_BYREF helper (i.e. JIT_ByRefWriteBarrier)
//   except r3 and r4. This helper uses and defines r3 and r4, so
//   they remain as live GC refs or byrefs, and are not killed.
//
//   r5 is trashed
//   r6 is trashed
//   r7 is trashed
//
// Exit:
//   r3, r4 are incremented by SIZEOF(LPVOID) = 8
LEAF_ENTRY JIT_ByRefWriteBarrier, _TEXT
	// Load the value from source address
	ld %r5, 0(%r4)
	
	// Check if destination is in the GC heap
	// Load g_lowest_address
	addis %r6, %r2, g_lowest_address@toc@ha
	ld %r6, g_lowest_address@toc@l(%r6)
	cmpld %r3, %r6
	blt LOCAL_LABEL(NotInHeap_ByRef)
	
	// Load g_highest_address
	addis %r6, %r2, g_highest_address@toc@ha
	ld %r6, g_highest_address@toc@l(%r6)
	cmpld %r3, %r6
	bge LOCAL_LABEL(NotInHeap_ByRef)
	
	// In heap - need to do full write barrier
	// Save r3 and r4 for later increment
	mr %r6, %r3
	mr %r7, %r4
	
	// Set up arguments: r3=dst, r4=value
	mr %r4, %r5
	
	// Call JIT_WriteBarrier (not CheckedWriteBarrier to avoid double check)
	// This will write the value and handle GC bookkeeping
	// It will also increment r3 by 8
	bl JIT_WriteBarrier
	
	// Increment source pointer
	addi %r4, %r7, 8
	
	// r3 was already incremented by JIT_WriteBarrier
	blr

LOCAL_LABEL(NotInHeap_ByRef):
	// Not in heap, just do the store and increment
	std %r5, 0(%r3)
	addi %r3, %r3, 8
	addi %r4, %r4, 8
	blr
LEAF_END_MARKED JIT_ByRefWriteBarrier, _TEXT

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
//.balign 16
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
	// Arguments are already in r3 (dst) and r4 (val)
	// Just jump to JIT_WriteBarrier
	b JIT_WriteBarrier
LEAF_END JIT_WriteBarrier_Callable, _TEXT
