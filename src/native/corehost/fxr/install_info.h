// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __INSTALL_INFO_H__
#define __INSTALL_INFO_H__

#include "pal.h"

namespace install_info
{
    bool print_environment(const pal::char_t* leading_whitespace);
    bool print_other_architectures(const pal::char_t* leading_whitespace);
};

#endif // __INSTALL_INFO_H__
