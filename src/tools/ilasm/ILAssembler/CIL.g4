grammar CIL;

import Instructions;

ID: [A-Za-z_][A-Za-z0-9_]*;
INT32: '-'? ('0x' [0-9A-Fa-f]+ | [0-9]+);
INT64: '-'? ('0x' [0-9A-Fa-f]+ | [0-9]+);
FLOAT64: '-'? [0-9]+ ('.' [0-9]+ | [eE] '-'? [0-9]+);
HEXBYTE: [0-9A-Fa-f][0-9A-Fa-f];
DCOLON: '::';
ELLIPSIS: '..';
QSTRING: '"' (~('"' | '\\') | '\\' ('"' | '\\'))* '"';
SQSTRING: '\'' (~('\'' | '\\') | '\\' ('\'' | '\\'))* '\'';
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
UNSIGNED: 'unsigned';
UINT8: 'uint8';
UINT16: 'uint16';
UINT32: 'uint32';
UINT64: 'uint64';
TYPE: 'type';
OBJECT: 'object';

WHITESPACE: [ \r\n] -> skip;

decls: decl+;

decl:
	classHead '{' classDecls '}'
	| nameSpaceHead '{' decls '}'
	| methodHead methodDecls '}'
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

id: ID | SQSTRING;
dottedName: (ID '.')* ID;

int32: INT32;
int64: INT64 | INT32;

float64:
	FLOAT64
	| FLOAT32 '(' int32 ')'
	| FLOAT64_ '(' int64 ')';

intOrWildcard: int32 | '*';

compQstring: (QSTRING '+')* QSTRING;

/*  Aliasing of types, type specs, methods, fields and custom attributes */
typedefDecl:
	'.typedef' type 'as' dottedName
	| '.typedef' className 'as' dottedName
	| '.typedef' memberRef 'as' dottedName
	| '.typedef' customDescr 'as' dottedName
	| '.typedef' customDescrWithOwner 'as' dottedName;

/* TODO: Handle in custom lexer and have this in the grammar just for completeness */
compControl:
	'#define' dottedName
	| '#define' dottedName compQstring
	| '#undef' dottedName
	| '#ifdef' dottedName
	| '#ifndef' dottedName
	| '#else'
	| '#endif'
	| '#include' QSTRING
	| ';';

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
		fieldOrProp seralizType dottedName '=' serInit
		| compControl
	)*;

fieldOrProp: 'field' | 'property';

seralizType: seralizTypeElement ('[' ']')?;

seralizTypeElement:
	simpleType
	| TYPE
	| OBJECT
	| 'enum' 'class' SQSTRING
	| 'enum' className;

/*  Module declaration */
moduleHead:
	'.module'
	| '.module' dottedName
	| '.module' 'extern' dottedName;

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

classHeadBegin: '.class' classAttr* dottedName typarsClause;
classHead: classHeadBegin extendsClause implClause;

classAttr:
	'public'
	| 'private'
	| 'value'
	| 'enum'
	| 'interface'
	| 'sealed'
	| 'abstract'
	| 'auto'
	| 'sequential'
	| 'explicit'
	| 'ansi'
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

classDecls: /* EMPTY */ | classDecls classDecl;

implList: implList ',' typeSpec | typeSpec;

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
	'.file' fileAttr dottedName fileEntry HASH '=' '(' bytes ')' fileEntry
	| '.file' fileAttr dottedName fileEntry;

fileAttr: /* EMPTY */ | fileAttr 'nometadata';

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
methodSpec: 'method';

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

instr_r_head: instr_r '(';

instr:
	instr_none
	| instr_var int32
	| instr_var id
	| instr_i int32
	| instr_i8 int64
	| instr_r float64
	| instr_r int64
	| instr_r_head bytes ')'
	| instr_brtarget int32
	| instr_brtarget id
	| instr_method methodRef
	| instr_field type typeSpec '::' dottedName
	| instr_field type dottedName
	| instr_field mdtoken
	| instr_field dottedName
	| instr_type typeSpec
	| instr_string compQstring
	| instr_string 'ansi' '(' compQstring ')'
	| instr_string 'bytearray' '(' bytes ')'
	| instr_sig callConv type sigArgs
	| instr_tok ownerType /* ownerType ::= memberRef | typeSpec */
	| instr_switch '(' labels ')';

