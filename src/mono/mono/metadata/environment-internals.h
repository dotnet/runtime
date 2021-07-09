/**
 * \file
 *
 * Copyright 2020 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef _MONO_METADATA_ENVIRONMENT_INTERNALS_H_
#define _MONO_METADATA_ENVIRONMENT_INTERNALS_H_

#include <mono/utils/mono-compiler.h>

void
mono_set_os_args (int argc, char **argv);

MONO_COMPONENT_API char *
mono_get_os_cmd_line (void);

#endif /* _MONO_METADATA_ENVIRONMENT_INTERNALS_H_ */
