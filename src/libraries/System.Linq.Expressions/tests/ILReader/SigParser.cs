// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Code adapted from https://blogs.msdn.microsoft.com/davbr/2005/10/13/sigparse-cpp

using sig_byte = System.Byte;
using sig_elem_type = System.Byte;
using sig_index_type = System.Byte;
using sig_index = System.Int32;
using sig_count = System.Int32;
using sig_mem_number = System.Int32;

namespace System.Linq.Expressions.Tests
{
    // NB: Ideally, we'd use System.Reflection.Metadata but it doesn't support the use of type
    //     kind ELEMENT_TYPE_INTERNAL which is used by System.Reflection.Emit.SignatureHelper
    //     when adding a type in AddOneArgTypeHelperWorker in the absence of a Module. In that
    //     case the InternalAddRuntimeType method is called which uses ELEMENT_TYPE_INTERNAL.
    //     We need to support this case when we're decompiling IL emitted for DynamicMethods.

    internal class SigParser
    {
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

        protected const int ELEMENT_TYPE_END = 0x00; //Marks end of a list
        protected const int ELEMENT_TYPE_VOID = 0x01;
        protected const int ELEMENT_TYPE_BOOLEAN = 0x02;
        protected const int ELEMENT_TYPE_CHAR = 0x03;
        protected const int ELEMENT_TYPE_I1 = 0x04;
        protected const int ELEMENT_TYPE_U1 = 0x05;
        protected const int ELEMENT_TYPE_I2 = 0x06;
        protected const int ELEMENT_TYPE_U2 = 0x07;
        protected const int ELEMENT_TYPE_I4 = 0x08;
        protected const int ELEMENT_TYPE_U4 = 0x09;
        protected const int ELEMENT_TYPE_I8 = 0x0a;
        protected const int ELEMENT_TYPE_U8 = 0x0b;
        protected const int ELEMENT_TYPE_R4 = 0x0c;
        protected const int ELEMENT_TYPE_R8 = 0x0d;
        protected const int ELEMENT_TYPE_STRING = 0x0e;
        protected const int ELEMENT_TYPE_PTR = 0x0f; // Followed by type
        protected const int ELEMENT_TYPE_BYREF = 0x10; // Followed by type
        protected const int ELEMENT_TYPE_VALUETYPE = 0x11; // Followed by TypeDef or TypeRef token
        protected const int ELEMENT_TYPE_CLASS = 0x12; // Followed by TypeDef or TypeRef token
        protected const int ELEMENT_TYPE_VAR = 0x13; // Generic parameter in a generic type definition, represented as number
        protected const int ELEMENT_TYPE_ARRAY = 0x14; // type rank boundsCount bound1 ... loCount lo1 ...
        protected const int ELEMENT_TYPE_GENERICINST = 0x15; // Generic type instantiation. Followed by type type-arg-count type-1 ... type-n
        protected const int ELEMENT_TYPE_TYPEDBYREF = 0x16;
        protected const int ELEMENT_TYPE_I = 0x18; // System.IntPtr
        protected const int ELEMENT_TYPE_U = 0x19; // System.UIntPtr
        protected const int ELEMENT_TYPE_FNPTR = 0x1b; // Followed by full method signature
        protected const int ELEMENT_TYPE_OBJECT = 0x1c; // System.Object
        protected const int ELEMENT_TYPE_SZARRAY = 0x1d; // Single-dim array with 0 lower bound

        private const int ELEMENT_TYPE_MVAR = 0x1e; // Generic parameter in a generic method definition,represented as number
        private const int ELEMENT_TYPE_CMOD_REQD = 0x1f; // Required modifier : followed by a TypeDef or TypeRef token
        private const int ELEMENT_TYPE_CMOD_OPT = 0x20; // Optional modifier : followed by a TypeDef or TypeRef token
        private const int ELEMENT_TYPE_INTERNAL = 0x21; // Implemented within the CLI
        private const int ELEMENT_TYPE_MODIFIER = 0x40; // Or'd with following element types
        private const int ELEMENT_TYPE_SENTINEL = 0x41; // Sentinel for vararg method signature
        private const int ELEMENT_TYPE_PINNED = 0x45; // Denotes a local variable that points at a pinned object

