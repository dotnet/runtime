#ifndef _MONO_METADATA_QCALLLIST_H_
#define _MONO_METADATA_QCALLLIST_H_

extern const void* gPalGlobalizationNative[];
extern const void* gEventPipeInternalFuncs[];


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

enum {
    func_flag_end_of_array   = 0x01,
    func_flag_has_signature = 0x02,
    func_flag_unreferenced = 0x04, // Suppress unused fcall check
    func_flag_qcall        = 0x08, // QCall - mscorlib.dll to mscorwks.dll transition implemented as PInvoke
};

const MonoQCallDef c_qcalls[] =
{
#define FCClassElement(name,namespace,funcs) {name, namespace, funcs},
FCClassElement("EventPipeInternal", "System.Diagnostics.Tracing", gEventPipeInternalFuncs)
FCClassElement("Globalization", "", gPalGlobalizationNative)
};

#endif 