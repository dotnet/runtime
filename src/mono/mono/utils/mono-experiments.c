
#include "config.h"
#include <glib.h>
#include "mono/utils/mono-lazy-init.h"
#include "mono/utils/mono-experiments.h"

mono_lazy_init_t mono_experiments_enabled_init;

guint8 mono_experiments_enabled_table[] = {
#define EXPERIMENT(id,ghurl) 0,
#include "mono-experiments.def"
#undef EXPERIMENT
};

static
const char* mono_experiment_names[] = {
#define EXPERIMENT(id,ghurl) #id,
#include "mono-experiments.def"
#undef EXPERIMENT
};

static int
lookup_experiment_by_name (const char *exp_name)
{
	/* slow loop, but we only do this once, on demand. */
	for (int i = 0; i < MONO_EXPERIMENT_NUM_EXPERIMENTS; i++) {
		if (!strcmp (mono_experiment_names[i], exp_name))
			return i;
	}
	return -1;
}

void
mono_experiments_initialize_table (void)
{
	char *str = g_getenv ("MONO_EXPERIMENT");
	if (str == NULL)
		return;
	char **experiments = g_strsplit (str, ",", 0);

	char **exp_name = &experiments[0];
	while (*exp_name) {
		int exp_id = lookup_experiment_by_name (*exp_name);
		if (exp_id < 0) {
			g_warning ("This version of Mono does not include experiment '%s'.  Experiments have no stability, backward compatability or deprecation guarantees.", *exp_name);
		} else {
			mono_experiments_enabled_table [exp_id] = 1;
		}
		exp_name++;
	}

	g_free (str);
	g_strfreev (experiments);
}
