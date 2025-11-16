// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// TODO-WASM-Factoring: rename the abstractions related to register allocation to make them less LSRA-specific.
class LinearScan : public LinearScanInterface
{
public:
    bool isRegCandidate(LclVarDsc* varDsc);
    bool isContainableMemoryOp(GenTree* node);
};
