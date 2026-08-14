/*
Licensed to the .NET Foundation under one or more agreements.
The .NET Foundation licenses this file to you under the MIT license.
*/

grammar CIL;

tokens { IncludedFileEof, SyntheticIncludedFileEof }

INT32: '-'? ('0x' [0-9A-Fa-f]+ | [0-9]+);
INT64: '-'? ('0x' [0-9A-Fa-f]+ | [0-9]+);
FLOAT64: '-'? ([0-9]+ ('.' [0-9]+ ([eE] [+\-]? [0-9]+)? | [eE] [+\-]? [0-9]+) | '.' [0-9]+ ([eE] [+\-]? [0-9]+)?);
// Blob hex bytes are parsed via the hexbyte parser rule; the HEXBYTE lexer token is defined later in this grammar.
DCOLON: '::';
ELLIPSIS: '...';
NULL: 'null';
NULLREF: 'nullref';
HASH: '.hash';
CHAR: 'char' | 'wchar';
STRING: 'string';
BOOL: 'bool';
INT8: 'int8';
INT16: 'int16';
INT32_: 'int32';
INT64_: 'int64';
FLOAT32: 'float32';
FLOAT64_: 'float64';
fragment UNSIGNED: 'unsigned';
UINT8: 'uint8';
UINT16: 'uint16';
UINT32: 'uint32';
UINT64: 'uint64';
INT: 'int';
UINT: 'uint';
TYPE: 'type';
OBJECT: 'object';
MODULE: '.module';
VALUE: 'value';
VALUETYPE: 'valuetype';
VOID: 'void';
ENUM: 'enum';
CUSTOM: 'custom';
FIXED: 'fixed';
SYSSTRING: 'sysstring';
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
// NESTEDSTRUCT, VARIANTBOOL, ANSIBSTR are now parser rules to handle whitespace
BYVALSTR: 'byvalstr';
ANSI: 'ansi';
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
TYPEDREF: 'typedref' | 'refany';
// NATIVE_INT and NATIVE_UINT are now parser rules (nativeInt, nativeUint)
// to handle whitespace between 'native' and 'int'/'uint'.
PARAM: '.param';
CONSTRAINT: 'constraint';

THIS: '.this';
BASE: '.base';
NESTER: '.nester';
REF: '&';
ARRAY_TYPE_NO_BOUNDS: '[' ']';
PTR: '*';

