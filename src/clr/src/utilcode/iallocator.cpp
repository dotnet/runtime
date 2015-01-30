//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "stdafx.h"                     // Precompiled header key.
#include "iallocator.h"

// static
DefaultAllocator DefaultAllocator::s_singleton;

// static
ProcessHeapAllocator ProcessHeapAllocator::s_singleton;

int AllowZeroAllocator::s_zeroLenAllocTarg;
