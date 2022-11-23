grammar CIL;

import Instructions;


ID: [A-Za-z_][A-Za-z0-9_]*;
INT32: ('0x' [0-9A-Fa-f]+ | '-'? [0-9]+);
INT64: ('0x' [0-9A-Fa-f]+ | '-'? [0-9]+);
FLOAT64: '-'? [0-9]+ ('.' [0-9]+ | [eE] '-'? [0-9]+);
HEXBYTE: [0-9A-Fa-f][0-9A-Fa-f];
DCOLON: '::';
ELLIPSIS: '..';
QSTRING: '"' (~('"' | '\\') | '\\' ('"' | '\\'))* '"';
SQSTRING: '\'' (~('\'' | '\\') | '\\' ('\'' | '\\'))* '\'';

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
	| 'float32' '(' int32 ')'
	| 'float64' '(' int64 ')';

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
	| 'type'
	| 'object'
	| 'enum' 'class' SQSTRING
	| 'enum' className;


/*  Module declaration */
moduleHead              : '.module'
                        | '.module' dottedName
                        | '.module' 'extern' dottedName
                        ;

/*  VTable Fixup table declaration  */
vtfixupDecl             : '.vtfixup' '[' int32 ']' vtfixupAttr 'at' id
                        ;

vtfixupAttr             : /* EMPTY */
                        | vtfixupAttr 'int32'
                        | vtfixupAttr 'int64'
                        | vtfixupAttr 'fromunmanaged'
                        | vtfixupAttr 'callmostderived'
                        | vtfixupAttr 'retainappdomain'
                        ;

vtableDecl              : '.vtable' '=' '('  bytes ')'   /* deprecated */
                        ;

/*  Namespace and class declaration  */
nameSpaceHead           : '.namespace' dottedName
                        ;

classHeadBegin          : '.class' classAttr* dottedName typarsClause
                        ;
classHead               : classHeadBegin extendsClause implClause
                        ;

classAttr               : 'public'
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
                        | 'flags' '(' int32 ')'
                        ;

extendsClause           : /* EMPTY */
                        | 'extends' typeSpec
                        ;

implClause              : /* EMPTY */
                        | 'implements' implList
                        ;

classDecls              : /* EMPTY */
                        | classDecls classDecl
                        ;

implList                : implList ',' typeSpec
                        | typeSpec
                                        ;


/*  External source declarations  */
esHead                  : '.line'
                        | '#line'
                        ;

extSourceSpec           : esHead int32 SQSTRING
                        | esHead int32
                        | esHead int32 ':' int32 SQSTRING
                        | esHead int32 ':' int32
                        | esHead int32 ':' int32 ',' int32 SQSTRING
                        | esHead int32 ':' int32 ',' int32
                        | esHead int32 ',' int32 ':' int32 SQSTRING
                        | esHead int32 ',' int32 ':' int32
                        | esHead int32 ',' int32 ':' int32 ',' int32 SQSTRING
                        | esHead int32 ',' int32 ':' int32 ',' int32
                        | esHead int32 QSTRING
                        ;

/*  Manifest declarations  */
fileDecl                : '.file' fileAttr dottedName fileEntry '.hash' '=' '(' bytes ')' fileEntry
                        | '.file' fileAttr dottedName fileEntry
                        ;

fileAttr                : /* EMPTY */
                        | fileAttr 'nometadata'
                        ;

fileEntry               : /* EMPTY */
                        | '.entrypoint'
                        ;


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
methodSpec              : 'method'
                        ;

instr_none              : INSTR_NONE
                        ;

instr_var               : INSTR_VAR
                        ;

instr_i                 : INSTR_I
                        ;

instr_i8                : INSTR_I8
                        ;

instr_r                 : INSTR_R
                        ;

instr_brtarget          : INSTR_BRTARGET
                        ;

instr_method            : INSTR_METHOD
                        ;

instr_field             : INSTR_FIELD
                        ;

instr_type              : INSTR_TYPE
                        ;

instr_string            : INSTR_STRING
                        ;

instr_sig               : INSTR_SIG
                        ;

instr_tok               : INSTR_TOK
                        ;

instr_switch            : INSTR_SWITCH
                        ;

instr_r_head            : instr_r '('
                        ;


instr                   : instr_none
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
                        | instr_switch '(' labels ')'
                        ;

labels                  : /* empty */
                        | id ',' labels
                        | int32 ',' labels
                        | id
                        | int32
                        ;

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

