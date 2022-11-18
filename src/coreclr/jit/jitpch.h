// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdint.h>
#include <windows.h>
#include <wchar.h>
#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>
#include <limits.h>
#include <string.h>
#include <float.h>
#include <cstdlib>
#include <intrin.h>

#include "jitconfig.h"
#include "jit.h"
#include "iallocator.h"
#include "hashbv.h"
#include "compiler.h"
#include "dataflow.h"
#include "block.h"
#include "jiteh.h"
#include "rationalize.h"
#include "jitstd.h"
#include "ssaconfig.h"
#include "blockset.h"
#include "bitvec.h"
#include "inline.h"
#include "objectalloc.h"
