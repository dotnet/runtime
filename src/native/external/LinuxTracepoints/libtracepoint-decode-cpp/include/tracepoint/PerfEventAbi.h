// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Adapted from linux/uapi/linux/perf_event.h.

#pragma once
#ifndef _included_PerfEventAbi_h
#define _included_PerfEventAbi_h

#include <stdint.h>

#ifdef _WIN32
#include <sal.h>
#endif
#ifndef _Ret_z_ 
#define _Ret_z_
#endif
#ifndef _Pre_cap_
#define _Pre_cap_(c)
#endif

#ifndef _ltpDecl
#ifdef _WIN32
#define _ltpDecl __cdecl
#else
#define _ltpDecl
#endif
#endif

// uint32 value for perf_event_attr::type.
enum perf_type_id : uint32_t {
    PERF_TYPE_HARDWARE = 0,
    PERF_TYPE_SOFTWARE = 1,
    PERF_TYPE_TRACEPOINT = 2,
    PERF_TYPE_HW_CACHE = 3,
    PERF_TYPE_RAW = 4,
    PERF_TYPE_BREAKPOINT = 5,

    PERF_TYPE_MAX, // non-ABI
};

// uint32 value for perf_event_attr::size.
enum perf_event_attr_size : uint32_t
{
    PERF_ATTR_SIZE_VER0 = 64,  // first published struct
    PERF_ATTR_SIZE_VER1 = 72,  // add: config2
    PERF_ATTR_SIZE_VER2 = 80,  // add: branch_sample_type
    PERF_ATTR_SIZE_VER3 = 96,  // add: sample_regs_user, sample_stack_user
    PERF_ATTR_SIZE_VER4 = 104, // add: sample_regs_intr
    PERF_ATTR_SIZE_VER5 = 112, // add: aux_watermark
    PERF_ATTR_SIZE_VER6 = 120, // add: aux_sample_size
    PERF_ATTR_SIZE_VER7 = 128, // add: sig_data
};

// bits that can be set in perf_event_attr::sample_type.
enum perf_event_sample_format {
    PERF_SAMPLE_IP = 1U << 0,
    PERF_SAMPLE_TID = 1U << 1,
    PERF_SAMPLE_TIME = 1U << 2,
    PERF_SAMPLE_ADDR = 1U << 3,
    PERF_SAMPLE_READ = 1U << 4,
    PERF_SAMPLE_CALLCHAIN = 1U << 5,
    PERF_SAMPLE_ID = 1U << 6,
    PERF_SAMPLE_CPU = 1U << 7,
    PERF_SAMPLE_PERIOD = 1U << 8,
    PERF_SAMPLE_STREAM_ID = 1U << 9,
    PERF_SAMPLE_RAW = 1U << 10,
    PERF_SAMPLE_BRANCH_STACK = 1U << 11,
    PERF_SAMPLE_REGS_USER = 1U << 12,
    PERF_SAMPLE_STACK_USER = 1U << 13,
    PERF_SAMPLE_WEIGHT = 1U << 14,
    PERF_SAMPLE_DATA_SRC = 1U << 15,
    PERF_SAMPLE_IDENTIFIER = 1U << 16,
    PERF_SAMPLE_TRANSACTION = 1U << 17,
    PERF_SAMPLE_REGS_INTR = 1U << 18,
    PERF_SAMPLE_PHYS_ADDR = 1U << 19,
    PERF_SAMPLE_AUX = 1U << 20,
    PERF_SAMPLE_CGROUP = 1U << 21,
    PERF_SAMPLE_DATA_PAGE_SIZE = 1U << 22,
    PERF_SAMPLE_CODE_PAGE_SIZE = 1U << 23,
    PERF_SAMPLE_WEIGHT_STRUCT = 1U << 24,

    PERF_SAMPLE_MAX = 1U << 25, // non-ABI
    PERF_SAMPLE_WEIGHT_TYPE = PERF_SAMPLE_WEIGHT | PERF_SAMPLE_WEIGHT_STRUCT,
};

// bits that can be set in perf_event_attr::read_format.
enum perf_event_read_format {
    PERF_FORMAT_TOTAL_TIME_ENABLED = 1U << 0,
    PERF_FORMAT_TOTAL_TIME_RUNNING = 1U << 1,
    PERF_FORMAT_ID = 1U << 2,
    PERF_FORMAT_GROUP = 1U << 3,
    PERF_FORMAT_LOST = 1U << 4,

