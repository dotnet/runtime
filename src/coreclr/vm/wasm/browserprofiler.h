// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef BROWSERPROFILER_H
#define BROWSERPROFILER_H

#ifdef TARGET_BROWSER

// Browser DevTools profiler for CoreCLR interpreter on WASM.
// Records method enter/leave events to the browser's Performance tab
// via performance.measure(). Uses a shadow stack to track method timing.

void BrowserProfiler_OnMethodEnter(void *pMethodDesc);
void BrowserProfiler_OnMethodLeave(void *pMethodDesc);

#endif // TARGET_BROWSER

#endif // BROWSERPROFILER_H