        private const int SIG_METHOD_DEFAULT = 0x0; // default calling convention
        private const int SIG_METHOD_C = 0x1;// C calling convention
        private const int SIG_METHOD_STDCALL = 0x2;// Stdcall calling convention
        private const int SIG_METHOD_THISCALL = 0x3; // thiscall  calling convention
        private const int SIG_METHOD_FASTCALL = 0x4; // fastcall calling convention
        private const int SIG_METHOD_VARARG = 0x5;// vararg calling convention
        private const int SIG_FIELD = 0x6; // encodes a field
        private const int SIG_LOCAL_SIG = 0x7; // used for the .locals directive
        private const int SIG_PROPERTY = 0x8; // used to encode a property


        private const int SIG_GENERIC = 0x10; // used to indicate that the method has one or more generic parameters.
        private const int SIG_HASTHIS = 0x20; // used to encode the keyword instance in the calling convention
        private const int SIG_EXPLICITTHIS = 0x40; // used to encode the keyword explicit in the calling convention

        private const int SIG_INDEX_TYPE_TYPEDEF = 0; // ParseTypeDefOrRefEncoded returns this as the out index type for typedefs
        private const int SIG_INDEX_TYPE_TYPEREF = 1; // ParseTypeDefOrRefEncoded returns this as the out index type for typerefs
        private const int SIG_INDEX_TYPE_TYPESPEC = 2; // ParseTypeDefOrRefEncoded returns this as the out index type for typespecs

        private sig_byte[] pb;
        private int pbCur;
        private int pbEnd;

        // a method with given elem_type
        protected virtual void NotifyBeginMethod(sig_elem_type elem_type) { }
        protected virtual void NotifyEndMethod() { }

        // total parameters for the method
        protected virtual void NotifyParamCount(sig_count count) { }

        // starting a return type
        protected virtual void NotifyBeginRetType() { }
        protected virtual void NotifyEndRetType() { }

        // starting a parameter
        protected virtual void NotifyBeginParam() { }
        protected virtual void NotifyEndParam() { }

        // sentinel indication the location of the "..." in the method signature
        protected virtual void NotifySentinel() { }

        // number of generic parameters in this method signature (if any)
        protected virtual void NotifyGenericParamCount(sig_count count) { }

        // a field with given elem_type
        protected virtual void NotifyBeginField(sig_elem_type elem_type) { }
        protected virtual void NotifyEndField() { }

        // a block of locals with given elem_type (always just LOCAL_SIG for now)
        protected virtual void NotifyBeginLocals(sig_elem_type elem_type) { }
        protected virtual void NotifyEndLocals() { }

        // count of locals with a block
        protected virtual void NotifyLocalsCount(sig_count count) { }

        // starting a new local within a local block
        protected virtual void NotifyBeginLocal() { }
        protected virtual void NotifyEndLocal() { }

        // the only constraint available to locals at the moment is ELEMENT_TYPE_PINNED
        protected virtual void NotifyConstraint(sig_elem_type elem_type) { }

        // a property with given element type
        protected virtual void NotifyBeginProperty(sig_elem_type elem_type) { }
        protected virtual void NotifyEndProperty() { }

        // starting array shape information for array types
        protected virtual void NotifyBeginArrayShape() { }
        protected virtual void NotifyEndArrayShape() { }

        // array rank (total number of dimensions)
        protected virtual void NotifyRank(sig_count count) { }

        // number of dimensions with specified sizes followed by the size of each
        protected virtual void NotifyNumSizes(sig_count count) { }
        protected virtual void NotifySize(sig_count count) { }

        // BUG BUG lower bounds can be negative, how can this be encoded?
        // number of dimensions with specified lower bounds followed by lower bound of each
        protected virtual void NotifyNumLoBounds(sig_count count) { }
        protected virtual void NotifyLoBound(sig_count count) { }

        // starting a normal type (occurs in many contexts such as param, field, local, etc)
        protected virtual void NotifyBeginType() { }
        protected virtual void NotifyEndType() { }

        protected virtual void NotifyTypedByref() { }

        // the type has the 'byref' modifier on it -- this normally proceeds the type definition in the context
        // the type is used, so for instance a parameter might have the byref modifier on it
        // so this happens before the BeginType in that context
        protected virtual void NotifyByref() { }

        // the type is "VOID" (this has limited uses, function returns and void pointer)
        protected virtual void NotifyVoid() { }

