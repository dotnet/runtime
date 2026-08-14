/*
Licensed to the .NET Foundation under one or more agreements.
The .NET Foundation licenses this file to you under the MIT license.
*/

grammar CIL;

@parser::header {
#nullable enable annotations
}

@parser::members {
    internal GrammarActions Actions { get; set; } = null!;
}

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
// NESTEDSTRUCT, VARIANTBOOL, ANSIBSTR are parser rules to handle whitespace
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
// NATIVE_INT and NATIVE_UINT are parser rules (nativeInt, nativeUint)
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
locals [CILParser.DottedNameBuilder Builder]
@init {_localctx.Builder = new CILParser.DottedNameBuilder();}
:
	direct = DOTTEDNAME {Actions.AddDottedNameToken(_localctx.Builder, $direct);}
	| ((part = dottedNamePart {Actions.AddDottedNamePart(_localctx.Builder, $part.Value);} '.')*
		tail = dottedNamePart {Actions.AddDottedNamePart(_localctx.Builder, $tail.Value);})
	| quoted = SQSTRING {Actions.AddDottedNameToken(_localctx.Builder, $quoted);}
;
finally {_localctx.Value = Actions.EndDottedName(_localctx.Builder);}

dottedNamePart returns [string Value]
@init {_localctx.Value = string.Empty;}
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
locals [System.Text.StringBuilder Builder]
@init {_localctx.Builder = new System.Text.StringBuilder();}
:
	(head = QSTRING {Actions.AddComposedStringPart(_localctx.Builder, $head);} PLUS)*
	tail = QSTRING {Actions.AddComposedStringPart(_localctx.Builder, $tail);}
;
finally {_localctx.Value = Actions.EndComposedString(_localctx.Builder);}


WS: [ \t\r\n] -> skip;
SINGLE_LINE_COMMENT: '//' ~[\r\n]* -> skip;
COMMENT: '/*' .*? '*/' -> skip;
PERMISSION: '.permission';
PERMISSIONSET: '.permissionset';

decls
:
    decl*
;

decl
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
finally {Actions.EndDeclaration(_localctx);}

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

assemblyBlock returns [CILParser.AssemblyDefinitionValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	'.assembly' attributes = asmAttr name = dottedName '{' declarations = assemblyDecls '}'
		{_localctx.Value = Actions.CreateAssemblyDefinition(
			$attributes.Value,
			$name.Value,
			$declarations.Value);};
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
	if (_localctx.HasSyntaxError)
	{
		_localctx.Value = null;
	}
}

mscorlib: '.mscorlib';

