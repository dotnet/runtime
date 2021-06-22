// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// for now, specify alloc_dealloc_mismatch=0 as there are too many error reports that are not an issue.
// Also specify use_sigaltstack=0 as coreclr uses own alternate stack for signal handlers
const char *__asan_default_options() {
  return "symbolize=1:alloc_dealloc_mismatch=0:use_sigaltstack=0";
}
