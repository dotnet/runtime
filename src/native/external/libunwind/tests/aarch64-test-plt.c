/*
 * Unittest AArch64 is_plt_entry function by inspecting output at
 * different points in a mock PLT address space.
 */

#include "dwarf.h"
#include "libunwind_i.h"

int unw_is_signal_frame (unw_cursor_t *cursor) { return 0; }
int dwarf_step (struct dwarf_cursor *c) { return 0; }
#include "aarch64/Gstep.c"

enum
{
  ip_guard0,
  ip_adrp,
  ip_ldr,
  ip_add,
  ip_br,
  ip_guard1,

  ip_program_end
};

/* Mock access_mem implementation */
static int
access_mem (unw_addr_space_t as, unw_word_t addr, unw_word_t *val, int write,
            void *arg)
{
  if (write != 0)
    return -1;

  const size_t mem_size   = ip_program_end * sizeof(uint32_t);
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

int
main ()
{
  if (target_is_big_endian())
    return 77;

  const uint32_t plt_instructions[ip_program_end] = {
    0xDEADBEEF,
    0xf0000990, // adrp    x16, 540000
    0xf9400a11, // ldr     x17, [x16,#16]
    0x91004210, // add     x16, x16, #0x10
    0xd61f0220, // br      x17
    0xDEADBEEF,
  };

  uint32_t test_instructions[ip_program_end];
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  struct unw_addr_space mock_address_space;
  mock_address_space.big_endian = 0;
  mock_address_space.acc.access_mem = &access_mem;

  struct dwarf_cursor c;
  c.as = &mock_address_space;
  c.as_arg = &test_instructions;

  /* ip at adrp */
  c.ip = (unw_word_t) (test_instructions + ip_adrp);
  if (is_plt_entry(&c) == 0) return -1;

  /* adrp uses different offset */
  test_instructions[ip_adrp] = 0x90272990;
  if (is_plt_entry(&c) == 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ldr uses different offset */
  test_instructions[ip_ldr] = 0xf948be11;
  if (is_plt_entry(&c) == 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* add uses different offset */
  test_instructions[ip_add] = 0x91726210;
  if (is_plt_entry(&c) == 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_ldr is not a ldr instruction */
  test_instructions[ip_ldr] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_add is not an add instruction */
  test_instructions[ip_add] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_br is not a br instruction */
  test_instructions[ip_br] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip at ldr */
  c.ip = (unw_word_t) (test_instructions + ip_ldr);
  if (is_plt_entry(&c) == 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_adrp is not an adrp instruction */
  test_instructions[ip_adrp] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_add is not an add instruction */
  test_instructions[ip_add] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_br is not a br instruction */
  test_instructions[ip_br] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip at add */
  c.ip = (unw_word_t) (test_instructions + ip_add);
  if (is_plt_entry(&c) == 0) return -1;

  /* ip_adrp is not an adrp instruction */
  test_instructions[ip_adrp] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_ldr is not a ldr instruction */
  test_instructions[ip_ldr] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_br is not a br instruction */
  test_instructions[ip_br] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip at br */
  c.ip = (unw_word_t) (test_instructions + ip_br);
  if (is_plt_entry(&c) == 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_adrp is not an adrp instruction */
  test_instructions[ip_adrp] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_ldr is not a ldr instruction */
  test_instructions[ip_ldr] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_add is not an add instruction */
  test_instructions[ip_add] = 0xf154f00d;
  if (is_plt_entry(&c) != 0) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip at non-PLT instruction */
  c.ip = (unw_word_t) (test_instructions + ip_guard0);
  if (is_plt_entry(&c)) return -1;

  /* ip at another non-PLT instruction */
  c.ip = (unw_word_t) (test_instructions + ip_guard1);
  if (is_plt_entry(&c)) return -1;

  return 0;
}