    PERF_FORMAT_MAX = 1U << 5, // non-ABI
};

// Event's collection parameters.
struct perf_event_attr {
    perf_type_id         type;  // Major type: hardware/software/tracepoint/etc.  
    perf_event_attr_size size;  // Size of the attr structure, for fwd/bwd compat.  
    uint64_t            config; // Type-specific configuration information.

    union {
        uint64_t        sample_period;
        uint64_t        sample_freq;
    };

    uint64_t sample_type; // perf_event_sample_format
    uint64_t read_format; // perf_event_read_format

    uint64_t disabled : 1; // off by default
    uint64_t inherit : 1; // children inherit it
    uint64_t pinned : 1; // must always be on PMU
    uint64_t exclusive : 1; // only group on PMU
    uint64_t exclude_user : 1; // don't count user
    uint64_t exclude_kernel : 1; // ditto kernel
    uint64_t exclude_hv : 1; // ditto hypervisor
    uint64_t exclude_idle : 1; // don't count when idle
    uint64_t mmap : 1; // include mmap data
    uint64_t comm : 1; // include comm data
    uint64_t freq : 1; // use freq, not period
    uint64_t inherit_stat : 1; // per task counts
    uint64_t enable_on_exec : 1; // next exec enables
    uint64_t task : 1; // trace fork/exit
    uint64_t watermark : 1; // wakeup_watermark

    // skid constraint:
    // 0 - SAMPLE_IP can have arbitrary skid
    // 1 - SAMPLE_IP must have constant skid
    // 2 - SAMPLE_IP requested to have 0 skid
    // 3 - SAMPLE_IP must have 0 skid
    // See also PERF_RECORD_MISC_EXACT_IP
    uint64_t precise_ip : 2;
    uint64_t mmap_data : 1; // non-exec mmap data
    uint64_t sample_id_all : 1; // sample_type all events

    uint64_t exclude_host : 1; // don't count in host
    uint64_t exclude_guest : 1; // don't count in guest

    uint64_t exclude_callchain_kernel : 1; // exclude kernel callchains
    uint64_t exclude_callchain_user : 1; // exclude user callchains
    uint64_t mmap2 : 1; // include mmap with inode data
    uint64_t comm_exec : 1; // flag comm events that are due to an exec
    uint64_t use_clockid : 1; // use @clockid for time fields
    uint64_t context_switch : 1; // context switch data
    uint64_t write_backward : 1; // Write ring buffer from end to beginning
    uint64_t namespaces : 1; // include namespaces data
    uint64_t ksymbol : 1; // include ksymbol events
    uint64_t bpf_event : 1; // include bpf events
    uint64_t aux_output : 1; // generate AUX records instead of events
    uint64_t cgroup : 1; // include cgroup events
    uint64_t text_poke : 1; // include text poke events
    uint64_t build_id : 1; // use build id in mmap2 events
    uint64_t inherit_thread : 1; // children only inherit if cloned with CLONE_THREAD
    uint64_t remove_on_exec : 1; // event is removed from task on exec
    uint64_t sigtrap : 1; // send synchronous SIGTRAP on event
    uint64_t reserved1 : 26;

    union {
        uint32_t        wakeup_events;      // wakeup every n events
        uint32_t        wakeup_watermark; // bytes before wakeup
    };

    uint32_t            bp_type;
    union {
        uint64_t        bp_addr;
        uint64_t        kprobe_func; // for perf_kprobe
        uint64_t        uprobe_path; // for perf_uprobe
        uint64_t        config1; // extension of config
    };
    union {
        uint64_t        bp_len;
        uint64_t        kprobe_addr; // when kprobe_func == NULL
        uint64_t        probe_offset; // for perf_[k,u]probe
        uint64_t        config2; // extension of config1
    };
    uint64_t    branch_sample_type; // enum perf_branch_sample_type

    // Defines set of user regs to dump on samples.
    // See asm/perf_regs.h for details.  
    uint64_t    sample_regs_user;

    // Defines size of the user stack to dump on samples.  
    uint32_t    sample_stack_user;

    int32_t    clockid;

    // Defines set of regs to dump for each sample state captured on:
    // - precise = 0: PMU interrupt
    // - precise > 0: sampled instruction
    // See asm/perf_regs.h for details.
    uint64_t    sample_regs_intr;

