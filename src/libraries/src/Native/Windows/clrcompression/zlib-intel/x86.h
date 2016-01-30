 /* cpu.h -- check for CPU features
 * Copyright (C) 2013 Intel Corporation Jim Kukunas
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#ifndef CPU_H
#define CPU_H

extern int x86_cpu_has_sse2;
extern int x86_cpu_has_sse42;
extern int x86_cpu_has_pclmulqdq;

void x86_check_features(void);

#endif
