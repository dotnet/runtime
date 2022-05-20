// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <elf.h>
#include <fcntl.h>
#include <string.h>
#include <unistd.h>
#include <stddef.h>

#include <sys/mman.h>

#include "_OOP_internal.h"

int
_OOP_find_proc_info (
    unw_word_t start_ip,
    unw_word_t end_ip,
    unw_word_t eh_frame_table,
    unw_word_t eh_frame_table_len,
    unw_word_t exidx_frame_table,
    unw_word_t exidx_frame_table_len,
    unw_addr_space_t as,
    unw_word_t ip,
    unw_proc_info_t *pi,
    int need_unwind_info,
    void *arg)
{
    int ret = 0;

    unw_dyn_info_t di;
    memset(&di, 0, sizeof(di));

    di.start_ip = start_ip;
    di.end_ip = end_ip;
    di.gp = pi->gp;
    di.u.rti.name_ptr = 0;

#if UNW_TARGET_ARM
    if (exidx_frame_table != 0) {
        di.format = UNW_INFO_FORMAT_ARM_EXIDX;
        di.u.rti.table_data = exidx_frame_table;
        di.u.rti.table_len = exidx_frame_table_len;
        di.u.rti.segbase = 0;
    }
    else
#endif
    if (eh_frame_table != 0) {
        unw_accessors_t *a = unw_get_accessors_int (as);

        unw_word_t hdr;
        if ((*a->access_mem)(as, eh_frame_table, &hdr, 0, arg) < 0) {
            return -UNW_EINVAL;
        }
        struct dwarf_eh_frame_hdr* exhdr = (struct dwarf_eh_frame_hdr*)&hdr;

        if (exhdr->version != DW_EH_VERSION) {
            Debug (1, "Unexpected version %d\n", exhdr->version);
            return -UNW_EBADVERSION;
        }
        unw_word_t addr = eh_frame_table + offsetof(struct dwarf_eh_frame_hdr, eh_frame);
        unw_word_t eh_frame_start;
        unw_word_t fde_count;

        /* read eh_frame_ptr */
        if ((ret = dwarf_read_encoded_pointer(as, a, &addr, exhdr->eh_frame_ptr_enc, pi, &eh_frame_start, arg)) < 0) {
            return ret;
        }

        /* read fde_count */
        if ((ret = dwarf_read_encoded_pointer(as, a, &addr, exhdr->fde_count_enc, pi, &fde_count, arg)) < 0) {
            return ret;
        }

        // If there are no frame table entries
        if (fde_count == 0) {
            Debug(1, "No frame table entries\n");
            return -UNW_ENOINFO;
        }

        if (exhdr->table_enc != (DW_EH_PE_datarel | DW_EH_PE_sdata4)) {
            Debug (1, "Table encoding not supported %x\n", exhdr->table_enc);
            return -UNW_EINVAL;
        }

        di.format = UNW_INFO_FORMAT_REMOTE_TABLE;
        di.u.rti.table_data = addr;
        di.u.rti.table_len = (fde_count * 8) / sizeof (unw_word_t);
        di.u.rti.segbase = eh_frame_table;
    }
    else {
        Debug (1, "No frame table data\n");
        return -UNW_ENOINFO;
    }

    ret = tdep_search_unwind_table(as, ip, &di, pi, need_unwind_info, arg);
    if (ret < 0) {
        return ret;
    }

    if (ip < pi->start_ip || ip >= pi->end_ip) {
        Debug (1, "ip %p not in range start_ip %p end_ip %p\n", ip, pi->start_ip, pi->end_ip);
        return -UNW_ENOINFO;
    }
    return UNW_ESUCCESS;
}
