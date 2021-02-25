// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <thread>
#include <pal.h>
#include <utils.h>
#include <trace.h>
#include "breadcrumbs.h"

breadcrumb_writer_t::breadcrumb_writer_t(std::unordered_set<pal::string_t> &files)
{
    assert(m_files.empty());
    m_files.swap(files);
    assert(files.empty());
    if (!pal::get_default_breadcrumb_store(&m_breadcrumb_store))
    {
        m_breadcrumb_store.clear();
    }
}

// Begin breadcrumb writing: launch a thread to write breadcrumbs.
std::shared_ptr<breadcrumb_writer_t> breadcrumb_writer_t::begin_write(std::unordered_set<pal::string_t> &files)
{
    trace::verbose(_X("--- Begin breadcrumb write"));

    auto instance = std::make_shared<breadcrumb_writer_t>(files);
    if (instance->m_breadcrumb_store.empty())
    {
        trace::verbose(_X("Breadcrumb store was not obtained... skipping write."));
        return nullptr;
    }

    // Add a reference to this object for the thread we will spawn
    instance->m_threads_instance = instance;

    instance->m_thread = std::thread(write_worker_callback, instance.get());
    trace::verbose(_X("Breadcrumbs will be written using a background thread"));

    return instance;
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
    trace::verbose(_X("--- End breadcrumb write %d"), successful);

    // Clear reference to this object for the thread.
    m_threads_instance.reset();
}

// ThreadProc for the background writer.
void breadcrumb_writer_t::write_worker_callback(breadcrumb_writer_t* p_this)
{
    assert(p_this);
    assert(p_this->m_threads_instance);
    assert(p_this->m_threads_instance.get() == p_this);
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
void breadcrumb_writer_t::end_write()
{
    if (m_thread.joinable())
    {
        trace::verbose(_X("Waiting for breadcrumb thread to exit..."));

        // Block on the thread to exit.
        m_thread.join();
    }
    trace::verbose(_X("Done waiting for breadcrumb thread to exit..."));
}
