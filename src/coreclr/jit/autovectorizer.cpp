// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#include "autovectorizer.h"

AutoVectorizer::AutoVectorizer(Compiler* compiler)
    : m_compiler(compiler)
{
}

PhaseStatus AutoVectorizer::RunAnalyze()
{
    Dump("AutoVec: analysis phase enabled\n");
    return PhaseStatus::MODIFIED_NOTHING;
}

PhaseStatus AutoVectorizer::RunRewrite()
{
    Dump("AutoVec: rewrite phase enabled\n");
    return PhaseStatus::MODIFIED_NOTHING;
}

bool AutoVectorizer::ShouldDump() const
{
#ifdef DEBUG
    return m_compiler->verbose ||
           JitConfig.JitAutoVectorizationDump().contains(m_compiler->info.compMethodHnd, m_compiler->info.compClassHnd,
                                                         &m_compiler->info.compMethodInfo->args);
#else
    return false;
#endif
}

void AutoVectorizer::Dump(const char* format, ...) const
{
#ifdef DEBUG
    if (!ShouldDump())
    {
        return;
    }

    va_list args;
    va_start(args, format);
    vprintf(format, args);
    va_end(args);
#endif
}

PhaseStatus Compiler::optAutoVectorizeAnalyze()
{
    AutoVectorizer autoVectorizer(this);
    return autoVectorizer.RunAnalyze();
}

PhaseStatus Compiler::optAutoVectorizeRewrite()
{
    AutoVectorizer autoVectorizer(this);
    return autoVectorizer.RunRewrite();
}
