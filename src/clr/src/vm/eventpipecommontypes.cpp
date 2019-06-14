// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "eventpipecommontypes.h"

#ifdef FEATURE_PERFTRACING

void EventPipeProviderCallbackDataQueue::Enqueue(EventPipeProviderCallbackData *pEventPipeProviderCallbackData)
{
    SListElem<EventPipeProviderCallbackData> *listnode = new SListElem<EventPipeProviderCallbackData>(); // throws
    listnode->m_Value = *pEventPipeProviderCallbackData;
    list.InsertTail(listnode);
}

bool EventPipeProviderCallbackDataQueue::TryDequeue(EventPipeProviderCallbackData *pEventPipeProviderCallbackData)
{
    if (list.IsEmpty())
        return false;

    SListElem<EventPipeProviderCallbackData> *listnode = list.RemoveHead();
    *pEventPipeProviderCallbackData = listnode->m_Value;
    delete listnode;
    return true;
}

#endif // FEATURE_PERFTRACING
