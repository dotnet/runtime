/**
 * \file
 * All binary protocol entries are defined here.  To keep compatibility with past binary
 * protocol files, new protocol entries need to be defined at the end of the file so that
 * the sequential numbering is preserved.  We also can't change the types or numbers of
 * arguments of existing entries.  Instead, a new entry has to be added and the old entry
 * retired - add a comment to make clear it's not used anymore, but it needs to stay in the
 * definition file, both to preserve the numbering as well as for the benefit of
 * `sgen-grep-binprot`, which will still want to read the old entries.
 *
 * It might become necessary to add a header protocol entry that includes version
 * information.  If/when we do that, we should also include word length and endianness.  As
 * of right now, binary protocol files don't identify themselves as 32 vs 64 bits or big- vs
 * little-endian.  At that point, `sgen-grep-binprot` should also be made able to read all
 * combinations of files, regardless of the host.
 */

BEGIN_PROTOCOL_ENTRY3 (binary_protocol_collection_requested, TYPE_INT, generation, TYPE_SIZE, requested_size, TYPE_BOOL, force)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY2 (binary_protocol_collection_begin, TYPE_INT, index, TYPE_INT, generation)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_FLUSH

BEGIN_PROTOCOL_ENTRY4 (binary_protocol_collection_end, TYPE_INT, index, TYPE_INT, generation, TYPE_LONGLONG, num_scanned_objects, TYPE_LONGLONG, num_unique_scanned_objects)
CUSTOM_PRINT (printf ("%d generation %d scanned %lld unique %lld %0.2f%%", entry->index, entry->generation, entry->num_scanned_objects, entry->num_unique_scanned_objects, entry->num_unique_scanned_objects ? (100.0 * (double) entry->num_scanned_objects / (double) entry->num_unique_scanned_objects) : 0.0))
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_FLUSH

BEGIN_PROTOCOL_ENTRY0 (binary_protocol_concurrent_start)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY0 (binary_protocol_concurrent_update)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY0 (binary_protocol_concurrent_finish)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY2 (binary_protocol_sweep_begin, TYPE_INT, generation, TYPE_BOOL, full_sweep)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY2 (binary_protocol_sweep_end, TYPE_INT, generation, TYPE_BOOL, full_sweep)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY3 (binary_protocol_world_stopping, TYPE_INT, generation, TYPE_LONGLONG, timestamp, TYPE_POINTER, thread)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (ptr == entry->thread ? 2 : BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY6 (binary_protocol_world_stopped, TYPE_INT, generation, TYPE_LONGLONG, timestamp, TYPE_LONGLONG, total_major_cards, TYPE_LONGLONG, marked_major_cards, TYPE_LONGLONG, total_los_cards, TYPE_LONGLONG, marked_los_cards)
CUSTOM_PRINT (printf ("generation %d timestamp %lld total %lld marked %lld %0.2f%%", entry->generation, entry->timestamp, entry->total_major_cards + entry->total_los_cards, entry->marked_major_cards + entry->marked_los_cards, 100.0 * (double) (entry->marked_major_cards + entry->marked_los_cards) / (double) (entry->total_major_cards + entry->total_los_cards)))
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY6 (binary_protocol_world_restarting, TYPE_INT, generation, TYPE_LONGLONG, timestamp, TYPE_LONGLONG, total_major_cards, TYPE_LONGLONG, marked_major_cards, TYPE_LONGLONG, total_los_cards, TYPE_LONGLONG, marked_los_cards)
CUSTOM_PRINT (printf ("generation %d timestamp %lld total %lld marked %lld %0.2f%%", entry->generation, entry->timestamp, entry->total_major_cards + entry->total_los_cards, entry->marked_major_cards + entry->marked_los_cards, 100.0 * (double) (entry->marked_major_cards + entry->marked_los_cards) / (double) (entry->total_major_cards + entry->total_los_cards)))
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY2 (binary_protocol_world_restarted, TYPE_INT, generation, TYPE_LONGLONG, timestamp)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_block_alloc, TYPE_POINTER, addr, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->addr, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_block_free, TYPE_POINTER, addr, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->addr, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY4 (binary_protocol_block_set_state, TYPE_POINTER, addr, TYPE_SIZE, size, TYPE_INT, old, TYPE_INT, new_)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->addr, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_mark_start, TYPE_INT, generation)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_mark_end, TYPE_INT, generation)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

