// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Syntax
{
    internal enum PredefinedType : uint
    {
        PT_BYTE,
        PT_SHORT,
        PT_INT,
        PT_LONG,
        PT_FLOAT,
        PT_DOUBLE,
        PT_DECIMAL,
        PT_CHAR,
        PT_BOOL,

        // "simple" types are certain types that the compiler knows about for conversion and operator purposes.ses.
        // Keep these first so that we can build conversion tables on their ordinals... Don't change the orderder
        // of the simple types because it will mess up conversion tables.
        // The following Quasi-Simple types are considered simple, except they are non-CLS compliant
        PT_SBYTE,
        PT_USHORT,
        PT_UINT,
        PT_ULONG,

        // The special "pointer-sized int" types. Note that this are not considered numeric types from the compiler's point of view --
        // they are special only in that they have special signature encodings.  
        PT_INTPTR,
        PT_UINTPTR,

        PT_OBJECT,

        // THE ORDER ABOVE HERE IS IMPORTANT!!!  It is used in tables in both fncbind and ilgen
        PT_STRING,
        PT_DELEGATE,
        PT_MULTIDEL,
        PT_ARRAY,
        PT_EXCEPTION,
        PT_TYPE,
        PT_MONITOR,
        PT_VALUE,
        PT_ENUM,
        PT_DATETIME,

        // predefined attribute types
        PT_DEBUGGABLEATTRIBUTE,
        PT_DEBUGGABLEATTRIBUTE_DEBUGGINGMODES,
        PT_IN,
        PT_OUT,
        PT_ATTRIBUTE,
        PT_ATTRIBUTEUSAGE,
        PT_ATTRIBUTETARGETS,
        PT_OBSOLETE,
        PT_CONDITIONAL,
        PT_CLSCOMPLIANT,
        PT_GUID,
        PT_DEFAULTMEMBER,
        PT_PARAMS,
        PT_COMIMPORT,
        PT_FIELDOFFSET,
        PT_STRUCTLAYOUT,
        PT_LAYOUTKIND,
        PT_MARSHALAS,
        PT_DLLIMPORT,
        PT_INDEXERNAME,
        PT_DECIMALCONSTANT,
        PT_DEFAULTVALUE,
        PT_UNMANAGEDFUNCTIONPOINTER,
        PT_CALLINGCONVENTION,
        PT_CHARSET,

        // predefined types for the BCL
        PT_TYPEHANDLE,
        PT_FIELDHANDLE,
        PT_METHODHANDLE,
        PT_G_DICTIONARY,
        PT_IASYNCRESULT,
        PT_ASYNCCBDEL,
        PT_IDISPOSABLE,
        PT_IENUMERABLE,
        PT_IENUMERATOR,
        PT_SYSTEMVOID,
        PT_RUNTIMEHELPERS,

        // signature MODIFIER for marking volatile fields
        PT_VOLATILEMOD,

        // Sets the CoClass for a COM interface wrapper
        PT_COCLASS,

        // For instantiating a type variable.
        PT_ACTIVATOR,

        // Generic variants of enumerator interfaces
        PT_G_IENUMERABLE,
        PT_G_IENUMERATOR,

        // Nullable<T>
        PT_G_OPTIONAL,

        // Marks a fixed buffer field
        PT_FIXEDBUFFER,

        // Sets the module-level default character set marshalling
        PT_DEFAULTCHARSET,

        // Used to disable string interning
        PT_COMPILATIONRELAXATIONS,

        // Used to enable wrapped exceptions
        PT_RUNTIMECOMPATIBILITY,

        // Used for friend assmeblies
        PT_FRIENDASSEMBLY,

        // Used to hide compiler-generated code from the debugger
        PT_DEBUGGERHIDDEN,

        // Used for type forwarders
        PT_TYPEFORWARDER,

        // Used to warn on usage of this instead of command-line options
        PT_KEYFILE,
        PT_KEYNAME,
        PT_DELAYSIGN,
        PT_NOTSUPPORTEDEXCEPTION,
        PT_COMPILERGENERATED,

        PT_UNSAFEVALUETYPE,

        // special assembly identity attributes
        PT_ASSEMBLYFLAGS,
        PT_ASSEMBLYVERSION,
        PT_ASSEMBLYCULTURE,

        // LINQ
        PT_G_IQUERYABLE,
        PT_IQUERYABLE,
        PT_STRINGBUILDER,
        PT_G_ICOLLECTION,
        PT_G_ILIST,
        PT_EXTENSION,
        PT_G_EXPRESSION,
        PT_EXPRESSION,
        PT_LAMBDAEXPRESSION,
        PT_BINARYEXPRESSION,
        PT_UNARYEXPRESSION,
        PT_CONDITIONALEXPRESSION,
        PT_CONSTANTEXPRESSION,
        PT_PARAMETEREXPRESSION,
        PT_MEMBEREXPRESSION,
        PT_METHODCALLEXPRESSION,
        PT_NEWEXPRESSION,
        PT_BINDING,
        PT_MEMBERINITEXPRESSION,
        PT_LISTINITEXPRESSION,
        PT_TYPEBINARYEXPRESSION,
        PT_NEWARRAYEXPRESSION,
        PT_MEMBERASSIGNMENT,
        PT_MEMBERLISTBINDING,
        PT_MEMBERMEMBERBINDING,
        PT_INVOCATIONEXPRESSION,
        PT_FIELDINFO,
        PT_METHODINFO,
        PT_CONSTRUCTORINFO,
        PT_PROPERTYINFO,
        PT_METHODBASE,
        PT_MEMBERINFO,

        PT_DEBUGGERDISPLAY,
        PT_DEBUGGERBROWSABLE,
        PT_DEBUGGERBROWSABLESTATE,
        PT_G_EQUALITYCOMPARER,
        PT_ELEMENTINITIALIZER,

        PT_MISSING,

        PT_G_IREADONLYLIST,
        PT_G_IREADONLYCOLLECTION,
        PT_COUNT,
        PT_VOID,             // (special case)

        PT_UNDEFINEDINDEX = 0xffffffff,
    }
}