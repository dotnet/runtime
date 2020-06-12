#ifndef _MONO_METADATA_QCALLLIST_H_
#define _MONO_METADATA_QCALLLIST_H_

extern const void* gPalGlobalizationNative[];

typedef struct MonoQCallDef
{
    const char* class_name;
    const char* namespace_name;
    const void**  functions;
} MonoQCallDef;

typedef struct MonoQCallFunc {
    int*            flags;
    void*           implementation;
    const char*     method_name;
} MonoQCallFunc;

#endif
