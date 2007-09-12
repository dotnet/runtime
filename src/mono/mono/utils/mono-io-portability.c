#include "config.h"

#include <glib.h>
#include <mono/utils/mono-io-portability.h>

int __mono_io_portability_helpers = PORTABILITY_UNKNOWN;

void mono_portability_helpers_init (void)
{
        const gchar *env;

	if (__mono_io_portability_helpers != PORTABILITY_UNKNOWN)
		return;
	
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