        // the type has the indicated custom modifiers (which can be optional or required)
        protected virtual void NotifyCustomMod(sig_elem_type cmod, sig_index token, sig_index_type indexType, sig_index index) { }

        // the type is a simple type, the elem_type defines it fully
        protected virtual void NotifyTypeSimple(sig_elem_type elem_type) { }

        // the type is specified by the given index of the given index type (normally a type index in the type metadata)
        // this callback is normally qualified by other ones such as NotifyTypeClass or NotifyTypeValueType
        protected virtual void NotifyTypeDefOrRef(sig_index token, sig_index_type indexType, int index) { }

        // number indicates the number of type specifications for the generic types that will follow
        protected virtual void NotifyTypeGenericInst(sig_mem_number number) { }

        // the type is the type of the nth generic type parameter for the class
        protected virtual void NotifyTypeGenericTypeVariable(sig_mem_number number) { }

        // the type is the type of the nth generic type parameter for the member
        protected virtual void NotifyTypeGenericMemberVariable(sig_mem_number number) { }

        // the type will be a value type
        protected virtual void NotifyTypeValueType() { }

        // the type will be a class
        protected virtual void NotifyTypeClass() { }

        // the type is a pointer to a type (nested type notifications follow)
        protected virtual void NotifyTypePointer() { }

        // the type is a function pointer, followed by the type of the function
        protected virtual void NotifyTypeFunctionPointer() { }

        // the type is an array, this is followed by the array shape, see above, as well as modifiers and element type
        protected virtual void NotifyTypeArray() { }

        // the type is a simple zero-based array, this has no shape but does have custom modifiers and element type
        protected virtual void NotifyTypeSzArray() { }

        // used when using a DynamicMethod with internal pointers
        protected virtual void NotifyTypeInternal(IntPtr ptr) { }

        public bool Parse(sig_byte[] pb)
        {
            this.pb = pb;
            pbCur = 0;
            pbEnd = pb.Length;

            sig_elem_type elem_type;

            if (!ParseByte(out elem_type))
                return false;

            switch (elem_type & 0xf)
            {
                case SIG_METHOD_DEFAULT:  // default calling convention
                case SIG_METHOD_C:    // C calling convention
                case SIG_METHOD_STDCALL: // Stdcall calling convention
                case SIG_METHOD_THISCALL: // thiscall  calling convention
                case SIG_METHOD_FASTCALL: // fastcall calling convention
                case SIG_METHOD_VARARG:   // vararg calling convention
                    return ParseMethod(elem_type);

                case SIG_FIELD:           // encodes a field
                    return ParseField(elem_type);

                case SIG_LOCAL_SIG:       // used for the .locals directive
                    return ParseLocals(elem_type);

                case SIG_PROPERTY:        // used to encode a property
                    return ParseProperty(elem_type);

                default:
                    // unknown signature
                    break;
            }

            return false;
        }


        bool ParseByte(out sig_byte pbOut)
        {
            if (pbCur < pbEnd)
            {
                pbOut = pb[pbCur];
                pbCur++;
                return true;
            }

            pbOut = 0;
            return false;
        }

        bool ParseIntPtr(out IntPtr ptr)
        {
            if (pbCur + IntPtr.Size <= pbEnd)
            {
                switch (IntPtr.Size)
                {
                    case 4:
                        ptr = new IntPtr(BitConverter.ToInt32(pb, pbCur));
                        break;
                    case 8:
                        ptr = new IntPtr(BitConverter.ToInt64(pb, pbCur));
                        break;
                    default:
                        throw new NotSupportedException();
                }

                pbCur += IntPtr.Size;
                return true;
            }

            ptr = IntPtr.Zero;
            return false;
        }

