// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef REGALLOC_H_
#define REGALLOC_H_

enum FrameType
{
    FT_NOT_SET,
    FT_ESP_FRAME,
    FT_EBP_FRAME,
#if DOUBLE_ALIGN
    FT_DOUBLE_ALIGN_FRAME,
#endif
};

#if DOUBLE_ALIGN
enum CanDoubleAlign
{
    CANT_DOUBLE_ALIGN,
    CAN_DOUBLE_ALIGN,
    MUST_DOUBLE_ALIGN,
    COUNT_DOUBLE_ALIGN,

    DEFAULT_DOUBLE_ALIGN = CAN_DOUBLE_ALIGN
};
#endif

#endif // REGALLOC_H_
