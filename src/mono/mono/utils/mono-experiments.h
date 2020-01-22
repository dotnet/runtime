
#ifndef _MONO_EXPERIMENTS_H_
#define _MONO_EXPERIMENTS_H_

#include <glib.h>

#include "mono/utils/mono-lazy-init.h"


extern mono_lazy_init_t mono_experiments_enabled_init;

extern guint8 mono_experiments_enabled_table[];

void mono_experiments_initialize_table (void);

static inline gboolean
mono_experiment_enabled (int experiment_id) {
	mono_lazy_initialize (&mono_experiments_enabled_init, mono_experiments_initialize_table);
	return !!mono_experiments_enabled_table[experiment_id];
}

typedef enum MonoExperimentId {
#define EXPERIMENT(id,ghurl) MONO_EXPERIMENT_ ## id ,
#include "mono-experiments.def"
#undef EXPERIMENT
	MONO_EXPERIMENT_NUM_EXPERIMENTS
} MonoExperimentId;

#endif
