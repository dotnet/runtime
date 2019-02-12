// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include <thread>
#include "pal.h"
#include "utils.h"
#include "trace.h"
#include "breadcrumbs.h"

breadcrumb_writer_t::breadcrumb_writer_t(bool enabled, const std::unordered_set<pal::string_t> &files)
    : m_status(false)
    , m_enabled(enabled)
    , m_files(files)
{
    if (enabled && !pal::get_default_breadcrumb_store(&m_breadcrumb_store))
    {
        m_breadcrumb_store.clear();
    }
}

breadcrumb_writer_t::~breadcrumb_writer_t()
{
    if (m_enabled)
    {
        end_write();
    }
}

// Begin breadcrumb writing: write synchronously or launch a
// thread to write breadcrumbs.
void breadcrumb_writer_t::begin_write()
{
    if (m_enabled)
    {
        trace::verbose(_X("--- Begin breadcrumb write"));
        if (m_breadcrumb_store.empty())
        {
            trace::verbose(_X("Breadcrumb store was not obtained... skipping write."));
            m_status = false;
            return;
        }

        trace::verbose(_X("Number of breadcrumb files to write is %d"), m_files.size());
        if (m_files.empty())
        {
            m_status = true;
            return;
        }
        m_thread = std::thread(write_worker_callback, this);
        trace::verbose(_X("Breadcrumbs will be written using a background thread"));
    }
}

// Write the breadcrumbs. This method should be called
// only from the background thread.
void breadcrumb_writer_t::write_callback()
{
    bool successful = true;
    for (const auto& file : m_files)
    {
        pal::string_t file_path = m_breadcrumb_store;
        pal::string_t file_name = _X("netcore,") + file;
        append_path(&file_path, file_name.c_str());
        if (!pal::file_exists(file_path))
        {
            if (!pal::touch_file(file_path))
            {
                successful = false;
            }
        }
    }
    // m_status should not be modified by anyone else.
    m_status = successful;
}

// ThreadProc for the background writer.
void breadcrumb_writer_t::write_worker_callback(breadcrumb_writer_t* p_this)
{
    try
    {
        trace::verbose(_X("Breadcrumb thread write callback..."));
        p_this->write_callback();
    }
    catch (...)
    {
        trace::warning(_X("An unexpected exception was thrown while leaving breadcrumbs"));
    }
}

// Wait for completion of the background tasks, if any.
bool breadcrumb_writer_t::end_write()
{
    if (m_thread.joinable())
    {
        trace::verbose(_X("Waiting for breadcrumb thread to exit..."));

        // Block on the thread to exit.
        m_thread.join();
    }
    trace::verbose(_X("--- End breadcrumb write %d"), m_status);
    return m_status;
}
