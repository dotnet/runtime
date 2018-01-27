// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#ifndef KNOWN_EVENT
 #define KNOWN_EVENT(name, provider, level, keyword)
#endif // KNOWN_EVENT

#ifndef DYNAMIC_EVENT
 #define DYNAMIC_EVENT(name, level, keyword, ...)
#endif // DYNAMIC_EVENT

KNOWN_EVENT(SetGCHandle, GCEventProvider_Default, GCEventLevel_Information, GCEventKeyword_GCHandle)
KNOWN_EVENT(DestroyGCHandle, GCEventProvider_Default, GCEventLevel_Information, GCEventKeyword_GCHandle)
KNOWN_EVENT(GCPerHeapHistory_V3, GCEventProvider_Default, GCEventLevel_Information, GCEventKeyword_GCPrivate)

KNOWN_EVENT(BGCBegin, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGC1stNonConEnd, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGC1stConEnd, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGC2ndNonConBegin, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGC2ndNonConEnd, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGC2ndConBegin, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGC2ndConEnd, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGCDrainMark, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGCRevisit, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGCOverflow, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGCAllocWaitBegin, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(BGCAllocWaitEnd, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(GCFullNotify_V1, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCPrivate)
KNOWN_EVENT(PrvSetGCHandle, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCHandlePrivate)
KNOWN_EVENT(PrvDestroyGCHandle, GCEventProvider_Private, GCEventLevel_Information, GCEventKeyword_GCHandlePrivate)

#undef KNOWN_EVENT
#undef DYNAMIC_EVENT
