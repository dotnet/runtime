#ifndef _CRASHPROTECTION_HPP
#define _CRASHPROTECTION_HPP

#include <functional>

#ifdef HOST_WINDOWS

/* Placeholders for windows.  Use SEH to handle hardware faults, instead. */
class CrashProtection final
{
public:
    ~CrashProtection() = default;
    static bool Init();

private:
    CrashProtection() = default;
    CrashProtection(const CrashProtection&) = delete;
    CrashProtection& operator=(const CrashProtection&) = delete;
};

class CrashGuard final
{
public:
    explicit CrashGuard(std::function<bool()>)
    {
    }
    ~CrashGuard()
    {
    }
    CrashGuard(const CrashGuard&) = delete;
    CrashGuard& operator=(const CrashGuard&) = delete;
};

#else /* !HOST_WINDOWS*/

#include <memory>

#include <signal.h>
#include <pthread.h>

#include "pipechannel.hpp"
class CrashProtection final
{
public:
    ~CrashProtection() = default;

    static bool Init();

    struct Handler
    {
        using HandlerFuncArg = const void*;
        typedef bool(*HandlerFunc)(HandlerFuncArg arg);
        HandlerFunc Fn;
        HandlerFuncArg Arg;
        Handler(HandlerFunc fn, HandlerFuncArg arg) : Fn(fn), Arg(arg) {}
        Handler(const Handler& other) = default;
        Handler& operator=(const Handler& other) = default;

        bool operator()() const {
            return Fn(Arg);
        }
    };

private:
    class HandlerThread
    {
        PipeChannel::Reader m_trigger;
        PipeChannel::Writer m_completion;
        const Handler *volatile m_currentHandler; // TODO: atomic
    public:
        HandlerThread(PipeChannel::Reader trigger, PipeChannel::Writer completion) : m_trigger{std::move(trigger)}, m_completion{std::move(completion)}, m_currentHandler(nullptr)
        {
        }
        HandlerThread(const HandlerThread&) = delete;
        HandlerThread& operator=(const HandlerThread&) = delete;
        HandlerThread(HandlerThread&& other) : m_trigger{std::move(other.m_trigger) }, m_completion{std::move(other.m_completion)}, m_currentHandler{other.m_currentHandler}
        {
        }
        HandlerThread& operator=(HandlerThread&& other)
        {
            m_trigger = std::move(other.m_trigger);
            m_completion = std::move(other.m_completion);
            return *this;
        }

        const Handler* ExchangeHandler(const Handler* newHandler)
        {
            // TODO: atomic
            const Handler* oldHandler = m_currentHandler;
            m_currentHandler = newHandler;
            return oldHandler;
        }

        const Handler* LoadHandler() const
        {
            // TODO: atomic
            return m_currentHandler;
        }

        bool WaitForTrigger() const;

        bool OnTrigger(const Handler *handler) const;

        static void HandlerThreadMain(CrashProtection::HandlerThread* self);
    };

    static std::unique_ptr<CrashProtection> s_self;
    const PipeChannel::Writer m_pipe;
    const PipeChannel::Reader m_completion;
    pthread_t m_handlerThread;
    HandlerThread m_threadData;
    /*const*/struct sigaction m_prevSigAction;
    int m_dbgActiveCount;
public:
    CrashProtection(PipeChannel::Writer writer, PipeChannel::Reader completion, HandlerThread threadData)
    : m_pipe {std::move(writer)}, m_completion{std::move(completion)}, m_handlerThread{}, m_threadData{std::move(threadData)}, m_prevSigAction{}, m_dbgActiveCount{0}
    {}
    CrashProtection(const CrashProtection&) = delete;
    CrashProtection& operator=(const CrashProtection&) = delete;

private:
    bool InstallSegvHandler();

    bool ChainSignal(int signo, siginfo_t *siginfo, void* ucontext) const;

private:
    /* async signal context */
    static void OnSigSegv(int signo, siginfo_t *siginfo, void* ucontext);

    /* async signal context */
    bool CanHandleCrash() const;

    /* async signal context */
    bool HandleCrash() const;

    /* async signal context */
    bool SendRun() const;

    /* async signal context */
    void WaitForCompletion() const;

private:
    const Handler* OnActivate(const Handler *handler);

    void OnDeactivate(const Handler *restoreHandler);

private:
    static const Handler* Activate(const Handler *handler)
    {
        return s_self->OnActivate(handler);
    }
    static void Deactivate(const Handler *restoreHandler)
    {
        s_self->OnDeactivate(restoreHandler);
    }
    friend class CrashGuard;
};

class CrashGuard final
{
private:
    const CrashProtection::Handler* m_prev;
    CrashProtection::Handler m_cur;
public:
    explicit CrashGuard(CrashProtection::Handler::HandlerFunc fn, CrashProtection::Handler::HandlerFuncArg arg = nullptr) : m_prev{nullptr}, m_cur{CrashProtection::Handler(fn, arg)}
    {
        m_prev = CrashProtection::Activate(&m_cur);
    }

    explicit CrashGuard(std::function<bool()> fn) : m_prev{nullptr}, m_cur{CrashProtection::Handler{&HandlerFnAdapter, &fn}}
    {
        m_prev = CrashProtection::Activate(&m_cur);
    }

    ~CrashGuard()
    {
        CrashProtection::Deactivate(m_prev);
    }

    CrashGuard(const CrashGuard&) = delete;
    CrashGuard& operator=(const CrashGuard&) = delete;

private:
    static bool HandlerFnAdapter(const void *arg)
    {
        std::function<bool()>* fn = static_cast<std::function<bool()>*>(const_cast<void*>(arg));
        return (*fn)();
    }
};
#endif /*!HOST_WINDOWS*/

#endif /*_CRASHPROTECTION_HPP*/
