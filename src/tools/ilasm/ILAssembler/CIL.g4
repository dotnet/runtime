grammar CIL;

import Instructions;

tokens { IncludedFileEof, SyntheticIncludedFileEof }

INT32: '-'? ('0x' [0-9A-Fa-f]+ | [0-9]+);
INT64: '-'? ('0x' [0-9A-Fa-f]+ | [0-9]+);
FLOAT64: '-'? [0-9]+ ('.' [0-9]+ | [eE] '-'? [0-9]+);
HEXBYTE: [0-9A-Fa-f][0-9A-Fa-f];
DCOLON: '::';
ELLIPSIS: '..';
NULL: 'null';
NULLREF: 'nullref';
HASH: '.hash';
CHAR: 'char';
STRING: 'string';
BOOL: 'bool';
INT8: 'int8';
INT16: 'int16';
INT32_: 'int32';
INT64_: 'int64';
FLOAT32: 'float32';
FLOAT64_: 'float64';
fragment UNSIGNED: 'unsigned';
UINT8: 'uint8' | (UNSIGNED INT8);
UINT16: 'uint16' | (UNSIGNED INT16);
UINT32: 'uint32' | (UNSIGNED INT32_);
UINT64: 'uint64' | (UNSIGNED INT64_);
INT: 'int';
UINT: 'uint' | (UNSIGNED 'int');
TYPE: 'type';
OBJECT: 'object';
MODULE: '.module';
VALUE: 'value';
VALUETYPE: 'valuetype';
VOID: 'void';
ENUM: 'enum';
CUSTOM: 'custom';
FIXED: 'fixed';
SYSSTRING: 'systring';
ARRAY: 'array';
VARIANT: 'variant';
CURRENCY: 'currency';
SYSCHAR: 'syschar';
ERROR: 'error';
DECIMAL: 'decimal';
DATE: 'date';
BSTR: 'bstr';
LPSTR: 'lpstr';
LPWSTR: 'lpwstr';
LPTSTR: 'lptstr';
OBJECTREF: 'objectref';
IUNKNOWN: 'iunknown';
IDISPATCH: 'idispatch';
STRUCT: 'struct';
INTERFACE: 'interface';
SAFEARRAY: 'safearray';
NESTEDSTRUCT: 'nested' STRUCT;
VARIANTBOOL: VARIANT BOOL;
BYVALSTR: 'byvalstr';
ANSI: 'ansi';
ANSIBSTR: ANSI BSTR;
TBSTR: 'tbstr';
METHOD: 'method';
ANY: 'any';
LPSTRUCT: 'lpstruct';
VECTOR: 'vector';
HRESULT: 'hresult';
CARRAY: 'carray';
USERDEFINED: 'userdefined';
RECORD: 'record';
FILETIME: 'filetime';
BLOB: 'blob';
STREAM: 'stream';
STORAGE: 'storage';
STREAMED_OBJECT: 'streamed_object';
STORED_OBJECT: 'stored_object';
BLOB_OBJECT: 'blob_object';
CF: 'cf';
CLSID: 'clsid';
INSTANCE: 'instance';
EXPLICIT: 'explicit';
DEFAULT: 'default';
VARARG: 'vararg';
UNMANAGED: 'unmanaged';
CDECL: 'cdecl';
STDCALL: 'stdcall';
THISCALL: 'thiscall';
FASTCALL: 'fastcall';
TYPE_PARAMETER: '!';
METHOD_TYPE_PARAMETER: '!' '!';
TYPEDREF: 'typedref';
NATIVE_INT: 'native' 'int';
NATIVE_UINT: ('native' 'unsigned' 'int') | ('native' 'uint');
PARAM: '.param';
CONSTRAINT: 'constraint';

THIS: '.this';
BASE: '.base';
NESTER: '.nester';
REF: '&';
ARRAY_TYPE_NO_BOUNDS: '[' ']';
PTR: '*';

QSTRING: '"' (~('"' | '\\') | '\\' ('"' | '\\'))* '"';
SQSTRING: '\'' (~('\'' | '\\') | '\\' ('\'' | '\\'))* '\'';
DOT: '.';
PLUS: '+';

PP_DEFINE: '#define';
PP_UNDEF: '#undef';
PP_IFDEF: '#ifdef';
PP_IFNDEF: '#ifndef';
PP_ELSE: '#else';
PP_ENDIF: '#endif';
PP_INCLUDE: '#include';
MRESOURCE: '.mresource';

