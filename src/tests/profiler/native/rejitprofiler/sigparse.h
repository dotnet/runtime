// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __PROFILER_SIGNATURE_PARSER__
#define __PROFILER_SIGNATURE_PARSER__

/*

Sig ::= MethodDefSig | MethodRefSig | StandAloneMethodSig | FieldSig | PropertySig | LocalVarSig

MethodDefSig ::= [[HASTHIS] [EXPLICITTHIS]] (DEFAULT|VARARG|GENERIC GenParamCount) ParamCount RetType Param*

MethodRefSig ::= [[HASTHIS] [EXPLICITTHIS]] VARARG ParamCount RetType Param* [SENTINEL Param+]

StandAloneMethodSig ::=  [[HASTHIS] [EXPLICITTHIS]] (DEFAULT|VARARG|C|STDCALL|THISCALL|FASTCALL)
                    ParamCount RetType Param* [SENTINEL Param+]

FieldSig ::= FIELD CustomMod* Type

PropertySig ::= PROPERTY [HASTHIS] ParamCount CustomMod* Type Param*

LocalVarSig ::= LOCAL_SIG Count (TYPEDBYREF | ([CustomMod] [Constraint])* [BYREF] Type)+


-------------

CustomMod ::= ( CMOD_OPT | CMOD_REQD ) ( TypeDefEncoded | TypeRefEncoded )

Constraint ::= #define ELEMENT_TYPE_PINNED

Param ::= CustomMod* ( TYPEDBYREF | [BYREF] Type )

RetType ::= CustomMod* ( VOID | TYPEDBYREF | [BYREF] Type )

Type ::= ( BOOLEAN | CHAR | I1 | U1 | U2 | U2 | I4 | U4 | I8 | U8 | R4 | R8 | I | U |
                | VALUETYPE TypeDefOrRefEncoded
                | CLASS TypeDefOrRefEncoded
                | STRING
                | OBJECT
                | PTR CustomMod* VOID
                | PTR CustomMod* Type
                | FNPTR MethodDefSig
                | FNPTR MethodRefSig
                | ARRAY Type ArrayShape
                | SZARRAY CustomMod* Type
                | GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded GenArgCount Type*
                | VAR Number
                | MVAR Number

ArrayShape ::= Rank NumSizes Size* NumLoBounds LoBound*

TypeDefOrRefEncoded ::= TypeDefEncoded | TypeRefEncoded
TypeDefEncoded ::= 32-bit-3-part-encoding-for-typedefs-and-typerefs
TypeRefEncoded ::= 32-bit-3-part-encoding-for-typedefs-and-typerefs

ParamCount ::= 29-bit-encoded-integer
GenArgCount ::= 29-bit-encoded-integer
Count ::= 29-bit-encoded-integer
Rank ::= 29-bit-encoded-integer
NumSizes ::= 29-bit-encoded-integer
Size ::= 29-bit-encoded-integer
NumLoBounds ::= 29-bit-encoded-integer
LoBounds ::= 29-bit-encoded-integer
Number ::= 29-bit-encoded-integer

*/

#define ELEMENT_TYPE_END         0x00 //Marks end of a list
#define ELEMENT_TYPE_VOID        0x01
#define ELEMENT_TYPE_BOOLEAN     0x02
#define ELEMENT_TYPE_CHAR        0x03
#define ELEMENT_TYPE_I1          0x04
#define ELEMENT_TYPE_U1          0x05
#define ELEMENT_TYPE_I2          0x06
#define ELEMENT_TYPE_U2          0x07
#define ELEMENT_TYPE_I4          0x08
#define ELEMENT_TYPE_U4          0x09
#define ELEMENT_TYPE_I8          0x0a
#define ELEMENT_TYPE_U8          0x0b
#define ELEMENT_TYPE_R4          0x0c
#define ELEMENT_TYPE_R8          0x0d
#define ELEMENT_TYPE_STRING      0x0e
#define ELEMENT_TYPE_PTR         0x0f // Followed by type
#define ELEMENT_TYPE_BYREF       0x10 // Followed by type
#define ELEMENT_TYPE_VALUETYPE   0x11 // Followed by TypeDef or TypeRef token
#define ELEMENT_TYPE_CLASS       0x12 // Followed by TypeDef or TypeRef token
#define ELEMENT_TYPE_VAR         0x13 // Generic parameter in a generic type definition, represented as number
#define ELEMENT_TYPE_ARRAY       0x14 // type rank boundsCount bound1 ... loCount lo1 ...
#define ELEMENT_TYPE_GENERICINST 0x15 // Generic type instantiation. Followed by type type-arg-count type-1 ... type-n
#define ELEMENT_TYPE_TYPEDBYREF  0x16
#define ELEMENT_TYPE_I           0x18 // System.IntPtr
#define ELEMENT_TYPE_U           0x19 // System.UIntPtr
#define ELEMENT_TYPE_FNPTR       0x1b // Followed by full method signature
#define ELEMENT_TYPE_OBJECT      0x1c // System.Object
#define ELEMENT_TYPE_SZARRAY     0x1d // Single-dim array with 0 lower bound
#define ELEMENT_TYPE_MVAR        0x1e // Generic parameter in a generic method definition,represented as number
#define ELEMENT_TYPE_CMOD_REQD   0x1f // Required modifier : followed by a TypeDef or TypeRef token
#define ELEMENT_TYPE_CMOD_OPT    0x20 // Optional modifier : followed by a TypeDef or TypeRef token
#define ELEMENT_TYPE_INTERNAL    0x21 // Implemented within the CLI
#define ELEMENT_TYPE_MODIFIER    0x40 // Or'd with following element types
#define ELEMENT_TYPE_SENTINEL    0x41 // Sentinel for vararg method signature
#define ELEMENT_TYPE_PINNED      0x45 // Denotes a local variable that points at a pinned object

