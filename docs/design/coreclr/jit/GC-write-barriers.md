# GC write barriers

The GC write barrier function (JIT_WriteBarrier) is generally the hottest function in CoreCLR and is written in assembly. The full pseudo code for the function is as follows:


````
JIT_WriteBarrier(Object **dst, Object *ref)
    Set *dst = ref

    // Shadow Heap update
    ifdef WRITE_BARRIER_CHECK: // Only set in DEBUG mode
        if g_GCShadow != 0:
            long *shadow_dst = g_GCShadow + (dst - g_lowest_address)
            // Check shadow heap location is within shadow heap
            if shadow_dst < g_GCShadowEnd:
                *shadow_dst = ref
                atomic: wait for stores to complete
                if *dst != ref:
                    *shadow_dst = INVALIDGCVALUE

    // Update the write watch table, if it's in use
    ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP:
        if g_sw_ww_table != 0:
            char *ww_table_dst = g_sw_ww_table + (dst>>11)
            if *ww_table_dst != 0:
                *ww_table_dst =  0xff

    // Return if the reference is not in ephemeral generations
    if ref < g_ephemeral_low || ref >= g_ephemeral_high:
        return

    // Region Checks
    if g_region_to_generation_table != 0:

        // Calculate region generations
        char reg_loc_dst = *((dst >> g_region_shr) + g_region_to_generation_table)
        char reg_loc_ref = *((ref >> g_region_shr) + g_region_to_generation_table)

        // Return if the region we're storing into is Gen 0
        if reg_loc_dst == 0:
            return

        // Return if the new reference is not from old to young
        if reg_loc_ref >= reg_loc_dst:
            return

        // Bitwise write barriers only
        if g_region_use_bitwise_write_barrier:

            char *card_table_dst = (dst >> 11) + g_card_table
            char dst_bit = 1 << (dst >> 8 && 7)

            // Check if we need to update the card table
            if *card_table_dst & dst_bit == 0:
                return
            
            // Atomically update the card table
            lock: *card_table_dst |= dst_bit

            goto CardBundle

    // Check if we need to update the card table
    char *card_table_dst = (dst >> 11) + g_card_table
    if *card_table_dst == 0xff:
        return

    // Update the card table
    *card_table_dst = 0xff

CardBundle:

    // Mark the card bundle table as dirty
    Ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES:
        char card_bundle_dst = (dst >> 21) + g_card_bundle_table
        if *card_bundle_dst != 0xff:
            *card_bundle_dst = 0xff

````

The Checked Write Barrier has additional checks:

````
JIT_CheckedWriteBarrier(Object **dst, Object *ref)

    // Return if the destination is not on the heap
    if ref < g_lowest_address || ref >= g_highest_address:
        return

    return JIT_WriteBarrier(dst, ref)
````

## WriteBarrierManager

On AMD64 and Arm64, there several different implementations of the write barrier function. Each version is a subset of the `JIT_WriteBarrier` above, assuming different state, meaning most `if` checks can be skipped. The actual write barrier that is called is a copy of one of these implementations.

The WriteBarrierManager keeps track of which implementation is currently being used. As internal state changes, the WriteBarrierManager updates the copy to the correct implementation. In practice, most of the internal state is fixed on startup, with only changes to/from use of write watch barriers changing during runtime.

`WRITE_BARRIER_CHECK` is only set in `DEBUG` mode. On Arm64 `WRITE_BARRIER_CHECK` checks exist at the top of each version of the function when `DEBUG` mode is enabled. On `Amd64` these checks do not exist. Instead, a special `JIT_WriteBarrier_Debug` version of the function exists, which contains most of the functionality of `JIT_WriteBarrier` pseudo code and is used exclusively when `DEBUG` mode is enabled.

On Arm64, `g_region_use_bitwise_write_barrier` is only set if LSE atomics are present on the hardware, as only LSE provides a single instruction to atomically update a byte via a bitwise OR.

