// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef _PHASE_H_
#define _PHASE_H_

// A phase encapsulates a part of the compilation pipeline for a method.
//
class Phase
{
    // Observations made before a phase runs that should still
    // be true afterwards,if the phase status is MODIFIED_NOTHING.
    class Observations
    {
    public:
        Observations(Compiler* compiler);
        void Check(PhaseStatus status);

    private:
#ifdef DEBUG
        Compiler* m_compiler;
        unsigned  m_fgBBcount;
        unsigned  m_fgBBNumMax;
        unsigned  m_compHndBBtabCount;
        unsigned  m_lvaCount;
        unsigned  m_compGenTreeID;
        unsigned  m_compStatementID;
        unsigned  m_compBasicBlockID;
#endif // DEBUG
    };

public:
    virtual void Run();

protected:
    Phase(Compiler* _compiler, Phases _phase) : comp(_compiler), m_name(nullptr), m_phase(_phase)
    {
        m_name = PhaseNames[_phase];
    }

    virtual void        PrePhase();
    virtual PhaseStatus DoPhase() = 0;
    virtual void PostPhase(PhaseStatus status);

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
    virtual PhaseStatus DoPhase() override
    {
        action();
        return PhaseStatus::MODIFIED_EVERYTHING;
    }

private:
    A action;
};

// Wrappers for using ActionPhase
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
    virtual PhaseStatus DoPhase() override
    {
        (comp->*action)();
        return PhaseStatus::MODIFIED_EVERYTHING;
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

// A simple phase that just invokes a method on the compiler instance
// where the method being invoked returns a PhaseStatus
//
class CompilerPhaseWithStatus final : public Phase
{
public:
    CompilerPhaseWithStatus(Compiler* _compiler, Phases _phase, PhaseStatus (Compiler::*_action)())
        : Phase(_compiler, _phase), action(_action)
    {
    }

protected:
    virtual PhaseStatus DoPhase() override
    {
        return (comp->*action)();
    }

private:
    PhaseStatus (Compiler::*action)();
};

// Wrapper for using CompilePhaseWithStatus
//
inline void DoPhase(Compiler* _compiler, Phases _phase, PhaseStatus (Compiler::*_action)())
{
    CompilerPhaseWithStatus phase(_compiler, _phase, _action);
    phase.Run();
}

#endif /* End of _PHASE_H_ */
