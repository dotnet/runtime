#include <mono/metadata/mh_log.h>
#include <mono/metadata/metadata-internals.h>

static int MH_LOG_verbosity_level = MH_LVL_DEBUG;
static int MH_LOG_verbosity_initialized = 0;

void mh_log_set_verbosity(int verbosity)
{
    MH_LOG_verbosity_level = verbosity;
    MH_LOG_verbosity_initialized = 1;
}

int mh_log_get_verbosity() {
    if (!MH_LOG_verbosity_initialized) {
        const char* env = getenv("MH_LOG_VERBOSITY");
        if (env) {
            MH_LOG_verbosity_level = atoi(env);
        }
        MH_LOG_verbosity_initialized = 1;
    }
    return MH_LOG_verbosity_level;
}


void log_mono_type(MonoType* type) {
    if (!type) {
        MH_LOG("MonoType: NULL");
        return;
    }

    const char* type_str = NULL;
    
    switch (type->type) {
        case MONO_TYPE_VOID: type_str = "VOID"; break;
        case MONO_TYPE_BOOLEAN: type_str = "BOOLEAN"; break;
        case MONO_TYPE_CHAR: type_str = "CHAR"; break;
        case MONO_TYPE_I1: type_str = "I1"; break;
        case MONO_TYPE_U1: type_str = "U1"; break;
        case MONO_TYPE_I2: type_str = "I2"; break;
        case MONO_TYPE_U2: type_str = "U2"; break;
        case MONO_TYPE_I4: type_str = "I4"; break;
        case MONO_TYPE_U4: type_str = "U4"; break;
        case MONO_TYPE_I8: type_str = "I8"; break;
        case MONO_TYPE_U8: type_str = "U8"; break;
        case MONO_TYPE_R4: type_str = "R4"; break;
        case MONO_TYPE_R8: type_str = "R8"; break;
        case MONO_TYPE_STRING: type_str = "STRING"; break;
        case MONO_TYPE_PTR: type_str = "PTR"; break;
        case MONO_TYPE_BYREF: type_str = "BYREF"; break;
        case MONO_TYPE_VALUETYPE: type_str = "VALUETYPE"; break;
        case MONO_TYPE_CLASS: type_str = "CLASS"; break;
        case MONO_TYPE_ARRAY: type_str = "ARRAY"; break;
        case MONO_TYPE_SZARRAY: type_str = "SZARRAY"; break;
        case MONO_TYPE_OBJECT: type_str = "OBJECT"; break;
        case MONO_TYPE_VAR: type_str = "VAR"; break;
        case MONO_TYPE_MVAR: type_str = "MVAR"; break;
        case MONO_TYPE_GENERICINST: type_str = "GENERICINST"; break;
        case MONO_TYPE_FNPTR: type_str = "FNPTR"; break;
        default: type_str = "UNKNOWN"; break;
    }
    
    MH_LOG("MonoType: %s (%d)", type_str, type->type);
}

