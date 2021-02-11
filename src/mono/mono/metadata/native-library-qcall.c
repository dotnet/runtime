#include "config.h"
#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/icall-decl.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/loader.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/utils/checked-build.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"
#include "mono/metadata/native-library.h"

enum {
    func_flag_end_of_array = 0x01,
    func_flag_has_signature = 0x02,
    func_flag_unreferenced = 0x04, // Suppress unused fcall check
    func_flag_qcall = 0x08, // QCall - mscorlib.dll to mscorwks.dll transition implemented as PInvoke
};

static const MonoQCallDef c_qcalls[] =
{
#ifndef DISABLE_QCALLS
    #define FCClassElement(name,namespace,funcs) {name, namespace, funcs},
    #include "mono/metadata/qcall-def.h"
    #undef FCClassElement
#endif
};

const int c_nECClasses = sizeof (c_qcalls) / sizeof (c_qcalls[0]);

static gboolean is_end_of_array (MonoQCallFunc *func) { return !!((int)func->flags & func_flag_end_of_array); }
static gboolean has_signature (MonoQCallFunc *func) { return !!((int)func->flags & func_flag_has_signature); }
static gboolean is_unreferenced (MonoQCallFunc *func) { return !!((int)func->flags & func_flag_unreferenced); }
static gboolean is_qcall (MonoQCallFunc *func) { return !!((int)func->flags & func_flag_qcall); }
//CorInfoIntrinsics   IntrinsicID(ECFunc *func)   { return (CorInfoIntrinsics)((INT8)(func->m_dwFlags >> 16)); }
//int                 DynamicID(ECFunc *func)     { return (int)              ((int8)(func->m_dwFlags >> 24)); }

static MonoQCallFunc *
next_in_array (MonoQCallFunc *func)
{
    return (MonoQCallFunc *)((char *)func +sizeof (MonoQCallFunc));
        //(HasSignature(func) ? sizeof(ECFunc) : offsetof(ECFunc, func->m_pMethodSig)));
}

static int 
find_impls_index_for_class (MonoMethod *method)
{
    const char *namespace_name = m_class_get_name_space (method->klass);
    const char *name = m_class_get_name (method->klass);

    if (name == NULL)
        return -1;

    unsigned low = 0;
    unsigned high = c_nECClasses;

#ifdef DEBUG
    static bool checkedSort = FALSE;
    if (!checkedSort) {
        checkedSort = TRUE;
        for (unsigned i = 1; i < high; i++)  {
            int cmp = strcmp (c_qcalls[i].class_name, c_qcalls[i-1].class_name);
            if (cmp == 0)
                cmp = strcmp (c_qcalls[i].namespace_name, c_qcalls[i-1].namespace_name);
            g_assert (cmp > 0);
        }
    }
#endif // DEBUG
    while (high > low) {
        unsigned mid  = (high + low) / 2;
        int cmp = strcmp (name, c_qcalls[mid].class_name);
        if (cmp == 0)
            cmp = strcmp (namespace_name, c_qcalls[mid].namespace_name);

        if (cmp == 0) {
            return mid;
        }
        if (cmp > 0)
            low = mid + 1;
        else
            high = mid;
    }
    return -1;
}

static int 
find_index_for_method (MonoMethod *method, const void **impls)
{
    const char *method_name = method->name;
    for (MonoQCallFunc *cur = (MonoQCallFunc *)impls; !is_end_of_array (cur); cur = next_in_array (cur))
    {
        if (strcmp (cur->method_name, method_name) != 0)
            continue;
        return (int)((const void**)cur - impls);
    }

    return -1;
}

gpointer
mono_lookup_pinvoke_qcall_internal (MonoMethod *method, MonoLookupPInvokeStatus *status_out)
{
    int pos_class = find_impls_index_for_class (method);
    if (pos_class < 0) {
        mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_QCALL,
            "Couldn't find class: '%s' in namespace '%s'.", m_class_get_name (method->klass), m_class_get_name_space (method->klass));
        return NULL;
    }
    int pos_method = find_index_for_method (method, c_qcalls[pos_class].functions);
    if (pos_method < 0) {
        mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_QCALL,
            "Couldn't find method: '%s' in class '%s' in namespace '%s'.", method->name, m_class_get_name (method->klass), m_class_get_name_space (method->klass));
        return NULL;
    }
    return (gpointer)c_qcalls[pos_class].functions[pos_method+1];
}
