
#ifndef HOST_WINDOWS

#include <cstddef>
#include <memory>
#include <utility>
#include <signal.h>
#include <string.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <pthread.h>
#include <errno.h>
#include <unistd.h>

#include "crashprotection.hpp"

std::unique_ptr<CrashProtection> CrashProtection::s_self;

#define LOG(msg) fprintf (stderr, msg "\n")
#define LOGV(fmt, ...) fprintf(stderr, fmt "\n", __VA_ARGS__)

namespace
{
    int async_safe_vfprintf (int handle, char const *format, va_list args)
    {
        char print_buff [1024];
        print_buff [0] = '\0';
        vsnprintf(print_buff, sizeof(print_buff), format, args);
        int ret = write(handle, print_buff, (uint32_t) strlen (print_buff));

        return ret;
    }

    int async_safe_vprintf (char const *format, va_list args)
    {
        return async_safe_vfprintf(1, format, args);
    }

    int async_safe_printf (char const *format, ...)
    {
        va_list args;
        va_start(args, format);
        int ret = async_safe_vfprintf(1, format, args);
        va_end(args);

        return ret;
    }
}

#define ASYNC_LOG(msg) async_safe_printf(msg "\n")
#define ASYNC_LOGV(fmt, ...) async_safe_printf(msg "\n", __VA_ARGS__)

namespace
{
    template<typename TPayload>
    class SyncThreadStarter
    {
    public:
        typedef void (*ThreadFunc)(TPayload payload);

    private:
        mutable TPayload m_payload;
        ThreadFunc m_func;
        pthread_mutex_t m_mutex;
        pthread_cond_t m_signal_started;
        mutable bool m_started;

    public:

        SyncThreadStarter(ThreadFunc func, TPayload&& payload) : m_payload{std::move(payload)}, m_func{func}, m_mutex{}, m_signal_started{}, m_started{false}
        {
            int err;
            if ((err = pthread_mutex_init (&m_mutex, nullptr)) != 0)
            {
                fprintf(stderr, "pthread_mutex_init failed: %s\n", strerror(err));
                abort();
            }
            if ((err = pthread_cond_init(&m_signal_started, nullptr)) != 0)
            {
                fprintf(stderr, "pthread_cond_init failed: %s\n", strerror(err));
                abort();
            }
        }

        ~SyncThreadStarter()
        {
            pthread_cond_destroy(&m_signal_started);
            pthread_mutex_destroy(&m_mutex);
        }

        SyncThreadStarter(const SyncThreadStarter&) = delete;
        SyncThreadStarter& operator=(const SyncThreadStarter&) = delete;

        bool Start(pthread_t* thread) const
        {
            Lock lock{*this};
            if (pthread_create (thread, nullptr, &CoordinatedStart, const_cast<void*>(static_cast<const void*>(this))) != 0)
            {
                perror ("pthread_create failed");
                return false;
            }
            WaitForStart();
            return true;
        }

    private:
        class Lock
        {
        private:
            pthread_mutex_t *m_mutex;
        public:
            Lock(const SyncThreadStarter<TPayload>& threadArg) : m_mutex(const_cast<pthread_mutex_t*>(&threadArg.m_mutex))
            {
                pthread_mutex_lock (m_mutex);
            }

            ~Lock()
            {
                pthread_mutex_unlock (m_mutex);
            }

            Lock(const Lock&) = delete;
            Lock& operator=(const Lock&) = delete;
        };

        static void *CoordinatedStart(void *thread_arg)
        {
            const SyncThreadStarter<TPayload> *self = reinterpret_cast<const SyncThreadStarter<TPayload> *>(thread_arg);
            typename SyncThreadStarter<TPayload>::ThreadFunc fn{self->m_func};
            TPayload payload{std::move(self->Extract())};
            {
                Lock lock{*self};
                self->SignalStarted();
            }
            fn(std::move(payload));
            return nullptr;
        }

        void WaitForStart() const
        {
            while (!m_started) {
                pthread_cond_wait (const_cast<pthread_cond_t*>(&m_signal_started), const_cast<pthread_mutex_t*>(&m_mutex));
            }
        }

        void SignalStarted() const
        {
            m_started = true;
            pthread_cond_signal (const_cast<pthread_cond_t*>(&m_signal_started));
        }

        TPayload&& Extract() const { return std::move(m_payload); }
    };

    template<typename TPayload> bool StartThread(typename SyncThreadStarter<TPayload>::ThreadFunc fn, TPayload&& arg, pthread_t *out_thread)
    {
        SyncThreadStarter<TPayload> starter {fn, std::forward<TPayload>(arg)};
        pthread_t thread;
        if (!starter.Start(&thread))
        {
            return false;
        }
        if (out_thread)
        {
            *out_thread = thread;
        }
        return true;
    }
}


