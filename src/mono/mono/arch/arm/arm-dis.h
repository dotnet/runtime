/*
 * Copyright (c) 2002 Sergey Chaban <serge@wildwestsoftware.com>
 */

#ifndef ARM_DIS
#define ARM_DIS

#include <stdlib.h>
#include <stdio.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct _ARMDis {
	FILE* dis_out;
	void* pi;
} ARMDis;


void _armdis_set_output(FILE* f);
FILE* _armdis_get_output(void);
void _armdis_decode(void* p, int size);
void _armdis_open(const char* dump_name);
void _armdis_close(void);
void _armdis_dump(const char* dump_name, void* p, int size);


void armdis_init(ARMDis* dis);
void armdis_set_output(ARMDis* dis, FILE* f);
FILE* armdis_get_output(ARMDis* dis);
void armdis_decode(ARMDis* dis, void* p, int size);
void armdis_open(ARMDis* dis, const char* dump_name);
void armdis_close(ARMDis* dis);
void armdis_dump(ARMDis* dis, const char* dump_name, void* p, int size);

#ifdef __cplusplus
}
#endif

#endif /* ARM_DIS */