#define SIG_METHOD_DEFAULT       0x00 // default calling convention
#define SIG_METHOD_C             0x01 // C calling convention
#define SIG_METHOD_STDCALL       0x02 // Stdcall calling convention
#define SIG_METHOD_THISCALL      0x03 // thiscall  calling convention
#define SIG_METHOD_FASTCALL      0x04 // fastcall calling convention
#define SIG_METHOD_VARARG        0x05 // vararg calling convention
#define SIG_FIELD                0x06 // encodes a field
#define SIG_LOCAL_SIG            0x07 // used for the .locals directive
#define SIG_PROPERTY             0x08 // used to encode a property

#define SIG_GENERIC              0x10 // used to indicate that the method has one or more generic parameters.
#define SIG_HASTHIS              0x20 // used to encode the keyword instance in the calling convention
#define SIG_EXPLICITTHIS         0x40 // used to encode the keyword explicit in the calling convention

#define SIG_INDEX_TYPE_TYPEDEF   0x00 // ParseTypeDefOrRefEncoded returns this as the out index type for typedefs
#define SIG_INDEX_TYPE_TYPEREF   0x01 // ParseTypeDefOrRefEncoded returns this as the out index type for typerefs
#define SIG_INDEX_TYPE_TYPESPEC  0x02 // ParseTypeDefOrRefEncoded returns this as the out index type for typespecs


typedef unsigned char sig_byte;
typedef unsigned char sig_elem_type;
typedef unsigned char sig_index_type;
typedef unsigned int  sig_index;
typedef unsigned int  sig_count;
typedef unsigned int  sig_mem_number;

class SigParser
{
private:
    sig_byte *pbBase;
    sig_byte *pbCur;
    sig_byte *pbEnd;

public:
    bool Parse(sig_byte *blob, sig_count len);

private:
    bool ParseByte(sig_byte *pbOut);
    bool ParseNumber(sig_count *pOut);
    bool ParseTypeDefOrRefEncoded(sig_index_type *pOutIndexType, sig_index *pOutIndex);

    bool ParseMethod(sig_elem_type);
    bool ParseField(sig_elem_type);
    bool ParseProperty(sig_elem_type);
    bool ParseLocals(sig_elem_type);
    bool ParseLocal();
    bool ParseOptionalCustomMods();
    bool ParseOptionalCustomModsOrConstraint();
    bool ParseCustomMod();
    bool ParseRetType();
    bool ParseType();
    bool ParseParam();
    bool ParseArrayShape();

protected:

    // subtype these methods to create your parser side-effects

    //----------------------------------------------------

    // a method with given elem_type
    virtual void NotifyBeginMethod(sig_elem_type elem_type) = 0;
    virtual void NotifyEndMethod() = 0;

    // the method has a this pointer
    virtual void NotifyHasThis() = 0;

    // total parameters for the method
    virtual void NotifyParamCount(sig_count) = 0;

    // starting a return type
    virtual void NotifyBeginRetType() = 0;
    virtual void NotifyEndRetType() = 0;

    // starting a parameter
    virtual void NotifyBeginParam() = 0;
    virtual void NotifyEndParam() = 0;