assemblyDecl: '.hash algorithm' int32 | secDecl | asmOrRefDecl;

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
	| 'bool'
	| 'int8'
	| 'int16'
	| 'int32'
	| 'int64'
	| 'float32'
	| 'float64'
	| 'error'
	| 'unsigned' 'int8'
	| 'unsigned' 'int16'
	| 'unsigned' 'int32'
	| 'unsigned' 'int64'
	| 'uint8'
	| 'uint16'
	| 'uint32'
	| 'uint64'
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
	| 'unsigned' 'int'
	| 'uint'
	| 'nested' 'struct'
	| 'byvalstr'
	| 'ansi' 'bstr'
	| 'tbstr'
	| 'variant' 'bool'
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
	| 'bool'
	| 'int8'
	| 'int16'
	| 'int32'
	| 'int64'
	| 'float32'
	| 'float64'
	| 'unsigned' 'int8'
	| 'unsigned' 'int16'
	| 'unsigned' 'int32'
	| 'unsigned' 'int64'
	| 'uint8'
	| 'uint16'
	| 'uint32'
	| 'uint64'
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
	| 'unsigned' 'int'
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
	| 'object'
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
	| 'native' 'unsigned' 'int'
	| 'native' 'uint'
	| simpleType
	| ELLIPSIS type;

simpleType:
	'char'
	| 'string'
	| 'bool'
	| 'int8'
	| 'int16'
	| 'int32'
	| 'int64'
	| 'float32'
	| 'float64'
	| 'unsigned' 'int8'
	| 'unsigned' 'int16'
	| 'unsigned' 'int32'
	| 'unsigned' 'int64'
	| 'uint8'
	| 'uint16'
	| 'uint32'
	| 'uint64'
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
	| 'int32' '(' int32 ')'
	| compQstring
	| className '(' 'int8' ':' int32 ')'
	| className '(' 'int16' ':' int32 ')'
	| className '(' 'int32' ':' int32 ')'
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
typeList                : /* EMPTY */
                        | typeListNotEmpty
                        ;

typeListNotEmpty        : typeSpec
                        | typeListNotEmpty ',' typeSpec
                        ;

typarsClause            : /* EMPTY */
                        | '<' typars '>'
                        ;

typarAttrib             : '+'
                        | '-'
                        | 'class'
                        | 'valuetype'
                        | 'byreflike'
                        | '.ctor'
                        | 'flags' '(' int32 ')'
                        ;

typarAttribs            : /* EMPTY */
                        | typarAttrib typarAttribs
                        ;

typars                  : typarAttribs tyBound dottedName typarsRest
                        | typarAttribs dottedName typarsRest
                        ;

typarsRest              : /* EMPTY */
                        | ',' typars
                        ;

tyBound                 : '(' typeList ')'
                        ;

genArity: /* EMPTY */ | genArityNotEmpty;

genArityNotEmpty: '<' '[' int32 ']' '>';

/*  Class body declarations  */
classDecl               : methodHead  methodDecls '}'
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
                        | '.override' 'method' callConv type typeSpec '::' methodName genArity sigArgs 'with' 'method' callConv type typeSpec '::' methodName genArity sigArgs
                        | languageDecl
                        | compControl
                        | '.param' 'type' '[' int32 ']'
                        | '.param' 'type' dottedName
                        | '.param' 'constraint' '[' int32 ']' ',' typeSpec
                        | '.param' 'constraint' dottedName ',' typeSpec
                        | '.interfaceimpl' 'type' typeSpec customDescr
                        ;

/*  Field declaration  */
fieldDecl               : '.field' repeatOpt fieldAttr* type dottedName atOpt initOpt
                        ;

fieldAttr               : 'static'
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
                        | 'flags' '(' int32 ')'
                        ;

atOpt                   : /* EMPTY */
                        | 'at' id
                        ;

initOpt                 : /* EMPTY */
                        | '=' fieldInit
                                                ;

repeatOpt               : /* EMPTY */
                        | '[' int32 ']'
                                                ;

/*  Event declaration  */
eventHead               : '.event' eventAttr typeSpec dottedName
                        | '.event' eventAttr dottedName
                        ;


eventAttr               : /* EMPTY */
                        | eventAttr 'rtspecialname'
                        | eventAttr 'specialname'
                        ;

eventDecls              : /* EMPTY */
                        | eventDecls eventDecl
                        ;

eventDecl               : '.addon' methodRef
                        | '.removeon' methodRef
                        | '.fire' methodRef
                        | '.other' methodRef
                        | extSourceSpec
                        | customAttrDecl
                        | languageDecl
                        | compControl
                        ;

/*  Property declaration  */
propHead                : '.property' propAttr callConv type dottedName sigArgs initOpt
                        ;