fragment ESC_SEQ: '\\' (["'\\/?abfnrtv0] | [0-7] [0-7]? [0-7]? | '\r'? '\n');
QSTRING: '"' (~["\\\r\n] | ESC_SEQ)* '"';
SQSTRING: '\'' (~['\\\r\n] | ESC_SEQ)* '\'';
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

// Instruction tokens MUST be defined before DOTTEDNAME and ID to ensure they take precedence
// For example, "ldc.r8" must be recognized as INSTR_R token, not as DOTTEDNAME
INSTR_NONE:
	'nop'
	| 'unused'
	| 'break'
	| 'ldarg.0'
	| 'ldarg.1'
	| 'ldarg.2'
	| 'ldarg.3'
	| 'ldloc.0'
	| 'ldloc.1'
	| 'ldloc.2'
	| 'ldloc.3'
	| 'stloc.0'
	| 'stloc.1'
	| 'stloc.2'
	| 'stloc.3'
	| 'ldnull'
	| 'ldc.i4.m1'
	| 'ldc.i4.M1'
	| 'ldc.i4.0'
	| 'ldc.i4.1'
	| 'ldc.i4.2'
	| 'ldc.i4.3'
	| 'ldc.i4.4'
	| 'ldc.i4.5'
	| 'ldc.i4.6'
	| 'ldc.i4.7'
	| 'ldc.i4.8'
	| 'dup'
	| 'pop'
	| 'ret'
	| 'ldind.i1'
	| 'ldind.u1'
	| 'ldind.i2'
	| 'ldind.u2'
	| 'ldind.i4'
	| 'ldind.u4'
	| 'ldind.i8'
	| 'ldind.u8'
	| 'ldind.i'
	| 'ldind.r4'
	| 'ldind.r8'
	| 'ldind.ref'
	| 'stind.ref'
	| 'stind.i1'
	| 'stind.i2'
	| 'stind.i4'
	| 'stind.i8'
	| 'stind.r4'
	| 'stind.r8'
	| 'add'
	| 'sub'
	| 'mul'
	| 'div'
	| 'div.un'
	| 'rem'
	| 'rem.un'
	| 'and'
	| 'or'
	| 'xor'
	| 'shl'
	| 'shr'
	| 'shr.un'
	| 'neg'
	| 'not'
	| 'conv.i1'
	| 'conv.i2'
	| 'conv.i4'
	| 'conv.i8'
	| 'conv.r4'
	| 'conv.r8'
	| 'conv.u4'
	| 'conv.u8'
	| 'conv.r.un'
	| 'throw'
	| 'conv.ovf.i1.un'
	| 'conv.ovf.i2.un'
	| 'conv.ovf.i4.un'
	| 'conv.ovf.i8.un'
	| 'conv.ovf.u1.un'
	| 'conv.ovf.u2.un'
	| 'conv.ovf.u4.un'
	| 'conv.ovf.u8.un'
	| 'conv.ovf.i.un'
	| 'conv.ovf.u.un'
	| 'ldlen'
	| 'ldelem.i1'
	| 'ldelem.u1'
	| 'ldelem.i2'
	| 'ldelem.u2'
	| 'ldelem.i4'
	| 'ldelem.u4'
	| 'ldelem.i8'
	| 'ldelem.u8'
	| 'ldelem.i'
	| 'ldelem.r4'
	| 'ldelem.r8'
	| 'ldelem.ref'
	| 'stelem.i'
	| 'stelem.i1'
	| 'stelem.i2'
	| 'stelem.i4'
	| 'stelem.i8'
	| 'stelem.r4'
	| 'stelem.r8'
	| 'stelem.ref'
	| 'conv.ovf.i1'
	| 'conv.ovf.u1'
	| 'conv.ovf.i2'
	| 'conv.ovf.u2'
	| 'conv.ovf.i4'
	| 'conv.ovf.u4'
	| 'conv.ovf.i8'
	| 'conv.ovf.u8'
	| 'ckfinite'
	| 'conv.u2'
	| 'conv.u1'
	| 'conv.i'
	| 'conv.ovf.i'
	| 'conv.ovf.u'
	| 'add.ovf'
	| 'add.ovf.un'
	| 'mul.ovf'
	| 'mul.ovf.un'
	| 'sub.ovf'
	| 'sub.ovf.un'
	| 'endfinally'
	| 'endfault'
	| 'stind.i'
	| 'conv.u'
	| 'prefix7'
	| 'prefix6'
	| 'prefix5'
	| 'prefix4'
	| 'prefix3'
	| 'prefix2'
	| 'prefix1'
	| 'prefixref'
	| 'arglist'
	| 'ceq'
	| 'cgt'
	| 'cgt.un'
	| 'clt'
	| 'clt.un'
	| 'localloc'
	| 'endfilter'
	| 'volatile.'
	| 'tail.'
	| 'cpblk'
	| 'initblk'
	| 'rethrow'
	| 'refanytype'
	| 'readonly.'
	| 'illegal'
	| 'endmac';

INSTR_VAR:
	'ldarg.s'
	| 'ldarga.s'
	| 'starg.s'
	| 'ldloc.s'
	| 'ldloca.s'
	| 'stloc.s'
	| 'ldarg'
	| 'ldarga'
	| 'starg'
	| 'ldloc'
	| 'ldloca'
	| 'stloc';

INSTR_I:
	'ldc.i4.s'
	| 'ldc.i4'
	| 'unaligned.'
	| 'no.';

INSTR_I8: 'ldc.i8';

INSTR_R:
	'ldc.r4'
	| 'ldc.r8';

INSTR_METHOD:
	'jmp'
	| 'call'
	| 'callvirt'
	| 'newobj'
	| 'ldftn'
	| 'ldvirtftn';

INSTR_SIG: 'calli';

INSTR_BRTARGET:
	'br.s'
	| 'brfalse.s'
	| 'brtrue.s'
	| 'beq.s'
	| 'bge.s'
	| 'bgt.s'
	| 'ble.s'
	| 'blt.s'
	| 'bne.un.s'
	| 'bge.un.s'
	| 'bgt.un.s'
	| 'ble.un.s'
	| 'blt.un.s'
	| 'br'
	| 'brfalse'
	| 'brtrue'
	| 'beq'
	| 'bge'
	| 'bgt'
	| 'ble'
	| 'blt'
	| 'bne.un'
	| 'bge.un'
	| 'bgt.un'
	| 'ble.un'
	| 'blt.un'
	| 'leave'
	| 'leave.s';

INSTR_SWITCH: 'switch';

INSTR_TYPE:
	'cpobj'
	| 'ldobj'
	| 'castclass'
	| 'isinst'
	| 'unbox'
	| 'stobj'
	| 'box'
	| 'newarr'
	| 'ldelema'
	| 'ldelem'
	| 'stelem'
	| 'unbox.any'
	| 'refanyval'
	| 'mkrefany'
	| 'initobj'
	| 'constrained.'
	| 'sizeof';

INSTR_STRING: 'ldstr';

INSTR_FIELD:
	'ldfld'
	| 'ldflda'
	| 'stfld'
	| 'ldsfld'
	| 'ldsflda'
	| 'stsfld';

INSTR_TOK: 'ldtoken';

// ID needs to be last to ensure it doesn't take priority over other token types
fragment IDSTART: [A-Za-z_#$@?];
fragment IDCONT: [A-Za-z0-9_#?$@`];
DOTTEDNAME: (ID DOT)+ ID;
ID: IDSTART IDCONT*;

// HEXBYTE: matches exactly two hex digits. Defined AFTER INT32 and ID so that:
// - Pure digit pairs (11, 00) match INT32 first (same length, INT32 defined earlier)
// - Letter-starting pairs (B0, FF) match ID first (same length, ID defined earlier)
// - Digit-letter pairs (3F, 0A) match HEXBYTE (2 chars beats INT32's 1-char match)
HEXBYTE: [0-9A-Fa-f][0-9A-Fa-f];

id:
	ID
	| 'native'
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
	| 'async'
	| 'extended'
	| VALUE
	| INSTANCE
	| SQSTRING;
dottedName returns [string Value]
@init {Actions.BeginDottedName(_localctx);}
:
	direct = DOTTEDNAME {Actions.AddDottedNameToken(_localctx, $direct);}
	| ((part = dottedNamePart {Actions.AddDottedNamePart(_localctx, $part.Value);} '.')*
		tail = dottedNamePart {Actions.AddDottedNamePart(_localctx, $tail.Value);})
	| quoted = SQSTRING {Actions.AddDottedNameToken(_localctx, $quoted);}
;
finally {_localctx.Value = Actions.EndDottedName(_localctx);}

dottedNamePart returns [string Value]
@after {_localctx.Value = Actions.ParseDottedNamePart(_localctx.Start);}
:
	ID
	| VALUE
	| INSTANCE
	| SQSTRING
	| DOTTEDNAME
	| 'volatile'
;
compQstring returns [string Value]
@init {BeginStreaming(); Actions.BeginComposedString(_localctx);}
:
	(head = QSTRING {Actions.AddComposedStringPart(_localctx, $head);} PLUS)*
	tail = QSTRING {Actions.AddComposedStringPart(_localctx, $tail);}
;
finally {_localctx.Value = Actions.EndComposedString(_localctx); EndParseTreeMode();}


WS: [ \t\r\n] -> skip;
SINGLE_LINE_COMMENT: '//' ~[\r\n]* -> skip;
COMMENT: '/*' .*? '*/' -> skip;

decls
@init {BeginStreaming();}
:
    decl*
;
finally {EndParseTreeMode();}

decl
@init {BeginStreaming();}
:
	classHead '{' classDecls '}'
	| nameSpaceHead '{' decls '}'
	| methodHead '{' methodDecls '}'
	| fieldDecl
	| {Actions.BeginTopLevelDirective();}
		data = dataDecl {Actions.ProcessTopLevelDataDeclaration($data.ctx);}
	| {Actions.BeginTopLevelDirective();}
		vtable = vtableDecl {Actions.ProcessTopLevelVTableDeclaration($vtable.ctx);}
	| {Actions.BeginTopLevelDirective();}
		vtfixup = vtfixupDecl {Actions.ProcessTopLevelVTableFixupDeclaration($vtfixup.ctx);}
	| {Actions.BeginTopLevelDirective();}
		source = extSourceSpec {Actions.ProcessTopLevelSourceDirective($source.ctx);}
	| {Actions.BeginTopLevelDirective();}
		file = fileDecl {Actions.ProcessTopLevelFileDeclaration($file.ctx);}
	| {Actions.BeginTopLevelDirective();}
		assembly = assemblyBlock {Actions.ProcessTopLevelAssembly($assembly.ctx);}
	| {Actions.BeginTopLevelDirective();}
		assemblyReference = assemblyRefBlock
		{Actions.ProcessTopLevelAssemblyReference($assemblyReference.ctx);}
	| {Actions.BeginTopLevelDirective();}
		exportedType = exptypeBlock {Actions.ProcessTopLevelExportedType($exportedType.ctx);}
	| {Actions.BeginTopLevelDirective();}
		resource = manifestResBlock {Actions.ProcessTopLevelManifestResource($resource.ctx);}
	| {Actions.BeginTopLevelDirective();}
		module = moduleHead
		{Actions.ProcessTopLevelModule($module.Value, $module.HasName, $module.IsExternal);}
	| {Actions.BeginTopLevelDirective();}
		security = secDecl {Actions.ProcessTopLevelSecurityDeclaration($security.ctx);}
	| attribute = customAttrDecl {Actions.ProcessTopLevelCustomAttribute($attribute.ctx);}
	| {Actions.BeginTopLevelDirective();} subsystem
	| {Actions.BeginTopLevelDirective();} corflags
	| {Actions.BeginTopLevelDirective();} alignment
	| {Actions.BeginTopLevelDirective();} imagebase
	| {Actions.BeginTopLevelDirective();} stackreserve
	| {Actions.BeginTopLevelDirective();}
		language = languageDecl {Actions.ProcessTopLevelLanguageDirective($language.ctx);}
	| {Actions.BeginTopLevelDirective();}
		typedef = typedefDecl {Actions.ProcessTopLevelTypedef($typedef.ctx);}
	| {Actions.BeginTopLevelDirective();} compControl
	| typelist
	| {Actions.BeginTopLevelDirective();} mscorlib;
finally {EndParseTreeMode(); Actions.EndDeclaration(_localctx);}

subsystem:
	'.subsystem' value = int32 {Actions.ProcessTopLevelSubsystem($value.start);};

corflags:
	'.corflags' value = int32 {Actions.ProcessTopLevelCorFlags($value.start);};

alignment:
	'.file' 'alignment' value = int32 {Actions.ProcessTopLevelAlignment($value.start);};

imagebase:
	'.imagebase' value = int64 {Actions.ProcessTopLevelImageBase($value.start);};

stackreserve:
	'.stackreserve' value = int64 {Actions.ProcessTopLevelStackReserve($value.start);};

assemblyBlock returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	'.assembly' asmAttr dottedName '{' assemblyDecls '}';
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

mscorlib: '.mscorlib';

languageDecl returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	'.language' languageString
	| '.language' languageString ',' languageString
	| '.language' languageString ',' languageString ',' languageString;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

languageString: SQSTRING | QSTRING;

typelist
@init {Actions.BeginTopLevelTypeList();}
:
	'.typelist' '{'
	(name = className {Actions.ProcessTopLevelTypeListEntry($name.Value);})*
	'}'
;

int32: INT32;
int64: INT64 | INT32;

float64 returns [double Value]:
	decimal = FLOAT64 {_localctx.Value = Actions.ParseFloatingLiteral($decimal);}
	| trailing = int32 '.' {_localctx.Value = Actions.ParseFloatingInteger($trailing.start);}	/* trailing-dot integer as float (e.g., ldc.r8 1.) */
	| integer = int32 {_localctx.Value = Actions.ParseFloatingInteger($integer.start);}
	| FLOAT32 '(' singleBits = int32 ')' {_localctx.Value = Actions.ParseFloat32Bits($singleBits.start);}
	| FLOAT64_ '(' doubleBits = int64 ')' {_localctx.Value = Actions.ParseFloat64Bits($doubleBits.start);};

intOrWildcard: int32 | PTR;

/* This is handled in the PreprocessedTokenSource lexer. We have this in the grammar just for completeness */
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
typedefDecl returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	'.typedef' type 'as' dottedName
	| '.typedef' className 'as' dottedName
	| '.typedef' memberRef 'as' dottedName
	| '.typedef' customDescr 'as' dottedName
	| '.typedef' customDescrWithOwner 'as' dottedName;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

/* Custom attribute declarations  */
customDescr returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	'.custom' constructor = customType
		{_localctx.Value = Actions.CreateDefaultCustomAttribute($constructor.Value);}
	| '.custom' constructor = customType '=' stringValue = compQstring
		{_localctx.Value = Actions.CreateStringCustomAttribute($constructor.Value, $stringValue.Value);}
	| '.custom' constructor = customType '=' '{' structuredValue = customBlobDescr '}'
		{_localctx.Value = Actions.CreateStructuredCustomAttribute($constructor.Value, $structuredValue.Value);}
	| '.custom' constructor = customType '=' '(' rawValue = bytes ')'
		{_localctx.Value = Actions.CreateRawCustomAttribute($constructor.Value, $rawValue.Value);};
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

customDescrWithOwner returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	'.custom' '(' owner = ownerType ')' constructor = customType
		{_localctx.Value = Actions.CreateDefaultOwnedCustomAttribute($owner.Value, $constructor.Value);}
	| '.custom' '(' owner = ownerType ')' constructor = customType '=' stringValue = compQstring
		{_localctx.Value = Actions.CreateStringOwnedCustomAttribute($owner.Value, $constructor.Value, $stringValue.Value);}
	| '.custom' '(' owner = ownerType ')' constructor = customType '=' '{' structuredValue = customBlobDescr '}'
		{_localctx.Value = Actions.CreateStructuredOwnedCustomAttribute($owner.Value, $constructor.Value, $structuredValue.Value);}
	| '.custom' '(' owner = ownerType ')' constructor = customType '=' '(' rawValue = bytes ')'
		{_localctx.Value = Actions.CreateRawOwnedCustomAttribute($owner.Value, $constructor.Value, $rawValue.Value);};
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

customType returns [object Value]:
	constructor = methodRef {_localctx.Value = Actions.CreateCustomAttributeType($constructor.Value);};

ownerType
returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	typeValue = typeSpec {_localctx.Value = Actions.CreateTypeOwner($typeValue.Value);}
	| member = memberRef {_localctx.Value = Actions.CreateMemberOwner($member.Value);}
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

/*  Verbal description of custom attribute initialization blob  */
customBlobDescr returns [object Value]:
	arguments = customBlobArgs namedArguments = customBlobNVPairs
		{_localctx.Value = Actions.CreateCustomAttributeBlob($arguments.Value, $namedArguments.Value);};

customBlobArgs returns [object Value]
@init {Actions.BeginCustomBlobArguments(_localctx);}
:
	(argument = serInit {Actions.AddCustomBlobArgument(_localctx, $argument.Value);} | compControl)*
;
finally {_localctx.Value = Actions.EndCustomBlobArguments(_localctx);}

customBlobNVPairs returns [object Value]
@init {Actions.BeginCustomBlobNamedArguments(_localctx);}
:
	(
		kind = fieldOrProp argumentType = serializType name = dottedName '=' value = serInit
			{Actions.AddCustomBlobNamedArgument(
				_localctx,
				$kind.Value,
				$argumentType.Value,
				$name.Value,
				$value.Value);}
		| compControl
	)*
;
finally {_localctx.Value = Actions.EndCustomBlobNamedArguments(_localctx);}

fieldOrProp returns [byte Value]:
	kind = ('field' | 'property') {_localctx.Value = Actions.GetCustomAttributeNamedArgumentKind($kind);};

serializType returns [object Value]:
	element = serializTypeElement array = ARRAY_TYPE_NO_BOUNDS?
		{_localctx.Value = Actions.CreateSerializationType($element.Value, $array);};

serializTypeElement returns [object Value]:
	primitive = simpleType {_localctx.Value = Actions.CreatePrimitiveSerializationType($primitive.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateSerializationTypeTypedef(_localctx, $alias.Value);} /* typedef */
	| simpleTypeToken = TYPE {_localctx.Value = Actions.CreateSimpleSerializationType($simpleTypeToken);}
	| simpleTypeToken = OBJECT {_localctx.Value = Actions.CreateSimpleSerializationType($simpleTypeToken);}
	| ENUM 'class' quotedName = SQSTRING {_localctx.Value = Actions.CreateEnumSerializationType($quotedName);}
	| ENUM classNameValue = className {_localctx.Value = Actions.CreateEnumSerializationType($classNameValue.Value);};

/*  Module declaration */
moduleHead returns [string Value, bool HasName, bool IsExternal]:
	MODULE 'extern' name = dottedName
		{Actions.SetModuleHeader(_localctx, $name.Value, true);}
	| MODULE name = dottedName
		{Actions.SetModuleHeader(_localctx, $name.Value, false);}
	| MODULE
		{Actions.SetEmptyModuleHeader(_localctx);};

/*  VTable Fixup table declaration  */
vtfixupDecl returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	'.vtfixup' '[' int32 ']' vtfixupAttr 'at' id
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

vtfixupAttr:
	/* EMPTY */
	| vtfixupAttr INT32_
	| vtfixupAttr INT64_
	| vtfixupAttr 'fromunmanaged'
	| vtfixupAttr 'callmostderived'
	| vtfixupAttr 'retainappdomain';

vtableDecl returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	'.vtable' '=' '(' bytes ')' /* deprecated */
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

/*  Namespace and class declaration  */
nameSpaceHead returns [string Value]
@init {BeginStreaming(); Actions.BeginNamespaceHeader(_localctx);}
@after {Actions.BeginNamespace(_localctx, _localctx.Value);}
:
	'.namespace' name = dottedName {_localctx.Value = $name.Value;}
;
finally {Actions.EndNamespaceHeader(_localctx); EndParseTreeMode();}

classHead returns [object Value]
@init {BeginStreaming(); Actions.BeginClassHeader(_localctx);}
@after {Actions.BeginType(_localctx, _localctx.Value);}
:
	'.class'
	(attribute = classAttr {Actions.AddClassHeaderAttribute(_localctx, $attribute.Value);})*
	name = dottedName genericParameters = typarsClause baseType = extendsClause interfaces = implClause
		{_localctx.Value = Actions.CreateClassHeader(
			_localctx,
			$name.stop,
			$name.Value,
			$genericParameters.Value,
			$baseType.Value,
			$interfaces.Value);}
;
finally {Actions.EndClassHeader(_localctx); EndParseTreeMode();}


classAttr returns [object Value]:
	attribute = 'public' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'private' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = VALUE {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = ENUM {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = INTERFACE {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'sealed' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'abstract' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'auto' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'sequential' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = EXPLICIT {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'extended' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = ANSI {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'unicode' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'autochar' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'import' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'serializable' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'windowsruntime' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| 'nested' visibility = 'public' {_localctx.Value = Actions.CreateNestedClassAttribute($visibility);}
	| 'nested' visibility = 'private' {_localctx.Value = Actions.CreateNestedClassAttribute($visibility);}
	| 'nested' visibility = 'family' {_localctx.Value = Actions.CreateNestedClassAttribute($visibility);}
	| 'nested' visibility = 'assembly' {_localctx.Value = Actions.CreateNestedClassAttribute($visibility);}
	| 'nested' visibility = 'famandassem' {_localctx.Value = Actions.CreateNestedClassAttribute($visibility);}
	| 'nested' visibility = 'famorassem' {_localctx.Value = Actions.CreateNestedClassAttribute($visibility);}
	| attribute = 'beforefieldinit' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'specialname' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| attribute = 'rtspecialname' {_localctx.Value = Actions.CreateClassAttribute($attribute);}
	| 'flags' '(' flags = int32 ')' {_localctx.Value = Actions.CreateRawClassAttribute($flags.start);};

extendsClause returns [object Value]
@init {_localctx.Value = Actions.CreateEmptyClassBase();}
:
	/* EMPTY */
	| 'extends' baseType = typeSpec {_localctx.Value = Actions.CreateClassBase($baseType.Value);}
;

implClause returns [object Value]
@init {_localctx.Value = Actions.CreateEmptyInterfaceList();}
:
	/* EMPTY */
	| 'implements' interfaces = implList {_localctx.Value = $interfaces.Value;}
;

classDecls
@init {BeginStreaming();}
:
    classDecl*
;
finally {EndParseTreeMode();}

implList returns [object Value]
@init {Actions.BeginInterfaceList(_localctx);}
:
	(interfaceType = typeSpec {Actions.AddInterfaceType(_localctx, $interfaceType.Value);} ',')*
	lastInterfaceType = typeSpec {Actions.AddInterfaceType(_localctx, $lastInterfaceType.Value);}
;
finally {_localctx.Value = Actions.EndInterfaceList(_localctx);}

/*  External source declarations  */
esHead: '.line' | '#line';

extSourceSpec returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
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
	| esHead int32 QSTRING
	| esHead int32 ':' int32 QSTRING
	| esHead int32 ':' int32 ',' int32 QSTRING
	| esHead int32 ',' int32 ':' int32 QSTRING
	| esHead int32 ',' int32 ':' int32 ',' int32 QSTRING;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

/*  Manifest declarations  */
fileDecl returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	'.file' fileAttr* dottedName fileEntry HASH '=' '(' bytes ')' fileEntry
	| '.file' fileAttr* dottedName fileEntry;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

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
instr:
	simpleInstr
	| op = INSTR_METHOD methodOperand = methodRef {Actions.EmitMethodReferenceInstruction($op, $methodOperand.ctx);}
	| op = INSTR_FIELD fieldOperand = fieldRef {Actions.EmitFieldReferenceInstruction($op, $fieldOperand.ctx);}
	| op = INSTR_FIELD metadataOperand = mdtoken {Actions.EmitMetadataTokenInstruction($op, $metadataOperand.ctx);}
	| op = INSTR_TYPE typeOperand = typeSpec {Actions.EmitTypeReferenceInstruction($op, $typeOperand.ctx);}
	| op = INSTR_SIG signatureOperand = calliSignature {Actions.EmitCalliInstruction($op, $signatureOperand.ctx);}
	| op = INSTR_TOK ownerOperand = ownerType {Actions.EmitOwnerTokenInstruction($op, $ownerOperand.ctx);}
;

simpleInstr
@after {Actions.CompleteSimpleInstruction(_localctx);}
:
	op = INSTR_NONE {Actions.EmitNoOperandInstruction($op);}
	| op = INSTR_VAR index = int32 {Actions.EmitVariableIndexInstruction($op, $index.start);}
	| op = INSTR_VAR name = id {Actions.EmitVariableNameInstruction($op, $name.start);}
	| op = INSTR_I value32 = int32 {Actions.EmitInt32Instruction($op, $value32.start);}
	| op = INSTR_I8 value64 = int64 {Actions.EmitInt64Instruction($op, $value64.start);}
	| op = INSTR_R value = float64 {Actions.EmitFloatingInstruction($op, $value.Value);}
	| op = INSTR_R integerValue = int64 {Actions.EmitFloatingInstruction($op, $integerValue.start);}
	| op = INSTR_R '(' rawFloat = bytes ')' {Actions.EmitRawFloatingInstruction($op, $rawFloat.Value, $rawFloat.start);}
	| op = INSTR_R 'bytearray' '(' rawFloat = bytes ')' {Actions.EmitRawFloatingInstruction($op, $rawFloat.Value, $rawFloat.start);}
	| op = INSTR_BRTARGET offset = int32 {Actions.EmitBranchOffsetInstruction($op, $offset.start);}
	| op = INSTR_BRTARGET label = id {Actions.EmitBranchLabelInstruction($op, $label.start);}
	| op = INSTR_STRING userString = compQstring {Actions.EmitStringInstruction($op, $userString.Value);}
	| op = INSTR_STRING ANSI '(' ansiString = compQstring ')' {Actions.EmitAnsiStringInstruction($op, $ansiString.Value);}
	| op = INSTR_STRING 'bytearray' '(' rawString = bytes ')' {Actions.EmitRawStringInstruction($op, $rawString.Value);}
	| op = INSTR_TOK rawToken = int32 {Actions.EmitRawTokenInstruction($op, $rawToken.start);}
	| op = INSTR_SWITCH {Actions.BeginSwitchInstruction(_localctx, $op);} ('(' labels ')' | '()')
;
finally {Actions.EndSwitchInstruction(_localctx);}

calliSignature
returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	convention = callConv returnType = type arguments = sigArgs
		{_localctx.Value = Actions.CreateCalliSignature($convention.Value, $returnType.Value, $arguments.Value);}
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

labels:
	/* empty */
	| ((headLabel = id {Actions.AddSwitchLabel($headLabel.start);} | headOffset = int32 {Actions.AddSwitchOffset($headOffset.start);}) ',')*
	  (tailLabel = id {Actions.AddSwitchLabel($tailLabel.start);} | tailOffset = int32 {Actions.AddSwitchOffset($tailOffset.start);});

typeArgs returns [object Value]
@init {Actions.BeginTypeArguments(_localctx);}
:
	'<' (argument = type {Actions.AddTypeArgument(_localctx, $argument.Value);} ',')*
		lastArgument = type {Actions.AddTypeArgument(_localctx, $lastArgument.Value);} '>'
;
finally {_localctx.Value = Actions.EndTypeArguments(_localctx);}

bounds returns [object Value]
@init {Actions.BeginBounds(_localctx);}
:
	'[' (item = bound {Actions.AddBound(_localctx, $item.ctx);} ',')*
		lastItem = bound {Actions.AddBound(_localctx, $lastItem.ctx);} ']'
;
finally {_localctx.Value = Actions.EndBounds(_localctx);}

sigArgs returns [object Value]
@init {Actions.BeginSignatureArguments(_localctx);}
:
	'(' (argument = sigArg {Actions.AddSignatureArgument(_localctx, $argument.Value);} ',')*
		lastArgument = sigArg {Actions.AddSignatureArgument(_localctx, $lastArgument.Value);} ')'
	| '()'
;
finally {_localctx.Value = Actions.EndSignatureArguments(_localctx);}

sigArg returns [object Value]:
	ELLIPSIS {_localctx.Value = Actions.CreateSentinelSignatureArgument();}
	| attributes = paramAttr argumentType = type marshalling = marshalClause name = id?
		{_localctx.Value = Actions.CreateSignatureArgument($attributes.Value, $argumentType.Value, $marshalling.Value, $name.ctx);};

/*  Class referencing  */

className returns [object Value]:
	'[' assemblyName = dottedName ']' typeName = slashedName
		{_localctx.Value = Actions.CreateAssemblyQualifiedClassName($assemblyName.Value, $typeName.Value);}
	| '[' scopeToken = mdtoken ']' typeName = slashedName
		{_localctx.Value = Actions.CreateTokenQualifiedClassName($scopeToken.Value, $typeName.Value);}
	| '[' PTR ']' typeName = slashedName
		{_localctx.Value = Actions.CreatePointerQualifiedClassName($typeName.Value);}
	| '[' MODULE moduleName = dottedName ']' typeName = slashedName
		{_localctx.Value = Actions.CreateModuleQualifiedClassName(_localctx.Start, $moduleName.Value, $typeName.Value);}
	| typeName = slashedName {_localctx.Value = Actions.CreateUnqualifiedClassName($typeName.Value);}
	| typeToken = mdtoken {_localctx.Value = Actions.CreateTokenClassName($typeToken.Value);}
	| THIS {_localctx.Value = Actions.CreateThisClassName(_localctx.Start);}
	| BASE {_localctx.Value = Actions.CreateBaseClassName(_localctx.Start);}
	| NESTER {_localctx.Value = Actions.CreateNesterClassName(_localctx.Start);};

slashedName returns [object Value]
@init {Actions.BeginSlashedName(_localctx);}
:
	(part = dottedName {Actions.AddSlashedNamePart(_localctx, $part.Value);} '/')*
	lastPart = dottedName {Actions.AddSlashedNamePart(_localctx, $lastPart.Value);}
;
finally {_localctx.Value = Actions.EndSlashedName(_localctx);}

assemblyDecls: assemblyDecl*;

assemblyDecl: (HASH 'algorithm' int32) | secDecl | asmOrRefDecl;

typeSpec
returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	classType = className {_localctx.Value = Actions.CreateClassTypeSpecification($classType.Value);}
	| '[' assemblyName = dottedName ']' {_localctx.Value = Actions.CreateAssemblyTypeSpecification($assemblyName.Value);}
	| '[' MODULE moduleName = dottedName ']' {_localctx.Value = Actions.CreateModuleTypeSpecification($moduleName.Value);}
	| signatureType = type {_localctx.Value = Actions.CreateSignatureTypeSpecification($signatureType.Value);}
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

/*  Native types for marshaling signatures  */
nativeType returns [object Value]
@init {Actions.BeginNativeType(_localctx);}
:
	/* EMPTY */
	| element = nativeTypeElement {Actions.SetNativeTypeElement(_localctx, $element.Value);}
		(info = nativeTypeArrayPointerInfo {Actions.AddNativeTypeArrayPointerInfo(_localctx, $info.Value);})*
;
finally {_localctx.Value = Actions.EndNativeType(_localctx);}

nativeTypeArrayPointerInfo returns [object Value]:
	PTR {_localctx.Value = Actions.CreatePointerNativeType();} # PointerNativeType
	| ARRAY_TYPE_NO_BOUNDS {_localctx.Value = Actions.CreatePointerArrayTypeNoSizeData();} # PointerArrayTypeNoSizeData
	| '[' size = int32 ']' {_localctx.Value = Actions.CreatePointerArrayTypeSize($size.start);} # PointerArrayTypeSize
	| '[' size = int32 PLUS parameterIndex = int32 ']'
		{_localctx.Value = Actions.CreatePointerArrayTypeSizeParamIndex($size.start, $parameterIndex.start);} # PointerArrayTypeSizeParamIndex
	| '[' PLUS parameterIndex = int32 ']'
		{_localctx.Value = Actions.CreatePointerArrayTypeParamIndex($parameterIndex.start);} # PointerArrayTypeParamIndex
    ;

nativeTypeElement returns [object Value]:
	/* EMPTY */ {_localctx.Value = Actions.CreateEmptyNativeType();}
	| marshalType = CUSTOM '(' guid = compQstring ',' nativeTypeName = compQstring ','
		marshallerType = compQstring ',' cookie = compQstring ')'
		{_localctx.Value = Actions.CreateDeprecatedCustomMarshallerNativeType(
			_localctx, $guid.Value, $nativeTypeName.Value, $marshallerType.Value, $cookie.Value);}
	| marshalType = CUSTOM '(' marshallerType = compQstring ',' cookie = compQstring ')'
		{_localctx.Value = Actions.CreateCustomMarshallerNativeType($marshallerType.Value, $cookie.Value);}
	| FIXED marshalType = SYSSTRING '[' size = int32 ']'
		{_localctx.Value = Actions.CreateFixedSysStringNativeType($size.start);}
	| FIXED marshalType = ARRAY '[' size = int32 ']' element = nativeType
		{_localctx.Value = Actions.CreateFixedArrayNativeType($size.start, $element.Value);}
	| marshalType = VARIANT {_localctx.Value = Actions.CreateDeprecatedNativeType(_localctx, $marshalType);}
	| marshalType = CURRENCY {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = SYSCHAR {_localctx.Value = Actions.CreateDeprecatedNativeType(_localctx, $marshalType);}
	| marshalType = VOID {_localctx.Value = Actions.CreateDeprecatedNativeType(_localctx, $marshalType);}
	| marshalType = BOOL {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = INT8 {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = INT16 {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = INT32_ {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = INT64_ {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = FLOAT32 {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = FLOAT64_ {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = ERROR {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = UINT8 {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = UINT16 {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = UINT32 {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = UINT64 {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = DECIMAL {_localctx.Value = Actions.CreateDeprecatedNativeType(_localctx, $marshalType);}
	| marshalType = DATE {_localctx.Value = Actions.CreateDeprecatedNativeType(_localctx, $marshalType);}
	| marshalType = BSTR {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = LPSTR {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = LPWSTR {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = LPTSTR {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = OBJECTREF {_localctx.Value = Actions.CreateDeprecatedNativeType(_localctx, $marshalType);}
	| marshalType = IUNKNOWN index = iidParamIndex {_localctx.Value = Actions.CreateIidNativeType($marshalType, $index.Value);}
	| marshalType = IDISPATCH index = iidParamIndex {_localctx.Value = Actions.CreateIidNativeType($marshalType, $index.Value);}
	| marshalType = STRUCT {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = INTERFACE index = iidParamIndex {_localctx.Value = Actions.CreateIidNativeType($marshalType, $index.Value);}
	| marshalType = SAFEARRAY variant = variantType
		{_localctx.Value = Actions.CreateSafeArrayNativeType($variant.Value, null);}
	| marshalType = SAFEARRAY variant = variantType ',' userDefinedType = compQstring
		{_localctx.Value = Actions.CreateSafeArrayNativeType($variant.Value, $userDefinedType.Value);}
	| marshalType = INT {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = UINT {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| 'unsigned' unsignedMarshalType = INT8 {_localctx.Value = Actions.CreateUnsignedNativeType($unsignedMarshalType);}
	| 'unsigned' unsignedMarshalType = INT16 {_localctx.Value = Actions.CreateUnsignedNativeType($unsignedMarshalType);}
	| 'unsigned' unsignedMarshalType = INT32_ {_localctx.Value = Actions.CreateUnsignedNativeType($unsignedMarshalType);}
	| 'unsigned' unsignedMarshalType = INT64_ {_localctx.Value = Actions.CreateUnsignedNativeType($unsignedMarshalType);}
	| 'nested' marshalType = STRUCT {_localctx.Value = Actions.CreateNestedStructNativeType(_localctx);}
	| marshalType = BYVALSTR {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| ANSI marshalType = BSTR {_localctx.Value = Actions.CreateAnsiBstrNativeType();}
	| marshalType = TBSTR {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| VARIANT marshalBool = BOOL {_localctx.Value = Actions.CreateVariantBoolNativeType();}
	| marshalType = METHOD {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| marshalType = LPSTRUCT {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| 'as' marshalType = ANY {_localctx.Value = Actions.CreateSimpleNativeType($marshalType);}
	| alias = dottedName {_localctx.Value = Actions.CreateNativeTypeTypedef(_localctx, $alias.Value);} /* typedef */;

iidParamIndex returns [object Value]:
	/* EMPTY */
	| '(' 'iidparam' '=' index = int32 ')' {_localctx.Value = Actions.GetIidParamIndex($index.start);};

variantType returns [object Value]
@init {Actions.BeginVariantType(_localctx);}
:
	/*EMPTY */
	| element = variantTypeElement {Actions.SetVariantTypeElement(_localctx, $element.Value);}
		(modifier = (ARRAY_TYPE_NO_BOUNDS | VECTOR | REF) {Actions.AddVariantTypeModifier(_localctx, $modifier);})*
;
finally {_localctx.Value = Actions.EndVariantType(_localctx);}

variantTypeElement returns [object Value]:
	value = NULL {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = VARIANT {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = CURRENCY {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = VOID {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = BOOL {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = INT8 {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = INT16 {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = INT32_ {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = INT64_ {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = FLOAT32 {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = FLOAT64_ {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = UINT8 {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = UINT16 {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = UINT32 {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = UINT64 {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = PTR {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = DECIMAL {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = DATE {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = BSTR {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = LPSTR {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = LPWSTR {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = IUNKNOWN {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = IDISPATCH {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = SAFEARRAY {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = INT {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = UINT {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = ERROR {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = HRESULT {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = CARRAY {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = USERDEFINED {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = RECORD {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = FILETIME {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = BLOB {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = STREAM {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = STORAGE {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = STREAMED_OBJECT {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = STORED_OBJECT {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = BLOB_OBJECT {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = CF {_localctx.Value = Actions.GetVariantTypeElement($value);}
	| value = CLSID {_localctx.Value = Actions.GetVariantTypeElement($value);};

/*  Managed types for signatures  */
type returns [object Value]
@init {Actions.BeginTypeSignature(_localctx);}
:
	element = elementType {Actions.SetTypeSignatureElement(_localctx, $element.Value);}
	(modifier = typeModifiers {Actions.AddTypeSignatureModifier(_localctx, $modifier.Value);})*
;
finally {_localctx.Value = Actions.EndTypeSignature(_localctx);}

typeModifiers returns [object Value]:
	ARRAY_TYPE_NO_BOUNDS {_localctx.Value = Actions.CreateSzArrayTypeModifier();} # SZArrayModifier
	| '[' ']' {_localctx.Value = Actions.CreateSzArrayTypeModifier();} # SZArrayModifier
	| arrayBounds = bounds {_localctx.Value = Actions.CreateArrayTypeModifier($arrayBounds.Value);} # ArrayModifier
	| REF {_localctx.Value = Actions.CreateByReferenceTypeModifier();} # ByRefModifier
	| PTR {_localctx.Value = Actions.CreatePointerTypeModifier();} # PtrModifier
	| 'pinned' {_localctx.Value = Actions.CreatePinnedTypeModifier();} # PinnedModifier
	| 'modreq' '(' modifierType = typeSpec ')' {_localctx.Value = Actions.CreateCustomTypeModifier($modifierType.Value, true);} # RequiredModifier
	| 'modopt' '(' modifierType = typeSpec ')' {_localctx.Value = Actions.CreateCustomTypeModifier($modifierType.Value, false);} # OptionalModifier
	| arguments = typeArgs {_localctx.Value = Actions.CreateGenericArgumentsModifier($arguments.Value);} # GenericArgumentsModifier;

elementType returns [object Value]:
	'class' classType = className {_localctx.Value = Actions.CreateClassElementType($classType.Value, false);}
	| OBJECT {_localctx.Value = Actions.CreateObjectElementType();}
	| VALUE 'class' valueClassType = className {_localctx.Value = Actions.CreateClassElementType($valueClassType.Value, true);}
	| VALUETYPE valueType = className {_localctx.Value = Actions.CreateClassElementType($valueType.Value, true);}
	| 'method' convention = callConv returnType = type PTR arguments = sigArgs
		{_localctx.Value = Actions.CreateFunctionPointerElementType($convention.Value, $returnType.Value, $arguments.Value);}
	| METHOD_TYPE_PARAMETER parameterIndex = int32 {_localctx.Value = Actions.CreateIndexedGenericParameterElementType(true, $parameterIndex.start);}
	| TYPE_PARAMETER parameterIndex = int32 {_localctx.Value = Actions.CreateIndexedGenericParameterElementType(false, $parameterIndex.start);}
	| METHOD_TYPE_PARAMETER parameterName = dottedName {_localctx.Value = Actions.CreateNamedGenericParameterElementType(_localctx.Start, true, $parameterName.Value);}
	| TYPE_PARAMETER parameterName = dottedName {_localctx.Value = Actions.CreateNamedGenericParameterElementType(_localctx.Start, false, $parameterName.Value);}
	| TYPEDREF {_localctx.Value = Actions.CreateTypedReferenceElementType();}
	| VOID {_localctx.Value = Actions.CreateVoidElementType();}
	| signedNative = nativeInt {_localctx.Value = Actions.CreatePrimitiveElementType($signedNative.Value);}
	| unsignedNative = nativeUint {_localctx.Value = Actions.CreatePrimitiveElementType($unsignedNative.Value);}
	| primitive = simpleType {_localctx.Value = Actions.CreatePrimitiveElementType($primitive.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateTypedefElementType(_localctx.Start, $alias.Value);} /* typedef */
	| ELLIPSIS sentinelType = type {_localctx.Value = Actions.CreateSentinelElementType($sentinelType.Value);};

simpleType returns [byte Value]:
	value = CHAR {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = STRING {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = BOOL {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = INT8 {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = INT16 {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = INT32_ {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = INT64_ {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = FLOAT32 {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = FLOAT64_ {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = UINT8 {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = UINT16 {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = UINT32 {_localctx.Value = Actions.GetSimpleType($value, false);}
	| value = UINT64 {_localctx.Value = Actions.GetSimpleType($value, false);}
	| 'unsigned' value = INT8 {_localctx.Value = Actions.GetSimpleType($value, true);}
	| 'unsigned' value = INT16 {_localctx.Value = Actions.GetSimpleType($value, true);}
	| 'unsigned' value = INT32_ {_localctx.Value = Actions.GetSimpleType($value, true);}
	| 'unsigned' value = INT64_ {_localctx.Value = Actions.GetSimpleType($value, true);};

bound returns [int Lower, int Upper, bool HasLower, bool HasUpper]
@init {Actions.InitializeBound(_localctx);}
:
	/* EMPTY */
	| ELLIPSIS
	| size = int32 {Actions.SetBoundSize(_localctx, $size.start);}
	| lower = int32 ELLIPSIS upper = int32 {Actions.SetBoundRange(_localctx, $lower.start, $upper.start);}
	| lower = int32 ELLIPSIS {Actions.SetBoundLower(_localctx, $lower.start);}
;

/* Parser rules for multi-word type tokens that need whitespace handling */
nativeInt returns [byte Value]:
	'native' INT {_localctx.Value = Actions.GetNativeIntType();};

nativeUint returns [byte Value]:
	'native' ('unsigned' INT | UINT) {_localctx.Value = Actions.GetNativeUIntType();};

/*  Security declarations  */
secDecl returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	PERMISSION secAction typeSpec '(' nameValPairs ')'
	| PERMISSION secAction typeSpec '=' '{' customBlobDescr '}'
	| PERMISSION secAction typeSpec
	| PERMISSIONSET secAction '=' 'bytearray'? '(' bytes ')'
	| PERMISSIONSET secAction 'bytearray' '(' bytes ')'
	| PERMISSIONSET secAction compQstring
	| PERMISSIONSET secAction '=' '{' secAttrSetBlob '}';
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

	PERMISSION: '.permission';
	PERMISSIONSET: '.permissionset';

	secAttrSetBlob: | (secAttrBlob ',')* secAttrBlob;

secAttrBlob:
	'class' SQSTRING '=' '{' customBlobNVPairs '}'
	| typeSpec '=' '{' customBlobNVPairs '}';

nameValPairs: (nameValPair ',')* nameValPair;

nameValPair: compQstring '=' caValue;

truefalse returns [bool Value]
@after {_localctx.Value = Actions.ParseBoolean(_localctx.Start);}
:
	'true'
	| 'false';

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
methodRef
returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	convention = callConv returnType = type owner = typeSpec '::' name = methodName genericArguments = typeArgs? arguments = sigArgs
		{_localctx.Value = Actions.CreateMethodReference(_localctx.Start, $convention.Value, $returnType.Value, $owner.Value, $name.Value, $genericArguments.ctx, null, $arguments.Value);}
	| convention = callConv returnType = type owner = typeSpec '::' name = methodName genericArity = genArityNotEmpty arguments = sigArgs
		{_localctx.Value = Actions.CreateMethodReference(_localctx.Start, $convention.Value, $returnType.Value, $owner.Value, $name.Value, null, $genericArity.Value, $arguments.Value);}
	| convention = callConv returnType = type name = methodName genericArguments = typeArgs? arguments = sigArgs
		{_localctx.Value = Actions.CreateMethodReference(_localctx.Start, $convention.Value, $returnType.Value, null, $name.Value, $genericArguments.ctx, null, $arguments.Value);}
	| convention = callConv returnType = type name = methodName genericArity = genArityNotEmpty arguments = sigArgs
		{_localctx.Value = Actions.CreateMethodReference(_localctx.Start, $convention.Value, $returnType.Value, null, $name.Value, null, $genericArity.Value, $arguments.Value);}
	| token = mdtoken {_localctx.Value = Actions.CreateTokenMethodReference($token.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateTypedefMethodReference(_localctx.Start, $alias.Value);} /* typeDef */
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

callConv returns [byte Value]:
	INSTANCE inner = callConv {_localctx.Value = Actions.AddInstanceCallingConvention($inner.Value);}
	| EXPLICIT inner = callConv {_localctx.Value = Actions.AddExplicitCallingConvention($inner.Value);}
	| kind = callKind {_localctx.Value = $kind.Value;}
	| 'callconv' '(' raw = int32 ')' {_localctx.Value = Actions.GetRawCallingConvention($raw.start);};

callKind returns [byte Value]
@init {_localctx.Value = Actions.GetDefaultCallingConvention();}
:
	/* EMPTY */
	| kind = DEFAULT {_localctx.Value = Actions.GetCallingConvention($kind);}
	| kind = VARARG {_localctx.Value = Actions.GetCallingConvention($kind);}
	| UNMANAGED kind = CDECL {_localctx.Value = Actions.GetCallingConvention($kind);}
	| UNMANAGED kind = STDCALL {_localctx.Value = Actions.GetCallingConvention($kind);}
	| UNMANAGED kind = THISCALL {_localctx.Value = Actions.GetCallingConvention($kind);}
	| UNMANAGED kind = FASTCALL {_localctx.Value = Actions.GetCallingConvention($kind);}
	| kind = UNMANAGED {_localctx.Value = Actions.GetCallingConvention($kind);}
;

mdtoken
returns [int Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	'mdtoken' '(' token = int32 ')' {_localctx.Value = Actions.ParseInt32($token.start);}
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

memberRef returns [object Value]:
	'method' method = methodRef {_localctx.Value = Actions.CreateMethodMemberReference($method.Value);}
	| 'field' field = fieldRef {_localctx.Value = Actions.CreateFieldMemberReference($field.Value);}
	| token = mdtoken {_localctx.Value = Actions.CreateTokenMemberReference($token.Value);};

fieldRef
returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	fieldType = type owner = typeSpec '::' name = dottedName
		{_localctx.Value = Actions.CreateFieldReference($fieldType.Value, $owner.Value, $name.Value);}
	| fieldType = type name = dottedName
		{_localctx.Value = Actions.CreateFieldReference($fieldType.Value, null, $name.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateTypedefFieldReference(_localctx.Start, $alias.Value);} // typedef
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

/* Generic type parameters declaration  */
typeList returns [object Value]
@init {Actions.BeginGenericTypeList(_localctx);}
:
	(item = typeSpec {Actions.AddGenericType(_localctx, $item.Value);} ',')*
	tail = typeSpec {Actions.AddGenericType(_localctx, $tail.Value);}
;
finally {_localctx.Value = Actions.EndGenericTypeList(_localctx);}

typarsClause returns [object Value]
@init {_localctx.Value = Actions.CreateEmptyGenericParameterList();}
:
	/* EMPTY */
	| '<' parameters = typars '>' {_localctx.Value = $parameters.Value;}
;

typarAttrib returns [object Value]:
	covariant = PLUS {_localctx.Value = Actions.CreateGenericParameterAttribute($covariant);}
	| contravariant = '-' {_localctx.Value = Actions.CreateGenericParameterAttribute($contravariant);}
	| class = 'class' {_localctx.Value = Actions.CreateGenericParameterAttribute($class);}
	| valuetype = VALUETYPE {_localctx.Value = Actions.CreateGenericParameterAttribute($valuetype);}
	| byrefLike = 'byreflike' {_localctx.Value = Actions.CreateGenericParameterAttribute($byrefLike);}
	| ctor = '.ctor' {_localctx.Value = Actions.CreateGenericParameterAttribute($ctor);}
	| 'flags' '(' flags = int32 ')' {_localctx.Value = Actions.CreateRawGenericParameterAttribute($flags.start);};

typarAttribs returns [object Value]
@init {Actions.BeginGenericParameterAttributes(_localctx);}
:
	(attribute = typarAttrib {Actions.AddGenericParameterAttribute(_localctx, $attribute.Value);})*
;
finally {_localctx.Value = Actions.EndGenericParameterAttributes(_localctx);}

typar returns [object Value]:
	attributes = typarAttribs constraints = tyBound? name = dottedName
		{_localctx.Value = Actions.CreateGenericParameterDeclaration($attributes.Value, $constraints.ctx, $name.Value);};

typars returns [object Value]
@init {Actions.BeginGenericParameters(_localctx);}
:
	(parameter = typar {Actions.AddGenericParameter(_localctx, $parameter.Value);} ',')*
	tail = typar {Actions.AddGenericParameter(_localctx, $tail.Value);}
;
finally {_localctx.Value = Actions.EndGenericParameters(_localctx);}

tyBound returns [object Value]:
	'(' constraints = typeList ')' {_localctx.Value = $constraints.Value;};

genArity returns [int Value]:
	value = genArityNotEmpty? {_localctx.Value = Actions.GetGenericArity($value.ctx);};

genArityNotEmpty returns [int Value]:
	'<' '[' value = int32 ']' '>' {_localctx.Value = Actions.ParseInt32($value.start);};

/*  Class body declarations  */
classDecl
@init {BeginStreaming();}
:
	methodHead '{' methodDecls '}'
	| classHead '{' classDecls '}'
	| eventHeader = eventHead {Actions.BeginEvent(_localctx, $eventHeader.Value);} '{' eventDecls '}'
	| property = propHead {Actions.BeginProperty(_localctx, $property.Value);} '{' propDecls '}'
	| fieldDecl
	| data = dataDecl {Actions.ProcessClassDataDeclaration($data.ctx);}
	| security = secDecl {Actions.ProcessClassSecurityDeclaration($security.ctx);}
	| source = extSourceSpec {Actions.ProcessClassSourceDirective($source.ctx);}
	| attribute = customAttrDecl {Actions.ProcessClassCustomAttribute($attribute.ctx);}
	| '.size' size = int32 {Actions.SetClassSize($size.start);}
	| '.pack' packing = int32 {Actions.SetClassPackingSize($packing.start);}
	| export = exportHead '{' exportDeclarations = exptypeDecls '}'
		{Actions.ProcessClassExport($export.ctx, $exportDeclarations.ctx);}
	| OVERRIDE declarationOwner = typeSpec '::' declarationName = methodName 'with'
		bodyConvention = callConv bodyReturnType = type bodyOwner = typeSpec '::'
		bodyName = methodName bodyArguments = sigArgs
		{Actions.AddClassMethodOverride(
			_localctx,
			$declarationOwner.Value,
			$declarationName.Value,
			$bodyConvention.Value,
			$bodyReturnType.Value,
			$bodyOwner.Value,
			$bodyName.Value,
			$bodyArguments.Value);}
	| OVERRIDE 'method'
		declarationConvention = callConv declarationReturnType = type declarationOwner = typeSpec '::'
		declarationName = methodName declarationArity = genArity declarationArguments = sigArgs
		'with' 'method'
		bodyConvention = callConv bodyReturnType = type bodyOwner = typeSpec '::'
		bodyName = methodName bodyArity = genArity bodyArguments = sigArgs
		{Actions.AddClassMethodOverride(
			_localctx,
			$declarationConvention.Value,
			$declarationReturnType.Value,
			$declarationOwner.Value,
			$declarationName.Value,
			$declarationArity.Value,
			$declarationArguments.Value,
			$bodyConvention.Value,
			$bodyReturnType.Value,
			$bodyOwner.Value,
			$bodyName.Value,
			$bodyArity.Value,
			$bodyArguments.Value);}
	| language = languageDecl {Actions.ProcessClassLanguageDirective($language.ctx);}
	| compControl {Actions.ProcessClassCompilerControl();}
	| PARAM TYPE '[' parameterIndex = int32 ']'
		{Actions.BeginClassGenericParameterDirective(_localctx, $parameterIndex.start);}
		(attribute = customAttrDecl {Actions.AddClassGenericDirectiveAttribute(_localctx, $attribute.ctx);})*
	| PARAM TYPE parameterName = dottedName
		{Actions.BeginClassGenericParameterDirective(_localctx, $parameterName.Value);}
		(attribute = customAttrDecl {Actions.AddClassGenericDirectiveAttribute(_localctx, $attribute.ctx);})*
	| PARAM CONSTRAINT '[' parameterIndex = int32 ']' ',' constraintType = typeSpec
		{Actions.BeginClassGenericConstraintDirective(_localctx, $parameterIndex.start, $constraintType.Value);}
		(attribute = customAttrDecl {Actions.AddClassGenericDirectiveAttribute(_localctx, $attribute.ctx);})*
	| PARAM CONSTRAINT parameterName = dottedName ',' constraintType = typeSpec
		{Actions.BeginClassGenericConstraintDirective(_localctx, $parameterName.Value, $constraintType.Value);}
		(attribute = customAttrDecl {Actions.AddClassGenericDirectiveAttribute(_localctx, $attribute.ctx);})*
	| '.interfaceimpl' TYPE interfaceType = typeSpec interfaceAttribute = customDescr
		{Actions.AddInterfaceImplementationAttribute(_localctx, $interfaceType.Value, $interfaceAttribute.ctx);}
;
finally {EndParseTreeMode(); Actions.EndClassDeclaration(_localctx);}

/*  Field declaration  */
fieldDecl returns [object Value]
@init {BeginStreaming(); Actions.BeginFieldDeclaration(_localctx);}
@after {Actions.DefineField(_localctx, _localctx.Value);}
:
	'.field' offset = repeatOpt
	(
		attribute = fieldAttr {Actions.AddFieldAttribute(_localctx, $attribute.Value);}
		| 'marshal' '(' marshalling = marshalBlob ')' {Actions.SetFieldMarshalling(_localctx, $marshalling.Value);}
	)*
	fieldType = type name = dottedName data = atOpt initializer = initOpt
		{_localctx.Value = Actions.CreateFieldDeclaration(
			_localctx,
			$offset.ctx,
			$fieldType.Value,
			$name.Value,
			$data.Value,
			$initializer.Value);}
;
finally {Actions.EndFieldDeclaration(_localctx); EndParseTreeMode();}

fieldAttr returns [object Value]:
	attribute = 'static' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'public' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'private' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'family' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'initonly' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'rtspecialname' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'specialname' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'assembly' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'famandassem' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'famorassem' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'privatescope' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'literal' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'notserialized' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| attribute = 'volatile' {_localctx.Value = Actions.CreateFieldAttribute($attribute);}
	| 'flags' '(' flags = int32 ')' {_localctx.Value = Actions.CreateRawFieldAttribute($flags.start);};

atOpt returns [string Value]:
	/* EMPTY */
	| 'at' name = id {_localctx.Value = Actions.GetFieldDataName($name.start);}
	| 'at' offset = int32 {_localctx.Value = Actions.GetFieldDataOffset($offset.start);};

initOpt returns [object Value, bool HasSyntaxError]
@init {Actions.BeginFieldInitializer(_localctx);}
:
	/* EMPTY */
	| '=' initializer = fieldInit {Actions.SetFieldInitializer(_localctx, $initializer.Value);}
;
finally {_localctx.HasSyntaxError = Actions.EndFieldInitializer(_localctx);}

repeatOpt returns [int Value, bool HasValue]:
	/* EMPTY */
	| '[' offset = int32 ']' {Actions.SetFieldOffset(_localctx, $offset.start);};

/*  Event declaration  */
eventHead returns [object Value]
@init {Actions.BeginEventHeader(_localctx);}
:
	'.event'
	(attribute = eventAttr {Actions.AddEventAttribute(_localctx, $attribute.Value);})*
	eventType = typeSpec name = dottedName
		{_localctx.Value = Actions.CreateEventHeader(_localctx, $eventType.Value, $name.Value);}
	| '.event'
	(attribute = eventAttr {Actions.AddEventAttribute(_localctx, $attribute.Value);})*
	name = dottedName
		{_localctx.Value = Actions.CreateEventHeader(_localctx, null, $name.Value);}
;
finally {Actions.EndEventHeader(_localctx);}

eventAttr returns [object Value]:
	attribute = 'rtspecialname' {_localctx.Value = Actions.CreateEventAttribute($attribute);}
	| attribute = 'specialname' {_localctx.Value = Actions.CreateEventAttribute($attribute);};

eventDecls: eventDecl*;

eventDecl:
	'.addon' accessor = methodRef {Actions.AddEventAdder(_localctx, $accessor.Value);}
	| '.removeon' accessor = methodRef {Actions.AddEventRemover(_localctx, $accessor.Value);}
	| '.fire' accessor = methodRef {Actions.AddEventRaiser(_localctx, $accessor.Value);}
	| '.other' accessor = methodRef {Actions.AddEventOther(_localctx, $accessor.Value);}
	| source = extSourceSpec {Actions.ProcessEventSourceDirective($source.ctx);}
	| attribute = customAttrDecl {Actions.AddEventCustomAttribute(_localctx, $attribute.ctx);}
	| language = languageDecl {Actions.ProcessEventLanguageDirective($language.ctx);}
	| compControl;

/*  Property declaration  */
propHead returns [object Value]
@init {Actions.BeginPropertyHeader(_localctx);}
:
	'.property'
	(attribute = propAttr {Actions.AddPropertyAttribute(_localctx, $attribute.Value);})*
	convention = callConv propertyType = type name = dottedName arguments = sigArgs initializer = initOpt
		{_localctx.Value = Actions.CreatePropertyHeader(
			_localctx,
			$convention.Value,
			$propertyType.Value,
			$name.Value,
			$arguments.Value,
			$initializer.Value);}
;
finally {Actions.EndPropertyHeader(_localctx);}

propAttr returns [object Value]:
	attribute = 'rtspecialname' {_localctx.Value = Actions.CreatePropertyAttribute($attribute);}
	| attribute = 'specialname' {_localctx.Value = Actions.CreatePropertyAttribute($attribute);};

propDecls: propDecl*;

propDecl:
	'.set' accessor = methodRef {Actions.AddPropertySetter(_localctx, $accessor.Value);}
	| '.get' accessor = methodRef {Actions.AddPropertyGetter(_localctx, $accessor.Value);}
	| '.other' accessor = methodRef {Actions.AddPropertyOther(_localctx, $accessor.Value);}
	| attribute = customAttrDecl {Actions.AddPropertyCustomAttribute(_localctx, $attribute.ctx);}
	| source = extSourceSpec {Actions.ProcessPropertySourceDirective($source.ctx);}
	| language = languageDecl {Actions.ProcessPropertyLanguageDirective($language.ctx);}
	| compControl;

/*  Method declaration  */

marshalClause returns [object Value]:
	/* EMPTY */ {_localctx.Value = Actions.CreateEmptyMarshallingDescriptor();}
	| 'marshal' '(' value = marshalBlob ')' {_localctx.Value = Actions.CompleteMarshalClause($value.Value);}
;

marshalBlob returns [object Value]
@init {Actions.BeginMarshalBlob(_localctx);}
:
	nativeValue = nativeType {Actions.SetMarshalBlobNativeType(_localctx, $nativeValue.Value);}
	| '{' (rawByte = hexbyte {Actions.AddMarshalBlobByte(_localctx, $rawByte.Value);})+ '}'
;
finally {_localctx.Value = Actions.EndMarshalBlob(_localctx);}

paramAttr returns [int Value]
@init {Actions.BeginParameterAttributes(_localctx);}
:
	(element = paramAttrElement {Actions.AddParameterAttribute(_localctx, $element.ctx);})*
;
finally {_localctx.Value = Actions.EndParameterAttributes(_localctx);}

paramAttrElement returns [int Value, bool ShouldAppend]:
	'[' attribute = 'in' ']' {Actions.SetParameterAttributeElement(_localctx, $attribute);}
	| '[' attribute = 'out' ']' {Actions.SetParameterAttributeElement(_localctx, $attribute);}
	| '[' attribute = 'opt' ']' {Actions.SetParameterAttributeElement(_localctx, $attribute);}
	| '[' raw = int32 ']' {Actions.SetRawParameterAttributeElement(_localctx, $raw.start);};

methodHead
returns [object Value]
@init {BeginStreaming(); Actions.BeginMethodHeader(_localctx);}
@after {Actions.BeginMethod(_localctx, _localctx.Value);}
:
	'.method'
	(
		attribute = methAttr {Actions.AddMethodAttribute(_localctx, $attribute.Value);}
		| pInvoke = pinvImpl {Actions.AddPInvoke(_localctx, $pInvoke.Value);}
	)*
	convention = callConv returnAttributes = paramAttr returnType = type returnMarshalling = marshalClause
	name = methodName genericParameters = typarsClause arguments = sigArgs
	(implementation = implAttr {Actions.AddMethodImplementationAttribute(_localctx, $implementation.Value);})*
		{_localctx.Value = Actions.CreateMethodHeader(
			_localctx,
			$convention.Value,
			$returnAttributes.Value,
			$returnType.Value,
			$returnMarshalling.Value,
			$name.Value,
			$genericParameters.Value,
			$arguments.Value);}
;
finally {Actions.EndMethodHeader(_localctx); EndParseTreeMode();}

methAttr returns [object Value]:
	attribute = 'static' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'public' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'private' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'family' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'final' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'specialname' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'virtual' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'strict' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'abstract' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'assembly' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'famandassem' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'famorassem' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'privatescope' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'hidebysig' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'newslot' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'rtspecialname' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'unmanagedexp' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| attribute = 'reqsecobj' {_localctx.Value = Actions.CreateMethodAttribute($attribute);}
	| 'flags' '(' flags = int32 ')' {_localctx.Value = Actions.CreateRawMethodAttribute($flags.start);};

pinvImpl returns [object Value]
@init {Actions.BeginPInvoke(_localctx);}
:
	'pinvokeimpl' '('
		(module = compQstring {Actions.SetPInvokeModule(_localctx, $module.Value);}
			('as' entryPoint = compQstring {Actions.SetPInvokeEntryPoint(_localctx, $entryPoint.Value);})?)?
		(attribute = pinvAttr {Actions.AddPInvokeAttribute(_localctx, $attribute.Value);})*
	')'
	| 'pinvokeimpl' '()'
;
finally {_localctx.Value = Actions.EndPInvoke(_localctx);}

pinvAttr returns [object Value]:
	attribute = 'nomangle' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'ansi' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'unicode' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'autochar' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'lasterr' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'winapi' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'cdecl' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'stdcall' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'thiscall' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| attribute = 'fastcall' {_localctx.Value = Actions.CreatePInvokeAttribute($attribute);}
	| 'bestfit' ':' setting = 'on' {_localctx.Value = Actions.CreateBestFitPInvokeAttribute($setting);}
	| 'bestfit' ':' setting = 'off' {_localctx.Value = Actions.CreateBestFitPInvokeAttribute($setting);}
	| 'charmaperror' ':' setting = 'on' {_localctx.Value = Actions.CreateCharMapErrorPInvokeAttribute($setting);}
	| 'charmaperror' ':' setting = 'off' {_localctx.Value = Actions.CreateCharMapErrorPInvokeAttribute($setting);}
	| 'flags' '(' flags = int32 ')' {_localctx.Value = Actions.CreateRawPInvokeAttribute($flags.start);};

methodName returns [string Value]:
	ctorName = '.ctor' {_localctx.Value = Actions.GetMethodName($ctorName);}
	| cctorName = '.cctor' {_localctx.Value = Actions.GetMethodName($cctorName);}
	| dotted = dottedName {_localctx.Value = $dotted.Value;};

implAttr returns [object Value]:
	attribute = 'native' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'cil' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'il' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'optil' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'managed' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'unmanaged' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'forwardref' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'preservesig' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'runtime' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'internalcall' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'synchronized' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'noinlining' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'aggressiveinlining' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'nooptimization' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'aggressiveoptimization' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| attribute = 'async' {_localctx.Value = Actions.CreateMethodImplementationAttribute($attribute);}
	| 'flags' '(' flags = int32 ')' {_localctx.Value = Actions.CreateRawMethodImplementationAttribute($flags.start);};

EMITBYTE: '.emitbyte';
MAXSTACK: '.maxstack';
ENTRYPOINT: '.entrypoint';
ZEROINIT: '.zeroinit';
LOCALS: '.locals';
EXPORT: '.export';
OVERRIDE: '.override';
VTENTRY: '.vtentry';

methodDecls
@init {BeginStreaming();}
:
    methodDecl*
;
finally {EndParseTreeMode();}

methodDecl:
	instr
	| EMITBYTE value = int32 {Actions.EmitByte($value.start);}
	| sehBlock
	| MAXSTACK value = int32 {Actions.SetMaxStack($value.start);}
	| ENTRYPOINT {Actions.SetEntryPoint();}
	| ZEROINIT {Actions.SetZeroInit();}
	| labelDecl
	| scopeBlock
	| localsDecl
	| declaration = dataDecl {Actions.ProcessMethodDataDeclaration($declaration.ctx);}
	| security = secDecl {Actions.ProcessMethodSecurityDeclaration($security.ctx);}
	| source = extSourceSpec {Actions.ProcessMethodSourceDirective($source.ctx);}
	| language = languageDecl {Actions.ProcessMethodLanguageDirective($language.ctx);}
	| attribute = customDescrInMethodBody {Actions.ProcessMethodCustomAttribute($attribute.ctx);}
	| compControl
	| exportDecl
	| vtentryDecl
	| overrideDecl
	| parameterDecl
;

localsDecl
@init {Actions.BeginSemanticRoot(_localctx);}
:
	LOCALS initialize = 'init'? arguments = sigArgs
;
finally {Actions.EndLocalsDirective(_localctx);}

exportDecl
@init {Actions.BeginSemanticRoot(_localctx);}
:
	EXPORT '[' ordinal = int32 ']' ('as' alias = id)?
;
finally {Actions.EndExportDirective(_localctx);}

vtentryDecl
@init {Actions.BeginSemanticRoot(_localctx);}
:
	VTENTRY table = int32 ':' slot = int32
;
finally {Actions.EndVTableEntryDirective(_localctx);}

overrideDecl
@init {Actions.BeginSemanticRoot(_localctx);}
:
	OVERRIDE owner = typeSpec '::' name = methodName
	| OVERRIDE 'method' convention = callConv returnType = type owner = typeSpec '::' name = methodName
		arity = genArity arguments = sigArgs
;
finally {Actions.EndOverrideDirective(_localctx);}

parameterDecl
@init {Actions.BeginParameterDirective(_localctx);}
:
	PARAM TYPE '[' genericIndex = int32 ']' (attribute = customAttrDecl {Actions.AddParameterCustomAttribute(_localctx, $attribute.ctx);})*
	| PARAM TYPE genericName = dottedName (attribute = customAttrDecl {Actions.AddParameterCustomAttribute(_localctx, $attribute.ctx);})*
	| PARAM CONSTRAINT '[' constraintIndex = int32 ']' ',' constraintType = typeSpec
		(attribute = customAttrDecl {Actions.AddParameterCustomAttribute(_localctx, $attribute.ctx);})*
	| PARAM CONSTRAINT constraintName = dottedName ',' constraintType = typeSpec
		(attribute = customAttrDecl {Actions.AddParameterCustomAttribute(_localctx, $attribute.ctx);})*
	| PARAM '[' parameterIndex = int32 ']' initializer = initOpt
		(attribute = customAttrDecl {Actions.AddParameterCustomAttribute(_localctx, $attribute.ctx);})*
;
finally {Actions.EndParameterDirective(_localctx);}

labelDecl:
	name = id ':' {Actions.DefineLabel($name.start);};

customDescrInMethodBody returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	directAttribute = customDescr {_localctx.Value = Actions.CreateCustomAttributeDeclaration($directAttribute.Value);}
	| ownedAttribute = customDescrWithOwner {_localctx.Value = Actions.CreateCustomAttributeDeclaration($ownedAttribute.Value);};
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

scopeBlock
@init {Actions.BeginScope(_localctx);}
:
	'{' methodDecls '}'
;
finally {Actions.EndScope(_localctx);}

/* Structured exception handling directives  */
sehBlock
@init {Actions.BeginExceptionBlock(_localctx);}
:
	tryRange = tryBlock clauses = sehClauses
;
finally {Actions.EndExceptionBlock(_localctx);}

sehClauses returns [object Value]
@init {Actions.BeginExceptionClauses(_localctx);}
:
	(clause = sehClause {Actions.AddExceptionClause(_localctx, $clause.Value);})+
;
finally {_localctx.Value = Actions.EndExceptionClauses(_localctx);}

tryBlock returns [object Value]:
	'.try' body = scopeBlock {_localctx.Value = Actions.CreateScopeExceptionRange($body.ctx);}
	| '.try' startLabel = id 'to' endLabel = id
		{_localctx.Value = Actions.CreateLabelExceptionRange($startLabel.start, $endLabel.start);}
	| '.try' startOffset = int32 'to' endOffset = int32
		{_localctx.Value = Actions.CreateOffsetExceptionRange($startOffset.start, $endOffset.start);};

sehClause returns [object Value]:
	caught = catchClause handler = handlerBlock
		{_localctx.Value = Actions.CreateCatchExceptionClause($caught.Value, $handler.Value);}
	| filtered = filterClause handler = handlerBlock
		{_localctx.Value = Actions.CreateFilterExceptionClause($filtered.Value, $handler.Value);}
	| finallyClause handler = handlerBlock
		{_localctx.Value = Actions.CreateFinallyExceptionClause($handler.Value);}
	| faultClause handler = handlerBlock
		{_localctx.Value = Actions.CreateFaultExceptionClause($handler.Value);};

filterClause returns [object Value]:
	'filter' body = scopeBlock {_localctx.Value = Actions.CreateScopeFilter($body.ctx);}
	| 'filter' label = id {_localctx.Value = Actions.CreateLabelFilter($label.start);}
	| 'filter' offset = int32 {_localctx.Value = Actions.CreateOffsetFilter($offset.start);};

catchClause
returns [object Value]
@init {Actions.BeginCatchClause(_localctx);}
:
	'catch' catchType = typeSpec
;
finally {_localctx.Value = Actions.EndCatchClause(_localctx);}

finallyClause: 'finally';

faultClause: 'fault';

handlerBlock returns [object Value]:
	body = scopeBlock {_localctx.Value = Actions.CreateScopeExceptionRange($body.ctx);}
	| 'handler' startLabel = id 'to' endLabel = id
		{_localctx.Value = Actions.CreateLabelExceptionRange($startLabel.start, $endLabel.start);}
	| 'handler' startOffset = int32 'to' endOffset = int32
		{_localctx.Value = Actions.CreateOffsetExceptionRange($startOffset.start, $endOffset.start);};

/*  Data declaration  */
dataDecl returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	ddHead ddBody
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

ddHead: '.data' tls id '=' | '.data' tls;

tls: /* EMPTY */ | 'tls' | 'cil';

ddBody: '{' ddItemList '}' | ddItem+;

ddItemList: (ddItem ',')* ddItem;

ddItemCount: /* EMPTY */ | '[' int32 ']';

ddItem:
	CHAR PTR '(' compQstring ')'
	| REF '(' id ')'
	| REF id
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
fieldSerInit returns [System.Reflection.Metadata.BlobBuilder Value]:
	FLOAT32 '(' float32Value = float64 ')'
		{_localctx.Value = Actions.CreateFloat32SerializedInitializer($float32Value.ctx, $float32Value.Value);}
	| FLOAT64_ '(' float64Value = float64 ')'
		{_localctx.Value = Actions.CreateFloat64SerializedInitializer($float64Value.ctx, $float64Value.Value);}
	| FLOAT32 '(' float32Bits = int32 ')'
		{_localctx.Value = Actions.CreateFloat32BitsSerializedInitializer($float32Bits.start);}
	| FLOAT64_ '(' float64Bits = int64 ')'
		{_localctx.Value = Actions.CreateFloat64BitsSerializedInitializer($float64Bits.start);}
	| int64Type = INT64_ '(' int64Value = int64 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($int64Type, $int64Value.start);}
	| int32Type = INT32_ '(' int32Value = int32 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($int32Type, $int32Value.start);}
	| int16Type = INT16 '(' int16Value = int32 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($int16Type, $int16Value.start);}
	| int8Type = INT8 '(' int8Value = int32 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($int8Type, $int8Value.start);}
	| uint64Type = UINT64 '(' uint64Value = int64 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($uint64Type, $uint64Value.start);}
	| uint32Type = UINT32 '(' uint32Value = int32 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($uint32Type, $uint32Value.start);}
	| uint16Type = UINT16 '(' uint16Value = int32 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($uint16Type, $uint16Value.start);}
	| uint8Type = UINT8 '(' uint8Value = int32 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($uint8Type, $uint8Value.start);}
	| charType = CHAR '(' charValue = int32 ')'
		{_localctx.Value = Actions.CreateIntegerSerializedInitializer($charType, $charValue.start);}
	| boolType = BOOL '(' boolValue = truefalse ')'
		{_localctx.Value = Actions.CreateBooleanSerializedInitializer($boolType, $boolValue.Value);}
	| 'bytearray' '(' byteArrayValue = bytes ')'
		{_localctx.Value = Actions.CreateByteArraySerializedInitializer($byteArrayValue.Value);};
finally {_localctx.Value ??= new System.Reflection.Metadata.BlobBuilder();}

bytes
returns [System.Collections.Immutable.ImmutableArray<byte> Value]
@init {BeginStreaming(); Actions.BeginBytes();}
:
	(b = hexbyte {Actions.AddByte($b.Value);})*
;
finally {_localctx.Value = Actions.EndBytes(); EndParseTreeMode();}

hexbyte
returns [byte Value]
@after {_localctx.Value = GrammarActions.ParseHexbyte(_localctx.Start);}
:
	INT32
	| ID
	| HEXBYTE
;
/*  Field/parameter initialization  */
fieldInit returns [object Value]:
	serializedValue = fieldSerInit {_localctx.Value = Actions.CreateFieldInitializer($serializedValue.Value);}
	| stringValue = compQstring {_localctx.Value = Actions.CreateFieldInitializer($stringValue.Value);}
	| NULLREF {_localctx.Value = Actions.CreateNullFieldInitializer();};

/*  Values for verbal form of CA blob description  */
serInit returns [object Value]:
	scalarValue = fieldSerInit
		{_localctx.Value = Actions.CreateScalarSerializedValue(_localctx, $scalarValue.ctx, $scalarValue.Value);}
	| STRING '(' NULLREF ')' {_localctx.Value = Actions.CreateStringSerializedValue();}
	| STRING '(' stringToken = SQSTRING ')' {_localctx.Value = Actions.CreateStringSerializedValue($stringToken);}
	| TYPE '(' 'class' typeToken = SQSTRING ')' {_localctx.Value = Actions.CreateTypeSerializedValue($typeToken);}
	| TYPE '(' typeName = className ')' {_localctx.Value = Actions.CreateTypeSerializedValue($typeName.Value);}
	| TYPE '(' NULLREF ')' {_localctx.Value = Actions.CreateNullTypeSerializedValue();}
	| OBJECT '(' objectValue = serInit ')' {_localctx.Value = Actions.CreateObjectSerializedValue($objectValue.Value);}
	| f32ElementToken = FLOAT32 '[' f32Length = int32 ']' '(' f32Values = f32seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($f32ElementToken, $f32Length.start, $f32Values.Value);}
	| f64ElementToken = FLOAT64_ '[' f64Length = int32 ']' '(' f64Values = f64seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($f64ElementToken, $f64Length.start, $f64Values.Value);}
	| i64ElementToken = INT64_ '[' i64Length = int32 ']' '(' i64Values = i64seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($i64ElementToken, $i64Length.start, $i64Values.Value);}
	| i32ElementToken = INT32_ '[' i32Length = int32 ']' '(' i32Values = i32seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($i32ElementToken, $i32Length.start, $i32Values.Value);}
	| i16ElementToken = INT16 '[' i16Length = int32 ']' '(' i16Values = i16seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($i16ElementToken, $i16Length.start, $i16Values.Value);}
	| i8ElementToken = INT8 '[' i8Length = int32 ']' '(' i8Values = i8seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($i8ElementToken, $i8Length.start, $i8Values.Value);}
	| u64ElementToken = UINT64 '[' u64Length = int32 ']' '(' u64Values = i64seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($u64ElementToken, $u64Length.start, $u64Values.Value);}
	| u32ElementToken = UINT32 '[' u32Length = int32 ']' '(' u32Values = i32seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($u32ElementToken, $u32Length.start, $u32Values.Value);}
	| u16ElementToken = UINT16 '[' u16Length = int32 ']' '(' u16Values = i16seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($u16ElementToken, $u16Length.start, $u16Values.Value);}
	| u8ElementToken = UINT8 '[' u8Length = int32 ']' '(' u8Values = i8seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($u8ElementToken, $u8Length.start, $u8Values.Value);}
	| charElementToken = CHAR '[' charLength = int32 ']' '(' charValues = i16seq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($charElementToken, $charLength.start, $charValues.Value);}
	| boolElementToken = BOOL '[' boolLength = int32 ']' '(' boolValues = boolSeq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($boolElementToken, $boolLength.start, $boolValues.Value);}
	| stringElementToken = STRING '[' stringLength = int32 ']' '(' stringValues = sqstringSeq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($stringElementToken, $stringLength.start, $stringValues.Value);}
	| typeElementToken = TYPE '[' typeLength = int32 ']' '(' typeValues = classSeq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($typeElementToken, $typeLength.start, $typeValues.Value);}
	| objectElementToken = OBJECT '[' objectLength = int32 ']' '(' objectValues = objSeq ')'
		{_localctx.Value = Actions.CreateArraySerializedValue($objectElementToken, $objectLength.start, $objectValues.Value);};

f32seq returns [System.Reflection.Metadata.BlobBuilder Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(floatingValue = float64 {Actions.AddFloat32SequenceValue(_localctx, $floatingValue.Value);}
	| integerValue = int32 {Actions.AddFloat32SequenceValue(_localctx, $integerValue.start);})*
;
finally {_localctx.Value = Actions.EndSerializationSequence(_localctx);}

f64seq returns [System.Reflection.Metadata.BlobBuilder Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(floatingValue = float64 {Actions.AddFloat64SequenceValue(_localctx, $floatingValue.Value);}
	| integerValue = int64 {Actions.AddFloat64SequenceValue(_localctx, $integerValue.start);})*
;
finally {_localctx.Value = Actions.EndSerializationSequence(_localctx);}

i64seq returns [System.Reflection.Metadata.BlobBuilder Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(value = int64 {Actions.AddInt64SequenceValue(_localctx, $value.start);})*
;
finally {_localctx.Value = Actions.EndSerializationSequence(_localctx);}

i32seq returns [System.Reflection.Metadata.BlobBuilder Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(value = int32 {Actions.AddInt32SequenceValue(_localctx, $value.start);})*
;
finally {_localctx.Value = Actions.EndSerializationSequence(_localctx);}

i16seq returns [System.Reflection.Metadata.BlobBuilder Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(value = int32 {Actions.AddInt16SequenceValue(_localctx, $value.start);})*
;
finally {_localctx.Value = Actions.EndSerializationSequence(_localctx);}

i8seq returns [System.Reflection.Metadata.BlobBuilder Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(value = int32 {Actions.AddInt8SequenceValue(_localctx, $value.start);})*
;
finally {_localctx.Value = Actions.EndSerializationSequence(_localctx);}

boolSeq returns [System.Reflection.Metadata.BlobBuilder Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(value = truefalse {Actions.AddBooleanSequenceValue(_localctx, $value.Value);})*
;
finally {_localctx.Value = Actions.EndSerializationSequence(_localctx);}

sqstringSeq returns [System.Reflection.Metadata.BlobBuilder Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(nullValue = NULLREF {Actions.AddStringSequenceValue(_localctx, $nullValue);}
	| stringValue = SQSTRING {Actions.AddStringSequenceValue(_localctx, $stringValue);})*
;
finally {_localctx.Value = Actions.EndSerializationSequence(_localctx);}

classSeq returns [object Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(value = classSeqElement {Actions.AddClassSequenceValue(_localctx, $value.Value);})*
;
finally {_localctx.Value = Actions.EndClassSerializationSequence(_localctx);}

classSeqElement returns [object Value]:
	NULLREF {_localctx.Value = Actions.CreateNullClassSequenceValue();}
	| 'class' quotedValue = SQSTRING {_localctx.Value = Actions.CreateQuotedClassSequenceValue($quotedValue);}
	| typeValue = className {_localctx.Value = Actions.CreateClassSequenceValue($typeValue.Value);};

objSeq returns [object Value]
@init {Actions.BeginSerializationSequence(_localctx);}
:
	(value = serInit {Actions.AddObjectSequenceValue(_localctx, $value.Value);})*
;
finally {_localctx.Value = Actions.EndObjectSerializationSequence(_localctx);}

customAttrDecl returns [object Value, bool HasSyntaxError]
@init {Actions.BeginSemanticRoot(_localctx);}
:
	directAttribute = customDescr {_localctx.Value = Actions.CreateCustomAttributeDeclaration($directAttribute.Value);}
	| ownedAttribute = customDescrWithOwner {_localctx.Value = Actions.CreateCustomAttributeDeclaration($ownedAttribute.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateCustomAttributeTypedef($alias.Value);};
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx);}

/* Assembly References */
asmOrRefDecl:
	('.publickey' | '.publicKey') '=' '(' bytes ')'
	| '.ver' intOrWildcard ':' intOrWildcard ':' intOrWildcard ':' intOrWildcard
	| '.locale' compQstring
	| '.locale' '=' '(' bytes ')'
	| customAttrDecl
	| compControl;

assemblyRefBlock returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	assemblyRefHead '{' assemblyRefDecls '}'
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

assemblyRefHead:
	'.assembly' 'extern' asmAttr dottedName
	| '.assembly' 'extern' asmAttr dottedName 'as' dottedName;

assemblyRefDecls: assemblyRefDecl*;

assemblyRefDecl:
	'.hash' '=' '(' bytes ')'
	| asmOrRefDecl
	| '.publickeytoken' '=' '(' bytes ')'
	| 'auto';

exptypeBlock returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	exptypeHead '{' exptypeDecls '}'
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

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

manifestResBlock returns [bool HasSyntaxError]
@init {BeginSubtree(); Actions.BeginSemanticRoot(_localctx);}
:
	manifestResHead '{' manifestResDecls '}'
;
finally {_localctx.HasSyntaxError = Actions.EndSemanticRoot(_localctx); EndParseTreeMode();}

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
