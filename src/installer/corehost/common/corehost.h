// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef _COREHOST_COMMON_COREHOST_H_
#define _COREHOST_COMMON_COREHOST_H_

#if FEATURE_LIBHOST

#include <pal.h>

//
// See ComActivator class in System.Private.CoreLib
//
struct com_activation_context
{
    GUID class_id;
    GUID interface_id;
    const pal::char_t *assembly_path;
    const pal::char_t *assembly_name;
    const pal::char_t *type_name;
    void **class_factory_dest;
};

using com_activation_fn = int(*)(com_activation_context*);

int get_com_activation_delegate(
    pal::string_t *app_path,
    com_activation_fn *delegate);

#endif

#endif //_COREHOST_COMMON_COREHOST_H_
