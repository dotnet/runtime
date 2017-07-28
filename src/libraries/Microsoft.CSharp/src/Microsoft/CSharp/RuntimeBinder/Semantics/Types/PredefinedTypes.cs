// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class PredefinedTypes
    {
        private SymbolTable _runtimeBinderSymbolTable;
        private readonly BSYMMGR _symbolManager;
        private AggregateSymbol[] _predefSyms;    // array of predefined symbol types.

        public PredefinedTypes(BSYMMGR symbolManager)
        {
            _symbolManager = symbolManager;
            _runtimeBinderSymbolTable = null;
        }

        // We want to delay load the predefined symbols as needed.
        private AggregateSymbol DelayLoadPredefSym(PredefinedType pt)
        {
            CType type = _runtimeBinderSymbolTable.GetCTypeFromType(PredefinedTypeFacts.GetAssociatedSystemType(pt));
            AggregateSymbol sym = type.getAggregate();
            return InitializePredefinedType(sym, pt);
        }

        internal static AggregateSymbol InitializePredefinedType(AggregateSymbol sym, PredefinedType pt)
        {
            sym.SetPredefined(true);
            sym.SetPredefType(pt);
            sym.SetSkipUDOps(pt <= PredefinedType.PT_ENUM && pt != PredefinedType.PT_INTPTR && pt != PredefinedType.PT_UINTPTR && pt != PredefinedType.PT_TYPE);

            return sym;
        }

        public void Init(SymbolTable symtable)
        {
            _runtimeBinderSymbolTable = symtable;
            Debug.Assert(_symbolManager != null);
            Debug.Assert(_predefSyms == null);

            _predefSyms = new AggregateSymbol[(int)PredefinedType.PT_COUNT];
        }

        public AggregateSymbol GetPredefinedAggregate(PredefinedType pt) =>
            _predefSyms[(int)pt] ?? (_predefSyms[(int)pt] = DelayLoadPredefSym(pt));

        ////////////////////////////////////////////////////////////////////////////////
        // Some of the predefined types have built-in names, like "int" or "string" or
        // "object". This return the nice name if one exists; otherwise null is 
        // returned.

        private static string GetNiceName(PredefinedType pt) => PredefinedTypeFacts.GetNiceName(pt);

        public static string GetNiceName(AggregateSymbol type) =>
            type.IsPredefined() ? GetNiceName(type.GetPredefType()) : null;

        public static string GetFullName(PredefinedType pt) => PredefinedTypeFacts.GetName(pt);
    }

    internal static class PredefinedTypeFacts
    {
        internal static string GetName(PredefinedType type) => s_types[(int)type].Name;

        internal static FUNDTYPE GetFundType(PredefinedType type) => s_types[(int)type].FundType;

        internal static Type GetAssociatedSystemType(PredefinedType type) => s_types[(int)type].AssociatedSystemType;

        internal static bool IsSimpleType(PredefinedType type) => type < PredefinedType.FirstNonSimpleType;

        internal static bool IsNumericType(PredefinedType type)
        {
            switch (type)
            {
                case PredefinedType.PT_BYTE:
                case PredefinedType.PT_SHORT:
                case PredefinedType.PT_INT:
                case PredefinedType.PT_LONG:
                case PredefinedType.PT_FLOAT:
                case PredefinedType.PT_DOUBLE:
                case PredefinedType.PT_DECIMAL:
                case PredefinedType.PT_SBYTE:
                case PredefinedType.PT_USHORT:
                case PredefinedType.PT_UINT:
                case PredefinedType.PT_ULONG:
                    return true;
                default:
                    return false;
            }
        }

        internal static string GetNiceName(PredefinedType type)
        {
            switch (type)
            {
                case PredefinedType.PT_BYTE:
                    return "byte";
                case PredefinedType.PT_SHORT:
                    return "short";
                case PredefinedType.PT_INT:
                    return "int";
                case PredefinedType.PT_LONG:
                    return "long";
                case PredefinedType.PT_FLOAT:
                    return "float";
                case PredefinedType.PT_DOUBLE:
                    return "double";
                case PredefinedType.PT_DECIMAL:
                    return "decimal";
                case PredefinedType.PT_CHAR:
                    return "char";
                case PredefinedType.PT_BOOL:
                    return "bool";
                case PredefinedType.PT_SBYTE:
                    return "sbyte";
                case PredefinedType.PT_USHORT:
                    return "ushort";
                case PredefinedType.PT_UINT:
                    return "uint";
                case PredefinedType.PT_ULONG:
                    return "ulong";
                case PredefinedType.PT_OBJECT:
                    return "object";
                case PredefinedType.PT_STRING:
                    return "string";
                default:
                    return null;
            }
        }

        public static PredefinedType TryGetPredefTypeIndex(string name) =>
            s_typesByName.TryGetValue(name, out PredefinedType type) ? type : PredefinedType.PT_UNDEFINEDINDEX;

        private sealed class PredefinedTypeInfo
        {
#if DEBUG
            public readonly PredefinedType Type;
#endif
            public readonly string Name;
            public readonly FUNDTYPE FundType;
            public readonly Type AssociatedSystemType;

            internal PredefinedTypeInfo(PredefinedType type, Type associatedSystemType, string name, FUNDTYPE fundType)
            {
#if DEBUG
                Type = type;
#endif
                Name = name;
                FundType = fundType;
                AssociatedSystemType = associatedSystemType;
            }

            internal PredefinedTypeInfo(PredefinedType type, Type associatedSystemType, string name)
                : this(type, associatedSystemType, name, FUNDTYPE.FT_REF)
            {
            }
        }

        private static readonly PredefinedTypeInfo[] s_types = {
            new PredefinedTypeInfo(PredefinedType.PT_BYTE,   typeof(byte), "System.Byte", FUNDTYPE.FT_U1),
            new PredefinedTypeInfo(PredefinedType.PT_SHORT,  typeof(short), "System.Int16", FUNDTYPE.FT_I2),
            new PredefinedTypeInfo(PredefinedType.PT_INT,    typeof(int), "System.Int32", FUNDTYPE.FT_I4),
            new PredefinedTypeInfo(PredefinedType.PT_LONG,   typeof(long), "System.Int64", FUNDTYPE.FT_I8),
            new PredefinedTypeInfo(PredefinedType.PT_FLOAT,  typeof(float), "System.Single", FUNDTYPE.FT_R4),
            new PredefinedTypeInfo(PredefinedType.PT_DOUBLE, typeof(double), "System.Double", FUNDTYPE.FT_R8),
            new PredefinedTypeInfo(PredefinedType.PT_DECIMAL, typeof(decimal), "System.Decimal", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_CHAR,   typeof(char), "System.Char", FUNDTYPE.FT_U2),
            new PredefinedTypeInfo(PredefinedType.PT_BOOL,   typeof(bool), "System.Boolean", FUNDTYPE.FT_I1),
            new PredefinedTypeInfo(PredefinedType.PT_SBYTE,  typeof(sbyte), "System.SByte", FUNDTYPE.FT_I1),
            new PredefinedTypeInfo(PredefinedType.PT_USHORT, typeof(ushort), "System.UInt16", FUNDTYPE.FT_U2),
            new PredefinedTypeInfo(PredefinedType.PT_UINT,   typeof(uint), "System.UInt32", FUNDTYPE.FT_U4),
            new PredefinedTypeInfo(PredefinedType.PT_ULONG,  typeof(ulong), "System.UInt64", FUNDTYPE.FT_U8),
            new PredefinedTypeInfo(PredefinedType.PT_INTPTR,  typeof(IntPtr), "System.IntPtr", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_UINTPTR, typeof(UIntPtr), "System.UIntPtr", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_OBJECT, typeof(object), "System.Object"),
            new PredefinedTypeInfo(PredefinedType.PT_STRING, typeof(string), "System.String"),
            new PredefinedTypeInfo(PredefinedType.PT_DELEGATE, typeof(Delegate), "System.Delegate"),
            new PredefinedTypeInfo(PredefinedType.PT_MULTIDEL, typeof(MulticastDelegate), "System.MulticastDelegate"),
            new PredefinedTypeInfo(PredefinedType.PT_ARRAY,   typeof(Array), "System.Array"),
            new PredefinedTypeInfo(PredefinedType.PT_EXCEPTION, typeof(Exception), "System.Exception"),
            new PredefinedTypeInfo(PredefinedType.PT_TYPE, typeof(Type), "System.Type"),
            new PredefinedTypeInfo(PredefinedType.PT_MONITOR, typeof(System.Threading.Monitor), "System.Threading.Monitor"),
            new PredefinedTypeInfo(PredefinedType.PT_VALUE,   typeof(ValueType), "System.ValueType"),
            new PredefinedTypeInfo(PredefinedType.PT_ENUM,    typeof(Enum), "System.Enum"),
            new PredefinedTypeInfo(PredefinedType.PT_DATETIME,    typeof(DateTime), "System.DateTime", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGABLEATTRIBUTE, typeof(DebuggableAttribute), "System.Diagnostics.DebuggableAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGABLEATTRIBUTE_DEBUGGINGMODES, typeof(DebuggableAttribute.DebuggingModes), "System.Diagnostics.DebuggableAttribute.DebuggingModes"),
            new PredefinedTypeInfo(PredefinedType.PT_IN,            typeof(System.Runtime.InteropServices.InAttribute), "System.Runtime.InteropServices.InAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_OUT,           typeof(System.Runtime.InteropServices.OutAttribute), "System.Runtime.InteropServices.OutAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTE, typeof(Attribute), "System.Attribute"),
            new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTEUSAGE, typeof(AttributeUsageAttribute), "System.AttributeUsageAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_ATTRIBUTETARGETS, typeof(AttributeTargets), "System.AttributeTargets", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_OBSOLETE, typeof(ObsoleteAttribute), "System.ObsoleteAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_CONDITIONAL, typeof(ConditionalAttribute), "System.Diagnostics.ConditionalAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_CLSCOMPLIANT, typeof(CLSCompliantAttribute), "System.CLSCompliantAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_GUID, typeof(System.Runtime.InteropServices.GuidAttribute), "System.Runtime.InteropServices.GuidAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DEFAULTMEMBER, typeof(System.Reflection.DefaultMemberAttribute), "System.Reflection.DefaultMemberAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_PARAMS, typeof(ParamArrayAttribute), "System.ParamArrayAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_COMIMPORT, typeof(System.Runtime.InteropServices.ComImportAttribute), "System.Runtime.InteropServices.ComImportAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_FIELDOFFSET, typeof(System.Runtime.InteropServices.FieldOffsetAttribute), "System.Runtime.InteropServices.FieldOffsetAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_STRUCTLAYOUT, typeof(System.Runtime.InteropServices.StructLayoutAttribute), "System.Runtime.InteropServices.StructLayoutAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_LAYOUTKIND, typeof(System.Runtime.InteropServices.LayoutKind), "System.Runtime.InteropServices.LayoutKind", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_MARSHALAS, typeof(System.Runtime.InteropServices.MarshalAsAttribute), "System.Runtime.InteropServices.MarshalAsAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DLLIMPORT, typeof(System.Runtime.InteropServices.DllImportAttribute), "System.Runtime.InteropServices.DllImportAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_INDEXERNAME, typeof(System.Runtime.CompilerServices.IndexerNameAttribute), "System.Runtime.CompilerServices.IndexerNameAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DECIMALCONSTANT, typeof(System.Runtime.CompilerServices.DecimalConstantAttribute), "System.Runtime.CompilerServices.DecimalConstantAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DEFAULTVALUE, typeof(System.Runtime.InteropServices.DefaultParameterValueAttribute), "System.Runtime.InteropServices.DefaultParameterValueAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_UNMANAGEDFUNCTIONPOINTER, typeof(System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute), "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_CALLINGCONVENTION, typeof(System.Runtime.InteropServices.CallingConvention), "System.Runtime.InteropServices.CallingConvention", FUNDTYPE.FT_I4),
            new PredefinedTypeInfo(PredefinedType.PT_CHARSET, typeof(System.Runtime.InteropServices.CharSet), "System.Runtime.InteropServices.CharSet", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_TYPEHANDLE, typeof(RuntimeTypeHandle), "System.RuntimeTypeHandle", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_FIELDHANDLE, typeof(RuntimeFieldHandle), "System.RuntimeFieldHandle", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_METHODHANDLE, typeof(RuntimeMethodHandle), "System.RuntimeMethodHandle", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_G_DICTIONARY, typeof(Dictionary<,>), "System.Collections.Generic.Dictionary`2"),
            new PredefinedTypeInfo(PredefinedType.PT_IASYNCRESULT, typeof(IAsyncResult), "System.IAsyncResult"),
            new PredefinedTypeInfo(PredefinedType.PT_ASYNCCBDEL, typeof(AsyncCallback), "System.AsyncCallback"),
            new PredefinedTypeInfo(PredefinedType.PT_IDISPOSABLE, typeof(IDisposable), "System.IDisposable"),
            new PredefinedTypeInfo(PredefinedType.PT_IENUMERABLE, typeof(System.Collections.IEnumerable), "System.Collections.IEnumerable"),
            new PredefinedTypeInfo(PredefinedType.PT_IENUMERATOR, typeof(System.Collections.IEnumerator), "System.Collections.IEnumerator"),
            new PredefinedTypeInfo(PredefinedType.PT_SYSTEMVOID, typeof(void), "System.Void", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_RUNTIMEHELPERS, typeof(System.Runtime.CompilerServices.RuntimeHelpers), "System.Runtime.CompilerServices.RuntimeHelpers"),
            new PredefinedTypeInfo(PredefinedType.PT_VOLATILEMOD, typeof(System.Runtime.CompilerServices.IsVolatile), "System.Runtime.CompilerServices.IsVolatile"),
            new PredefinedTypeInfo(PredefinedType.PT_COCLASS,    typeof(System.Runtime.InteropServices.CoClassAttribute), "System.Runtime.InteropServices.CoClassAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_ACTIVATOR,  typeof(Activator), "System.Activator"),
            new PredefinedTypeInfo(PredefinedType.PT_G_IENUMERABLE, typeof(IEnumerable<>), "System.Collections.Generic.IEnumerable`1"),
            new PredefinedTypeInfo(PredefinedType.PT_G_IENUMERATOR, typeof(IEnumerator<>), "System.Collections.Generic.IEnumerator`1"),
            new PredefinedTypeInfo(PredefinedType.PT_G_OPTIONAL, typeof(Nullable<>), "System.Nullable`1", FUNDTYPE.FT_STRUCT),
            new PredefinedTypeInfo(PredefinedType.PT_FIXEDBUFFER, typeof(System.Runtime.CompilerServices.FixedBufferAttribute), "System.Runtime.CompilerServices.FixedBufferAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DEFAULTCHARSET, typeof(System.Runtime.InteropServices.DefaultCharSetAttribute), "System.Runtime.InteropServices.DefaultCharSetAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_COMPILATIONRELAXATIONS, typeof(System.Runtime.CompilerServices.CompilationRelaxationsAttribute), "System.Runtime.CompilerServices.CompilationRelaxationsAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_RUNTIMECOMPATIBILITY, typeof(System.Runtime.CompilerServices.RuntimeCompatibilityAttribute), "System.Runtime.CompilerServices.RuntimeCompatibilityAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_FRIENDASSEMBLY, typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute), "System.Runtime.CompilerServices.InternalsVisibleToAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERHIDDEN, typeof(DebuggerHiddenAttribute), "System.Diagnostics.DebuggerHiddenAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_TYPEFORWARDER, typeof(System.Runtime.CompilerServices.TypeForwardedToAttribute), "System.Runtime.CompilerServices.TypeForwardedToAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_KEYFILE, typeof(System.Reflection.AssemblyKeyFileAttribute), "System.Reflection.AssemblyKeyFileAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_KEYNAME, typeof(System.Reflection.AssemblyKeyNameAttribute), "System.Reflection.AssemblyKeyNameAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DELAYSIGN, typeof(System.Reflection.AssemblyDelaySignAttribute), "System.Reflection.AssemblyDelaySignAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_NOTSUPPORTEDEXCEPTION, typeof(NotSupportedException), "System.NotSupportedException"),
            new PredefinedTypeInfo(PredefinedType.PT_COMPILERGENERATED, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), "System.Runtime.CompilerServices.CompilerGeneratedAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_UNSAFEVALUETYPE, typeof(System.Runtime.CompilerServices.UnsafeValueTypeAttribute), "System.Runtime.CompilerServices.UnsafeValueTypeAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYFLAGS, typeof(System.Reflection.AssemblyFlagsAttribute), "System.Reflection.AssemblyFlagsAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYVERSION, typeof(System.Reflection.AssemblyVersionAttribute), "System.Reflection.AssemblyVersionAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_ASSEMBLYCULTURE, typeof(System.Reflection.AssemblyCultureAttribute), "System.Reflection.AssemblyCultureAttribute"),
            // LINQ
            new PredefinedTypeInfo(PredefinedType.PT_G_IQUERYABLE, typeof(System.Linq.IQueryable<>), "System.Linq.IQueryable`1"),
            new PredefinedTypeInfo(PredefinedType.PT_IQUERYABLE, typeof(System.Linq.IQueryable), "System.Linq.IQueryable"),
            new PredefinedTypeInfo(PredefinedType.PT_STRINGBUILDER, typeof(System.Text.StringBuilder), "System.Text.StringBuilder"),
            new PredefinedTypeInfo(PredefinedType.PT_G_ICOLLECTION, typeof(ICollection<>), "System.Collections.Generic.ICollection`1"),
            new PredefinedTypeInfo(PredefinedType.PT_G_ILIST, typeof(IList<>), "System.Collections.Generic.IList`1"),
            new PredefinedTypeInfo(PredefinedType.PT_EXTENSION, typeof(System.Runtime.CompilerServices.ExtensionAttribute), "System.Runtime.CompilerServices.ExtensionAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_G_EXPRESSION, typeof(System.Linq.Expressions.Expression<>), "System.Linq.Expressions.Expression`1"),
            new PredefinedTypeInfo(PredefinedType.PT_EXPRESSION, typeof(System.Linq.Expressions.Expression), "System.Linq.Expressions.Expression"),
            new PredefinedTypeInfo(PredefinedType.PT_LAMBDAEXPRESSION, typeof(System.Linq.Expressions.LambdaExpression), "System.Linq.Expressions.LambdaExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_BINARYEXPRESSION, typeof(System.Linq.Expressions.BinaryExpression), "System.Linq.Expressions.BinaryExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_UNARYEXPRESSION, typeof(System.Linq.Expressions.UnaryExpression), "System.Linq.Expressions.UnaryExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_CONDITIONALEXPRESSION, typeof(System.Linq.Expressions.ConditionalExpression), "System.Linq.Expressions.ConditionalExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_CONSTANTEXPRESSION, typeof(System.Linq.Expressions.ConstantExpression), "System.Linq.Expressions.ConstantExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_PARAMETEREXPRESSION, typeof(System.Linq.Expressions.ParameterExpression), "System.Linq.Expressions.ParameterExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBEREXPRESSION, typeof(System.Linq.Expressions.MemberExpression), "System.Linq.Expressions.MemberExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_METHODCALLEXPRESSION, typeof(System.Linq.Expressions.MethodCallExpression), "System.Linq.Expressions.MethodCallExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_NEWEXPRESSION, typeof(System.Linq.Expressions.NewExpression), "System.Linq.Expressions.NewExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_BINDING, typeof(System.Linq.Expressions.MemberBinding), "System.Linq.Expressions.MemberBinding"),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERINITEXPRESSION, typeof(System.Linq.Expressions.MemberInitExpression), "System.Linq.Expressions.MemberInitExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_LISTINITEXPRESSION, typeof(System.Linq.Expressions.ListInitExpression), "System.Linq.Expressions.ListInitExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_TYPEBINARYEXPRESSION, typeof(System.Linq.Expressions.TypeBinaryExpression), "System.Linq.Expressions.TypeBinaryExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_NEWARRAYEXPRESSION, typeof(System.Linq.Expressions.NewArrayExpression), "System.Linq.Expressions.NewArrayExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERASSIGNMENT, typeof(System.Linq.Expressions.MemberAssignment), "System.Linq.Expressions.MemberAssignment"),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERLISTBINDING, typeof(System.Linq.Expressions.MemberListBinding), "System.Linq.Expressions.MemberListBinding"),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERMEMBERBINDING, typeof(System.Linq.Expressions.MemberMemberBinding), "System.Linq.Expressions.MemberMemberBinding"),
            new PredefinedTypeInfo(PredefinedType.PT_INVOCATIONEXPRESSION, typeof(System.Linq.Expressions.InvocationExpression), "System.Linq.Expressions.InvocationExpression"),
            new PredefinedTypeInfo(PredefinedType.PT_FIELDINFO, typeof(System.Reflection.FieldInfo), "System.Reflection.FieldInfo"),
            new PredefinedTypeInfo(PredefinedType.PT_METHODINFO, typeof(System.Reflection.MethodInfo), "System.Reflection.MethodInfo"),
            new PredefinedTypeInfo(PredefinedType.PT_CONSTRUCTORINFO, typeof(System.Reflection.ConstructorInfo), "System.Reflection.ConstructorInfo"),
            new PredefinedTypeInfo(PredefinedType.PT_PROPERTYINFO, typeof(System.Reflection.PropertyInfo), "System.Reflection.PropertyInfo"),
            new PredefinedTypeInfo(PredefinedType.PT_METHODBASE, typeof(System.Reflection.MethodBase), "System.Reflection.MethodBase"),
            new PredefinedTypeInfo(PredefinedType.PT_MEMBERINFO, typeof(System.Reflection.MemberInfo), "System.Reflection.MemberInfo"),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERDISPLAY, typeof(DebuggerDisplayAttribute), "System.Diagnostics.DebuggerDisplayAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERBROWSABLE, typeof(DebuggerBrowsableAttribute), "System.Diagnostics.DebuggerBrowsableAttribute"),
            new PredefinedTypeInfo(PredefinedType.PT_DEBUGGERBROWSABLESTATE, typeof(DebuggerBrowsableState), "System.Diagnostics.DebuggerBrowsableState", FUNDTYPE.FT_I4),
            new PredefinedTypeInfo(PredefinedType.PT_G_EQUALITYCOMPARER, typeof(EqualityComparer<>), "System.Collections.Generic.EqualityComparer`1"),
            new PredefinedTypeInfo(PredefinedType.PT_ELEMENTINITIALIZER, typeof(System.Linq.Expressions.ElementInit), "System.Linq.Expressions.ElementInit"),

            new PredefinedTypeInfo(PredefinedType.PT_MISSING, typeof(System.Reflection.Missing), "System.Reflection.Missing"),
            new PredefinedTypeInfo(PredefinedType.PT_G_IREADONLYLIST, typeof(IReadOnlyList<>), "System.Collections.Generic.IReadOnlyList`1"),
            new PredefinedTypeInfo(PredefinedType.PT_G_IREADONLYCOLLECTION, typeof(IReadOnlyCollection<>), "System.Collections.Generic.IReadOnlyCollection`1"),
        };

        private static readonly Dictionary<string, PredefinedType> s_typesByName = CreatePredefinedTypeFacts();

        private static Dictionary<string, PredefinedType> CreatePredefinedTypeFacts()
        {
            var typesByName = new Dictionary<string, PredefinedType>((int)PredefinedType.PT_COUNT);
            for (int i = 0; i < (int)PredefinedType.PT_COUNT; i++)
            {
#if DEBUG
                Debug.Assert(s_types[i].Type == (PredefinedType)i);
#endif
                typesByName.Add(s_types[i].Name, (PredefinedType)i);
            }

            return typesByName;
        }
    }
}
