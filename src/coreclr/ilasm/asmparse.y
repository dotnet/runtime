%{

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// File asmparse.y
//
#include "ilasmpch.h"

#include "grammar_before.cpp"

%}

%union {
        CorRegTypeAttr classAttr;
        CorMethodAttr methAttr;
        CorFieldAttr fieldAttr;
        CorMethodImpl implAttr;
        CorEventAttr  eventAttr;
        CorPropertyAttr propAttr;
        CorPinvokeMap pinvAttr;
        CorDeclSecurity secAct;
        CorFileFlags fileAttr;
        CorAssemblyFlags asmAttr;
        CorAssemblyFlags asmRefAttr;
        CorTypeAttr exptAttr;
        CorManifestResourceFlags manresAttr;
        double*  float64;
        __int64* int64;
        __int32  int32;
        char*    string;
        BinStr*  binstr;
        Labels*  labels;
        Instr*   instr;         // instruction opcode
        NVPair*  pair;
        pTyParList typarlist;
        mdToken token;
        TypeDefDescr* tdd;
        CustomDescr*  cad;
        unsigned short opcode;
};

        /* These are returned by the LEXER and have values */
%token ERROR_ BAD_COMMENT_ BAD_LITERAL_                         /* bad strings,    */
%token <string>  ID             /* testing343 */
%token <string>  DOTTEDNAME     /* System.Object */
%token <binstr>  QSTRING        /* "Hello World\n" */
%token <string>  SQSTRING       /* 'Hello World\n' */
%token <int32>   INT32          /* 3425 0x34FA  0352  */
%token <int64>   INT64          /* 342534523534534      0x34FA434644554 */
%token <float64> FLOAT64        /* -334234 24E-34 */
%token <int32>   HEXBYTE        /* 05 1A FA */
%token <tdd>     TYPEDEF_T
%token <tdd>     TYPEDEF_M
%token <tdd>     TYPEDEF_F
%token <tdd>     TYPEDEF_TS
%token <tdd>     TYPEDEF_MR
%token <tdd>     TYPEDEF_CA


        /* multi-character punctuation */
%token DCOLON                   /* :: */
%token ELLIPSIS                  /* ... */

        /* Keywords   Note the undersores are to avoid collisions as these are common names */
%token VOID_ BOOL_ CHAR_ UNSIGNED_ INT_ INT8_ INT16_ INT32_ INT64_ FLOAT_ FLOAT32_ FLOAT64_ BYTEARRAY_
%token UINT_ UINT8_ UINT16_ UINT32_ UINT64_  FLAGS_ CALLCONV_ MDTOKEN_
%token OBJECT_ STRING_ NULLREF_
        /* misc keywords */
%token DEFAULT_ CDECL_ VARARG_ STDCALL_ THISCALL_ FASTCALL_ CLASS_ BYREFLIKE_
%token TYPEDREF_ UNMANAGED_ FINALLY_ HANDLER_ CATCH_ FILTER_ FAULT_
%token EXTENDS_ IMPLEMENTS_ TO_ AT_ TLS_ TRUE_ FALSE_ _INTERFACEIMPL

        /* class, method, field attributes */

%token VALUE_ VALUETYPE_ NATIVE_ INSTANCE_ SPECIALNAME_ FORWARDER_
%token STATIC_ PUBLIC_ PRIVATE_ FAMILY_ FINAL_ SYNCHRONIZED_ INTERFACE_ SEALED_ NESTED_
%token ABSTRACT_ AUTO_ SEQUENTIAL_ EXPLICIT_ ANSI_ UNICODE_ AUTOCHAR_ IMPORT_ ENUM_
%token VIRTUAL_ NOINLINING_ AGGRESSIVEINLINING_ NOOPTIMIZATION_ AGGRESSIVEOPTIMIZATION_ UNMANAGEDEXP_ BEFOREFIELDINIT_
%token STRICT_ RETARGETABLE_ WINDOWSRUNTIME_ NOPLATFORM_
%token METHOD_ FIELD_ PINNED_ MODREQ_ MODOPT_ SERIALIZABLE_ PROPERTY_ TYPE_
%token ASSEMBLY_ FAMANDASSEM_ FAMORASSEM_ PRIVATESCOPE_ HIDEBYSIG_ NEWSLOT_ RTSPECIALNAME_ PINVOKEIMPL_
%token _CTOR _CCTOR LITERAL_ NOTSERIALIZED_ INITONLY_ REQSECOBJ_
        /* method implementation attributes: NATIVE_ and UNMANAGED_ listed above */
%token CIL_ OPTIL_ MANAGED_ FORWARDREF_ PRESERVESIG_ RUNTIME_ INTERNALCALL_
        /* PInvoke-specific keywords */
%token _IMPORT NOMANGLE_ LASTERR_ WINAPI_ AS_ BESTFIT_ ON_ OFF_ CHARMAPERROR_

        /* instruction tokens (actually instruction groupings) */
%token <opcode> INSTR_NONE INSTR_VAR INSTR_I INSTR_I8 INSTR_R INSTR_BRTARGET INSTR_METHOD INSTR_FIELD
%token <opcode> INSTR_TYPE INSTR_STRING INSTR_SIG INSTR_TOK
%token <opcode> INSTR_SWITCH

        /* assember directives */
%token _CLASS _NAMESPACE _METHOD _FIELD _DATA _THIS _BASE _NESTER
%token _EMITBYTE _TRY _MAXSTACK _LOCALS _ENTRYPOINT _ZEROINIT
%token _EVENT _ADDON _REMOVEON _FIRE _OTHER
%token _PROPERTY _SET _GET DEFAULT_
%token _PERMISSION _PERMISSIONSET

                /* security actions */
%token REQUEST_ DEMAND_ ASSERT_ DENY_ PERMITONLY_ LINKCHECK_ INHERITCHECK_
%token REQMIN_ REQOPT_ REQREFUSE_ PREJITGRANT_ PREJITDENY_ NONCASDEMAND_
%token NONCASLINKDEMAND_ NONCASINHERITANCE_

        /* extern debug info specifier (to be used by precompilers only) */
%token _LINE P_LINE _LANGUAGE
        /* custom value specifier */
%token _CUSTOM
        /* local vars zeroinit specifier */
%token INIT_
        /* class layout */
%token _SIZE _PACK
%token _VTABLE _VTFIXUP FROMUNMANAGED_ CALLMOSTDERIVED_ _VTENTRY RETAINAPPDOMAIN_
        /* manifest */
%token _FILE NOMETADATA_ _HASH _ASSEMBLY _PUBLICKEY _PUBLICKEYTOKEN ALGORITHM_ _VER _LOCALE EXTERN_
%token _MRESOURCE
%token _MODULE _EXPORT
%token LEGACY_ LIBRARY_ X86_ AMD64_ ARM_ ARM64_
        /* field marshaling */
%token MARSHAL_ CUSTOM_ SYSSTRING_ FIXED_ VARIANT_ CURRENCY_ SYSCHAR_ DECIMAL_ DATE_ BSTR_ TBSTR_ LPSTR_
%token LPWSTR_ LPTSTR_ OBJECTREF_ IUNKNOWN_ IDISPATCH_ STRUCT_ SAFEARRAY_ BYVALSTR_ LPVOID_ ANY_ ARRAY_ LPSTRUCT_
%token IIDPARAM_
        /* parameter keywords */
%token IN_ OUT_ OPT_
        /* .param directive */
%token _PARAM
                /* method implementations */
%token _OVERRIDE WITH_
                /* variant type specifics */
%token NULL_ ERROR_ HRESULT_ CARRAY_ USERDEFINED_ RECORD_ FILETIME_ BLOB_ STREAM_ STORAGE_
%token STREAMED_OBJECT_ STORED_OBJECT_ BLOB_OBJECT_ CF_ CLSID_ VECTOR_
                /* header flags */
%token _SUBSYSTEM _CORFLAGS ALIGNMENT_ _IMAGEBASE _STACKRESERVE

        /* syntactic sugar */
%token _TYPEDEF _TEMPLATE _TYPELIST _MSCORLIB

        /* compilation control directives */
%token P_DEFINE P_UNDEF P_IFDEF P_IFNDEF P_ELSE P_ENDIF P_INCLUDE

        /* newly added tokens go here */
%token  CONSTRAINT_

        /* nonTerminals */
%type <string> dottedName id methodName atOpt slashedName
%type <labels> labels
%type <int32> callConv callKind int32 customHead customHeadWithOwner vtfixupAttr paramAttr ddItemCount variantType repeatOpt truefalse typarAttrib typarAttribs
%type <int32> iidParamIndex genArity genArityNotEmpty
%type <float64> float64
%type <int64> int64
%type <binstr> sigArgs0 sigArgs1 sigArg type bound bounds1 bytes hexbytes nativeType marshalBlob initOpt compQstring caValue
%type <binstr> marshalClause
%type <binstr> fieldInit serInit fieldSerInit
%type <binstr> f32seq f64seq i8seq i16seq i32seq i64seq boolSeq sqstringSeq classSeq objSeq
%type <binstr> simpleType
%type <binstr> tyArgs0 tyArgs1 tyArgs2 typeList typeListNotEmpty tyBound
%type <binstr> customBlobDescr serializType customBlobArgs customBlobNVPairs
%type <binstr> secAttrBlob secAttrSetBlob
%type <int32> fieldOrProp intOrWildcard
%type <typarlist> typarsRest typars typarsClause
%type <token> className typeSpec ownerType customType memberRef methodRef mdtoken
%type <classAttr> classAttr
%type <methAttr> methAttr
%type <fieldAttr> fieldAttr
%type <implAttr> implAttr
%type <eventAttr> eventAttr
%type <propAttr> propAttr
%type <pinvAttr> pinvAttr
%type <pair> nameValPairs nameValPair
%type <secAct> secAction
%type <secAct> psetHead
%type <fileAttr> fileAttr
%type <fileAttr> fileEntry
%type <asmAttr> asmAttr
%type <exptAttr> exptAttr
%type <manresAttr> manresAttr
%type <cad> customDescr customDescrWithOwner
%type <instr> instr_none instr_var instr_i instr_i8 instr_r instr_brtarget instr_method instr_field
%type <instr> instr_type instr_string instr_sig instr_tok instr_switch
%type <instr> instr_r_head

%start decls

/**************************************************************************/
%%

decls                   : /* EMPTY */
                        | decls decl
                        ;
/* Module-level declarations */
decl                    : classHead '{' classDecls '}'                          { PASM->EndClass(); }
                        | nameSpaceHead '{' decls '}'                           { PASM->EndNameSpace(); }
                        | methodHead  methodDecls '}'                           { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                                                  {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                                     PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                                                  PASM->EndMethod(); }
                        | fieldDecl
                        | dataDecl
                        | vtableDecl
                        | vtfixupDecl
                        | extSourceSpec
                        | fileDecl
                        | assemblyHead '{' assemblyDecls '}'                    { PASMM->EndAssembly(); }
                        | assemblyRefHead '{' assemblyRefDecls '}'              { PASMM->EndAssembly(); }
                        | exptypeHead '{' exptypeDecls '}'                      { PASMM->EndComType(); }
                        | manifestResHead '{' manifestResDecls '}'              { PASMM->EndManifestRes(); }
                        | moduleHead
                        | secDecl
                        | customAttrDecl
                        | _SUBSYSTEM int32                                      {
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:22011) // Suppress PREFast warning about integer overflow/underflow
#endif
                                                                                  PASM->m_dwSubsystem = $2;
#ifdef _PREFAST_
#pragma warning(pop)
#endif
                                                                                }
                        | _CORFLAGS int32                                       { PASM->m_dwComImageFlags = $2; }
                        | _FILE ALIGNMENT_ int32                                { PASM->m_dwFileAlignment = $3;
                                                                                  if(($3 & ($3 - 1))||($3 < 0x200)||($3 > 0x10000))
                                                                                    PASM->report->error("Invalid file alignment, must be power of 2 from 0x200 to 0x10000\n");}
                        | _IMAGEBASE int64                                      { PASM->m_stBaseAddress = (ULONGLONG)(*($2)); delete $2;
                                                                                  if(PASM->m_stBaseAddress & 0xFFFF)
                                                                                    PASM->report->error("Invalid image base, must be 0x10000-aligned\n");}
                        | _STACKRESERVE int64                                   { PASM->m_stSizeOfStackReserve = (size_t)(*($2)); delete $2; }
                        | languageDecl
                        | typedefDecl
                        | compControl
                        | _TYPELIST '{' classNameSeq '}'
                        | _MSCORLIB                                             { PASM->m_fIsMscorlib = TRUE; }
                        ;

classNameSeq            : /* EMPTY */
                        | className classNameSeq
                        ;

compQstring             : QSTRING                                               { $$ = $1; }
                        | compQstring '+' QSTRING                               { $$ = $1; $$->append($3); delete $3; }
                        ;

