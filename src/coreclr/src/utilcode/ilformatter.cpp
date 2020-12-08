// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***************************************************************************/
/*                             ILFormatter.h                               */
/***************************************************************************/

#include "stdafx.h"
#include <cor.h>
#include <debugmacros.h>            // for ASSERTE
#include "ilformatter.h"
#include "outstring.h"
#include "opinfo.h"

/***************************************************************************/
void ILFormatter::init(IMetaDataImport* aMeta, const BYTE* aStart,
                  const BYTE* aLimit, unsigned maxStack, const COR_ILMETHOD_SECT_EH* eh) {
    this->~ILFormatter();       // clean out old stuff

    meta = aMeta;
    start = aStart;
    limit = aLimit;
    if (maxStack == 0) maxStack++;
    stackStart = stackCur = new StackEntry[maxStack];
    stackEnd = stackStart + maxStack;
    targetStart = targetCur = targetEnd = 0;
    if (eh != 0) {
        COR_ILMETHOD_SECT_EH_CLAUSE_FAT buff;
        const COR_ILMETHOD_SECT_EH_CLAUSE_FAT* clause;
        for(unsigned i = 0; i < eh->EHCount(); i++) {
            clause = (COR_ILMETHOD_SECT_EH_CLAUSE_FAT*)eh->EHClause(i, &buff);
                // is it a regular catch clause ?
            if ((clause->GetFlags() & (COR_ILEXCEPTION_CLAUSE_FINALLY | COR_ILEXCEPTION_CLAUSE_FAULT)) == 0)
                setTarget(clause->GetHandlerOffset(), 1);
            if(clause->GetFlags() & COR_ILEXCEPTION_CLAUSE_FILTER)
                setTarget(clause->GetFilterOffset(), 1);
        }
    }
}

/***************************************************************************/
inline size_t ILFormatter::stackDepth() {
    return(stackCur - stackStart);
}

/***************************************************************************/
inline void ILFormatter::pushAndClear(OutString* val, int prec) {
    if (stackCur >= stackEnd) {
        _ASSERTE(!"Stack Overflow (can be ignored)");
        return;             // Ignore overflow in free build
    }
    stackCur->val.swap(*val);
    val->clear();
    stackCur->prec = prec;
    stackCur++;
}

/***************************************************************************/
inline OutString*  ILFormatter::top() {
    if (stackDepth() == 0) {
        _ASSERTE(!"Stack underflow (can be ignored)");
        stackStart->val.clear();
        stackStart->val << "<UNDERFLOW ERROR>";
        return (&stackStart->val);
    }
    return(&stackCur[-1].val);
}

/***************************************************************************/
inline OutString* ILFormatter::pop(int prec) {
    if (stackDepth() == 0) {
        _ASSERTE(!"Stack underflow (can be ignored)");
        stackStart->val.clear();
        stackStart->val << "<UNDERFLOW ERROR>";
        return (&stackStart->val);
    }
    --stackCur;
    if (stackCur->prec < prec) {
        stackCur->val.prepend('(');
        stackCur->val << ')';
    }
    return(&stackCur->val);
}

/***************************************************************************/
inline void ILFormatter::popN(size_t num) {
    if (stackCur-stackStart < (SSIZE_T)num) {
        _ASSERTE(!"Stack underflow (can be ignored)");
        stackCur = stackStart;
        return;
    }
    stackCur -= num;
}

/***************************************************************************/
void ILFormatter::setStackAsTarget(size_t ilOffset) {

    Target*ptr = targetStart;
    for(;;) {
        if (ptr >= targetCur)
            return;
        if (ptr->ilOffset == ilOffset)
            break;
        ptr++;
    }

    for(size_t i = 0; i < ptr->stackDepth; i++) {
        stackStart[i].val.clear();
        stackStart[i].val << "@STK" << (unsigned)i;
    }
    stackCur = stackStart + ptr->stackDepth;
}

/***************************************************************************/
void ILFormatter::setTarget(size_t ilOffset, size_t depth) {
    if (depth == 0)
        return;

    if (targetCur >= targetEnd) {
        Target* targetOld = targetStart;
        size_t oldLen = targetCur-targetStart;
        targetStart = new Target[oldLen+10];
        targetEnd = &targetStart[oldLen+10];
        targetCur = &targetStart[oldLen];
        memcpy(targetStart, targetOld, sizeof(Target)*oldLen);
        delete [] targetOld;
    }
    targetCur->ilOffset = ilOffset;
    targetCur->stackDepth = depth;
    targetCur++;
}

