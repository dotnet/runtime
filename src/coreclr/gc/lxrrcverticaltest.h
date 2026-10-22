// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_RC_VERTICAL_TEST_H
#define LXR_RC_VERTICAL_TEST_H

void LxrRcVerticalObserveWrite(Object** destination, Object* oldReference);
void LxrRcVerticalOnThreadDetach(gc_alloc_context* context);
bool LxrRcVerticalUnlogObservedSlot(Object** destination);

#endif // LXR_RC_VERTICAL_TEST_H
