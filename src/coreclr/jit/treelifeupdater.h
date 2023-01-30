// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "compiler.h"

//------------------------------------------------------------------------
// TreeLifeUpdater: class that handles changes in variable liveness from a given tree.
// Keeps set of temporary VARSET_TP during its lifetime to avoid unnecessary memory allocations.
template <bool ForCodeGen>
class TreeLifeUpdater
{
public:
    TreeLifeUpdater(Compiler* compiler);
    void UpdateLife(GenTree* tree);
    bool UpdateLifeFieldVar(GenTreeLclVar* lclNode, unsigned multiRegIndex);

private:
    void UpdateLifeVar(GenTree* tree, GenTreeLclVarCommon* lclVarTree);
    void UpdateLifeBit(VARSET_TP& set, LclVarDsc* dsc, bool isBorn, bool isDying);
    void StoreCurrentLifeForDump();
    void DumpLifeDelta(GenTree* tree);

private:
    Compiler* compiler;
#ifdef DEBUG
    unsigned  epoch; // VarSets epoch when the class was created, must stay the same during its using.
    VARSET_TP oldLife;
    VARSET_TP oldStackPtrsLife;
#endif // DEBUG
};
