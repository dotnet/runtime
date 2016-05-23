// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __BREADCRUMBS_H__
#define __BREADCRUMBS_H__

#include <thread>

class breadcrumb_writer_t
{
public:
    breadcrumb_writer_t(const std::unordered_set<pal::string_t>* files);

    void begin_write();
    bool end_write();

private:
    void write_callback();
    static void write_worker_callback(breadcrumb_writer_t* p_this);

    pal::string_t m_breadcrumb_store;
    std::thread m_thread;
    const std::unordered_set<pal::string_t>* m_files;
    volatile bool m_status;
};

#endif // __BREADCRUMBS_H__
