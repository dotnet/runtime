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
    void UpdateLifeVar(GenTree* tree);

private:
    Compiler* compiler;
    VARSET_TP newLife;          // a live set after processing an argument tree.
    VARSET_TP stackVarDeltaSet; // a live set of tracked stack ptr lcls.
    VARSET_TP varDeltaSet;      // a set of variables that changed their liveness.
    VARSET_TP gcTrkStkDeltaSet; // // a set of gc tracked stack variables that changed their liveness..
#ifdef DEBUG
    VARSET_TP gcVarPtrSetNew; // a set to print changes to live part of tracked stack ptr lcls (gcVarPtrSetCur).
    unsigned  epoch;          // VarSets epoch when the class was created, must stay the same during its using.
#endif                        // DEBUG
};
