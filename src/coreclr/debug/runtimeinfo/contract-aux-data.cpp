#include "common.h"

#include <stddef.h>
#include <stdint.h>

#include "threads.h"

extern "C"
{
  
extern const uintptr_t contractDescriptorAuxData[];

const uintptr_t contractDescriptorAuxData[] = {
    (uintptr_t)0, // placeholder
#define CDAC_BASELINE(name)
#define CDAC_TYPES_BEGIN()
#define CDAC_TYPE_BEGIN(name)
#define CDAC_TYPE_INDETERMINATE(name)
#define CDAC_TYPE_SIZE(size)
#define CDAC_TYPE_FIELD(tyname,membertyname,membername,offset)
#define CDAC_TYPE_END(name)
#define CDAC_TYPES_END()
#define CDAC_GLOBALS_BEGIN()
#define CDAC_GLOBAL_POINTER(name,value) (uintptr_t)(value),
#define CDAC_GLOBAL(name,tyname,value)
#define CDAC_GLOBALS_END()
#include "data-descriptor.h"
#undef CDAC_BASELINE
#undef CDAC_TYPES_BEGIN
#undef CDAC_TYPES_END
#undef CDAC_TYPE_BEGIN
#undef CDAC_TYPE_INDETERMINATE
#undef CDAC_TYPE_SIZE
#undef CDAC_TYPE_FIELD
#undef CDAC_TYPE_END
#undef DECL_LEN
#undef CDAC_GLOBALS_BEGIN
#undef CDAC_GLOBAL_POINTER
#undef CDAC_GLOBAL
#undef CDAC_GLOBALS_END
};

}
