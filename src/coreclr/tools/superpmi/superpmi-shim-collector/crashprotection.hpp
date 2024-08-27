#ifndef _CRASHPROTECTION_HPP
#define _CRASHPROTECTION_HPP

#include <memory>

#include <signal.h>
#include <pthread.h>

#include "pipechannel.hpp"

class CrashProtection final
{
public:
	struct Handler {
		using HandlerFuncArg = const void*;
		typedef bool(*HandlerFunc)(HandlerFuncArg arg);
		HandlerFunc Fn;
		HandlerFuncArg Arg;
		Handler(HandlerFunc fn, HandlerFuncArg arg) : Fn(fn), Arg(arg) {}
		Handler(const Handler& other) = default;
		Handler& operator=(const Handler& other) = default;

		bool Run() const {
			return Fn(Arg);
		}
	};

private:
	class HandlerThread {
		PipeChannel::Reader m_commands;
		PipeChannel::Writer m_completion;
		const Handler *volatile m_currentHandler; // TODO: atomic
	public:
		HandlerThread(PipeChannel::Reader commands, PipeChannel::Writer completion) : m_commands{std::move(commands)}, m_completion{std::move(completion)}, m_currentHandler(nullptr) {}
		HandlerThread(const HandlerThread&) = delete;
		HandlerThread& operator=(const HandlerThread&) = delete;
		HandlerThread(HandlerThread&& other) : m_commands{std::move(other.m_commands) }, m_completion{std::move(other.m_completion)}, m_currentHandler{other.m_currentHandler} {}
		HandlerThread& operator=(HandlerThread&& other)
		{
			this->m_commands = std::move(other.m_commands);
			this->m_completion = std::move(other.m_completion);
			return *this;
		}

		const Handler* ExchangeHandler(const Handler* newHandler) {
			// TODO: atomic
			const Handler* oldHandler = m_currentHandler;
			m_currentHandler = newHandler;
			return oldHandler;
		}

		const Handler* LoadHandler() const {
			// TODO: atomic
			return m_currentHandler;
		}

		const PipeChannel::Reader& Commands() const {
			return m_commands;
		}

		enum class Command : int {
			Run = 'R',
		};

		bool OnRun(const Handler *handler);

		void SignalComplection();

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
	: m_pipe {std::move(writer)}, m_completion{std::move(completion)}, m_handlerThread{0,}, m_threadData{std::move(threadData)}, m_prevSigAction{0,}, m_dbgActiveCount{0}
	{}
	CrashProtection(const CrashProtection&) = delete;
	CrashProtection& operator=(const CrashProtection&) = delete;

public:
	~CrashProtection()
	{
	}

	static bool Init();

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
	void WaitForCompletion() const;
	
	/* async signal context */
	bool SendRun() const;

private:
	void OnActivate(const Handler *handler, const Handler * *prevHandler);

	void OnDeactivate(const Handler *restoreHandler);

private:
	static void Activate(const Handler *handler, const Handler * *prevHandler)
	{
		s_self->OnActivate(handler, prevHandler);
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
	const CrashProtection::Handler * m_prev;
	CrashProtection::Handler m_cur;
public:
	explicit CrashGuard(CrashProtection::Handler::HandlerFunc fn, CrashProtection::Handler::HandlerFuncArg arg = nullptr) : m_cur{CrashProtection::Handler(fn, arg)}, m_prev(nullptr)
	{
		CrashProtection::Activate(&m_cur, &m_prev);
	}

	~CrashGuard()
	{
		CrashProtection::Deactivate(m_prev);
	}

	CrashGuard(const CrashGuard&) = delete;
	CrashGuard& operator=(const CrashGuard&) = delete;
};

#endif /*_CRASHPROTECTION_HPP*/
