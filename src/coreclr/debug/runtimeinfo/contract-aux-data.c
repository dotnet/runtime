#include <stddef.h>
#include <stdint.h>

extern const uintptr_t contractDescriptorAuxData[];

static int foo;

const uintptr_t contractDescriptorAuxData[] = { 1+(uintptr_t)&foo, (uintptr_t)NULL };
