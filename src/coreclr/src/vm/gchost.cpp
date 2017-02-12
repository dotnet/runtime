// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// gchost.cpp
//
// This module contains the implementation for the IGCController interface.
// This interface is published through the gchost.idl file.  It allows a host
// environment to set config values for the GC.
//

//
//*****************************************************************************

//********** Includes *********************************************************

#include "common.h"
#include "vars.hpp"
#include "eeconfig.h"
#include "perfcounters.h"
#include "gchost.h"
#include "corhost.h"
#include "excep.h"
#include "field.h"
#include "gcheaputilities.h"



