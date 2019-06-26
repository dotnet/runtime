//
//  btls-util.h
//  MonoBtls
//
//  Created by Martin Baulig on 3/23/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#ifndef __btls__btls_error__
#define __btls__btls_error__

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <openssl/ssl.h>
#include "btls-util.h"

MONO_API int
mono_btls_error_peek_error (void);

MONO_API int
mono_btls_error_get_error (void);

MONO_API void
mono_btls_error_clear_error (void);

MONO_API int
mono_btls_error_peek_error_line (const char **file, int *line);

MONO_API int
mono_btls_error_get_error_line (const char **file, int *line);

MONO_API void
mono_btls_error_get_error_string_n (int error, char *buf, int len);

MONO_API int
mono_btls_error_get_reason (int error);

#endif /* __btls__btls_error__ */