languageDecl            : _LANGUAGE SQSTRING                                    { LPCSTRToGuid($2,&(PASM->m_guidLang)); }
                        | _LANGUAGE SQSTRING ',' SQSTRING                       { LPCSTRToGuid($2,&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid($4,&(PASM->m_guidLangVendor));}
                        | _LANGUAGE SQSTRING ',' SQSTRING ',' SQSTRING          { LPCSTRToGuid($2,&(PASM->m_guidLang));
                                                                                  LPCSTRToGuid($4,&(PASM->m_guidLangVendor));
                                                                                  LPCSTRToGuid($4,&(PASM->m_guidDoc));}
                        ;
/*  Basic tokens  */
id                      : ID                                  { $$ = $1; }
                        | SQSTRING                            { $$ = $1; }
                        ;

dottedName              : id                                  { $$ = $1; }
                        | DOTTEDNAME                          { $$ = $1; }
                        | dottedName '.' dottedName           { $$ = newStringWDel($1, '.', $3); }
                        ;

int32                   : INT32                               { $$ = $1; }
                        ;

int64                   : INT64                               { $$ = $1; }
                        | INT32                               { $$ = neg ? new __int64($1) : new __int64((unsigned)$1); }
                        ;

float64                 : FLOAT64                             { $$ = $1; }
                        | FLOAT32_ '(' int32 ')'              { float f; *((__int32*) (&f)) = $3; $$ = new double(f); }
                        | FLOAT64_ '(' int64 ')'              { $$ = (double*) $3; }
                        ;

/*  Aliasing of types, type specs, methods, fields and custom attributes */
typedefDecl             : _TYPEDEF type AS_ dottedName                          { PASM->AddTypeDef($2,$4); }
                        | _TYPEDEF className AS_ dottedName                     { PASM->AddTypeDef($2,$4); }
                        | _TYPEDEF memberRef AS_ dottedName                     { PASM->AddTypeDef($2,$4); }
                        | _TYPEDEF customDescr AS_ dottedName                   { $2->tkOwner = 0; PASM->AddTypeDef($2,$4); }
                        | _TYPEDEF customDescrWithOwner AS_ dottedName          { PASM->AddTypeDef($2,$4); }
                        ;

/*  Compilation control directives are processed within yylex(),
    displayed here just for grammar completeness */
compControl             : P_DEFINE dottedName                                   { DefineVar($2, NULL); }
                        | P_DEFINE dottedName compQstring                       { DefineVar($2, $3); }
                        | P_UNDEF dottedName                                    { UndefVar($2); }
                        | P_IFDEF dottedName                                    { SkipToken = !IsVarDefined($2);
                                                                                  IfEndif++;
                                                                                }
                        | P_IFNDEF dottedName                                   { SkipToken = IsVarDefined($2);
                                                                                  IfEndif++;
                                                                                }
                        | P_ELSE                                                { if(IfEndif == 1) SkipToken = !SkipToken;}
                        | P_ENDIF                                               { if(IfEndif == 0)
                                                                                    PASM->report->error("Unmatched #endif\n");
                                                                                  else IfEndif--;
                                                                                }
                        | P_INCLUDE QSTRING                                     { _ASSERTE(!"yylex should have dealt with this"); }
                        | ';'                                                   { }
                        ;

/* Custom attribute declarations  */
customDescr             : _CUSTOM customType                                    { $$ = new CustomDescr(PASM->m_tkCurrentCVOwner, $2, NULL); }
                        | _CUSTOM customType '=' compQstring                    { $$ = new CustomDescr(PASM->m_tkCurrentCVOwner, $2, $4); }
                        | _CUSTOM customType '=' '{' customBlobDescr '}'        { $$ = new CustomDescr(PASM->m_tkCurrentCVOwner, $2, $5); }
                        | customHead bytes ')'                                  { $$ = new CustomDescr(PASM->m_tkCurrentCVOwner, $1, $2); }
                        ;

customDescrWithOwner    : _CUSTOM '(' ownerType ')' customType                  { $$ = new CustomDescr($3, $5, NULL); }
                        | _CUSTOM '(' ownerType ')' customType '=' compQstring  { $$ = new CustomDescr($3, $5, $7); }
                        | _CUSTOM '(' ownerType ')' customType '=' '{' customBlobDescr '}'
                                                                                { $$ = new CustomDescr($3, $5, $8); }
                        | customHeadWithOwner bytes ')'                         { $$ = new CustomDescr(PASM->m_tkCurrentCVOwner, $1, $2); }
                        ;

customHead              : _CUSTOM customType '=' '('                            { $$ = $2; bParsingByteArray = TRUE; }
                        ;

customHeadWithOwner     : _CUSTOM '(' ownerType ')' customType '=' '('
                                                                                { PASM->m_pCustomDescrList = NULL;
                                                                                  PASM->m_tkCurrentCVOwner = $3;
                                                                                  $$ = $5; bParsingByteArray = TRUE; }
                        ;

customType              : methodRef                         { $$ = $1; }
                        ;

ownerType               : typeSpec                          { $$ = $1; }
                        | memberRef                         { $$ = $1; }
                        ;

/*  Verbal description of custom attribute initialization blob  */
customBlobDescr         : customBlobArgs customBlobNVPairs                      { $$ = $1;
                                                                                  $$->appendInt16(VAL16(nCustomBlobNVPairs));
                                                                                  $$->append($2);
                                                                                  nCustomBlobNVPairs = 0; }
                        ;

customBlobArgs          : /* EMPTY */                                           { $$ = new BinStr(); $$->appendInt16(VAL16(0x0001)); }
                        | customBlobArgs serInit                                { $$ = $1;
                                                                                  AppendFieldToCustomBlob($$,$2); }
                        | customBlobArgs compControl                            { $$ = $1; }
                        ;

customBlobNVPairs       : /* EMPTY */                                           { $$ = new BinStr(); }
                        | customBlobNVPairs fieldOrProp serializType dottedName '=' serInit
                                                                                { $$ = $1; $$->appendInt8($2);
                                                                                  $$->append($3);
                                                                                  AppendStringWithLength($$,$4);
                                                                                  AppendFieldToCustomBlob($$,$6);
                                                                                  nCustomBlobNVPairs++; }
                        | customBlobNVPairs compControl                         { $$ = $1; }
                        ;

fieldOrProp             : FIELD_                                                { $$ = SERIALIZATION_TYPE_FIELD; }
                        | PROPERTY_                                             { $$ = SERIALIZATION_TYPE_PROPERTY; }
                        ;

customAttrDecl          : customDescr                                           { if($1->tkOwner && !$1->tkInterfacePair)
                                                                                    PASM->DefineCV($1);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH($1); }
                        | customDescrWithOwner                                  { PASM->DefineCV($1); }
                        | TYPEDEF_CA                                            { CustomDescr* pNew = new CustomDescr($1->m_pCA);
                                                                                  if(pNew->tkOwner == 0) pNew->tkOwner = PASM->m_tkCurrentCVOwner;
                                                                                  if(pNew->tkOwner)
                                                                                    PASM->DefineCV(pNew);
                                                                                  else if(PASM->m_pCustomDescrList)
                                                                                    PASM->m_pCustomDescrList->PUSH(pNew); }
                        ;

serializType            : simpleType                          { $$ = $1; }
                        | TYPE_                               { $$ = new BinStr(); $$->appendInt8(SERIALIZATION_TYPE_TYPE); }
                        | OBJECT_                             { $$ = new BinStr(); $$->appendInt8(SERIALIZATION_TYPE_TAGGED_OBJECT); }
                        | ENUM_ CLASS_ SQSTRING               { $$ = new BinStr(); $$->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength($$,$3); }
                        | ENUM_ className                     { $$ = new BinStr(); $$->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                AppendStringWithLength($$,PASM->ReflectionNotation($2)); }
                        | serializType '[' ']'                { $$ = $1; $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        ;


/*  Module declaration */
moduleHead              : _MODULE                                               { PASMM->SetModuleName(NULL); PASM->m_tkCurrentCVOwner=1; }
                        | _MODULE dottedName                                    { PASMM->SetModuleName($2); PASM->m_tkCurrentCVOwner=1; }
                        | _MODULE EXTERN_ dottedName                            { BinStr* pbs = new BinStr();
                                                                                  unsigned L = (unsigned)strlen($3);
                                                                                  memcpy((char*)(pbs->getBuff(L)),$3,L);
                                                                                  PASM->EmitImport(pbs); delete pbs;}
                        ;

/*  VTable Fixup table declaration  */
vtfixupDecl             : _VTFIXUP '[' int32 ']' vtfixupAttr AT_ id             { /*PASM->SetDataSection(); PASM->EmitDataLabel($7);*/
                                                                                  PASM->m_VTFList.PUSH(new VTFEntry((USHORT)$3, (USHORT)$5, $7)); }
                        ;

vtfixupAttr             : /* EMPTY */                                           { $$ = 0; }
                        | vtfixupAttr INT32_                                    { $$ = $1 | COR_VTABLE_32BIT; }
                        | vtfixupAttr INT64_                                    { $$ = $1 | COR_VTABLE_64BIT; }
                        | vtfixupAttr FROMUNMANAGED_                            { $$ = $1 | COR_VTABLE_FROM_UNMANAGED; }
                        | vtfixupAttr CALLMOSTDERIVED_                          { $$ = $1 | COR_VTABLE_CALL_MOST_DERIVED; }
                        | vtfixupAttr RETAINAPPDOMAIN_                          { $$ = $1 | COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN; }
                        ;

vtableDecl              : vtableHead bytes ')'   /* deprecated */               { PASM->m_pVTable = $2; }
                        ;

vtableHead              : _VTABLE '=' '('        /* deprecated */               { bParsingByteArray = TRUE; }
                        ;

/*  Namespace and class declaration  */
nameSpaceHead           : _NAMESPACE dottedName                                 { PASM->StartNameSpace($2); }
                        ;

_class                  : _CLASS                                                { newclass = TRUE; }
                        ;

classHeadBegin          : _class classAttr dottedName typarsClause              { if($4) FixupConstraints();
                                                                                  PASM->StartClass($3, $2, $4);
                                                                                  TyParFixupList.RESET(false);
                                                                                  newclass = FALSE;
                                                                                }
                        ;
classHead               : classHeadBegin extendsClause implClause               { PASM->AddClass(); }
                        ;

classAttr               : /* EMPTY */                       { $$ = (CorRegTypeAttr) 0; }
                        | classAttr PUBLIC_                 { $$ = (CorRegTypeAttr) (($1 & ~tdVisibilityMask) | tdPublic); }
                        | classAttr PRIVATE_                { $$ = (CorRegTypeAttr) (($1 & ~tdVisibilityMask) | tdNotPublic); }
                        | classAttr VALUE_                  { $$ = (CorRegTypeAttr) ($1 | 0x80000000 | tdSealed); }
                        | classAttr ENUM_                   { $$ = (CorRegTypeAttr) ($1 | 0x40000000); }
                        | classAttr INTERFACE_              { $$ = (CorRegTypeAttr) ($1 | tdInterface | tdAbstract); }
                        | classAttr SEALED_                 { $$ = (CorRegTypeAttr) ($1 | tdSealed); }
                        | classAttr ABSTRACT_               { $$ = (CorRegTypeAttr) ($1 | tdAbstract); }
                        | classAttr AUTO_                   { $$ = (CorRegTypeAttr) (($1 & ~tdLayoutMask) | tdAutoLayout); }
                        | classAttr SEQUENTIAL_             { $$ = (CorRegTypeAttr) (($1 & ~tdLayoutMask) | tdSequentialLayout); }
                        | classAttr EXPLICIT_               { $$ = (CorRegTypeAttr) (($1 & ~tdLayoutMask) | tdExplicitLayout); }
                        | classAttr ANSI_                   { $$ = (CorRegTypeAttr) (($1 & ~tdStringFormatMask) | tdAnsiClass); }
                        | classAttr UNICODE_                { $$ = (CorRegTypeAttr) (($1 & ~tdStringFormatMask) | tdUnicodeClass); }
                        | classAttr AUTOCHAR_               { $$ = (CorRegTypeAttr) (($1 & ~tdStringFormatMask) | tdAutoClass); }
                        | classAttr IMPORT_                 { $$ = (CorRegTypeAttr) ($1 | tdImport); }
                        | classAttr SERIALIZABLE_           { $$ = (CorRegTypeAttr) ($1 | tdSerializable); }
                        | classAttr WINDOWSRUNTIME_         { $$ = (CorRegTypeAttr) ($1 | tdWindowsRuntime); }
                        | classAttr NESTED_ PUBLIC_         { $$ = (CorRegTypeAttr) (($1 & ~tdVisibilityMask) | tdNestedPublic); }
                        | classAttr NESTED_ PRIVATE_        { $$ = (CorRegTypeAttr) (($1 & ~tdVisibilityMask) | tdNestedPrivate); }
                        | classAttr NESTED_ FAMILY_         { $$ = (CorRegTypeAttr) (($1 & ~tdVisibilityMask) | tdNestedFamily); }
                        | classAttr NESTED_ ASSEMBLY_       { $$ = (CorRegTypeAttr) (($1 & ~tdVisibilityMask) | tdNestedAssembly); }
                        | classAttr NESTED_ FAMANDASSEM_    { $$ = (CorRegTypeAttr) (($1 & ~tdVisibilityMask) | tdNestedFamANDAssem); }
                        | classAttr NESTED_ FAMORASSEM_     { $$ = (CorRegTypeAttr) (($1 & ~tdVisibilityMask) | tdNestedFamORAssem); }
                        | classAttr BEFOREFIELDINIT_        { $$ = (CorRegTypeAttr) ($1 | tdBeforeFieldInit); }
                        | classAttr SPECIALNAME_            { $$ = (CorRegTypeAttr) ($1 | tdSpecialName); }
                        | classAttr RTSPECIALNAME_          { $$ = (CorRegTypeAttr) ($1); }
                        | classAttr FLAGS_ '(' int32 ')'    { $$ = (CorRegTypeAttr) ($4); }
                        ;

extendsClause           : /* EMPTY */
                        | EXTENDS_ typeSpec                                 { PASM->m_crExtends = $2; }
                        ;

implClause              : /* EMPTY */
                        | IMPLEMENTS_ implList
                        ;

classDecls              : /* EMPTY */
                        | classDecls classDecl
                        ;

implList                : implList ',' typeSpec             { PASM->AddToImplList($3); }
                        | typeSpec                          { PASM->AddToImplList($1); }
                                        ;

/* Generic type parameters declaration  */
typeList                : /* EMPTY */                       { $$ = new BinStr(); }
                        | typeListNotEmpty                  { $$ = $1; }
                        ;

typeListNotEmpty        : typeSpec                          { $$ = new BinStr(); $$->appendInt32($1); }
                        | typeListNotEmpty ',' typeSpec     { $$ = $1; $$->appendInt32($3); }
                        ;

typarsClause            : /* EMPTY */                       { $$ = NULL; PASM->m_TyParList = NULL;}
                        | '<' typars '>'                    { $$ = $2;   PASM->m_TyParList = $2;}
                        ;

typarAttrib             : '+'                               { $$ = gpCovariant; }
                        | '-'                               { $$ = gpContravariant; }
                        | CLASS_                            { $$ = gpReferenceTypeConstraint; }
                        | VALUETYPE_                        { $$ = gpNotNullableValueTypeConstraint; }
                        | BYREFLIKE_                        { $$ = gpAcceptByRefLike; }
                        | _CTOR                             { $$ = gpDefaultConstructorConstraint; }
                        | FLAGS_ '(' int32 ')'              { $$ = (CorGenericParamAttr)$3; }
                        ;

typarAttribs            : /* EMPTY */                       { $$ = 0; }
                        | typarAttrib typarAttribs          { $$ = $1 | $2; }
                        ;

typars                  : typarAttribs tyBound dottedName typarsRest {$$ = new TyParList($1, $2, $3, $4);}
                        | typarAttribs dottedName typarsRest   {$$ = new TyParList($1, NULL, $2, $3);}
                        ;

typarsRest              : /* EMPTY */                       { $$ = NULL; }
                        | ',' typars                        { $$ = $2; }
                        ;

tyBound                 : '(' typeList ')'                  { $$ = $2; }
                        ;

genArity                : /* EMPTY */                       { $$= 0; }
                        | genArityNotEmpty                  { $$ = $1; }
                        ;

genArityNotEmpty        : '<' '[' int32 ']' '>'             { $$ = $3; }
                        ;

/*  Class body declarations  */
classDecl               : methodHead  methodDecls '}'       { if(PASM->m_pCurMethod->m_ulLines[1] ==0)
                                                              {  PASM->m_pCurMethod->m_ulLines[1] = PASM->m_ulCurLine;
                                                                 PASM->m_pCurMethod->m_ulColumns[1]=PASM->m_ulCurColumn;}
                                                              PASM->EndMethod(); }
                        | classHead '{' classDecls '}'      { PASM->EndClass(); }
                        | eventHead '{' eventDecls '}'      { PASM->EndEvent(); }
                        | propHead '{' propDecls '}'        { PASM->EndProp(); }
                        | fieldDecl
                        | dataDecl
                        | secDecl
                        | extSourceSpec
                        | customAttrDecl
                        | _SIZE int32                           { PASM->m_pCurClass->m_ulSize = $2; }
                        | _PACK int32                           { PASM->m_pCurClass->m_ulPack = $2; }
                        | exportHead '{' exptypeDecls '}'       { PASMM->EndComType(); }
                        | _OVERRIDE typeSpec DCOLON methodName WITH_ callConv type typeSpec DCOLON methodName '(' sigArgs0 ')'
                                                                { BinStr *sig1 = parser->MakeSig($6, $7, $12);
                                                                  BinStr *sig2 = new BinStr(); sig2->append(sig1);
                                                                  PASM->AddMethodImpl($2,$4,sig1,$8,$10,sig2);
                                                                  PASM->ResetArgNameList();
                                                                }
                        | _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')' WITH_ METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'
                                                                 { PASM->AddMethodImpl($5,$7,
                                                                      ($8==0 ? parser->MakeSig($3,$4,$10) :
                                                                      parser->MakeSig($3| IMAGE_CEE_CS_CALLCONV_GENERIC,$4,$10,$8)),
                                                                      $16,$18,
                                                                      ($19==0 ? parser->MakeSig($14,$15,$21) :
                                                                      parser->MakeSig($14| IMAGE_CEE_CS_CALLCONV_GENERIC,$15,$21,$19)));
                                                                   PASM->ResetArgNameList();
                                                                 }
                        | languageDecl
                        | compControl
                        | _PARAM TYPE_ '[' int32 ']'        { if(($4 > 0) && ($4 <= (int)PASM->m_pCurClass->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[$4-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
                        | _PARAM TYPE_ dottedName           { int n = PASM->m_pCurClass->FindTyPar($3);
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurClass->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",$3);
                                                            }
                        | _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec { PASM->AddGenericParamConstraint($4, 0, $7); }
                        | _PARAM CONSTRAINT_ dottedName ',' typeSpec    { PASM->AddGenericParamConstraint(0, $3, $5); }
                        | _INTERFACEIMPL TYPE_ typeSpec customDescr   { $4->tkInterfacePair = $3;
                                                                        if(PASM->m_pCustomDescrList)
                                                                            PASM->m_pCustomDescrList->PUSH($4);
                                                                      }
                        ;

/*  Field declaration  */
fieldDecl               : _FIELD repeatOpt fieldAttr type dottedName atOpt initOpt
                                                            { $4->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                              PASM->AddField($5, $4, $3, $6, $7, $2); }
                        ;

fieldAttr               : /* EMPTY */                       { $$ = (CorFieldAttr) 0; }
                        | fieldAttr STATIC_                 { $$ = (CorFieldAttr) ($1 | fdStatic); }
                        | fieldAttr PUBLIC_                 { $$ = (CorFieldAttr) (($1 & ~mdMemberAccessMask) | fdPublic); }
                        | fieldAttr PRIVATE_                { $$ = (CorFieldAttr) (($1 & ~mdMemberAccessMask) | fdPrivate); }
                        | fieldAttr FAMILY_                 { $$ = (CorFieldAttr) (($1 & ~mdMemberAccessMask) | fdFamily); }
                        | fieldAttr INITONLY_               { $$ = (CorFieldAttr) ($1 | fdInitOnly); }
                        | fieldAttr RTSPECIALNAME_          { $$ = $1; } /*{ $$ = (CorFieldAttr) ($1 | fdRTSpecialName); }*/
                        | fieldAttr SPECIALNAME_            { $$ = (CorFieldAttr) ($1 | fdSpecialName); }
                                                /* <STRIP>commented out because PInvoke for fields is not supported by EE
                        | fieldAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'
                                                            { $$ = (CorFieldAttr) ($1 | fdPinvokeImpl);
                                                              PASM->SetPinvoke($4,0,$6,$7); }
                        | fieldAttr PINVOKEIMPL_ '(' compQstring  pinvAttr ')'
                                                            { $$ = (CorFieldAttr) ($1 | fdPinvokeImpl);
                                                              PASM->SetPinvoke($4,0,NULL,$5); }
                        | fieldAttr PINVOKEIMPL_ '(' pinvAttr ')'
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,$4);
                                                              $$ = (CorFieldAttr) ($1 | fdPinvokeImpl); }
                                                </STRIP>*/
                        | fieldAttr MARSHAL_ '(' marshalBlob ')'
                                                            { PASM->m_pMarshal = $4; }
                        | fieldAttr ASSEMBLY_               { $$ = (CorFieldAttr) (($1 & ~mdMemberAccessMask) | fdAssembly); }
                        | fieldAttr FAMANDASSEM_            { $$ = (CorFieldAttr) (($1 & ~mdMemberAccessMask) | fdFamANDAssem); }
                        | fieldAttr FAMORASSEM_             { $$ = (CorFieldAttr) (($1 & ~mdMemberAccessMask) | fdFamORAssem); }
                        | fieldAttr PRIVATESCOPE_           { $$ = (CorFieldAttr) (($1 & ~mdMemberAccessMask) | fdPrivateScope); }
                        | fieldAttr LITERAL_                { $$ = (CorFieldAttr) ($1 | fdLiteral); }
                        | fieldAttr NOTSERIALIZED_          { $$ = (CorFieldAttr) ($1 | fdNotSerialized); }
                        | fieldAttr FLAGS_ '(' int32 ')'    { $$ = (CorFieldAttr) ($4); }
                        ;

atOpt                   : /* EMPTY */                       { $$ = 0; }
                        | AT_ id                            { $$ = $2; }
                        ;

initOpt                 : /* EMPTY */                       { $$ = NULL; }
                        | '=' fieldInit                     { $$ = $2; }
                                                ;

repeatOpt               : /* EMPTY */                       { $$ = 0xFFFFFFFF; }
                        | '[' int32 ']'                     { $$ = $2; }
                                                ;

/*  Method referencing  */
methodRef               : callConv type typeSpec DCOLON methodName tyArgs0 '(' sigArgs0 ')'
                                                             { PASM->ResetArgNameList();
                                                               if ($6 == NULL)
                                                               {
                                                                 if((iCallConv)&&(($1 & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                                 $$ = PASM->MakeMemberRef($3, $5, parser->MakeSig($1|iCallConv, $2, $8));
                                                               }
                                                               else
                                                               {
                                                                 mdToken mr;
                                                                 if((iCallConv)&&(($1 & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                                 mr = PASM->MakeMemberRef($3, $5,
                                                                   parser->MakeSig($1 | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, $2, $8, corCountArgs($6)));
                                                                 $$ = PASM->MakeMethodSpec(mr,
                                                                   parser->MakeSig(IMAGE_CEE_CS_CALLCONV_INSTANTIATION, 0, $6));
                                                               }
                                                             }
                        | callConv type typeSpec DCOLON methodName genArityNotEmpty '(' sigArgs0 ')'
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(($1 & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               $$ = PASM->MakeMemberRef($3, $5,
                                                                 parser->MakeSig($1 | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, $2, $8, $6));
                                                             }
                        | callConv type methodName tyArgs0 '(' sigArgs0 ')'
                                                             { PASM->ResetArgNameList();
                                                               if ($4 == NULL)
                                                               {
                                                                 if((iCallConv)&&(($1 & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                                 $$ = PASM->MakeMemberRef(mdTokenNil, $3, parser->MakeSig($1|iCallConv, $2, $6));
                                                               }
                                                               else
                                                               {
                                                                 mdToken mr;
                                                                 if((iCallConv)&&(($1 & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                                 mr = PASM->MakeMemberRef(mdTokenNil, $3, parser->MakeSig($1 | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, $2, $6, corCountArgs($4)));
                                                                 $$ = PASM->MakeMethodSpec(mr,
                                                                   parser->MakeSig(IMAGE_CEE_CS_CALLCONV_INSTANTIATION, 0, $4));
                                                               }
                                                             }
                        | callConv type methodName genArityNotEmpty '(' sigArgs0 ')'
                                                             { PASM->ResetArgNameList();
                                                               if((iCallConv)&&(($1 & iCallConv) != iCallConv)) parser->warn("'instance' added to method's calling convention\n");
                                                               $$ = PASM->MakeMemberRef(mdTokenNil, $3, parser->MakeSig($1 | IMAGE_CEE_CS_CALLCONV_GENERIC|iCallConv, $2, $6, $4));
                                                             }
                        | mdtoken                            { $$ = $1; }
                        | TYPEDEF_M                          { $$ = $1->m_tkTypeSpec; }
                        | TYPEDEF_MR                         { $$ = $1->m_tkTypeSpec; }
                        ;

callConv                : INSTANCE_ callConv                  { $$ = ($2 | IMAGE_CEE_CS_CALLCONV_HASTHIS); }
                        | EXPLICIT_ callConv                  { $$ = ($2 | IMAGE_CEE_CS_CALLCONV_EXPLICITTHIS); }
                        | callKind                            { $$ = $1; }
                        | CALLCONV_ '(' int32 ')'             { $$ = $3; }
                        ;

callKind                : /* EMPTY */                         { $$ = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
                        | DEFAULT_                            { $$ = IMAGE_CEE_CS_CALLCONV_DEFAULT; }
                        | VARARG_                             { $$ = IMAGE_CEE_CS_CALLCONV_VARARG; }
                        | UNMANAGED_ CDECL_                   { $$ = IMAGE_CEE_CS_CALLCONV_C; }
                        | UNMANAGED_ STDCALL_                 { $$ = IMAGE_CEE_CS_CALLCONV_STDCALL; }
                        | UNMANAGED_ THISCALL_                { $$ = IMAGE_CEE_CS_CALLCONV_THISCALL; }
                        | UNMANAGED_ FASTCALL_                { $$ = IMAGE_CEE_CS_CALLCONV_FASTCALL; }
                        | UNMANAGED_                          { $$ = IMAGE_CEE_CS_CALLCONV_UNMANAGED; }
                        ;

mdtoken                 : MDTOKEN_ '(' int32 ')'             { $$ = $3; }
                        ;

memberRef               : methodSpec methodRef               { $$ = $2;
                                                               PASM->delArgNameList(PASM->m_firstArgName);
                                                               PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                               PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                               PASM->SetMemberRefFixup($2,iOpcodeLen); }
                        | FIELD_ type typeSpec DCOLON dottedName
                                                             { $2->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               $$ = PASM->MakeMemberRef($3, $5, $2);
                                                               PASM->SetMemberRefFixup($$,iOpcodeLen); }
                        | FIELD_ type dottedName
                                                             { $2->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               $$ = PASM->MakeMemberRef(NULL, $3, $2);
                                                               PASM->SetMemberRefFixup($$,iOpcodeLen); }
                        | FIELD_ TYPEDEF_F                   { $$ = $2->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup($$,iOpcodeLen); }
                        | FIELD_ TYPEDEF_MR                  { $$ = $2->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup($$,iOpcodeLen); }
                        | mdtoken                            { $$ = $1;
                                                               PASM->SetMemberRefFixup($$,iOpcodeLen); }
                        ;

/*  Event declaration  */
eventHead               : _EVENT eventAttr typeSpec dottedName   { PASM->ResetEvent($4, $3, $2); }
                        | _EVENT eventAttr dottedName            { PASM->ResetEvent($3, mdTypeRefNil, $2); }
                        ;


eventAttr               : /* EMPTY */                       { $$ = (CorEventAttr) 0; }
                        | eventAttr RTSPECIALNAME_          { $$ = $1; }/*{ $$ = (CorEventAttr) ($1 | evRTSpecialName); }*/
                        | eventAttr SPECIALNAME_            { $$ = (CorEventAttr) ($1 | evSpecialName); }
                        ;

eventDecls              : /* EMPTY */
                        | eventDecls eventDecl
                        ;

eventDecl               : _ADDON methodRef                 { PASM->SetEventMethod(0, $2); }
                        | _REMOVEON methodRef              { PASM->SetEventMethod(1, $2); }
                        | _FIRE methodRef                  { PASM->SetEventMethod(2, $2); }
                        | _OTHER methodRef                 { PASM->SetEventMethod(3, $2); }
                        | extSourceSpec
                        | customAttrDecl
                        | languageDecl
                        | compControl
                        ;

/*  Property declaration  */
propHead                : _PROPERTY propAttr callConv type dottedName '(' sigArgs0 ')' initOpt
                                                            { PASM->ResetProp($5,
                                                              parser->MakeSig((IMAGE_CEE_CS_CALLCONV_PROPERTY |
                                                              ($3 & IMAGE_CEE_CS_CALLCONV_HASTHIS)),$4,$7), $2, $9);}
                        ;

propAttr                : /* EMPTY */                       { $$ = (CorPropertyAttr) 0; }
                        | propAttr RTSPECIALNAME_           { $$ = $1; }/*{ $$ = (CorPropertyAttr) ($1 | prRTSpecialName); }*/
                        | propAttr SPECIALNAME_             { $$ = (CorPropertyAttr) ($1 | prSpecialName); }
                        ;

propDecls               : /* EMPTY */
                        | propDecls propDecl
                        ;


propDecl                : _SET methodRef                    { PASM->SetPropMethod(0, $2); }
                        | _GET methodRef                    { PASM->SetPropMethod(1, $2); }
                        | _OTHER methodRef                  { PASM->SetPropMethod(2, $2); }
                        | customAttrDecl
                        | extSourceSpec
                        | languageDecl
                        | compControl
                        ;

/*  Method declaration  */
methodHeadPart1         : _METHOD                           { PASM->ResetForNextMethod();
                                                              uMethodBeginLine = PASM->m_ulCurLine;
                                                              uMethodBeginColumn=PASM->m_ulCurColumn;
                                                            }
                        ;

marshalClause           : /* EMPTY */                       { $$ = NULL; }
                        | MARSHAL_ '(' marshalBlob ')'       { $$ = $3; }
                        ;

marshalBlob             : nativeType                        { $$ = $1; }
                        | marshalBlobHead hexbytes '}'       { $$ = $2; }
                        ;

marshalBlobHead         : '{'                                { bParsingByteArray = TRUE; }
                        ;

methodHead              : methodHeadPart1 methAttr callConv paramAttr type marshalClause methodName typarsClause'(' sigArgs0 ')' implAttr '{'
                                                            { BinStr* sig;
                                                              if ($8 == NULL) sig = parser->MakeSig($3, $5, $10);
                                                              else {
                                                               FixupTyPars($5);
                                                               sig = parser->MakeSig($3 | IMAGE_CEE_CS_CALLCONV_GENERIC, $5, $10, $8->Count());
                                                               FixupConstraints();
                                                              }
                                                              PASM->StartMethod($7, sig, $2, $6, $4, $8);
                                                              TyParFixupList.RESET(false);
                                                              PASM->SetImplAttr((USHORT)$12);
                                                              PASM->m_pCurMethod->m_ulLines[0] = uMethodBeginLine;
                                                              PASM->m_pCurMethod->m_ulColumns[0]=uMethodBeginColumn;
                                                            }
                        ;

methAttr                : /* EMPTY */                       { $$ = (CorMethodAttr) 0; }
                        | methAttr STATIC_                  { $$ = (CorMethodAttr) ($1 | mdStatic); }
                        | methAttr PUBLIC_                  { $$ = (CorMethodAttr) (($1 & ~mdMemberAccessMask) | mdPublic); }
                        | methAttr PRIVATE_                 { $$ = (CorMethodAttr) (($1 & ~mdMemberAccessMask) | mdPrivate); }
                        | methAttr FAMILY_                  { $$ = (CorMethodAttr) (($1 & ~mdMemberAccessMask) | mdFamily); }
                        | methAttr FINAL_                   { $$ = (CorMethodAttr) ($1 | mdFinal); }
                        | methAttr SPECIALNAME_             { $$ = (CorMethodAttr) ($1 | mdSpecialName); }
                        | methAttr VIRTUAL_                 { $$ = (CorMethodAttr) ($1 | mdVirtual); }
                        | methAttr STRICT_                  { $$ = (CorMethodAttr) ($1 | mdCheckAccessOnOverride); }
                        | methAttr ABSTRACT_                { $$ = (CorMethodAttr) ($1 | mdAbstract); }
                        | methAttr ASSEMBLY_                { $$ = (CorMethodAttr) (($1 & ~mdMemberAccessMask) | mdAssem); }
                        | methAttr FAMANDASSEM_             { $$ = (CorMethodAttr) (($1 & ~mdMemberAccessMask) | mdFamANDAssem); }
                        | methAttr FAMORASSEM_              { $$ = (CorMethodAttr) (($1 & ~mdMemberAccessMask) | mdFamORAssem); }
                        | methAttr PRIVATESCOPE_            { $$ = (CorMethodAttr) (($1 & ~mdMemberAccessMask) | mdPrivateScope); }
                        | methAttr HIDEBYSIG_               { $$ = (CorMethodAttr) ($1 | mdHideBySig); }
                        | methAttr NEWSLOT_                 { $$ = (CorMethodAttr) ($1 | mdNewSlot); }
                        | methAttr RTSPECIALNAME_           { $$ = $1; }/*{ $$ = (CorMethodAttr) ($1 | mdRTSpecialName); }*/
                        | methAttr UNMANAGEDEXP_            { $$ = (CorMethodAttr) ($1 | mdUnmanagedExport); }
                        | methAttr REQSECOBJ_               { $$ = (CorMethodAttr) ($1 | mdRequireSecObject); }
                        | methAttr FLAGS_ '(' int32 ')'     { $$ = (CorMethodAttr) ($4); }
                        | methAttr PINVOKEIMPL_ '(' compQstring AS_ compQstring pinvAttr ')'
                                                            { PASM->SetPinvoke($4,0,$6,$7);
                                                              $$ = (CorMethodAttr) ($1 | mdPinvokeImpl); }
                        | methAttr PINVOKEIMPL_ '(' compQstring  pinvAttr ')'
                                                            { PASM->SetPinvoke($4,0,NULL,$5);
                                                              $$ = (CorMethodAttr) ($1 | mdPinvokeImpl); }
                        | methAttr PINVOKEIMPL_ '(' pinvAttr ')'
                                                            { PASM->SetPinvoke(new BinStr(),0,NULL,$4);
                                                              $$ = (CorMethodAttr) ($1 | mdPinvokeImpl); }
                        ;

pinvAttr                : /* EMPTY */                       { $$ = (CorPinvokeMap) 0; }
                        | pinvAttr NOMANGLE_                { $$ = (CorPinvokeMap) ($1 | pmNoMangle); }
                        | pinvAttr ANSI_                    { $$ = (CorPinvokeMap) ($1 | pmCharSetAnsi); }
                        | pinvAttr UNICODE_                 { $$ = (CorPinvokeMap) ($1 | pmCharSetUnicode); }
                        | pinvAttr AUTOCHAR_                { $$ = (CorPinvokeMap) ($1 | pmCharSetAuto); }
                        | pinvAttr LASTERR_                 { $$ = (CorPinvokeMap) ($1 | pmSupportsLastError); }
                        | pinvAttr WINAPI_                  { $$ = (CorPinvokeMap) ($1 | pmCallConvWinapi); }
                        | pinvAttr CDECL_                   { $$ = (CorPinvokeMap) ($1 | pmCallConvCdecl); }
                        | pinvAttr STDCALL_                 { $$ = (CorPinvokeMap) ($1 | pmCallConvStdcall); }
                        | pinvAttr THISCALL_                { $$ = (CorPinvokeMap) ($1 | pmCallConvThiscall); }
                        | pinvAttr FASTCALL_                { $$ = (CorPinvokeMap) ($1 | pmCallConvFastcall); }
                        | pinvAttr BESTFIT_ ':' ON_         { $$ = (CorPinvokeMap) ($1 | pmBestFitEnabled); }
                        | pinvAttr BESTFIT_ ':' OFF_        { $$ = (CorPinvokeMap) ($1 | pmBestFitDisabled); }
                        | pinvAttr CHARMAPERROR_ ':' ON_    { $$ = (CorPinvokeMap) ($1 | pmThrowOnUnmappableCharEnabled); }
                        | pinvAttr CHARMAPERROR_ ':' OFF_   { $$ = (CorPinvokeMap) ($1 | pmThrowOnUnmappableCharDisabled); }
                        | pinvAttr FLAGS_ '(' int32 ')'     { $$ = (CorPinvokeMap) ($4); }
                        ;

methodName              : _CTOR                             { $$ = newString(COR_CTOR_METHOD_NAME); }
                        | _CCTOR                            { $$ = newString(COR_CCTOR_METHOD_NAME); }
                        | dottedName                        { $$ = $1; }
                        ;

paramAttr               : /* EMPTY */                       { $$ = 0; }
                        | paramAttr '[' IN_ ']'             { $$ = $1 | pdIn; }
                        | paramAttr '[' OUT_ ']'            { $$ = $1 | pdOut; }
                        | paramAttr '[' OPT_ ']'            { $$ = $1 | pdOptional; }
                        | paramAttr '[' int32 ']'           { $$ = $3 + 1; }
                        ;

implAttr                : /* EMPTY */                       { $$ = (CorMethodImpl) (miIL | miManaged); }
                        | implAttr NATIVE_                  { $$ = (CorMethodImpl) (($1 & 0xFFF4) | miNative); }
                        | implAttr CIL_                     { $$ = (CorMethodImpl) (($1 & 0xFFF4) | miIL); }
                        | implAttr OPTIL_                   { $$ = (CorMethodImpl) (($1 & 0xFFF4) | miOPTIL); }
                        | implAttr MANAGED_                 { $$ = (CorMethodImpl) (($1 & 0xFFFB) | miManaged); }
                        | implAttr UNMANAGED_               { $$ = (CorMethodImpl) (($1 & 0xFFFB) | miUnmanaged); }
                        | implAttr FORWARDREF_              { $$ = (CorMethodImpl) ($1 | miForwardRef); }
                        | implAttr PRESERVESIG_             { $$ = (CorMethodImpl) ($1 | miPreserveSig); }
                        | implAttr RUNTIME_                 { $$ = (CorMethodImpl) ($1 | miRuntime); }
                        | implAttr INTERNALCALL_            { $$ = (CorMethodImpl) ($1 | miInternalCall); }
                        | implAttr SYNCHRONIZED_            { $$ = (CorMethodImpl) ($1 | miSynchronized); }
                        | implAttr NOINLINING_              { $$ = (CorMethodImpl) ($1 | miNoInlining); }
                        | implAttr AGGRESSIVEINLINING_      { $$ = (CorMethodImpl) ($1 | miAggressiveInlining); }
                        | implAttr NOOPTIMIZATION_          { $$ = (CorMethodImpl) ($1 | miNoOptimization); }
                        | implAttr AGGRESSIVEOPTIMIZATION_  { $$ = (CorMethodImpl) ($1 | miAggressiveOptimization); }
                        | implAttr FLAGS_ '(' int32 ')'     { $$ = (CorMethodImpl) ($4); }
                        ;

localsHead              : _LOCALS                           { PASM->delArgNameList(PASM->m_firstArgName); PASM->m_firstArgName = NULL;PASM->m_lastArgName = NULL;
                                                            }
                        ;

methodDecls             : /* EMPTY */
                        | methodDecls methodDecl
                        ;

methodDecl              : _EMITBYTE int32                   { PASM->EmitByte($2); }
                        | sehBlock                          { delete PASM->m_SEHD; PASM->m_SEHD = PASM->m_SEHDstack.POP(); }
                        | _MAXSTACK int32                   { PASM->EmitMaxStack($2); }
                        | localsHead '(' sigArgs0 ')'       { PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, $3));
                                                            }
                        | localsHead INIT_ '(' sigArgs0 ')' { PASM->EmitZeroInit();
                                                              PASM->EmitLocals(parser->MakeSig(IMAGE_CEE_CS_CALLCONV_LOCAL_SIG, 0, $4));
                                                            }
                        | _ENTRYPOINT                       { PASM->EmitEntryPoint(); }
                        | _ZEROINIT                         { PASM->EmitZeroInit(); }
                        | dataDecl
                        | instr
                        | id ':'                            { PASM->AddLabel(PASM->m_CurPC,$1); /*PASM->EmitLabel($1);*/ }
                        | secDecl
                        | extSourceSpec
                        | languageDecl
                        | customAttrDecl
                        | compControl
                        | _EXPORT '[' int32 ']'             { if(PASM->m_pCurMethod->m_dwExportOrdinal == 0xFFFFFFFF)
                                                              {
                                                                PASM->m_pCurMethod->m_dwExportOrdinal = $3;
                                                                PASM->m_pCurMethod->m_szExportAlias = NULL;
                                                                if(PASM->m_pCurMethod->m_wVTEntry == 0) PASM->m_pCurMethod->m_wVTEntry = 1;
                                                                if(PASM->m_pCurMethod->m_wVTSlot  == 0) PASM->m_pCurMethod->m_wVTSlot = (WORD)($3 + 0x8000);
                                                              }
                                                              else
                                                                PASM->report->warn("Duplicate .export directive, ignored\n");
                                                            }
                        | _EXPORT '[' int32 ']' AS_ id      { if(PASM->m_pCurMethod->m_dwExportOrdinal == 0xFFFFFFFF)
                                                              {
                                                                PASM->m_pCurMethod->m_dwExportOrdinal = $3;
                                                                PASM->m_pCurMethod->m_szExportAlias = $6;
                                                                if(PASM->m_pCurMethod->m_wVTEntry == 0) PASM->m_pCurMethod->m_wVTEntry = 1;
                                                                if(PASM->m_pCurMethod->m_wVTSlot  == 0) PASM->m_pCurMethod->m_wVTSlot = (WORD)($3 + 0x8000);
                                                              }
                                                              else
                                                                PASM->report->warn("Duplicate .export directive, ignored\n");
                                                            }
                        | _VTENTRY int32 ':' int32          { PASM->m_pCurMethod->m_wVTEntry = (WORD)$2;
                                                              PASM->m_pCurMethod->m_wVTSlot = (WORD)$4; }
                        | _OVERRIDE typeSpec DCOLON methodName
                                                            { PASM->AddMethodImpl($2,$4,NULL,NULL,NULL,NULL); }

                        | _OVERRIDE METHOD_ callConv type typeSpec DCOLON methodName genArity '(' sigArgs0 ')'
                                                            { PASM->AddMethodImpl($5,$7,
                                                              ($8==0 ? parser->MakeSig($3,$4,$10) :
                                                              parser->MakeSig($3| IMAGE_CEE_CS_CALLCONV_GENERIC,$4,$10,$8))
                                                              ,NULL,NULL,NULL);
                                                              PASM->ResetArgNameList();
                                                            }
                        | scopeBlock
                        | _PARAM TYPE_ '[' int32 ']'        { if(($4 > 0) && ($4 <= (int)PASM->m_pCurMethod->m_NumTyPars))
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[$4-1].CAList();
                                                              else
                                                                PASM->report->error("Type parameter index out of range\n");
                                                            }
                        | _PARAM TYPE_ dottedName           { int n = PASM->m_pCurMethod->FindTyPar($3);
                                                              if(n >= 0)
                                                                PASM->m_pCustomDescrList = PASM->m_pCurMethod->m_TyPars[n].CAList();
                                                              else
                                                                PASM->report->error("Type parameter '%s' undefined\n",$3);
                                                            }
                        | _PARAM CONSTRAINT_ '[' int32 ']' ',' typeSpec { PASM->m_pCurMethod->AddGenericParamConstraint($4, 0, $7); }
                        | _PARAM CONSTRAINT_ dottedName ',' typeSpec    { PASM->m_pCurMethod->AddGenericParamConstraint(0, $3, $5); }

                        | _PARAM '[' int32 ']' initOpt
                                                            { if( $3 ) {
                                                                ARG_NAME_LIST* pAN=PASM->findArg(PASM->m_pCurMethod->m_firstArgName, $3 - 1);
                                                                if(pAN)
                                                                {
                                                                    PASM->m_pCustomDescrList = &(pAN->CustDList);
                                                                    pAN->pValue = $5;
                                                                }
                                                                else
                                                                {
                                                                    PASM->m_pCustomDescrList = NULL;
                                                                    if($5) delete $5;
                                                                }
                                                              } else {
                                                                PASM->m_pCustomDescrList = &(PASM->m_pCurMethod->m_RetCustDList);
                                                                PASM->m_pCurMethod->m_pRetValue = $5;
                                                              }
                                                              PASM->m_tkCurrentCVOwner = 0;
                                                            }
                        ;

scopeBlock              : scopeOpen methodDecls '}'         { PASM->m_pCurMethod->CloseScope(); }
                        ;

scopeOpen               : '{'                               { PASM->m_pCurMethod->OpenScope(); }
                        ;

/* Structured exception handling directives  */
sehBlock                : tryBlock sehClauses
                        ;

sehClauses              : sehClause sehClauses
                        | sehClause
                        ;

tryBlock                : tryHead scopeBlock                { PASM->m_SEHD->tryTo = PASM->m_CurPC; }
                        | tryHead id TO_ id                 { PASM->SetTryLabels($2, $4); }
                        | tryHead int32 TO_ int32           { if(PASM->m_SEHD) {PASM->m_SEHD->tryFrom = $2;
                                                              PASM->m_SEHD->tryTo = $4;} }
                        ;

tryHead                 : _TRY                              { PASM->NewSEHDescriptor();
                                                              PASM->m_SEHD->tryFrom = PASM->m_CurPC; }
                        ;


sehClause               : catchClause handlerBlock           { PASM->EmitTry(); }
                        | filterClause handlerBlock          { PASM->EmitTry(); }
                        | finallyClause handlerBlock         { PASM->EmitTry(); }
                        | faultClause handlerBlock           { PASM->EmitTry(); }
                        ;


filterClause            : filterHead scopeBlock              { PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
                        | filterHead id                      { PASM->SetFilterLabel($2);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
                        | filterHead int32                   { PASM->m_SEHD->sehFilter = $2;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
                        ;

filterHead              : FILTER_                            { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FILTER;
                                                               PASM->m_SEHD->sehFilter = PASM->m_CurPC; }
                        ;

catchClause             : CATCH_ typeSpec                   {  PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_NONE;
                                                               PASM->SetCatchClass($2);
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
                        ;

finallyClause           : FINALLY_                           { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FINALLY;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
                        ;

faultClause             : FAULT_                             { PASM->m_SEHD->sehClause = COR_ILEXCEPTION_CLAUSE_FAULT;
                                                               PASM->m_SEHD->sehHandler = PASM->m_CurPC; }
                        ;

handlerBlock            : scopeBlock                         { PASM->m_SEHD->sehHandlerTo = PASM->m_CurPC; }
                        | HANDLER_ id TO_ id                 { PASM->SetHandlerLabels($2, $4); }
                        | HANDLER_ int32 TO_ int32           { PASM->m_SEHD->sehHandler = $2;
                                                               PASM->m_SEHD->sehHandlerTo = $4; }
                        ;

/*  Data declaration  */
dataDecl                : ddHead ddBody
                        ;

ddHead                  : _DATA tls id '='                   { PASM->EmitDataLabel($3); }
                        | _DATA tls
                        ;

tls                     : /* EMPTY */                        { PASM->SetDataSection(); }
                        | TLS_                               { PASM->SetTLSSection(); }
                        | CIL_                               { PASM->SetILSection(); }
                        ;

ddBody                  : '{' ddItemList '}'
                        | ddItem
                        ;

ddItemList              : ddItem ',' ddItemList
                        | ddItem
                        ;

ddItemCount             : /* EMPTY */                        { $$ = 1; }
                        | '[' int32 ']'                      { $$ = $2;
                                                               if($2 <= 0) { PASM->report->error("Illegal item count: %d\n",$2);
                                                                  if(!PASM->OnErrGo) $$ = 1; }}
                        ;

ddItem                  : CHAR_ '*' '(' compQstring ')'      { PASM->EmitDataString($4); }
                        | '&' '(' id ')'                     { PASM->EmitDD($3); }
                        | bytearrayhead bytes ')'            { PASM->EmitData($2->ptr(),$2->length()); }
                        | FLOAT32_ '(' float64 ')' ddItemCount
                                                             { float f = (float) (*$3); float* p = new (nothrow) float[$5];
                                                               if(p != NULL) {
                                                                 for(int i=0; i < $5; i++) p[i] = f;
                                                                 PASM->EmitData(p, sizeof(float)*$5); delete $3; delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(float)*$5); }
                        | FLOAT64_ '(' float64 ')' ddItemCount
                                                             { double* p = new (nothrow) double[$5];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<$5; i++) p[i] = *($3);
                                                                 PASM->EmitData(p, sizeof(double)*$5); delete $3; delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(double)*$5); }
                        | INT64_ '(' int64 ')' ddItemCount
                                                             { __int64* p = new (nothrow) __int64[$5];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<$5; i++) p[i] = *($3);
                                                                 PASM->EmitData(p, sizeof(__int64)*$5); delete $3; delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int64)*$5); }
                        | INT32_ '(' int32 ')' ddItemCount
                                                             { __int32* p = new (nothrow) __int32[$5];
                                                               if(p != NULL) {
                                                                 for(int i=0; i<$5; i++) p[i] = $3;
                                                                 PASM->EmitData(p, sizeof(__int32)*$5); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int32)*$5); }
                        | INT16_ '(' int32 ')' ddItemCount
                                                             { __int16 i = (__int16) $3; FAIL_UNLESS(i == $3, ("Value %d too big\n", $3));
                                                               __int16* p = new (nothrow) __int16[$5];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<$5; j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(__int16)*$5); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int16)*$5); }
                        | INT8_ '(' int32 ')' ddItemCount
                                                             { __int8 i = (__int8) $3; FAIL_UNLESS(i == $3, ("Value %d too big\n", $3));
                                                               __int8* p = new (nothrow) __int8[$5];
                                                               if(p != NULL) {
                                                                 for(int j=0; j<$5; j++) p[j] = i;
                                                                 PASM->EmitData(p, sizeof(__int8)*$5); delete [] p;
                                                               } else PASM->report->error("Out of memory emitting data block %d bytes\n",
                                                                     sizeof(__int8)*$5); }
                        | FLOAT32_ ddItemCount               { PASM->EmitData(NULL, sizeof(float)*$2); }
                        | FLOAT64_ ddItemCount               { PASM->EmitData(NULL, sizeof(double)*$2); }
                        | INT64_ ddItemCount                 { PASM->EmitData(NULL, sizeof(__int64)*$2); }
                        | INT32_ ddItemCount                 { PASM->EmitData(NULL, sizeof(__int32)*$2); }
                        | INT16_ ddItemCount                 { PASM->EmitData(NULL, sizeof(__int16)*$2); }
                        | INT8_ ddItemCount                  { PASM->EmitData(NULL, sizeof(__int8)*$2); }
                        ;

/*  Default values declaration for fields, parameters and verbal form of CA blob description  */
fieldSerInit            : FLOAT32_ '(' float64 ')'           { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_R4);
                                                               float f = (float)(*$3);
                                                               $$->appendInt32(*((__int32*)&f)); delete $3; }
                        | FLOAT64_ '(' float64 ')'           { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_R8);
                                                               $$->appendInt64((__int64 *)$3); delete $3; }
                        | FLOAT32_ '(' int32 ')'             { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_R4);
                                                               $$->appendInt32($3); }
                        | FLOAT64_ '(' int64 ')'             { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_R8);
                                                               $$->appendInt64((__int64 *)$3); delete $3; }
                        | INT64_ '(' int64 ')'               { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I8);
                                                               $$->appendInt64((__int64 *)$3); delete $3; }
                        | INT32_ '(' int32 ')'               { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I4);
                                                               $$->appendInt32($3); }
                        | INT16_ '(' int32 ')'               { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I2);
                                                               $$->appendInt16($3); }
                        | INT8_ '(' int32 ')'                { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I1);
                                                               $$->appendInt8($3); }
                        | UNSIGNED_ INT64_ '(' int64 ')'     { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U8);
                                                               $$->appendInt64((__int64 *)$4); delete $4; }
                        | UNSIGNED_ INT32_ '(' int32 ')'     { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U4);
                                                               $$->appendInt32($4); }
                        | UNSIGNED_ INT16_ '(' int32 ')'     { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U2);
                                                               $$->appendInt16($4); }
                        | UNSIGNED_ INT8_ '(' int32 ')'      { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U1);
                                                               $$->appendInt8($4); }
                        | UINT64_ '(' int64 ')'              { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U8);
                                                               $$->appendInt64((__int64 *)$3); delete $3; }
                        | UINT32_ '(' int32 ')'              { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U4);
                                                               $$->appendInt32($3); }
                        | UINT16_ '(' int32 ')'              { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U2);
                                                               $$->appendInt16($3); }
                        | UINT8_ '(' int32 ')'               { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U1);
                                                               $$->appendInt8($3); }
                        | CHAR_ '(' int32 ')'                { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_CHAR);
                                                               $$->appendInt16($3); }
                        | BOOL_ '(' truefalse ')'            { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_BOOLEAN);
                                                               $$->appendInt8($3);}
                        | bytearrayhead bytes ')'            { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_STRING);
                                                               $$->append($2); delete $2;}
                        ;