labels:
	/* empty */
	| id ',' labels
	| int32 ',' labels
	| id
	| int32;

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
	| '[' '*' ']' slashedName
	| '[' '.module' dottedName ']' slashedName
	| slashedName
	| mdtoken
	| dottedName // typeDef
	| '.this'
	| '.base'
	| '.nester';

slashedName: (dottedName '/')* dottedName;

assemblyDecls: assemblyDecl*;

assemblyDecl: (HASH 'algorithm' int32) | secDecl | asmOrRefDecl;

typeSpec:
	className
	| '[' dottedName ']'
	| '[' '.module' dottedName ']'
	| type;

/*  Native types for marshaling signatures  */
nativeType:
	/* EMPTY */
	| nativeTypeElement (
		'*'
		| '[' ']'
		| '[' int32 ']'
		| '[' int32 '+' int32 ']'
		| '[' '+' int32 ']'
	)*;

nativeTypeElement:
	/* EMPTY */
	| 'custom' '(' compQstring ',' compQstring ',' compQstring ',' compQstring ')'
	| 'custom' '(' compQstring ',' compQstring ')'
	| 'fixed' 'sysstring' '[' int32 ']'
	| 'fixed' 'array' '[' int32 ']' nativeType
	| 'variant'
	| 'currency'
	| 'syschar'
	| 'void'
	| BOOL
	| INT8
	| INT16
	| INT32_
	| INT64_
	| FLOAT32
	| FLOAT64_
	| 'error'
	| UNSIGNED INT8
	| UNSIGNED INT16
	| UNSIGNED INT32_
	| UNSIGNED INT64_
	| UINT8
	| UINT16
	| UINT32
	| UINT64
	| 'decimal'
	| 'date'
	| 'bstr'
	| 'lpstr'
	| 'lpwstr'
	| 'lptstr'
	| 'objectref'
	| 'iunknown' iidParamIndex
	| 'idispatch' iidParamIndex
	| 'struct'
	| 'interface' iidParamIndex
	| 'safearray' variantType
	| 'safearray' variantType ',' compQstring
	| 'int'
	| UNSIGNED 'int'
	| 'uint'
	| 'nested' 'struct'
	| 'byvalstr'
	| 'ansi' 'bstr'
	| 'tbstr'
	| 'variant' BOOL
	| 'method'
	| 'as' 'any'
	| 'lpstruct'
	| dottedName /* typedef */;

iidParamIndex: /* EMPTY */ | '(' 'iidparam' '=' int32 ')';

variantType: variantTypeElement ('[' ']' | 'vector' | '&')*;

variantTypeElement:
	/* EMPTY */
	| 'null'
	| 'variant'
	| 'currency'
	| 'void'
	| BOOL
	| INT8
	| INT16
	| INT32_
	| INT64_
	| FLOAT32
	| FLOAT64_
	| UNSIGNED INT8
	| UNSIGNED INT16
	| UNSIGNED INT32_
	| UNSIGNED INT64_
	| UINT8
	| UINT16
	| UINT32
	| UINT64
	| '*'
	| 'decimal'
	| 'date'
	| 'bstr'
	| 'lpstr'
	| 'lpwstr'
	| 'iunknown'
	| 'idispatch'
	| 'safearray'
	| 'int'
	| UNSIGNED 'int'
	| 'uint'
	| 'error'
	| 'hresult'
	| 'carray'
	| 'userdefined'
	| 'record'
	| 'filetime'
	| 'blob'
	| 'stream'
	| 'storage'
	| 'streamed_object'
	| 'stored_object'
	| 'blob_object'
	| 'cf'
	| 'clsid';

/*  Managed types for signatures  */
type:
	elementType (
		'[' ']'
		| bounds
		| '&'
		| '*'
		| 'pinned'
		| 'modreq' '(' typeSpec ')'
		| 'modopt' '(' typeSpec ')'
		| typeArgs
	)*;

elementType:
	'class' className
	| OBJECT
	| 'value' 'class' className
	| 'valuetype' className
	| 'method' callConv type '*' sigArgs
	| '!' '!' int32
	| '!' int32
	| '!' '!' dottedName
	| '!' dottedName
	| 'typedref'
	| 'void'
	| 'native' 'int'
	| 'native' UNSIGNED 'int'
	| 'native' 'uint'
	| simpleType
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
	| UNSIGNED INT8
	| UNSIGNED INT16
	| UNSIGNED INT32_
	| UNSIGNED INT64_
	| UINT8
	| UINT16
	| UINT32
	| UINT64
	| dottedName /* typedef */;

