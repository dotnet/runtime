#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ep-rt-config.h"
#include "ep-types.h"
#include "ep-rt.h"

ep_rt_spin_lock_handle_t _ep_rt_mono_config_lock = {0};
EventPipeMonoFuncTable _ep_rt_mono_func_table = {0};

#endif /* ENABLE_PERFTRACING */

extern const char quiet_linker_empty_file_warning_eventpipe_rt_mono;
const char quiet_linker_empty_file_warning_eventpipe_rt_mono = 0;