bytearrayhead           : BYTEARRAY_ '('                     { bParsingByteArray = TRUE; }
                        ;

bytes                   : /* EMPTY */                        { $$ = new BinStr(); }
                        | hexbytes                           { $$ = $1; }
                        ;

hexbytes                : HEXBYTE                            { __int8 i = (__int8) $1; $$ = new BinStr(); $$->appendInt8(i); }
                        | hexbytes HEXBYTE                   { __int8 i = (__int8) $2; $$ = $1; $$->appendInt8(i); }
                        ;

/*  Field/parameter initialization  */
fieldInit               : fieldSerInit                       { $$ = $1; }
                        | compQstring                        { $$ = BinStrToUnicode($1,true); $$->insertInt8(ELEMENT_TYPE_STRING);}
                        | NULLREF_                           { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_CLASS);
                                                               $$->appendInt32(0); }
                        ;

/*  Values for verbal form of CA blob description  */
serInit                 : fieldSerInit                       { $$ = $1; }
                        | STRING_ '(' NULLREF_ ')'           { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_STRING); $$->appendInt8(0xFF); }
                        | STRING_ '(' SQSTRING ')'           { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_STRING);
                                                               AppendStringWithLength($$,$3); delete [] $3;}
                        | TYPE_ '(' CLASS_ SQSTRING ')'      { $$ = new BinStr(); $$->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength($$,$4); delete [] $4;}
                        | TYPE_ '(' className ')'            { $$ = new BinStr(); $$->appendInt8(SERIALIZATION_TYPE_TYPE);
                                                               AppendStringWithLength($$,PASM->ReflectionNotation($3));}
                        | TYPE_ '(' NULLREF_ ')'             { $$ = new BinStr(); $$->appendInt8(SERIALIZATION_TYPE_TYPE); $$->appendInt8(0xFF); }
                        | OBJECT_ '(' serInit ')'            { $$ = $3; $$->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);}
                        | FLOAT32_ '[' int32 ']' '(' f32seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_R4);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | FLOAT64_ '[' int32 ']' '(' f64seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_R8);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | INT64_ '[' int32 ']' '(' i64seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_I8);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | INT32_ '[' int32 ']' '(' i32seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_I4);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | INT16_ '[' int32 ']' '(' i16seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_I2);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | INT8_ '[' int32 ']' '(' i8seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_I1);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | UINT64_ '[' int32 ']' '(' i64seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_U8);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | UINT32_ '[' int32 ']' '(' i32seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_U4);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | UINT16_ '[' int32 ']' '(' i16seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_U2);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | UINT8_ '[' int32 ']' '(' i8seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_U1);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | UNSIGNED_ INT64_ '[' int32 ']' '(' i64seq ')'
                                                             { $$ = $7; $$->insertInt32($4);
                                                               $$->insertInt8(ELEMENT_TYPE_U8);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | UNSIGNED_ INT32_ '[' int32 ']' '(' i32seq ')'
                                                             { $$ = $7; $$->insertInt32($4);
                                                               $$->insertInt8(ELEMENT_TYPE_U4);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | UNSIGNED_ INT16_ '[' int32 ']' '(' i16seq ')'
                                                             { $$ = $7; $$->insertInt32($4);
                                                               $$->insertInt8(ELEMENT_TYPE_U2);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | UNSIGNED_ INT8_ '[' int32 ']' '(' i8seq ')'
                                                             { $$ = $7; $$->insertInt32($4);
                                                               $$->insertInt8(ELEMENT_TYPE_U1);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | CHAR_ '[' int32 ']' '(' i16seq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_CHAR);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | BOOL_ '[' int32 ']' '(' boolSeq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_BOOLEAN);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | STRING_ '[' int32 ']' '(' sqstringSeq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(ELEMENT_TYPE_STRING);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | TYPE_ '[' int32 ']' '(' classSeq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(SERIALIZATION_TYPE_TYPE);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | OBJECT_ '[' int32 ']' '(' objSeq ')'
                                                             { $$ = $6; $$->insertInt32($3);
                                                               $$->insertInt8(SERIALIZATION_TYPE_TAGGED_OBJECT);
                                                               $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        ;


f32seq                  : /* EMPTY */                        { $$ = new BinStr(); }
                        | f32seq float64                     { $$ = $1;
                                                               float f = (float) (*$2); $$->appendInt32(*((__int32*)&f)); delete $2; }
                        | f32seq int32                       { $$ = $1;
                                                               $$->appendInt32($2); }
                        ;

f64seq                  : /* EMPTY */                        { $$ = new BinStr(); }
                        | f64seq float64                     { $$ = $1;
                                                               $$->appendInt64((__int64 *)$2); delete $2; }
                        | f64seq int64                       { $$ = $1;
                                                               $$->appendInt64((__int64 *)$2); delete $2; }
                        ;

i64seq                  : /* EMPTY */                        { $$ = new BinStr(); }
                        | i64seq int64                       { $$ = $1;
                                                               $$->appendInt64((__int64 *)$2); delete $2; }
                        ;

i32seq                  : /* EMPTY */                        { $$ = new BinStr(); }
                        | i32seq int32                       { $$ = $1; $$->appendInt32($2);}
                        ;

i16seq                  : /* EMPTY */                        { $$ = new BinStr(); }
                        | i16seq int32                       { $$ = $1; $$->appendInt16($2);}
                        ;

i8seq                   : /* EMPTY */                        { $$ = new BinStr(); }
                        | i8seq int32                        { $$ = $1; $$->appendInt8($2); }
                        ;

boolSeq                 : /* EMPTY */                        { $$ = new BinStr(); }
                        | boolSeq truefalse                  { $$ = $1;
                                                               $$->appendInt8($2);}
                        ;

sqstringSeq             : /* EMPTY */                        { $$ = new BinStr(); }
                        | sqstringSeq NULLREF_               { $$ = $1; $$->appendInt8(0xFF); }
                        | sqstringSeq SQSTRING               { $$ = $1;
                                                               AppendStringWithLength($$,$2); delete [] $2;}
                        ;

classSeq                : /* EMPTY */                        { $$ = new BinStr(); }
                        | classSeq NULLREF_                  { $$ = $1; $$->appendInt8(0xFF); }
                        | classSeq CLASS_ SQSTRING           { $$ = $1;
                                                               AppendStringWithLength($$,$3); delete [] $3;}
                        | classSeq className                 { $$ = $1;
                                                               AppendStringWithLength($$,PASM->ReflectionNotation($2));}
                        ;

objSeq                  : /* EMPTY */                        { $$ = new BinStr(); }
                        | objSeq serInit                     { $$ = $1; $$->append($2); delete $2; }
                        ;

/*  IL instructions and associated definitions  */
methodSpec              : METHOD_                            { parser->m_ANSFirst.PUSH(PASM->m_firstArgName);
                                                               parser->m_ANSLast.PUSH(PASM->m_lastArgName);
                                                               PASM->m_firstArgName = NULL;
                                                               PASM->m_lastArgName = NULL; }
                        ;

instr_none              : INSTR_NONE                         { $$ = SetupInstr($1); }
                        ;

instr_var               : INSTR_VAR                          { $$ = SetupInstr($1); }
                        ;

instr_i                 : INSTR_I                            { $$ = SetupInstr($1); }
                        ;

instr_i8                : INSTR_I8                           { $$ = SetupInstr($1); }
                        ;

instr_r                 : INSTR_R                            { $$ = SetupInstr($1); }
                        ;

instr_brtarget          : INSTR_BRTARGET                     { $$ = SetupInstr($1); }
                        ;

instr_method            : INSTR_METHOD                       { $$ = SetupInstr($1);
                                                               if((!PASM->OnErrGo)&&
                                                               (($1 == CEE_NEWOBJ)||
                                                                ($1 == CEE_CALLVIRT)))
                                                                  iCallConv = IMAGE_CEE_CS_CALLCONV_HASTHIS;
                                                             }
                        ;

instr_field             : INSTR_FIELD                        { $$ = SetupInstr($1); }
                        ;

instr_type              : INSTR_TYPE                         { $$ = SetupInstr($1); }
                        ;

instr_string            : INSTR_STRING                       { $$ = SetupInstr($1); }
                        ;

instr_sig               : INSTR_SIG                          { $$ = SetupInstr($1); }
                        ;

instr_tok               : INSTR_TOK                          { $$ = SetupInstr($1); iOpcodeLen = PASM->OpcodeLen($$); }
                        ;

instr_switch            : INSTR_SWITCH                       { $$ = SetupInstr($1); }
                        ;

instr_r_head            : instr_r '('                        { $$ = $1; bParsingByteArray = TRUE; }
                        ;


instr                   : instr_none                         { PASM->EmitOpcode($1); }
                        | instr_var int32                    { PASM->EmitInstrVar($1, $2); }
                        | instr_var id                       { PASM->EmitInstrVarByName($1, $2); }
                        | instr_i int32                      { PASM->EmitInstrI($1, $2); }
                        | instr_i8 int64                     { PASM->EmitInstrI8($1, $2); }
                        | instr_r float64                    { PASM->EmitInstrR($1, $2); delete ($2);}
                        | instr_r int64                      { double f = (double) (*$2); PASM->EmitInstrR($1, &f); }
                        | instr_r_head bytes ')'             { unsigned L = $2->length();
                                                               FAIL_UNLESS(L >= sizeof(float), ("%d hexbytes, must be at least %d\n",
                                                                           L,sizeof(float)));
                                                               if(L < sizeof(float)) {YYERROR; }
                                                               else {
                                                                   double f = (L >= sizeof(double)) ? *((double *)($2->ptr()))
                                                                                    : (double)(*(float *)($2->ptr()));
                                                                   PASM->EmitInstrR($1,&f); }
                                                               delete $2; }
                        | instr_brtarget int32               { PASM->EmitInstrBrOffset($1, $2); }
                        | instr_brtarget id                  { PASM->EmitInstrBrTarget($1, $2); }
                        | instr_method methodRef
                                                             { PASM->SetMemberRefFixup($2,PASM->OpcodeLen($1));
                                                               PASM->EmitInstrI($1,$2);
                                                               PASM->m_tkCurrentCVOwner = $2;
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iCallConv = 0;
                                                             }
                        | instr_field type typeSpec DCOLON dottedName
                                                             { $2->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef($3, $5, $2);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen($1));
                                                               PASM->EmitInstrI($1,mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
                        | instr_field type dottedName
                                                             { $2->insertInt8(IMAGE_CEE_CS_CALLCONV_FIELD);
                                                               mdToken mr = PASM->MakeMemberRef(mdTokenNil, $3, $2);
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen($1));
                                                               PASM->EmitInstrI($1,mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
                        | instr_field mdtoken                { mdToken mr = $2;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen($1));
                                                               PASM->EmitInstrI($1,mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
                        | instr_field TYPEDEF_F              { mdToken mr = $2->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen($1));
                                                               PASM->EmitInstrI($1,mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
                        | instr_field TYPEDEF_MR             { mdToken mr = $2->m_tkTypeSpec;
                                                               PASM->SetMemberRefFixup(mr, PASM->OpcodeLen($1));
                                                               PASM->EmitInstrI($1,mr);
                                                               PASM->m_tkCurrentCVOwner = mr;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
                        | instr_type typeSpec                { PASM->EmitInstrI($1, $2);
                                                               PASM->m_tkCurrentCVOwner = $2;
                                                               PASM->m_pCustomDescrList = NULL;
                                                             }
                        | instr_string compQstring           { PASM->EmitInstrStringLiteral($1, $2,TRUE); }
                        | instr_string ANSI_ '(' compQstring ')'
                                                             { PASM->EmitInstrStringLiteral($1, $4,FALSE); }
                        | instr_string bytearrayhead bytes ')'
                                                             { PASM->EmitInstrStringLiteral($1, $3,FALSE,TRUE); }
                        | instr_sig callConv type '(' sigArgs0 ')'
                                                             { PASM->EmitInstrSig($1, parser->MakeSig($2, $3, $5));
                                                               PASM->ResetArgNameList();
                                                             }
                        | instr_tok ownerType /* ownerType ::= memberRef | typeSpec */
                                                             { PASM->EmitInstrI($1,$2);
                                                               PASM->m_tkCurrentCVOwner = $2;
                                                               PASM->m_pCustomDescrList = NULL;
                                                               iOpcodeLen = 0;
                                                             }
                        | instr_switch '(' labels ')'        { PASM->EmitInstrSwitch($1, $3); }
                        ;

labels                  : /* empty */                         { $$ = 0; }
                        | id ',' labels                       { $$ = new Labels($1, $3, TRUE); }
                        | int32 ',' labels                    { $$ = new Labels((char *)(UINT_PTR)$1, $3, FALSE); }
                        | id                                  { $$ = new Labels($1, NULL, TRUE); }
                        | int32                               { $$ = new Labels((char *)(UINT_PTR)$1, NULL, FALSE); }
                        ;

/*  Signatures  */
tyArgs0                 : /* EMPTY */                        { $$ = NULL; }
                        | '<' tyArgs1 '>'                    { $$ = $2; }
                        ;

tyArgs1                 : /* EMPTY */                        { $$ = NULL; }
                        | tyArgs2                            { $$ = $1; }
                        ;

tyArgs2                 : type                               { $$ = $1; }
                        | tyArgs2 ',' type                   { $$ = $1; $$->append($3); delete $3; }
                        ;


sigArgs0                : /* EMPTY */                        { $$ = new BinStr(); }
                        | sigArgs1                           { $$ = $1;}
                        ;

sigArgs1                : sigArg                             { $$ = $1; }
                        | sigArgs1 ',' sigArg                { $$ = $1; $$->append($3); delete $3; }
                        ;

sigArg                  : ELLIPSIS                             { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_SENTINEL); }
                        | paramAttr type marshalClause        { $$ = new BinStr(); $$->append($2); PASM->addArgName(NULL, $2, $3, $1); }
                        | paramAttr type marshalClause id     { $$ = new BinStr(); $$->append($2); PASM->addArgName($4, $2, $3, $1);}
                        ;

/*  Class referencing  */
className               : '[' dottedName ']' slashedName      { $$ = PASM->ResolveClassRef(PASM->GetAsmRef($2), $4, NULL); delete[] $2;}
                        | '[' mdtoken ']' slashedName         { $$ = PASM->ResolveClassRef($2, $4, NULL); }
                        | '[' '*' ']' slashedName             { $$ = PASM->ResolveClassRef(mdTokenNil, $4, NULL); }
                        | '[' _MODULE dottedName ']' slashedName   { $$ = PASM->ResolveClassRef(PASM->GetModRef($3),$5, NULL); delete[] $3;}
                        | slashedName                         { $$ = PASM->ResolveClassRef(1,$1,NULL); }
                        | mdtoken                             { $$ = $1; }
                        | TYPEDEF_T                           { $$ = $1->m_tkTypeSpec; }
                        | _THIS                               { if(PASM->m_pCurClass != NULL) $$ = PASM->m_pCurClass->m_cl;
                                                                else { $$ = 0; PASM->report->error(".this outside class scope\n"); }
                                                              }
                        | _BASE                               { if(PASM->m_pCurClass != NULL) {
                                                                  $$ = PASM->m_pCurClass->m_crExtends;
                                                                  if(RidFromToken($$) == 0)
                                                                    PASM->report->error(".base undefined\n");
                                                                } else { $$ = 0; PASM->report->error(".base outside class scope\n"); }
                                                              }
                        | _NESTER                             { if(PASM->m_pCurClass != NULL) {
                                                                  if(PASM->m_pCurClass->m_pEncloser != NULL) $$ = PASM->m_pCurClass->m_pEncloser->m_cl;
                                                                  else { $$ = 0; PASM->report->error(".nester undefined\n"); }
                                                                } else { $$ = 0; PASM->report->error(".nester outside class scope\n"); }
                                                              }
                        ;

slashedName             : dottedName                          { $$ = $1; }
                        | slashedName '/' dottedName          { $$ = newStringWDel($1, NESTING_SEP, $3); }
                        ;

typeSpec                : className                           { $$ = $1;}
                        | '[' dottedName ']'                  { $$ = PASM->GetAsmRef($2); delete[] $2;}
                        | '[' _MODULE dottedName ']'          { $$ = PASM->GetModRef($3); delete[] $3;}
                        | type                                { $$ = PASM->ResolveTypeSpec($1); }
                        ;

/*  Native types for marshaling signatures  */
nativeType              : /* EMPTY */                         { $$ = new BinStr(); }
                        | CUSTOM_ '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'
                                                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt($$,$3->length()); $$->append($3);
                                                                corEmitInt($$,$5->length()); $$->append($5);
                                                                corEmitInt($$,$7->length()); $$->append($7);
                                                                corEmitInt($$,$9->length()); $$->append($9);
                                                                PASM->report->warn("Deprecated 4-string form of custom marshaler, first two strings ignored\n");}
                        | CUSTOM_ '(' compQstring ',' compQstring ')'
                                                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_CUSTOMMARSHALER);
                                                                corEmitInt($$,0);
                                                                corEmitInt($$,0);
                                                                corEmitInt($$,$3->length()); $$->append($3);
                                                                corEmitInt($$,$5->length()); $$->append($5); }
                        | FIXED_ SYSSTRING_ '[' int32 ']'     { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_FIXEDSYSSTRING);
                                                                corEmitInt($$,$4); }
                        | FIXED_ ARRAY_ '[' int32 ']' nativeType
                                                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_FIXEDARRAY);
                                                                corEmitInt($$,$4); $$->append($6); }
                        | VARIANT_                            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_VARIANT);
                                                                PASM->report->warn("Deprecated native type 'variant'\n"); }
                        | CURRENCY_                           { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_CURRENCY); }
                        | SYSCHAR_                            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_SYSCHAR);
                                                                PASM->report->warn("Deprecated native type 'syschar'\n"); }
                        | VOID_                               { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_VOID);
                                                                PASM->report->warn("Deprecated native type 'void'\n"); }
                        | BOOL_                               { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_BOOLEAN); }
                        | INT8_                               { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_I1); }
                        | INT16_                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_I2); }
                        | INT32_                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_I4); }
                        | INT64_                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_I8); }
                        | FLOAT32_                            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_R4); }
                        | FLOAT64_                            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_R8); }
                        | ERROR_                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_ERROR); }
                        | UNSIGNED_ INT8_                     { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_U1); }
                        | UNSIGNED_ INT16_                    { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_U2); }
                        | UNSIGNED_ INT32_                    { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_U4); }
                        | UNSIGNED_ INT64_                    { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_U8); }
                        | UINT8_                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_U1); }
                        | UINT16_                             { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_U2); }
                        | UINT32_                             { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_U4); }
                        | UINT64_                             { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_U8); }
                        | nativeType '*'                      { $$ = $1; $$->insertInt8(NATIVE_TYPE_PTR);
                                                                PASM->report->warn("Deprecated native type '*'\n"); }
                        | nativeType '[' ']'                  { $$ = $1; if($$->length()==0) $$->appendInt8(NATIVE_TYPE_MAX);
                                                                $$->insertInt8(NATIVE_TYPE_ARRAY); }
                        | nativeType '[' int32 ']'            { $$ = $1; if($$->length()==0) $$->appendInt8(NATIVE_TYPE_MAX);
                                                                $$->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt($$,0);
                                                                corEmitInt($$,$3);
                                                                corEmitInt($$,0); }
                        | nativeType '[' int32 '+' int32 ']'  { $$ = $1; if($$->length()==0) $$->appendInt8(NATIVE_TYPE_MAX);
                                                                $$->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt($$,$5);
                                                                corEmitInt($$,$3);
                                                                corEmitInt($$,ntaSizeParamIndexSpecified); }
                        | nativeType '[' '+' int32 ']'        { $$ = $1; if($$->length()==0) $$->appendInt8(NATIVE_TYPE_MAX);
                                                                $$->insertInt8(NATIVE_TYPE_ARRAY);
                                                                corEmitInt($$,$4); }
                        | DECIMAL_                            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_DECIMAL);
                                                                PASM->report->warn("Deprecated native type 'decimal'\n"); }
                        | DATE_                               { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_DATE);
                                                                PASM->report->warn("Deprecated native type 'date'\n"); }
                        | BSTR_                               { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_BSTR); }
                        | LPSTR_                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_LPSTR); }
                        | LPWSTR_                             { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_LPWSTR); }
                        | LPTSTR_                             { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_LPTSTR); }
                        | OBJECTREF_                          { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_OBJECTREF);
                                                                PASM->report->warn("Deprecated native type 'objectref'\n"); }
                        | IUNKNOWN_  iidParamIndex            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_IUNKNOWN);
                                                                if($2 != -1) corEmitInt($$,$2); }
                        | IDISPATCH_ iidParamIndex            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_IDISPATCH);
                                                                if($2 != -1) corEmitInt($$,$2); }
                        | STRUCT_                             { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_STRUCT); }
                        | INTERFACE_ iidParamIndex            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_INTF);
                                                                if($2 != -1) corEmitInt($$,$2); }
                        | SAFEARRAY_ variantType              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt($$,$2);
                                                                corEmitInt($$,0);}
                        | SAFEARRAY_ variantType ',' compQstring { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_SAFEARRAY);
                                                                corEmitInt($$,$2);
                                                                corEmitInt($$,$4->length()); $$->append($4); }

                        | INT_                                { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_INT); }
                        | UNSIGNED_ INT_                      { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_UINT); }
                        | UINT_                               { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_UINT); }
                        | NESTED_ STRUCT_                     { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_NESTEDSTRUCT);
                                                                PASM->report->warn("Deprecated native type 'nested struct'\n"); }
                        | BYVALSTR_                           { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_BYVALSTR); }
                        | ANSI_ BSTR_                         { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_ANSIBSTR); }
                        | TBSTR_                              { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_TBSTR); }
                        | VARIANT_ BOOL_                      { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_VARIANTBOOL); }
                        | METHOD_                             { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_FUNC); }
                        | AS_ ANY_                            { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_ASANY); }
                        | LPSTRUCT_                           { $$ = new BinStr(); $$->appendInt8(NATIVE_TYPE_LPSTRUCT); }
                        | TYPEDEF_TS                          { $$ = new BinStr(); $$->append($1->m_pbsTypeSpec); }
                        ;