/***************************************************************************/
void ILFormatter::spillStack(OutString* out) {

    for(unsigned i = 0; i < stackDepth(); i++) {
        // don't bother spilling something already spilled.
        if (memcmp(stackStart[i].val.val(), "@STK", 4) != 0)
            *out << "@STK" << i << " = " << stackStart[i].val.val() << "\n";
        stackStart[i].val.clear();
        stackStart[i].val << "@STK" << i ;
    }
}

/***************************************************************************/
const BYTE* ILFormatter::formatInstr(const BYTE* instrPtr, OutString* out) {

    _ASSERTE(start < instrPtr && instrPtr < limit);
    OpArgsVal arg;
    OpInfo op;
    instrPtr = op.fetch(instrPtr, &arg);
    *out << op.getName();
    if (op.getArgsInfo() != InlineNone)
        *out << ' ';
    formatInstrArgs(op, arg, out, instrPtr - start);
    return(instrPtr);
}

/***************************************************************************/
void ILFormatter::formatArgs(unsigned numArgs, OutString* out) {

    *out << '(';
    if (numArgs > stackDepth()) {
        _ASSERTE(!"Underflow error");
        *out << "<UNDERFLOW ERROR>";
    }
    else {
        popN(numArgs);
        for(unsigned i = 0; i < numArgs; i++) {
            if (i != 0) *out << ", ";
            *out << stackCur[i].val.val();
        }
    }
    *out << ')';
}