bound:
	| ELLIPSIS
	| int32
	| int32 ELLIPSIS int32
	| int32 ELLIPSIS;

/*  Security declarations  */
secDecl:
	'.permission' secAction typeSpec '(' nameValPairs ')'
	| '.permission' secAction typeSpec '=' '{' customBlobDescr '}'
	| '.permission' secAction typeSpec
	| '.permissionset' secAction '=' 'bytearray'? '(' bytes ')'
	| '.permissionset' secAction compQstring
	| '.permissionset' secAction '=' '{' secAttrSetBlob '}';

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
	'instance' callConv
	| 'explicit' callConv
	| callKind
	| 'callconv' '(' int32 ')';

callKind:
	/* EMPTY */
	| 'default'
	| 'vararg'
	| 'unmanaged' 'cdecl'
	| 'unmanaged' 'stdcall'
	| 'unmanaged' 'thiscall'
	| 'unmanaged' 'fastcall'
	| 'unmanaged';

mdtoken: 'mdtoken' '(' int32 ')';

memberRef:
	'method' methodRef
	| 'field' type typeSpec '::' dottedName
	| 'field' type dottedName
	| 'field' dottedName /* typedef */
	| mdtoken;

/* Generic type parameters declaration  */
typeList: /* EMPTY */ | typeListNotEmpty;

typeListNotEmpty: typeSpec | typeListNotEmpty ',' typeSpec;

typarsClause: /* EMPTY */ | '<' typars '>';

typarAttrib:
	'+'
	| '-'
	| 'class'
	| 'valuetype'
	| 'byreflike'
	| '.ctor'
	| 'flags' '(' int32 ')';

typarAttribs: /* EMPTY */ | typarAttrib typarAttribs;

typars:
	typarAttribs tyBound dottedName typarsRest
	| typarAttribs dottedName typarsRest;

typarsRest: /* EMPTY */ | ',' typars;

tyBound: '(' typeList ')';

genArity: /* EMPTY */ | genArityNotEmpty;

genArityNotEmpty: '<' '[' int32 ']' '>';

/*  Class body declarations  */
classDecl:
	methodHead methodDecls '}'
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
	| '.override' typeSpec '::' methodName 'with' callConv type typeSpec '::' methodName sigArgs
	| '.override' 'method' callConv type typeSpec '::' methodName genArity sigArgs 'with' 'method'
		callConv type typeSpec '::' methodName genArity sigArgs
	| languageDecl
	| compControl
	| '.param' TYPE '[' int32 ']'
	| '.param' TYPE dottedName
	| '.param' 'constraint' '[' int32 ']' ',' typeSpec
	| '.param' 'constraint' dottedName ',' typeSpec
	| '.interfaceimpl' TYPE typeSpec customDescr;

/*  Field declaration  */
fieldDecl:
	'.field' repeatOpt fieldAttr* type dottedName atOpt initOpt;

fieldAttr:
	'static'
	| 'public'
	| 'private'
	| 'family'
	| 'initonly'
	| 'rtspecialname'
	| 'specialname'
	| 'marshal' '(' marshalBlob ')'
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
	'.event' eventAttr typeSpec dottedName
	| '.event' eventAttr dottedName;

eventAttr:
	/* EMPTY */
	| eventAttr 'rtspecialname'
	| eventAttr 'specialname';

eventDecls: /* EMPTY */ | eventDecls eventDecl;

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
	'.property' propAttr callConv type dottedName sigArgs initOpt;

propAttr:
	/* EMPTY */
	| propAttr 'rtspecialname'
	| propAttr 'specialname';

propDecls: /* EMPTY */ | propDecls propDecl;

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

marshalBlobHead: '{';

paramAttr: paramAttrElement*;

paramAttrElement:
	'[' 'in' ']'
	| '[' 'out' ']'
	| '[' 'opt' ']'
	| '[' int32 ']';

methodHead:
	'.method' methAttr callConv paramAttr type marshalClause methodName typarsClause sigArgs
		implAttr '{';