        bool ParseMethod(sig_elem_type elem_type)
        {
            // MethodDefSig ::= [[HASTHIS] [EXPLICITTHIS]] (DEFAULT|VARARG|GENERIC GenParamCount)
            //                    ParamCount RetType Param* [SENTINEL Param+]

            NotifyBeginMethod(elem_type);

            sig_count gen_param_count;
            sig_count param_count;

            if ((elem_type & SIG_GENERIC) != 0)
            {
                if (!ParseNumber(out gen_param_count))
                    return false;

                NotifyGenericParamCount(gen_param_count);
            }

            if (!ParseNumber(out param_count))
                return false;

            NotifyParamCount(param_count);

            if (!ParseRetType())
                return false;

            bool fEncounteredSentinel = false;

            for (sig_count i = 0; i < param_count; i++)
            {
                if (pbCur >= pbEnd)
                    return false;

                if (pb[pbCur] == ELEMENT_TYPE_SENTINEL)
                {
                    if (fEncounteredSentinel)
                        return false;

                    fEncounteredSentinel = true;
                    NotifySentinel();
                    pbCur++;
                }

                if (!ParseParam())
                    return false;
            }

            NotifyEndMethod();

            return true;
        }

        bool ParseField(sig_elem_type elem_type)
        {
            // FieldSig ::= FIELD CustomMod* Type

            NotifyBeginField(elem_type);

            if (!ParseOptionalCustomMods())
                return false;

            if (!ParseType())
                return false;

            NotifyEndField();

            return true;
        }

        bool ParseProperty(sig_elem_type elem_type)
        {
            // PropertySig ::= PROPERTY [HASTHIS] ParamCount CustomMod* Type Param*

            NotifyBeginProperty(elem_type);

            sig_count param_count;

            if (!ParseNumber(out param_count))
                return false;

            NotifyParamCount(param_count);

            if (!ParseOptionalCustomMods())
                return false;

            if (!ParseType())
                return false;

            for (sig_count i = 0; i < param_count; i++)
            {
                if (!ParseParam())
                    return false;
            }

            NotifyEndProperty();

            return true;
        }

        bool ParseLocals(sig_elem_type elem_type)
        {
            //   LocalVarSig ::= LOCAL_SIG Count (TYPEDBYREF | ([CustomMod] [Constraint])* [BYREF] Type)+

            NotifyBeginLocals(elem_type);

            sig_count local_count;

            if (!ParseNumber(out local_count))
                return false;

            NotifyLocalsCount(local_count);

            for (sig_count i = 0; i < local_count; i++)
            {
                if (!ParseLocal())
                    return false;
            }

            NotifyEndLocals();

            return true;
        }

        bool ParseLocal()
        {
            //TYPEDBYREF | ([CustomMod] [Constraint])* [BYREF] Type
            NotifyBeginLocal();

            if (pbCur >= pbEnd)
                return false;

            if (pb[pbCur] == ELEMENT_TYPE_TYPEDBYREF)
            {
                NotifyTypedByref();
                pbCur++;
                goto Success;
            }

            if (!ParseOptionalCustomModsOrConstraint())
                return false;

            if (pbCur >= pbEnd)
                return false;

            if (pb[pbCur] == ELEMENT_TYPE_BYREF)
            {
                NotifyByref();
                pbCur++;
            }

            if (!ParseType())
                return false;

            Success:
            NotifyEndLocal();
            return true;
        }

        bool ParseOptionalCustomModsOrConstraint()
        {
            while (true)
            {
                if (pbCur >= pbEnd)
                    return true;

                switch (pb[pbCur])
                {
                    case ELEMENT_TYPE_CMOD_OPT:
                    case ELEMENT_TYPE_CMOD_REQD:
                        if (!ParseCustomMod())
                            return false;
                        break;

                    case ELEMENT_TYPE_PINNED:
                        NotifyConstraint(pb[pbCur]);
                        pbCur++;
                        break;

                    default:
                        return true;
                }
            }
        }

        bool ParseOptionalCustomMods()
        {
            while (true)
            {
                if (pbCur >= pbEnd)
                    return true;

                switch (pb[pbCur])
                {
                    case ELEMENT_TYPE_CMOD_OPT:
                    case ELEMENT_TYPE_CMOD_REQD:
                        if (!ParseCustomMod())
                            return false;
                        break;

                    default:
                        return true;
                }
            }
        }

        bool ParseCustomMod()
        {
            sig_elem_type cmod = 0;
            sig_index index;
            sig_index_type indexType;

            if (!ParseByte(out cmod))
                return false;

            if (cmod == ELEMENT_TYPE_CMOD_OPT || cmod == ELEMENT_TYPE_CMOD_REQD)
            {
                sig_index encoded;

                if (!ParseTypeDefOrRefEncoded(out encoded, out indexType, out index))
                    return false;

                NotifyCustomMod(cmod, encoded, indexType, index);
                return true;
            }

            return false;
        }

