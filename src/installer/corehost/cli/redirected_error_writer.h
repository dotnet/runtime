// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _COREHOST_CLI_REDIRECTED_ERROR_WRITER_H_
#define _COREHOST_CLI_REDIRECTED_ERROR_WRITER_H_

#include <pal.h>

void reset_redirected_error_writer();

void redirected_error_writer(const pal::char_t* msg);

pal::string_t get_redirected_error_string();

#endif /* _COREHOST_CLI_REDIRECTED_ERROR_WRITER_H_ */