void log_mono_type_enum(MonoTypeEnum type_enum) {
    const char* type_str = NULL;
    
    switch (type_enum) {
        case MONO_TYPE_END: type_str = "END"; break;
        case MONO_TYPE_VOID: type_str = "VOID"; break;
        case MONO_TYPE_BOOLEAN: type_str = "BOOLEAN"; break;
        case MONO_TYPE_CHAR: type_str = "CHAR"; break;
        case MONO_TYPE_I1: type_str = "I1"; break;
        case MONO_TYPE_U1: type_str = "U1"; break;
        case MONO_TYPE_I2: type_str = "I2"; break;
        case MONO_TYPE_U2: type_str = "U2"; break;
        case MONO_TYPE_I4: type_str = "I4"; break;
        case MONO_TYPE_U4: type_str = "U4"; break;
        case MONO_TYPE_I8: type_str = "I8"; break;
        case MONO_TYPE_U8: type_str = "U8"; break;
        case MONO_TYPE_R4: type_str = "R4"; break;
        case MONO_TYPE_R8: type_str = "R8"; break;
        case MONO_TYPE_STRING: type_str = "STRING"; break;
        case MONO_TYPE_PTR: type_str = "PTR"; break;
        case MONO_TYPE_BYREF: type_str = "BYREF"; break;
        case MONO_TYPE_VALUETYPE: type_str = "VALUETYPE"; break;
        case MONO_TYPE_CLASS: type_str = "CLASS"; break;
        case MONO_TYPE_VAR: type_str = "VAR"; break;
        case MONO_TYPE_ARRAY: type_str = "ARRAY"; break;
        case MONO_TYPE_GENERICINST: type_str = "GENERICINST"; break;
        case MONO_TYPE_TYPEDBYREF: type_str = "TYPEDBYREF"; break;
        case MONO_TYPE_I: type_str = "I"; break;
        case MONO_TYPE_U: type_str = "U"; break;
        case MONO_TYPE_FNPTR: type_str = "FNPTR"; break;
        case MONO_TYPE_OBJECT: type_str = "OBJECT"; break;
        case MONO_TYPE_SZARRAY: type_str = "SZARRAY"; break;
        case MONO_TYPE_MVAR: type_str = "MVAR"; break;
        case MONO_TYPE_CMOD_REQD: type_str = "CMOD_REQD"; break;
        case MONO_TYPE_CMOD_OPT: type_str = "CMOD_OPT"; break;
        case MONO_TYPE_INTERNAL: type_str = "INTERNAL"; break;
        case MONO_TYPE_MODIFIER: type_str = "MODIFIER"; break;
        case MONO_TYPE_SENTINEL: type_str = "SENTINEL"; break;
        case MONO_TYPE_PINNED: type_str = "PINNED"; break;
        case MONO_TYPE_ENUM: type_str = "MONO_TYPE_ENUM"; break;
        default: type_str = "UNKNOWN"; break;
    }
    
    MH_LOG("MonoTypeEnum: %s (0x%x)", type_str, (int)type_enum);
}

#ifndef MH_MINT_TYPES_DEFINED
#define MH_MINT_TYPES_DEFINED
// mirror those in interp-internals.h
#define MH_MINT_TYPE_I1 0
#define MH_MINT_TYPE_U1 1
#define MH_MINT_TYPE_I2 2
#define MH_MINT_TYPE_U2 3
#define MH_MINT_TYPE_I4 4
#define MH_MINT_TYPE_I8 5
#define MH_MINT_TYPE_R4 6
#define MH_MINT_TYPE_R8 7
#define MH_MINT_TYPE_O  8
#define MH_MINT_TYPE_VT 9
#define MH_MINT_TYPE_VOID 10
#endif
void log_mint_type(int value)
{
	const char* type_str = NULL;
	switch (value)
	{
case MH_MINT_TYPE_I1: type_str = "MINT_TYPE_I1";break;
case MH_MINT_TYPE_U1: type_str = "MINT_TYPE_U1";break;
case MH_MINT_TYPE_I2: type_str = "MINT_TYPE_I2";break;
case MH_MINT_TYPE_U2: type_str = "MINT_TYPE_U2";break;
case MH_MINT_TYPE_I4: type_str = "MINT_TYPE_I4";break;
case MH_MINT_TYPE_I8: type_str = "MINT_TYPE_I8";break;
case MH_MINT_TYPE_R4: type_str = "MINT_TYPE_R4";break;
case MH_MINT_TYPE_R8: type_str = "MINT_TYPE_R8";break;
case MH_MINT_TYPE_O : type_str = "MINT_TYPE_O";break;
case MH_MINT_TYPE_VT: type_str = "MINT_TYPE_VT";break;
case MH_MINT_TYPE_VOID: type_str = "MINT_TYPE_VOID"; break;
	default:
		type_str = "UNKNOWN";
	}
	MH_LOG("MintType: %s (%d)", type_str, value);
}