methAttr:
	/* EMPTY */
	| methAttr 'static'
	| methAttr 'public'
	| methAttr 'private'
	| methAttr 'family'
	| methAttr 'final'
	| methAttr 'specialname'
	| methAttr 'virtual'
	| methAttr 'strict'
	| methAttr 'abstract'
	| methAttr 'assembly'
	| methAttr 'famandassem'
	| methAttr 'famorassem'
	| methAttr 'privatescope'
	| methAttr 'hidebysig'
	| methAttr 'newslot'
	| methAttr 'rtspecialname'
	| methAttr 'unmanagedexp'
	| methAttr 'reqsecobj'
	| methAttr 'flags' '(' int32 ')'
	| methAttr 'pinvokeimpl' '(' compQstring 'as' compQstring pinvAttr* ')'
	| methAttr 'pinvokeimpl' '(' compQstring pinvAttr* ')'
	| methAttr 'pinvokeimpl' '(' pinvAttr* ')';

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
	/* EMPTY */
	| implAttr 'native'
	| implAttr 'cil'
	| implAttr 'optil'
	| implAttr 'managed'
	| implAttr 'unmanaged'
	| implAttr 'forwardref'
	| implAttr 'preservesig'
	| implAttr 'runtime'
	| implAttr 'internalcall'
	| implAttr 'synchronized'
	| implAttr 'noinlining'
	| implAttr 'aggressiveinlining'
	| implAttr 'nooptimization'
	| implAttr 'aggressiveoptimization'
	| implAttr 'flags' '(' int32 ')';

localsHead: '.locals';

methodDecls: /* EMPTY */ | methodDecls methodDecl;

methodDecl:
	'.emitbyte' int32
	| sehBlock
	| '.maxstack' int32
	| localsHead sigArgs
	| localsHead 'init' sigArgs
	| '.entrypoint'
	| '.zeroinit'
	| dataDecl
	| instr
	| id ':'
	| secDecl
	| extSourceSpec
	| languageDecl
	| customAttrDecl
	| compControl
	| '.export' '[' int32 ']'
	| '.export' '[' int32 ']' 'as' id
	| '.vtentry' int32 ':' int32
	| '.override' typeSpec '::' methodName
	| '.override' 'method' callConv type typeSpec '::' methodName genArity sigArgs
	| scopeBlock
	| '.param' TYPE '[' int32 ']'
	| '.param' TYPE dottedName
	| '.param' 'constraint' '[' int32 ']' ',' typeSpec
	| '.param' 'constraint' dottedName ',' typeSpec
	| '.param' '[' int32 ']' initOpt;

scopeBlock: '{' methodDecls '}';

/* Structured exception handling directives  */
sehBlock: tryBlock sehClauses;

sehClauses: sehClause+;

tryBlock:
	tryHead scopeBlock
	| tryHead id 'to' id
	| tryHead int32 'to' int32;

tryHead: '.try';

sehClause:
	catchClause handlerBlock
	| filterClause handlerBlock
	| finallyClause handlerBlock
	| faultClause handlerBlock;

filterClause:
	filterHead scopeBlock
	| filterHead id
	| filterHead int32;

filterHead: 'filter';

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

ddItemList: ddItem ',' ddItemList | ddItem;

ddItemCount: /* EMPTY */ | '[' int32 ']';

ddItem:
	CHAR '*' '(' compQstring ')'
	| '&' '(' id ')'
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
	| UNSIGNED INT64_ '(' int64 ')'
	| UNSIGNED INT32_ '(' int32 ')'
	| UNSIGNED INT16 '(' int32 ')'
	| UNSIGNED INT8 '(' int32 ')'
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
	| UNSIGNED INT64_ '[' int32 ']' '(' i64seq ')'
	| UNSIGNED INT32_ '[' int32 ']' '(' i32seq ')'
	| UNSIGNED INT16 '[' int32 ']' '(' i16seq ')'
	| UNSIGNED INT8 '[' int32 ']' '(' i8seq ')'
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

assemblyRefDecls:
	/* EMPTY */
	| assemblyRefDecls assemblyRefDecl;

assemblyRefDecl:
	'.hash' '=' '(' bytes ')'
	| asmOrRefDecl
	| '.publickeytoken' '=' '(' bytes ')'
	| 'auto';

exptypeHead: '.class' 'extern' exptAttr dottedName;

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
	| 'mdtoken' '(' int32 ')'
	| '.class' int32
	| customAttrDecl
	| compControl;

manifestResHead:
	'.mresource' manresAttr* dottedName
	| '.mresource' manresAttr* dottedName 'as' dottedName;

manresAttr: 'public' | 'private';

manifestResDecls:
	/* EMPTY */
	| manifestResDecls manifestResDecl;

manifestResDecl:
	'.file' dottedName 'at' int32
	| '.assembly' 'extern' dottedName
	| customAttrDecl
	| compControl;

