//
//  btls-error.c
//  MonoBtls
//
//  Created by Martin Baulig on 6/19/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include <btls-error.h>
#include <btls-util.h>
#include <assert.h>

MONO_API int
mono_btls_error_peek_error (void)
{
	return ERR_peek_error ();
}

MONO_API int
mono_btls_error_get_error (void)
{
	return ERR_get_error ();
}

MONO_API int
mono_btls_error_peek_error_line (const char **file, int *line)
{
	return ERR_peek_error_line (file, line);
}

MONO_API int
mono_btls_error_get_error_line (const char **file, int *line)
{
	return ERR_get_error_line (file, line);
}

MONO_API void
mono_btls_error_clear_error (void)
{
	ERR_clear_error ();
}

MONO_API void
mono_btls_error_get_error_string_n (int error, char *buf, int len)
{
	ERR_error_string_n (error, buf, len);
}