propAttr                : /* EMPTY */
                        | propAttr 'rtspecialname'
                        | propAttr 'specialname'
                        ;

propDecls               : /* EMPTY */
                        | propDecls propDecl
                        ;


propDecl                : '.set' methodRef
                        | '.get' methodRef
                        | '.other' methodRef
                        | customAttrDecl
                        | extSourceSpec
                        | languageDecl
                        | compControl
                        ;

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

methodHead              : '.method' methAttr callConv paramAttr type marshalClause methodName typarsClause sigArgs implAttr '{'
                        ;

methAttr                : /* EMPTY */
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
                        | methAttr 'pinvokeimpl' '(' compQstring  pinvAttr* ')'
                        | methAttr 'pinvokeimpl' '(' pinvAttr* ')'
                        ;

pinvAttr                : 'nomangle'
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
                        | 'flags' '(' int32 ')'
                        ;

methodName              : '.ctor'
                        | '.cctor'
                        | dottedName
                        ;

implAttr                : /* EMPTY */
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
                        | implAttr 'flags' '(' int32 ')'
                        ;

localsHead              : '.locals'
                        ;

methodDecls             : /* EMPTY */
                        | methodDecls methodDecl
                        ;

methodDecl              : '.emitbyte' int32
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
                        | '.param' 'type' '[' int32 ']'
                        | '.param' 'type' dottedName
                        | '.param' 'constraint' '[' int32 ']' ',' typeSpec
                        | '.param' 'constraint' dottedName ',' typeSpec

                        | '.param' '[' int32 ']' initOpt
                        ;

scopeBlock              : '{' methodDecls '}'
                        ;


/* Structured exception handling directives  */
sehBlock                : tryBlock sehClauses
                        ;

sehClauses              : sehClause sehClauses
                        | sehClause
                        ;

tryBlock                : tryHead scopeBlock
                        | tryHead id 'to' id
                        | tryHead int32 'to' int32
                        ;

tryHead                 : '.try'
                        ;


sehClause               : catchClause handlerBlock
                        | filterClause handlerBlock
                        | finallyClause handlerBlock
                        | faultClause handlerBlock
                        ;


filterClause            : filterHead scopeBlock
                        | filterHead id
                        | filterHead int32
                        ;

filterHead              : 'filter'
                        ;

catchClause             : 'catch' typeSpec
                        ;

finallyClause           : 'finally'
                        ;

faultClause             : 'fault'
                        ;

handlerBlock            : scopeBlock
                        | 'handler' id 'to' id
                        | 'handler' int32 'to' int32
                        ;

/*  Data declaration  */
dataDecl                : ddHead ddBody
                        ;

ddHead                  : '.data' tls id '='
                        | '.data' tls
                        ;

tls                     : /* EMPTY */
                        | 'tls'
                        | 'cil'
                        ;

ddBody                  : '{' ddItemList '}'
                        | ddItem
                        ;

ddItemList              : ddItem ',' ddItemList
                        | ddItem
                        ;

ddItemCount             : /* EMPTY */
                        | '[' int32 ']'
                        ;

ddItem                  : 'char' '*' '(' compQstring ')'
                        | '&' '(' id ')'
                        | 'bytearray' '(' bytes ')'
                        | 'float32' '(' float64 ')' ddItemCount
                        | 'float64' '(' float64 ')' ddItemCount
                        | 'int64' '(' int64 ')' ddItemCount
                        | 'int32' '(' int32 ')' ddItemCount
                        | 'int16' '(' int32 ')' ddItemCount
                        | 'int8' '(' int32 ')' ddItemCount
                        | 'float32' ddItemCount
                        | 'float64' ddItemCount
                        | 'int64' ddItemCount
                        | 'int32' ddItemCount
                        | 'int16' ddItemCount
                        | 'int8' ddItemCount
                        ;

/*  Default values declaration for fields, parameters and verbal form of CA blob description  */
fieldSerInit:
	'float32' '(' float64 ')'
	| 'float64' '(' float64 ')'
	| 'float32' '(' int32 ')'
	| 'float64' '(' int64 ')'
	| 'int64' '(' int64 ')'
	| 'int32' '(' int32 ')'
	| 'int16' '(' int32 ')'
	| 'int8' '(' int32 ')'
	| 'unsigned' 'int64' '(' int64 ')'
	| 'unsigned' 'int32' '(' int32 ')'
	| 'unsigned' 'int16' '(' int32 ')'
	| 'unsigned' 'int8' '(' int32 ')'
	| 'uint64' '(' int64 ')'
	| 'uint32' '(' int32 ')'
	| 'uint16' '(' int32 ')'
	| 'uint8' '(' int32 ')'
	| 'char' '(' int32 ')'
	| 'bool' '(' truefalse ')'
	| 'bytearray' '(' bytes ')';

