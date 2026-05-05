// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _AUTOVECTORIZER_H_
#define _AUTOVECTORIZER_H_

class AutoVectorizer
{
public:
    explicit AutoVectorizer(Compiler* compiler);

    PhaseStatus RunAnalyze();
    PhaseStatus RunRewrite();

private:
    Compiler* m_compiler;

    bool ShouldDump() const;
    void Dump(const char* format, ...) const;
};

#endif // _AUTOVECTORIZER_H_
