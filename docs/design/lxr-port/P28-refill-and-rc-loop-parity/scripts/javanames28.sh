#!/bin/bash
# P28: find the Java LXR functions that the README names (mmtk-core 304ce69d43aa exported to /root/p28/pin).
cd /root/p28/pin
grep -rn --include=*.rs -E "PREFETCH_STEP|PREFETCH_HEADER|PREFETCH_RC|LOG_REF_COUNT_BITS|fn is_dead|fn rc_get_next_available_lines|fn get_clean_block|fn get_reusable_block|fn reset_block_cursors|fn prepare_gc|fn inc_buffer_size|BUFFER_SIZE: usize|fn initialize_new_clean_block|fn clear_prefetch|fn promote|fn process_inc|fn rc_stick|fn inc_young|fn inc_old|fn inc_inner|fn prefetch_" . | cut -c1-190 | head -70