    // Wakeup watermark for AUX area 
    uint32_t    aux_watermark;
    uint16_t    sample_max_stack;
    uint16_t    reserved2;
    uint32_t    aux_sample_size;
    uint32_t    reserved3;

    // User provided data if sigtrap=1, passed back to user via
    // siginfo_t::si_perf_data, e.g. to permit user to identify the event.
    // Note, siginfo_t::si_perf_data is long-sized, and sig_data will be
    // truncated accordingly on 32 bit architectures.
    uint64_t    sig_data;

    // Reverse the endian order of all fields in this struct.
    void ByteSwap() noexcept;
};
static_assert(sizeof(perf_event_attr) == PERF_ATTR_SIZE_VER7, "Bad perf_event_attr");

// uint32 value for perf_event_header::type.
enum perf_event_type : uint32_t {

    /*
     * If perf_event_attr.sample_id_all is set then all event types will
     * have the sample_type selected fields related to where/when
     * (identity) an event took place (TID, TIME, ID, STREAM_ID, CPU,
     * IDENTIFIER) described in PERF_RECORD_SAMPLE below, it will be stashed
     * just after the perf_event_header and the fields already present for
     * the existing fields, i.e. at the end of the payload. That way a newer
     * perf.data file will be supported by older perf tools, with these new
     * optional fields being ignored.
     *
     * struct sample_id {
     *     { u32            pid, tid; } && PERF_SAMPLE_TID
     *     { u64            time;     } && PERF_SAMPLE_TIME
     *     { u64            id;       } && PERF_SAMPLE_ID
     *     { u64            stream_id;} && PERF_SAMPLE_STREAM_ID
     *     { u32            cpu, res; } && PERF_SAMPLE_CPU
     *    { u64            id;      } && PERF_SAMPLE_IDENTIFIER
     * } && perf_event_attr::sample_id_all
     *
     * Note that PERF_SAMPLE_IDENTIFIER duplicates PERF_SAMPLE_ID.  The
     * advantage of PERF_SAMPLE_IDENTIFIER is that its position is fixed
     * relative to header.size.
     */