// ID needs to be last to ensure it doesn't take priority over other token types
fragment IDSTART: [A-Za-z_#$@];
fragment IDCONT: [A-Za-z0-9_#?$@`];
DOTTEDNAME: (ID DOT)+ ID;
ID: IDSTART IDCONT*;

id: ID | SQSTRING;
dottedName: DOTTEDNAME | ((ID '.')* ID);
compQstring: (QSTRING PLUS)* QSTRING;


WS: [ \t\r\n] -> skip;
SINGLE_LINE_COMMENT: '//' ~[\r\n]* -> skip;
COMMENT: '/*' .*? '*/' -> skip;

decls: decl+;

decl:
	classHead '{' classDecls '}'
	| nameSpaceHead '{' decls '}'
	| methodHead '{' methodDecls '}'
	| fieldDecl
	| dataDecl
	| vtableDecl
	| vtfixupDecl
	| extSourceSpec
	| fileDecl
	| assemblyBlock
	| assemblyRefHead '{' assemblyRefDecls '}'
	| exptypeHead '{' exptypeDecls '}'
	| manifestResHead '{' manifestResDecls '}'
	| moduleHead
	| secDecl
	| customAttrDecl
	| subsystem
	| corflags
	| alignment
	| imagebase
	| stackreserve
	| languageDecl
	| typedefDecl
	| compControl
	| typelist
	| mscorlib;

subsystem: '.subsystem' int32;

corflags: '.corflags' int32;

alignment: '.file' 'alignment' int32;

imagebase: '.imagebase' int64;

stackreserve: '.stackreserve' int64;

assemblyBlock:
	'.assembly' asmAttr dottedName '{' assemblyDecls '}';

mscorlib: '.mscorlib';

languageDecl:
	'.language' SQSTRING
	| '.language' SQSTRING ',' SQSTRING
	| '.language' SQSTRING ',' SQSTRING ',' SQSTRING;

typelist: '.typelist' '{' (className)* '}';

int32: INT32;
int64: INT64 | INT32;

float64:
	FLOAT64
	| FLOAT32 '(' int32 ')'
	| FLOAT64_ '(' int64 ')';

intOrWildcard: int32 | PTR;

/* TODO: Handle in custom lexer and have this in the grammar just for completeness */
compControl:
	PP_DEFINE ID
	| PP_DEFINE ID QSTRING
	| PP_UNDEF ID
	| PP_IFDEF ID
	| PP_IFNDEF ID
	| PP_ELSE
	| PP_ENDIF
	| PP_INCLUDE QSTRING
    | ';';


/*  Aliasing of types, type specs, methods, fields and custom attributes */
typedefDecl:
	'.typedef' type 'as' dottedName
	| '.typedef' className 'as' dottedName
	| '.typedef' memberRef 'as' dottedName
	| '.typedef' customDescr 'as' dottedName
	| '.typedef' customDescrWithOwner 'as' dottedName;

/* Custom attribute declarations  */
customDescr:
	'.custom' customType
	| '.custom' customType '=' compQstring
	| '.custom' customType '=' '{' customBlobDescr '}'
	| '.custom' customType '=' '(' bytes ')';

customDescrWithOwner:
	'.custom' '(' ownerType ')' customType
	| '.custom' '(' ownerType ')' customType '=' compQstring
	| '.custom' '(' ownerType ')' customType '=' '{' customBlobDescr '}'
	| '.custom' '(' ownerType ')' customType '=' '(' bytes ')';

customType: methodRef;

ownerType: typeSpec | memberRef;

/*  Verbal description of custom attribute initialization blob  */
customBlobDescr: customBlobArgs customBlobNVPairs;

customBlobArgs: (serInit | compControl)*;

customBlobNVPairs: (
		fieldOrProp serializType dottedName '=' serInit
		| compControl
	)*;

fieldOrProp: 'field' | 'property';

serializType: serializTypeElement (ARRAY_TYPE_NO_BOUNDS)?;

serializTypeElement:
	simpleType
	| dottedName /* typedef */
	| TYPE
	| OBJECT
	| ENUM 'class' SQSTRING
	| ENUM className;

/*  Module declaration */
moduleHead:
	MODULE
	| MODULE dottedName
	| MODULE 'extern' dottedName;

/*  VTable Fixup table declaration  */
vtfixupDecl: '.vtfixup' '[' int32 ']' vtfixupAttr 'at' id;

vtfixupAttr:
	/* EMPTY */
	| vtfixupAttr INT32_
	| vtfixupAttr INT64_
	| vtfixupAttr 'fromunmanaged'
	| vtfixupAttr 'callmostderived'
	| vtfixupAttr 'retainappdomain';

vtableDecl: '.vtable' '=' '(' bytes ')' /* deprecated */;

/*  Namespace and class declaration  */
nameSpaceHead: '.namespace' dottedName;

classHead:
	'.class' classAttr* dottedName typarsClause extendsClause implClause;


classAttr:
	'public'
	| 'private'
	| VALUE
	| ENUM
	| 'interface'
	| 'sealed'
	| 'abstract'
	| 'auto'
	| 'sequential'
	| 'explicit'
	| ANSI
	| 'unicode'
	| 'autochar'
	| 'import'
	| 'serializable'
	| 'windowsruntime'
	| 'nested' 'public'
	| 'nested' 'private'
	| 'nested' 'family'
	| 'nested' 'assembly'
	| 'nested' 'famandassem'
	| 'nested' 'famorassem'
	| 'beforefieldinit'
	| 'specialname'
	| 'rtspecialname'
	| 'flags' '(' int32 ')';

extendsClause: /* EMPTY */ | 'extends' typeSpec;

implClause: /* EMPTY */ | 'implements' implList;

classDecls: classDecl*;

implList: (typeSpec ',')* typeSpec;

/*  External source declarations  */
esHead: '.line' | '#line';

extSourceSpec:
	esHead int32 SQSTRING
	| esHead int32
	| esHead int32 ':' int32 SQSTRING
	| esHead int32 ':' int32
	| esHead int32 ':' int32 ',' int32 SQSTRING
	| esHead int32 ':' int32 ',' int32
	| esHead int32 ',' int32 ':' int32 SQSTRING
	| esHead int32 ',' int32 ':' int32
	| esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING
	| esHead int32 ',' int32 ':' int32 ',' int32
	| esHead int32 QSTRING;

/*  Manifest declarations  */
fileDecl:
	'.file' fileAttr* dottedName fileEntry HASH '=' '(' bytes ')' fileEntry
	| '.file' fileAttr* dottedName fileEntry;

fileAttr: 'nometadata';

fileEntry: /* EMPTY */ | '.entrypoint';

asmAttrAny:
	'retargetable'
	| 'windowsruntime'
	| 'noplatform'
	| 'legacy library'
	| 'cil'
	| 'x86'
	| 'amd64'
	| 'arm'
	| 'arm64';

asmAttr: asmAttrAny*;

/*  IL instructions and associated definitions  */
instr_none: INSTR_NONE;

instr_var: INSTR_VAR;

instr_i: INSTR_I;

instr_i8: INSTR_I8;

instr_r: INSTR_R;

instr_brtarget: INSTR_BRTARGET;

instr_method: INSTR_METHOD;

instr_field: INSTR_FIELD;

instr_type: INSTR_TYPE;

instr_string: INSTR_STRING;

instr_sig: INSTR_SIG;

instr_tok: INSTR_TOK;

instr_switch: INSTR_SWITCH;

instr:
	instr_none
	| instr_var int32
	| instr_var id
	| instr_i int32
	| instr_i8 int64
	| instr_r float64
	| instr_r int64
	| instr_r '(' bytes ')'
	| instr_brtarget int32
	| instr_brtarget id
	| instr_method methodRef
	| instr_field type typeSpec '::' dottedName
	| instr_field type dottedName
	| instr_field mdtoken
	| instr_field dottedName // typedef
	| instr_type typeSpec
	| instr_string compQstring
	| instr_string ANSI '(' compQstring ')'
	| instr_string 'bytearray' '(' bytes ')'
	| instr_sig callConv type sigArgs
	| instr_tok ownerType /* ownerType ::= memberRef | typeSpec */
	| instr_switch '(' labels ')';

labels:
	/* empty */
	| (id | int32 ',')* (id | int32);

typeArgs: '<' (type ',')* type '>';

bounds: '[' (bound ',')* bound ']';

sigArgs: '(' (sigArg ',')* sigArg ')' | '()';

sigArg:
	ELLIPSIS
	| paramAttr type marshalClause
	| paramAttr type marshalClause id;

/*  Class referencing  */

className:
	'[' dottedName ']' slashedName
	| '[' mdtoken ']' slashedName
	| '[' PTR ']' slashedName
	| '[' MODULE dottedName ']' slashedName
	| slashedName
	| mdtoken
	| THIS
	| BASE
	| NESTER;

slashedName: (dottedName '/')* dottedName;

assemblyDecls: assemblyDecl*;

assemblyDecl: (HASH 'algorithm' int32) | secDecl | asmOrRefDecl;

typeSpec:
	className
	| '[' dottedName ']'
	| '[' MODULE dottedName ']'
	| type;

/*  Native types for marshaling signatures  */
nativeType:
	/* EMPTY */
	| nativeTypeElement nativeTypeArrayPointerInfo*;

nativeTypeArrayPointerInfo:
	PTR # PointerNativeType
	| ARRAY_TYPE_NO_BOUNDS # PointerArrayTypeNoSizeData
	| '[' int32 ']' # PointerArrayTypeSize
	| '[' int32 PLUS int32 ']' # PointerArrayTypeSizeParamIndex
	| '[' PLUS int32 ']' # PointerArrayTypeParamIndex
    ;

nativeTypeElement:
	/* EMPTY */
	| marshalType=CUSTOM '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'
	| marshalType=CUSTOM '(' compQstring ',' compQstring ')'
	| FIXED marshalType=SYSSTRING '[' int32 ']'
	| FIXED marshalType=ARRAY '[' int32 ']' nativeType
	| marshalType=VARIANT
	| marshalType=CURRENCY
	| marshalType=SYSCHAR
	| marshalType=VOID
	| marshalType=BOOL
	| marshalType=INT8
	| marshalType=INT16
	| marshalType=INT32_
	| marshalType=INT64_
	| marshalType=FLOAT32
	| marshalType=FLOAT64_
	| marshalType=ERROR
	| marshalType=UINT8
	| marshalType=UINT16
	| marshalType=UINT32
	| marshalType=UINT64
	| marshalType=DECIMAL
	| marshalType=DATE
	| marshalType=BSTR
	| marshalType=LPSTR
	| marshalType=LPWSTR
	| marshalType=LPTSTR
	| marshalType=OBJECTREF
	| marshalType=IUNKNOWN iidParamIndex
	| marshalType=IDISPATCH iidParamIndex
	| marshalType=STRUCT
	| marshalType=INTERFACE iidParamIndex
	| marshalType=SAFEARRAY variantType
	| marshalType=SAFEARRAY variantType ',' compQstring
	| marshalType=INT
	| marshalType=UINT
	| marshalType=NESTEDSTRUCT
	| marshalType=BYVALSTR
	| marshalType=ANSIBSTR
	| marshalType=TBSTR
	| marshalType=VARIANTBOOL
	| marshalType=METHOD
	| marshalType=LPSTRUCT
	| 'as' marshalType=ANY
	| dottedName /* typedef */;

iidParamIndex: /* EMPTY */ | '(' 'iidparam' '=' int32 ')';

variantType:
	/*EMPTY */
	| variantTypeElement (ARRAY_TYPE_NO_BOUNDS | VECTOR | REF)*;

variantTypeElement:
	NULL
	| VARIANT
	| CURRENCY
	| VOID
	| BOOL
	| INT8
	| INT16
	| INT32_
	| INT64_
	| FLOAT32
	| FLOAT64_
	| UINT8
	| UINT16
	| UINT32
	| UINT64
	| PTR
	| DECIMAL
	| DATE
	| BSTR
	| LPSTR
	| LPWSTR
	| IUNKNOWN
	| IDISPATCH
	| SAFEARRAY
	| INT
	| UINT
	| ERROR
	| HRESULT
	| CARRAY
	| USERDEFINED
	| RECORD
	| FILETIME
	| BLOB
	| STREAM
	| STORAGE
	| STREAMED_OBJECT
	| STORED_OBJECT
	| BLOB_OBJECT
	| CF
	| CLSID;

/*  Managed types for signatures  */
type: elementType typeModifiers*;

typeModifiers:
	'[' ']'						# SZArrayModifier
	| bounds					# ArrayModifier
	| REF						# ByRefModifier
	| PTR  				# PtrModifier
	| 'pinned'					# PinnedModifier
	| 'modreq' '(' typeSpec ')'	# RequiredModifier
	| 'modopt' '(' typeSpec ')'	# OptionalModifier
	| typeArgs					# GenericArgumentsModifier;

elementType:
	'class' className
	| OBJECT
	| VALUE 'class' className
	| VALUETYPE className
	| 'method' callConv type PTR sigArgs
	| METHOD_TYPE_PARAMETER int32
	| TYPE_PARAMETER int32
	| METHOD_TYPE_PARAMETER dottedName
	| TYPE_PARAMETER dottedName
	| TYPEDREF
	| VOID
	| NATIVE_INT
	| NATIVE_UINT
	| simpleType
	| dottedName /* typedef */
	| ELLIPSIS type;

simpleType:
	CHAR
	| STRING
	| BOOL
	| INT8
	| INT16
	| INT32_
	| INT64_
	| FLOAT32
	| FLOAT64_
	| UINT8
	| UINT16
	| UINT32
	| UINT64;

bound:
	| ELLIPSIS
	| int32
	| int32 ELLIPSIS int32
	| int32 ELLIPSIS;

/*  Security declarations  */
PERMISSION: '.permission';
PERMISSIONSET: '.permissionset';

secDecl:
	PERMISSION secAction typeSpec '(' nameValPairs ')'
	| PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'
	| PERMISSION secAction typeSpec
	| PERMISSIONSET secAction '=' 'bytearray'? '(' bytes ')'
	| PERMISSIONSET secAction compQstring
	| PERMISSIONSET secAction '=' '{' secAttrSetBlob '}';

secAttrSetBlob: | (secAttrBlob ',')* secAttrBlob;

secAttrBlob:
	typeSpec '=' '{' customBlobNVPairs '}'
	| 'class' SQSTRING '=' '{' customBlobNVPairs '}';

nameValPairs: (nameValPair ',')* nameValPair;

nameValPair: compQstring '=' caValue;

truefalse: 'true' | 'false';

caValue:
	truefalse
	| int32
	| INT32_ '(' int32 ')'
	| compQstring
	| className '(' INT8 ':' int32 ')'
	| className '(' INT16 ':' int32 ')'
	| className '(' INT32_ ':' int32 ')'
	| className '(' int32 ')';

secAction:
	'request'
	| 'demand'
	| 'assert'
	| 'deny'
	| 'permitonly'
	| 'linkcheck'
	| 'inheritcheck'
	| 'reqmin'
	| 'reqopt'
	| 'reqrefuse'
	| 'prejitgrant'
	| 'prejitdeny'
	| 'noncasdemand'
	| 'noncaslinkdemand'
	| 'noncasinheritance';

/*  Method referencing  */
methodRef:
	callConv type typeSpec '::' methodName typeArgs? sigArgs
	| callConv type typeSpec '::' methodName genArityNotEmpty sigArgs
	| callConv type methodName typeArgs? sigArgs
	| callConv type methodName genArityNotEmpty sigArgs
	| mdtoken
	| dottedName /* typeDef */;

callConv:
	INSTANCE callConv
	| EXPLICIT callConv
	| callKind
	| 'callconv' '(' int32 ')';

callKind:
	/* EMPTY */
	| DEFAULT
	| VARARG
	| UNMANAGED CDECL
	| UNMANAGED STDCALL
	| UNMANAGED THISCALL
	| UNMANAGED FASTCALL
	| UNMANAGED;

mdtoken: 'mdtoken' '(' int32 ')';

memberRef:
	'method' methodRef
	| 'field' fieldRef
	| mdtoken;

fieldRef:
	type typeSpec '::' dottedName
	| type dottedName
	| dottedName // typedef
    ;

/* Generic type parameters declaration  */
typeList: (typeSpec ',')* typeSpec;

typarsClause: /* EMPTY */ | '<' typars '>';

typarAttrib:
	covariant = PLUS
	| contravariant = '-'
	| class = 'class'
	| valuetype = VALUETYPE
	| byrefLike = 'byreflike'
	| ctor = '.ctor'
	| 'flags' '(' flags = int32 ')';

typarAttribs: typarAttrib*;

typar: typarAttribs tyBound? dottedName;

typars: (typar ',')* typar;

tyBound: '(' typeList ')';

genArity: /* EMPTY */ | genArityNotEmpty;

genArityNotEmpty: '<' '[' int32 ']' '>';

/*  Class body declarations  */
classDecl:
	methodHead '{' methodDecls '}'
	| classHead '{' classDecls '}'
	| eventHead '{' eventDecls '}'
	| propHead '{' propDecls '}'
	| fieldDecl
	| dataDecl
	| secDecl
	| extSourceSpec
	| customAttrDecl
	| '.size' int32
	| '.pack' int32
	| exportHead '{' exptypeDecls '}'
	| OVERRIDE typeSpec '::' methodName 'with' callConv type typeSpec '::' methodName sigArgs
	| OVERRIDE 'method' callConv type typeSpec '::' methodName genArity sigArgs 'with' 'method'
		callConv type typeSpec '::' methodName genArity sigArgs
	| languageDecl
	| compControl
	| PARAM TYPE '[' int32 ']' customAttrDecl*
	| PARAM TYPE dottedName customAttrDecl*
	| PARAM CONSTRAINT '[' int32 ']' ',' typeSpec customAttrDecl*
	| PARAM CONSTRAINT dottedName ',' typeSpec customAttrDecl*
	| '.interfaceimpl' TYPE typeSpec customDescr;

/*  Field declaration  */
fieldDecl:
	'.field' repeatOpt (fieldAttr | 'marshal' '(' marshalBlob ')')* type dottedName atOpt initOpt;

fieldAttr:
	'static'
	| 'public'
	| 'private'
	| 'family'
	| 'initonly'
	| 'rtspecialname'
	| 'specialname'
	| 'assembly'
	| 'famandassem'
	| 'famorassem'
	| 'privatescope'
	| 'literal'
	| 'notserialized'
	| 'flags' '(' int32 ')';

atOpt: /* EMPTY */ | 'at' id;

initOpt: /* EMPTY */ | '=' fieldInit;

repeatOpt: /* EMPTY */ | '[' int32 ']';

/*  Event declaration  */
eventHead:
	'.event' eventAttr* typeSpec dottedName
	| '.event' eventAttr* dottedName;

eventAttr:
    'rtspecialname'
	| 'specialname';

eventDecls: eventDecl*;

eventDecl:
	'.addon' methodRef
	| '.removeon' methodRef
	| '.fire' methodRef
	| '.other' methodRef
	| extSourceSpec
	| customAttrDecl
	| languageDecl
	| compControl;

/*  Property declaration  */
propHead:
	'.property' propAttr* callConv type dottedName sigArgs initOpt;

propAttr:
	'rtspecialname'
	| 'specialname';

propDecls: propDecl*;

propDecl:
	'.set' methodRef
	| '.get' methodRef
	| '.other' methodRef
	| customAttrDecl
	| extSourceSpec
	| languageDecl
	| compControl;

/*  Method declaration  */

marshalClause: /* EMPTY */ | 'marshal' '(' marshalBlob ')';

marshalBlob: nativeType | '{' hexbytes '}';

paramAttr: paramAttrElement*;

paramAttrElement:
	'[' in = 'in' ']'
	| '[' out = 'out' ']'
	| '[' opt = 'opt' ']'
	| '[' int32 ']';

methodHead:
	'.method' (methAttr | pinvImpl)* callConv paramAttr type marshalClause methodName typarsClause sigArgs
		implAttr*;

methAttr: 'static'
	| 'public'
	| 'private'
	| 'family'
	| 'final'
	| 'specialname'
	| 'virtual'
	| 'strict'
	| 'abstract'
	| 'assembly'
	| 'famandassem'
	| 'famorassem'
	| 'privatescope'
	| 'hidebysig'
	| 'newslot'
	| 'rtspecialname'
	| 'unmanagedexp'
	| 'reqsecobj'
	| 'flags' '(' int32 ')';

pinvImpl: 'pinvokeimpl' '(' (compQstring ('as' compQstring)?)? pinvAttr* ')';

pinvAttr:
	'nomangle'
	| 'ansi'
	| 'unicode'
	| 'autochar'
	| 'lasterr'
	| 'winapi'
	| 'cdecl'
	| 'stdcall'
	| 'thiscall'
	| 'fastcall'
	| 'bestfit' ':' 'on'
	| 'bestfit' ':' 'off'
	| 'charmaperror' ':' 'on'
	| 'charmaperror' ':' 'off'
	| 'flags' '(' int32 ')';

methodName: '.ctor' | '.cctor' | dottedName;

implAttr:
	'native'
	| 'cil'
	| 'optil'
	| 'managed'
	| 'unmanaged'
	| 'forwardref'
	| 'preservesig'
	| 'runtime'
	| 'internalcall'
	| 'synchronized'
	| 'noinlining'
	| 'aggressiveinlining'
	| 'nooptimization'
	| 'aggressiveoptimization'
	| 'flags' '(' int32 ')';

EMITBYTE: '.emitbyte';
MAXSTACK: '.maxstack';
ENTRYPOINT: '.entrypoint';
ZEROINIT: '.zeroinit';
LOCALS: '.locals';
EXPORT: '.export';
OVERRIDE: '.override';
VTENTRY: '.vtentry';

methodDecls: methodDecl*;

methodDecl:
	EMITBYTE int32
	| sehBlock
	| MAXSTACK int32
	| LOCALS sigArgs
	| LOCALS 'init' sigArgs
	| ENTRYPOINT
	| ZEROINIT
	| dataDecl
	| instr
	| id ':'
	| secDecl
	| extSourceSpec // Leave for later when I get to generating symbols.
	| languageDecl // Leave for later when I get to generating symbols.
	| customAttrDecl
	| compControl
	| EXPORT '[' int32 ']'
	| EXPORT '[' int32 ']' 'as' id
	| VTENTRY int32 ':' int32
	| OVERRIDE typeSpec '::' methodName
	| OVERRIDE 'method' callConv type typeSpec '::' methodName genArity sigArgs
	| scopeBlock
	| PARAM TYPE '[' int32 ']' customAttrDecl*
	| PARAM TYPE dottedName customAttrDecl*
	| PARAM CONSTRAINT '[' int32 ']' ',' typeSpec customAttrDecl*
	| PARAM CONSTRAINT dottedName ',' typeSpec customAttrDecl*
	| PARAM '[' int32 ']' initOpt customAttrDecl*;

scopeBlock: '{' methodDecls '}';

/* Structured exception handling directives  */
sehBlock: tryBlock sehClauses;

sehClauses: sehClause+;

tryBlock:
	'.try' scopeBlock
	| '.try' id 'to' id
	| '.try' int32 'to' int32;

sehClause:
	catchClause handlerBlock
	| filterClause handlerBlock
	| finallyClause handlerBlock
	| faultClause handlerBlock;

filterClause:
	'filter' scopeBlock
	| 'filter' id
	| 'filter' int32;

catchClause: 'catch' typeSpec;

finallyClause: 'finally';

faultClause: 'fault';

handlerBlock:
	scopeBlock
	| 'handler' id 'to' id
	| 'handler' int32 'to' int32;

/*  Data declaration  */
dataDecl: ddHead ddBody;

ddHead: '.data' tls id '=' | '.data' tls;

tls: /* EMPTY */ | 'tls' | 'cil';

ddBody: '{' ddItemList '}' | ddItem;

ddItemList: (ddItem ',')* ddItem;

ddItemCount: /* EMPTY */ | '[' int32 ']';

ddItem:
	CHAR PTR '(' compQstring ')'
	| REF '(' id ')'
	| 'bytearray' '(' bytes ')'
	| FLOAT32 '(' float64 ')' ddItemCount
	| FLOAT64_ '(' float64 ')' ddItemCount
	| INT64_ '(' int64 ')' ddItemCount
	| INT32_ '(' int32 ')' ddItemCount
	| INT16 '(' int32 ')' ddItemCount
	| INT8 '(' int32 ')' ddItemCount
	| FLOAT32 ddItemCount
	| FLOAT64_ ddItemCount
	| INT64_ ddItemCount
	| INT32_ ddItemCount
	| INT16 ddItemCount
	| INT8 ddItemCount;

/*  Default values declaration for fields, parameters and verbal form of CA blob description  */
fieldSerInit:
	FLOAT32 '(' float64 ')'
	| FLOAT64_ '(' float64 ')'
	| FLOAT32 '(' int32 ')'
	| FLOAT64_ '(' int64 ')'
	| INT64_ '(' int64 ')'
	| INT32_ '(' int32 ')'
	| INT16 '(' int32 ')'
	| INT8 '(' int32 ')'
	| UINT64 '(' int64 ')'
	| UINT32 '(' int32 ')'
	| UINT16 '(' int32 ')'
	| UINT8 '(' int32 ')'
	| CHAR '(' int32 ')'
	| BOOL '(' truefalse ')'
	| 'bytearray' '(' bytes ')';

bytes: hexbytes*;

hexbytes: HEXBYTE+;
/*  Field/parameter initialization  */
fieldInit: fieldSerInit | compQstring | NULLREF;

/*  Values for verbal form of CA blob description  */
serInit:
	fieldSerInit
	| STRING '(' NULLREF ')'
	| STRING '(' SQSTRING ')'
	| TYPE '(' 'class' SQSTRING ')'
	| TYPE '(' className ')'
	| TYPE '(' NULLREF ')'
	| OBJECT '(' serInit ')'
	| FLOAT32 '[' int32 ']' '(' f32seq ')'
	| FLOAT64_ '[' int32 ']' '(' f64seq ')'
	| INT64_ '[' int32 ']' '(' i64seq ')'
	| INT32_ '[' int32 ']' '(' i32seq ')'
	| INT16 '[' int32 ']' '(' i16seq ')'
	| INT8 '[' int32 ']' '(' i8seq ')'
	| UINT64 '[' int32 ']' '(' i64seq ')'
	| UINT32 '[' int32 ']' '(' i32seq ')'
	| UINT16 '[' int32 ']' '(' i16seq ')'
	| UINT8 '[' int32 ']' '(' i8seq ')'
	| CHAR '[' int32 ']' '(' i16seq ')'
	| BOOL '[' int32 ']' '(' boolSeq ')'
	| STRING '[' int32 ']' '(' sqstringSeq ')'
	| TYPE '[' int32 ']' '(' classSeq ')'
	| OBJECT '[' int32 ']' '(' objSeq ')';

f32seq: (float64 | int32)*;

f64seq: (float64 | int64)*;

i64seq: int64*;

i32seq: int32*;

i16seq: int32*;

i8seq: int32*;

boolSeq: truefalse*;

sqstringSeq: (NULLREF | SQSTRING)*;

classSeq: classSeqElement*;

classSeqElement: NULLREF | 'class' SQSTRING | className;

objSeq: serInit*;

customAttrDecl:
	customDescr
	| customDescrWithOwner
	| dottedName /* typedef */;

/* Assembly References */
asmOrRefDecl:
	'.publicKey' '=' '(' bytes ')'
	| '.ver' intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard
	| '.locale' compQstring
	| '.locale' '=' '(' bytes ')'
	| customAttrDecl
	| compControl;

assemblyRefHead:
	'.assembly' 'extern' asmAttr dottedName
	| '.assembly' 'extern' asmAttr dottedName 'as' dottedName;

assemblyRefDecls: assemblyRefDecl*;

assemblyRefDecl:
	'.hash' '=' '(' bytes ')'
	| asmOrRefDecl
	| '.publickeytoken' '=' '(' bytes ')'
	| 'auto';

exptypeHead: '.class' 'extern' exptAttr* dottedName;

exportHead: '.export' exptAttr* dottedName;

exptAttr:
	'private'
	| 'public'
	| 'forwarder'
	| 'nested' 'public'
	| 'nested' 'private'
	| 'nested' 'family'
	| 'nested' 'assembly'
	| 'nested' 'famandassem'
	| 'nested' 'famorassem';

exptypeDecls: exptypeDecl*;

exptypeDecl:
	'.file' dottedName
	| '.class' 'extern' slashedName
	| '.assembly' 'extern' dottedName
	| mdtoken
	| '.class' int32
	| customAttrDecl
	| compControl;

manifestResHead:
	MRESOURCE manresAttr* dottedName
	| MRESOURCE manresAttr* dottedName 'as' dottedName;

manresAttr: 'public' | 'private';

manifestResDecls: manifestResDecl*;

manifestResDecl:
	'.file' dottedName 'at' int32
	| '.assembly' 'extern' dottedName
	| customAttrDecl
	| compControl;

