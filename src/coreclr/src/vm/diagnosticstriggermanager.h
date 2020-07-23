// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DIAGNOSTICS_TRIGGER_MANAGER_H__
#define __DIAGNOSTICS_TRIGGER_MANAGER_H__

class IDiagnosticsTriggerAction
{
public:
    virtual void Run() = 0;
    virtual void Cancel() = 0;
};

struct SurvivorAnalyzer
{
    int gen;
    int promotedBytesThreshold;
    IDiagnosticsTriggerAction* action;
    WCHAR* identity;
    struct SurvivorAnalyzer* next;
};

class DiagnosticsTriggerManager
{
public:
    static DiagnosticsTriggerManager& GetInstance();
    bool RegisterTrigger(const WCHAR* condition, const WCHAR* identity, IDiagnosticsTriggerAction* action);
    bool UnregisterTrigger(const WCHAR* identity);
    void AnalyzeSurvivors();
    void Shutdown();
private:
    DiagnosticsTriggerManager();
    static DiagnosticsTriggerManager s_instance;
    struct SurvivorAnalyzer* survivorAnalyzers;
};

#endif __DIAGNOSTICS_TRIGGER_MANAGER_H__