bool
CrashProtection::Init()
{
    PipeChannel commands{PipeChannel::Create()};
    PipeChannel completion{PipeChannel::Create()};
    CrashProtection::HandlerThread threadData{commands.ExtractReader(), completion.ExtractWriter()};

    s_self = std::unique_ptr<CrashProtection>(new CrashProtection{commands.ExtractWriter(), completion.ExtractReader(), std::move(threadData)});

    if (!StartThread(&HandlerThread::HandlerThreadMain, &s_self->m_threadData, &s_self->m_handlerThread))
    {
        return false;
    }
    pthread_detach (s_self->m_handlerThread);

    if (!s_self->InstallSegvHandler())
    {
        return false;
    }
    return true;
}

bool
CrashProtection::InstallSegvHandler()
{
    struct sigaction sa{};
    sa.sa_sigaction = &CrashProtection::OnSigSegv;
    sa.sa_flags = SA_SIGINFO;
    if (sigaction(SIGSEGV, const_cast<const struct sigaction*>(&sa), &m_prevSigAction) != 0)
    {
        perror("sigaction failed");
        return false;
    }
    return true;
}

bool CrashProtection::ChainSignal(int signo, siginfo_t *siginfo, void* ucontext) const
{
    // FIXME: see PAL's signal.cpp IsSigDfl - this is not correct
    if (m_prevSigAction.sa_handler == SIG_DFL || m_prevSigAction.sa_handler == SIG_IGN)
    {
        ASYNC_LOG("default or ignored handler");
        return false;
    }
    if ((m_prevSigAction.sa_flags & SA_SIGINFO) != 0)
    {
        ASYNC_LOG("chained sigaction signal");
        m_prevSigAction.sa_sigaction (signo, siginfo, ucontext);
        return true;
    }
    else
    {
        ASYNC_LOG("chained handler signal");
        m_prevSigAction.sa_handler (signo);
        return true;
    }
}

void CrashProtection::OnSigSegv(int signo, siginfo_t *siginfo, void *ucontext)
{
    ASYNC_LOG("caught signal");
    if (s_self->CanHandleCrash())
    {
        ASYNC_LOG("handling signal");
        bool success = s_self->HandleCrash();
        if (success)
        {
            ASYNC_LOG("signal handling finished, aborting");
        }
        else
        {
            ASYNC_LOG("signal handling failing, aborting");
        }
        abort();
    } else {
        ASYNC_LOG("chaining signal");
        bool result = s_self->ChainSignal (signo, siginfo, ucontext);
        if (!result)
        {
            ASYNC_LOG("default or ignored signal handler, aborting");
        }
        else
        {
            ASYNC_LOG("unexpected: signal chaining returned");
        }
        abort();
    }
}

bool CrashProtection::CanHandleCrash() const
{
    if (pthread_equal (pthread_self(), s_self->m_handlerThread))
    {
        // can't handle crashes on the handler thread
        return false;
    }
    if (!s_self->m_threadData.LoadHandler())
    {
       // no handler to run
        return false;
    }
    return true;
}

bool CrashProtection::HandleCrash() const
{
    if (!SendRun())
    {
        return false;
    }
    WaitForCompletion();
    return true;
}

bool CrashProtection::SendRun() const
{
    return 1 == m_pipe.SendOne (1);
}

void CrashProtection::WaitForCompletion() const
{
    char dk;
    m_completion.ReadOne(dk);
}

bool CrashProtection::HandlerThread::OnTrigger(const Handler *handler) const
{
    if (handler)
    {
        bool result = (*handler)();
        m_completion.SendOne(1);
        return result;
    }
    else
        return false;
}

bool CrashProtection::HandlerThread::WaitForTrigger() const
{
    char cmd;
    int nread = 0;

    LOG("handler thread: waiting for trigger");
    if ((nread = m_trigger.ReadOne (cmd)) < 0)
    {
        // EINTR handled by pipe
        perror ("read failed in HandlerThreadMain");
        abort();
    }
    else
    {
        return nread != 0;
    }
}

void CrashProtection::HandlerThread::HandlerThreadMain(CrashProtection::HandlerThread *self)
{
    bool running = true;
    LOG("handler thread: started");
    do
    {
        if (!self->WaitForTrigger())
        {
            running = false;
        }
        else
        {
            LOG("handler thread: received trigger");
            running = !self->OnTrigger(self->LoadHandler());
            LOG("handler thread: OnTrigger done");
        }
    }
    while (running);
    LOG("handler thread: terminating");
}

const CrashProtection::Handler* CrashProtection::OnActivate(const Handler *handler)
{
    LOGV("activated, count is %d", ++m_dbgActiveCount);
    return m_threadData.ExchangeHandler(handler);
}

void CrashProtection::OnDeactivate(const Handler *restoreHandler)
{
    m_threadData.ExchangeHandler(restoreHandler);
    LOGV("deactivated, count is %d", --m_dbgActiveCount);
}

#else /* HOST_WINDOWS*/
void CrashProtection::Init()
{
    // Nothing to do on Windows
}
#endif
