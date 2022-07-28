// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct CallDescrData
{
    uint8_t* pSrc;
    int numStackSlots;
    int fpReturnSize;
    uint8_t* pArgumentRegisters;
    uint8_t* pFloatArgumentRegisters;
    void* pTarget;
    void* pReturnBuffer;
};