iidParamIndex           : /* EMPTY */                         { $$ = -1; }
                        | '(' IIDPARAM_ '=' int32 ')'         { $$ = $4; }
                        ;

variantType             : /* EMPTY */                         { $$ = VT_EMPTY; }
                        | NULL_                               { $$ = VT_NULL; }
                        | VARIANT_                            { $$ = VT_VARIANT; }
                        | CURRENCY_                           { $$ = VT_CY; }
                        | VOID_                               { $$ = VT_VOID; }
                        | BOOL_                               { $$ = VT_BOOL; }
                        | INT8_                               { $$ = VT_I1; }
                        | INT16_                              { $$ = VT_I2; }
                        | INT32_                              { $$ = VT_I4; }
                        | INT64_                              { $$ = VT_I8; }
                        | FLOAT32_                            { $$ = VT_R4; }
                        | FLOAT64_                            { $$ = VT_R8; }
                        | UNSIGNED_ INT8_                     { $$ = VT_UI1; }
                        | UNSIGNED_ INT16_                    { $$ = VT_UI2; }
                        | UNSIGNED_ INT32_                    { $$ = VT_UI4; }
                        | UNSIGNED_ INT64_                    { $$ = VT_UI8; }
                        | UINT8_                              { $$ = VT_UI1; }
                        | UINT16_                             { $$ = VT_UI2; }
                        | UINT32_                             { $$ = VT_UI4; }
                        | UINT64_                             { $$ = VT_UI8; }
                        | '*'                                 { $$ = VT_PTR; }
                        | variantType '[' ']'                 { $$ = $1 | VT_ARRAY; }
                        | variantType VECTOR_                 { $$ = $1 | VT_VECTOR; }
                        | variantType '&'                     { $$ = $1 | VT_BYREF; }
                        | DECIMAL_                            { $$ = VT_DECIMAL; }
                        | DATE_                               { $$ = VT_DATE; }
                        | BSTR_                               { $$ = VT_BSTR; }
                        | LPSTR_                              { $$ = VT_LPSTR; }
                        | LPWSTR_                             { $$ = VT_LPWSTR; }
                        | IUNKNOWN_                           { $$ = VT_UNKNOWN; }
                        | IDISPATCH_                          { $$ = VT_DISPATCH; }
                        | SAFEARRAY_                          { $$ = VT_SAFEARRAY; }
                        | INT_                                { $$ = VT_INT; }
                        | UNSIGNED_ INT_                      { $$ = VT_UINT; }
                        | UINT_                               { $$ = VT_UINT; }
                        | ERROR_                              { $$ = VT_ERROR; }
                        | HRESULT_                            { $$ = VT_HRESULT; }
                        | CARRAY_                             { $$ = VT_CARRAY; }
                        | USERDEFINED_                        { $$ = VT_USERDEFINED; }
                        | RECORD_                             { $$ = VT_RECORD; }
                        | FILETIME_                           { $$ = VT_FILETIME; }
                        | BLOB_                               { $$ = VT_BLOB; }
                        | STREAM_                             { $$ = VT_STREAM; }
                        | STORAGE_                            { $$ = VT_STORAGE; }
                        | STREAMED_OBJECT_                    { $$ = VT_STREAMED_OBJECT; }
                        | STORED_OBJECT_                      { $$ = VT_STORED_OBJECT; }
                        | BLOB_OBJECT_                        { $$ = VT_BLOB_OBJECT; }
                        | CF_                                 { $$ = VT_CF; }
                        | CLSID_                              { $$ = VT_CLSID; }
                        ;