/***************************************************************************/
void ILFormatter::formatInstrArgs(OpInfo op, OpArgsVal arg, OutString* out, size_t curILOffset) {

    MDUTF8CSTR typeName=0;
    HRESULT hr = S_OK;
    switch(op.getArgsInfo() & PrimaryMask) {
        case InlineNone:
            break;
        case InlineVar:
            *out << arg.i;
            break;
        case InlineI:
        case InlineRVA:
            out->hex(arg.i, 0, OutString::put0x);
            break;
        case InlineR:
            *out << arg.r;
            break;
        case InlineBrTarget: {
            _ASSERTE(curILOffset != INVALID_IL_OFFSET);
            size_t target = curILOffset + arg.i;
            setTarget(target, stackDepth());
            *out << "IL_"; out->hex(static_cast<unsigned __int64>(target), 4, OutString::zeroFill);
            } break;
        case InlineI8:
            out->hex(arg.i, 0, OutString::put0x);
            break;
        case InlineString: {
            ULONG numChars;
            WCHAR str[84];

            hr = meta->GetUserString(arg.i, str, 80, &numChars);
            _ASSERTE(SUCCEEDED(hr));
            if (numChars < 80)
                str[numChars] = 0;
            wcscpy_s(&str[79], 4, W("..."));
            *out << '"';
            WCHAR* ptr = str;
            while(*ptr != 0) {
                if (*ptr == '\n')
                    *out << "\\n";
                else if (*ptr == '"')
                    *out << "\\\"";
                else if (*ptr < 0x20 || * ptr >= 0x80) {
                    *out << '\\';
                    out->hex(*ptr, 4, OutString::zeroFill);
                }
                else
                    *out << char(*ptr);
                ptr++;
            }
            *out << '"';
            } break;
        case InlineMethod:
        case InlineField:
        case InlineTok: {
                // Get the typeName if possible
            mdToken mdType = mdTypeDefNil;
            if (TypeFromToken(arg.i) == mdtMethodDef)
                hr = meta->GetMethodProps(mdMethodDef(arg.i), &mdType, 0, 0, 0, 0, 0, 0, 0, 0);
            else if (TypeFromToken(arg.i) == mdtMemberRef)
                hr = meta->GetMemberRefProps(mdMemberRef(arg.i), &mdType, 0, 0, 0, 0, 0);
            else if (TypeFromToken(arg.i) == mdtFieldDef)
                hr = meta->GetFieldProps(mdMethodDef(arg.i), &mdType, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            if (SUCCEEDED(hr) && mdType != mdTypeDefNil) {
                hr = meta->GetNameFromToken(mdType, &typeName);
            }
        }
            FALLTHROUGH;
        case InlineType: {
            // FIX handle case if (TypeFromToken(arg.i) == mdtTypeSpec)
            MDUTF8CSTR name;
            hr = meta->GetNameFromToken(arg.i, &name);
            if (SUCCEEDED(hr)) {
                if (typeName) {
                    const char* lastDot = strrchr(typeName, '.');
                    if (lastDot) typeName = lastDot + 1;
                    *out << typeName << "::";
                }
                *out << name;
            }
            else {
                *out << "TOK<";
                out->hex(arg.i, 0, OutString::put0x);
                *out << '>';
            }
        }   break;
        case InlineSig:
            *out << "SIG<";
            out->hex(arg.i, 0, OutString::put0x);
            *out << '>';
            break;
        case InlineSwitch: {
            _ASSERTE(curILOffset != INVALID_IL_OFFSET);
            unsigned count = arg.switch_.count;
            unsigned i;
            for (i = 0; i < count; i++) {
                size_t target = curILOffset + GET_UNALIGNED_VAL32(&arg.switch_.targets[i]);
                setTarget(target, stackDepth()-1);
                *out << "IL_"; out->hex(static_cast<unsigned __int64>(target), 4, OutString::zeroFill);
                *out << ' ';
                }
            } break;
        case InlinePhi: {
            unsigned count = arg.phi.count;
            unsigned i;
            for (i = 0; i < count; i++) {
                *out << GET_UNALIGNED_VAL32(&arg.phi.vars[i]);
                *out << ' ';
                }
            } break;
        default:
            _ASSERTE(!"BadType");
    }
}

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:21000) // Suppress PREFast warning about overly large function
#endif
/***************************************************************************/
const BYTE* ILFormatter::formatStatement(const BYTE* instrPtr, OutString* out) {

    OutString result;
    OpInfo op;
    OutString *lhs, *rhs, *idx;
    const char* name;
    int prec = 0;

        // set stack as it would be if it was begin jumped to
    setStackAsTarget(instrPtr - start);

    while(instrPtr < limit) {
        OpArgsVal inlineArg;
        instrPtr = op.fetch(instrPtr, &inlineArg);

        switch(op.getOpcode()) {
            case CEE_UNALIGNED:
            case CEE_TAILCALL:
            case CEE_VOLATILE:
                // for now just skip these
                break;

            case CEE_LDARGA_S:
            case CEE_LDARGA:
                result << "&";
                goto DO_LDARG;

            case CEE_LDARG_0:
            case CEE_LDARG_1:
            case CEE_LDARG_2:
            case CEE_LDARG_3:
                inlineArg.i = op.getOpcode() - CEE_LDARG_0;
                goto DO_LDARG;

            case CEE_LDARG:
            case CEE_LDARG_S:
            DO_LDARG:
                name = "arg";
            DO_LDARG_LDLOC:
                result << name << inlineArg.i;
                prec = 0x1000;
                goto DO_PUSH;
            DO_PUSH:
                pushAndClear(&result, prec);   // also clears result!
                break;

            case CEE_LDLOCA_S:
            case CEE_LDLOCA:
                result << "&";
                goto DO_LDLOC;

            case CEE_LDLOC_0:
            case CEE_LDLOC_1:
            case CEE_LDLOC_2:
            case CEE_LDLOC_3:
                inlineArg.i = op.getOpcode() - CEE_LDLOC_0;
                goto DO_LDLOC;

            case CEE_LDLOC:
            case CEE_LDLOC_S:
            DO_LDLOC:
                name = "loc";
                goto DO_LDARG_LDLOC;

            case CEE_STARG:
            case CEE_STARG_S:
                name = "arg";
            DO_STARG_STLOC:
                lhs = pop(0x10);
                result << name << inlineArg.i << " = " << lhs->val();
            DO_STMT:
                spillStack(out);
                *out << result.val() << '\n';
                    // if flow of control does not fall through,
                    // assume the stack is empty
                if (op.getFlow() == FLOW_BRANCH || op.getFlow() == FLOW_RETURN ||
                    op.getFlow() == FLOW_THROW) {
                    popN(stackDepth());
                }
                return(instrPtr);

            case CEE_STLOC_0:
            case CEE_STLOC_1:
            case CEE_STLOC_2:
            case CEE_STLOC_3:
                inlineArg.i = op.getOpcode() - CEE_STLOC_0;
                goto DO_STLOC;

            case CEE_STLOC:
            case CEE_STLOC_S:
            DO_STLOC:
                name = "loc";
                goto DO_STARG_STLOC;

            case CEE_LDC_I4_M1:
            case CEE_LDC_I4_0:
            case CEE_LDC_I4_1:
            case CEE_LDC_I4_2:
            case CEE_LDC_I4_3:
            case CEE_LDC_I4_4:
            case CEE_LDC_I4_5:
            case CEE_LDC_I4_6:
            case CEE_LDC_I4_7:
            case CEE_LDC_I4_8:
                inlineArg.i = op.getOpcode() - CEE_LDC_I4_0;
                FALLTHROUGH;
            case CEE_LDC_I4:
            case CEE_LDC_I4_S:
                result << inlineArg.i;
                prec = 0x1000;
                goto DO_PUSH;

            case CEE_LDC_I8:
                result.hex(inlineArg.i8);
                prec = 0x1000;
                goto DO_PUSH;

            case CEE_LDC_R4:
            case CEE_LDC_R8:
                result << inlineArg.r;
                prec = 0x1000;
                goto DO_PUSH;

            case CEE_LDNULL:
                result << "null";
                prec = 0x1000;
                goto DO_PUSH;

            case CEE_LDSTR:
                formatInstrArgs(op, inlineArg, &result);
                prec = 0x1000;
                goto DO_PUSH;

            case CEE_BEQ:
            case CEE_BEQ_S:
                name = "==";    prec = 0x40;    goto DO_BR_BINOP;
            case CEE_BGE:
            case CEE_BGE_S:
                name = ">=";    prec = 0x40;    goto DO_BR_BINOP;
            case CEE_BGE_UN:
            case CEE_BGE_UN_S:
                name = ">=un";  prec = 0x40;    goto DO_BR_BINOP;

            case CEE_BGT:
            case CEE_BGT_S:
                name = ">";     prec = 0x40;    goto DO_BR_BINOP;
            case CEE_BGT_UN:
            case CEE_BGT_UN_S:
                name = ">un";   prec = 0x40;    goto DO_BR_BINOP;
            case CEE_BLE:
            case CEE_BLE_S:
                name = "<=";    prec = 0x40;    goto DO_BR_BINOP;
            case CEE_BLE_UN:
            case CEE_BLE_UN_S:
                name = "<=un";  prec = 0x40;    goto DO_BR_BINOP;
            case CEE_BLT:
            case CEE_BLT_S:
                name = "<";     prec = 0x40;    goto DO_BR_BINOP;
            case CEE_BLT_UN:
            case CEE_BLT_UN_S:
                name = "<un";   prec = 0x40;    goto DO_BR_BINOP;
            case CEE_BNE_UN:
            case CEE_BNE_UN_S:
                name = "!=un";  prec = 0x40;    goto DO_BR_BINOP;
            DO_BR_BINOP:
                rhs = pop(prec);
                lhs = pop(prec-1);
                result << "if (" << lhs->val() << ' ' << name << ' ' << rhs->val() << ") ";
                goto DO_BR;

            case CEE_LEAVE_S:
            case CEE_LEAVE:
                while (stackDepth() > 0) {
                    lhs = pop();
                    *lhs << '\n' << result;     // put the result in front of anything else
                    result.swap(*lhs);
                }
                FALLTHROUGH;
            case CEE_BR_S:
            case CEE_BR:
            DO_BR: {
                size_t target = (instrPtr - start) + inlineArg.i;
                setTarget(target, stackDepth());
                result << "goto IL_"; result.hex(static_cast<unsigned __int64>(target), 4, OutString::zeroFill);
                } goto DO_STMT;

            case CEE_BRFALSE_S:
            case CEE_BRFALSE:
                name = "!";
                goto DO_BR_UNOP;
            case CEE_BRTRUE_S:
            case CEE_BRTRUE:
                name = "";
            DO_BR_UNOP:
                lhs = pop();
                result << "if (" << name << lhs->val() << ") ";
                goto DO_BR;

            case CEE_OR:
                name = "|";     prec = 0x20;    goto DO_BINOP;
            case CEE_XOR:
                name = "^";     prec = 0x20;    goto DO_BINOP;
            case CEE_AND:
                name = "&";     prec = 0x30;    goto DO_BINOP;
            case CEE_SHL:
                name = "<<";    prec = 0x50;    goto DO_BINOP;
            case CEE_SHR:
                name = ">>";    prec = 0x50;    goto DO_BINOP;
            case CEE_SHR_UN:
                name = ">>un";  prec = 0x50;    goto DO_BINOP;
            case CEE_CEQ:
                name = "==";    prec = 0x40;    goto DO_BINOP;
            case CEE_CGT:
                name = ">";     prec = 0x40;    goto DO_BINOP;
            case CEE_CGT_UN:
                name = ">un";   prec = 0x40;    goto DO_BINOP;
            case CEE_CLT:
                name = "<";     prec = 0x40;    goto DO_BINOP;
            case CEE_CLT_UN:
                name = "<un";   prec = 0x40;    goto DO_BINOP;
            case CEE_ADD:
                name = "+";     prec = 0x60;    goto DO_BINOP;
            case CEE_ADD_OVF:
                name = "+ovf";  prec = 0x60;    goto DO_BINOP;
            case CEE_ADD_OVF_UN:
                name = "+ovf.un";prec = 0x60;   goto DO_BINOP;
            case CEE_SUB:
                name = "-";     prec = 0x60;    goto DO_BINOP;
            case CEE_SUB_OVF:
                name = "-ovf";  prec = 0x60;    goto DO_BINOP;
            case CEE_SUB_OVF_UN:
                name = "-ovf.un";prec = 0x60;   goto DO_BINOP;
            case CEE_MUL:
                name = "*";     prec = 0x70;    goto DO_BINOP;
            case CEE_MUL_OVF:
                name = "*ovf";  prec = 0x70;    goto DO_BINOP;
            case CEE_MUL_OVF_UN:
                name = "*ovf.un";prec = 0x70;   goto DO_BINOP;
            case CEE_DIV:
                name = "/";     prec = 0x70;    goto DO_BINOP;
            case CEE_DIV_UN:
                name = "/un";   prec = 0x70;    goto DO_BINOP;
            case CEE_REM:
                name = "%";     prec = 0x70;    goto DO_BINOP;
            case CEE_REM_UN:
                name = "%un";   prec = 0x70;    goto DO_BINOP;
            DO_BINOP:
                rhs = pop(prec);
                lhs = pop(prec-1);
                result << lhs->val() << ' ' << name << ' ' << rhs->val();
            goto DO_PUSH;

            case CEE_NOT:
                name = "~"; prec = 0x80;    goto DO_UNOP;
            case CEE_NEG:
                name = "-"; prec = 0x80;    goto DO_UNOP;
            DO_UNOP:
                lhs = pop(prec-1);
                result << name << lhs->val();
            goto DO_PUSH;

            case CEE_RET:
                _ASSERTE(stackDepth() <= 1);
                result << "return";
                if (stackDepth() > 0) {
                    lhs = pop();
                    result << ' ' << lhs->val();
                }
            goto DO_STMT;

            case CEE_POP:
                lhs = pop();
                result.swap(*lhs);
                goto DO_STMT;

            case CEE_DUP:
                spillStack(out);
                lhs = top();
                result << lhs->val();
                prec = 0x1000;      // spillstack makes them temps, so they have high prec
                goto DO_PUSH;

            case CEE_LDFLDA:
                name = "&";
                goto DO_LDFLD_LDFLDA;
            case CEE_LDFLD:
                name = "";
            DO_LDFLD_LDFLDA:
                prec = 0x110;
                lhs = pop(prec-1);
                result << name << lhs->val() << '.';
                formatInstrArgs(op, inlineArg, &result);
                goto DO_PUSH;

            case CEE_LDSFLDA:
                name = "&";
                goto DO_LDSFLD_LDSFLDA;
            case CEE_LDSFLD:
                name = "";
            DO_LDSFLD_LDSFLDA:
                prec = 0x1000;
                result << name;
                formatInstrArgs(op, inlineArg, &result);
                goto DO_PUSH;

            case CEE_STFLD:
                rhs = pop(0x10);
                lhs = pop(0x110-1);
                result << lhs->val() << '.';
                formatInstrArgs(op, inlineArg, &result);
                result << " = " << rhs->val();
                goto DO_STMT;

            case CEE_STSFLD:
                rhs = pop(0x20);
                formatInstrArgs(op, inlineArg, &result);
                result << " = " << rhs->val();
                goto DO_STMT;

            case CEE_CALLI:
                lhs = pop();
                result << "CALLI<" << lhs->val() << '>';
                goto DO_CALL;

            case CEE_NEWOBJ:
                result << "new ";
                FALLTHROUGH;
            case CEE_CALL:
            case CEE_CALLVIRT: {
                formatInstrArgs(op, inlineArg, &result);

            DO_CALL:
                    // Get the signature stuff
                PCCOR_SIGNATURE sig;
                ULONG cSig;
                HRESULT hr;
                if (TypeFromToken(inlineArg.i) == mdtMethodDef)
                    hr = meta->GetMethodProps(mdMethodDef(inlineArg.i), 0, 0, 0, 0, 0, &sig, &cSig, 0, 0);
                else if (TypeFromToken(inlineArg.i) == mdtMemberRef)
                    hr = meta->GetMemberRefProps(mdMemberRef(inlineArg.i), 0, 0, 0, 0, &sig, &cSig);
                else
                    hr = meta->GetSigFromToken(mdSignature(inlineArg.i), &sig, &cSig);
                _ASSERTE(SUCCEEDED(hr));
        unsigned callConv = CorSigUncompressData(sig);
                unsigned hasThis = callConv & IMAGE_CEE_CS_CALLCONV_HASTHIS;
        if (callConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        {
          CorSigUncompressData(sig);
        }
                unsigned numArgs = CorSigUncompressData(sig);
                while(*sig == ELEMENT_TYPE_CMOD_REQD || *sig == ELEMENT_TYPE_CMOD_OPT) {
                    sig++;
                    CorSigUncompressToken(sig);
                }

                formatArgs(numArgs, &result);
                if (hasThis && op.getOpcode() != CEE_NEWOBJ) {
                    lhs = pop(0x90);
                    result.swap(*lhs);
                    result << '.' << lhs->val();
                }
                prec = 0x1000;
                if (op.getOpcode() == CEE_NEWOBJ || *sig != ELEMENT_TYPE_VOID)
                    goto DO_PUSH;
                } goto DO_STMT;

            case CEE_LDELEM_I1:
            case CEE_LDELEM_I2:
            case CEE_LDELEM_I4:
            case CEE_LDELEM_I8:
            case CEE_LDELEM_REF:
            case CEE_LDELEM_R4:
            case CEE_LDELEM_R8:
            case CEE_LDELEM_U1:
            case CEE_LDELEM_U2:
            case CEE_LDELEM_I:
                rhs = pop(0x100);
                lhs = pop();
                result << lhs->val() << '[' << rhs->val() << ']';
                prec = 0x100;
                goto DO_PUSH;

            case CEE_STELEM_I1:
            case CEE_STELEM_I2:
            case CEE_STELEM_I4:
            case CEE_STELEM_I8:
            case CEE_STELEM_REF:
            case CEE_STELEM_R4:
            case CEE_STELEM_R8:
            case CEE_STELEM_I:
                rhs = pop(0x100);
                idx = pop();
                lhs = pop(0x20);
                result << lhs->val() << '[' << idx->val() << "] = " << rhs->val();
                goto DO_STMT;

            case CEE_LDIND_I1:  name = "I1"; goto DO_LDIND;
            case CEE_LDIND_I2:  name = "I2"; goto DO_LDIND;
            case CEE_LDIND_I4:  name = "I4"; goto DO_LDIND;
            case CEE_LDIND_I8:  name = "I8"; goto DO_LDIND;
            case CEE_LDIND_I:   name = "I";  goto DO_LDIND;
            case CEE_LDIND_R4:  name = "R4"; goto DO_LDIND;
            case CEE_LDIND_R8:  name = "R8"; goto DO_LDIND;
            case CEE_LDIND_U1:  name = "U1"; goto DO_LDIND;
            case CEE_LDIND_U2:  name = "U2"; goto DO_LDIND;
            case CEE_LDIND_REF: name = "REF";goto DO_LDIND;
            DO_LDIND:
                prec = 0x90;
                lhs = pop(prec);
                result << name << "(*" << lhs->val() << ')';
                goto DO_PUSH;

            case CEE_STIND_I1:  name = "I1"; goto DO_STIND;
            case CEE_STIND_I2:  name = "I2"; goto DO_STIND;
            case CEE_STIND_I4:  name = "I4"; goto DO_STIND;
            case CEE_STIND_I8:  name = "I8"; goto DO_STIND;
            case CEE_STIND_REF: name = "REF";goto DO_STIND;
            case CEE_STIND_R4:  name = "R4"; goto DO_STIND;
            case CEE_STIND_R8:  name = "R8"; goto DO_STIND;
            DO_STIND:
                rhs = pop();
                lhs = pop(0x90);
                result << '*' << lhs->val() << " = " << name << '(' << rhs->val() << ')';
                goto DO_STMT;

            case CEE_LDVIRTFTN:
            case CEE_ARGLIST:
            case CEE_BREAK:
            case CEE_ENDFILTER:
            case CEE_CPBLK:
            case CEE_INITBLK:
            case CEE_LDOBJ:
            case CEE_CPOBJ:
            case CEE_STOBJ:
            case CEE_INITOBJ:
            case CEE_LOCALLOC:
            case CEE_NOP:
            case CEE_SWITCH:
            case CEE_CASTCLASS:
            case CEE_ISINST:
            case CEE_LDLEN:
            case CEE_JMP:
            case CEE_NEWARR:
            case CEE_THROW:
            case CEE_RETHROW:
            case CEE_LDELEM_U4:
            case CEE_LDIND_U4:
            case CEE_LDELEMA:
            case CEE_ENDFINALLY:
            case CEE_STIND_I:
            case CEE_CKFINITE:
            case CEE_MKREFANY:
            case CEE_REFANYTYPE:
            case CEE_REFANYVAL:
            case CEE_CONV_I1:
            case CEE_CONV_I2:
            case CEE_CONV_I4:
            case CEE_CONV_I8:
            case CEE_CONV_R4:
            case CEE_CONV_R8:
            case CEE_CONV_R_UN:
            case CEE_CONV_OVF_I_UN:
            case CEE_CONV_OVF_I1_UN:
            case CEE_CONV_OVF_I2_UN:
            case CEE_CONV_OVF_I4_UN:
            case CEE_CONV_OVF_I8_UN:
            case CEE_CONV_OVF_U_UN:
            case CEE_CONV_OVF_U1_UN:
            case CEE_CONV_OVF_U2_UN:
            case CEE_CONV_OVF_U4_UN:
            case CEE_CONV_OVF_U8_UN:
            case CEE_CONV_OVF_I1:
            case CEE_CONV_OVF_I2:
            case CEE_CONV_OVF_I4:
            case CEE_CONV_OVF_I8:
            case CEE_CONV_OVF_U1:
            case CEE_CONV_OVF_U2:
            case CEE_CONV_OVF_U4:
            case CEE_CONV_OVF_U8:
            case CEE_CONV_U4:
            case CEE_CONV_U8:
            case CEE_CONV_U2:
            case CEE_CONV_U1:
            case CEE_CONV_I:
            case CEE_CONV_OVF_I:
            case CEE_CONV_OVF_U:
            case CEE_CONV_U:
            case CEE_BOX:
            case CEE_LDELEM:
            case CEE_STELEM:
            case CEE_UNBOX_ANY:
            case CEE_UNBOX:
            case CEE_LDFTN:
            case CEE_LDTOKEN:
            case CEE_SIZEOF:
            default:
                result << op.getName();
                if (op.getArgsInfo() != InlineNone) {
                    result << '<';
                    formatInstrArgs(op, inlineArg, &result, instrPtr-start);
                    result << '>';
                }

                _ASSERTE(op.getNumPop() >= 0);
                if (op.getNumPop() > 0)
                    formatArgs(op.getNumPop(), &result);

                prec = 0x1000;
                _ASSERTE(op.getNumPush() == 0 || op.getNumPush() == 1);
                if (op.getNumPush() > 0)
                    goto DO_PUSH;
                goto DO_STMT;
        }
    }
    return(instrPtr);
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif


