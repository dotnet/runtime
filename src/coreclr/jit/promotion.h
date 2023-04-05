// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _PROMOTION_H
#define _PROMOTION_H

#include "compiler.h"
#include "vector.h"

struct Replacement;

class Promotion
{
    Compiler* m_compiler;

    friend class LocalsUseVisitor;
    friend class ReplaceVisitor;

    void InsertInitialReadBack(unsigned lclNum, const jitstd::vector<Replacement>& replacements, Statement** prevStmt);
    void ExplicitlyZeroInitReplacementLocals(unsigned                           lclNum,
                                             const jitstd::vector<Replacement>& replacements,
                                             Statement**                        prevStmt);
    void InsertInitStatement(Statement** prevStmt, GenTree* tree);

public:
    explicit Promotion(Compiler* compiler) : m_compiler(compiler)
    {
    }

    PhaseStatus Run();
};

#endif