/*  Managed types for signatures  */
type                    : CLASS_ className                    { if($2 == PASM->m_tkSysString)
                                                                {     $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_STRING); }
                                                                else if($2 == PASM->m_tkSysObject)
                                                                {     $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_OBJECT); }
                                                                else
                                                                 $$ = parser->MakeTypeClass(ELEMENT_TYPE_CLASS, $2); }
                        | OBJECT_                             { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_OBJECT); }
                        | VALUE_ CLASS_ className             { $$ = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, $3); }
                        | VALUETYPE_ className                { $$ = parser->MakeTypeClass(ELEMENT_TYPE_VALUETYPE, $2); }
                        | type '[' ']'                        { $$ = $1; $$->insertInt8(ELEMENT_TYPE_SZARRAY); }
                        | type '[' bounds1 ']'                { $$ = parser->MakeTypeArray(ELEMENT_TYPE_ARRAY, $1, $3); }
                        | type '&'                            { $$ = $1; $$->insertInt8(ELEMENT_TYPE_BYREF); }
                        | type '*'                            { $$ = $1; $$->insertInt8(ELEMENT_TYPE_PTR); }
                        | type PINNED_                        { $$ = $1; $$->insertInt8(ELEMENT_TYPE_PINNED); }
                        | type MODREQ_ '(' typeSpec ')'       { $$ = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_REQD, $4);
                                                                $$->append($1); }
                        | type MODOPT_ '(' typeSpec ')'       { $$ = parser->MakeTypeClass(ELEMENT_TYPE_CMOD_OPT, $4);
                                                                $$->append($1); }
                        | methodSpec callConv type '*' '(' sigArgs0 ')'
                                                              { $$ = parser->MakeSig($2, $3, $6);
                                                                $$->insertInt8(ELEMENT_TYPE_FNPTR);
                                                                PASM->delArgNameList(PASM->m_firstArgName);
                                                                PASM->m_firstArgName = parser->m_ANSFirst.POP();
                                                                PASM->m_lastArgName = parser->m_ANSLast.POP();
                                                              }
                        | type '<' tyArgs1 '>'                { if($3 == NULL) $$ = $1;
                                                                else {
                                                                  $$ = new BinStr();
                                                                  $$->appendInt8(ELEMENT_TYPE_GENERICINST);
                                                                  $$->append($1);
                                                                  corEmitInt($$, corCountArgs($3));
                                                                  $$->append($3); delete $1; delete $3; }}
                        | '!' '!' int32                       { //if(PASM->m_pCurMethod)  {
                                                                //  if(($3 < 0)||((DWORD)$3 >= PASM->m_pCurMethod->m_NumTyPars))
                                                                //    PASM->report->error("Invalid method type parameter '%d'\n",$3);
                                                                  $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_MVAR); corEmitInt($$, $3);
                                                                //} else PASM->report->error("Method type parameter '%d' outside method scope\n",$3);
                                                              }
                        | '!' int32                           { //if(PASM->m_pCurClass)  {
                                                                //  if(($2 < 0)||((DWORD)$2 >= PASM->m_pCurClass->m_NumTyPars))
                                                                //    PASM->report->error("Invalid type parameter '%d'\n",$2);
                                                                  $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_VAR); corEmitInt($$, $2);
                                                                //} else PASM->report->error("Type parameter '%d' outside class scope\n",$2);
                                                              }
                        | '!' '!' dottedName                  { int eltype = ELEMENT_TYPE_MVAR;
                                                                int n=-1;
                                                                if(PASM->m_pCurMethod) n = PASM->m_pCurMethod->FindTyPar($3);
                                                                else {
                                                                  if(PASM->m_TyParList) n = PASM->m_TyParList->IndexOf($3);
                                                                  if(n == -1)
                                                                  { n = TyParFixupList.COUNT();
                                                                    TyParFixupList.PUSH($3);
                                                                    eltype = ELEMENT_TYPE_MVARFIXUP;
                                                                  }
                                                                }
                                                                if(n == -1) { PASM->report->error("Invalid method type parameter '%s'\n",$3);
                                                                n = 0x1FFFFFFF; }
                                                                $$ = new BinStr(); $$->appendInt8(eltype); corEmitInt($$,n);
                                                              }
                        | '!' dottedName                      { int eltype = ELEMENT_TYPE_VAR;
                                                                int n=-1;
                                                                if(PASM->m_pCurClass && !newclass) n = PASM->m_pCurClass->FindTyPar($2);
                                                                else {
                                                                  if(PASM->m_TyParList) n = PASM->m_TyParList->IndexOf($2);
                                                                  if(n == -1)
                                                                  { n = TyParFixupList.COUNT();
                                                                    TyParFixupList.PUSH($2);
                                                                    eltype = ELEMENT_TYPE_VARFIXUP;
                                                                  }
                                                                }
                                                                if(n == -1) { PASM->report->error("Invalid type parameter '%s'\n",$2);
                                                                n = 0x1FFFFFFF; }
                                                                $$ = new BinStr(); $$->appendInt8(eltype); corEmitInt($$,n);
                                                              }
                        | TYPEDREF_                           { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_TYPEDBYREF); }
                        | VOID_                               { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_VOID); }
                        | NATIVE_ INT_                        { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I); }
                        | NATIVE_ UNSIGNED_ INT_              { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U); }
                        | NATIVE_ UINT_                       { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U); }
                        | simpleType                          { $$ = $1; }
                        | ELLIPSIS type                        { $$ = $2; $$->insertInt8(ELEMENT_TYPE_SENTINEL); }
                        ;