/* FIXME: unify sweep and reclaim */
BEGIN_PROTOCOL_ENTRY1 (binary_protocol_reclaim_start, TYPE_INT, generation)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_reclaim_end, TYPE_INT, generation)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY_HEAVY4 (binary_protocol_alloc, TYPE_POINTER, obj, TYPE_POINTER, vtable, TYPE_SIZE, size, TYPE_POINTER, provenance)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->obj, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY4 (binary_protocol_copy, TYPE_POINTER, from, TYPE_POINTER, to, TYPE_POINTER, vtable, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->from, entry->size) ? 0 : matches_interval (ptr, entry->to, entry->size) ? 1 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_pin_stage, TYPE_POINTER, addr_ptr, TYPE_POINTER, addr)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->addr_ptr ? 0 : ptr == entry->addr ? 1 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY1 (binary_protocol_cement_stage, TYPE_POINTER, addr)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->addr ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_pin, TYPE_POINTER, obj, TYPE_POINTER, vtable, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->obj, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_mark, TYPE_POINTER, obj, TYPE_POINTER, vtable, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->obj, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_scan_begin, TYPE_POINTER, obj, TYPE_POINTER, vtable, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->obj, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_scan_vtype_begin, TYPE_POINTER, obj, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->obj, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_scan_process_reference, TYPE_POINTER, obj, TYPE_POINTER, ptr, TYPE_POINTER, value)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->obj ? 0 : ptr == entry->ptr ? 1 : ptr == entry->value ? 2 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY4 (binary_protocol_scan_stack, TYPE_POINTER, thread, TYPE_POINTER, stack_start, TYPE_POINTER, stack_end, TYPE_INT, skip_reason)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->thread ? 0 : (ptr >= entry->stack_start && ptr < entry->stack_end) ? 1 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_wbarrier, TYPE_POINTER, ptr, TYPE_POINTER, value, TYPE_POINTER, value_vtable)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->ptr ? 0 : ptr == entry->value ? 1 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->value_vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_global_remset, TYPE_POINTER, ptr, TYPE_POINTER, value, TYPE_POINTER, value_vtable)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->ptr ? 0 : ptr == entry->value ? 1 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->value_vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY5 (binary_protocol_ptr_update, TYPE_POINTER, ptr, TYPE_POINTER, old_value, TYPE_POINTER, new_value, TYPE_POINTER, vtable, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->ptr ? 0 : matches_interval (ptr, entry->old_value, entry->size) ? 1 : matches_interval (ptr, entry->new_value, entry->size) ? 2 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_cleanup, TYPE_POINTER, ptr, TYPE_POINTER, vtable, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->ptr, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_empty, TYPE_POINTER, start, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->start, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY2 (binary_protocol_thread_suspend, TYPE_POINTER, thread, TYPE_POINTER, stopped_ip)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_thread_restart, TYPE_POINTER, thread)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_thread_register, TYPE_POINTER, thread)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_thread_unregister, TYPE_POINTER, thread)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY6 (binary_protocol_missing_remset, TYPE_POINTER, obj, TYPE_POINTER, obj_vtable, TYPE_INT, offset, TYPE_POINTER, value, TYPE_POINTER, value_vtable, TYPE_BOOL, value_pinned)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->obj ? 0 : ptr == entry->value ? 3 : ptr == entry->obj + entry->offset ? BINARY_PROTOCOL_MATCH : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->obj_vtable || ptr == entry->value_vtable)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY_HEAVY4 (binary_protocol_alloc_pinned, TYPE_POINTER, obj, TYPE_POINTER, vtable, TYPE_SIZE, size, TYPE_POINTER, provenance)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->obj, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY4 (binary_protocol_alloc_degraded, TYPE_POINTER, obj, TYPE_POINTER, vtable, TYPE_SIZE, size, TYPE_POINTER, provenance)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->obj, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_card_scan, TYPE_POINTER, start, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->start, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY3 (binary_protocol_cement, TYPE_POINTER, obj, TYPE_POINTER, vtable, TYPE_SIZE, size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (matches_interval (ptr, entry->obj, entry->size) ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->vtable)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY0 (binary_protocol_cement_reset)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_dislink_add, TYPE_POINTER, link, TYPE_POINTER, obj, TYPE_BOOL, track)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->link ? 0 : ptr == entry->obj ? 1 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_dislink_update, TYPE_POINTER, link, TYPE_POINTER, obj, TYPE_BOOL, track)
CUSTOM_PRINT(entry->obj ? printf ("link 0x%"MWORD_FORMAT_SPEC_P" obj 0x%"MWORD_FORMAT_SPEC_P" track %d", entry->link, entry->obj, entry->track) : printf ("link 0x%"MWORD_FORMAT_SPEC_P" obj 0x%"MWORD_FORMAT_SPEC_P, entry->link, entry->obj))
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->link ? 0 : ptr == entry->obj ? 1 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_dislink_remove, TYPE_POINTER, link, TYPE_BOOL, track)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->link ? 0 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_domain_unload_begin, TYPE_POINTER, domain)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_domain_unload_end, TYPE_POINTER, domain)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_gray_enqueue, TYPE_POINTER, queue, TYPE_POINTER, cursor, TYPE_POINTER, value)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->cursor ? 1 : ptr == entry->value ? 2 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY3 (binary_protocol_gray_dequeue, TYPE_POINTER, queue, TYPE_POINTER, cursor, TYPE_POINTER, value)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->cursor ? 1 : ptr == entry->value ? 2 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY4 (binary_protocol_mod_union_remset, TYPE_POINTER, obj, TYPE_POINTER, ptr, TYPE_POINTER, value, TYPE_POINTER, value_vtable)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (FALSE)
MATCH_INDEX (ptr == entry->obj ? 0 : ptr == entry->ptr ? 1 : ptr == entry->value ? 2 : BINARY_PROTOCOL_NO_MATCH)
IS_VTABLE_MATCH (ptr == entry->value_vtable)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_major_card_table_scan_start, TYPE_LONGLONG, timestamp, TYPE_BOOL, mod_union)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_major_card_table_scan_end, TYPE_LONGLONG, timestamp, TYPE_BOOL, mod_union)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_los_card_table_scan_start, TYPE_LONGLONG, timestamp, TYPE_BOOL, mod_union)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_los_card_table_scan_end, TYPE_LONGLONG, timestamp, TYPE_BOOL, mod_union)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_finish_gray_stack_start, TYPE_LONGLONG, timestamp, TYPE_INT, generation)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY_HEAVY2 (binary_protocol_finish_gray_stack_end, TYPE_LONGLONG, timestamp, TYPE_INT, generation)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_HEAVY

