// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __BREADCRUMBS_H__
#define __BREADCRUMBS_H__

#include <thread>

class breadcrumb_writer_t
{
public:
    breadcrumb_writer_t(bool enabled, const std::unordered_set<pal::string_t> &files);
    ~breadcrumb_writer_t();

    void begin_write();

private:
    void write_callback();
    bool end_write();
    static void write_worker_callback(breadcrumb_writer_t* p_this);

    pal::string_t m_breadcrumb_store;
    std::thread m_thread;
    const std::unordered_set<pal::string_t> &m_files;
    bool m_enabled;
    volatile bool m_status;
};

#endif // __BREADCRUMBS_H__
