//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*++

Module Name:

    gcrecord.h

--*/

#ifndef __gc_record_h__
#define __gc_record_h__

#define max_generation 2

// We pack the dynamic tuning for deciding which gen to condemn in a DWORD.
// We assume that 2 bits are enough to represent the generation. 
#define bits_generation 2
#define generation_mask (~(~0 << bits_generation))
//=======================note !!!===================================//
// If you add stuff to this enum, remember to update total_gen_reasons
// and record_condemn_gen_reasons below.
//=======================note !!!===================================//

// These are condemned reasons related to generations.
// Each reason takes up 2 bits as we have 3 generations.
// So we can store up to 16 reasons in this DWORD.
// They need processing before being used.
// See the set and the get method for details.
enum gc_condemn_reason_gen
{
    gen_initial         = 0, // indicates the initial gen to condemn.
    gen_final_per_heap  = 1, // indicates the final gen to condemn per heap.
    gen_alloc_budget    = 2, // indicates which gen's budget is exceeded.
    gen_time_tuning     = 3, // indicates the gen number that time based tuning decided.
    gcrg_max = 4
};

// These are condemned reasons related to conditions we are in.
// For example, we are in very high memory load which is a condition. 
// Each condition takes up a single bit indicates TRUE or FALSE.
// We can store 32 of these.
enum gc_condemn_reason_condition
{
    gen_induced_fullgc_p  = 0,
    gen_expand_fullgc_p = 1,
    gen_high_mem_p = 2,
    gen_very_high_mem_p = 3,
    gen_low_ephemeral_p = 4,
    gen_low_card_p = 5,
    gen_eph_high_frag_p = 6,
    gen_max_high_frag_p = 7,
    gen_max_high_frag_e_p = 8,
    gen_max_high_frag_m_p = 9, 
    gen_max_high_frag_vm_p = 10,
    gen_max_gen1 = 11,
    gen_before_oom = 12,
    gen_gen2_too_small = 13,
    gen_induced_noforce_p = 14,
    gen_before_bgc = 15,
    gen_almost_max_alloc = 16,
    gcrc_max = 17
};

#ifdef DT_LOG
static char* record_condemn_reasons_gen_header = "[cg]i|f|a|t|";
static char* record_condemn_reasons_condition_header = "[cc]i|e|h|v|l|l|e|m|m|m|m|g|o|s|n|b|a|";
static char char_gen_number[4] = {'0', '1', '2', '3'};
#endif //DT_LOG

class gen_to_condemn_tuning
{
    DWORD condemn_reasons_gen;
    DWORD condemn_reasons_condition;

#ifdef DT_LOG
    char str_reasons_gen[64];
    char str_reasons_condition[64];
#endif //DT_LOG

    void init_str()
    {
#ifdef DT_LOG
        memset (str_reasons_gen, '|', sizeof (char) * 64);
        str_reasons_gen[gcrg_max*2] = 0;
        memset (str_reasons_condition, '|', sizeof (char) * 64);
        str_reasons_condition[gcrc_max*2] = 0;
#endif //DT_LOG
    }

public:
    void init()
    {
        condemn_reasons_gen = 0;
        condemn_reasons_condition = 0;
        init_str();
    }

    void init (gen_to_condemn_tuning* reasons)
    {
        condemn_reasons_gen = reasons->condemn_reasons_gen;
        condemn_reasons_condition = reasons->condemn_reasons_condition;
        init_str();
    }

    void set_gen (gc_condemn_reason_gen condemn_gen_reason, DWORD value)
    {
        assert ((value & (~generation_mask)) == 0);
        condemn_reasons_gen |= (value << (condemn_gen_reason * 2));
    }

    void set_condition (gc_condemn_reason_condition condemn_gen_reason)
    {
        condemn_reasons_condition |= (1 << condemn_gen_reason);
    }

    // This checks if condition_to_check is the only condition set.
    BOOL is_only_condition (gc_condemn_reason_condition condition_to_check)
    {
        DWORD temp_conditions = 1 << condition_to_check;
        return !(condemn_reasons_condition ^ temp_conditions);
    }

    DWORD get_gen (gc_condemn_reason_gen condemn_gen_reason)
    {
        DWORD value = ((condemn_reasons_gen >> (condemn_gen_reason * 2)) & generation_mask);
        return value;
    }

    DWORD get_condition (gc_condemn_reason_condition condemn_gen_reason)
    {
        DWORD value = (condemn_reasons_condition & (1 << condemn_gen_reason));
        return value;
    }

    DWORD get_reasons0()
    {
        return condemn_reasons_gen;
    }

    DWORD get_reasons1()
    {
        return condemn_reasons_condition;
    }

#ifdef DT_LOG
    char get_gen_char (DWORD value)
    {
        return char_gen_number[value];
    }
    char get_condition_char (DWORD value)
    {
        return (value ? 'Y' : 'N');
    }
#endif //DT_LOG

    void print (int heap_num);
};

// Right now these are all size_t's but if you add a type that requires
// padding you should add a pragma pack here since I am firing this as
// a struct in an ETW event.
struct gc_generation_data
{
    // data recorded at the beginning of a GC
    size_t size_before; // including fragmentation.
    size_t free_list_space_before;
    size_t free_obj_space_before;
    