    /*
     * The MMAP events record the PROT_EXEC mappings so that we can
     * correlate userspace IPs to code. They have the following structure:
     *
     * struct {
     *    struct perf_event_header    header;
     *
     *    u32                pid, tid;
     *    u64                addr;
     *    u64                len;
     *    u64                pgoff;
     *    char                filename[];
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_MMAP            = 1,

    /*
     * struct {
     *    struct perf_event_header    header;
     *    u64                id;
     *    u64                lost;
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_LOST            = 2,

    /*
     * struct {
     *    struct perf_event_header    header;
     *
     *    u32                pid, tid;
     *    char                comm[];
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_COMM            = 3,

    /*
     * struct {
     *    struct perf_event_header    header;
     *    u32                pid, ppid;
     *    u32                tid, ptid;
     *    u64                time;
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_EXIT            = 4,

    /*
     * struct {
     *    struct perf_event_header    header;
     *    u64                time;
     *    u64                id;
     *    u64                stream_id;
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_THROTTLE            = 5,
    PERF_RECORD_UNTHROTTLE            = 6,

    /*
     * struct {
     *    struct perf_event_header    header;
     *    u32                pid, ppid;
     *    u32                tid, ptid;
     *    u64                time;
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_FORK            = 7,

    /*
     * struct {
     *    struct perf_event_header    header;
     *    u32                pid, tid;
     *
     *    struct read_format        values;
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_READ            = 8,

    /*
     * struct {
     *    struct perf_event_header    header;
     *
     *    #
     *    # Note that PERF_SAMPLE_IDENTIFIER duplicates PERF_SAMPLE_ID.
     *    # The advantage of PERF_SAMPLE_IDENTIFIER is that its position
     *    # is fixed relative to header.
     *    #
     *
     *    { u64            id;      } && PERF_SAMPLE_IDENTIFIER
     *    { u64            ip;      } && PERF_SAMPLE_IP
     *    { u32            pid, tid; } && PERF_SAMPLE_TID
     *    { u64            time;     } && PERF_SAMPLE_TIME
     *    { u64            addr;     } && PERF_SAMPLE_ADDR
     *    { u64            id;      } && PERF_SAMPLE_ID
     *    { u64            stream_id;} && PERF_SAMPLE_STREAM_ID
     *    { u32            cpu, res; } && PERF_SAMPLE_CPU
     *    { u64            period;   } && PERF_SAMPLE_PERIOD
     *
     *    { struct read_format    values;      } && PERF_SAMPLE_READ
     *
     *    { u64            nr,
     *      u64            ips[nr];  } && PERF_SAMPLE_CALLCHAIN
     *
     *    #
     *    # The RAW record below is opaque data wrt the ABI
     *    #
     *    # That is, the ABI doesn't make any promises wrt to
     *    # the stability of its content, it may vary depending
     *    # on event, hardware, kernel version and phase of
     *    # the moon.
     *    #
     *    # In other words, PERF_SAMPLE_RAW contents are not an ABI.
     *    #
     *
     *    { u32            size;
     *      char                  data[size];}&& PERF_SAMPLE_RAW
     *
     *    { u64                   nr;
     *      { u64    hw_idx; } && PERF_SAMPLE_BRANCH_HW_INDEX
     *        { u64 from, to, flags } lbr[nr];
     *      } && PERF_SAMPLE_BRANCH_STACK
     *
     *     { u64            abi; # enum perf_sample_regs_abi
     *       u64            regs[weight(mask)]; } && PERF_SAMPLE_REGS_USER
     *
     *     { u64            size;
     *       char            data[size];
     *       u64            dyn_size; } && PERF_SAMPLE_STACK_USER
     *
     *    { union perf_sample_weight
     *     {
     *        u64        full; && PERF_SAMPLE_WEIGHT
     *    #if defined(__LITTLE_ENDIAN_BITFIELD)
     *        struct {
     *            u32    var1_dw;
     *            u16    var2_w;
     *            u16    var3_w;
     *        } && PERF_SAMPLE_WEIGHT_STRUCT
     *    #elif defined(__BIG_ENDIAN_BITFIELD)
     *        struct {
     *            u16    var3_w;
     *            u16    var2_w;
     *            u32    var1_dw;
     *        } && PERF_SAMPLE_WEIGHT_STRUCT
     *    #endif
     *     }
     *    }
     *    { u64            data_src; } && PERF_SAMPLE_DATA_SRC
     *    { u64            transaction; } && PERF_SAMPLE_TRANSACTION
     *    { u64            abi; # enum perf_sample_regs_abi
     *      u64            regs[weight(mask)]; } && PERF_SAMPLE_REGS_INTR
     *    { u64            phys_addr;} && PERF_SAMPLE_PHYS_ADDR
     *    { u64            size;
     *      char            data[size]; } && PERF_SAMPLE_AUX
     *    { u64            data_page_size;} && PERF_SAMPLE_DATA_PAGE_SIZE
     *    { u64            code_page_size;} && PERF_SAMPLE_CODE_PAGE_SIZE
     * };
     */
    PERF_RECORD_SAMPLE            = 9,

