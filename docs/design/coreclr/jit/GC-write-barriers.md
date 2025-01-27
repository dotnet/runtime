# GC write barriers

The GC write barrier function (JIT_WriteBarrier) is generally the hottest function in CoreCLR and is written in assembly. The full pseudo code for the function is as follows:


````
JIT_WriteBarrier(Object **dst, Object *ref)
    Set *dst = ref

    // Shadow Heap update:
    ifdef TARGET_ARM64:
        if *wbs_GCShadow != 0:
            if g_GCShadow + (dst - g_lowest_address) < wbs_GCShadowEnd:
                *(g_GCShadow + (dst - g_lowest_address) = ref
                if *dst != ref:
                    *(g_GCShadow + (dst - g_lowest_address) = INVALIDGCVALUE

    // Write watch for GC Heap:
    ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP:
        if g_sw_ww_table != 0:
            if *(g_sw_ww_table + (dst>>11)) != 0:
                *(g_sw_ww_table + (dst>>11)) =  0xff


    // Return if the reference is not in the heap
    if ref < g_ephemeral_low || reg >= g_ephemeral_high:
        return

    // Region Checks
    if g_wbs_region_to_generation_table != 0:

        // Calculate region locations
        char reg_loc_dst = *((dst >> g_region_shr) + g_region_to_generation_table)
        char reg_loc_ref = *((ref >> g_region_shr) + g_region_to_generation_table)

        // Check whether the region we're storing into is gen 0 - nothing to do in this case
        if reg_loc_dst == 0:
            return

        // Check this is going from old to young
        if reg_loc_dst >= reg_loc_ref:
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

## WritebarrierManager

On AMD64, there several different implementations of the write barrier function. Each implementation assumes different state and so can skip certain checks. The actual write barrier that is called is a copy of one of these implementations. The WritebarrierManager keeps track of which implementation is currently being used. As internal state changes, the WritebarrierManager updates the copy to the correct implementation. In practice, most of the internal state is fixed on startup, with only changes to/from use of write watch barriers changing during runtime.
