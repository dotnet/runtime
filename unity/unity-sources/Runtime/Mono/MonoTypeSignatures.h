/*
 * blob.h: Definitions used to pull information out of the Blob
 *
 */
#ifndef _MONO_METADATA_BLOB_H_
#define _MONO_METADATA_BLOB_H_

#if ENABLE_MONO

#define SIGNATURE_HAS_THIS      0x20
#define SIGNATURE_EXPLICIT_THIS 0x40
#define SIGNATURE_VARARG        0x05

/*
 * Encoding for type signatures used in the Metadata
 */
typedef enum
{
    MONO_TYPE_END        = 0x00,       /* End of List */
    MONO_TYPE_VOID       = 0x01,
    MONO_TYPE_BOOLEAN    = 0x02,
    MONO_TYPE_CHAR       = 0x03,
    MONO_TYPE_I1         = 0x04,
    MONO_TYPE_U1         = 0x05,
    MONO_TYPE_I2         = 0x06,
    MONO_TYPE_U2         = 0x07,
    MONO_TYPE_I4         = 0x08,
    MONO_TYPE_U4         = 0x09,
    MONO_TYPE_I8         = 0x0a,
    MONO_TYPE_U8         = 0x0b,
    MONO_TYPE_R4         = 0x0c,
    MONO_TYPE_R8         = 0x0d,
    MONO_TYPE_STRING     = 0x0e,
    MONO_TYPE_PTR        = 0x0f,       /* arg: <type> token */
    MONO_TYPE_BYREF      = 0x10,       /* arg: <type> token */
    MONO_TYPE_VALUETYPE  = 0x11,       /* arg: <type> token */
    MONO_TYPE_CLASS      = 0x12,       /* arg: <type> token */
    MONO_TYPE_VAR        = 0x13,       /* number */
    MONO_TYPE_ARRAY      = 0x14,       /* type, rank, boundsCount, bound1, loCount, lo1 */
    MONO_TYPE_GENERICINST = 0x15,       /* <type> <type-arg-count> <type-1> \x{2026} <type-n> */
    MONO_TYPE_TYPEDBYREF = 0x16,
    MONO_TYPE_I          = 0x18,
    MONO_TYPE_U          = 0x19,
    MONO_TYPE_FNPTR      = 0x1b,          /* arg: full method signature */
    MONO_TYPE_OBJECT     = 0x1c,
    MONO_TYPE_SZARRAY    = 0x1d,       /* 0-based one-dim-array */
    MONO_TYPE_MVAR       = 0x1e,       /* number */
    MONO_TYPE_CMOD_REQD  = 0x1f,       /* arg: typedef or typeref token */
    MONO_TYPE_CMOD_OPT   = 0x20,       /* optional arg: typedef or typref token */
    MONO_TYPE_INTERNAL   = 0x21,       /* CLR internal type */

    MONO_TYPE_MODIFIER   = 0x40,       /* Or with the following types */
    MONO_TYPE_SENTINEL   = 0x41,       /* Sentinel for varargs method signature */
    MONO_TYPE_PINNED     = 0x45       /* Local var that points to pinned object */
} MonoTypeEnum;

typedef enum
{
    MONO_PROFILE_NONE = 0,
    MONO_PROFILE_APPDOMAIN_EVENTS = 1 << 0,
    MONO_PROFILE_ASSEMBLY_EVENTS  = 1 << 1,
    MONO_PROFILE_MODULE_EVENTS    = 1 << 2,
    MONO_PROFILE_CLASS_EVENTS     = 1 << 3,
    MONO_PROFILE_JIT_COMPILATION  = 1 << 4,
    MONO_PROFILE_INLINING         = 1 << 5,
    MONO_PROFILE_EXCEPTIONS       = 1 << 6,
    MONO_PROFILE_ALLOCATIONS      = 1 << 7,
    MONO_PROFILE_GC               = 1 << 8,
    MONO_PROFILE_THREADS          = 1 << 9,
    MONO_PROFILE_REMOTING         = 1 << 10,
    MONO_PROFILE_TRANSITIONS      = 1 << 11,
    MONO_PROFILE_ENTER_LEAVE      = 1 << 12,
    MONO_PROFILE_COVERAGE         = 1 << 13,
    MONO_PROFILE_INS_COVERAGE     = 1 << 14,
    MONO_PROFILE_STATISTICAL      = 1 << 15
} MonoProfileFlags;

/*
 * Type Attributes (23.1.15).
 */
enum
{
    MONO_TYPE_ATTR_VISIBILITY_MASK       = 0x00000007,
    MONO_TYPE_ATTR_NOT_PUBLIC            = 0x00000000,
    MONO_TYPE_ATTR_PUBLIC                = 0x00000001,
    MONO_TYPE_ATTR_NESTED_PUBLIC         = 0x00000002,
    MONO_TYPE_ATTR_NESTED_PRIVATE        = 0x00000003,
    MONO_TYPE_ATTR_NESTED_FAMILY         = 0x00000004,
    MONO_TYPE_ATTR_NESTED_ASSEMBLY       = 0x00000005,
    MONO_TYPE_ATTR_NESTED_FAM_AND_ASSEM  = 0x00000006,
    MONO_TYPE_ATTR_NESTED_FAM_OR_ASSEM   = 0x00000007,

