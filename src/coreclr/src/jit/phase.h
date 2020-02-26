// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/
#ifndef _PHASE_H_
#define _PHASE_H_

// A phase encapsulates a part of the compilation pipeline for a method.
//
class Phase
{
public:
    virtual void Run();

protected:
    Phase(Compiler* _compiler, Phases _phase) : comp(_compiler), m_name(nullptr), m_phase(_phase)
    {
        m_name = PhaseNames[_phase];
    }

    virtual void PrePhase();
    virtual void DoPhase() = 0;
    virtual void PostPhase();

    Compiler*   comp;
    const char* m_name;
    Phases      m_phase;
};

// A phase that accepts a lambda for the actions done by the phase.
//
template <typename A>
class ActionPhase final : public Phase
{
public:
    ActionPhase(Compiler* _compiler, Phases _phase, A _action) : Phase(_compiler, _phase), action(_action)
    {
    }

protected:
    virtual void DoPhase() override
    {
        action();
    }

private:
    A action;
};

// Wrapper for using ActionPhase
//
template <typename A>
void DoPhase(Compiler* _compiler, Phases _phase, A _action)
{
    ActionPhase<A> phase(_compiler, _phase, _action);
    phase.Run();
}

// A simple phase that just invokes a method on the compiler instance
//
class CompilerPhase final : public Phase
{
public:
    CompilerPhase(Compiler* _compiler, Phases _phase, void (Compiler::*_action)())
        : Phase(_compiler, _phase), action(_action)
    {
    }

protected:
    virtual void DoPhase() override
    {
        (comp->*action)();
    }

private:
    void (Compiler::*action)();
};

// Wrapper for using CompilePhase
//
inline void DoPhase(Compiler* _compiler, Phases _phase, void (Compiler::*_action)())
{
    CompilerPhase phase(_compiler, _phase, _action);
    phase.Run();
}

#endif /* End of _PHASE_H_ */