        bool ParseParam()
        {
            // Param ::= CustomMod* ( TYPEDBYREF | [BYREF] Type )

            NotifyBeginParam();

            if (!ParseOptionalCustomMods())
                return false;

            if (pbCur >= pbEnd)
                return false;

            if (pb[pbCur] == ELEMENT_TYPE_TYPEDBYREF)
            {
                NotifyTypedByref();
                pbCur++;
                goto Success;
            }

            if (pb[pbCur] == ELEMENT_TYPE_BYREF)
            {
                NotifyByref();
                pbCur++;
            }

            if (!ParseType())
                return false;

            Success:
            NotifyEndParam();
            return true;
        }

        bool ParseRetType()
        {
            // RetType ::= CustomMod* ( VOID | TYPEDBYREF | [BYREF] Type )

            NotifyBeginRetType();

            if (!ParseOptionalCustomMods())
                return false;

            if (pbCur >= pbEnd)
                return false;

            if (pb[pbCur] == ELEMENT_TYPE_TYPEDBYREF)
            {
                NotifyTypedByref();
                pbCur++;
                goto Success;
            }

            if (pb[pbCur] == ELEMENT_TYPE_VOID)
            {
                NotifyVoid();
                pbCur++;
                goto Success;
            }

            if (pb[pbCur] == ELEMENT_TYPE_BYREF)
            {
                NotifyByref();
                pbCur++;
            }

            if (!ParseType())
                return false;

            Success:
            NotifyEndRetType();
            return true;
        }

        bool ParseArrayShape()
        {
            sig_count rank;
            sig_count numsizes;
            sig_count size;

            // ArrayShape ::= Rank NumSizes Size* NumLoBounds LoBound*
            NotifyBeginArrayShape();
            if (!ParseNumber(out rank))
                return false;

            NotifyRank(rank);

            if (!ParseNumber(out numsizes))
                return false;

            NotifyNumSizes(numsizes);

            for (sig_count i = 0; i < numsizes; i++)
            {
                if (!ParseNumber(out size))
                    return false;

                NotifySize(size);
            }

            if (!ParseNumber(out numsizes))
                return false;

            NotifyNumLoBounds(numsizes);

            for (sig_count i = 0; i < numsizes; i++)
            {
                if (!ParseNumber(out size))
                    return false;

                NotifyLoBound(size);
            }

            NotifyEndArrayShape();
            return true;
        }