simpleType              : CHAR_                               { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_CHAR); }
                        | STRING_                             { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_STRING); }
                        | BOOL_                               { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_BOOLEAN); }
                        | INT8_                               { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I1); }
                        | INT16_                              { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I2); }
                        | INT32_                              { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I4); }
                        | INT64_                              { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_I8); }
                        | FLOAT32_                            { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_R4); }
                        | FLOAT64_                            { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_R8); }
                        | UNSIGNED_ INT8_                     { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U1); }
                        | UNSIGNED_ INT16_                    { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U2); }
                        | UNSIGNED_ INT32_                    { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U4); }
                        | UNSIGNED_ INT64_                    { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U8); }
                        | UINT8_                              { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U1); }
                        | UINT16_                             { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U2); }
                        | UINT32_                             { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U4); }
                        | UINT64_                             { $$ = new BinStr(); $$->appendInt8(ELEMENT_TYPE_U8); }
                        | TYPEDEF_TS                          { $$ = new BinStr(); $$->append($1->m_pbsTypeSpec); }
                        ;

bounds1                 : bound                               { $$ = $1; }
                        | bounds1 ',' bound                   { $$ = $1; $1->append($3); delete $3; }
                        ;

bound                   : /* EMPTY */                         { $$ = new BinStr(); $$->appendInt32(0x7FFFFFFF); $$->appendInt32(0x7FFFFFFF);  }
                        | ELLIPSIS                             { $$ = new BinStr(); $$->appendInt32(0x7FFFFFFF); $$->appendInt32(0x7FFFFFFF);  }
                        | int32                               { $$ = new BinStr(); $$->appendInt32(0); $$->appendInt32($1); }
                        | int32 ELLIPSIS int32                 { FAIL_UNLESS($1 <= $3, ("lower bound %d must be <= upper bound %d\n", $1, $3));
                                                                if ($1 > $3) { YYERROR; };
                                                                $$ = new BinStr(); $$->appendInt32($1); $$->appendInt32($3-$1+1); }
                        | int32 ELLIPSIS                       { $$ = new BinStr(); $$->appendInt32($1); $$->appendInt32(0x7FFFFFFF); }
                        ;

