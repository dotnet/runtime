// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++

Module Name:

    gcrecord.h

--*/

#ifndef __gc_record_h__
#define __gc_record_h__

//#define max_generation 2

// We pack the dynamic tuning for deciding which gen to condemn in a uint32_t.
// We assume that 2 bits are enough to represent the generation. 
#define bits_generation 2
#define generation_mask (~(~0u << bits_generation))
//=======================note !!!===================================//
// If you add stuff to this enum, remember to update total_gen_reasons
// and record_condemn_gen_reasons below.
//=======================note !!!===================================//

// These are condemned reasons related to generations.
// Each reason takes up 2 bits as we have 3 generations.
// So we can store up to 16 reasons in this uint32_t.
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
    uint32_t condemn_reasons_gen;
    uint32_t condemn_reasons_condition;

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

    void set_gen (gc_condemn_reason_gen condemn_gen_reason, uint32_t value)
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
        uint32_t temp_conditions = 1 << condition_to_check;
        return !(condemn_reasons_condition ^ temp_conditions);
    }

    uint32_t get_gen (gc_condemn_reason_gen condemn_gen_reason)
    {
        uint32_t value = ((condemn_reasons_gen >> (condemn_gen_reason * 2)) & generation_mask);
        return value;
    }

    uint32_t get_condition (gc_condemn_reason_condition condemn_gen_reason)
    {
        uint32_t value = (condemn_reasons_condition & (1 << condemn_gen_reason));
        return value;
    }

    uint32_t get_reasons0()
    {
        return condemn_reasons_gen;
    }

    uint32_t get_reasons1()
    {
        return condemn_reasons_condition;
    }

#ifdef DT_LOG
    char get_gen_char (uint32_t value)
    {
        return char_gen_number[value];
    }
    char get_condition_char (uint32_t value)
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
    uint32_t running_free_list_efficiency;
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
    expand_reuse_normal = 0,
    expand_reuse_bestfit = 1,
    expand_new_seg_ep = 2, // new seg with ephemeral promotion
    expand_new_seg = 3,
    expand_no_memory = 4, // we can't get a new seg.
    expand_next_full_gc = 5, 
    max_expand_mechanisms_count = 6
};

#ifdef DT_LOG
static char* str_heap_expand_mechanisms[] = 
{
    "reused seg with normal fit",
    "reused seg with best fit",
    "expand promoting eph",
    "expand with a new seg",
    "no memory for a new seg",
    "expand in next full GC"
};
#endif //DT_LOG

enum gc_heap_compact_reason
{
    compact_low_ephemeral = 0,
    compact_high_frag = 1,
    compact_no_gaps = 2,
    compact_loh_forced = 3,
    compact_last_gc = 4,
    compact_induced_compacting = 5,
    compact_fragmented_gen0 = 6, 
    compact_high_mem_load = 7, 
    compact_high_mem_frag = 8, 
    compact_vhigh_mem_frag = 9,
    compact_no_gc_mode = 10,
    max_compact_reasons_count = 11
};

#ifndef DACCESS_COMPILE
static BOOL gc_heap_compact_reason_mandatory_p[] =
{
    TRUE, //compact_low_ephemeral = 0,
    FALSE, //compact_high_frag = 1,
    TRUE, //compact_no_gaps = 2,
    TRUE, //compact_loh_forced = 3,
    TRUE, //compact_last_gc = 4
    TRUE, //compact_induced_compacting = 5,
    FALSE, //compact_fragmented_gen0 = 6, 
    FALSE, //compact_high_mem_load = 7, 
    TRUE, //compact_high_mem_frag = 8, 
    TRUE, //compact_vhigh_mem_frag = 9,
    TRUE //compact_no_gc_mode = 10
};

static BOOL gc_expand_mechanism_mandatory_p[] =
{
    FALSE, //expand_reuse_normal = 0,
    TRUE, //expand_reuse_bestfit = 1,
    FALSE, //expand_new_seg_ep = 2, // new seg with ephemeral promotion
    TRUE, //expand_new_seg = 3,
    FALSE, //expand_no_memory = 4, // we can't get a new seg.
    TRUE //expand_next_full_gc = 5
};
#endif //!DACCESS_COMPILE

