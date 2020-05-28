
#pragma once

#include "pal_compiler.h"

PALEXPORT int32_t GlobalizationNative_LoadICU(void);

PALEXPORT void GlobalizationNative_InitICUFunctions(void* icuuc, void* icuin, const char* version, const char* suffix);

PALEXPORT int32_t GlobalizationNative_GetICUVersion(void);