    /*
     * The MMAP2 records are an augmented version of MMAP, they add
     * maj, min, ino numbers to be used to uniquely identify each mapping
     *
     * struct {
     *    struct perf_event_header    header;
     *
     *    u32                pid, tid;
     *    u64                addr;
     *    u64                len;
     *    u64                pgoff;
     *    union {
     *        struct {
     *            u32        maj;
     *            u32        min;
     *            u64        ino;
     *            u64        ino_generation;
     *        };
     *        struct {
     *            u8        build_id_size;
     *            u8        __reserved_1;
     *            u16        __reserved_2;
     *            u8        build_id[20];
     *        };
     *    };
     *    u32                prot, flags;
     *    char                filename[];
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_MMAP2            = 10,

    /*
     * Records that new data landed in the AUX buffer part.
     *
     * struct {
     *     struct perf_event_header    header;
     *
     *     u64                aux_offset;
     *     u64                aux_size;
     *    u64                flags;
     *     struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_AUX                = 11,

    /*
     * Indicates that instruction trace has started
     *
     * struct {
     *    struct perf_event_header    header;
     *    u32                pid;
     *    u32                tid;
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_ITRACE_START        = 12,

    /*
     * Records the dropped/lost sample number.
     *
     * struct {
     *    struct perf_event_header    header;
     *
     *    u64                lost;
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_LOST_SAMPLES        = 13,

    /*
     * Records a context switch in or out (flagged by
     * PERF_RECORD_MISC_SWITCH_OUT). See also
     * PERF_RECORD_SWITCH_CPU_WIDE.
     *
     * struct {
     *    struct perf_event_header    header;
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_SWITCH            = 14,

    /*
     * CPU-wide version of PERF_RECORD_SWITCH with next_prev_pid and
     * next_prev_tid that are the next (switching out) or previous
     * (switching in) pid/tid.
     *
     * struct {
     *    struct perf_event_header    header;
     *    u32                next_prev_pid;
     *    u32                next_prev_tid;
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_SWITCH_CPU_WIDE        = 15,

    /*
     * struct {
     *    struct perf_event_header    header;
     *    u32                pid;
     *    u32                tid;
     *    u64                nr_namespaces;
     *    { u64                dev, inode; } [nr_namespaces];
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_NAMESPACES            = 16,

    /*
     * Record ksymbol register/unregister events:
     *
     * struct {
     *    struct perf_event_header    header;
     *    u64                addr;
     *    u32                len;
     *    u16                ksym_type;
     *    u16                flags;
     *    char                name[];
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_KSYMBOL            = 17,

    /*
     * Record bpf events:
     *  enum perf_bpf_event_type {
     *    PERF_BPF_EVENT_UNKNOWN        = 0,
     *    PERF_BPF_EVENT_PROG_LOAD    = 1,
     *    PERF_BPF_EVENT_PROG_UNLOAD    = 2,
     *  };
     *
     * struct {
     *    struct perf_event_header    header;
     *    u16                type;
     *    u16                flags;
     *    u32                id;
     *    u8                tag[BPF_TAG_SIZE];
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_BPF_EVENT            = 18,

    /*
     * struct {
     *    struct perf_event_header    header;
     *    u64                id;
     *    char                path[];
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_CGROUP            = 19,

    /*
     * Records changes to kernel text i.e. self-modified code. 'old_len' is
     * the number of old bytes, 'new_len' is the number of new bytes. Either
     * 'old_len' or 'new_len' may be zero to indicate, for example, the
     * addition or removal of a trampoline. 'bytes' contains the old bytes
     * followed immediately by the new bytes.
     *
     * struct {
     *    struct perf_event_header    header;
     *    u64                addr;
     *    u16                old_len;
     *    u16                new_len;
     *    u8                bytes[];
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_TEXT_POKE            = 20,

    /*
     * Data written to the AUX area by hardware due to aux_output, may need
     * to be matched to the event by an architecture-specific hardware ID.
     * This records the hardware ID, but requires sample_id to provide the
     * event ID. e.g. Intel PT uses this record to disambiguate PEBS-via-PT
     * records from multiple events.
     *
     * struct {
     *    struct perf_event_header    header;
     *    u64                hw_id;
     *    struct sample_id        sample_id;
     * };
     */
    PERF_RECORD_AUX_OUTPUT_HW_ID        = 21,

    PERF_RECORD_MAX, // non-ABI

    PERF_RECORD_USER_TYPE_START = 64,

    /*
     * struct attr_event {
     *     struct perf_event_header header;
     *     struct perf_event_attr attr;
     *     uint64_t id[];
     * };
     */
    PERF_RECORD_HEADER_ATTR = 64,

    /* deprecated
     *
     * #define MAX_EVENT_NAME 64
     * 
     * struct perf_trace_event_type {
     *     uint64_t    event_id;
     *     char    name[MAX_EVENT_NAME];
     * };
     * 
     * struct event_type_event {
     *     struct perf_event_header header;
     *     struct perf_trace_event_type event_type;
     * };
     */
    PERF_RECORD_HEADER_EVENT_TYPE = 65,

    /* Describe me
     *
     * struct tracing_data_event {
     *     struct perf_event_header header;
     *     uint32_t size;
     * };
     */
    PERF_RECORD_HEADER_TRACING_DATA = 66,

    /* Define a ELF build ID for a referenced executable. */
    PERF_RECORD_HEADER_BUILD_ID = 67,

    /* No event reordering over this header. No payload. */
    PERF_RECORD_FINISHED_ROUND = 68,

    /*
     * Map event ids to CPUs and TIDs.
     * 
     * struct id_index_entry {
     *     uint64_t id;
     *     uint64_t idx;
     *     uint64_t cpu;
     *     uint64_t tid;
     * };
     * 
     * struct id_index_event {
     *     struct perf_event_header header;
     *     uint64_t nr;
     *     struct id_index_entry entries[nr];
     * };
     */
    PERF_RECORD_ID_INDEX = 69,

