/*
 * Unittest AArch64 get_frame_state function by inspecting output at
 * different points in example address spaces (from python2.7)
 */

#include "dwarf.h"
#include "libunwind_i.h"

int unw_is_signal_frame (unw_cursor_t *cursor) { return 0; }
int dwarf_step (struct dwarf_cursor *c) { return 0; }
#include "aarch64/Gstep.c"

static int procedure_size;

/* Mock access_mem implementation */
static int
access_mem (unw_addr_space_t as, unw_word_t addr, unw_word_t *val, int write,
            void *arg)
{
  if (write != 0)
    return -1;

  const size_t mem_size   = procedure_size * sizeof(uint32_t);
  const void *mem_start   = arg;
  const void *mem_end     = (const char*) arg + mem_size;
  const unw_word_t *paddr = (const unw_word_t*) addr;

  if ((void*) paddr < mem_start || (void*) paddr > mem_end)
    {
      return -1;
    }

  *val = *paddr;
  return 0;
}

//! Stub implementation of get_proc_name - returns offset to start of procedure
static int
get_proc_name (unw_addr_space_t as, unw_word_t ip, char *buf, size_t len, unw_word_t *offp,
               void *arg)
{
  *offp = ip - (unw_word_t) arg;
  return 0;
}

int
main ()
{
  struct unw_addr_space mock_address_space;
  mock_address_space.acc.access_mem = &access_mem;
  mock_address_space.acc.get_proc_name = &get_proc_name;

  frame_state_t fs;
  unw_cursor_t cursor;
  struct cursor *c = (struct cursor *) &cursor;
  c->dwarf.as = &mock_address_space;

  /* STP_MOV_start procedure */
  {
    int IpStp  = 0;
    int IpMov  = 1;
    int IpLdp1 = 4;
    int IpLdp2 = 7;

    // 0000000000418254 <copy_absolute>:
    unsigned int instructions[9] = {
      0xa9be7bfd, // stp     x29, x30, [sp,#-32]!     <= FP+LR stored
      0x910003fd, // mov     x29, sp                  <= FP updated
      0xa90153f3, // stp     x19, x20, [sp,#16]
                  // some instructions skipped
      0xa94153f3, // ldp     x19, x20, [sp,#16]
      0xa8c27bfd, // ldp     x29, x30, [sp],#32       <= FP+LR retrieved
      0x17ffff33, // b       417f50 <strcpy@plt>
                  // some instructions skipped
      0xa94153f3, // ldp     x19, x20, [sp,#16]
      0xa8c27bfd, // ldp     x29, x30, [sp],#32       <= FP+LR retrieved
      0x1400de12, // b       44fb08 <joinpath>
    };
    procedure_size = 9;

    c->dwarf.as_arg = &instructions;

    /* IP is pointing to instruction that stores FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpStp);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction that updates the FP */
    c->dwarf.ip = (unw_word_t) (instructions+IpMov);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_SP_OFFSET || fs.offset != 0) return -1;

    /* IP is pointing to instruction after FP was updated */
    c->dwarf.ip = (unw_word_t) (instructions+IpMov+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to first instruction that retrieves FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp1);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction after first retrieval of FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp1+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to second instruction that retrieves FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp2);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction after second retrieval of FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp2+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;
  }

  /* STP_start_MOV_later procedure */
  {
    int IpStp = 0;
    int IpMov = 2;
    int IpLdp = 5;

    // 00000000004181b4 <get_time>:
    unsigned int instructions[7] = {
      0xa9bd7bfd, // stp     x29, x30, [sp,#-48]!     <= FP+LR stored
      0xf0001740, // adrp    x0, 703000
      0x910003fd, // mov     x29, sp                  <= FP updated
      0xf943cc00, // ldr     x0, [x0,#1944]
                  // some instructions skipped
      0xf9400bf3, // ldr     x19, [sp,#16]
      0xa8c37bfd, // ldp     x29, x30, [sp],#48       <= FP+LR retrieved
      0xd65f03c0, // ret
    };
    procedure_size = 7;

    c->dwarf.as_arg = &instructions;

    /* IP is pointing to instruction that stores FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpStp);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction after FP and LR are stored */
    c->dwarf.ip = (unw_word_t) (instructions+IpStp+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_SP_OFFSET || fs.offset != 0) return -1;

    /* IP is pointing to instruction that updates the FP */
    c->dwarf.ip = (unw_word_t) (instructions+IpMov);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_SP_OFFSET || fs.offset != 0) return -1;

    /* IP is pointing to instruction after FP was updated */
    c->dwarf.ip = (unw_word_t) (instructions+IpMov+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction that retrieves FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction after retrieval of FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;
  }

  /* STP_MOV_later procedure */
  {
    int IpStp = 3;
    int IpMov = 4;
    int IpLdp = 7;

    // 0000000000418370 <indenterror>:
    uint32_t instructions[9] = {
      0xb941fc01, // ldr     w1, [x0,#508]
                  // some instructions skipped
      0xd65f03c0, // ret
                  // some instructions skipped
      0x34ffffa4, // cbz     w4, 41838c <indenterror+0x1c>
      0xa9be7bfd, // stp     x29, x30, [sp,#-32]      <= FP+LR stored
      0x910003fd, // mov     x29, sp                  <= FP updated
      0xf9000bf3, // str     x19, [sp,#16]
      0xf9400bf3, // ldr     x19, [sp,#16]
      0xa8c27bfd, // ldp     x29, x30, [sp],#32       <= FP+LR retrieved
      0xd65f03c0, // ret
    };
    procedure_size = 9;

    c->dwarf.as_arg = &instructions;

    /* IP is pointing to start of procedure */
    c->dwarf.ip = (unw_word_t) (instructions);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction that stores FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpStp);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction that updates the FP */
    c->dwarf.ip = (unw_word_t) (instructions+IpMov);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_SP_OFFSET || fs.offset != 0) return -1;

    /* IP is pointing to instruction after FP was updated */
    c->dwarf.ip = (unw_word_t) (instructions+IpMov+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction that retrieves FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction after FP and LR are retrieved */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;
  }

  /* STP_POS_OFFSET procedure */
  {
    int IpStp = 3;
    int IpAdd = 4;
    int IpLdp = 7;

    // 000000000046d1d8 <PyEval_EvalCode>:
    unsigned int instructions[9] = {
      0xd10083ff, // sub     sp, sp, #0x20
      0xd2800007, // mov     x7, #0x0                        // #0
                  // some instructions skipped
      0xd2800003, // mov     x3, #0x0                        // #0
      0xa9017bfd, // stp     x29, x30, [sp,#16]
      0x910043fd, // add     x29, sp, #0x10
      0xb90003ff, // str     wzr, [sp]
                  // some instructions skipped
      0x910003bf, // mov     sp, x29
      0xa8c17bfd, // ldp     x29, x30, [sp],#16
      0xd65f03c0, // ret
    };
    procedure_size = 9;

    c->dwarf.as_arg = &instructions;

    /* IP is pointing to start of procedure */
    c->dwarf.ip = (unw_word_t) (instructions);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction that stores FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpStp);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction that updates the FP */
    c->dwarf.ip = (unw_word_t) (instructions+IpAdd);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_SP_OFFSET || fs.offset != 16) return -1;

    /* IP is pointing to instruction after FP was updated */
    c->dwarf.ip = (unw_word_t) (instructions+IpAdd+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction that retrieves FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction after FP and LR are retrieved */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;
  }

  /* STP_NEG_OFFSET procedure */
  {
    int IpStp = 3;
    int IpAdd = 4;
    int IpLdp = 7;

    // Artificial example based on PyEval_EvalCode with negative offset STP
    unsigned int instructions[9] = {
      0xd10083ff, // sub     sp, sp, #0x20
      0xd2800007, // mov     x7, #0x0                        // #0
                  // some instructions skipped
      0xd2800003, // mov     x3, #0x0                        // #0
      0xa9217bfd, // stp     x29, x30, [sp,#-16]
      0x910043fd, // add     x29, sp, #0x10
      0xb90003ff, // str     wzr, [sp]
                  // some instructions skipped
      0x910003bf, // mov     sp, x29
      0xa8c17bfd, // ldp     x29, x30, [sp],#16
      0xd65f03c0, // ret
    };
    procedure_size = 9;

    c->dwarf.as_arg = &instructions;

    /* IP is pointing to start of procedure */
    c->dwarf.ip = (unw_word_t) (instructions);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction that stores FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpStp);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to instruction that updates the FP */
    c->dwarf.ip = (unw_word_t) (instructions+IpAdd);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_SP_OFFSET || fs.offset != -16) return -1;

    /* IP is pointing to instruction after FP was updated */
    c->dwarf.ip = (unw_word_t) (instructions+IpAdd+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction that retrieves FP and LR */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp);
    fs = get_frame_state(&cursor);
    if (fs.loc != AT_FP || fs.offset != 0) return -1;

    /* IP is pointing to instruction after FP and LR are retrieved */
    c->dwarf.ip = (unw_word_t) (instructions+IpLdp+1);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;
  }

  /* PLT_entry procedure */
  {
    // 0000000000417760 <fork@plt>:
    unsigned int instructions[4] = {
      0x900013d0, // adrp    x16, 68f000
      0xf942a211, // ldr     x17, [x16,#1344]
      0x91150210, // add     x16, x16, #0x540
      0xd61f0220, // br      x17
    };
    procedure_size = 4;

    c->dwarf.as_arg = &instructions;

    /* IP is pointing to start of procedure */
    c->dwarf.ip = (unw_word_t) (instructions);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to end of procedure */
    c->dwarf.ip = (unw_word_t) (instructions+procedure_size-1);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;
  }

  /* no_STP_MOV procedure */
  {
    // 000000000041ddf0 <init_bisect>:
    uint32_t instructions[9] = {
      0xb0001641, // adrp    x1, 6e6000 <ioctl_doc+0x550>
      0x90000d00, // adrp    x0, 5bd000 <_PyImport_StandardFiletab+0x1468>
      0x913d4023, // add     x3, x1, #0xf50
      0x911b4000, // add     x0, x0, #0x6d0
      0x91330062, // add     x2, x3, #0xcc0
      0x91374061, // add     x1, x3, #0xdd0
      0x52807ea4, // mov     w4, #0x3f5
      0xd2800003, // mov     x3, #0x0
      0x1400bccc, // b       44d140 <Py_InitModule4_64>
    };
    procedure_size = 9;

    c->dwarf.as_arg = &instructions;

    /* IP is pointing to start of procedure */
    c->dwarf.ip = (unw_word_t) (instructions);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;

    /* IP is pointing to end of procedure */
    c->dwarf.ip = (unw_word_t) (instructions+procedure_size-1);
    fs = get_frame_state(&cursor);
    if (fs.loc != NONE || fs.offset != 0) return -1;
  }

  return 0;
}