/*  Security declarations  */
secDecl                 : _PERMISSION secAction typeSpec '(' nameValPairs ')'
                                                              { PASM->AddPermissionDecl($2, $3, $5); }
                        | _PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'
                                                              { PASM->AddPermissionDecl($2, $3, $6); }
                        | _PERMISSION secAction typeSpec      { PASM->AddPermissionDecl($2, $3, (NVPair *)NULL); }
                        | psetHead bytes ')'                  { PASM->AddPermissionSetDecl($1, $2); }
                        | _PERMISSIONSET secAction compQstring
                                                              { PASM->AddPermissionSetDecl($2,BinStrToUnicode($3,true));}
                        | _PERMISSIONSET secAction '=' '{' secAttrSetBlob '}'
                                                              { BinStr* ret = new BinStr();
                                                                ret->insertInt8('.');
                                                                corEmitInt(ret, nSecAttrBlobs);
                                                                ret->append($5);
                                                                PASM->AddPermissionSetDecl($2,ret);
                                                                nSecAttrBlobs = 0; }
                        ;

secAttrSetBlob          : /* EMPTY */                         { $$ = new BinStr(); nSecAttrBlobs = 0;}
                        | secAttrBlob                         { $$ = $1; nSecAttrBlobs = 1; }
                        | secAttrBlob ',' secAttrSetBlob      { $$ = $1; $$->append($3); nSecAttrBlobs++; }
                        ;

