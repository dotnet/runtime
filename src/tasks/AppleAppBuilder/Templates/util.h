// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef util_h
#define util_h

// XHarness is looking for this tag in app's output to determine the exit code
#define EXIT_CODE_TAG "DOTNET.APP_EXIT_CODE"

size_t get_managed_args (char*** managed_args_array);
void free_managed_args (char*** managed_args_array, size_t array_size);

#endif /* util_h */
