// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __BREADCRUMBS_H__
#define __BREADCRUMBS_H__

#include <thread>

class breadcrumb_writer_t
{
public:
    breadcrumb_writer_t(std::unordered_set<pal::string_t> &files);

    // Starts writing breadcrumbs on a new thread if necessary.
    // If end_write is not called on the returned instance before it is destructed, the process will be terminated.
    static std::shared_ptr<breadcrumb_writer_t> begin_write(std::unordered_set<pal::string_t> &files);

    // Waits for the breadcrumb thread to finish writing.
    void end_write();

private:
    void write_callback();
    static void write_worker_callback(breadcrumb_writer_t* p_this);

    std::shared_ptr<breadcrumb_writer_t> m_threads_instance;
    pal::string_t m_breadcrumb_store;
    std::thread m_thread;
    std::unordered_set<pal::string_t> m_files;
};

#endif // __BREADCRUMBS_H__