secAttrBlob             : typeSpec '=' '{' customBlobNVPairs '}'
                                                              { $$ = PASM->EncodeSecAttr(PASM->ReflectionNotation($1),$4,nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
                        | CLASS_ SQSTRING '=' '{' customBlobNVPairs '}'
                                                              { $$ = PASM->EncodeSecAttr($2,$5,nCustomBlobNVPairs);
                                                                nCustomBlobNVPairs = 0; }
                        ;

psetHead                : _PERMISSIONSET secAction '=' '('    { $$ = $2; bParsingByteArray = TRUE; }
                        | _PERMISSIONSET secAction BYTEARRAY_ '('
                                                              { $$ = $2; bParsingByteArray = TRUE; }
                        ;

nameValPairs            : nameValPair                         { $$ = $1; }
                        | nameValPair ',' nameValPairs        { $$ = $1->Concat($3); }
                        ;

nameValPair             : compQstring '=' caValue             { $1->appendInt8(0); $$ = new NVPair($1, $3); }
                        ;

truefalse               : TRUE_                               { $$ = 1; }
                        | FALSE_                              { $$ = 0; }
                        ;

caValue                 : truefalse                           { $$ = new BinStr();
                                                                $$->appendInt8(SERIALIZATION_TYPE_BOOLEAN);
                                                                $$->appendInt8($1); }
                        | int32                               { $$ = new BinStr();
                                                                $$->appendInt8(SERIALIZATION_TYPE_I4);
                                                                $$->appendInt32($1); }
                        | INT32_ '(' int32 ')'                { $$ = new BinStr();
                                                                $$->appendInt8(SERIALIZATION_TYPE_I4);
                                                                $$->appendInt32($3); }
                        | compQstring                         { $$ = new BinStr();
                                                                $$->appendInt8(SERIALIZATION_TYPE_STRING);
                                                                $$->append($1); delete $1;
                                                                $$->appendInt8(0); }
                        | className '(' INT8_ ':' int32 ')'   { $$ = new BinStr();
                                                                $$->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation($1);
                                                                strcpy_s((char *)$$->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                $$->appendInt8(1);
                                                                $$->appendInt32($5); }
                        | className '(' INT16_ ':' int32 ')'  { $$ = new BinStr();
                                                                $$->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation($1);
                                                                strcpy_s((char *)$$->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                $$->appendInt8(2);
                                                                $$->appendInt32($5); }
                        | className '(' INT32_ ':' int32 ')'  { $$ = new BinStr();
                                                                $$->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation($1);
                                                                strcpy_s((char *)$$->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                $$->appendInt8(4);
                                                                $$->appendInt32($5); }
                        | className '(' int32 ')'             { $$ = new BinStr();
                                                                $$->appendInt8(SERIALIZATION_TYPE_ENUM);
                                                                char* sz = PASM->ReflectionNotation($1);
                                                                strcpy_s((char *)$$->getBuff((unsigned)strlen(sz) + 1), strlen(sz) + 1,sz);
                                                                $$->appendInt8(4);
                                                                $$->appendInt32($3); }
                        ;

secAction               : REQUEST_                            { $$ = dclRequest; }
                        | DEMAND_                             { $$ = dclDemand; }
                        | ASSERT_                             { $$ = dclAssert; }
                        | DENY_                               { $$ = dclDeny; }
                        | PERMITONLY_                         { $$ = dclPermitOnly; }
                        | LINKCHECK_                          { $$ = dclLinktimeCheck; }
                        | INHERITCHECK_                       { $$ = dclInheritanceCheck; }
                        | REQMIN_                             { $$ = dclRequestMinimum; }
                        | REQOPT_                             { $$ = dclRequestOptional; }
                        | REQREFUSE_                          { $$ = dclRequestRefuse; }
                        | PREJITGRANT_                        { $$ = dclPrejitGrant; }
                        | PREJITDENY_                         { $$ = dclPrejitDenied; }
                        | NONCASDEMAND_                       { $$ = dclNonCasDemand; }
                        | NONCASLINKDEMAND_                   { $$ = dclNonCasLinkDemand; }
                        | NONCASINHERITANCE_                  { $$ = dclNonCasInheritance; }
                        ;

/*  External source declarations  */
esHead                  : _LINE                               { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = FALSE; }
                        | P_LINE                              { PASM->ResetLineNumbers(); nCurrPC = PASM->m_CurPC; PENV->bExternSource = TRUE; PENV->bExternSourceAutoincrement = TRUE; }
                        ;

extSourceSpec           : esHead int32 SQSTRING               { PENV->nExtLine = PENV->nExtLineEnd = $2;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName($3);}
                        | esHead int32                        { PENV->nExtLine = PENV->nExtLineEnd = $2;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd  = static_cast<unsigned>(-1); }
                        | esHead int32 ':' int32 SQSTRING     { PENV->nExtLine = PENV->nExtLineEnd = $2;
                                                                PENV->nExtCol=$4; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName($5);}
                        | esHead int32 ':' int32              { PENV->nExtLine = PENV->nExtLineEnd = $2;
                                                                PENV->nExtCol=$4; PENV->nExtColEnd = static_cast<unsigned>(-1);}
                        | esHead int32 ':' int32 ',' int32 SQSTRING
                                                              { PENV->nExtLine = PENV->nExtLineEnd = $2;
                                                                PENV->nExtCol=$4; PENV->nExtColEnd = $6;
                                                                PASM->SetSourceFileName($7);}
                        | esHead int32 ':' int32 ',' int32
                                                              { PENV->nExtLine = PENV->nExtLineEnd = $2;
                                                                PENV->nExtCol=$4; PENV->nExtColEnd = $6; }
                        | esHead int32 ',' int32 ':' int32 SQSTRING
                                                              { PENV->nExtLine = $2; PENV->nExtLineEnd = $4;
                                                                PENV->nExtCol=$6; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName($7);}
                        | esHead int32 ',' int32 ':' int32
                                                              { PENV->nExtLine = $2; PENV->nExtLineEnd = $4;
                                                                PENV->nExtCol=$6; PENV->nExtColEnd = static_cast<unsigned>(-1); }
                        | esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING
                                                              { PENV->nExtLine = $2; PENV->nExtLineEnd = $4;
                                                                PENV->nExtCol=$6; PENV->nExtColEnd = $8;
                                                                PASM->SetSourceFileName($9);}
                        | esHead int32 ',' int32 ':' int32 ',' int32
                                                              { PENV->nExtLine = $2; PENV->nExtLineEnd = $4;
                                                                PENV->nExtCol=$6; PENV->nExtColEnd = $8; }
                        | esHead int32 QSTRING                { PENV->nExtLine = PENV->nExtLineEnd = $2 - 1;
                                                                PENV->nExtCol = 0; PENV->nExtColEnd = static_cast<unsigned>(-1);
                                                                PASM->SetSourceFileName($3);}
                        ;

/*  Manifest declarations  */
fileDecl                : _FILE fileAttr dottedName fileEntry hashHead bytes ')' fileEntry
                                                              { PASMM->AddFile($3, $2|$4|$8, $6); }
                        | _FILE fileAttr dottedName fileEntry { PASMM->AddFile($3, $2|$4, NULL); }
                        ;

fileAttr                : /* EMPTY */                         { $$ = (CorFileFlags) 0; }
                        | fileAttr NOMETADATA_                { $$ = (CorFileFlags) ($1 | ffContainsNoMetaData); }
                        ;

fileEntry               : /* EMPTY */                         { $$ = (CorFileFlags) 0; }
                        | _ENTRYPOINT                         { $$ = (CorFileFlags) 0x80000000; }
                        ;

hashHead                : _HASH '=' '('                       { bParsingByteArray = TRUE; }
                        ;

assemblyHead            : _ASSEMBLY asmAttr dottedName        { PASMM->StartAssembly($3, NULL, (DWORD)$2, FALSE); }
                        ;

asmAttr                 : /* EMPTY */                         { $$ = (CorAssemblyFlags) 0; }
                        | asmAttr RETARGETABLE_               { $$ = (CorAssemblyFlags) ($1 | afRetargetable); }
                        | asmAttr WINDOWSRUNTIME_             { $$ = (CorAssemblyFlags) ($1 | afContentType_WindowsRuntime); }
                        | asmAttr NOPLATFORM_                 { $$ = (CorAssemblyFlags) ($1 | afPA_NoPlatform); }
                        | asmAttr LEGACY_ LIBRARY_            { $$ = $1; }
                        | asmAttr CIL_                        { SET_PA($$,$1,afPA_MSIL); }
                        | asmAttr X86_                        { SET_PA($$,$1,afPA_x86); }
                        | asmAttr AMD64_                      { SET_PA($$,$1,afPA_AMD64); }
                        | asmAttr ARM_                        { SET_PA($$,$1,afPA_ARM); }
                        | asmAttr ARM64_                      { SET_PA($$,$1,afPA_ARM64); }
                        ;

assemblyDecls           : /* EMPTY */
                        | assemblyDecls assemblyDecl
                        ;

assemblyDecl            : _HASH ALGORITHM_ int32              { PASMM->SetAssemblyHashAlg($3); }
                        | secDecl
                        | asmOrRefDecl
                        ;

intOrWildcard           : int32                               { $$ = $1; }
                        | '*'                                 { $$ = 0xFFFF; }
                        ;

asmOrRefDecl            : publicKeyHead bytes ')'             { PASMM->SetAssemblyPublicKey($2); }
                        | _VER intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard
                                                              { PASMM->SetAssemblyVer((USHORT)$2, (USHORT)$4, (USHORT)$6, (USHORT)$8); }
                        | _LOCALE compQstring                 { $2->appendInt8(0); PASMM->SetAssemblyLocale($2,TRUE); }
                        | localeHead bytes ')'                { PASMM->SetAssemblyLocale($2,FALSE); }
                        | customAttrDecl
                        | compControl
                        ;

publicKeyHead           : _PUBLICKEY '=' '('                  { bParsingByteArray = TRUE; }
                        ;

publicKeyTokenHead      : _PUBLICKEYTOKEN '=' '('             { bParsingByteArray = TRUE; }
                        ;

localeHead              : _LOCALE '=' '('                     { bParsingByteArray = TRUE; }
                        ;

assemblyRefHead         : _ASSEMBLY EXTERN_ asmAttr dottedName
                                                              { PASMM->StartAssembly($4, NULL, $3, TRUE); }
                        | _ASSEMBLY EXTERN_ asmAttr dottedName AS_ dottedName
                                                              { PASMM->StartAssembly($4, $6, $3, TRUE); }
                        ;

assemblyRefDecls        : /* EMPTY */
                        | assemblyRefDecls assemblyRefDecl
                        ;

assemblyRefDecl         : hashHead bytes ')'                  { PASMM->SetAssemblyHashBlob($2); }
                        | asmOrRefDecl
                        | publicKeyTokenHead bytes ')'        { PASMM->SetAssemblyPublicKeyToken($2); }
                        | AUTO_                               { PASMM->SetAssemblyAutodetect(); }
                        ;

exptypeHead             : _CLASS EXTERN_ exptAttr dottedName  { PASMM->StartComType($4, $3);}
                        ;

exportHead              : _EXPORT exptAttr dottedName   /* deprecated */      { PASMM->StartComType($3, $2); }
                        ;

exptAttr                : /* EMPTY */                         { $$ = (CorTypeAttr) 0; }
                        | exptAttr PRIVATE_                   { $$ = (CorTypeAttr) ($1 | tdNotPublic); }
                        | exptAttr PUBLIC_                    { $$ = (CorTypeAttr) ($1 | tdPublic); }
                        | exptAttr FORWARDER_                 { $$ = (CorTypeAttr) ($1 | tdForwarder); }
                        | exptAttr NESTED_ PUBLIC_            { $$ = (CorTypeAttr) ($1 | tdNestedPublic); }
                        | exptAttr NESTED_ PRIVATE_           { $$ = (CorTypeAttr) ($1 | tdNestedPrivate); }
                        | exptAttr NESTED_ FAMILY_            { $$ = (CorTypeAttr) ($1 | tdNestedFamily); }
                        | exptAttr NESTED_ ASSEMBLY_          { $$ = (CorTypeAttr) ($1 | tdNestedAssembly); }
                        | exptAttr NESTED_ FAMANDASSEM_       { $$ = (CorTypeAttr) ($1 | tdNestedFamANDAssem); }
                        | exptAttr NESTED_ FAMORASSEM_        { $$ = (CorTypeAttr) ($1 | tdNestedFamORAssem); }
                        ;

exptypeDecls            : /* EMPTY */
                        | exptypeDecls exptypeDecl
                        ;

exptypeDecl             : _FILE dottedName                    { PASMM->SetComTypeFile($2); }
                        | _CLASS EXTERN_ slashedName           { PASMM->SetComTypeComType($3); }
                        | _ASSEMBLY EXTERN_ dottedName        { PASMM->SetComTypeAsmRef($3); }
                        | MDTOKEN_ '(' int32 ')'              { if(!PASMM->SetComTypeImplementationTok($3))
                                                                  PASM->report->error("Invalid implementation of exported type\n"); }
                        | _CLASS  int32                       { if(!PASMM->SetComTypeClassTok($2))
                                                                  PASM->report->error("Invalid TypeDefID of exported type\n"); }
                        | customAttrDecl
                        | compControl
                        ;

manifestResHead         : _MRESOURCE manresAttr dottedName    { PASMM->StartManifestRes($3, $3, $2); }
                        | _MRESOURCE manresAttr dottedName AS_ dottedName
                                                              { PASMM->StartManifestRes($3, $5, $2); }
                        ;

manresAttr              : /* EMPTY */                         { $$ = (CorManifestResourceFlags) 0; }
                        | manresAttr PUBLIC_                  { $$ = (CorManifestResourceFlags) ($1 | mrPublic); }
                        | manresAttr PRIVATE_                 { $$ = (CorManifestResourceFlags) ($1 | mrPrivate); }
                        ;

manifestResDecls        : /* EMPTY */
                        | manifestResDecls manifestResDecl
                        ;

manifestResDecl         : _FILE dottedName AT_ int32          { PASMM->SetManifestResFile($2, (ULONG)$4); }
                        | _ASSEMBLY EXTERN_ dottedName        { PASMM->SetManifestResAsmRef($3); }
                        | customAttrDecl
                        | compControl
                        ;

%%

#include "grammar_after.cpp"
