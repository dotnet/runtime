/*
 * Unittest PPC64 is_plt_entry function by inspecting output at
 * different points in a mock PLT address space.
 */

#include "dwarf.h"
#include "libunwind_i.h"

#undef unw_get_accessors_int
unw_accessors_t *unw_get_accessors_int (unw_addr_space_t) { return NULL; }
int dwarf_step (struct dwarf_cursor*) { return 0; }
#include "ppc64/Gstep.c"

enum
{
  ip_guard0,
  ip_std,
  ip_ld,
  ip_mtctr,
  ip_bctr,
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

  const uint32_t plt_instructions[ip_program_end] =
    {
      0xdeadbeef,
      0xf8410018, // std     r2,24(r1)
      0xe9828730, // ld      r12,-30928(r2)
      0x7d8903a6, // mtctr   r12
      0x4e800420, // bctr
      0xdeadbeef,
    };
  uint32_t test_instructions[ip_program_end];
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  struct unw_addr_space mock_address_space;
  mock_address_space.big_endian = 0;
  mock_address_space.acc.access_mem = &access_mem;

  struct dwarf_cursor c;
  c.as = &mock_address_space;
  c.as_arg = &test_instructions;

  /* ip at std r2,24(r1) */
  c.ip = (unw_word_t) (test_instructions + ip_std);
  if (!is_plt_entry(&c)) return -1;

  /* ld uses a different offset */
  test_instructions[ip_ld] = 0xe9820000;
  if (!is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_ld is not a ld instruction */
  test_instructions[ip_ld] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_mtctr is not a mtctr instruction */
  test_instructions[ip_mtctr] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_bctr is not a bctr instruction */
  test_instructions[ip_bctr] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip at ld r12,-30928(r2) */
  c.ip = (unw_word_t) (test_instructions + ip_ld);
  if (!is_plt_entry(&c)) return -1;

  /* ip_std is not a std instruction */
  test_instructions[ip_std] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_mtctr is not a mtctr instruction */
  test_instructions[ip_mtctr] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_bctr is not a bctr instruction */
  test_instructions[ip_bctr] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip at mtctr r12 */
  c.ip = (unw_word_t) (test_instructions + ip_mtctr);
  if (!is_plt_entry(&c)) return -1;

  /* ip_std is not a std instruction */
  test_instructions[ip_std] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_ld is not a ld instruction */
  test_instructions[ip_ld] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_bctr is not a bctr instruction */
  test_instructions[ip_bctr] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip at bctr */
  c.ip = (unw_word_t) (test_instructions + ip_bctr);
  if (!is_plt_entry(&c)) return -1;

  /* ip_std is not a std instruction */
  test_instructions[ip_std] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_ld is not a ld instruction */
  test_instructions[ip_ld] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip_mtctr is not a mtctr instruction */
  test_instructions[ip_mtctr] = 0xf154f00d;
  if (is_plt_entry(&c)) return -1;
  memcpy(test_instructions, plt_instructions, sizeof(test_instructions));

  /* ip at non-PLT instruction */
  c.ip = (unw_word_t) (test_instructions + ip_guard0);
  if (is_plt_entry(&c)) return -1;

  /* ip at another non-PLT instruction */
  c.ip = (unw_word_t) (test_instructions + ip_guard1);
  if (is_plt_entry(&c)) return -1;

  return 0;
}
