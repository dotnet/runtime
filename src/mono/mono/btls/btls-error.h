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

int
mono_btls_error_peek_error (void);

int
mono_btls_error_get_error (void);

void
mono_btls_error_clear_error (void);

int
mono_btls_error_peek_error_line (const char **file, int *line);

int
mono_btls_error_get_error_line (const char **file, int *line);

void
mono_btls_error_get_error_string_n (int error, char *buf, int len);

#endif /* __btls__btls_error__ */
