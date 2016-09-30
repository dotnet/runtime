//
//  btls-error.c
//  MonoBtls
//
//  Created by Martin Baulig on 6/19/16.
//  Copyright © 2016 Xamarin. All rights reserved.
//

#include <btls-error.h>
#include <assert.h>

int
mono_btls_error_peek_error (void)
{
	return ERR_peek_error ();
}

int
mono_btls_error_get_error (void)
{
	return ERR_get_error ();
}

void
mono_btls_error_clear_error (void)
{
	ERR_clear_error ();
}

void
mono_btls_error_get_error_string_n (int error, char *buf, int len)
{
	ERR_error_string_n (error, buf, len);
}

