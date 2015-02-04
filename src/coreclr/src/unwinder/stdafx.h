//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// File: stdafx.h
// 

// Prevent the inclusion of Random.h from disabling rand().  rand() is used by some other headers we include
// and there's no reason why DAC should be forbidden from using it.
#define DO_NOT_DISABLE_RAND

#define USE_COM_CONTEXT_DEF

#include <common.h>
#include <debugger.h>
#include <methoditer.h>
#include <dacprivate.h>
#include <dacimpl.h>
