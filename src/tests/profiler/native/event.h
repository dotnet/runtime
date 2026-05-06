// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <mutex>
#include <condition_variable>
#include <functional>

class AutoEvent
{
private:
    std::mutex m_mtx;
    std::condition_variable m_cv;
    bool m_set = false;

    static void DoNothing()
    {

    }

public:
    AutoEvent() = default;
    ~AutoEvent() = default;
    AutoEvent(AutoEvent& other) = delete;
    AutoEvent(AutoEvent &&other) = delete;
    AutoEvent &operator=(AutoEvent &other) = delete;
    AutoEvent &operator=(AutoEvent &&other) = delete;

    void Wait(std::function<void()> spuriousCallback = DoNothing)
    {
        std::unique_lock<std::mutex> lock(m_mtx);
        while (!m_set)
        {
            m_cv.wait(lock, [&]() { return m_set; });
            if (!m_set)
            {
                spuriousCallback();
            }
        }
        m_set = false;
    }

    void WaitFor(int milliseconds, std::function<void()> spuriousCallback = DoNothing)
    {
        std::unique_lock<std::mutex> lock(m_mtx);
        while (!m_set)
        {
            m_cv.wait_for(lock, std::chrono::milliseconds(milliseconds), [&]() { return m_set; });
            if (!m_set)
            {
                spuriousCallback();
            }
        }
        m_set = false;
    }

    void Signal()
    {
        {
            std::lock_guard<std::mutex> guard(m_mtx);
            m_set = true;
        }

        m_cv.notify_one();
    }
};

class ManualEvent
{
private:
    std::mutex m_mtx;
    std::condition_variable m_cv;
    bool m_set = false;

    static void DoNothing()
    {

    }

public:
    ManualEvent() = default;
    ~ManualEvent() = default;
    ManualEvent(ManualEvent& other) = delete;
    ManualEvent(ManualEvent&& other) = delete;
    ManualEvent& operator= (ManualEvent& other) = delete;
    ManualEvent& operator= (ManualEvent&& other) = delete;

    void Wait(std::function<void()> spuriousCallback = DoNothing)
    {
        std::unique_lock<std::mutex> lock(m_mtx);
        while (!m_set)
        {
            m_cv.wait(lock, [&]() { return m_set; });
            if (!m_set)
            {
                spuriousCallback();
            }
        }
    }

    void Signal()
    {
        {
            std::lock_guard<std::mutex> guard(m_mtx);
            m_set = true;
        }

        m_cv.notify_all();
    }

    void Reset()
    {
        std::lock_guard<std::mutex> guard(m_mtx);
        m_set = false;
    }
};