bytes: hexbytes*;

hexbytes: HEXBYTE+;
/*  Field/parameter initialization  */
fieldInit: fieldSerInit | compQstring | 'nullref';

/*  Values for verbal form of CA blob description  */
serInit:
	fieldSerInit
	| 'string' '(' 'nullref' ')'
	| 'string' '(' SQSTRING ')'
	| 'type' '(' 'class' SQSTRING ')'
	| 'type' '(' className ')'
	| 'type' '(' 'nullref' ')'
	| 'object' '(' serInit ')'
	| 'float32' '[' int32 ']' '(' f32seq ')'
	| 'float64' '[' int32 ']' '(' f64seq ')'
	| 'int64' '[' int32 ']' '(' i64seq ')'
	| 'int32' '[' int32 ']' '(' i32seq ')'
	| 'int16' '[' int32 ']' '(' i16seq ')'
	| 'int8' '[' int32 ']' '(' i8seq ')'
	| 'uint64' '[' int32 ']' '(' i64seq ')'
	| 'uint32' '[' int32 ']' '(' i32seq ')'
	| 'uint16' '[' int32 ']' '(' i16seq ')'
	| 'uint8' '[' int32 ']' '(' i8seq ')'
	| 'unsigned' 'int64' '[' int32 ']' '(' i64seq ')'
	| 'unsigned' 'int32' '[' int32 ']' '(' i32seq ')'
	| 'unsigned' 'int16' '[' int32 ']' '(' i16seq ')'
	| 'unsigned' 'int8' '[' int32 ']' '(' i8seq ')'
	| 'char' '[' int32 ']' '(' i16seq ')'
	| 'bool' '[' int32 ']' '(' boolSeq ')'
	| 'string' '[' int32 ']' '(' sqstringSeq ')'
	| 'type' '[' int32 ']' '(' classSeq ')'
	| 'object' '[' int32 ']' '(' objSeq ')';

f32seq: (float64 | int32)*;

f64seq: (float64 | int64)*;

i64seq: int64*;

i32seq: int32*;

i16seq: int32*;

i8seq: int32*;

boolSeq: truefalse*;

sqstringSeq: ('nullref' | SQSTRING)*;

classSeq: classSeqElement*;

classSeqElement: 'nullref' | 'class' SQSTRING | className;

objSeq: serInit*;

customAttrDecl: customDescr | customDescrWithOwner | dottedName /* typedef */;

/* Assembly References */
asmOrRefDecl:
	'.publicKey' '=' '(' bytes ')'
	| '.ver' intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard
	| '.locale' compQstring
	| '.locale' '=' '(' bytes ')'
	| customAttrDecl
	| compControl;

assemblyRefHead         : '.assembly' 'extern' asmAttr dottedName
                        | '.assembly' 'extern' asmAttr dottedName 'as' dottedName
                        ;

assemblyRefDecls        : /* EMPTY */
                        | assemblyRefDecls assemblyRefDecl
                        ;

assemblyRefDecl         : '.hash' '=' '('  bytes ')'
                        | asmOrRefDecl
                        | '.publickeytoken' '=' '(' bytes ')'
                        | 'auto'
                        ;

exptypeHead             : '.class' 'extern' exptAttr dottedName
                        ;

exportHead              : '.export' exptAttr* dottedName
                        ;

exptAttr                : 'private'
                        | 'public'
                        | 'forwarder'
                        | 'nested' 'public'
                        | 'nested' 'private'
                        | 'nested' 'family'
                        | 'nested' 'assembly'
                        | 'nested' 'famandassem'
                        | 'nested' 'famorassem'
                        ;

exptypeDecls            : exptypeDecl*
                        ;

exptypeDecl             : '.file' dottedName
                        | '.class' 'extern' slashedName
                        | '.assembly' 'extern' dottedName
                        | 'mdtoken' '(' int32 ')'
                        | '.class'  int32
                        | customAttrDecl
                        | compControl
                        ;

manifestResHead         : '.mresource' manresAttr* dottedName
                        | '.mresource' manresAttr* dottedName 'as' dottedName
                        ;

manresAttr              : 'public'
                        | 'private'
                        ;

manifestResDecls        : /* EMPTY */
                        | manifestResDecls manifestResDecl
                        ;

manifestResDecl         : '.file' dottedName 'at' int32
                        | '.assembly' 'extern' dottedName
                        | customAttrDecl
                        | compControl
                        ;