    // data recorded at the end of a GC
    size_t size_after;  // including fragmentation.
    size_t free_list_space_after;
    size_t free_obj_space_after;
    size_t in;
    size_t pinned_surv;
    size_t npinned_surv;
    size_t new_allocation;

    void print (int heap_num, int gen_num);
};

struct maxgen_size_increase
{
    size_t free_list_allocated;
    size_t free_list_rejected;
    size_t end_seg_allocated;
    size_t condemned_allocated;
    size_t pinned_allocated;
    size_t pinned_allocated_advance;
    DWORD running_free_list_efficiency;
};

// The following indicates various mechanisms and one value
// related to each one. Each value has its corresponding string
// representation so if you change the enum's, make sure you
// also add its string form.

// Note that if we are doing a gen1 GC, we won't 
// really expand the heap if we are reusing, but
// we'll record the can_expand_into_p result here.
enum gc_heap_expand_mechanism
{
    expand_reuse_normal,
    expand_reuse_bestfit,
    expand_new_seg_ep, // new seg with ephemeral promotion
    expand_new_seg,
    expand_no_memory // we can't get a new seg.
};

#ifdef DT_LOG
static char* str_heap_expand_mechanisms[] = 
{
    "reused seg with normal fit",
    "reused seg with best fit",
    "expand promoting eph",
    "expand with a new seg",
    "no memory for a new seg"
};
#endif //DT_LOG

enum gc_compact_reason
{
    compact_low_ephemeral,
    compact_high_frag,
    compact_no_gaps,
    compact_loh_forced
};

#ifdef DT_LOG
static char* str_compact_reasons[] = 
{
    "low on ephemeral space",
    "high fragmetation",
    "couldn't allocate gaps",
    "user specfied compact LOH"
};
#endif //DT_LOG

#ifdef DT_LOG
static char* str_concurrent_compact_reasons[] = 
{
    "high fragmentation",
    "low on ephemeral space in concurrent marking"
};
#endif //DT_LOG

enum gc_mechanism_per_heap
{
    gc_heap_expand,
    gc_compact,
    max_mechanism_per_heap
};

#ifdef DT_LOG
struct gc_mechanism_descr
{
    char* name;
    char** descr;
};

static gc_mechanism_descr gc_mechanisms_descr[max_mechanism_per_heap] =
{
    {"expanded heap ", str_heap_expand_mechanisms},
    {"compacted because of ", str_compact_reasons}
};
#endif //DT_LOG

int index_of_set_bit (size_t power2);

#define mechanism_mask (1 << (sizeof (DWORD) * 8 - 1))
// interesting per heap data we want to record for each GC.
class gc_history_per_heap
{
public:
    gc_generation_data gen_data[max_generation+2]; 
    maxgen_size_increase maxgen_size_info;
    gen_to_condemn_tuning gen_to_condemn_reasons;

    // The mechanisms data is compacted in the following way:
    // most significant bit indicates if we did the operation.
    // the rest of the bits indicate the reason
    // why we chose to do the operation. For example:
    // if we did a heap expansion using best fit we'd have
    // 0x80000002 for the gc_heap_expand mechanism.
    // Only one value is possible for each mechanism - meaning the 
    // values are all exclusive
    DWORD mechanisms[max_mechanism_per_heap];

    DWORD heap_index; 

    size_t extra_gen0_committed;

    void set_mechanism (gc_mechanism_per_heap mechanism_per_heap, DWORD value)
    {
        DWORD* mechanism = &mechanisms[mechanism_per_heap];
        *mechanism |= mechanism_mask;
        *mechanism |= (1 << value);
    }

    void clear_mechanism (gc_mechanism_per_heap mechanism_per_heap)
    {
        DWORD* mechanism = &mechanisms[mechanism_per_heap];
        *mechanism = 0;
    }

    int get_mechanism (gc_mechanism_per_heap mechanism_per_heap)
    {
        DWORD mechanism = mechanisms[mechanism_per_heap];

        if (mechanism & mechanism_mask)
        {
            int index = index_of_set_bit ((size_t)(mechanism & (~mechanism_mask)));
            assert (index != -1);
            return index;
        }

        return -1;
    }

    void print();
};

// we store up to 32 boolean settings.
enum gc_global_mechanism_p
{
    global_concurrent = 0,
    global_compaction,
    global_promotion,
    global_demotion,
    global_card_bundles,
    global_elevation,
    max_global_mechanism
};

struct gc_history_global
{
    // We may apply other factors after we calculated gen0 budget in
    // desired_new_allocation such as equalization or smoothing so
    // record the final budget here. 
    size_t final_youngest_desired;
    DWORD num_heaps;
    int condemned_generation;
    int gen0_reduction_count;
    gc_reason reason;
    int pause_mode;
    DWORD mem_pressure;
    DWORD global_mechanims_p;

    void set_mechanism_p (gc_global_mechanism_p mechanism)
    {
        global_mechanims_p |= (1 << mechanism);
    }

    BOOL get_mechanism_p (gc_global_mechanism_p mechanism)
    {
        return (global_mechanims_p & (1 << mechanism));
    }

    void print();
};

#endif //__gc_record_h__