#ifdef DT_LOG
static char* str_heap_compact_reasons[] = 
{
    "low on ephemeral space",
    "high fragmentation",
    "couldn't allocate gaps",
    "user specfied compact LOH",
    "last GC before OOM",
    "induced compacting GC",
    "fragmented gen0 (ephemeral GC)", 
    "high memory load (ephemeral GC)",
    "high memory load and frag",
    "very high memory load and frag",
    "no gc mode"
};
#endif //DT_LOG

enum gc_mechanism_per_heap
{
    gc_heap_expand,
    gc_heap_compact,
    max_mechanism_per_heap
};

enum gc_mechanism_bit_per_heap
{
    gc_mark_list_bit = 0,
    gc_demotion_bit = 1, 
    max_gc_mechanism_bits_count = 2
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
    {"compacted because of ", str_heap_compact_reasons}
};
#endif //DT_LOG

// Get the 0-based index of the most-significant bit in the value.
// Returns -1 if the input value is zero (i.e. has no set bits).
int index_of_highest_set_bit (size_t value);

#define mechanism_mask (1 << (sizeof (uint32_t) * 8 - 1))
// interesting per heap data we want to record for each GC.
class gc_history_per_heap
{
public:
    gc_generation_data gen_data[max_generation+2]; 
    maxgen_size_increase maxgen_size_info;
    gen_to_condemn_tuning gen_to_condemn_reasons;

    // The mechanisms data is compacted in the following way:
    // most significant bit indicates if we did the operation.
    // the rest of the bits indicate the reason/mechanism
    // why we chose to do the operation. For example:
    // if we did a heap expansion using best fit we'd have
    // 0x80000002 for the gc_heap_expand mechanism.
    // Only one value is possible for each mechanism - meaning the 
    // values are all exclusive
    // TODO: for the config stuff I need to think more about how to represent this
    // because we might want to know all reasons (at least all mandatory ones) for 
    // compact.
    // TODO: no need to the MSB for this
    uint32_t mechanisms[max_mechanism_per_heap];

    // Each bit in this uint32_t represent if a mechanism was used or not.
    uint32_t machanism_bits;

    uint32_t heap_index; 

    size_t extra_gen0_committed;

    void set_mechanism (gc_mechanism_per_heap mechanism_per_heap, uint32_t value);

    void set_mechanism_bit (gc_mechanism_bit_per_heap mech_bit)
    {
        machanism_bits |= 1 << mech_bit;
    }

    void clear_mechanism_bit (gc_mechanism_bit_per_heap mech_bit)
    {
        machanism_bits &= ~(1 << mech_bit);
    }

    BOOL is_mechanism_bit_set (gc_mechanism_bit_per_heap mech_bit)
    {
        return (machanism_bits & (1 << mech_bit));
    }
    
    void clear_mechanism(gc_mechanism_per_heap mechanism_per_heap)
    {
        uint32_t* mechanism = &mechanisms[mechanism_per_heap];
        *mechanism = 0;
    }

    int get_mechanism (gc_mechanism_per_heap mechanism_per_heap)
    {
        uint32_t mechanism = mechanisms[mechanism_per_heap];

        if (mechanism & mechanism_mask)
        {
            int index = index_of_highest_set_bit ((size_t)(mechanism & (~mechanism_mask)));
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
    global_compaction = 1,
    global_promotion = 2,
    global_demotion = 3,
    global_card_bundles = 4,
    global_elevation = 5,
    max_global_mechanisms_count
};

struct gc_history_global
{
    // We may apply other factors after we calculated gen0 budget in
    // desired_new_allocation such as equalization or smoothing so
    // record the final budget here. 
    size_t final_youngest_desired;
    uint32_t num_heaps;
    int condemned_generation;
    int gen0_reduction_count;
    gc_reason reason;
    int pause_mode;
    uint32_t mem_pressure;
    uint32_t global_mechanisms_p;

    void set_mechanism_p (gc_global_mechanism_p mechanism)
    {
        global_mechanisms_p |= (1 << mechanism);
    }

    BOOL get_mechanism_p (gc_global_mechanism_p mechanism)
    {
        return (global_mechanisms_p & (1 << mechanism));
    }

    void print();
};

#endif //__gc_record_h__
