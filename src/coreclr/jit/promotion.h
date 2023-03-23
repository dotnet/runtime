// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _PROMOTION_H
#define _PROMOTION_H

class Compiler;
struct Replacement;

class Promotion
{
    Compiler* m_compiler;

    friend class LocalsUseVisitor;
    friend class ReplaceVisitor;

    void InsertInitialReadBack(unsigned lclNum, const jitstd::vector<Replacement>& replacements, Statement** prevStmt);
public:
    explicit Promotion(Compiler* compiler) : m_compiler(compiler)
    {
    }

    PhaseStatus Run();
};

#endif
