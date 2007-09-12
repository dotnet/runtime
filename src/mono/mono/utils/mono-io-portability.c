#include "config.h"

#include <glib.h>
#include <mono/io-layer/mono-mutex.h>
#include <mono/utils/mono-io-portability.h>

static mono_once_t options_once = MONO_ONCE_INIT;
int __mono_io_portability_helpers = PORTABILITY_UNKNOWN;

static void options_init (void)
{
        const gchar *env;
        
        __mono_io_portability_helpers = PORTABILITY_NONE;
        
        env = g_getenv ("MONO_IOMAP");
        if (env != NULL) {
                /* parse the environment setting and set up some vars
                 * here
                 */
                gchar **options = g_strsplit (env, ":", 0);
                int i;
                
                if (options == NULL) {
                        /* This shouldn't happen */
                        return;
                }
                
                for (i = 0; options[i] != NULL; i++) {
#ifdef DEBUG
                        g_message ("%s: Setting option [%s]", __func__,
                                   options[i]);
#endif
                        if (!strncasecmp (options[i], "drive", 5)) {
                                __mono_io_portability_helpers |= PORTABILITY_DRIVE;
                        } else if (!strncasecmp (options[i], "case", 4)) {
                                __mono_io_portability_helpers |= PORTABILITY_CASE;
                        } else if (!strncasecmp (options[i], "all", 3)) {
                                __mono_io_portability_helpers |= (PORTABILITY_DRIVE |
								  PORTABILITY_CASE);
                        }
                }
        }
}

void mono_portability_helpers_init ()
{
	mono_once (&options_once, options_init);
}