    MONO_TYPE_ATTR_LAYOUT_MASK           = 0x00000018,
    MONO_TYPE_ATTR_AUTO_LAYOUT           = 0x00000000,
    MONO_TYPE_ATTR_SEQUENTIAL_LAYOUT     = 0x00000008,
    MONO_TYPE_ATTR_EXPLICIT_LAYOUT       = 0x00000010,

    MONO_TYPE_ATTR_CLASS_SEMANTIC_MASK   = 0x00000020,
    MONO_TYPE_ATTR_CLASS                 = 0x00000000,
    MONO_TYPE_ATTR_INTERFACE             = 0x00000020,

    MONO_TYPE_ATTR_ABSTRACT              = 0x00000080,
    MONO_TYPE_ATTR_SEALED                = 0x00000100,
    MONO_TYPE_ATTR_SPECIAL_NAME          = 0x00000400,

    MONO_TYPE_ATTR_IMPORT                = 0x00001000,
    MONO_TYPE_ATTR_SERIALIZABLE          = 0x00002000,

    MONO_TYPE_ATTR_STRING_FORMAT_MASK    = 0x00030000,
    MONO_TYPE_ATTR_ANSI_CLASS            = 0x00000000,
    MONO_TYPE_ATTR_UNICODE_CLASS         = 0x00010000,
    MONO_TYPE_ATTR_AUTO_CLASS            = 0x00020000,
    MONO_TYPE_ATTR_CUSTOM_CLASS          = 0x00030000,
    MONO_TYPE_ATTR_CUSTOM_MASK           = 0x00c00000,

    MONO_TYPE_ATTR_BEFORE_FIELD_INIT     = 0x00100000,
    MONO_TYPE_ATTR_FORWARDER             = 0x00200000,

    MONO_TYPE_ATTR_RESERVED_MASK         = 0x00040800,
    MONO_TYPE_ATTR_RT_SPECIAL_NAME       = 0x00000800,
    MONO_TYPE_ATTR_HAS_SECURITY          = 0x00040000
};


/*
 * Method Attributes (22.1.9)
 */
enum
{
    METHOD_IMPL_ATTRIBUTE_CODE_TYPE_MASK      = 0x0003,
    METHOD_IMPL_ATTRIBUTE_IL                  = 0x0000,
    METHOD_IMPL_ATTRIBUTE_NATIVE              = 0x0001,
    METHOD_IMPL_ATTRIBUTE_OPTIL               = 0x0002,
    METHOD_IMPL_ATTRIBUTE_RUNTIME             = 0x0003,

    METHOD_IMPL_ATTRIBUTE_MANAGED_MASK        = 0x0004,
    METHOD_IMPL_ATTRIBUTE_UNMANAGED           = 0x0004,
    METHOD_IMPL_ATTRIBUTE_MANAGED             = 0x0000,

    METHOD_IMPL_ATTRIBUTE_FORWARD_REF         = 0x0010,
    METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG        = 0x0080,
    METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL       = 0x1000,
    METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED        = 0x0020,
    METHOD_IMPL_ATTRIBUTE_NOINLINING          = 0x0008,
    METHOD_IMPL_ATTRIBUTE_MAX_METHOD_IMPL_VAL = 0xffff,

    METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK       = 0x0007,
    METHOD_ATTRIBUTE_COMPILER_CONTROLLED      = 0x0000,
    METHOD_ATTRIBUTE_PRIVATE                  = 0x0001,
    METHOD_ATTRIBUTE_FAM_AND_ASSEM            = 0x0002,
    METHOD_ATTRIBUTE_ASSEM                    = 0x0003,
    METHOD_ATTRIBUTE_FAMILY                   = 0x0004,
    METHOD_ATTRIBUTE_FAM_OR_ASSEM             = 0x0005,
    METHOD_ATTRIBUTE_PUBLIC                   = 0x0006,

    METHOD_ATTRIBUTE_STATIC                   = 0x0010,
    METHOD_ATTRIBUTE_FINAL                    = 0x0020,
    METHOD_ATTRIBUTE_VIRTUAL                  = 0x0040,
    METHOD_ATTRIBUTE_HIDE_BY_SIG              = 0x0080,

    METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK       = 0x0100,
    METHOD_ATTRIBUTE_REUSE_SLOT               = 0x0000,
    METHOD_ATTRIBUTE_NEW_SLOT                 = 0x0100,

    METHOD_ATTRIBUTE_ABSTRACT                 = 0x0400,
    METHOD_ATTRIBUTE_SPECIAL_NAME             = 0x0800,

    METHOD_ATTRIBUTE_PINVOKE_IMPL             = 0x2000,
    METHOD_ATTRIBUTE_UNMANAGED_EXPORT         = 0x0008,
};

inline bool IsMonoBuiltinType(int type) { return type >= MONO_TYPE_BOOLEAN && type <= MONO_TYPE_R8;  }

#endif

#endif
