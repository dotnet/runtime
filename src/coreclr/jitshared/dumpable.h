// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _DUMPABLE_H_
#define _DUMPABLE_H_

#include <stdio.h>

// Base class for objects that can dump their contents to a file.
// Used by Histogram and other statistics-gathering classes.
class Dumpable
{
public:
    virtual ~Dumpable() = default;
    virtual void dump(FILE* output) const = 0;
};

#endif // _DUMPABLE_H_