        bool ParseType()
        {
            /*
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
                            | GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded GenArgCount Type *
                            | VAR Number
                            | MVAR Number

            */

            NotifyBeginType();

            sig_elem_type elem_type;
            sig_index index;
            sig_mem_number number;
            sig_index_type indexType;

            if (!ParseByte(out elem_type))
                return false;

            switch (elem_type)
            {
                case ELEMENT_TYPE_BOOLEAN:
                case ELEMENT_TYPE_CHAR:
                case ELEMENT_TYPE_I1:
                case ELEMENT_TYPE_U1:
                case ELEMENT_TYPE_U2:
                case ELEMENT_TYPE_I2:
                case ELEMENT_TYPE_I4:
                case ELEMENT_TYPE_U4:
                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                case ELEMENT_TYPE_R4:
                case ELEMENT_TYPE_R8:
                case ELEMENT_TYPE_I:
                case ELEMENT_TYPE_U:
                case ELEMENT_TYPE_STRING:
                case ELEMENT_TYPE_OBJECT:
                    // simple types
                    NotifyTypeSimple(elem_type);
                    break;

                case ELEMENT_TYPE_PTR:
                    // PTR CustomMod* VOID
                    // PTR CustomMod* Type

                    NotifyTypePointer();

                    if (!ParseOptionalCustomMods())
                        return false;

                    if (pbCur >= pbEnd)
                        return false;

                    if (pb[pbCur] == ELEMENT_TYPE_VOID)
                    {
                        pbCur++;
                        NotifyVoid();
                        break;
                    }

                    if (!ParseType())
                        return false;

                    break;

                case ELEMENT_TYPE_CLASS:
                    // CLASS TypeDefOrRefEncoded
                    NotifyTypeClass();

                    sig_index encodedC;

                    if (!ParseTypeDefOrRefEncoded(out encodedC, out indexType, out index))
                        return false;

                    NotifyTypeDefOrRef(encodedC, indexType, index);
                    break;

                case ELEMENT_TYPE_VALUETYPE:
                    //VALUETYPE TypeDefOrRefEncoded
                    NotifyTypeValueType();

                    sig_index encodedV;

                    if (!ParseTypeDefOrRefEncoded(out encodedV, out indexType, out index))
                        return false;

                    NotifyTypeDefOrRef(encodedV, indexType, index);
                    break;

                case ELEMENT_TYPE_FNPTR:
                    // FNPTR MethodDefSig
                    // FNPTR MethodRefSig
                    NotifyTypeFunctionPointer();

                    if (!ParseByte(out elem_type))
                        return false;

                    if (!ParseMethod(elem_type))
                        return false;

                    break;

                case ELEMENT_TYPE_ARRAY:
                    // ARRAY Type ArrayShape
                    NotifyTypeArray();

                    if (!ParseType())
                        return false;

                    if (!ParseArrayShape())
                        return false;
                    break;

                case ELEMENT_TYPE_SZARRAY:
                    // SZARRAY CustomMod* Type

                    NotifyTypeSzArray();

                    if (!ParseOptionalCustomMods())
                        return false;

                    if (!ParseType())
                        return false;

                    break;

                case ELEMENT_TYPE_GENERICINST:
                    // GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded GenArgCount Type *

                    if (!ParseType())
                        return false;

                    if (!ParseNumber(out number))
                        return false;

                    for (sig_mem_number i = 0; i < number; i++)
                    {
                        if (!ParseType())
                            return false;
                    }

                    NotifyTypeGenericInst(number);

                    break;

                case ELEMENT_TYPE_VAR:
                    // VAR Number
                    if (!ParseNumber(out number))
                        return false;
                    NotifyTypeGenericTypeVariable(number);
                    break;

                case ELEMENT_TYPE_MVAR:
                    // MVAR Number
                    if (!ParseNumber(out number))
                        return false;
                    NotifyTypeGenericMemberVariable(number);
                    break;

                case ELEMENT_TYPE_INTERNAL:
                    // Used for DynamicMethod when signature helper does not have a module set
                    if (!ParseInternal())
                        return false;
                    break;
            }

            NotifyEndType();

            return true;
        }

        bool ParseInternal()
        {
            IntPtr ptr;
            if (!ParseIntPtr(out ptr))
                return false;
            NotifyTypeInternal(ptr);
            return true;
        }

        bool ParseTypeDefOrRefEncoded(out sig_index pEncoded, out sig_index_type pIndexTypeOut, out sig_index pIndexOut)
        {
            pIndexTypeOut = 0;
            pIndexOut = 0;

            // parse an encoded typedef or typeref

            if (!ParseNumber(out pEncoded))
                return false;

            pIndexTypeOut = (sig_index_type)(pEncoded & 0x3);
            pIndexOut = (pEncoded >> 2);
            return true;
        }

        bool ParseNumber(out sig_count pOut)
        {
            pOut = 0;

            // parse the variable length number format (0-4 bytes)

            sig_byte b1 = 0, b2 = 0, b3 = 0, b4 = 0;

            // at least one byte in the encoding, read that

            if (!ParseByte(out b1))
                return false;

            if (b1 == 0xff)
            {
                // special encoding of 'NULL'
                // not sure what this means as a number, don't expect to see it except for string lengths
                // which we don't encounter anyway so calling it an error
                return false;
            }

            // early out on 1 byte encoding
            if ((b1 & 0x80) == 0)
            {
                pOut = (int)b1;
                return true;
            }

            // now at least 2 bytes in the encoding, read 2nd byte
            if (!ParseByte(out b2))
                return false;

            // early out on 2 byte encoding
            if ((b1 & 0x40) == 0)
            {
                pOut = (((b1 & 0x3f) << 8) | b2);
                return true;
            }

            // must be a 4 byte encoding

            if ((b1 & 0x20) != 0)
            {
                // 4 byte encoding has this bit clear -- error if not
                return false;
            }

            if (!ParseByte(out b3))
                return false;

            if (!ParseByte(out b4))
                return false;

            pOut = ((b1 & 0x1f) << 24) | (b2 << 16) | (b3 << 8) | b4;
            return true;
        }
    }
}