languageDecl returns [CILParser.LanguageDirectiveValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	'.language' language = languageString
		{_localctx.Value = Actions.CreateLanguageDirective($language.Value);}
	| '.language' language = languageString ',' vendor = languageString
		{_localctx.Value = Actions.CreateLanguageDirective($language.Value, $vendor.Value);}
	| '.language' language = languageString ',' vendor = languageString ',' documentType = languageString
		{_localctx.Value = Actions.CreateLanguageDirective($language.Value, $vendor.Value, $documentType.Value);};
finally {Actions.EndLanguageDirective(_localctx, _localctx.InitialSyntaxErrorCount);}

languageString returns [string Value]
@init {_localctx.Value = string.Empty;}
@after {_localctx.Value = Actions.ParseLanguageString(_localctx.Start);}
:
	SQSTRING
	| QSTRING;

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

intOrWildcard returns [int? Value]:
	value = int32 {_localctx.Value = Actions.ParseInt32($value.start);}
	| PTR {_localctx.Value = null;};

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
typedefDecl returns [CILParser.TypedefDeclarationValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.TypedefDeclarationValue.Error;
}
:
	'.typedef' signature = type 'as' alias = dottedName
		{_localctx.Value = Actions.CreateTypeSignatureTypedef($signature.Value, $alias.Value);}
	| '.typedef' classType = className 'as' alias = dottedName
		{_localctx.Value = Actions.CreateClassTypedef($classType.Value, $alias.Value);}
	| '.typedef' member = memberRef 'as' alias = dottedName
		{_localctx.Value = Actions.CreateMemberTypedef($member.Value, $alias.Value);}
	| '.typedef' attribute = customDescr 'as' alias = dottedName
		{_localctx.Value = Actions.CreateCustomAttributeTypedefDeclaration(
			$attribute.Value,
			$attribute.start,
			$alias.Value);}
	| '.typedef' ownedAttribute = customDescrWithOwner 'as' alias = dottedName
		{_localctx.Value = Actions.CreateCustomAttributeTypedefDeclaration(
			$ownedAttribute.Value,
			$ownedAttribute.start,
			$alias.Value);};
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

/* Custom attribute declarations  */
customDescr returns [CILParser.CustomAttributeDescriptorValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.CustomAttributeDescriptorValue.Error;
}
:
	'.custom' constructor = customType
		{_localctx.Value = Actions.CreateDefaultCustomAttribute($constructor.Value);}
	| '.custom' constructor = customType '=' stringValue = compQstring
		{_localctx.Value = Actions.CreateStringCustomAttribute($constructor.Value, $stringValue.Value);}
	| '.custom' constructor = customType '=' '{' structuredValue = customBlobDescr '}'
		{_localctx.Value = Actions.CreateStructuredCustomAttribute($constructor.Value, $structuredValue.Value);}
	| '.custom' constructor = customType '=' '(' rawValue = bytes ')'
		{_localctx.Value = Actions.CreateRawCustomAttribute($constructor.Value, $rawValue.Value);};
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

customDescrWithOwner returns [CILParser.CustomAttributeDescriptorValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.CustomAttributeDescriptorValue.Error;
}
:
	'.custom' '(' owner = ownerType ')' constructor = customType
		{_localctx.Value = Actions.CreateDefaultOwnedCustomAttribute($owner.Value, $constructor.Value);}
	| '.custom' '(' owner = ownerType ')' constructor = customType '=' stringValue = compQstring
		{_localctx.Value = Actions.CreateStringOwnedCustomAttribute($owner.Value, $constructor.Value, $stringValue.Value);}
	| '.custom' '(' owner = ownerType ')' constructor = customType '=' '{' structuredValue = customBlobDescr '}'
		{_localctx.Value = Actions.CreateStructuredOwnedCustomAttribute($owner.Value, $constructor.Value, $structuredValue.Value);}
	| '.custom' '(' owner = ownerType ')' constructor = customType '=' '(' rawValue = bytes ')'
		{_localctx.Value = Actions.CreateRawOwnedCustomAttribute($owner.Value, $constructor.Value, $rawValue.Value);};
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

customType returns [CILParser.MethodReferenceValue Value]
@init {_localctx.Value = CILParser.MethodReferenceValue.Error;}
:
	constructor = methodRef {_localctx.Value = Actions.CreateCustomAttributeType($constructor.Value);};

ownerType
returns [CILParser.OwnerTypeValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.OwnerTypeValue.Error;
}
:
	typeValue = typeSpec {_localctx.Value = Actions.CreateTypeOwner($typeValue.Value);}
	| member = memberRef {_localctx.Value = Actions.CreateMemberOwner($member.Value);}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

/*  Verbal description of custom attribute initialization blob  */
customBlobDescr returns [CILParser.CustomAttributeBlobValue Value]
@init {_localctx.Value = CILParser.CustomAttributeBlobValue.Error;}
:
	arguments = customBlobArgs namedArguments = customBlobNVPairs
		{_localctx.Value = Actions.CreateCustomAttributeBlob($arguments.Value, $namedArguments.Value);};

customBlobArgs returns [System.Collections.Immutable.ImmutableArray<CILParser.SerializedInitializerValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.SerializedInitializerValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.SerializedInitializerValue>();}
:
	(argument = serInit {_localctx.Builder.Add($argument.Value);} | compControl)*
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

customBlobNVPairs returns [System.Collections.Immutable.ImmutableArray<CILParser.CustomAttributeNamedArgumentValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.CustomAttributeNamedArgumentValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.CustomAttributeNamedArgumentValue>();}
:
	(
		kind = fieldOrProp argumentType = serializType name = dottedName '=' value = serInit
			{_localctx.Builder.Add(Actions.CreateCustomBlobNamedArgument(
				$kind.Value,
				$argumentType.Value,
				$name.Value,
				$value.Value));}
		| compControl
	)*
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

fieldOrProp returns [byte Value]:
	kind = ('field' | 'property') {_localctx.Value = Actions.GetCustomAttributeNamedArgumentKind($kind);};

serializType returns [CILParser.SerializationTypeValue Value]
@init {_localctx.Value = CILParser.SerializationTypeValue.Error;}
:
	element = serializTypeElement array = ARRAY_TYPE_NO_BOUNDS?
		{_localctx.Value = Actions.CreateSerializationType($element.Value, $array);};

serializTypeElement returns [CILParser.SerializationTypeValue Value]
@init {_localctx.Value = CILParser.SerializationTypeValue.Error;}
:
	primitive = simpleType {_localctx.Value = Actions.CreatePrimitiveSerializationType($primitive.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateSerializationTypeTypedef(_localctx, $alias.Value);} /* typedef */
	| simpleTypeToken = TYPE {_localctx.Value = Actions.CreateSimpleSerializationType($simpleTypeToken);}
	| simpleTypeToken = OBJECT {_localctx.Value = Actions.CreateSimpleSerializationType($simpleTypeToken);}
	| ENUM 'class' quotedName = SQSTRING {_localctx.Value = Actions.CreateEnumSerializationType($quotedName);}
	| ENUM classNameValue = className {_localctx.Value = Actions.CreateEnumSerializationType($classNameValue.Value);};

/*  Module declaration */
moduleHead returns [string? Value, bool HasName, bool IsExternal]
@init {_localctx.Value = null;}
:
	MODULE 'extern' name = dottedName
		{Actions.SetModuleHeader(_localctx, $name.Value, true);}
	| MODULE name = dottedName
		{Actions.SetModuleHeader(_localctx, $name.Value, false);}
	| MODULE
		{Actions.SetEmptyModuleHeader(_localctx);};

/*  VTable Fixup table declaration  */
vtfixupDecl returns [CILParser.VTableFixupValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	'.vtfixup' '[' count = int32 ']' attributes = vtfixupAttr 'at' label = id
		{_localctx.Value = Actions.CreateVTableFixup(
			$count.start,
			$attributes.Value,
			$label.start);}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
	if (_localctx.HasSyntaxError)
	{
		_localctx.Value = null;
	}
}

vtfixupAttr returns [ushort Value]
@init {_localctx.Value = 0;}
:
	(attribute = vtfixupAttrElement
		{_localctx.Value = Actions.AddVTableFixupAttribute(_localctx.Value, $attribute.Value);})*
	{_localctx.Value = Actions.CompleteVTableFixupAttributes(_localctx.Value);}
;

vtfixupAttrElement returns [ushort Value]
@after {_localctx.Value = Actions.ParseVTableFixupAttribute(_localctx.Start);}
:
	INT32_
	| INT64_
	| 'fromunmanaged'
	| 'callmostderived'
	| 'retainappdomain';

vtableDecl returns [CILParser.RawVTableValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	'.vtable' '=' '(' value = bytes ')'
		{_localctx.Value = Actions.CreateRawVTable($value.Value);} /* deprecated */
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
	if (_localctx.HasSyntaxError)
	{
		_localctx.Value = null;
	}
}

/*  Namespace and class declaration  */
nameSpaceHead returns [string Value]
locals [int InitialSyntaxErrorCount]
@init {
	Actions.PrepareNamespaceHeader();
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = string.Empty;
}
@after {Actions.BeginNamespace(_localctx, _localctx.Value, _localctx.InitialSyntaxErrorCount);}
:
	'.namespace' name = dottedName {_localctx.Value = $name.Value;}
;

classHead returns [CILParser.ClassHeaderValue Value]
locals [int InitialSyntaxErrorCount, CILParser.ClassHeaderBuilder Builder]
@init {
	_localctx.Builder = Actions.PrepareClassHeader();
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.ClassHeaderValue.Error;
}
@after {Actions.BeginType(_localctx, _localctx.Value);}
:
	'.class'
	(attribute = classAttr {Actions.AddClassHeaderAttribute(_localctx.Builder, $attribute.Value);})*
	name = dottedName genericParameters = typarsClause baseType = extendsClause interfaces = implClause
		{_localctx.Value = Actions.CreateClassHeader(
			_localctx,
			_localctx.Builder,
			_localctx.InitialSyntaxErrorCount,
			$name.stop,
			$name.Value,
			$genericParameters.Value,
			$baseType.Value,
			$interfaces.Value);}
;


classAttr returns [CILParser.ClassAttributeValue Value]
@init {_localctx.Value = CILParser.ClassAttributeValue.Empty;}
:
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

extendsClause returns [CILParser.TypeSpecificationValue? Value]
@init {_localctx.Value = Actions.CreateEmptyClassBase();}
:
	/* EMPTY */
	| 'extends' baseType = typeSpec {_localctx.Value = Actions.CreateClassBase($baseType.Value);}
;

implClause returns [System.Collections.Immutable.ImmutableArray<CILParser.TypeSpecificationValue> Value]
@init {_localctx.Value = Actions.CreateEmptyInterfaceList();}
:
	/* EMPTY */
	| 'implements' interfaces = implList {_localctx.Value = $interfaces.Value;}
;

classDecls
:
    classDecl*
;

implList returns [System.Collections.Immutable.ImmutableArray<CILParser.TypeSpecificationValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.TypeSpecificationValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.TypeSpecificationValue>();}
:
	(interfaceType = typeSpec {_localctx.Builder.Add($interfaceType.Value);} ',')*
	lastInterfaceType = typeSpec {_localctx.Builder.Add($lastInterfaceType.Value);}
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

/*  External source declarations  */
esHead returns [bool AutoIncrement]
@after {_localctx.AutoIncrement = Actions.IsAutoIncrementSourceDirective(_localctx.Start);}
:
	'.line'
	| '#line';

extSourceSpec returns [CILParser.SourceDirectiveValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	head = esHead line = int32 path = (SQSTRING | QSTRING)?
		{_localctx.Value = Actions.CreateSourceLine($head.AutoIncrement, $line.start, $path);}
	| head = esHead line = int32 ':' column = int32 path = (SQSTRING | QSTRING)?
		{_localctx.Value = Actions.CreateSourceColumn($head.AutoIncrement, $line.start, $column.start, $path);}
	| head = esHead line = int32 ':' startColumn = int32 ',' endColumn = int32 path = (SQSTRING | QSTRING)?
		{_localctx.Value = Actions.CreateSourceColumnRange(
			$head.AutoIncrement,
			$line.start,
			$startColumn.start,
			$endColumn.start,
			$path);}
	| head = esHead startLine = int32 ',' endLine = int32 ':' column = int32 path = (SQSTRING | QSTRING)?
		{_localctx.Value = Actions.CreateSourceLineRange(
			$head.AutoIncrement,
			$startLine.start,
			$endLine.start,
			$column.start,
			$path);}
	| head = esHead startLine = int32 ',' endLine = int32 ':' startColumn = int32 ',' endColumn = int32
		path = (SQSTRING | QSTRING)?
		{_localctx.Value = Actions.CreateSourceRange(
			$head.AutoIncrement,
			$startLine.start,
			$endLine.start,
			$startColumn.start,
			$endColumn.start,
			$path);};
finally {Actions.EndSourceDirective(_localctx, _localctx.InitialSyntaxErrorCount);}

/*  Manifest declarations  */
fileDecl returns [CILParser.FileDeclarationValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount, CILParser.FileDeclarationBuilder Builder]
@init {
	_localctx.Builder = new CILParser.FileDeclarationBuilder();
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
}
:
	'.file'
		(attribute = fileAttr {Actions.AddFileAttribute(_localctx.Builder, $attribute.Value);})*
		name = dottedName {Actions.SetFileName(_localctx.Builder, $name.Value);}
		entry = fileEntry {Actions.AddFileEntry(_localctx.Builder, $entry.Value);}
		(HASH '=' '(' hash = bytes ')' {Actions.SetFileHash(_localctx.Builder, $hash.Value);}
			trailingEntry = fileEntry {Actions.AddFileEntry(_localctx.Builder, $trailingEntry.Value);})?
;
finally {Actions.EndFileDeclaration(_localctx, _localctx.Builder, _localctx.InitialSyntaxErrorCount);}

fileAttr returns [bool Value]
@after {_localctx.Value = Actions.ParseFileAttribute(_localctx.Start);}
:
	'nometadata';

fileEntry returns [bool Value]
@init {_localctx.Value = false;}
:
	/* EMPTY */
	| entry = '.entrypoint' {_localctx.Value = Actions.ParseFileEntry($entry);};

asmAttrAny returns [System.Reflection.AssemblyFlags Value, System.Reflection.AssemblyFlags Mask]
@after {Actions.SetAssemblyAttribute(_localctx);}
:
	'retargetable'
	| 'windowsruntime'
	| 'noplatform'
	| 'legacy library'
	| 'cil'
	| 'x86'
	| 'amd64'
	| 'arm'
	| 'arm64';

asmAttr returns [System.Reflection.AssemblyFlags Value]
@init {_localctx.Value = 0;}
:
	(attribute = asmAttrAny
		{_localctx.Value = Actions.AddAssemblyAttribute(
			_localctx.Value,
			$attribute.Value,
			$attribute.Mask);})*;

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
locals [CILParser.SwitchInstructionBuilder SwitchBuilder]
@after {Actions.CompleteSwitchInstruction(_localctx.SwitchBuilder);}
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
	| op = INSTR_SWITCH {_localctx.SwitchBuilder = Actions.CreateSwitchInstruction($op);}
		('(' labels[_localctx.SwitchBuilder] ')' | '()')
;

calliSignature
returns [CILParser.CalliSignatureValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.CalliSignatureValue.Error;
}
:
	convention = callConv returnType = type arguments = sigArgs
		{_localctx.Value = Actions.CreateCalliSignature($convention.Value, $returnType.Value, $arguments.Value);}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

labels [CILParser.SwitchInstructionBuilder Builder]:
	/* empty */
	| ((headLabel = id {Actions.AddSwitchLabel($Builder, $headLabel.start);} | headOffset = int32 {Actions.AddSwitchOffset($Builder, $headOffset.start);}) ',')*
	  (tailLabel = id {Actions.AddSwitchLabel($Builder, $tailLabel.start);} | tailOffset = int32 {Actions.AddSwitchOffset($Builder, $tailOffset.start);});

typeArgs returns [System.Collections.Immutable.ImmutableArray<CILParser.TypeValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.TypeValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.TypeValue>();}
:
	'<' (argument = type {_localctx.Builder.Add($argument.Value);} ',')*
		lastArgument = type {_localctx.Builder.Add($lastArgument.Value);} '>'
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

bounds returns [System.Collections.Immutable.ImmutableArray<CILParser.ArrayBoundValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.ArrayBoundValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.ArrayBoundValue>();}
:
	'[' (item = bound {_localctx.Builder.Add(Actions.CreateArrayBound($item.ctx));} ',')*
		lastItem = bound {_localctx.Builder.Add(Actions.CreateArrayBound($lastItem.ctx));} ']'
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

sigArgs returns [System.Collections.Immutable.ImmutableArray<CILParser.SignatureArgumentValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.SignatureArgumentValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.SignatureArgumentValue>();}
:
	'(' (argument = sigArg {_localctx.Builder.Add($argument.Value);} ',')*
		lastArgument = sigArg {_localctx.Builder.Add($lastArgument.Value);} ')'
	| '()'
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

sigArg returns [CILParser.SignatureArgumentValue Value]
@init {_localctx.Value = CILParser.SignatureArgumentValue.Error;}
:
	ELLIPSIS {_localctx.Value = Actions.CreateSentinelSignatureArgument();}
	| attributes = paramAttr argumentType = type marshalling = marshalClause name = id?
		{_localctx.Value = Actions.CreateSignatureArgument($attributes.Value, $argumentType.Value, $marshalling.Value, $name.ctx);};

/*  Class referencing  */

className returns [CILParser.ClassNameValue Value]
@init {_localctx.Value = CILParser.ClassNameValue.Error;}
:
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

slashedName returns [CILParser.TypeName Value]
locals [CILParser.TypeName CurrentName]
:
	(part = dottedName {_localctx.CurrentName = Actions.AddSlashedNamePart(_localctx.CurrentName, $part.Value);} '/')*
	lastPart = dottedName {_localctx.CurrentName = Actions.AddSlashedNamePart(_localctx.CurrentName, $lastPart.Value);}
;
finally {_localctx.Value = _localctx.CurrentName ?? new CILParser.TypeName(null, string.Empty);}

assemblyDecls returns [System.Collections.Immutable.ImmutableArray<CILParser.AssemblyDeclarationValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.AssemblyDeclarationValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.AssemblyDeclarationValue>();}
:
	(declaration = assemblyDecl
		{if ($declaration.Value is not null) _localctx.Builder.Add($declaration.Value);})*
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

assemblyDecl returns [CILParser.AssemblyDeclarationValue? Value]:
	HASH 'algorithm' algorithm = int32
		{_localctx.Value = Actions.CreateAssemblyHashAlgorithmDeclaration($algorithm.start);}
	| security = secDecl
		{_localctx.Value = Actions.CreateAssemblySecurityDeclaration(
			$security.Value,
			$security.start);}
	| shared = asmOrRefDecl {_localctx.Value = $shared.Value;};

typeSpec
returns [CILParser.TypeSpecificationValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.TypeSpecificationValue.Error;
}
:
	classType = className {_localctx.Value = Actions.CreateClassTypeSpecification($classType.Value);}
	| '[' assemblyName = dottedName ']' {_localctx.Value = Actions.CreateAssemblyTypeSpecification($assemblyName.Value);}
	| '[' MODULE moduleName = dottedName ']' {_localctx.Value = Actions.CreateModuleTypeSpecification($moduleName.Value);}
	| signatureType = type {_localctx.Value = Actions.CreateSignatureTypeSpecification($signatureType.Value);}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

/*  Native types for marshaling signatures  */
nativeType returns [CILParser.NativeTypeValue Value]
locals [CILParser.NativeTypeBuilder Builder]
@init {_localctx.Builder = new CILParser.NativeTypeBuilder();}
:
	/* EMPTY */
	| element = nativeTypeElement {Actions.SetNativeTypeElement(_localctx.Builder, $element.Value);}
		(info = nativeTypeArrayPointerInfo {Actions.AddNativeTypeArrayPointerInfo(_localctx.Builder, $info.Value);})*
;
finally {_localctx.Value = Actions.CreateNativeType(_localctx.Start, _localctx.Builder);}

nativeTypeArrayPointerInfo returns [CILParser.NativeTypeArrayPointerInfoValue Value]
@init {_localctx.Value = Actions.CreatePointerNativeType();}
:
	PTR {_localctx.Value = Actions.CreatePointerNativeType();} # PointerNativeType
	| ARRAY_TYPE_NO_BOUNDS {_localctx.Value = Actions.CreatePointerArrayTypeNoSizeData();} # PointerArrayTypeNoSizeData
	| '[' size = int32 ']' {_localctx.Value = Actions.CreatePointerArrayTypeSize($size.start);} # PointerArrayTypeSize
	| '[' size = int32 PLUS parameterIndex = int32 ']'
		{_localctx.Value = Actions.CreatePointerArrayTypeSizeParamIndex($size.start, $parameterIndex.start);} # PointerArrayTypeSizeParamIndex
	| '[' PLUS parameterIndex = int32 ']'
		{_localctx.Value = Actions.CreatePointerArrayTypeParamIndex($parameterIndex.start);} # PointerArrayTypeParamIndex
    ;

nativeTypeElement returns [CILParser.NativeTypeElementValue Value]
@init {_localctx.Value = CILParser.EmptyNativeTypeElementValue.Instance;}
:
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

iidParamIndex returns [CILParser.IidParamIndexValue Value]
@init {_localctx.Value = CILParser.IidParamIndexValue.Empty;}
:
	/* EMPTY */
	| '(' 'iidparam' '=' index = int32 ')' {_localctx.Value = Actions.GetIidParamIndex($index.start);};

variantType returns [CILParser.VariantTypeValue Value]
locals [CILParser.VariantTypeBuilder Builder]
@init {_localctx.Builder = new CILParser.VariantTypeBuilder();}
:
	/*EMPTY */
	| element = variantTypeElement {Actions.SetVariantTypeElement(_localctx.Builder, $element.Value);}
		(modifier = (ARRAY_TYPE_NO_BOUNDS | VECTOR | REF) {Actions.AddVariantTypeModifier(_localctx.Builder, $modifier);})*
;
finally {_localctx.Value = Actions.CreateVariantType(_localctx.Builder);}

variantTypeElement returns [CILParser.VariantTypeElementValue Value]
@init {_localctx.Value = CILParser.VariantTypeElementValue.Error;}
:
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
type returns [CILParser.TypeValue Value]
locals [
	CILParser.ElementTypeValue ElementType,
	System.Collections.Immutable.ImmutableArray<CILParser.TypeModifierValue>.Builder Modifiers
]
@init {
	_localctx.ElementType = CILParser.ElementTypeValue.Error;
	_localctx.Modifiers = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.TypeModifierValue>();
}
:
	element = elementType {_localctx.ElementType = $element.Value;}
	(modifier = typeModifiers {_localctx.Modifiers.Add($modifier.Value);})*
;
finally {_localctx.Value = new CILParser.TypeValue(_localctx.ElementType, _localctx.Modifiers.ToImmutable());}

typeModifiers returns [CILParser.TypeModifierValue Value]
@init {_localctx.Value = CILParser.TypeModifierValue.Error;}
:
	ARRAY_TYPE_NO_BOUNDS {_localctx.Value = Actions.CreateSzArrayTypeModifier();} # SZArrayModifier
	| '[' ']' {_localctx.Value = Actions.CreateSzArrayTypeModifier();} # SZArrayModifier
	| arrayBounds = bounds {_localctx.Value = Actions.CreateArrayTypeModifier($arrayBounds.Value);} # ArrayModifier
	| REF {_localctx.Value = Actions.CreateByReferenceTypeModifier();} # ByRefModifier
	| PTR {_localctx.Value = Actions.CreatePointerTypeModifier();} # PtrModifier
	| 'pinned' {_localctx.Value = Actions.CreatePinnedTypeModifier();} # PinnedModifier
	| 'modreq' '(' modifierType = typeSpec ')' {_localctx.Value = Actions.CreateCustomTypeModifier($modifierType.Value, true);} # RequiredModifier
	| 'modopt' '(' modifierType = typeSpec ')' {_localctx.Value = Actions.CreateCustomTypeModifier($modifierType.Value, false);} # OptionalModifier
	| arguments = typeArgs {_localctx.Value = Actions.CreateGenericArgumentsModifier($arguments.Value);} # GenericArgumentsModifier;

elementType returns [CILParser.ElementTypeValue Value]
@init {_localctx.Value = CILParser.ElementTypeValue.Error;}
:
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
secDecl returns [CILParser.SecurityDeclarationValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	PERMISSION action = secAction permissionType = typeSpec '(' pairs = nameValPairs ')'
		{_localctx.Value = Actions.CreateNamedPermissionDeclaration(
			$action.Value,
			$permissionType.Value,
			$pairs.Value);}
	| PERMISSION action = secAction permissionType = typeSpec '=' '{' structuredValue = customBlobDescr '}'
		{_localctx.Value = Actions.CreateStructuredPermissionDeclaration(
			$action.Value,
			$permissionType.Value,
			$structuredValue.Value);}
	| PERMISSION action = secAction permissionType = typeSpec
		{_localctx.Value = Actions.CreateEmptyPermissionDeclaration($action.Value, $permissionType.Value);}
	| PERMISSIONSET action = secAction '=' 'bytearray'? '(' rawValue = bytes ')'
		{_localctx.Value = Actions.CreateRawPermissionSetDeclaration($action.Value, $rawValue.Value);}
	| PERMISSIONSET action = secAction 'bytearray' '(' rawValue = bytes ')'
		{_localctx.Value = Actions.CreateRawPermissionSetDeclaration($action.Value, $rawValue.Value);}
	| PERMISSIONSET action = secAction textValue = compQstring
		{_localctx.Value = Actions.CreateStringPermissionSetDeclaration($action.Value, $textValue.Value);}
	| PERMISSIONSET action = secAction '=' '{' attributes = secAttrSetBlob '}'
		{_localctx.Value = Actions.CreateAttributePermissionSetDeclaration($action.Value, $attributes.Value);};
finally {Actions.EndSecurityDeclaration(_localctx, _localctx.InitialSyntaxErrorCount);}

secAttrSetBlob returns [System.Collections.Immutable.ImmutableArray<CILParser.SecurityAttributeValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.SecurityAttributeValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.SecurityAttributeValue>();}
:
	/* EMPTY */
	| (attribute = secAttrBlob {_localctx.Builder.Add($attribute.Value);} ',')*
		tail = secAttrBlob {_localctx.Builder.Add($tail.Value);}
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

secAttrBlob returns [CILParser.SecurityAttributeValue Value]
@init {_localctx.Value = CILParser.SecurityAttributeValue.Error;}
:
	'class' name = SQSTRING '=' '{' arguments = customBlobNVPairs '}'
		{_localctx.Value = Actions.CreateNamedSecurityAttribute($name, $arguments.Value);}
	| securityType = typeSpec '=' '{' arguments = customBlobNVPairs '}'
		{_localctx.Value = Actions.CreateTypedSecurityAttribute($securityType.Value, $arguments.Value);};

nameValPairs returns [System.Collections.Immutable.ImmutableArray<CILParser.SecurityNameValuePairValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.SecurityNameValuePairValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.SecurityNameValuePairValue>();}
:
	(pair = nameValPair {_localctx.Builder.Add($pair.Value);} ',')*
	tail = nameValPair {_localctx.Builder.Add($tail.Value);}
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

nameValPair returns [CILParser.SecurityNameValuePairValue Value]
@init {_localctx.Value = CILParser.SecurityNameValuePairValue.Error;}
:
	name = compQstring '=' value = caValue
		{_localctx.Value = Actions.CreateSecurityNameValuePair($name.Value, $value.Value);};

truefalse returns [bool Value]
@after {_localctx.Value = Actions.ParseBoolean(_localctx.Start);}
:
	'true'
	| 'false';

caValue returns [CILParser.SecurityCaValue Value]
@init {_localctx.Value = CILParser.SecurityCaValue.Error;}
:
	booleanValue = truefalse {_localctx.Value = Actions.CreateSecurityBooleanValue($booleanValue.Value);}
	| integerValue = int32 {_localctx.Value = Actions.CreateSecurityInt32Value($integerValue.start);}
	| INT32_ '(' integerValue = int32 ')' {_localctx.Value = Actions.CreateSecurityInt32Value($integerValue.start);}
	| textValue = compQstring {_localctx.Value = Actions.CreateSecurityStringValue($textValue.Value);}
	| enumType = className '(' kind = INT8 ':' enumValue = int32 ')'
		{_localctx.Value = Actions.CreateSecurityEnumValue($enumType.Value, $kind, $enumValue.start);}
	| enumType = className '(' kind = INT16 ':' enumValue = int32 ')'
		{_localctx.Value = Actions.CreateSecurityEnumValue($enumType.Value, $kind, $enumValue.start);}
	| enumType = className '(' kind = INT32_ ':' enumValue = int32 ')'
		{_localctx.Value = Actions.CreateSecurityEnumValue($enumType.Value, $kind, $enumValue.start);}
	| enumType = className '(' enumValue = int32 ')'
		{_localctx.Value = Actions.CreateSecurityEnumValue($enumType.Value, $enumValue.start);};

secAction returns [System.Reflection.DeclarativeSecurityAction Value]
@after {_localctx.Value = Actions.ParseSecurityAction(_localctx.Start);}
:
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
returns [CILParser.MethodReferenceValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.MethodReferenceValue.Error;
}
:
	convention = callConv returnType = type owner = typeSpec '::' name = methodName genericArguments = typeArgs? arguments = sigArgs
		{_localctx.Value = Actions.CreateMethodReference(_localctx.Start, $convention.Value, $returnType.Value, $owner.Value, $name.Value, $genericArguments.ctx is null ? null : $genericArguments.Value, null, $arguments.Value);}
	| convention = callConv returnType = type owner = typeSpec '::' name = methodName genericArity = genArityNotEmpty arguments = sigArgs
		{_localctx.Value = Actions.CreateMethodReference(_localctx.Start, $convention.Value, $returnType.Value, $owner.Value, $name.Value, null, $genericArity.Value, $arguments.Value);}
	| convention = callConv returnType = type name = methodName genericArguments = typeArgs? arguments = sigArgs
		{_localctx.Value = Actions.CreateMethodReference(_localctx.Start, $convention.Value, $returnType.Value, null, $name.Value, $genericArguments.ctx is null ? null : $genericArguments.Value, null, $arguments.Value);}
	| convention = callConv returnType = type name = methodName genericArity = genArityNotEmpty arguments = sigArgs
		{_localctx.Value = Actions.CreateMethodReference(_localctx.Start, $convention.Value, $returnType.Value, null, $name.Value, null, $genericArity.Value, $arguments.Value);}
	| token = mdtoken {_localctx.Value = Actions.CreateTokenMethodReference($token.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateTypedefMethodReference(_localctx.Start, $alias.Value);} /* typeDef */
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

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
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	'mdtoken' '(' token = int32 ')' {_localctx.Value = Actions.ParseInt32($token.start);}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

memberRef returns [CILParser.MemberReferenceValue Value]
@init {_localctx.Value = CILParser.MemberReferenceValue.Error;}
:
	'method' method = methodRef {_localctx.Value = Actions.CreateMethodMemberReference($method.Value);}
	| 'field' field = fieldRef {_localctx.Value = Actions.CreateFieldMemberReference($field.Value);}
	| token = mdtoken {_localctx.Value = Actions.CreateTokenMemberReference($token.Value);};

fieldRef
returns [CILParser.FieldReferenceValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.FieldReferenceValue.Error;
}
:
	fieldType = type owner = typeSpec '::' name = dottedName
		{_localctx.Value = Actions.CreateFieldReference($fieldType.Value, $owner.Value, $name.Value);}
	| fieldType = type name = dottedName
		{_localctx.Value = Actions.CreateFieldReference($fieldType.Value, null, $name.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateTypedefFieldReference(_localctx.Start, $alias.Value);} // typedef
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

/* Generic type parameters declaration  */
typeList returns [System.Collections.Immutable.ImmutableArray<CILParser.TypeSpecificationValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.TypeSpecificationValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.TypeSpecificationValue>();}
:
	(item = typeSpec {_localctx.Builder.Add($item.Value);} ',')*
	tail = typeSpec {_localctx.Builder.Add($tail.Value);}
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

typarsClause returns [System.Collections.Immutable.ImmutableArray<CILParser.GenericParameterDeclarationValue> Value]
@init {_localctx.Value = System.Collections.Immutable.ImmutableArray<CILParser.GenericParameterDeclarationValue>.Empty;}
:
	/* EMPTY */
	| '<' parameters = typars '>' {_localctx.Value = $parameters.Value;}
;

typarAttrib returns [CILParser.AttributeValue<System.Reflection.GenericParameterAttributes> Value]
@init {_localctx.Value = CILParser.AttributeValue<System.Reflection.GenericParameterAttributes>.Empty;}
:
	covariant = PLUS {_localctx.Value = Actions.CreateGenericParameterAttribute($covariant);}
	| contravariant = '-' {_localctx.Value = Actions.CreateGenericParameterAttribute($contravariant);}
	| class = 'class' {_localctx.Value = Actions.CreateGenericParameterAttribute($class);}
	| valuetype = VALUETYPE {_localctx.Value = Actions.CreateGenericParameterAttribute($valuetype);}
	| byrefLike = 'byreflike' {_localctx.Value = Actions.CreateGenericParameterAttribute($byrefLike);}
	| ctor = '.ctor' {_localctx.Value = Actions.CreateGenericParameterAttribute($ctor);}
	| 'flags' '(' flags = int32 ')' {_localctx.Value = Actions.CreateRawGenericParameterAttribute($flags.start);};

typarAttribs returns [System.Reflection.GenericParameterAttributes Value]
@init {_localctx.Value = 0;}
:
	(attribute = typarAttrib
		{_localctx.Value = Actions.AddGenericParameterAttribute(_localctx.Value, $attribute.Value);})*
;

typar returns [CILParser.GenericParameterDeclarationValue Value]
@init {_localctx.Value = CILParser.GenericParameterDeclarationValue.Error;}
:
	attributes = typarAttribs constraints = tyBound? name = dottedName
		{_localctx.Value = Actions.CreateGenericParameterDeclaration($attributes.Value, $constraints.ctx, $name.Value);};

typars returns [System.Collections.Immutable.ImmutableArray<CILParser.GenericParameterDeclarationValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.GenericParameterDeclarationValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.GenericParameterDeclarationValue>();}
:
	(parameter = typar {_localctx.Builder.Add($parameter.Value);} ',')*
	tail = typar {_localctx.Builder.Add($tail.Value);}
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

tyBound returns [System.Collections.Immutable.ImmutableArray<CILParser.TypeSpecificationValue> Value]
@init {_localctx.Value = System.Collections.Immutable.ImmutableArray<CILParser.TypeSpecificationValue>.Empty;}
:
	'(' constraints = typeList ')' {_localctx.Value = $constraints.Value;};

genArity returns [int Value]:
	value = genArityNotEmpty? {_localctx.Value = Actions.GetGenericArity($value.ctx);};

genArityNotEmpty returns [int Value]:
	'<' '[' value = int32 ']' '>' {_localctx.Value = Actions.ParseInt32($value.start);};

/*  Class body declarations  */
classDecl
locals [
	CILParser.PropertyBodyValue PropertyBody,
	CILParser.EventBodyValue EventBody,
	CILParser.CustomAttributeOwnerValue AttributeOwner
]
:
	methodHead '{' methodDecls '}'
	| classHead '{' classDecls '}'
	| eventHeader = eventHead {_localctx.EventBody = Actions.BeginEvent($eventHeader.Value);}
		'{' eventDecls[_localctx.EventBody] '}'
	| property = propHead {_localctx.PropertyBody = Actions.BeginProperty($property.Value);}
		'{' propDecls[_localctx.PropertyBody] '}'
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
		{_localctx.AttributeOwner = Actions.BeginClassGenericParameterDirective(_localctx, $parameterIndex.start);}
		(attribute = customAttrDecl {Actions.AddClassGenericDirectiveAttribute(_localctx.AttributeOwner, $attribute.ctx);})*
	| PARAM TYPE parameterName = dottedName
		{_localctx.AttributeOwner = Actions.BeginClassGenericParameterDirective($parameterName.Value);}
		(attribute = customAttrDecl {Actions.AddClassGenericDirectiveAttribute(_localctx.AttributeOwner, $attribute.ctx);})*
	| PARAM CONSTRAINT '[' parameterIndex = int32 ']' ',' constraintType = typeSpec
		{_localctx.AttributeOwner = Actions.BeginClassGenericConstraintDirective(_localctx, $parameterIndex.start, $constraintType.Value);}
		(attribute = customAttrDecl {Actions.AddClassGenericDirectiveAttribute(_localctx.AttributeOwner, $attribute.ctx);})*
	| PARAM CONSTRAINT parameterName = dottedName ',' constraintType = typeSpec
		{_localctx.AttributeOwner = Actions.BeginClassGenericConstraintDirective($parameterName.Value, $constraintType.Value);}
		(attribute = customAttrDecl {Actions.AddClassGenericDirectiveAttribute(_localctx.AttributeOwner, $attribute.ctx);})*
	| '.interfaceimpl' TYPE interfaceType = typeSpec interfaceAttribute = customDescr
		{Actions.AddInterfaceImplementationAttribute(_localctx, $interfaceType.Value, $interfaceAttribute.ctx);}
;
finally {Actions.EndClassDeclaration(_localctx);}

/*  Field declaration  */
fieldDecl returns [CILParser.FieldDeclarationValue Value]
locals [int InitialSyntaxErrorCount, CILParser.FieldDeclarationBuilder Builder]
@init {
	_localctx.Builder = Actions.PrepareFieldDeclaration();
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.FieldDeclarationValue.Error;
}
@after {Actions.DefineField(_localctx, _localctx.Value);}
:
	'.field' offset = repeatOpt
	(
		attribute = fieldAttr {Actions.AddFieldAttribute(_localctx.Builder, $attribute.Value);}
		| 'marshal' '(' marshalling = marshalBlob ')' {Actions.SetFieldMarshalling(_localctx.Builder, $marshalling.Value);}
	)*
	fieldType = type name = dottedName data = atOpt initializer = initOpt
		{_localctx.Value = Actions.CreateFieldDeclaration(
			_localctx,
			_localctx.Builder,
			_localctx.InitialSyntaxErrorCount,
			$offset.ctx,
			$fieldType.Value,
			$name.Value,
			$data.Value,
			$initializer.Value);}
;

fieldAttr returns [CILParser.AttributeValue<System.Reflection.FieldAttributes> Value]
@init {_localctx.Value = CILParser.AttributeValue<System.Reflection.FieldAttributes>.Empty;}
:
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

atOpt returns [string? Value]
@init {_localctx.Value = null;}
:
	/* EMPTY */
	| 'at' name = id {_localctx.Value = Actions.GetFieldDataName($name.start);}
	| 'at' offset = int32 {_localctx.Value = Actions.GetFieldDataOffset($offset.start);};

initOpt returns [CILParser.FieldInitializerValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.FieldInitializerValue.Empty;
}
:
	/* EMPTY */
	| '=' initializer = fieldInit {_localctx.Value = $initializer.Value;}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

repeatOpt returns [int Value, bool HasValue]:
	/* EMPTY */
	| '[' offset = int32 ']' {Actions.SetFieldOffset(_localctx, $offset.start);};

/*  Event declaration  */
eventHead returns [CILParser.EventHeaderValue Value]
locals [int InitialSyntaxErrorCount, CILParser.EventHeaderBuilder Builder]
@init {
	_localctx.Builder = new CILParser.EventHeaderBuilder();
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.EventHeaderValue.Error;
}
:
	'.event'
	(attribute = eventAttr {Actions.AddEventAttribute(_localctx.Builder, $attribute.Value);})*
	eventType = typeSpec name = dottedName
		{_localctx.Value = Actions.CreateEventHeader(
			_localctx,
			_localctx.Builder,
			_localctx.InitialSyntaxErrorCount,
			$eventType.Value,
			$name.Value);}
	| '.event'
	(attribute = eventAttr {Actions.AddEventAttribute(_localctx.Builder, $attribute.Value);})*
	name = dottedName
		{_localctx.Value = Actions.CreateEventHeader(
			_localctx,
			_localctx.Builder,
			_localctx.InitialSyntaxErrorCount,
			null,
			$name.Value);}
;

eventAttr returns [CILParser.AttributeValue<System.Reflection.EventAttributes> Value]
@init {_localctx.Value = CILParser.AttributeValue<System.Reflection.EventAttributes>.Empty;}
:
	attribute = 'rtspecialname' {_localctx.Value = Actions.CreateEventAttribute($attribute);}
	| attribute = 'specialname' {_localctx.Value = Actions.CreateEventAttribute($attribute);};

eventDecls [CILParser.EventBodyValue Body]: eventDecl[$Body]*;

eventDecl [CILParser.EventBodyValue Body]:
	'.addon' accessor = methodRef {Actions.AddEventAdder($Body, $accessor.Value);}
	| '.removeon' accessor = methodRef {Actions.AddEventRemover($Body, $accessor.Value);}
	| '.fire' accessor = methodRef {Actions.AddEventRaiser($Body, $accessor.Value);}
	| '.other' accessor = methodRef {Actions.AddEventOther($Body, $accessor.Value);}
	| source = extSourceSpec {Actions.ProcessEventSourceDirective($Body, $source.ctx);}
	| attribute = customAttrDecl {Actions.AddEventCustomAttribute($Body, $attribute.ctx);}
	| language = languageDecl {Actions.ProcessEventLanguageDirective($Body, $language.ctx);}
	| compControl;

/*  Property declaration  */
propHead returns [CILParser.PropertyHeaderValue Value]
locals [int InitialSyntaxErrorCount, CILParser.PropertyHeaderBuilder Builder]
@init {
	_localctx.Builder = new CILParser.PropertyHeaderBuilder();
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.PropertyHeaderValue.Error;
}
:
	'.property'
	(attribute = propAttr {Actions.AddPropertyAttribute(_localctx.Builder, $attribute.Value);})*
	convention = callConv propertyType = type name = dottedName arguments = sigArgs initializer = initOpt
		{_localctx.Value = Actions.CreatePropertyHeader(
			_localctx,
			_localctx.Builder,
			_localctx.InitialSyntaxErrorCount,
			$convention.Value,
			$propertyType.Value,
			$name.Value,
			$arguments.Value,
			$initializer.Value);}
;

propAttr returns [CILParser.AttributeValue<System.Reflection.PropertyAttributes> Value]
@init {_localctx.Value = CILParser.AttributeValue<System.Reflection.PropertyAttributes>.Empty;}
:
	attribute = 'rtspecialname' {_localctx.Value = Actions.CreatePropertyAttribute($attribute);}
	| attribute = 'specialname' {_localctx.Value = Actions.CreatePropertyAttribute($attribute);};

propDecls [CILParser.PropertyBodyValue Body]: propDecl[$Body]*;

propDecl [CILParser.PropertyBodyValue Body]:
	'.set' accessor = methodRef {Actions.AddPropertySetter($Body, $accessor.Value);}
	| '.get' accessor = methodRef {Actions.AddPropertyGetter($Body, $accessor.Value);}
	| '.other' accessor = methodRef {Actions.AddPropertyOther($Body, $accessor.Value);}
	| attribute = customAttrDecl {Actions.AddPropertyCustomAttribute($Body, $attribute.ctx);}
	| source = extSourceSpec {Actions.ProcessPropertySourceDirective($Body, $source.ctx);}
	| language = languageDecl {Actions.ProcessPropertyLanguageDirective($Body, $language.ctx);}
	| compControl;

/*  Method declaration  */

marshalClause returns [CILParser.MarshallingDescriptorValue Value]
@init {_localctx.Value = CILParser.MarshallingDescriptorValue.Empty;}
:
	/* EMPTY */ {_localctx.Value = Actions.CreateEmptyMarshallingDescriptor();}
	| 'marshal' '(' value = marshalBlob ')' {_localctx.Value = Actions.CompleteMarshalClause($value.Value);}
;

marshalBlob returns [CILParser.MarshallingDescriptorValue Value]
locals [CILParser.MarshalBlobBuilder Builder]
@init {_localctx.Builder = new CILParser.MarshalBlobBuilder();}
:
	nativeValue = nativeType {Actions.SetMarshalBlobNativeType(_localctx.Builder, $nativeValue.Value);}
	| '{' (rawByte = hexbyte {Actions.AddMarshalBlobByte(_localctx.Builder, $rawByte.Value);})+ '}'
;
finally {_localctx.Value = Actions.CreateMarshallingDescriptor(_localctx.Builder);}

paramAttr returns [int Value]
@init {_localctx.Value = 0;}
:
	(element = paramAttrElement
		{_localctx.Value = Actions.AddParameterAttribute(
			_localctx.Value,
			$element.Value,
			$element.ShouldAppend);})*
;

paramAttrElement returns [int Value, bool ShouldAppend]:
	'[' attribute = 'in' ']' {Actions.SetParameterAttributeElement(_localctx, $attribute);}
	| '[' attribute = 'out' ']' {Actions.SetParameterAttributeElement(_localctx, $attribute);}
	| '[' attribute = 'opt' ']' {Actions.SetParameterAttributeElement(_localctx, $attribute);}
	| '[' raw = int32 ']' {Actions.SetRawParameterAttributeElement(_localctx, $raw.start);};

methodHead
returns [CILParser.MethodHeaderValue Value]
locals [int InitialSyntaxErrorCount, CILParser.MethodHeaderBuilder Builder]
@init {
	_localctx.Builder = Actions.PrepareMethodHeader();
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.MethodHeaderValue.Error;
}
@after {Actions.BeginMethod(_localctx, _localctx.Value);}
:
	'.method'
	(
		attribute = methAttr {Actions.AddMethodAttribute(_localctx.Builder, $attribute.Value);}
		| pInvoke = pinvImpl {Actions.AddPInvoke(_localctx.Builder, $pInvoke.Value);}
	)*
	convention = callConv returnAttributes = paramAttr returnType = type returnMarshalling = marshalClause
	name = methodName genericParameters = typarsClause arguments = sigArgs
	(implementation = implAttr {Actions.AddMethodImplementationAttribute(_localctx.Builder, $implementation.Value);})*
		{_localctx.Value = Actions.CreateMethodHeader(
			_localctx,
			_localctx.Builder,
			_localctx.InitialSyntaxErrorCount,
			$convention.Value,
			$returnAttributes.Value,
			$returnType.Value,
			$returnMarshalling.Value,
			$name.Value,
			$genericParameters.Value,
			$arguments.Value);}
;

methAttr returns [CILParser.AttributeValue<System.Reflection.MethodAttributes> Value]
@init {_localctx.Value = CILParser.AttributeValue<System.Reflection.MethodAttributes>.Empty;}
:
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

pinvImpl returns [CILParser.PInvokeValue Value]
locals [CILParser.PInvokeBuilder Builder]
@init {_localctx.Builder = new CILParser.PInvokeBuilder();}
:
	'pinvokeimpl' '('
		(module = compQstring {Actions.SetPInvokeModule(_localctx.Builder, $module.Value);}
			('as' entryPoint = compQstring {Actions.SetPInvokeEntryPoint(_localctx.Builder, $entryPoint.Value);})?)?
		(attribute = pinvAttr {Actions.AddPInvokeAttribute(_localctx.Builder, $attribute.Value);})*
	')'
	| 'pinvokeimpl' '()'
;
finally {_localctx.Value = Actions.CreatePInvoke(_localctx.Builder);}

pinvAttr returns [CILParser.AttributeValue<System.Reflection.MethodImportAttributes> Value]
@init {_localctx.Value = CILParser.AttributeValue<System.Reflection.MethodImportAttributes>.Empty;}
:
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

methodName returns [string Value]
@init {_localctx.Value = string.Empty;}
:
	ctorName = '.ctor' {_localctx.Value = Actions.GetMethodName($ctorName);}
	| cctorName = '.cctor' {_localctx.Value = Actions.GetMethodName($cctorName);}
	| dotted = dottedName {_localctx.Value = $dotted.Value;};

implAttr returns [CILParser.AttributeValue<System.Reflection.MethodImplAttributes> Value]
@init {_localctx.Value = CILParser.AttributeValue<System.Reflection.MethodImplAttributes>.Empty;}
:
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
:
    methodDecl*
;

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
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	LOCALS initialize = 'init'? arguments = sigArgs
;
finally {Actions.EndLocalsDirective(_localctx, _localctx.InitialSyntaxErrorCount);}

exportDecl
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	EXPORT '[' ordinal = int32 ']' ('as' alias = id)?
;
finally {Actions.EndExportDirective(_localctx, _localctx.InitialSyntaxErrorCount);}

vtentryDecl
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	VTENTRY table = int32 ':' slot = int32
;
finally {Actions.EndVTableEntryDirective(_localctx, _localctx.InitialSyntaxErrorCount);}

overrideDecl
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	OVERRIDE owner = typeSpec '::' name = methodName
	| OVERRIDE 'method' convention = callConv returnType = type owner = typeSpec '::' name = methodName
		arity = genArity arguments = sigArgs
;
finally {Actions.EndOverrideDirective(_localctx, _localctx.InitialSyntaxErrorCount);}

parameterDecl
locals [
	int InitialSyntaxErrorCount,
	System.Collections.Immutable.ImmutableArray<CILParser.CustomAttributeApplicationValue>.Builder Attributes
]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Attributes = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.CustomAttributeApplicationValue>();
}
:
	PARAM TYPE '[' genericIndex = int32 ']' (attribute = customAttrDecl {Actions.AddCustomAttributeApplication(_localctx.Attributes, $attribute.ctx);})*
	| PARAM TYPE genericName = dottedName (attribute = customAttrDecl {Actions.AddCustomAttributeApplication(_localctx.Attributes, $attribute.ctx);})*
	| PARAM CONSTRAINT '[' constraintIndex = int32 ']' ',' constraintType = typeSpec
		(attribute = customAttrDecl {Actions.AddCustomAttributeApplication(_localctx.Attributes, $attribute.ctx);})*
	| PARAM CONSTRAINT constraintName = dottedName ',' constraintType = typeSpec
		(attribute = customAttrDecl {Actions.AddCustomAttributeApplication(_localctx.Attributes, $attribute.ctx);})*
	| PARAM '[' parameterIndex = int32 ']' initializer = initOpt
		(attribute = customAttrDecl {Actions.AddCustomAttributeApplication(_localctx.Attributes, $attribute.ctx);})*
;
finally {Actions.EndParameterDirective(
	_localctx,
	_localctx.Attributes.ToImmutable(),
	_localctx.InitialSyntaxErrorCount);}

labelDecl:
	name = id ':' {Actions.DefineLabel($name.start);};

customDescrInMethodBody returns [CILParser.CustomAttributeDeclarationValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.CustomAttributeDeclarationValue.Error;
}
:
	directAttribute = customDescr {_localctx.Value = Actions.CreateCustomAttributeDeclaration($directAttribute.Value);}
	| ownedAttribute = customDescrWithOwner {_localctx.Value = Actions.CreateCustomAttributeDeclaration($ownedAttribute.Value);};
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

scopeBlock
@init {Actions.BeginScope(_localctx);}
:
	'{' methodDecls '}'
;
finally {Actions.EndScope(_localctx);}

/* Structured exception handling directives  */
sehBlock
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	tryRange = tryBlock clauses = sehClauses
;
finally {Actions.EndExceptionBlock(_localctx, _localctx.InitialSyntaxErrorCount);}

sehClauses returns [System.Collections.Immutable.ImmutableArray<CILParser.ExceptionClauseValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.ExceptionClauseValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.ExceptionClauseValue>();}
:
	(clause = sehClause {_localctx.Builder.Add($clause.Value);})+
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

tryBlock returns [CILParser.ExceptionRangeValue Value]
@init {_localctx.Value = CILParser.ExceptionRangeValue.Invalid;}
:
	'.try' body = scopeBlock {_localctx.Value = Actions.CreateScopeExceptionRange($body.ctx);}
	| '.try' startLabel = id 'to' endLabel = id
		{_localctx.Value = Actions.CreateLabelExceptionRange($startLabel.start, $endLabel.start);}
	| '.try' startOffset = int32 'to' endOffset = int32
		{_localctx.Value = Actions.CreateOffsetExceptionRange($startOffset.start, $endOffset.start);};

sehClause returns [CILParser.ExceptionClauseValue Value]
@init {_localctx.Value = CILParser.ExceptionClauseValue.Invalid;}
:
	caught = catchClause handler = handlerBlock
		{_localctx.Value = Actions.CreateCatchExceptionClause($caught.Value, $handler.Value);}
	| filtered = filterClause handler = handlerBlock
		{_localctx.Value = Actions.CreateFilterExceptionClause($filtered.Value, $handler.Value);}
	| finallyClause handler = handlerBlock
		{_localctx.Value = Actions.CreateFinallyExceptionClause($handler.Value);}
	| faultClause handler = handlerBlock
		{_localctx.Value = Actions.CreateFaultExceptionClause($handler.Value);};

filterClause returns [CILParser.ExceptionFilterValue Value]
@init {_localctx.Value = CILParser.ExceptionFilterValue.Invalid;}
:
	'filter' body = scopeBlock {_localctx.Value = Actions.CreateScopeFilter($body.ctx);}
	| 'filter' label = id {_localctx.Value = Actions.CreateLabelFilter($label.start);}
	| 'filter' offset = int32 {_localctx.Value = Actions.CreateOffsetFilter($offset.start);};

catchClause
returns [CILParser.CatchTypeValue Value]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.CatchTypeValue.Invalid;
}
:
	'catch' catchType = typeSpec
;
finally {_localctx.Value = Actions.EndCatchClause(_localctx, _localctx.InitialSyntaxErrorCount);}

finallyClause: 'finally';

faultClause: 'fault';

handlerBlock returns [CILParser.ExceptionRangeValue Value]
@init {_localctx.Value = CILParser.ExceptionRangeValue.Invalid;}
:
	body = scopeBlock {_localctx.Value = Actions.CreateScopeExceptionRange($body.ctx);}
	| 'handler' startLabel = id 'to' endLabel = id
		{_localctx.Value = Actions.CreateLabelExceptionRange($startLabel.start, $endLabel.start);}
	| 'handler' startOffset = int32 'to' endOffset = int32
		{_localctx.Value = Actions.CreateOffsetExceptionRange($startOffset.start, $endOffset.start);};

/*  Data declaration  */
dataDecl returns [bool HasSyntaxError]
locals [int InitialSyntaxErrorCount, CILParser.DataDeclarationBuilder Builder]
@init {
	_localctx.Builder = Actions.CreateDataDeclaration(_localctx);
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
}
:
	ddHead[_localctx.Builder] ddBody[_localctx.Builder]
;
finally {Actions.EndDataDeclaration(_localctx, _localctx.Builder, _localctx.InitialSyntaxErrorCount);}

ddHead [CILParser.DataDeclarationBuilder Builder]:
	'.data' section = tls name = id '='
		{Actions.SetDataDeclarationHeader($Builder, $section.Value, $name.start);}
	| '.data' section = tls
		{Actions.SetAnonymousDataDeclarationHeader($Builder, $section.Value);};

tls returns [byte Value]
@init {_localctx.Value = Actions.GetMappedDataSection();}
:
	/* EMPTY */
	| 'tls' {_localctx.Value = Actions.GetTlsDataSection(_localctx);}
	| 'cil' {_localctx.Value = Actions.GetCilDataSection();};

ddBody [CILParser.DataDeclarationBuilder Builder]:
	'{' ddItemList[$Builder] '}' | ddItem[$Builder]+;

ddItemList [CILParser.DataDeclarationBuilder Builder]:
	(ddItem[$Builder] ',')* ddItem[$Builder];

ddItemCount returns [int Value]
@init {_localctx.Value = 1;}
:
	/* EMPTY */
	| '[' count = int32 ']' {_localctx.Value = Actions.ParseDataItemCount($count.start);};

ddItem [CILParser.DataDeclarationBuilder Builder]:
	CHAR PTR '(' stringValue = compQstring ')' {Actions.AddDataString($Builder, $stringValue.Value);}
	| REF '(' target = id ')' {Actions.AddDataReference($Builder, $target.start);}
	| REF target = id {Actions.AddDataReference($Builder, $target.start);}
	| 'bytearray' '(' byteValue = bytes ')' {Actions.AddDataBytes($Builder, $byteValue.Value);}
	| kind = (FLOAT32 | FLOAT64_) '(' floatingValue = float64 ')' count = ddItemCount
		{Actions.AddFloatingPointData($Builder, $kind, $floatingValue.Value, $count.Value);}
	| kind = INT64_ '(' int64Value = int64 ')' count = ddItemCount
		{Actions.AddInt64Data($Builder, $kind, $int64Value.start, $count.Value);}
	| kind = (INT32_ | INT16 | INT8) '(' integerValue = int32 ')' count = ddItemCount
		{Actions.AddIntegerData($Builder, $kind, $integerValue.start, $count.Value);}
	| kind = (FLOAT32 | FLOAT64_ | INT64_ | INT32_ | INT16 | INT8) count = ddItemCount
		{Actions.AddZeroData($Builder, $kind, $count.Value);};

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
locals [System.Collections.Immutable.ImmutableArray<byte>.Builder Builder]
@init {_localctx.Builder = Actions.CreateByteAccumulator();}
:
	(b = hexbyte {Actions.AddByte(_localctx.Builder, $b.Value);})*
;
finally {_localctx.Value = Actions.EndBytes(_localctx.Builder);}

hexbyte
returns [byte Value]
@after {_localctx.Value = GrammarActions.ParseHexbyte(_localctx.Start);}
:
	INT32
	| ID
	| HEXBYTE
;
/*  Field/parameter initialization  */
fieldInit returns [CILParser.FieldInitializerValue Value]
@init {_localctx.Value = CILParser.FieldInitializerValue.Empty;}
:
	serializedValue = fieldSerInit {_localctx.Value = Actions.CreateFieldInitializer($serializedValue.Value);}
	| stringValue = compQstring {_localctx.Value = Actions.CreateFieldInitializer($stringValue.Value);}
	| NULLREF {_localctx.Value = Actions.CreateNullFieldInitializer();};

/*  Values for verbal form of CA blob description  */
serInit returns [CILParser.SerializedInitializerValue Value]
@init {_localctx.Value = CILParser.SerializedInitializerValue.Error;}
:
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
locals [System.Reflection.Metadata.BlobBuilder Builder]
@init {_localctx.Builder = new System.Reflection.Metadata.BlobBuilder();}
:
	(floatingValue = float64 {Actions.AddFloat32SequenceValue(_localctx.Builder, $floatingValue.Value);}
	| integerValue = int32 {Actions.AddFloat32SequenceValue(_localctx.Builder, $integerValue.start);})*
;
finally {_localctx.Value = _localctx.Builder;}

f64seq returns [System.Reflection.Metadata.BlobBuilder Value]
locals [System.Reflection.Metadata.BlobBuilder Builder]
@init {_localctx.Builder = new System.Reflection.Metadata.BlobBuilder();}
:
	(floatingValue = float64 {Actions.AddFloat64SequenceValue(_localctx.Builder, $floatingValue.Value);}
	| integerValue = int64 {Actions.AddFloat64SequenceValue(_localctx.Builder, $integerValue.start);})*
;
finally {_localctx.Value = _localctx.Builder;}

i64seq returns [System.Reflection.Metadata.BlobBuilder Value]
locals [System.Reflection.Metadata.BlobBuilder Builder]
@init {_localctx.Builder = new System.Reflection.Metadata.BlobBuilder();}
:
	(value = int64 {Actions.AddInt64SequenceValue(_localctx.Builder, $value.start);})*
;
finally {_localctx.Value = _localctx.Builder;}

i32seq returns [System.Reflection.Metadata.BlobBuilder Value]
locals [System.Reflection.Metadata.BlobBuilder Builder]
@init {_localctx.Builder = new System.Reflection.Metadata.BlobBuilder();}
:
	(value = int32 {Actions.AddInt32SequenceValue(_localctx.Builder, $value.start);})*
;
finally {_localctx.Value = _localctx.Builder;}

i16seq returns [System.Reflection.Metadata.BlobBuilder Value]
locals [System.Reflection.Metadata.BlobBuilder Builder]
@init {_localctx.Builder = new System.Reflection.Metadata.BlobBuilder();}
:
	(value = int32 {Actions.AddInt16SequenceValue(_localctx.Builder, $value.start);})*
;
finally {_localctx.Value = _localctx.Builder;}

i8seq returns [System.Reflection.Metadata.BlobBuilder Value]
locals [System.Reflection.Metadata.BlobBuilder Builder]
@init {_localctx.Builder = new System.Reflection.Metadata.BlobBuilder();}
:
	(value = int32 {Actions.AddInt8SequenceValue(_localctx.Builder, $value.start);})*
;
finally {_localctx.Value = _localctx.Builder;}

boolSeq returns [System.Reflection.Metadata.BlobBuilder Value]
locals [System.Reflection.Metadata.BlobBuilder Builder]
@init {_localctx.Builder = new System.Reflection.Metadata.BlobBuilder();}
:
	(value = truefalse {Actions.AddBooleanSequenceValue(_localctx.Builder, $value.Value);})*
;
finally {_localctx.Value = _localctx.Builder;}

sqstringSeq returns [System.Reflection.Metadata.BlobBuilder Value]
locals [System.Reflection.Metadata.BlobBuilder Builder]
@init {_localctx.Builder = new System.Reflection.Metadata.BlobBuilder();}
:
	(nullValue = NULLREF {Actions.AddStringSequenceValue(_localctx.Builder, $nullValue);}
	| stringValue = SQSTRING {Actions.AddStringSequenceValue(_localctx.Builder, $stringValue);})*
;
finally {_localctx.Value = _localctx.Builder;}

classSeq returns [CILParser.SerializedSequenceValue Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.ClassSequenceElementValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.ClassSequenceElementValue>();}
:
	(value = classSeqElement {_localctx.Builder.Add($value.Value);})*
;
finally {_localctx.Value = new CILParser.ClassSerializedSequenceValue(_localctx.Builder.ToImmutable());}

classSeqElement returns [CILParser.ClassSequenceElementValue Value]
@init {_localctx.Value = CILParser.ClassSequenceElementValue.Error;}
:
	NULLREF {_localctx.Value = Actions.CreateNullClassSequenceValue();}
	| 'class' quotedValue = SQSTRING {_localctx.Value = Actions.CreateQuotedClassSequenceValue($quotedValue);}
	| typeValue = className {_localctx.Value = Actions.CreateClassSequenceValue($typeValue.Value);};

objSeq returns [CILParser.SerializedSequenceValue Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.SerializedInitializerValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.SerializedInitializerValue>();}
:
	(value = serInit {_localctx.Builder.Add($value.Value);})*
;
finally {_localctx.Value = new CILParser.ObjectSerializedSequenceValue(_localctx.Builder.ToImmutable());}

customAttrDecl returns [CILParser.CustomAttributeDeclarationValue Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {
	_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;
	_localctx.Value = CILParser.CustomAttributeDeclarationValue.Error;
}
:
	directAttribute = customDescr {_localctx.Value = Actions.CreateCustomAttributeDeclaration($directAttribute.Value);}
	| ownedAttribute = customDescrWithOwner {_localctx.Value = Actions.CreateCustomAttributeDeclaration($ownedAttribute.Value);}
	| alias = dottedName {_localctx.Value = Actions.CreateCustomAttributeTypedef($alias.Value);};
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
}

/* Assembly References */
asmOrRefDecl returns [CILParser.AssemblyDeclarationValue? Value]:
	('.publickey' | '.publicKey') '=' '(' key = bytes ')'
		{_localctx.Value = Actions.CreateAssemblyPublicKeyDeclaration($key.Value);}
	| '.ver' major = intOrWildcard ':' minor = intOrWildcard ':' build = intOrWildcard ':' revision = intOrWildcard
		{_localctx.Value = Actions.CreateAssemblyVersionDeclaration(
			$major.Value,
			$minor.Value,
			$build.Value,
			$revision.Value);}
	| '.locale' locale = compQstring
		{_localctx.Value = Actions.CreateAssemblyLocaleDeclaration($locale.Value);}
	| '.locale' '=' '(' localeBytes = bytes ')'
		{_localctx.Value = Actions.CreateAssemblyLocaleDeclaration($localeBytes.Value);}
	| attribute = customAttrDecl
		{_localctx.Value = Actions.CreateAssemblyCustomAttributeDeclaration(
			$attribute.Value,
			$attribute.start);}
	| compControl;

assemblyRefBlock returns [CILParser.AssemblyReferenceValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	header = assemblyRefHead '{' declarations = assemblyRefDecls '}'
		{_localctx.Value = Actions.CreateAssemblyReference(
			$header.Value,
			$declarations.Value);}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
	if (_localctx.HasSyntaxError)
	{
		_localctx.Value = null;
	}
}

assemblyRefHead returns [CILParser.AssemblyReferenceHeaderValue Value]
@init {_localctx.Value = CILParser.AssemblyReferenceHeaderValue.Error;}
:
	'.assembly' 'extern' attributes = asmAttr name = dottedName
		{_localctx.Value = Actions.CreateAssemblyReferenceHeader(
			$attributes.Value,
			$name.Value,
			$name.Value);}
	| '.assembly' 'extern' attributes = asmAttr name = dottedName 'as' alias = dottedName
		{_localctx.Value = Actions.CreateAssemblyReferenceHeader(
			$attributes.Value,
			$name.Value,
			$alias.Value);};

assemblyRefDecls returns [System.Collections.Immutable.ImmutableArray<CILParser.AssemblyDeclarationValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.AssemblyDeclarationValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.AssemblyDeclarationValue>();}
:
	(declaration = assemblyRefDecl
		{if ($declaration.Value is not null) _localctx.Builder.Add($declaration.Value);})*
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

assemblyRefDecl returns [CILParser.AssemblyDeclarationValue? Value]:
	'.hash' '=' '(' hash = bytes ')'
		{_localctx.Value = Actions.CreateAssemblyReferenceHashDeclaration($hash.Value);}
	| shared = asmOrRefDecl {_localctx.Value = $shared.Value;}
	| '.publickeytoken' '=' '(' token = bytes ')'
		{_localctx.Value = Actions.CreateAssemblyReferencePublicKeyTokenDeclaration($token.Value);}
	| 'auto' {_localctx.Value = Actions.CreateAssemblyReferenceAutoDeclaration();};

exptypeBlock returns [CILParser.ExportedTypeValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	header = exptypeHead '{' declarations = exptypeDecls '}'
		{_localctx.Value = Actions.CreateExportedType(
			$header.Value,
			$declarations.Value);}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
	if (_localctx.HasSyntaxError)
	{
		_localctx.Value = null;
	}
}

exptypeHead returns [CILParser.ExportedTypeHeaderValue Value]
@init {_localctx.Value = CILParser.ExportedTypeHeaderValue.Error;}
:
	head = '.class' 'extern' attributes = exptAttrs name = dottedName
		{_localctx.Value = Actions.CreateExportedTypeHeader(
			$attributes.Value,
			$name.Value,
			$head);};

exportHead returns [CILParser.ExportedTypeHeaderValue Value]
@init {_localctx.Value = CILParser.ExportedTypeHeaderValue.Error;}
:
	head = '.export' attributes = exptAttrs name = dottedName
		{_localctx.Value = Actions.CreateExportedTypeHeader(
			$attributes.Value,
			$name.Value,
			$head);};

exptAttrs returns [System.Reflection.TypeAttributes Value]
@init {_localctx.Value = 0;}
:
	(attribute = exptAttr
		{_localctx.Value = Actions.AddExportedTypeAttribute(
			_localctx.Value,
			$attribute.Value,
			$attribute.Mask);})*
;

exptAttr returns [System.Reflection.TypeAttributes Value, System.Reflection.TypeAttributes Mask]
@after {Actions.SetExportedTypeAttribute(_localctx);}
:
	'private'
	| 'public'
	| 'forwarder'
	| 'nested' 'public'
	| 'nested' 'private'
	| 'nested' 'family'
	| 'nested' 'assembly'
	| 'nested' 'famandassem'
	| 'nested' 'famorassem';

exptypeDecls returns [System.Collections.Immutable.ImmutableArray<CILParser.ExportedTypeDeclarationValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.ExportedTypeDeclarationValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.ExportedTypeDeclarationValue>();}
:
	(declaration = exptypeDecl
		{if ($declaration.Value is not null) _localctx.Builder.Add($declaration.Value);})*
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

exptypeDecl returns [CILParser.ExportedTypeDeclarationValue? Value]:
	location = '.file' name = dottedName
		{_localctx.Value = Actions.CreateExportedTypeFileDeclaration(
			$name.Value,
			$location);}
	| location = '.class' 'extern' nestedName = slashedName
		{_localctx.Value = Actions.CreateNestedExportedTypeDeclaration(
			$nestedName.Value,
			$location);}
	| location = '.assembly' 'extern' assemblyName = dottedName
		{_localctx.Value = Actions.CreateExportedTypeAssemblyDeclaration(
			$assemblyName.Value,
			$location);}
	| token = mdtoken
		{_localctx.Value = Actions.CreateExportedTypeMetadataTokenDeclaration(
			$token.Value,
			$token.start);}
	| '.class' typeDefinitionId = int32
		{_localctx.Value = Actions.CreateExportedTypeDefinitionIdDeclaration(
			$typeDefinitionId.start);}
	| attribute = customAttrDecl
		{_localctx.Value = Actions.CreateExportedTypeCustomAttributeDeclaration(
			$attribute.Value,
			$attribute.start);}
	| compControl;

manifestResBlock returns [CILParser.ManifestResourceValue? Value, bool HasSyntaxError]
locals [int InitialSyntaxErrorCount]
@init {_localctx.InitialSyntaxErrorCount = Actions.SyntaxErrorCount;}
:
	header = manifestResHead '{' declarations = manifestResDecls '}'
		{_localctx.Value = Actions.CreateManifestResource(
			$header.Value,
			$declarations.Value);}
;
finally {
	_localctx.HasSyntaxError =
		Actions.HasSyntaxErrorsSince(_localctx.InitialSyntaxErrorCount) ||
		_localctx.exception is not null;
	if (_localctx.HasSyntaxError)
	{
		_localctx.Value = null;
	}
}

manifestResHead returns [CILParser.ManifestResourceHeaderValue Value]
@init {_localctx.Value = CILParser.ManifestResourceHeaderValue.Error;}
:
	head = MRESOURCE attributes = manresAttrs name = dottedName
		{_localctx.Value = Actions.CreateManifestResourceHeader(
			$attributes.Value,
			$name.Value,
			$name.Value,
			$head);}
	| head = MRESOURCE attributes = manresAttrs name = dottedName 'as' alias = dottedName
		{_localctx.Value = Actions.CreateManifestResourceHeader(
			$attributes.Value,
			$name.Value,
			$alias.Value,
			$head);};

manresAttrs returns [System.Reflection.ManifestResourceAttributes Value]
@init {_localctx.Value = 0;}
:
	(attribute = manresAttr
		{_localctx.Value = Actions.AddManifestResourceAttribute(
			_localctx.Value,
			$attribute.Value);})*
;

manresAttr returns [System.Reflection.ManifestResourceAttributes Value]
@after {_localctx.Value = Actions.ParseManifestResourceAttribute(_localctx.Start);}
:
	'public'
	| 'private';

manifestResDecls returns [System.Collections.Immutable.ImmutableArray<CILParser.ManifestResourceDeclarationValue> Value]
locals [System.Collections.Immutable.ImmutableArray<CILParser.ManifestResourceDeclarationValue>.Builder Builder]
@init {_localctx.Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CILParser.ManifestResourceDeclarationValue>();}
:
	(declaration = manifestResDecl
		{if ($declaration.Value is not null) _localctx.Builder.Add($declaration.Value);})*
;
finally {_localctx.Value = _localctx.Builder.ToImmutable();}

manifestResDecl returns [CILParser.ManifestResourceDeclarationValue? Value]:
	location = '.file' name = dottedName 'at' offset = int32
		{_localctx.Value = Actions.CreateManifestResourceFileDeclaration(
			$name.Value,
			$offset.start,
			$location);}
	| '.assembly' 'extern' name = dottedName
		{_localctx.Value = Actions.CreateManifestResourceAssemblyDeclaration($name.Value);}
	| attribute = customAttrDecl
		{_localctx.Value = Actions.CreateManifestResourceCustomAttributeDeclaration(
			$attribute.Value,
			$attribute.start);}
	| compControl;
