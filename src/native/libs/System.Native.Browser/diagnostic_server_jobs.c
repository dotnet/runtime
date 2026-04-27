// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <stdlib.h>
#include <assert.h>
#include <emscripten.h>

#include "diagnostic_server_jobs.h"

extern void SystemJS_ScheduleDiagnosticServer(void);

typedef struct DsJobNode {
    ds_job_cb cb;
    void *data;
    struct DsJobNode *next;
} DsJobNode;

static DsJobNode *jobs;

void
SystemJS_DiagnosticServerQueueJob (ds_job_cb cb, void *data)
{
    int wasEmpty = jobs == NULL;
    assert (cb);
    DsJobNode *node = (DsJobNode *)calloc (1, sizeof (DsJobNode));
    if (!node) {
        abort ();
    }
    node->cb = cb;
    node->data = data;
    node->next = jobs;
    jobs = node;
    if (wasEmpty) {
        SystemJS_ScheduleDiagnosticServer ();
    }
}

void
SystemJS_ExecuteDiagnosticServerCallback (void)
{
    DsJobNode *list = jobs;
    jobs = NULL;

    while (list) {
        DsJobNode *cur = list;
        list = cur->next;
        assert (cur->cb);
        size_t done = cur->cb (cur->data);
        if (done) {
            free (cur);
        } else {
            cur->next = jobs;
            jobs = cur;
        }
    }

    if (jobs) {
        SystemJS_ScheduleDiagnosticServer ();
    }
}