BEGIN_PROTOCOL_ENTRY2 (binary_protocol_worker_finish, TYPE_LONGLONG, timestamp, TYPE_BOOL, forced)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_evacuating_blocks, TYPE_SIZE, block_size)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY1 (binary_protocol_concurrent_sweep_end, TYPE_LONGLONG, timestamp)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY4 (binary_protocol_header, TYPE_LONGLONG, check, TYPE_INT, version, TYPE_INT, ptr_size, TYPE_BOOL, little_endian)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY_FLUSH

BEGIN_PROTOCOL_ENTRY4 (binary_protocol_pin_stats, TYPE_INT, objects_pinned_in_nursery, TYPE_SIZE, bytes_pinned_in_nursery, TYPE_INT, objects_pinned_in_major, TYPE_SIZE, bytes_pinned_in_major)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY6 (binary_protocol_worker_finish_stats, TYPE_INT, worker_index, TYPE_INT, generation, TYPE_BOOL, forced, TYPE_LONGLONG, major_scan, TYPE_LONGLONG, los_scan, TYPE_LONGLONG, work_time)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

BEGIN_PROTOCOL_ENTRY3 (binary_protocol_collection_end_stats, TYPE_LONGLONG, major_scan, TYPE_LONGLONG, los_scan, TYPE_LONGLONG, finish_stack)
DEFAULT_PRINT ()
IS_ALWAYS_MATCH (TRUE)
MATCH_INDEX (BINARY_PROTOCOL_MATCH)
IS_VTABLE_MATCH (FALSE)
END_PROTOCOL_ENTRY

#undef BEGIN_PROTOCOL_ENTRY0
#undef BEGIN_PROTOCOL_ENTRY1
#undef BEGIN_PROTOCOL_ENTRY2
#undef BEGIN_PROTOCOL_ENTRY3
#undef BEGIN_PROTOCOL_ENTRY4
#undef BEGIN_PROTOCOL_ENTRY5
#undef BEGIN_PROTOCOL_ENTRY6
#undef BEGIN_PROTOCOL_ENTRY_HEAVY0
#undef BEGIN_PROTOCOL_ENTRY_HEAVY1
#undef BEGIN_PROTOCOL_ENTRY_HEAVY2
#undef BEGIN_PROTOCOL_ENTRY_HEAVY3
#undef BEGIN_PROTOCOL_ENTRY_HEAVY4
#undef BEGIN_PROTOCOL_ENTRY_HEAVY5
#undef BEGIN_PROTOCOL_ENTRY_HEAVY6

#undef DEFAULT_PRINT
#undef CUSTOM_PRINT

#undef IS_ALWAYS_MATCH
#undef MATCH_INDEX
#undef IS_VTABLE_MATCH

#undef END_PROTOCOL_ENTRY
#undef END_PROTOCOL_ENTRY_FLUSH
#undef END_PROTOCOL_ENTRY_HEAVY
