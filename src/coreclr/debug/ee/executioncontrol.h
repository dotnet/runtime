// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: executioncontrol.h
//
// Abstraction for breakpoint and single-step operations across different
// code execution strategies (JIT, interpreter, R2R).
// TODO: Currently only interpreter is supported. https://github.com/dotnet/runtime/issues/120842
//
//*****************************************************************************

#ifndef EXECUTIONCONTROL_H_
#define EXECUTIONCONTROL_H_

struct DebuggerControllerPatch;

#ifdef FEATURE_INTERPRETER

class IExecutionControl
{
public:
    virtual ~IExecutionControl() = default;

    virtual bool ApplyPatch(DebuggerControllerPatch* patch) = 0;
    virtual bool UnapplyPatch(DebuggerControllerPatch* patch) = 0;
    virtual void BypassPatch(DebuggerControllerPatch* patch, Thread* pThread) = 0;
};

// Interpreter execution control using bytecode patching
class InterpreterExecutionControl : public IExecutionControl
{
public:
    static InterpreterExecutionControl* GetInstance();

    // Apply a breakpoint patch
    virtual bool ApplyPatch(DebuggerControllerPatch* patch) override;

    // Remove a breakpoint patch and restore original instruction
    virtual bool UnapplyPatch(DebuggerControllerPatch* patch) override;

    // Set bypass on the InterpThreadContext so the interpreter executes
    // the original opcode instead of re-hitting the breakpoint.
    // The bypass is cleared when the breakpoint is hit and the original opcode is executed.
    virtual void BypassPatch(DebuggerControllerPatch* patch, Thread* pThread) override;

private:
    InterpreterExecutionControl() = default;
};

#endif // FEATURE_INTERPRETER
#endif // EXECUTIONCONTROL_H_