    /*
     * Auxtrace type specific information. Describe me
     * 
     * struct auxtrace_info_event {
     *     struct perf_event_header header;
     *     uint32_t type;
     *     uint32_t reserved__; // For alignment
     *     uint64_t priv[];
     * };
     */
    PERF_RECORD_AUXTRACE_INFO = 70,

    /*
     * Defines auxtrace data. Followed by the actual data. The contents of
     * the auxtrace data is dependent on the event and the CPU. For example
     * for Intel Processor Trace it contains Processor Trace data generated
     * by the CPU.
     * 
     * struct auxtrace_event {
     *      struct perf_event_header header;
     *      uint64_t size;
     *      uint64_t offset;
     *      uint64_t reference;
     *      uint32_t idx;
     *      uint32_t tid;
     *      uint32_t cpu;
     *      uint32_t reserved__; // For alignment
     * };
     * 
     * struct aux_event {
     *      struct perf_event_header header;
     *      uint64_t    aux_offset;
     *      uint64_t    aux_size;
     *      uint64_t    flags;
     * };
     */
    PERF_RECORD_AUXTRACE = 71,

    /*
     * Describes an error in hardware tracing
     * 
     * enum auxtrace_error_type {
     *     PERF_AUXTRACE_ERROR_ITRACE  = 1,
     *     PERF_AUXTRACE_ERROR_MAX
     * };
     * 
     * #define MAX_AUXTRACE_ERROR_MSG 64
     * 
     * struct auxtrace_error_event {
     *     struct perf_event_header header;
     *     uint32_t type;
     *     uint32_t code;
     *     uint32_t cpu;
     *     uint32_t pid;
     *     uint32_t tid;
     *     uint32_t reserved__; // For alignment
     *     uint64_t ip;
     *     char msg[MAX_AUXTRACE_ERROR_MSG];
     * };
     */
    PERF_RECORD_AUXTRACE_ERROR = 72,

    PERF_RECORD_THREAD_MAP = 73,
    PERF_RECORD_CPU_MAP = 74,
    PERF_RECORD_STAT_CONFIG = 75,
    PERF_RECORD_STAT = 76,
    PERF_RECORD_STAT_ROUND = 77,
    PERF_RECORD_EVENT_UPDATE = 78,
    PERF_RECORD_TIME_CONV = 79,

    /*
     * Describes a header feature. These are records used in pipe-mode that
     * contain information that otherwise would be in perf.data file's header.
     */
    PERF_RECORD_HEADER_FEATURE = 80,

    /*
     * struct compressed_event {
     *     struct perf_event_header    header;
     *     char                data[];
     * };

    The header is followed by compressed data frame that can be decompressed
    into array of perf trace records. The size of the entire compressed event
    record including the header is limited by the max value of header.size.
     */
    PERF_RECORD_COMPRESSED = 81,

    /*
    Marks the end of records for the system, pre-existing threads in system wide
    sessions, etc. Those are the ones prefixed PERF_RECORD_USER_*.

    This is used, for instance, to 'perf inject' events after init and before
    regular events, those emitted by the kernel, to support combining guest and
    host records.
    */
    PERF_RECORD_FINISHED_INIT = 82,
};

// Information at the start of each event.
struct perf_event_header {
    perf_event_type type;
    uint16_t misc;
    uint16_t size;

    // Reverse the endian order of all fields in this struct.
    void ByteSwap() noexcept;
};

namespace tracepoint_decode
{
    // Returns a string for the PERF_TYPE_* enum value, e.g. "HARDWARE".
    // If enum is not recognized, formats decimal value into and returns scratch.
    _Ret_z_ char const* _ltpDecl
    PerfEnumToString(perf_type_id value, _Pre_cap_(11) char* scratch) noexcept;

    // Returns a string for the PERF_RECORD_* enum value, e.g. "SAMPLE".
    // If enum is not recognized, formats decimal value into and returns scratch.
    _Ret_z_ char const* _ltpDecl
    PerfEnumToString(perf_event_type value, _Pre_cap_(11) char* scratch) noexcept;
}
// namespace tracepoint_decode

#endif // _included_PerfEventAbi_h