    // sentinel indication the location of the "..." in the method signature
    virtual void NotifySentinel() = 0;

    // number of generic parameters in this method signature (if any)
    virtual void NotifyGenericParamCount(sig_count) = 0;

    //----------------------------------------------------

    // a field with given elem_type
    virtual void NotifyBeginField(sig_elem_type elem_type) = 0;
    virtual void NotifyEndField() = 0;

    //----------------------------------------------------

    // a block of locals with given elem_type (always just LOCAL_SIG for now)
    virtual void NotifyBeginLocals(sig_elem_type elem_type) = 0;
    virtual void NotifyEndLocals() = 0;

    // count of locals with a block
    virtual void NotifyLocalsCount(sig_count) = 0;

    // starting a new local within a local block
    virtual void NotifyBeginLocal() = 0;
    virtual void NotifyEndLocal() = 0;

    // the only constraint available to locals at the moment is ELEMENT_TYPE_PINNED
    virtual void NotifyConstraint(sig_elem_type elem_type) = 0;


    //----------------------------------------------------

    // a property with given element type
    virtual void NotifyBeginProperty(sig_elem_type elem_type) = 0;
    virtual void NotifyEndProperty() = 0;

    //----------------------------------------------------

    // starting array shape information for array types
    virtual void NotifyBeginArrayShape() = 0;
    virtual void NotifyEndArrayShape() = 0;

    // array rank (total number of dimensions)
    virtual void NotifyRank(sig_count) = 0;

    // number of dimensions with specified sizes followed by the size of each
    virtual void NotifyNumSizes(sig_count) = 0;
    virtual void NotifySize(sig_count) = 0;

    // BUG BUG lower bounds can be negative, how can this be encoded?
    // number of dimensions with specified lower bounds followed by lower bound of each
    virtual void NotifyNumLoBounds(sig_count) = 0;
    virtual void NotifyLoBound(sig_count) = 0;

    //----------------------------------------------------


    // starting a normal type (occurs in many contexts such as param, field, local, etc)
    virtual void NotifyBeginType() = 0;
    virtual void NotifyEndType() = 0;

    virtual void NotifyTypedByref() = 0;

    // the type has the 'byref' modifier on it -- this normally proceeds the type definition in the context
    // the type is used, so for instance a parameter might have the byref modifier on it
    // so this happens before the BeginType in that context
    virtual void NotifyByref() = 0;

    // the type is "VOID" (this has limited uses, function returns and void pointer)
    virtual void NotifyVoid() = 0;

    // the type has the indicated custom modifiers (which can be optional or required)
    virtual void NotifyCustomMod(sig_elem_type cmod, sig_index_type indexType, sig_index index) = 0;

    // the type is a simple type, the elem_type defines it fully
    virtual void NotifyTypeSimple(sig_elem_type  elem_type) = 0;

    // the type is specified by the given index of the given index type (normally a type index in the type metadata)
    // this callback is normally qualified by other ones such as NotifyTypeClass or NotifyTypeValueType
    virtual void NotifyTypeDefOrRef(sig_index_type  indexType, int index) = 0;

    // the type is an instance of a generic
    // elem_type indicates value_type or class
    // indexType and index indicate the metadata for the type in question
    // number indicates the number of type specifications for the generic types that will follow
    virtual void NotifyTypeGenericInst(sig_elem_type elem_type, sig_index_type indexType, sig_index index, sig_mem_number number) = 0;

    // the type is the type of the nth generic type parameter for the class
    virtual void NotifyTypeGenericTypeVariable(sig_mem_number number) = 0;

    // the type is the type of the nth generic type parameter for the member
    virtual void NotifyTypeGenericMemberVariable(sig_mem_number number) = 0;

    // the type will be a value type
    virtual void NotifyTypeValueType() = 0;

    // the type will be a class
    virtual void NotifyTypeClass() = 0;

    // the type is a pointer to a type (nested type notifications follow)
    virtual void NotifyTypePointer() = 0;

    // the type is a function pointer, followed by the type of the function
    virtual void NotifyTypeFunctionPointer() = 0;

    // the type is an array, this is followed by the array shape, see above, as well as modifiers and element type
    virtual void NotifyTypeArray() = 0;

    // the type is a simple zero-based array, this has no shape but does have custom modifiers and element type
    virtual void NotifyTypeSzArray() = 0;
};

//----------------------------------------------------

#endif // __PROFILER_SIGNATURE_PARSER__
