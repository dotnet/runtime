// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// RegexCompiler translates a block of RegexCode to MSIL, and creates a
    /// subclass of the RegexRunner type.
    /// </summary>
    internal abstract class RegexCompiler
    {
        private static readonly FieldInfo s_runtextbegField = RegexRunnerField("runtextbeg");
        private static readonly FieldInfo s_runtextendField = RegexRunnerField("runtextend");
        private static readonly FieldInfo s_runtextstartField = RegexRunnerField("runtextstart");
        private static readonly FieldInfo s_runtextposField = RegexRunnerField("runtextpos");
        private static readonly FieldInfo s_runtextField = RegexRunnerField("runtext");
        private static readonly FieldInfo s_runtrackposField = RegexRunnerField("runtrackpos");
        private static readonly FieldInfo s_runtrackField = RegexRunnerField("runtrack");
        private static readonly FieldInfo s_runstackposField = RegexRunnerField("runstackpos");
        private static readonly FieldInfo s_runstackField = RegexRunnerField("runstack");
        protected static readonly FieldInfo s_runtrackcountField = RegexRunnerField("runtrackcount");

        private static readonly MethodInfo s_doubleStackMethod = RegexRunnerMethod("DoubleStack");
        private static readonly MethodInfo s_doubleTrackMethod = RegexRunnerMethod("DoubleTrack");
        private static readonly MethodInfo s_captureMethod = RegexRunnerMethod("Capture");
        private static readonly MethodInfo s_transferCaptureMethod = RegexRunnerMethod("TransferCapture");
        private static readonly MethodInfo s_uncaptureMethod = RegexRunnerMethod("Uncapture");
        private static readonly MethodInfo s_isMatchedMethod = RegexRunnerMethod("IsMatched");
        private static readonly MethodInfo s_matchLengthMethod = RegexRunnerMethod("MatchLength");
        private static readonly MethodInfo s_matchIndexMethod = RegexRunnerMethod("MatchIndex");
        private static readonly MethodInfo s_isBoundaryMethod = RegexRunnerMethod("IsBoundary");
        private static readonly MethodInfo s_isECMABoundaryMethod = RegexRunnerMethod("IsECMABoundary");
        private static readonly MethodInfo s_crawlposMethod = RegexRunnerMethod("Crawlpos");
        private static readonly MethodInfo s_charInClassMethod = RegexRunnerMethod("CharInClass");
        private static readonly MethodInfo s_checkTimeoutMethod = RegexRunnerMethod("CheckTimeout");
#if DEBUG
        private static readonly MethodInfo s_dumpStateM = RegexRunnerMethod("DumpState");
#endif

        private static readonly MethodInfo s_charIsDigitMethod = typeof(char).GetMethod("IsDigit", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charIsWhiteSpaceMethod = typeof(char).GetMethod("IsWhiteSpace", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charGetUnicodeInfo = typeof(char).GetMethod("GetUnicodeCategory", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charToLowerInvariantMethod = typeof(char).GetMethod("ToLowerInvariant", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_cultureInfoGetCurrentCultureMethod = typeof(CultureInfo).GetMethod("get_CurrentCulture")!;
        private static readonly MethodInfo s_cultureInfoGetTextInfoMethod = typeof(CultureInfo).GetMethod("get_TextInfo")!;
#if DEBUG
        private static readonly MethodInfo s_debugWriteLine = typeof(Debug).GetMethod("WriteLine", new Type[] { typeof(string) })!;
#endif
        private static readonly MethodInfo s_spanGetItemMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Item", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_spanGetLengthMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Length")!;
        private static readonly MethodInfo s_memoryMarshalGetReference = typeof(MemoryMarshal).GetMethod("GetReference", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfChar = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfSpan = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnyCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnyCharCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnySpan = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanLastIndexOfChar = typeof(MemoryExtensions).GetMethod("LastIndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanLastIndexOfSpan = typeof(MemoryExtensions).GetMethod("LastIndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanSliceIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_spanSliceIntIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[] { typeof(int), typeof(int) })!;
        private static readonly MethodInfo s_spanStartsWith = typeof(MemoryExtensions).GetMethod("StartsWith", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_stringAsSpanMethod = typeof(MemoryExtensions).GetMethod("AsSpan", new Type[] { typeof(string) })!;
        private static readonly MethodInfo s_stringAsSpanIntIntMethod = typeof(MemoryExtensions).GetMethod("AsSpan", new Type[] { typeof(string), typeof(int), typeof(int) })!;
        private static readonly MethodInfo s_stringGetCharsMethod = typeof(string).GetMethod("get_Chars", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_stringIndexOfCharInt = typeof(string).GetMethod("IndexOf", new Type[] { typeof(char), typeof(int) })!;
        private static readonly MethodInfo s_stringLastIndexOfCharIntInt = typeof(string).GetMethod("LastIndexOf", new Type[] { typeof(char), typeof(int), typeof(int) })!;
        private static readonly MethodInfo s_textInfoToLowerMethod = typeof(TextInfo).GetMethod("ToLower", new Type[] { typeof(char) })!;

        protected ILGenerator? _ilg;

        // tokens representing local variables
        private LocalBuilder? _runtextbegLocal;
        private LocalBuilder? _runtextendLocal;
        private LocalBuilder? _runtextposLocal;
        private LocalBuilder? _runtextLocal;
        private LocalBuilder? _runtrackposLocal;
        private LocalBuilder? _runtrackLocal;
        private LocalBuilder? _runstackposLocal;
        private LocalBuilder? _runstackLocal;
        private LocalBuilder? _textInfoLocal;  // cached to avoid extraneous TLS hits from CurrentCulture and virtual calls to TextInfo
        private LocalBuilder? _loopTimeoutCounterLocal; // timeout counter for setrep and setloop

        protected RegexOptions _options;                                           // options
        protected RegexCode? _code;                                                // the RegexCode object
        protected int[]? _codes;                                                   // the RegexCodes being translated
        protected string[]? _strings;                                              // the stringtable associated with the RegexCodes
        protected bool _hasTimeout;                                                // whether the regex has a non-infinite timeout

        private Label[]? _labels;                                                  // a label for every operation in _codes
        private BacktrackNote[]? _notes;                                           // a list of the backtracking states to be generated
        private int _notecount;                                                    // true count of _notes (allocation grows exponentially)
        protected int _trackcount;                                                 // count of backtracking states (used to reduce allocations)
        private Label _backtrack;                                                  // label for backtracking
        private Stack<LocalBuilder>? _int32LocalsPool;                             // pool of Int32 local variables
        private Stack<LocalBuilder>? _readOnlySpanCharLocalsPool;                  // pool of ReadOnlySpan<char> local variables

        private int _regexopcode;             // the current opcode being processed
        private int _codepos;                 // the current code being translated
        private int _backpos;                 // the current backtrack-note being translated

        // special code fragments
        private int[]? _uniquenote;           // _notes indices for code that should be emitted <= once
        private int[]? _goto;                 // indices for forward-jumps-through-switch (for allocations)

        // indices for unique code fragments
        private const int Stackpop = 0;       // pop one
        private const int Stackpop2 = 1;      // pop two
        private const int Capback = 3;        // uncapture
        private const int Capback2 = 4;       // uncapture 2
        private const int Branchmarkback2 = 5;      // back2 part of branchmark
        private const int Lazybranchmarkback2 = 6;  // back2 part of lazybranchmark
        private const int Branchcountback2 = 7;     // back2 part of branchcount
        private const int Lazybranchcountback2 = 8; // back2 part of lazybranchcount
        private const int Forejumpback = 9;         // back part of forejump
        private const int Uniquecount = 10;
        private const int LoopTimeoutCheckCount = 2048; // A conservative value to guarantee the correct timeout handling.

        private static FieldInfo RegexRunnerField(string fieldname) => typeof(RegexRunner).GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;

        private static MethodInfo RegexRunnerMethod(string methname) => typeof(RegexRunner).GetMethod(methname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;

        /// <summary>
        /// Entry point to dynamically compile a regular expression.  The expression is compiled to
        /// an in-memory assembly.
        /// </summary>
        internal static RegexRunnerFactory Compile(string pattern, RegexCode code, RegexOptions options, bool hasTimeout) =>
            new RegexLWCGCompiler().FactoryInstanceFromCode(pattern, code, options, hasTimeout);

        /// <summary>
        /// Keeps track of an operation that needs to be referenced in the backtrack-jump
        /// switch table, and that needs backtracking code to be emitted (if flags != 0)
        /// </summary>
        private sealed class BacktrackNote
        {
            internal int _codepos;
            internal int _flags;
            internal Label _label;

            public BacktrackNote(int flags, Label label, int codepos)
            {
                _codepos = codepos;
                _flags = flags;
                _label = label;
            }
        }

        /// <summary>
        /// Adds a backtrack note to the list of them, and returns the index of the new
        /// note (which is also the index for the jump used by the switch table)
        /// </summary>
        private int AddBacktrackNote(int flags, Label l, int codepos)
        {
            if (_notes == null || _notecount >= _notes.Length)
            {
                var newnotes = new BacktrackNote[_notes == null ? 16 : _notes.Length * 2];
                if (_notes != null)
                {
                    Array.Copy(_notes, newnotes, _notecount);
                }
                _notes = newnotes;
            }

            _notes[_notecount] = new BacktrackNote(flags, l, codepos);

            return _notecount++;
        }

        /// <summary>
        /// Adds a backtrack note for the current operation; creates a new label for
        /// where the code will be, and returns the switch index.
        /// </summary>
        private int AddTrack() => AddTrack(RegexCode.Back);

        /// <summary>
        /// Adds a backtrack note for the current operation; creates a new label for
        /// where the code will be, and returns the switch index.
        /// </summary>
        private int AddTrack(int flags) => AddBacktrackNote(flags, DefineLabel(), _codepos);

        /// <summary>
        /// Adds a switchtable entry for the specified position (for the forward
        /// logic; does not cause backtracking logic to be generated)
        /// </summary>
        private int AddGoto(int destpos)
        {
            if (_goto![destpos] == -1)
            {
                _goto[destpos] = AddBacktrackNote(0, _labels![destpos], destpos);
            }

            return _goto[destpos];
        }

        /// <summary>
        /// Adds a note for backtracking code that only needs to be generated once;
        /// if it's already marked to be generated, returns the switch index
        /// for the unique piece of code.
        /// </summary>
        private int AddUniqueTrack(int i) => AddUniqueTrack(i, RegexCode.Back);

        /// <summary>
        /// Adds a note for backtracking code that only needs to be generated once;
        /// if it's already marked to be generated, returns the switch index
        /// for the unique piece of code.
        /// </summary>
        private int AddUniqueTrack(int i, int flags)
        {
            if (_uniquenote![i] == -1)
            {
                _uniquenote[i] = AddTrack(flags);
            }

            return _uniquenote[i];
        }

        /// <summary>A macro for _ilg.DefineLabel</summary>
        private Label DefineLabel() => _ilg!.DefineLabel();

        /// <summary>A macro for _ilg.MarkLabel</summary>
        private void MarkLabel(Label l) => _ilg!.MarkLabel(l);

        /// <summary>Returns the ith operand of the current operation.</summary>
        private int Operand(int i) => _codes![_codepos + i + 1];

        /// <summary>True if the current operation is marked for the leftward direction.</summary>
        private bool IsRightToLeft() => (_regexopcode & RegexCode.Rtl) != 0;

        /// <summary>True if the current operation is marked for case insensitive operation.</summary>
        private bool IsCaseInsensitive() => (_regexopcode & RegexCode.Ci) != 0;

        /// <summary>Returns the raw regex opcode (masking out Back and Rtl).</summary>
        private int Code() => _regexopcode & RegexCode.Mask;

        /// <summary>A macro for _ilg.Emit(Opcodes.Ldstr, str)</summary>
        protected void Ldstr(string str) => _ilg!.Emit(OpCodes.Ldstr, str);

        /// <summary>A macro for the various forms of Ldc.</summary>
        protected void Ldc(int i) => _ilg!.Emit(OpCodes.Ldc_I4, i);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldc_I8).</summary>
        protected void LdcI8(long i) => _ilg!.Emit(OpCodes.Ldc_I8, i);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ret).</summary>
        protected void Ret() => _ilg!.Emit(OpCodes.Ret);

        /// <summary>A macro for _ilg.Emit(OpCodes.Newobj, constructor).</summary>
        protected void Newobj(ConstructorInfo constructor) => _ilg!.Emit(OpCodes.Newobj, constructor);

        /// <summary>A macro for _ilg.Emit(OpCodes.Dup).</summary>
        protected void Dup() => _ilg!.Emit(OpCodes.Dup);

        /// <summary>A macro for _ilg.Emit(OpCodes.Rem_Un).</summary>
        private void RemUn() => _ilg!.Emit(OpCodes.Rem_Un);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ceq).</summary>
        private void Ceq() => _ilg!.Emit(OpCodes.Ceq);

        /// <summary>A macro for _ilg.Emit(OpCodes.Cgt_Un).</summary>
        private void CgtUn() => _ilg!.Emit(OpCodes.Cgt_Un);

        /// <summary>A macro for _ilg.Emit(OpCodes.Clt_Un).</summary>
        private void CltUn() => _ilg!.Emit(OpCodes.Clt_Un);

        /// <summary>A macro for _ilg.Emit(OpCodes.Pop).</summary>
        private void Pop() => _ilg!.Emit(OpCodes.Pop);

        /// <summary>A macro for _ilg.Emit(OpCodes.Add).</summary>
        private void Add() => _ilg!.Emit(OpCodes.Add);

        /// <summary>A macro for _ilg.Emit(OpCodes.Add); a true flag can turn it into a Sub.</summary>
        private void Add(bool negate) => _ilg!.Emit(negate ? OpCodes.Sub : OpCodes.Add);

        /// <summary>A macro for _ilg.Emit(OpCodes.Sub).</summary>
        private void Sub() => _ilg!.Emit(OpCodes.Sub);

        /// <summary>A macro for _ilg.Emit(OpCodes.Sub) or _ilg.Emit(OpCodes.Add).</summary>
        private void Sub(bool negate) => _ilg!.Emit(negate ? OpCodes.Add : OpCodes.Sub);

        /// <summary>A macro for _ilg.Emit(OpCodes.Neg).</summary>
        private void Neg() => _ilg!.Emit(OpCodes.Neg);

        /// <summary>A macro for _ilg.Emit(OpCodes.Mul).</summary>
        private void Mul() => _ilg!.Emit(OpCodes.Mul);

        /// <summary>A macro for _ilg.Emit(OpCodes.And).</summary>
        private void And() => _ilg!.Emit(OpCodes.And);

        /// <summary>A macro for _ilg.Emit(OpCodes.Or).</summary>
        private void Or() => _ilg!.Emit(OpCodes.Or);

        /// <summary>A macro for _ilg.Emit(OpCodes.Shl).</summary>
        private void Shl() => _ilg!.Emit(OpCodes.Shl);

        /// <summary>A macro for _ilg.Emit(OpCodes.Shr).</summary>
        private void Shr() => _ilg!.Emit(OpCodes.Shr);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldloc).</summary>
        /// <remarks>ILGenerator will switch to the optimal form based on the local's index.</remarks>
        private void Ldloc(LocalBuilder lt) => _ilg!.Emit(OpCodes.Ldloc, lt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldloca).</summary>
        /// <remarks>ILGenerator will switch to the optimal form based on the local's index.</remarks>
        private void Ldloca(LocalBuilder lt) => _ilg!.Emit(OpCodes.Ldloca, lt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldind_U2).</summary>
        private void LdindU2() => _ilg!.Emit(OpCodes.Ldind_U2);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldind_I4).</summary>
        private void LdindI4() => _ilg!.Emit(OpCodes.Ldind_I4);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldind_I8).</summary>
        private void LdindI8() => _ilg!.Emit(OpCodes.Ldind_I8);

        /// <summary>A macro for _ilg.Emit(OpCodes.Unaligned).</summary>
        private void Unaligned(byte alignment) => _ilg!.Emit(OpCodes.Unaligned, alignment);

        /// <summary>A macro for _ilg.Emit(OpCodes.Stloc).</summary>
        /// <remarks>ILGenerator will switch to the optimal form based on the local's index.</remarks>
        private void Stloc(LocalBuilder lt) => _ilg!.Emit(OpCodes.Stloc, lt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldarg_0).</summary>
        protected void Ldthis() => _ilg!.Emit(OpCodes.Ldarg_0);

        /// <summary>A macro for Ldthis(); Ldfld();</summary>
        protected void Ldthisfld(FieldInfo ft)
        {
            Ldthis();
            Ldfld(ft);
        }

        /// <summary>A macro for Ldthis(); Ldfld(); Stloc();</summary>
        private void Mvfldloc(FieldInfo ft, LocalBuilder lt)
        {
            Ldthisfld(ft);
            Stloc(lt);
        }

        /// <summary>A macro for Ldthis(); Ldloc(); Stfld();</summary>
        private void Mvlocfld(LocalBuilder lt, FieldInfo ft)
        {
            Ldthis();
            Ldloc(lt);
            Stfld(ft);
        }

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldfld).</summary>
        private void Ldfld(FieldInfo ft) => _ilg!.Emit(OpCodes.Ldfld, ft);

        /// <summary>A macro for _ilg.Emit(OpCodes.Stfld).</summary>
        protected void Stfld(FieldInfo ft) => _ilg!.Emit(OpCodes.Stfld, ft);

        /// <summary>A macro for _ilg.Emit(OpCodes.Callvirt, mt).</summary>
        protected void Callvirt(MethodInfo mt) => _ilg!.Emit(OpCodes.Callvirt, mt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Call, mt).</summary>
        protected void Call(MethodInfo mt) => _ilg!.Emit(OpCodes.Call, mt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Brfalse) (long form).</summary>
        private void BrfalseFar(Label l) => _ilg!.Emit(OpCodes.Brfalse, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Brtrue) (long form).</summary>
        private void BrtrueFar(Label l) => _ilg!.Emit(OpCodes.Brtrue, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Br) (long form).</summary>
        private void BrFar(Label l) => _ilg!.Emit(OpCodes.Br, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ble) (long form).</summary>
        private void BleFar(Label l) => _ilg!.Emit(OpCodes.Ble, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Blt) (long form).</summary>
        private void BltFar(Label l) => _ilg!.Emit(OpCodes.Blt, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Blt_Un) (long form).</summary>
        private void BltUnFar(Label l) => _ilg!.Emit(OpCodes.Blt_Un, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bge) (long form).</summary>
        private void BgeFar(Label l) => _ilg!.Emit(OpCodes.Bge, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bge_Un) (long form).</summary>
        private void BgeUnFar(Label l) => _ilg!.Emit(OpCodes.Bge_Un, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bgt) (long form).</summary>
        private void BgtFar(Label l) => _ilg!.Emit(OpCodes.Bgt, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bne) (long form).</summary>
        private void BneFar(Label l) => _ilg!.Emit(OpCodes.Bne_Un, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Beq) (long form).</summary>
        private void BeqFar(Label l) => _ilg!.Emit(OpCodes.Beq, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Brfalse_S) (short jump).</summary>
        private void Brfalse(Label l) => _ilg!.Emit(OpCodes.Brfalse_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Brtrue_S) (short jump).</summary>
        private void Brtrue(Label l) => _ilg!.Emit(OpCodes.Brtrue_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Br_S) (short jump).</summary>
        private void Br(Label l) => _ilg!.Emit(OpCodes.Br_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ble_S) (short jump).</summary>
        private void Ble(Label l) => _ilg!.Emit(OpCodes.Ble_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Blt_S) (short jump).</summary>
        private void Blt(Label l) => _ilg!.Emit(OpCodes.Blt_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bge_S) (short jump).</summary>
        private void Bge(Label l) => _ilg!.Emit(OpCodes.Bge_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bge_Un_S) (short jump).</summary>
        private void BgeUn(Label l) => _ilg!.Emit(OpCodes.Bge_Un_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bgt_S) (short jump).</summary>
        private void Bgt(Label l) => _ilg!.Emit(OpCodes.Bgt_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bgt_Un_S) (short jump).</summary>
        private void BgtUn(Label l) => _ilg!.Emit(OpCodes.Bgt_Un_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bne_S) (short jump).</summary>
        private void Bne(Label l) => _ilg!.Emit(OpCodes.Bne_Un_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Beq_S) (short jump).</summary>
        private void Beq(Label l) => _ilg!.Emit(OpCodes.Beq_S, l);

        /// <summary>A macro for the Ldlen instruction.</summary>
        private void Ldlen() => _ilg!.Emit(OpCodes.Ldlen);

        /// <summary>A macro for the Ldelem_I4 instruction.</summary>
        private void LdelemI4() => _ilg!.Emit(OpCodes.Ldelem_I4);

        /// <summary>A macro for the Stelem_I4 instruction.</summary>
        private void StelemI4() => _ilg!.Emit(OpCodes.Stelem_I4);

        private void Switch(Label[] table) => _ilg!.Emit(OpCodes.Switch, table);

        /// <summary>Declares a local int.</summary>
        private LocalBuilder DeclareInt32() => _ilg!.DeclareLocal(typeof(int));

        /// <summary>Declares a local CultureInfo.</summary>
        private LocalBuilder? DeclareTextInfo() => _ilg!.DeclareLocal(typeof(TextInfo));

        /// <summary>Declares a local int[].</summary>
        private LocalBuilder DeclareInt32Array() => _ilg!.DeclareLocal(typeof(int[]));

        /// <summary>Declares a local string.</summary>
        private LocalBuilder DeclareString() => _ilg!.DeclareLocal(typeof(string));

        private LocalBuilder DeclareReadOnlySpanChar() => _ilg!.DeclareLocal(typeof(ReadOnlySpan<char>));

        /// <summary>Rents an Int32 local variable slot from the pool of locals.</summary>
        /// <remarks>
        /// Care must be taken to Dispose of the returned <see cref="RentedLocalBuilder"/> when it's no longer needed,
        /// and also not to jump into the middle of a block involving a rented local from outside of that block.
        /// </remarks>
        private RentedLocalBuilder RentInt32Local() => new RentedLocalBuilder(
            _int32LocalsPool ??= new Stack<LocalBuilder>(),
            _int32LocalsPool.TryPop(out LocalBuilder? iterationLocal) ? iterationLocal : DeclareInt32());

        /// <summary>Rents a ReadOnlySpan(char) local variable slot from the pool of locals.</summary>
        /// <remarks>
        /// Care must be taken to Dispose of the returned <see cref="RentedLocalBuilder"/> when it's no longer needed,
        /// and also not to jump into the middle of a block involving a rented local from outside of that block.
        /// </remarks>
        private RentedLocalBuilder RentReadOnlySpanCharLocal() => new RentedLocalBuilder(
            _readOnlySpanCharLocalsPool ??= new Stack<LocalBuilder>(1), // capacity == 1 as we currently don't expect overlapping instances
            _readOnlySpanCharLocalsPool.TryPop(out LocalBuilder? iterationLocal) ? iterationLocal : DeclareReadOnlySpanChar());

        /// <summary>Returned a rented local to the pool.</summary>
        private struct RentedLocalBuilder : IDisposable
        {
            private Stack<LocalBuilder> _pool;
            private LocalBuilder _local;

            internal RentedLocalBuilder(Stack<LocalBuilder> pool, LocalBuilder local)
            {
                _local = local;
                _pool = pool;
            }

            public static implicit operator LocalBuilder(RentedLocalBuilder local) => local._local;

            public void Dispose()
            {
                Debug.Assert(_pool != null);
                Debug.Assert(_local != null);
                Debug.Assert(!_pool.Contains(_local));
                _pool.Push(_local);
                this = default;
            }
        }

        /// <summary>Loads the char to the right of the current position.</summary>
        private void Rightchar()
        {
            Ldloc(_runtextLocal!);
            Ldloc(_runtextposLocal!);
            Call(s_stringGetCharsMethod);
        }

        /// <summary>Loads the char to the right of the current position and advances the current position.</summary>
        private void Rightcharnext()
        {
            Ldloc(_runtextLocal!);
            Ldloc(_runtextposLocal!);
            Call(s_stringGetCharsMethod);
            Ldloc(_runtextposLocal!);
            Ldc(1);
            Add();
            Stloc(_runtextposLocal!);
        }

        /// <summary>Loads the char to the left of the current position.</summary>
        private void Leftchar()
        {
            Ldloc(_runtextLocal!);
            Ldloc(_runtextposLocal!);
            Ldc(1);
            Sub();
            Call(s_stringGetCharsMethod);
        }

        /// <summary>Loads the char to the left of the current position and advances (leftward).</summary>
        private void Leftcharnext()
        {
            Ldloc(_runtextposLocal!);
            Ldc(1);
            Sub();
            Stloc(_runtextposLocal!);
            Ldloc(_runtextLocal!);
            Ldloc(_runtextposLocal!);
            Call(s_stringGetCharsMethod);
        }

        /// <summary>Creates a backtrack note and pushes the switch index it on the tracking stack.</summary>
        private void Track()
        {
            ReadyPushTrack();
            Ldc(AddTrack());
            DoPush();
        }

        /// <summary>
        /// Pushes the current switch index on the tracking stack so the backtracking
        /// logic will be repeated again next time we backtrack here.
        /// </summary>
        private void Trackagain()
        {
            ReadyPushTrack();
            Ldc(_backpos);
            DoPush();
        }

        /// <summary>Saves the value of a local variable on the tracking stack.</summary>
        private void PushTrack(LocalBuilder lt)
        {
            ReadyPushTrack();
            Ldloc(lt);
            DoPush();
        }

        /// <summary>
        /// Creates a backtrack note for a piece of code that should only be generated once,
        /// and emits code that pushes the switch index on the backtracking stack.
        /// </summary>
        private void TrackUnique(int i)
        {
            ReadyPushTrack();
            Ldc(AddUniqueTrack(i));
            DoPush();
        }

        /// <summary>
        /// Creates a second-backtrack note for a piece of code that should only be
        /// generated once, and emits code that pushes the switch index on the
        /// backtracking stack.
        /// </summary>
        private void TrackUnique2(int i)
        {
            ReadyPushTrack();
            Ldc(AddUniqueTrack(i, RegexCode.Back2));
            DoPush();
        }

        /// <summary>Prologue to code that will push an element on the tracking stack.</summary>
        private void ReadyPushTrack()
        {
            Ldloc(_runtrackposLocal!);
            Ldc(1);
            Sub();
            Stloc(_runtrackposLocal!);
            Ldloc(_runtrackLocal!);
            Ldloc(_runtrackposLocal!);
        }

        /// <summary>Pops an element off the tracking stack (leave it on the operand stack).</summary>
        private void PopTrack()
        {
            Ldloc(_runtrackLocal!);
            Ldloc(_runtrackposLocal!);
            LdelemI4();
            using RentedLocalBuilder tmp = RentInt32Local();
            Stloc(tmp);
            Ldloc(_runtrackposLocal!);
            Ldc(1);
            Add();
            Stloc(_runtrackposLocal!);
            Ldloc(tmp);
        }

        /// <summary>Retrieves the top entry on the tracking stack without popping.</summary>
        private void TopTrack()
        {
            Ldloc(_runtrackLocal!);
            Ldloc(_runtrackposLocal!);
            LdelemI4();
        }

        /// <summary>Saves the value of a local variable on the grouping stack.</summary>
        private void PushStack(LocalBuilder lt)
        {
            ReadyPushStack();
            Ldloc(lt);
            DoPush();
        }

        /// <summary>Prologue to code that will replace the ith element on the grouping stack.</summary>
        internal void ReadyReplaceStack(int i)
        {
            Ldloc(_runstackLocal!);
            Ldloc(_runstackposLocal!);
            if (i != 0)
            {
                Ldc(i);
                Add();
            }
        }

        /// <summary>Prologue to code that will push an element on the grouping stack.</summary>
        private void ReadyPushStack()
        {
            Ldloc(_runstackposLocal!);
            Ldc(1);
            Sub();
            Stloc(_runstackposLocal!);
            Ldloc(_runstackLocal!);
            Ldloc(_runstackposLocal!);
        }

        /// <summary>Retrieves the top entry on the stack without popping.</summary>
        private void TopStack()
        {
            Ldloc(_runstackLocal!);
            Ldloc(_runstackposLocal!);
            LdelemI4();
        }

        /// <summary>Pops an element off the grouping stack (leave it on the operand stack).</summary>
        private void PopStack()
        {
            using RentedLocalBuilder elementLocal = RentInt32Local();
            Ldloc(_runstackLocal!);
            Ldloc(_runstackposLocal!);
            LdelemI4();
            Stloc(elementLocal);
            Ldloc(_runstackposLocal!);
            Ldc(1);
            Add();
            Stloc(_runstackposLocal!);
            Ldloc(elementLocal);
        }

        /// <summary>Pops 1 element off the grouping stack and discards it.</summary>
        private void PopDiscardStack() => PopDiscardStack(1);

        /// <summary>Pops i elements off the grouping stack and discards them.</summary>
        private void PopDiscardStack(int i)
        {
            Ldloc(_runstackposLocal!);
            Ldc(i);
            Add();
            Stloc(_runstackposLocal!);
        }

        /// <summary>Epilogue to code that will replace an element on a stack (use Ld* in between).</summary>
        private void DoReplace() => StelemI4();

        /// <summary>Epilogue to code that will push an element on a stack (use Ld* in between).</summary>
        private void DoPush() => StelemI4();

        /// <summary>Jump to the backtracking switch.</summary>
        private void Back() => BrFar(_backtrack);

        /// <summary>
        /// Branch to the MSIL corresponding to the regex code at i
        /// </summary>
        /// <remarks>
        /// A trick: since track and stack space is gobbled up unboundedly
        /// only as a result of branching backwards, this is where we check
        /// for sufficient space and trigger reallocations.
        ///
        /// If the "goto" is backwards, we generate code that checks
        /// available space against the amount of space that would be needed
        /// in the worst case by code that will only go forward; if there's
        /// not enough, we push the destination on the tracking stack, then
        /// we jump to the place where we invoke the allocator.
        ///
        /// Since forward gotos pose no threat, they just turn into a Br.
        /// </remarks>
        private void Goto(int i)
        {
            if (i < _codepos)
            {
                Label l1 = DefineLabel();

                // When going backwards, ensure enough space.
                Ldloc(_runtrackposLocal!);
                Ldc(_trackcount * 4);
                Ble(l1);
                Ldloc(_runstackposLocal!);
                Ldc(_trackcount * 3);
                BgtFar(_labels![i]);
                MarkLabel(l1);
                ReadyPushTrack();
                Ldc(AddGoto(i));
                DoPush();
                BrFar(_backtrack);
            }
            else
            {
                BrFar(_labels![i]);
            }
        }

        /// <summary>
        /// Returns the position of the next operation in the regex code, taking
        /// into account the different numbers of arguments taken by operations
        /// </summary>
        private int NextCodepos() => _codepos + RegexCode.OpcodeSize(_codes![_codepos]);

        /// <summary>The label for the next (forward) operation.</summary>
        private Label AdvanceLabel() => _labels![NextCodepos()];

        /// <summary>Goto the next (forward) operation.</summary>
        private void Advance() => BrFar(AdvanceLabel());

        /// <summary>Sets the culture local to CultureInfo.CurrentCulture.</summary>
        private void InitLocalCultureInfo()
        {
            Debug.Assert(_textInfoLocal != null);
            Call(s_cultureInfoGetCurrentCultureMethod);
            Callvirt(s_cultureInfoGetTextInfoMethod);
            Stloc(_textInfoLocal);
        }

        /// <summary>Whether ToLower operations should be performed with the invariant culture as opposed to the one in <see cref="_textInfoLocal"/>.</summary>
        private bool UseToLowerInvariant => _textInfoLocal == null || (_options & RegexOptions.CultureInvariant) != 0;

        /// <summary>Invokes either char.ToLowerInvariant(c) or _textInfo.ToLower(c).</summary>
        private void CallToLower()
        {
            if (UseToLowerInvariant)
            {
                Call(s_charToLowerInvariantMethod);
            }
            else
            {
                using RentedLocalBuilder currentCharLocal = RentInt32Local();
                Stloc(currentCharLocal);
                Ldloc(_textInfoLocal!);
                Ldloc(currentCharLocal);
                Callvirt(s_textInfoToLowerMethod);
            }
        }

        /// <summary>
        /// Generates the first section of the MSIL. This section contains all
        /// the forward logic, and corresponds directly to the regex codes.
        /// In the absence of backtracking, this is all we would need.
        /// </summary>
        private void GenerateForwardSection()
        {
            _uniquenote = new int[Uniquecount];
            _labels = new Label[_codes!.Length];
            _goto = new int[_codes.Length];

            // initialize

            Array.Fill(_uniquenote, -1);
            for (int codepos = 0; codepos < _codes.Length; codepos += RegexCode.OpcodeSize(_codes[codepos]))
            {
                _goto[codepos] = -1;
                _labels[codepos] = DefineLabel();
            }

            // emit variable initializers

            Mvfldloc(s_runtextField, _runtextLocal!);
            Mvfldloc(s_runtextbegField, _runtextbegLocal!);
            Mvfldloc(s_runtextendField, _runtextendLocal!);
            Mvfldloc(s_runtextposField, _runtextposLocal!);
            Mvfldloc(s_runtrackField, _runtrackLocal!);
            Mvfldloc(s_runtrackposField, _runtrackposLocal!);
            Mvfldloc(s_runstackField, _runstackLocal!);
            Mvfldloc(s_runstackposField, _runstackposLocal!);

            _backpos = -1;

            for (int codepos = 0; codepos < _codes.Length; codepos += RegexCode.OpcodeSize(_codes[codepos]))
            {
                MarkLabel(_labels[codepos]);
                _codepos = codepos;
                _regexopcode = _codes[codepos];
                GenerateOneCode();
            }
        }

        /// <summary>
        /// Generates the middle section of the MSIL. This section contains the
        /// big switch jump that allows us to simulate a stack of addresses,
        /// and it also contains the calls that expand the tracking and the
        /// grouping stack when they get too full.
        /// </summary>
        private void GenerateMiddleSection()
        {
            using RentedLocalBuilder limitLocal = RentInt32Local();
            Label afterDoubleStack = DefineLabel();
            Label afterDoubleTrack = DefineLabel();

            // Backtrack:
            MarkLabel(_backtrack);

            // (Equivalent of EnsureStorage, but written to avoid unnecessary local spilling.)

            // int limitLocal = runtrackcount * 4;
            Ldthisfld(s_runtrackcountField);
            Ldc(4);
            Mul();
            Stloc(limitLocal);

            // if (runstackpos < limit)
            // {
            //     this.runstackpos = runstackpos;
            //     DoubleStack(); // might change runstackpos and runstack
            //     runstackpos = this.runstackpos;
            //     runstack = this.runstack;
            // }
            Ldloc(_runstackposLocal!);
            Ldloc(limitLocal);
            Bge(afterDoubleStack);
            Mvlocfld(_runstackposLocal!, s_runstackposField);
            Ldthis();
            Call(s_doubleStackMethod);
            Mvfldloc(s_runstackposField, _runstackposLocal!);
            Mvfldloc(s_runstackField, _runstackLocal!);
            MarkLabel(afterDoubleStack);

            // if (runtrackpos < limit)
            // {
            //     this.runtrackpos = runtrackpos;
            //     DoubleTrack(); // might change runtrackpos and runtrack
            //     runtrackpos = this.runtrackpos;
            //     runtrack = this.runtrack;
            // }
            Ldloc(_runtrackposLocal!);
            Ldloc(limitLocal);
            Bge(afterDoubleTrack);
            Mvlocfld(_runtrackposLocal!, s_runtrackposField);
            Ldthis();
            Call(s_doubleTrackMethod);
            Mvfldloc(s_runtrackposField, _runtrackposLocal!);
            Mvfldloc(s_runtrackField, _runtrackLocal!);
            MarkLabel(afterDoubleTrack);

            // runtrack[runtrackpos++]
            PopTrack();

            // Backtracking jump table
            var table = new Label[_notecount];
            for (int i = 0; i < _notecount; i++)
            {
                table[i] = _notes![i]._label;
            }
            Switch(table);
        }

        /// <summary>
        /// Generates the last section of the MSIL. This section contains all of
        /// the backtracking logic.
        /// </summary>
        private void GenerateBacktrackSection()
        {
            for (int i = 0; i < _notecount; i++)
            {
                BacktrackNote n = _notes![i];
                if (n._flags != 0)
                {
                    MarkLabel(n._label);
                    _codepos = n._codepos;
                    _backpos = i;
                    _regexopcode = _codes![n._codepos] | n._flags;
                    GenerateOneCode();
                }
            }
        }

        /// <summary>
        /// Generates FindFirstChar.
        /// </summary>
        protected void GenerateFindFirstChar()
        {
            Debug.Assert(_code != null);
            _int32LocalsPool?.Clear();
            _readOnlySpanCharLocalsPool?.Clear();

            _runtextposLocal = DeclareInt32();
            _runtextendLocal = DeclareInt32();
            if (_code.RightToLeft)
            {
                _runtextbegLocal = DeclareInt32();
            }
            _runtextLocal = DeclareString();
            _textInfoLocal = null;
            if ((_options & RegexOptions.CultureInvariant) == 0)
            {
                bool needsCulture = _code.FindOptimizations.FindMode switch
                {
                    FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseInsensitive or
                    FindNextStartingPositionMode.LeadingLiteral_RightToLeft_CaseInsensitive or
                    FindNextStartingPositionMode.FixedSets_LeftToRight_CaseInsensitive or
                    FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseInsensitive or
                    FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseInsensitive => true,

                    _ when _code.FindOptimizations.FixedDistanceSets is List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)> sets => sets.Exists(set => set.CaseInsensitive),

                    _ => false,
                };

                if (needsCulture)
                {
                    _textInfoLocal = DeclareTextInfo();
                    InitLocalCultureInfo();
                }
            }

            // Load necessary locals
            // int runtextpos = this.runtextpos;
            // int runtextend = this.runtextend;
            Mvfldloc(s_runtextposField, _runtextposLocal);
            Mvfldloc(s_runtextendField, _runtextendLocal);
            if (_code.RightToLeft)
            {
                Mvfldloc(s_runtextbegField, _runtextbegLocal!);
            }

            // Generate length check.  If the input isn't long enough to possibly match, fail quickly.
            // It's rare for min required length to be 0, so we don't bother special-casing the check,
            // especially since we want the "return false" code regardless.
            int minRequiredLength = _code.Tree.MinRequiredLength;
            Debug.Assert(minRequiredLength >= 0);
            Label returnFalse = DefineLabel();
            Label finishedLengthCheck = DefineLabel();
            if (!_code.RightToLeft)
            {
                // if (runtextpos > runtextend - _code.Tree.MinRequiredLength)
                // {
                //     this.runtextpos = runtextend;
                //     return false;
                // }
                Ldloc(_runtextposLocal);
                Ldloc(_runtextendLocal);
                if (minRequiredLength > 0)
                {
                    Ldc(minRequiredLength);
                    Sub();
                }
                Ble(finishedLengthCheck);

                MarkLabel(returnFalse);
                Ldthis();
                Ldloc(_runtextendLocal);
            }
            else
            {
                // if (runtextpos - _code.Tree.MinRequiredLength < runtextbeg)
                // {
                //     this.runtextpos = runtextbeg;
                //     return false;
                // }
                Ldloc(_runtextposLocal);
                if (minRequiredLength > 0)
                {
                    Ldc(minRequiredLength);
                    Sub();
                }
                Ldloc(_runtextbegLocal!);
                Bge(finishedLengthCheck);

                MarkLabel(returnFalse);
                Ldthis();
                Ldloc(_runtextbegLocal!);
            }
            Stfld(s_runtextposField);
            Ldc(0);
            Ret();
            MarkLabel(finishedLengthCheck);

            // Emit any anchors.
            if (GenerateAnchors())
            {
                return;
            }

            // Either anchors weren't specified, or they don't completely root all matches to a specific location.

            switch (_code.FindOptimizations.FindMode)
            {
                case FindNextStartingPositionMode.LeadingPrefix_LeftToRight_CaseSensitive:
                    Debug.Assert(!string.IsNullOrEmpty(_code.FindOptimizations.LeadingCaseSensitivePrefix));
                    GenerateIndexOf_LeftToRight(_code.FindOptimizations.LeadingCaseSensitivePrefix);
                    break;

                case FindNextStartingPositionMode.LeadingPrefix_RightToLeft_CaseSensitive:
                    Debug.Assert(!string.IsNullOrEmpty(_code.FindOptimizations.LeadingCaseSensitivePrefix));
                    GenerateIndexOf_RightToLeft(_code.FindOptimizations.LeadingCaseSensitivePrefix);
                    break;

                case FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseSensitive:
                case FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseInsensitive:
                case FindNextStartingPositionMode.FixedSets_LeftToRight_CaseSensitive:
                case FindNextStartingPositionMode.FixedSets_LeftToRight_CaseInsensitive:
                    Debug.Assert(_code.FindOptimizations.FixedDistanceSets is { Count: > 0 });
                    GenerateFixedSet_LeftToRight();
                    break;

                case FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseSensitive:
                case FindNextStartingPositionMode.LeadingSet_RightToLeft_CaseInsensitive:
                    Debug.Assert(_code.FindOptimizations.FixedDistanceSets is { Count: > 0 });
                    GenerateFixedSet_RightToLeft();
                    break;

                default:
                    Debug.Fail($"Unexpected mode: {_code.FindOptimizations.FindMode}");
                    goto case FindNextStartingPositionMode.NoSearch;

                case FindNextStartingPositionMode.NoSearch:
                    // return true;
                    Ldc(1);
                    Ret();
                    break;
            }

            // Emits any anchors.  Returns true if the anchor roots any match to a specific location and thus no further
            // searching is required; otherwise, false.
            bool GenerateAnchors()
            {
                // Generate anchor checks.
                if ((_code.FindOptimizations.LeadingAnchor & (RegexPrefixAnalyzer.Beginning | RegexPrefixAnalyzer.Start | RegexPrefixAnalyzer.EndZ | RegexPrefixAnalyzer.End | RegexPrefixAnalyzer.Bol)) != 0)
                {
                    switch (_code.FindOptimizations.LeadingAnchor)
                    {
                        case RegexPrefixAnalyzer.Beginning:
                            {
                                Label l1 = DefineLabel();
                                Ldloc(_runtextposLocal);
                                if (!_code.RightToLeft)
                                {
                                    Ldthisfld(s_runtextbegField);
                                    Ble(l1);
                                    Br(returnFalse);
                                }
                                else
                                {
                                    Ldloc(_runtextbegLocal!);
                                    Ble(l1);
                                    Ldthis();
                                    Ldloc(_runtextbegLocal!);
                                    Stfld(s_runtextposField);
                                }
                                MarkLabel(l1);
                            }
                            Ldc(1);
                            Ret();
                            return true;

                        case RegexPrefixAnalyzer.Start:
                            {
                                Label l1 = DefineLabel();
                                Ldloc(_runtextposLocal);
                                Ldthisfld(s_runtextstartField);
                                if (!_code.RightToLeft)
                                {
                                    Ble(l1);
                                }
                                else
                                {
                                    Bge(l1);
                                }
                                Br(returnFalse);
                                MarkLabel(l1);
                            }
                            Ldc(1);
                            Ret();
                            return true;

                        case RegexPrefixAnalyzer.EndZ:
                            {
                                Label l1 = DefineLabel();
                                if (!_code.RightToLeft)
                                {
                                    Ldloc(_runtextposLocal);
                                    Ldloc(_runtextendLocal);
                                    Ldc(1);
                                    Sub();
                                    Bge(l1);
                                    Ldthis();
                                    Ldloc(_runtextendLocal);
                                    Ldc(1);
                                    Sub();
                                    Stfld(s_runtextposField);
                                    MarkLabel(l1);
                                }
                                else
                                {
                                    Label l2 = DefineLabel();
                                    Ldloc(_runtextposLocal);
                                    Ldloc(_runtextendLocal);
                                    Ldc(1);
                                    Sub();
                                    Blt(l1);
                                    Ldloc(_runtextposLocal);
                                    Ldloc(_runtextendLocal);
                                    Beq(l2);
                                    Ldthisfld(s_runtextField);
                                    Ldloc(_runtextposLocal);
                                    Call(s_stringGetCharsMethod);
                                    Ldc('\n');
                                    Beq(l2);
                                    MarkLabel(l1);
                                    BrFar(returnFalse);
                                    MarkLabel(l2);
                                }
                            }
                            Ldc(1);
                            Ret();
                            return true;

                        case RegexPrefixAnalyzer.End:
                            {
                                Label l1 = DefineLabel();
                                Ldloc(_runtextposLocal);
                                Ldloc(_runtextendLocal);
                                if (!_code.RightToLeft)
                                {
                                    Bge(l1);
                                    Ldthis();
                                    Ldloc(_runtextendLocal);
                                    Stfld(s_runtextposField);
                                }
                                else
                                {
                                    Bge(l1);
                                    Br(returnFalse);
                                }
                                MarkLabel(l1);
                            }
                            Ldc(1);
                            Ret();
                            return true;

                        case RegexPrefixAnalyzer.Bol:
                            {
                                // Optimize the handling of a Beginning-Of-Line (BOL) anchor.  BOL is special, in that unlike
                                // other anchors like Beginning, there are potentially multiple places a BOL can match.  So unlike
                                // the other anchors, which all skip all subsequent processing if found, with BOL we just use it
                                // to boost our position to the next line, and then continue normally with any prefix or char class searches.

                                Debug.Assert(!_code.RightToLeft, "RightToLeft isn't implemented and should have been filtered out previously");
                                Label atBeginningOfLine = DefineLabel();

                                // if (runtextpos > runtextbeg...
                                Ldloc(_runtextposLocal!);
                                Ldthisfld(s_runtextbegField);
                                Ble(atBeginningOfLine);

                                // ... && runtext[runtextpos - 1] != '\n') { ... }
                                Ldthisfld(s_runtextField);
                                Ldloc(_runtextposLocal);
                                Ldc(1);
                                Sub();
                                Call(s_stringGetCharsMethod);
                                Ldc('\n');
                                Beq(atBeginningOfLine);

                                // int tmp = runtext.IndexOf('\n', runtextpos);
                                Ldthisfld(s_runtextField);
                                Ldc('\n');
                                Ldloc(_runtextposLocal);
                                Call(s_stringIndexOfCharInt);
                                using (RentedLocalBuilder newlinePos = RentInt32Local())
                                {
                                    Stloc(newlinePos);

                                    // if (newlinePos == -1 || newlinePos + 1 > runtextend)
                                    // {
                                    //     runtextpos = runtextend;
                                    //     return false;
                                    // }
                                    Ldloc(newlinePos);
                                    Ldc(-1);
                                    Beq(returnFalse);
                                    Ldloc(newlinePos);
                                    Ldc(1);
                                    Add();
                                    Ldloc(_runtextendLocal);
                                    Bgt(returnFalse);

                                    // runtextpos = newlinePos + 1;
                                    Ldloc(newlinePos);
                                    Ldc(1);
                                    Add();
                                    Stloc(_runtextposLocal);
                                }

                                MarkLabel(atBeginningOfLine);
                            }
                            break;
                    }
                }

                return false;
            }

            void GenerateIndexOf_LeftToRight(string prefix)
            {
                using RentedLocalBuilder i = RentInt32Local();

                // int i = runtext.AsSpan(runtextpos, runtextend - runtextpos).IndexOf(prefix);
                Ldthis();
                Ldfld(s_runtextField);
                Ldloc(_runtextposLocal);
                Ldloc(_runtextendLocal);
                Ldloc(_runtextposLocal);
                Sub();
                Call(s_stringAsSpanIntIntMethod);
                Ldstr(prefix);
                Call(s_stringAsSpanMethod);
                Call(s_spanIndexOfSpan);
                Stloc(i);

                // if (i < 0) goto ReturnFalse;
                Ldloc(i);
                Ldc(0);
                BltFar(returnFalse);

                // base.runtextpos = runtextpos + i;
                // return true;
                Ldthis();
                Ldloc(_runtextposLocal);
                Ldloc(i);
                Add();
                Stfld(s_runtextposField);
                Ldc(1);
                Ret();
            }

            void GenerateIndexOf_RightToLeft(string prefix)
            {
                using RentedLocalBuilder i = RentInt32Local();

                // int i = runtext.AsSpan(runtextpos, runtextbeg, runtextpos - runtextbeg).LastIndexOf(prefix);
                Ldthis();
                Ldfld(s_runtextField);
                Ldloc(_runtextbegLocal!);
                Ldloc(_runtextposLocal);
                Ldloc(_runtextbegLocal!);
                Sub();
                Call(s_stringAsSpanIntIntMethod);
                Ldstr(prefix);
                Call(s_stringAsSpanMethod);
                Call(s_spanLastIndexOfSpan);
                Stloc(i);

                // if (i < 0) goto ReturnFalse;
                Ldloc(i);
                Ldc(0);
                BltFar(returnFalse);

                // base.runtextpos = runtextbeg + i + LeadingCaseSensitivePrefix.Length;
                // return true;
                Ldthis();
                Ldloc(_runtextbegLocal!);
                Ldloc(i);
                Add();
                Ldc(prefix.Length);
                Add();
                Stfld(s_runtextposField);
                Ldc(1);
                Ret();
            }

            void GenerateFixedSet_RightToLeft()
            {
                (char[]? Chars, string Set, int Distance, bool CaseInsensitive) set = _code.FindOptimizations.FixedDistanceSets![0];
                Debug.Assert(set.Distance == 0);

                using RentedLocalBuilder i = RentInt32Local();

                if (set.Chars is { Length: 1 } && !set.CaseInsensitive)
                {
                    // int i = runtext.AsSpan(runtextpos, runtextbeg, runtextpos - runtextbeg).LastIndexOf(set.Chars[0]);
                    Ldthis();
                    Ldfld(s_runtextField);
                    Ldloc(_runtextbegLocal!);
                    Ldloc(_runtextposLocal);
                    Ldloc(_runtextbegLocal!);
                    Sub();
                    Call(s_stringAsSpanIntIntMethod);
                    Ldc(set.Chars[0]);
                    Call(s_spanLastIndexOfChar);
                    Stloc(i);

                    // if (i < 0) goto ReturnFalse;
                    Ldloc(i);
                    Ldc(0);
                    BltFar(returnFalse);

                    // base.runtextpos = runtextbeg + i + 1;
                    // return true;
                    Ldthis();
                    Ldloc(_runtextbegLocal!);
                    Ldloc(i);
                    Add();
                    Ldc(1);
                    Add();
                    Stfld(s_runtextposField);
                    Ldc(1);
                    Ret();
                }
                else
                {
                    Label condition = DefineLabel();
                    Label increment = DefineLabel();
                    Label body = DefineLabel();

                    Mvfldloc(s_runtextField, _runtextLocal);

                    // for (int i = runtextpos - 1; ...
                    Ldloc(_runtextposLocal);
                    Ldc(1);
                    Sub();
                    Stloc(i);
                    BrFar(condition);

                    // if (MatchCharClass(runtext[i], set))
                    MarkLabel(body);
                    Ldloc(_runtextLocal);
                    Ldloc(i);
                    Call(s_stringGetCharsMethod);
                    EmitMatchCharacterClass(set.Set, set.CaseInsensitive);
                    Brfalse(increment);

                    // base.runtextpos = i + 1;
                    // return true;
                    Ldthis();
                    Ldloc(i);
                    Ldc(1);
                    Add();
                    Stfld(s_runtextposField);
                    Ldc(1);
                    Ret();

                    // for (...; ...; i--)
                    MarkLabel(increment);
                    Ldloc(i);
                    Ldc(1);
                    Sub();
                    Stloc(i);

                    // for (...; i >= runtextbeg; ...)
                    MarkLabel(condition);
                    Ldloc(i);
                    Ldloc(_runtextbegLocal!);
                    BgeFar(body);

                    BrFar(returnFalse);
                }
            }

            void GenerateFixedSet_LeftToRight()
            {
                List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)>? sets = _code.FindOptimizations.FixedDistanceSets;
                (char[]? Chars, string Set, int Distance, bool CaseInsensitive) primarySet = sets![0];
                const int MaxSets = 4;
                int setsToUse = Math.Min(sets.Count, MaxSets);

                using RentedLocalBuilder iLocal = RentInt32Local();
                using RentedLocalBuilder textSpanLocal = RentReadOnlySpanCharLocal();

                // ReadOnlySpan<char> span = this.runtext.AsSpan(runtextpos, runtextend - runtextpos);
                Ldthisfld(s_runtextField);
                Ldloc(_runtextposLocal);
                Ldloc(_runtextendLocal);
                Ldloc(_runtextposLocal);
                Sub();
                Call(s_stringAsSpanIntIntMethod);
                Stloc(textSpanLocal);

                // If we can use IndexOf{Any}, try to accelerate the skip loop via vectorization to match the first prefix.
                // We can use it if this is a case-sensitive class with a small number of characters in the class.
                int setIndex = 0;
                bool canUseIndexOf = !primarySet.CaseInsensitive && primarySet.Chars is not null;
                bool needLoop = !canUseIndexOf || setsToUse > 1;

                Label checkSpanLengthLabel = default;
                Label charNotInClassLabel = default;
                Label loopBody = default;
                if (needLoop)
                {
                    checkSpanLengthLabel = DefineLabel();
                    charNotInClassLabel = DefineLabel();
                    loopBody = DefineLabel();

                    // for (int i = 0;
                    Ldc(0);
                    Stloc(iLocal);
                    BrFar(checkSpanLengthLabel);
                    MarkLabel(loopBody);
                }

                if (canUseIndexOf)
                {
                    setIndex = 1;

                    if (needLoop)
                    {
                        // textSpan.Slice(iLocal + primarySet.Distance);
                        Ldloca(textSpanLocal);
                        Ldloc(iLocal);
                        if (primarySet.Distance != 0)
                        {
                            Ldc(primarySet.Distance);
                            Add();
                        }
                        Call(s_spanSliceIntMethod);
                    }
                    else if (primarySet.Distance != 0)
                    {
                        // textSpan.Slice(primarySet.Distance)
                        Ldloca(textSpanLocal);
                        Ldc(primarySet.Distance);
                        Call(s_spanSliceIntMethod);
                    }
                    else
                    {
                        // textSpan
                        Ldloc(textSpanLocal);
                    }

                    switch (primarySet.Chars!.Length)
                    {
                        case 1:
                            // tmp = ...IndexOf(setChars[0]);
                            Ldc(primarySet.Chars[0]);
                            Call(s_spanIndexOfChar);
                            break;

                        case 2:
                            // tmp = ...IndexOfAny(setChars[0], setChars[1]);
                            Ldc(primarySet.Chars[0]);
                            Ldc(primarySet.Chars[1]);
                            Call(s_spanIndexOfAnyCharChar);
                            break;

                        case 3:
                            // tmp = ...IndexOfAny(setChars[0], setChars[1], setChars[2]});
                            Ldc(primarySet.Chars[0]);
                            Ldc(primarySet.Chars[1]);
                            Ldc(primarySet.Chars[2]);
                            Call(s_spanIndexOfAnyCharCharChar);
                            break;

                        default:
                            Ldstr(new string(primarySet.Chars));
                            Call(s_stringAsSpanMethod);
                            Call(s_spanIndexOfAnySpan);
                            break;
                    }

                    if (needLoop)
                    {
                        // i += tmp;
                        // if (tmp < 0) goto returnFalse;
                        using (RentedLocalBuilder tmp = RentInt32Local())
                        {
                            Stloc(tmp);
                            Ldloc(iLocal);
                            Ldloc(tmp);
                            Add();
                            Stloc(iLocal);
                            Ldloc(tmp);
                            Ldc(0);
                            BltFar(returnFalse);
                        }
                    }
                    else
                    {
                        // i = tmp;
                        // if (i < 0) goto returnFalse;
                        Stloc(iLocal);
                        Ldloc(iLocal);
                        Ldc(0);
                        BltFar(returnFalse);
                    }

                    // if (i >= textSpan.Length - (minRequiredLength - 1)) goto returnFalse;
                    if (sets.Count > 1)
                    {
                        Debug.Assert(needLoop);
                        Ldloca(textSpanLocal);
                        Call(s_spanGetLengthMethod);
                        Ldc(minRequiredLength - 1);
                        Sub();
                        Ldloc(iLocal);
                        BleFar(returnFalse);
                    }
                }

                // if (!CharInClass(textSpan[i], prefix[0], "...")) continue;
                // if (!CharInClass(textSpan[i + 1], prefix[1], "...")) continue;
                // if (!CharInClass(textSpan[i + 2], prefix[2], "...")) continue;
                // ...
                Debug.Assert(setIndex == 0 || setIndex == 1);
                for ( ; setIndex < sets.Count; setIndex++)
                {
                    Debug.Assert(needLoop);
                    Ldloca(textSpanLocal);
                    Ldloc(iLocal);
                    if (sets[setIndex].Distance != 0)
                    {
                        Ldc(sets[setIndex].Distance);
                        Add();
                    }
                    Call(s_spanGetItemMethod);
                    LdindU2();
                    EmitMatchCharacterClass(sets[setIndex].Set, sets[setIndex].CaseInsensitive);
                    BrfalseFar(charNotInClassLabel);
                }

                // this.runtextpos = runtextpos + i;
                // return true;
                Ldthis();
                Ldloc(_runtextposLocal);
                Ldloc(iLocal);
                Add();
                Stfld(s_runtextposField);
                Ldc(1);
                Ret();

                if (needLoop)
                {
                    MarkLabel(charNotInClassLabel);

                    // for (...; ...; i++)
                    Ldloc(iLocal);
                    Ldc(1);
                    Add();
                    Stloc(iLocal);

                    // for (...; i < span.Length - (minRequiredLength - 1); ...);
                    MarkLabel(checkSpanLengthLabel);
                    Ldloc(iLocal);
                    Ldloca(textSpanLocal);
                    Call(s_spanGetLengthMethod);
                    if (setsToUse > 1 || primarySet.Distance != 0)
                    {
                        Ldc(minRequiredLength - 1);
                        Sub();
                    }
                    BltFar(loopBody);

                    // runtextpos = runtextend;
                    // return false;
                    BrFar(returnFalse);
                }
            }
        }

        private bool TryGenerateSimplifiedGo(RegexNode node)
        {
            Debug.Assert(node.Type == RegexNode.Capture, "Every generated tree should begin with a capture node");
            Debug.Assert(node.ChildCount() == 1, "Capture nodes should have one child");

            // RightToLeft is rare and not worth adding a lot of custom code to handle in this path.
            if ((node.Options & RegexOptions.RightToLeft) != 0)
            {
                return false;
            }

            // Skip the Capture node. We handle the implicit root capture specially.
            node = node.Child(0);
            if (!node.SupportsSimplifiedCodeGenerationImplementation())
            {
                return false;
            }

            // We've determined that the RegexNode can be handled with this optimized path.  Generate the code.
#if DEBUG
            if ((_options & RegexOptions.Debug) != 0)
            {
                Debug.WriteLine("Using optimized non-backtracking code gen.");
            }
#endif

            // In some limited cases, FindFirstChar will only return true if it successfully matched the whole thing.
            // This is the case, in particular, for strings.  We can special case these to do essentially nothing
            // in Go other than emit the capture.
            if (!IsCaseInsensitive(node)) // FindFirstChar may not be 100% accurate on casing in all cultures
            {
                switch (node.Type)
                {
                    case RegexNode.Multi:
                    case RegexNode.Notone:
                    case RegexNode.One:
                    case RegexNode.Set:
                        // base.Capture(0, base.runtextpos, base.runtextpos + node.Str.Length);
                        // base.runtextpos = base.runtextpos + node.Str.Length;
                        // return;
                        Ldthis();
                        Dup();
                        Ldc(0);
                        Ldthisfld(s_runtextposField);
                        Dup();
                        Ldc(node.Type == RegexNode.Multi ? node.Str!.Length : 1);
                        Add();
                        Call(s_captureMethod);
                        Ldthisfld(s_runtextposField);
                        Ldc(node.Type == RegexNode.Multi ? node.Str!.Length : 1);
                        Add();
                        Stfld(s_runtextposField);
                        Ret();
                        return true;
                }
            }

            // Declare some locals.
            LocalBuilder runtextLocal = DeclareString();
            LocalBuilder originalruntextposLocal = DeclareInt32();
            LocalBuilder runtextposLocal = DeclareInt32();
            LocalBuilder textSpanLocal = DeclareReadOnlySpanChar();
            LocalBuilder runtextendLocal = DeclareInt32();
            Label stopSuccessLabel = DefineLabel();
            Label doneLabel = DefineLabel();
            Label originalDoneLabel = doneLabel;
            if (_hasTimeout)
            {
                _loopTimeoutCounterLocal = DeclareInt32();
            }

            // CultureInfo culture = CultureInfo.CurrentCulture; // only if the whole expression or any subportion is ignoring case, and we're not using invariant
            InitializeCultureForGoIfNecessary();

            // string runtext = this.runtext;
            // int runtextend = this.runtextend;
            Mvfldloc(s_runtextField, runtextLocal);
            Mvfldloc(s_runtextendField, runtextendLocal);

            // int runtextpos = this.runtextpos;
            // int originalruntextpos = this.runtextpos;
            Ldthisfld(s_runtextposField);
            Stloc(runtextposLocal);
            Ldloc(runtextposLocal);
            Stloc(originalruntextposLocal);

            // The implementation tries to use const indexes into the span wherever possible, which we can do
            // in all places except for variable-length loops.  For everything else, we know at any point in
            // the regex exactly how far into it we are, and we can use that to index into the span created
            // at the beginning of the routine to begin at exactly where we're starting in the input.  For
            // variable-length loops, we index at this textSpanPos + i, and then after the loop we slice the input
            // by i so that this position is still accurate for everything after it.
            int textSpanPos = 0;
            LoadTextSpanLocal();

            // Emit the code for all nodes in the tree.
            bool expressionHasCaptures = (node.Options & RegexNode.HasCapturesFlag) != 0;
            EmitNode(node);

            // Success:
            // runtextpos += textSpanPos;
            // this.runtextpos = runtextpos;
            // Capture(0, originalruntextpos, runtextpos);
            MarkLabel(stopSuccessLabel);
            Ldthis();
            Ldloc(runtextposLocal);
            if (textSpanPos > 0)
            {
                Ldc(textSpanPos);
                Add();
                Stloc(runtextposLocal);
                Ldloc(runtextposLocal);
            }
            Stfld(s_runtextposField);
            Ldthis();
            Ldc(0);
            Ldloc(originalruntextposLocal);
            Ldloc(runtextposLocal);
            Call(s_captureMethod);

            // If the graph contained captures, undo any remaining to handle failed matches.
            if (expressionHasCaptures)
            {
                // while (Crawlpos() != 0) Uncapture();

                Label finalReturnLabel = DefineLabel();
                Br(finalReturnLabel);

                MarkLabel(originalDoneLabel);
                Label condition = DefineLabel();
                Label body = DefineLabel();
                Br(condition);
                MarkLabel(body);
                Ldthis();
                Call(s_uncaptureMethod);
                MarkLabel(condition);
                Ldthis();
                Call(s_crawlposMethod);
                Brtrue(body);

                // Done:
                MarkLabel(finalReturnLabel);
            }
            else
            {
                // Done:
                MarkLabel(originalDoneLabel);
            }

            // return;
            Ret();

            // Generated code successfully with non-backtracking implementation.
            return true;

            static bool IsCaseInsensitive(RegexNode node) => (node.Options & RegexOptions.IgnoreCase) != 0;

            // Creates a span for runtext starting at runtextpos until this.runtextend.
            void LoadTextSpanLocal()
            {
                // textSpan = runtext.AsSpan(runtextpos, this.runtextend - runtextpos);
                Ldloc(runtextLocal);
                Ldloc(runtextposLocal);
                Ldloc(runtextendLocal);
                Ldloc(runtextposLocal);
                Sub();
                Call(s_stringAsSpanIntIntMethod);
                Stloc(textSpanLocal);
            }

            // Emits the sum of a constant and a value from a local.
            void EmitSum(int constant, LocalBuilder? local)
            {
                if (local == null)
                {
                    Ldc(constant);
                }
                else if (constant == 0)
                {
                    Ldloc(local);
                }
                else
                {
                    Ldloc(local);
                    Ldc(constant);
                    Add();
                }
            }

            // Emits a check that the span is large enough at the currently known static position to handle the required additional length.
            void EmitSpanLengthCheck(int requiredLength, LocalBuilder? dynamicRequiredLength = null)
            {
                // if ((uint)(textSpanPos + requiredLength + dynamicRequiredLength - 1) >= (uint)textSpan.Length) goto Done;
                Debug.Assert(requiredLength > 0);
                EmitSum(textSpanPos + requiredLength - 1, dynamicRequiredLength);
                Ldloca(textSpanLocal);
                Call(s_spanGetLengthMethod);
                BgeUnFar(doneLabel);
            }

            // Emits code to get ref textSpan[textSpanPos]
            void EmitTextSpanOffset()
            {
                Ldloc(textSpanLocal);
                Call(s_memoryMarshalGetReference);
                if (textSpanPos > 0)
                {
                    Ldc(textSpanPos * sizeof(char));
                    Add();
                }
            }

            // Adds the value of textSpanPos into the runtextpos local, slices textspan by the corresponding amount,
            // and zeros out textSpanPos.
            void TransferTextSpanPosToRunTextPos()
            {
                if (textSpanPos > 0)
                {
                    // runtextpos += textSpanPos;
                    Ldloc(runtextposLocal);
                    Ldc(textSpanPos);
                    Add();
                    Stloc(runtextposLocal);

                    // textSpan = textSpan.Slice(textSpanPos);
                    Ldloca(textSpanLocal);
                    Ldc(textSpanPos);
                    Call(s_spanSliceIntMethod);
                    Stloc(textSpanLocal);

                    // textSpanPos = 0;
                    textSpanPos = 0;
                }
            }

            // Emits the code for an atomic alternate, one that once a branch successfully matches is non-backtracking into it.
            // This amounts to generating the code for each branch, with failures in a branch resetting state to what it was initially
            // and then jumping to the next branch. We don't need to worry about uncapturing, because capturing is only allowed for the
            // implicit capture that happens for the whole match at the end.
            void EmitAtomicAlternate(RegexNode node)
            {
                // int startingTextSpanPos = textSpanPos;
                // int startingRunTextPos = runtextpos;
                //
                // Branch0(); // jumps to NextBranch1 on failure
                // goto Success;
                //
                // NextBranch1:
                // runtextpos = originalruntextpos;
                // textSpan = originalTextSpan;
                // Branch1(); // jumps to NextBranch2 on failure
                // goto Success;
                //
                // ...
                //
                // NextBranchN:
                // runtextpos = startingRunTextPos;
                // textSpan = this.runtext.AsSpan(runtextpos, this.runtextend - runtextpos);
                // textSpanPos = startingTextSpanPos;
                // BranchN(); // jumps to Done on failure

                // Save off runtextpos.  We'll need to reset this each time a branch fails.
                LocalBuilder startingRunTextPos = DeclareInt32();
                Ldloc(runtextposLocal);
                Stloc(startingRunTextPos);
                int startingTextSpanPos = textSpanPos;

                // If the alternation's branches contain captures, save off the relevant
                // state.  Note that this is only about subexpressions within the alternation,
                // as the alternation is atomic, so we're not concerned about captures after
                // the alternation.
                LocalBuilder? startingCrawlpos = null;
                if ((node.Options & RegexNode.HasCapturesFlag) != 0)
                {
                    startingCrawlpos = DeclareInt32();
                    Ldthis();
                    Call(s_crawlposMethod);
                    Stloc(startingCrawlpos);
                }

                // Label to jump to when any branch completes successfully.
                Label doneAlternate = DefineLabel();

                // A failure in a branch other than the last should jump to the next
                // branch, not to the final done.
                Label originalDoneLabel = doneLabel;

                int childCount = node.ChildCount();
                for (int i = 0; i < childCount - 1; i++)
                {
                    Label nextBranch = DefineLabel();
                    doneLabel = nextBranch;

                    // Emit the code for each branch.
                    EmitNode(node.Child(i));

                    // If we get here in the generated code, the branch completed successfully.
                    // Before jumping to the end, we need to zero out textSpanPos, so that no
                    // matter what the value is after the branch, whatever follows the alternate
                    // will see the same textSpanPos.
                    TransferTextSpanPosToRunTextPos();
                    BrFar(doneAlternate);

                    // Reset state for next branch and loop around to generate it.  This includes
                    // setting runtextpos back to what it was at the beginning of the alternation,
                    // updating textSpan to be the full length it was, and if there's a capture that
                    // needs to be reset, uncapturing it.
                    MarkLabel(nextBranch);
                    Ldloc(startingRunTextPos);
                    Stloc(runtextposLocal);
                    LoadTextSpanLocal();
                    textSpanPos = startingTextSpanPos;
                    if (startingCrawlpos != null)
                    {
                        EmitUncaptureUntil(startingCrawlpos);
                    }
                }

                // If the final branch fails, that's like any other failure, and we jump to
                // done (unless we have captures we need to unwind first, in which case we uncapture
                // them and then jump to done).
                if (startingCrawlpos != null)
                {
                    Label uncapture = DefineLabel();
                    doneLabel = uncapture;
                    EmitNode(node.Child(childCount - 1));
                    doneLabel = originalDoneLabel;
                    TransferTextSpanPosToRunTextPos();
                    Br(doneAlternate);

                    MarkLabel(uncapture);
                    EmitUncaptureUntil(startingCrawlpos);
                    BrFar(doneLabel);
                }
                else
                {
                    doneLabel = originalDoneLabel;
                    EmitNode(node.Child(childCount - 1));
                    TransferTextSpanPosToRunTextPos();
                }

                // Successfully completed the alternate.
                MarkLabel(doneAlternate);
                Debug.Assert(textSpanPos == 0);
            }

            // Emits the code for a Capture node.
            void EmitCapture(RegexNode node, RegexNode? subsequent = null)
            {
                Debug.Assert(node.N == -1);
                LocalBuilder startingRunTextPos = DeclareInt32();

                // Get the capture number.  This needs to be kept
                // in sync with MapCapNum in RegexWriter.
                Debug.Assert(node.Type == RegexNode.Capture);
                Debug.Assert(node.N == -1, "Currently only support capnum, not uncapnum");
                int capnum = node.M;
                if (capnum != -1 && _code!.Caps != null)
                {
                    capnum = (int)_code.Caps[capnum]!;
                }

                // runtextpos += textSpanPos;
                // textSpan = textSpan.Slice(textSpanPos);
                // startingRunTextPos = runtextpos;
                TransferTextSpanPosToRunTextPos();
                Ldloc(runtextposLocal);
                Stloc(startingRunTextPos);

                // Emit child node.
                EmitNode(node.Child(0), subsequent);

                // runtextpos += textSpanPos;
                // textSpan = textSpan.Slice(textSpanPos);
                // Capture(capnum, startingRunTextPos, runtextpos);
                TransferTextSpanPosToRunTextPos();
                Ldthis();
                Ldc(capnum);
                Ldloc(startingRunTextPos);
                Ldloc(runtextposLocal);
                Call(s_captureMethod);
            }

            // Emits code to unwind the capture stack until the crawl position specified in the provided local.
            void EmitUncaptureUntil(LocalBuilder startingCrawlpos)
            {
                Debug.Assert(startingCrawlpos != null);

                // while (Crawlpos() != startingCrawlpos) Uncapture();
                Label condition = DefineLabel();
                Label body = DefineLabel();
                Br(condition);
                MarkLabel(body);
                Ldthis();
                Call(s_uncaptureMethod);
                MarkLabel(condition);
                Ldthis();
                Call(s_crawlposMethod);
                Ldloc(startingCrawlpos);
                Bne(body);
            }

            // Emits the code to handle a positive lookahead assertion.
            void EmitPositiveLookaheadAssertion(RegexNode node)
            {
                // Save off runtextpos.  We'll need to reset this upon successful completion of the lookahead.
                LocalBuilder startingRunTextPos = DeclareInt32();
                Ldloc(runtextposLocal);
                Stloc(startingRunTextPos);
                int startingTextSpanPos = textSpanPos;

                // Emit the child.
                EmitNode(node.Child(0));

                // After the child completes successfully, reset the text positions.
                // Do not reset captures, which persist beyond the lookahead.
                Ldloc(startingRunTextPos);
                Stloc(runtextposLocal);
                LoadTextSpanLocal();
                textSpanPos = startingTextSpanPos;
            }

            // Emits the code to handle a negative lookahead assertion.
            void EmitNegativeLookaheadAssertion(RegexNode node)
            {
                // Save off runtextpos.  We'll need to reset this upon successful completion of the lookahead.
                LocalBuilder startingRunTextPos = DeclareInt32();
                Ldloc(runtextposLocal);
                Stloc(startingRunTextPos);
                int startingTextSpanPos = textSpanPos;

                Label originalDoneLabel = doneLabel;
                Label negativeLookaheadDoneLabel = DefineLabel();
                doneLabel = negativeLookaheadDoneLabel;

                // Emit the child.
                EmitNode(node.Child(0));

                // If the generated code ends up here, it matched the lookahead, which actually
                // means failure for a _negative_ lookahead, so we need to jump to the original done.
                BrFar(originalDoneLabel);

                // Failures (success for a negative lookahead) jump here.
                MarkLabel(negativeLookaheadDoneLabel);
                Debug.Assert(doneLabel == negativeLookaheadDoneLabel);
                doneLabel = originalDoneLabel;

                // After the child completes in failure (success for negative lookahead), reset the text positions.
                Ldloc(startingRunTextPos);
                Stloc(runtextposLocal);
                LoadTextSpanLocal();
                textSpanPos = startingTextSpanPos;
            }

            // Emits the code for the node.
            void EmitNode(RegexNode node, RegexNode? subsequent = null, bool emitLengthChecksIfRequired = true)
            {
                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    StackHelper.CallOnEmptyStack(EmitNode, node, subsequent, emitLengthChecksIfRequired);
                    return;
                }

                switch (node.Type)
                {
                    case RegexNode.One:
                    case RegexNode.Notone:
                    case RegexNode.Set:
                        EmitSingleChar(node, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Boundary:
                    case RegexNode.NonBoundary:
                    case RegexNode.ECMABoundary:
                    case RegexNode.NonECMABoundary:
                        EmitBoundary(node);
                        break;

                    case RegexNode.Beginning:
                    case RegexNode.Start:
                    case RegexNode.Bol:
                    case RegexNode.Eol:
                    case RegexNode.End:
                    case RegexNode.EndZ:
                        EmitAnchors(node);
                        break;

                    case RegexNode.Multi:
                        EmitMultiChar(node, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Oneloopatomic:
                    case RegexNode.Notoneloopatomic:
                    case RegexNode.Setloopatomic:
                        EmitSingleCharAtomicLoop(node);
                        break;

                    case RegexNode.Loop:
                        EmitAtomicNodeLoop(node);
                        break;

                    case RegexNode.Onelazy:
                    case RegexNode.Notonelazy:
                    case RegexNode.Setlazy:
                    case RegexNode.Lazyloop:
                        EmitLazy(node, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Atomic:
                        EmitNode(node.Child(0), subsequent);
                        break;

                    case RegexNode.Alternate:
                        EmitAtomicAlternate(node);
                        break;

                    case RegexNode.Oneloop:
                    case RegexNode.Notoneloop:
                    case RegexNode.Setloop:
                        EmitSingleCharLoop(node, subsequent, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Concatenate:
                        EmitConcatenation(node, subsequent, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Capture:
                        EmitCapture(node, subsequent);
                        break;

                    case RegexNode.Require:
                        EmitPositiveLookaheadAssertion(node);
                        break;

                    case RegexNode.Prevent:
                        EmitNegativeLookaheadAssertion(node);
                        break;

                    case RegexNode.Nothing:
                        BrFar(doneLabel);
                        break;

                    case RegexNode.Empty:
                        // Emit nothing.
                        break;

                    case RegexNode.UpdateBumpalong:
                        EmitUpdateBumpalong();
                        break;

                    default:
                        Debug.Fail($"Unexpected node type: {node.Type}");
                        break;
                }
            }

            // Emits the code to handle updating base.runtextpos to runtextpos in response to
            // an UpdateBumpalong node.  This is used when we want to inform the scan loop that
            // it should bump from this location rather than from the original location.
            void EmitUpdateBumpalong()
            {
                // base.runtextpos = runtextpos;
                TransferTextSpanPosToRunTextPos();
                Ldthis();
                Ldloc(runtextposLocal);
                Stfld(s_runtextposField);
            }

            // Emits code for a concatenation
            void EmitConcatenation(RegexNode node, RegexNode? subsequent, bool emitLengthChecksIfRequired)
            {
                int childCount = node.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    if (emitLengthChecksIfRequired && node.TryGetJoinableLengthCheckChildRange(i, out int requiredLength, out int exclusiveEnd))
                    {
                        EmitSpanLengthCheck(requiredLength);
                        for (; i < exclusiveEnd; i++)
                        {
                            EmitNode(node.Child(i), i + 1 < childCount ? node.Child(i + 1) : subsequent, emitLengthChecksIfRequired: false);
                        }

                        i--;
                        continue;
                    }

                    EmitNode(node.Child(i), i + 1 < childCount ? node.Child(i + 1) : subsequent);
                }
            }

            // Emits the code to handle a single-character match.
            void EmitSingleChar(RegexNode node, bool emitLengthCheck = true, LocalBuilder? offset = null)
            {
                // This only emits a single check, but it's called from the looping constructs in a loop
                // to generate the code for a single check, so we check for each "family" (one, notone, set)
                // rather than only for the specific single character nodes.

                // if ((uint)(textSpanPos + offset) >= textSpan.Length || textSpan[textSpanPos + offset] != ch) goto Done;
                if (emitLengthCheck)
                {
                    EmitSpanLengthCheck(1, offset);
                }
                Ldloca(textSpanLocal);
                EmitSum(textSpanPos, offset);
                Call(s_spanGetItemMethod);
                LdindU2();
                if (node.IsSetFamily)
                {
                    EmitMatchCharacterClass(node.Str!, IsCaseInsensitive(node));
                    BrfalseFar(doneLabel);
                }
                else
                {
                    if (IsCaseInsensitive(node))
                    {
                        CallToLower();
                    }
                    Ldc(node.Ch);
                    if (node.IsOneFamily)
                    {
                        BneFar(doneLabel);
                    }
                    else // IsNotoneFamily
                    {
                        BeqFar(doneLabel);
                    }
                }

                textSpanPos++;
            }

            // Emits the code to handle a boundary check on a character.
            void EmitBoundary(RegexNode node)
            {
                // if (!IsBoundary(runtextpos + textSpanPos, this.runtextbeg, this.runtextend)) goto doneLabel;
                Ldthis();
                Ldloc(runtextposLocal);
                if (textSpanPos > 0)
                {
                    Ldc(textSpanPos);
                    Add();
                }
                Ldthisfld(s_runtextbegField);
                Ldloc(runtextendLocal);
                switch (node.Type)
                {
                    case RegexNode.Boundary:
                        Call(s_isBoundaryMethod);
                        BrfalseFar(doneLabel);
                        break;

                    case RegexNode.NonBoundary:
                        Call(s_isBoundaryMethod);
                        BrtrueFar(doneLabel);
                        break;

                    case RegexNode.ECMABoundary:
                        Call(s_isECMABoundaryMethod);
                        BrfalseFar(doneLabel);
                        break;

                    default:
                        Debug.Assert(node.Type == RegexNode.NonECMABoundary);
                        Call(s_isECMABoundaryMethod);
                        BrtrueFar(doneLabel);
                        break;
                }
            }

            // Emits the code to handle various anchors.
            void EmitAnchors(RegexNode node)
            {
                Debug.Assert(textSpanPos >= 0);
                switch (node.Type)
                {
                    case RegexNode.Beginning:
                    case RegexNode.Start:
                        if (textSpanPos > 0)
                        {
                            // If we statically know we've already matched part of the regex, there's no way we're at the
                            // beginning or start, as we've already progressed past it.
                            BrFar(doneLabel);
                        }
                        else
                        {
                            // if (runtextpos > this.runtextbeg/start) goto doneLabel;
                            Ldloc(runtextposLocal);
                            Ldthisfld(node.Type == RegexNode.Beginning ? s_runtextbegField : s_runtextstartField);
                            BneFar(doneLabel);
                        }
                        break;

                    case RegexNode.Bol:
                        if (textSpanPos > 0)
                        {
                            // if (textSpan[textSpanPos - 1] != '\n') goto doneLabel;
                            Ldloca(textSpanLocal);
                            Ldc(textSpanPos - 1);
                            Call(s_spanGetItemMethod);
                            LdindU2();
                            Ldc('\n');
                            BneFar(doneLabel);
                        }
                        else
                        {
                            // We can't use our textSpan in this case, because we'd need to access textSpan[-1], so we access the runtext field directly:
                            // if (runtextpos > this.runtextbeg && this.runtext[runtextpos - 1] != '\n') goto doneLabel;
                            Label success = DefineLabel();
                            Ldloc(runtextposLocal);
                            Ldthisfld(s_runtextbegField);
                            Ble(success);
                            Ldthisfld(s_runtextField);
                            Ldloc(runtextposLocal);
                            Ldc(1);
                            Sub();
                            Call(s_stringGetCharsMethod);
                            Ldc('\n');
                            BneFar(doneLabel);
                            MarkLabel(success);
                        }
                        break;

                    case RegexNode.End:
                        // if (textSpanPos < textSpan.Length) goto doneLabel;
                        Ldc(textSpanPos);
                        Ldloca(textSpanLocal);
                        Call(s_spanGetLengthMethod);
                        BltUnFar(doneLabel);
                        break;

                    case RegexNode.EndZ:
                        // if (textSpanPos < textSpan.Length - 1) goto doneLabel;
                        Ldc(textSpanPos);
                        Ldloca(textSpanLocal);
                        Call(s_spanGetLengthMethod);
                        Ldc(1);
                        Sub();
                        BltFar(doneLabel);
                        goto case RegexNode.Eol;

                    case RegexNode.Eol:
                        // if (textSpanPos < textSpan.Length && textSpan[textSpanPos] != '\n') goto doneLabel;
                        {
                            Label success = DefineLabel();
                            Ldc(textSpanPos);
                            Ldloca(textSpanLocal);
                            Call(s_spanGetLengthMethod);
                            BgeUn(success);
                            Ldloca(textSpanLocal);
                            Ldc(textSpanPos);
                            Call(s_spanGetItemMethod);
                            LdindU2();
                            Ldc('\n');
                            BneFar(doneLabel);
                            MarkLabel(success);
                        }
                        break;
                }
            }

            // Emits the code to handle a multiple-character match.
            void EmitMultiChar(RegexNode node, bool emitLengthCheck = true)
            {
                bool caseInsensitive = IsCaseInsensitive(node);

                // If the multi string's length exceeds the maximum length we want to unroll, instead generate a call to StartsWith.
                // Each character that we unroll results in code generation that increases the size of both the IL and the resulting asm,
                // and with a large enough string, that can cause significant overhead as well as even risk stack overflow due to
                // having an obscenely long method.  Such long string lengths in a pattern are generally quite rare.  However, we also
                // want to unroll for shorter strings, because the overhead of invoking StartsWith instead of doing a few simple
                // inline comparisons is very measurable, especially if we're doing a culture-sensitive comparison and StartsWith
                // accesses CultureInfo.CurrentCulture on each call.  We need to be cognizant not only of the cost if the whole
                // string matches, but also the cost when the comparison fails early on, and thus we pay for the call overhead
                // but don't reap the benefits of all the vectorization StartsWith can do.
                const int MaxUnrollLength = 64;
                if (!caseInsensitive && // StartsWith(..., XxIgnoreCase) won't necessarily be the same as char-by-char comparison
                    node.Str!.Length > MaxUnrollLength)
                {
                    // if (!textSpan.Slice(textSpanPos).StartsWith("...") goto doneLabel;
                    Ldloca(textSpanLocal);
                    Ldc(textSpanPos);
                    Call(s_spanSliceIntMethod);
                    Ldstr(node.Str);
                    Call(s_stringAsSpanMethod);
                    Call(s_spanStartsWith);
                    BrfalseFar(doneLabel);
                    textSpanPos += node.Str.Length;
                    return;
                }

                // Emit the length check for the whole string.  If the generated code gets past this point,
                // we know the span is at least textSpanPos + s.Length long.
                ReadOnlySpan<char> s = node.Str;
                if (emitLengthCheck)
                {
                    EmitSpanLengthCheck(s.Length);
                }

                // If we're doing a case-insensitive comparison, we need to lower case each character,
                // so we just go character-by-character.  But if we're not, we try to process multiple
                // characters at a time; this is helpful not only for throughput but also in reducing
                // the amount of IL and asm that results from this unrolling. This optimization
                // is subject to endianness issues if the generated code is used on a machine with a
                // different endianness, but that's not a concern when the code is emitted by the
                // same process that then uses it.
                if (!caseInsensitive)
                {
                    // On 64-bit, process 4 characters at a time until the string isn't at least 4 characters long.
                    if (IntPtr.Size == 8)
                    {
                        const int CharsPerInt64 = 4;
                        while (s.Length >= CharsPerInt64)
                        {
                            // if (Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), textSpanPos)) != value) goto doneLabel;
                            EmitTextSpanOffset();
                            Unaligned(1);
                            LdindI8();
                            LdcI8(MemoryMarshal.Read<long>(MemoryMarshal.AsBytes(s)));
                            BneFar(doneLabel);
                            textSpanPos += CharsPerInt64;
                            s = s.Slice(CharsPerInt64);
                        }
                    }

                    // Of what remains, process 2 characters at a time until the string isn't at least 2 characters long.
                    const int CharsPerInt32 = 2;
                    while (s.Length >= CharsPerInt32)
                    {
                        // if (Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref MemoryMarshal.GetReference(textSpan), textSpanPos)) != value) goto doneLabel;
                        EmitTextSpanOffset();
                        Unaligned(1);
                        LdindI4();
                        Ldc(MemoryMarshal.Read<int>(MemoryMarshal.AsBytes(s)));
                        BneFar(doneLabel);
                        textSpanPos += CharsPerInt32;
                        s = s.Slice(CharsPerInt32);
                    }
                }

                // Finally, process all of the remaining characters one by one.
                for (int i = 0; i < s.Length; i++)
                {
                    // if (s[i] != textSpan[textSpanPos++]) goto doneLabel;
                    EmitTextSpanOffset();
                    textSpanPos++;
                    LdindU2();
                    if (caseInsensitive)
                    {
                        CallToLower();
                    }
                    Ldc(s[i]);
                    BneFar(doneLabel);
                }
            }

            // Emits the code to handle a backtracking, single-character loop.
            void EmitSingleCharLoop(RegexNode node, RegexNode? subsequent = null, bool emitLengthChecksIfRequired = true)
            {
                // If this is actually a repeater, emit that instead; no backtracking necessary.
                if (node.M == node.N)
                {
                    EmitSingleCharFixedRepeater(node, emitLengthChecksIfRequired);
                    return;
                }

                Debug.Assert(node.M < node.N);
                Label backtrackingLabel = DefineLabel();
                Label endLoop = DefineLabel();
                LocalBuilder startingPos = DeclareInt32();
                LocalBuilder endingPos = DeclareInt32();
                LocalBuilder crawlPos = DeclareInt32();

                // We're about to enter a loop, so ensure our text position is 0.
                TransferTextSpanPosToRunTextPos();

                // int startingPos = runtextpos;
                // Single char atomic loop
                // int endingPos = runtextpos;
                // int crawlPos = base.Crawlpos();
                // startingPos += node.M;
                // goto endLoop;
                Ldloc(runtextposLocal);
                Stloc(startingPos);
                EmitSingleCharAtomicLoop(node);
                TransferTextSpanPosToRunTextPos();
                Ldloc(runtextposLocal);
                Stloc(endingPos);
                Ldthis();
                Call(s_crawlposMethod);
                Stloc(crawlPos);
                if (node.M > 0)
                {
                    Ldloc(startingPos);
                    Ldc(node.M);
                    Add();
                    Stloc(startingPos);
                }
                Br(endLoop);

                // Backtracking:
                // if (startingPos >= endingPos) goto doneLabel;
                MarkLabel(backtrackingLabel);
                Ldloc(startingPos);
                Ldloc(endingPos);
                BgeFar(doneLabel);
                doneLabel = backtrackingLabel; // leave set to the backtracking label for all subsequent nodes

                // while (base.Crawlpos() != crawlPos) Uncapture();
                if (expressionHasCaptures)
                {
                    // Uncapture any captures if the expression has any.  It's possible the captures it has
                    // are before this node, in which case this is wasted effort, but still functionally correct.
                    EmitUncaptureUntil(crawlPos);
                }

                if (subsequent?.FindStartingCharacter() is char subsequentCharacter)
                {
                    // endingPos = runtext.LastIndexOf(subsequentCharacter, endingPos - 1, endingPos - startingPos);
                    // if (endingPos < 0)
                    // {
                    //     goto doneLabel;
                    // }
                    Ldloc(runtextLocal);
                    Ldc(subsequentCharacter);
                    Ldloc(endingPos);
                    Ldc(1);
                    Sub();
                    Ldloc(endingPos);
                    Ldloc(startingPos);
                    Sub();
                    Call(s_stringLastIndexOfCharIntInt);
                    Stloc(endingPos);
                    Ldloc(endingPos);
                    Ldc(0);
                    BltFar(doneLabel);
                }
                else
                {
                    // endingPos--;
                    Ldloc(endingPos);
                    Ldc(1);
                    Sub();
                    Stloc(endingPos);
                }

                // runtextpos = endingPos;
                Ldloc(endingPos);
                Stloc(runtextposLocal);

                // textspan = runtext.AsSpan(runtextpos, runtextend - runtextpos);
                LoadTextSpanLocal();

                MarkLabel(endLoop);
            }

            void EmitLazy(RegexNode node, bool emitLengthChecksIfRequired = true)
            {
                bool isSingleChar = node.IsOneFamily || node.IsNotoneFamily || node.IsSetFamily;

                // Emit the min iterations as a repeater.  Any failures here don't necessitate backtracking,
                // as the lazy itself failed to match.
                if (node.M > 0)
                {
                    if (isSingleChar)
                    {
                        EmitSingleCharFixedRepeater(node, emitLengthChecksIfRequired);
                    }
                    else
                    {
                        EmitNodeRepeater(node);
                    }
                }

                // If the whole thing was actually that repeater, we're done. Similarly, if this is actually an atomic
                // lazy loop, nothing will ever backtrack into this node, so we never need to iterate more than the minimum.
                if (node.M == node.N || node.Next is { Type: RegexNode.Atomic })
                {
                    return;
                }

                Debug.Assert(node.M < node.N);

                // We now need to match one character at a time, each time allowing the remainder of the expression
                // to try to match, and only matching another character if the subsequent expression fails to match.

                // We're about to enter a loop, so ensure our text position is 0.
                TransferTextSpanPosToRunTextPos();

                // If the loop isn't unbounded, track the number of iterations and the max number to allow.
                LocalBuilder? iterationCount = null;
                LocalBuilder? maxIterations = null;
                if (node.N != int.MaxValue)
                {
                    // int iterationCount = 0;
                    // int maxIterations = node.N - node.M;
                    iterationCount = DeclareInt32();
                    maxIterations = DeclareInt32();
                    Ldc(0);
                    Stloc(iterationCount);
                    Ldc(node.N - node.M);
                    Stloc(maxIterations);
                }

                // Track the current crawl position.  Upon backtracking, we'll unwind any captures beyond this point.
                LocalBuilder? crawlPos = null;
                if (expressionHasCaptures)
                {
                    // int crawlPos = base.Crawlpos();
                    crawlPos = DeclareInt32();
                    Ldthis();
                    Call(s_crawlposMethod);
                    Stloc(crawlPos);
                }

                // Track the current runtextpos.  Each time we backtrack, we'll reset to the stored position, which
                // is also incremented each time we match another character in the loop.
                // int nextPos = runtextpos;
                LocalBuilder nextPos = DeclareInt32();
                Ldloc(runtextposLocal);
                Stloc(nextPos);

                // Skip the backtracking section for the initial subsequent matching.  We've already matched the
                // minimum number of iterations, which means we can successfully match with zero additional iterations.
                // goto endLoopLabel;
                Label endLoopLabel = DefineLabel();
                BrFar(endLoopLabel);

                // Backtracking section. Subsequent failures will jump to here.
                Label backtrackingLabel = DefineLabel();
                MarkLabel(backtrackingLabel);

                // Uncapture any captures if the expression has any.  It's possible the captures it has
                // are before this node, in which case this is wasted effort, but still functionally correct.
                if (expressionHasCaptures)
                {
                    EmitUncaptureUntil(crawlPos!);
                }

                // If there's a max number of iterations, see if we've exceeded the maximum number of characters
                // to match.  If we haven't, increment the iteration count.
                if (maxIterations is not null)
                {
                    // if (iterationCount >= maxIterations) goto doneLabel;
                    Ldloc(iterationCount!);
                    Ldloc(maxIterations);
                    BgeFar(doneLabel);

                    // iterationCount++;
                    Ldloc(iterationCount!);
                    Ldc(1);
                    Add();
                    Stloc(iterationCount!);
                }

                // Now match the next character in the lazy loop.  We need to reset the runtextpos to the position
                // just after the last character in this loop was matched, and we need to store the resulting position
                // for the next time we backtrack.

                // runtextpos = nextPos;
                // MatchSingleChar();
                // nextpos = runtextpos;
                Ldloc(nextPos);
                Stloc(runtextposLocal);
                LoadTextSpanLocal();
                if (isSingleChar)
                {
                    EmitSingleChar(node);
                }
                else
                {
                    EmitNode(node.Child(0));
                }
                TransferTextSpanPosToRunTextPos();
                Ldloc(runtextposLocal);
                Stloc(nextPos);

                // Update the done label for everything that comes after this node.  This is done after we emit the single char
                // matching, as that failing indicates the loop itself has failed to match.
                Label originalDoneLabel = doneLabel;
                doneLabel = backtrackingLabel; // leave set to the backtracking label for all subsequent nodes

                MarkLabel(endLoopLabel);

                // We explicitly do not reset doneLabel back to originalDoneLabel.
                // It's left pointing to the backtracking label for everything subsequent in the expression.
            }

            // Emits the code to handle a loop (repeater) with a fixed number of iterations.
            // RegexNode.M is used for the number of iterations; RegexNode.N is ignored.
            void EmitSingleCharFixedRepeater(RegexNode node, bool emitLengthChecksIfRequired = true)
            {
                int iterations = node.M;

                if (iterations == 0)
                {
                    // No iterations, nothing to do.
                    return;
                }

                // if ((uint)(textSpanPos + iterations - 1) >= (uint)textSpan.Length) goto doneLabel;
                if (emitLengthChecksIfRequired)
                {
                    EmitSpanLengthCheck(iterations);
                }

                // Arbitrary limit for unrolling vs creating a loop.  We want to balance size in the generated
                // code with other costs, like the (small) overhead of slicing to create the temp span to iterate.
                const int MaxUnrollSize = 16;

                if (iterations <= MaxUnrollSize)
                {
                    // if (textSpan[textSpanPos] != c1 ||
                    //     textSpan[textSpanPos + 1] != c2 ||
                    //     ...)
                    //       goto doneLabel;
                    for (int i = 0; i < iterations; i++)
                    {
                        EmitSingleChar(node, emitLengthCheck: false);
                    }
                }
                else
                {
                    // ReadOnlySpan<char> tmp = textSpan.Slice(textSpanPos, iterations);
                    // for (int i = 0; i < tmp.Length; i++)
                    // {
                    //     TimeoutCheck();
                    //     if (tmp[i] != ch) goto Done;
                    // }
                    // textSpanPos += iterations;

                    Label conditionLabel = DefineLabel();
                    Label bodyLabel = DefineLabel();

                    using RentedLocalBuilder spanLocal = RentReadOnlySpanCharLocal();
                    Ldloca(textSpanLocal);
                    Ldc(textSpanPos);
                    Ldc(iterations);
                    Call(s_spanSliceIntIntMethod);
                    Stloc(spanLocal);

                    using RentedLocalBuilder iterationLocal = RentInt32Local();
                    Ldc(0);
                    Stloc(iterationLocal);
                    BrFar(conditionLabel);

                    MarkLabel(bodyLabel);
                    EmitTimeoutCheck();

                    LocalBuilder tmpTextSpanLocal = textSpanLocal; // we want EmitSingleChar to refer to this temporary
                    int tmpTextSpanPos = textSpanPos;
                    textSpanLocal = spanLocal;
                    textSpanPos = 0;
                    EmitSingleChar(node, emitLengthCheck: false, offset: iterationLocal);
                    textSpanLocal = tmpTextSpanLocal;
                    textSpanPos = tmpTextSpanPos;

                    Ldloc(iterationLocal);
                    Ldc(1);
                    Add();
                    Stloc(iterationLocal);

                    MarkLabel(conditionLabel);
                    Ldloc(iterationLocal);
                    Ldloca(spanLocal);
                    Call(s_spanGetLengthMethod);
                    BltFar(bodyLabel);

                    textSpanPos += iterations;
                }
            }

            // Emits the code to handle a loop (repeater) with a fixed number of iterations.
            // This is used both to handle the case of A{5, 5} where the min and max are equal,
            // and also to handle part of the case of A{3, 5}, where this method is called to
            // handle the A{3, 3} portion, and then remaining A{0, 2} is handled separately.
            void EmitNodeRepeater(RegexNode node)
            {
                int iterations = node.M;
                Debug.Assert(iterations > 0);

                if (iterations == 1)
                {
                    Debug.Assert(node.ChildCount() == 1);
                    EmitNode(node.Child(0));
                    return;
                }

                // Ensure textSpanPos is 0 prior to emitting the child.
                TransferTextSpanPosToRunTextPos();

                // for (int i = 0; i < iterations; i++)
                // {
                //     TimeoutCheck();
                //     if (textSpan[textSpanPos] != ch) goto Done;
                // }

                Label conditionLabel = DefineLabel();
                Label bodyLabel = DefineLabel();

                LocalBuilder iterationLocal = DeclareInt32();
                Ldc(0);
                Stloc(iterationLocal);
                BrFar(conditionLabel);

                MarkLabel(bodyLabel);
                EmitTimeoutCheck();

                Debug.Assert(node.ChildCount() == 1);
                Debug.Assert(textSpanPos == 0);
                EmitNode(node.Child(0));
                TransferTextSpanPosToRunTextPos();

                Ldloc(iterationLocal);
                Ldc(1);
                Add();
                Stloc(iterationLocal);

                MarkLabel(conditionLabel);
                Ldloc(iterationLocal);
                Ldc(iterations);
                BltFar(bodyLabel);
            }

            // Emits the code to handle a non-backtracking, variable-length loop around a single character comparison.
            void EmitSingleCharAtomicLoop(RegexNode node)
            {
                // If this is actually a repeater, emit that instead.
                if (node.M == node.N)
                {
                    EmitSingleCharFixedRepeater(node);
                    return;
                }

                // If this is actually an optional single char, emit that instead.
                if (node.M == 0 && node.N == 1)
                {
                    EmitAtomicSingleCharZeroOrOne(node);
                    return;
                }

                Debug.Assert(node.N > node.M);
                int minIterations = node.M;
                int maxIterations = node.N;

                using RentedLocalBuilder iterationLocal = RentInt32Local();

                Label atomicLoopDoneLabel = DefineLabel();

                Span<char> setChars = stackalloc char[5]; // max optimized by IndexOfAny today
                int numSetChars = 0;

                if (node.IsNotoneFamily &&
                    maxIterations == int.MaxValue &&
                    (!IsCaseInsensitive(node)))
                {
                    // For Notone, we're looking for a specific character, as everything until we find
                    // it is consumed by the loop.  If we're unbounded, such as with ".*" and if we're case-sensitive,
                    // we can use the vectorized IndexOf to do the search, rather than open-coding it.  The unbounded
                    // restriction is purely for simplicity; it could be removed in the future with additional code to
                    // handle the unbounded case.

                    // int i = textSpan.Slice(textSpanPos).IndexOf(char);
                    if (textSpanPos > 0)
                    {
                        Ldloca(textSpanLocal);
                        Ldc(textSpanPos);
                        Call(s_spanSliceIntMethod);
                    }
                    else
                    {
                        Ldloc(textSpanLocal);
                    }
                    Ldc(node.Ch);
                    Call(s_spanIndexOfChar);
                    Stloc(iterationLocal);

                    // if (i >= 0) goto atomicLoopDoneLabel;
                    Ldloc(iterationLocal);
                    Ldc(0);
                    BgeFar(atomicLoopDoneLabel);

                    // i = textSpan.Length - textSpanPos;
                    Ldloca(textSpanLocal);
                    Call(s_spanGetLengthMethod);
                    if (textSpanPos > 0)
                    {
                        Ldc(textSpanPos);
                        Sub();
                    }
                    Stloc(iterationLocal);
                }
                else if (node.IsSetFamily &&
                    maxIterations == int.MaxValue &&
                    !IsCaseInsensitive(node) &&
                    (numSetChars = RegexCharClass.GetSetChars(node.Str!, setChars)) != 0 &&
                    RegexCharClass.IsNegated(node.Str!))
                {
                    // If the set is negated and contains only a few characters (if it contained 1 and was negated, it would
                    // have been reduced to a Notone), we can use an IndexOfAny to find any of the target characters.
                    // As with the notoneloopatomic above, the unbounded constraint is purely for simplicity.
                    Debug.Assert(numSetChars > 1);

                    // int i = textSpan.Slice(textSpanPos).IndexOfAny(ch1, ch2, ...);
                    if (textSpanPos > 0)
                    {
                        Ldloca(textSpanLocal);
                        Ldc(textSpanPos);
                        Call(s_spanSliceIntMethod);
                    }
                    else
                    {
                        Ldloc(textSpanLocal);
                    }
                    switch (numSetChars)
                    {
                        case 2:
                            Ldc(setChars[0]);
                            Ldc(setChars[1]);
                            Call(s_spanIndexOfAnyCharChar);
                            break;

                        case 3:
                            Ldc(setChars[0]);
                            Ldc(setChars[1]);
                            Ldc(setChars[2]);
                            Call(s_spanIndexOfAnyCharCharChar);
                            break;

                        default:
                            Ldstr(setChars.Slice(0, numSetChars).ToString());
                            Call(s_stringAsSpanMethod);
                            Call(s_spanIndexOfSpan);
                            break;
                    }
                    Stloc(iterationLocal);

                    // if (i >= 0) goto atomicLoopDoneLabel;
                    Ldloc(iterationLocal);
                    Ldc(0);
                    BgeFar(atomicLoopDoneLabel);

                    // i = textSpan.Length - textSpanPos;
                    Ldloca(textSpanLocal);
                    Call(s_spanGetLengthMethod);
                    if (textSpanPos > 0)
                    {
                        Ldc(textSpanPos);
                        Sub();
                    }
                    Stloc(iterationLocal);
                }
                else if (node.IsSetFamily && maxIterations == int.MaxValue && node.Str == RegexCharClass.AnyClass)
                {
                    // .* was used with RegexOptions.Singleline, which means it'll consume everything.  Just jump to the end.
                    // The unbounded constraint is the same as in the Notone case above, done purely for simplicity.

                    // int i = runtextend - runtextpos;
                    TransferTextSpanPosToRunTextPos();
                    Ldloc(runtextendLocal);
                    Ldloc(runtextposLocal);
                    Sub();
                    Stloc(iterationLocal);
                }
                else
                {
                    // For everything else, do a normal loop.

                    // Transfer text pos to runtextpos to help with bounds check elimination on the loop.
                    TransferTextSpanPosToRunTextPos();

                    Label conditionLabel = DefineLabel();
                    Label bodyLabel = DefineLabel();

                    // int i = 0;
                    Ldc(0);
                    Stloc(iterationLocal);
                    BrFar(conditionLabel);

                    // Body:
                    // TimeoutCheck();
                    MarkLabel(bodyLabel);
                    EmitTimeoutCheck();

                    // if ((uint)i >= (uint)textSpan.Length) goto atomicLoopDoneLabel;
                    Ldloc(iterationLocal);
                    Ldloca(textSpanLocal);
                    Call(s_spanGetLengthMethod);
                    BgeUnFar(atomicLoopDoneLabel);

                    // if (textSpan[i] != ch) goto atomicLoopDoneLabel;
                    Ldloca(textSpanLocal);
                    Ldloc(iterationLocal);
                    Call(s_spanGetItemMethod);
                    LdindU2();
                    if (node.IsSetFamily)
                    {
                        EmitMatchCharacterClass(node.Str!, IsCaseInsensitive(node));
                        BrfalseFar(atomicLoopDoneLabel);
                    }
                    else
                    {
                        if (IsCaseInsensitive(node))
                        {
                            CallToLower();
                        }
                        Ldc(node.Ch);
                        if (node.IsOneFamily)
                        {
                            BneFar(atomicLoopDoneLabel);
                        }
                        else // IsNotoneFamily
                        {
                            BeqFar(atomicLoopDoneLabel);
                        }
                    }

                    // i++;
                    Ldloc(iterationLocal);
                    Ldc(1);
                    Add();
                    Stloc(iterationLocal);

                    // if (i >= maxIterations) goto atomicLoopDoneLabel;
                    MarkLabel(conditionLabel);
                    if (maxIterations != int.MaxValue)
                    {
                        Ldloc(iterationLocal);
                        Ldc(maxIterations);
                        BltFar(bodyLabel);
                    }
                    else
                    {
                        BrFar(bodyLabel);
                    }
                }

                // Done:
                MarkLabel(atomicLoopDoneLabel);

                // Check to ensure we've found at least min iterations.
                if (minIterations > 0)
                {
                    Ldloc(iterationLocal);
                    Ldc(minIterations);
                    BltFar(doneLabel);
                }

                // Now that we've completed our optional iterations, advance the text span
                // and runtextpos by the number of iterations completed.

                // textSpan = textSpan.Slice(i);
                Ldloca(textSpanLocal);
                Ldloc(iterationLocal);
                Call(s_spanSliceIntMethod);
                Stloc(textSpanLocal);

                // runtextpos += i;
                Ldloc(runtextposLocal);
                Ldloc(iterationLocal);
                Add();
                Stloc(runtextposLocal);
            }

            // Emits the code to handle a non-backtracking optional zero-or-one loop.
            void EmitAtomicSingleCharZeroOrOne(RegexNode node)
            {
                Debug.Assert(node.M == 0 && node.N == 1);

                Label skipUpdatesLabel = DefineLabel();

                // if ((uint)textSpanPos >= (uint)textSpan.Length) goto skipUpdatesLabel;
                Ldc(textSpanPos);
                Ldloca(textSpanLocal);
                Call(s_spanGetLengthMethod);
                BgeUnFar(skipUpdatesLabel);

                // if (textSpan[textSpanPos] != ch) goto skipUpdatesLabel;
                Ldloca(textSpanLocal);
                Ldc(textSpanPos);
                Call(s_spanGetItemMethod);
                LdindU2();
                if (node.IsSetFamily)
                {
                    EmitMatchCharacterClass(node.Str!, IsCaseInsensitive(node));
                    BrfalseFar(skipUpdatesLabel);
                }
                else
                {
                    if (IsCaseInsensitive(node))
                    {
                        CallToLower();
                    }
                    Ldc(node.Ch);
                    if (node.IsOneFamily)
                    {
                        BneFar(skipUpdatesLabel);
                    }
                    else // IsNotoneFamily
                    {
                        BeqFar(skipUpdatesLabel);
                    }
                }

                // textSpan = textSpan.Slice(1);
                Ldloca(textSpanLocal);
                Ldc(1);
                Call(s_spanSliceIntMethod);
                Stloc(textSpanLocal);

                // runtextpos++;
                Ldloc(runtextposLocal);
                Ldc(1);
                Add();
                Stloc(runtextposLocal);

                MarkLabel(skipUpdatesLabel);
            }

            // Emits the code to handle a non-backtracking, variable-length loop around another node.
            void EmitAtomicNodeLoop(RegexNode node)
            {
                Debug.Assert(node.Type == RegexNode.Loop);
                Debug.Assert(node.M == node.N || (node.Next != null && (node.Next.Type is RegexNode.Atomic or RegexNode.Capture)));
                Debug.Assert(node.M < int.MaxValue);

                // If this is actually a repeater, emit that instead.
                if (node.M == node.N)
                {
                    EmitNodeRepeater(node);
                    return;
                }

                LocalBuilder iterationLocal = DeclareInt32();
                LocalBuilder startingRunTextPosLocal = DeclareInt32();

                Label originalDoneLabel = doneLabel;
                Label atomicNodeLabel = DefineLabel();
                doneLabel = atomicNodeLabel;

                // We might loop any number of times.  In order to ensure this loop
                // and subsequent code sees textSpanPos the same regardless, we always need it to contain
                // the same value, and the easiest such value is 0.  So, we transfer
                // textSpanPos to runtextpos, and ensure that any path out of here has
                // textSpanPos as 0.
                TransferTextSpanPosToRunTextPos();

                Label conditionLabel = DefineLabel();
                Label bodyLabel = DefineLabel();

                Debug.Assert(node.N > node.M);
                int minIterations = node.M;
                int maxIterations = node.N;

                // int i = 0;
                Ldc(0);
                Stloc(iterationLocal);
                BrFar(conditionLabel);

                // Body:
                // TimeoutCheck();
                // if (!match) goto Done;
                MarkLabel(bodyLabel);
                EmitTimeoutCheck();

                // Iteration body
                Label successfulIterationLabel = DefineLabel();

                Label prevDone = doneLabel;
                Label iterationDone = DefineLabel();
                doneLabel = iterationDone;

                // Save off runtextpos.
                Ldloc(runtextposLocal);
                Stloc(startingRunTextPosLocal);

                // Emit the child.
                Debug.Assert(textSpanPos == 0);
                EmitNode(node.Child(0));
                TransferTextSpanPosToRunTextPos(); // ensure textSpanPos remains 0
                Br(successfulIterationLabel); // iteration succeeded

                // If the generated code gets here, the iteration failed.
                // Reset state, branch to done.
                MarkLabel(iterationDone);
                Debug.Assert(doneLabel == iterationDone);
                doneLabel = prevDone;

                Ldloc(startingRunTextPosLocal);
                Stloc(runtextposLocal);
                BrFar(doneLabel);

                // Successful iteration.
                MarkLabel(successfulIterationLabel);

                // i++;
                Ldloc(iterationLocal);
                Ldc(1);
                Add();
                Stloc(iterationLocal);

                // if (i >= maxIterations) goto doneLabel;
                MarkLabel(conditionLabel);
                if (maxIterations != int.MaxValue)
                {
                    Ldloc(iterationLocal);
                    Ldc(maxIterations);
                    BltFar(bodyLabel);
                }
                else
                {
                    BrFar(bodyLabel);
                }

                // Done:
                MarkLabel(atomicNodeLabel);
                Debug.Assert(doneLabel == atomicNodeLabel);
                doneLabel = originalDoneLabel;

                // Check to ensure we've found at least min iterations.
                if (minIterations > 0)
                {
                    Ldloc(iterationLocal);
                    Ldc(minIterations);
                    BltFar(doneLabel);
                }
            }
        }

        /// <summary>Generates the code for "RegexRunner.Go".</summary>
        protected void GenerateGo()
        {
            Debug.Assert(_code != null);
            _int32LocalsPool?.Clear();
            _readOnlySpanCharLocalsPool?.Clear();

            // Generate simpler code when we're dealing with simpler regexes.
            if (TryGenerateSimplifiedGo(_code.Tree.Root))
            {
                return;
            }

            // We're dealing with a regex more complicated that the fast-path non-backtracking
            // implementation can handle.  Do the full-fledged thing.

            // declare some locals

            _runtextposLocal = DeclareInt32();
            _runtextLocal = DeclareString();
            _runtrackposLocal = DeclareInt32();
            _runtrackLocal = DeclareInt32Array();
            _runstackposLocal = DeclareInt32();
            _runstackLocal = DeclareInt32Array();
            if (_hasTimeout)
            {
                _loopTimeoutCounterLocal = DeclareInt32();
            }
            _runtextbegLocal = DeclareInt32();
            _runtextendLocal = DeclareInt32();

            InitializeCultureForGoIfNecessary();

            // clear some tables

            _labels = null;
            _notes = null;
            _notecount = 0;

            // globally used labels

            _backtrack = DefineLabel();

            // emit the code!

            GenerateForwardSection();
            GenerateMiddleSection();
            GenerateBacktrackSection();
        }

        private void InitializeCultureForGoIfNecessary()
        {
            _textInfoLocal = null;
            if ((_options & RegexOptions.CultureInvariant) == 0)
            {
                bool needsCulture = (_options & RegexOptions.IgnoreCase) != 0;
                if (!needsCulture)
                {
                    for (int codepos = 0; codepos < _codes!.Length; codepos += RegexCode.OpcodeSize(_codes[codepos]))
                    {
                        if ((_codes[codepos] & RegexCode.Ci) == RegexCode.Ci)
                        {
                            needsCulture = true;
                            break;
                        }
                    }
                }

                if (needsCulture)
                {
                    // cache CultureInfo in local variable which saves excessive thread local storage accesses
                    _textInfoLocal = DeclareTextInfo();
                    InitLocalCultureInfo();
                }
            }
        }

        /// <summary>
        /// The main translation function. It translates the logic for a single opcode at
        /// the current position. The structure of this function exactly mirrors
        /// the structure of the inner loop of RegexInterpreter.Go().
        /// </summary>
        /// <remarks>
        /// The C# code from RegexInterpreter.Go() that corresponds to each case is
        /// included as a comment.
        ///
        /// Note that since we're generating code, we can collapse many cases that are
        /// dealt with one-at-a-time in RegexIntepreter. We can also unroll loops that
        /// iterate over constant strings or sets.
        /// </remarks>
        private void GenerateOneCode()
        {
#if DEBUG
            if ((_options & RegexOptions.Debug) != 0)
                DumpBacktracking();
#endif

            // Before executing any RegEx code in the unrolled loop,
            // we try checking for the match timeout:

            if (_hasTimeout)
            {
                Ldthis();
                Call(s_checkTimeoutMethod);
            }

            // Now generate the IL for the RegEx code saved in _regexopcode.
            // We unroll the loop done by the RegexCompiler creating as very long method
            // that is longer if the pattern is longer:

            switch (_regexopcode)
            {
                case RegexCode.Stop:
                    //: return;
                    Mvlocfld(_runtextposLocal!, s_runtextposField);       // update _textpos
                    Ret();
                    break;

                case RegexCode.Nothing:
                    //: break Backward;
                    Back();
                    break;

                case RegexCode.UpdateBumpalong:
                    // UpdateBumpalong should only exist in the code stream at such a point where the root
                    // of the backtracking stack contains the runtextpos from the start of this Go call. Replace
                    // that tracking value with the current runtextpos value.
                    //: base.runtrack[base.runtrack.Length - 1] = runtextpos;
                    Ldloc(_runtrackLocal!);
                    Dup();
                    Ldlen();
                    Ldc(1);
                    Sub();
                    Ldloc(_runtextposLocal!);
                    StelemI4();
                    break;

                case RegexCode.Goto:
                    //: Goto(Operand(0));
                    Goto(Operand(0));
                    break;

                case RegexCode.Testref:
                    //: if (!_match.IsMatched(Operand(0)))
                    //:     break Backward;
                    Ldthis();
                    Ldc(Operand(0));
                    Call(s_isMatchedMethod);
                    BrfalseFar(_backtrack);
                    break;

                case RegexCode.Lazybranch:
                    //: Track(Textpos());
                    PushTrack(_runtextposLocal!);
                    Track();
                    break;

                case RegexCode.Lazybranch | RegexCode.Back:
                    //: Trackframe(1);
                    //: Textto(Tracked(0));
                    //: Goto(Operand(0));
                    PopTrack();
                    Stloc(_runtextposLocal!);
                    Goto(Operand(0));
                    break;

                case RegexCode.Nullmark:
                    //: Stack(-1);
                    //: Track();
                    ReadyPushStack();
                    Ldc(-1);
                    DoPush();
                    TrackUnique(Stackpop);
                    break;

                case RegexCode.Setmark:
                    //: Stack(Textpos());
                    //: Track();
                    PushStack(_runtextposLocal!);
                    TrackUnique(Stackpop);
                    break;

                case RegexCode.Nullmark | RegexCode.Back:
                case RegexCode.Setmark | RegexCode.Back:
                    //: Stackframe(1);
                    //: break Backward;
                    PopDiscardStack();
                    Back();
                    break;

                case RegexCode.Getmark:
                    //: Stackframe(1);
                    //: Track(Stacked(0));
                    //: Textto(Stacked(0));
                    ReadyPushTrack();
                    PopStack();
                    Stloc(_runtextposLocal!);
                    Ldloc(_runtextposLocal!);
                    DoPush();

                    Track();
                    break;

                case RegexCode.Getmark | RegexCode.Back:
                    //: Trackframe(1);
                    //: Stack(Tracked(0));
                    //: break Backward;
                    ReadyPushStack();
                    PopTrack();
                    DoPush();
                    Back();
                    break;

                case RegexCode.Capturemark:
                    //: if (!IsMatched(Operand(1)))
                    //:     break Backward;
                    //: Stackframe(1);
                    //: if (Operand(1) != -1)
                    //:     TransferCapture(Operand(0), Operand(1), Stacked(0), Textpos());
                    //: else
                    //:     Capture(Operand(0), Stacked(0), Textpos());
                    //: Track(Stacked(0));

                    //: Stackframe(1);
                    //: Capture(Operand(0), Stacked(0), Textpos());
                    //: Track(Stacked(0));

                    if (Operand(1) != -1)
                    {
                        Ldthis();
                        Ldc(Operand(1));
                        Call(s_isMatchedMethod);
                        BrfalseFar(_backtrack);
                    }

                    using (RentedLocalBuilder stackedLocal = RentInt32Local())
                    {
                        PopStack();
                        Stloc(stackedLocal);

                        if (Operand(1) != -1)
                        {
                            Ldthis();
                            Ldc(Operand(0));
                            Ldc(Operand(1));
                            Ldloc(stackedLocal);
                            Ldloc(_runtextposLocal!);
                            Call(s_transferCaptureMethod);
                        }
                        else
                        {
                            Ldthis();
                            Ldc(Operand(0));
                            Ldloc(stackedLocal);
                            Ldloc(_runtextposLocal!);
                            Call(s_captureMethod);
                        }

                        PushTrack(stackedLocal);
                    }

                    TrackUnique(Operand(0) != -1 && Operand(1) != -1 ? Capback2 : Capback);
                    break;


                case RegexCode.Capturemark | RegexCode.Back:
                    //: Trackframe(1);
                    //: Stack(Tracked(0));
                    //: Uncapture();
                    //: if (Operand(0) != -1 && Operand(1) != -1)
                    //:     Uncapture();
                    //: break Backward;
                    ReadyPushStack();
                    PopTrack();
                    DoPush();
                    Ldthis();
                    Call(s_uncaptureMethod);
                    if (Operand(0) != -1 && Operand(1) != -1)
                    {
                        Ldthis();
                        Call(s_uncaptureMethod);
                    }
                    Back();
                    break;

                case RegexCode.Branchmark:
                    //: Stackframe(1);
                    //:
                    //: if (Textpos() != Stacked(0))
                    //: {                                   // Nonempty match -> loop now
                    //:     Track(Stacked(0), Textpos());   // Save old mark, textpos
                    //:     Stack(Textpos());               // Make new mark
                    //:     Goto(Operand(0));               // Loop
                    //: }
                    //: else
                    //: {                                   // Empty match -> straight now
                    //:     Track2(Stacked(0));             // Save old mark
                    //:     Advance(1);                     // Straight
                    //: }
                    //: continue Forward;
                    {
                        Label l1 = DefineLabel();

                        PopStack();
                        using (RentedLocalBuilder mark = RentInt32Local())
                        {
                            Stloc(mark);                        // Stacked(0) -> temp
                            PushTrack(mark);
                            Ldloc(mark);
                        }
                        Ldloc(_runtextposLocal!);
                        Beq(l1);                                // mark == textpos -> branch

                        // (matched != 0)

                        PushTrack(_runtextposLocal!);
                        PushStack(_runtextposLocal!);
                        Track();
                        Goto(Operand(0));                       // Goto(Operand(0))

                        // else

                        MarkLabel(l1);
                        TrackUnique2(Branchmarkback2);
                        break;
                    }

                case RegexCode.Branchmark | RegexCode.Back:
                    //: Trackframe(2);
                    //: Stackframe(1);
                    //: Textto(Tracked(1));                     // Recall position
                    //: Track2(Tracked(0));                     // Save old mark
                    //: Advance(1);
                    PopTrack();
                    Stloc(_runtextposLocal!);
                    PopStack();
                    Pop();
                    // track spot 0 is already in place
                    TrackUnique2(Branchmarkback2);
                    Advance();
                    break;

                case RegexCode.Branchmark | RegexCode.Back2:
                    //: Trackframe(1);
                    //: Stack(Tracked(0));                      // Recall old mark
                    //: break Backward;                         // Backtrack
                    ReadyPushStack();
                    PopTrack();
                    DoPush();
                    Back();
                    break;

                case RegexCode.Lazybranchmark:
                    //: StackPop();
                    //: int oldMarkPos = StackPeek();
                    //:
                    //: if (Textpos() != oldMarkPos) {         // Nonempty match -> next loop
                    //: {                                   // Nonempty match -> next loop
                    //:     if (oldMarkPos != -1)
                    //:         Track(Stacked(0), Textpos());   // Save old mark, textpos
                    //:     else
                    //:         TrackPush(Textpos(), Textpos());
                    //: }
                    //: else
                    //: {                                   // Empty match -> no loop
                    //:     Track2(Stacked(0));             // Save old mark
                    //: }
                    //: Advance(1);
                    //: continue Forward;
                    {
                        using (RentedLocalBuilder mark = RentInt32Local())
                        {
                            PopStack();
                            Stloc(mark);                      // Stacked(0) -> temp

                            // if (oldMarkPos != -1)
                            Label l2 = DefineLabel();
                            Label l3 = DefineLabel();
                            Ldloc(mark);
                            Ldc(-1);
                            Beq(l2);                           // mark == -1 -> branch
                            PushTrack(mark);
                            Br(l3);
                            // else
                            MarkLabel(l2);
                            PushTrack(_runtextposLocal!);
                            MarkLabel(l3);

                            // if (Textpos() != mark)
                            Label l1 = DefineLabel();
                            Ldloc(_runtextposLocal!);
                            Ldloc(mark);
                            Beq(l1);                            // mark == textpos -> branch
                            PushTrack(_runtextposLocal!);
                            Track();
                            Br(AdvanceLabel());                 // Advance (near)
                                                                // else
                            MarkLabel(l1);
                            ReadyPushStack();                   // push the current textPos on the stack.
                                                                // May be ignored by 'back2' or used by a true empty match.
                            Ldloc(mark);
                        }

                        DoPush();
                        TrackUnique2(Lazybranchmarkback2);

                        break;
                    }

                case RegexCode.Lazybranchmark | RegexCode.Back:
                    //: Trackframe(2);
                    //: Track2(Tracked(0));                     // Save old mark
                    //: Stack(Textpos());                       // Make new mark
                    //: Textto(Tracked(1));                     // Recall position
                    //: Goto(Operand(0));                       // Loop

                    PopTrack();
                    Stloc(_runtextposLocal!);
                    PushStack(_runtextposLocal!);
                    TrackUnique2(Lazybranchmarkback2);
                    Goto(Operand(0));
                    break;

                case RegexCode.Lazybranchmark | RegexCode.Back2:
                    //: Stackframe(1);
                    //: Trackframe(1);
                    //: Stack(Tracked(0));                  // Recall old mark
                    //: break Backward;
                    ReadyReplaceStack(0);
                    PopTrack();
                    DoReplace();
                    Back();
                    break;

                case RegexCode.Nullcount:
                    //: Stack(-1, Operand(0));
                    //: Track();
                    ReadyPushStack();
                    Ldc(-1);
                    DoPush();
                    ReadyPushStack();
                    Ldc(Operand(0));
                    DoPush();
                    TrackUnique(Stackpop2);
                    break;

                case RegexCode.Setcount:
                    //: Stack(Textpos(), Operand(0));
                    //: Track();
                    PushStack(_runtextposLocal!);
                    ReadyPushStack();
                    Ldc(Operand(0));
                    DoPush();
                    TrackUnique(Stackpop2);
                    break;

                case RegexCode.Nullcount | RegexCode.Back:
                case RegexCode.Setcount | RegexCode.Back:
                    //: Stackframe(2);
                    //: break Backward;
                    PopDiscardStack(2);
                    Back();
                    break;

                case RegexCode.Branchcount:
                    //: Stackframe(2);
                    //: int mark = Stacked(0);
                    //: int count = Stacked(1);
                    //:
                    //: if (count >= Operand(1) || Textpos() == mark && count >= 0)
                    //: {                                   // Max loops or empty match -> straight now
                    //:     Track2(mark, count);            // Save old mark, count
                    //:     Advance(2);                     // Straight
                    //: }
                    //: else
                    //: {                                   // Nonempty match -> count+loop now
                    //:     Track(mark);                    // remember mark
                    //:     Stack(Textpos(), count + 1);    // Make new mark, incr count
                    //:     Goto(Operand(0));               // Loop
                    //: }
                    //: continue Forward;
                    {
                        using (RentedLocalBuilder count = RentInt32Local())
                        {
                            PopStack();
                            Stloc(count);                           // count -> temp
                            PopStack();
                            using (RentedLocalBuilder mark = RentInt32Local())
                            {
                                Stloc(mark);                        // mark -> temp2
                                PushTrack(mark);
                                Ldloc(mark);
                            }

                            Label l1 = DefineLabel();
                            Label l2 = DefineLabel();
                            Ldloc(_runtextposLocal!);
                            Bne(l1);                                // mark != textpos -> l1
                            Ldloc(count);
                            Ldc(0);
                            Bge(l2);                                // count >= 0 && mark == textpos -> l2

                            MarkLabel(l1);
                            Ldloc(count);
                            Ldc(Operand(1));
                            Bge(l2);                                // count >= Operand(1) -> l2

                            // else
                            PushStack(_runtextposLocal!);
                            ReadyPushStack();
                            Ldloc(count);                           // mark already on track
                            Ldc(1);
                            Add();
                            DoPush();
                            Track();
                            Goto(Operand(0));

                            // if (count >= Operand(1) || Textpos() == mark)
                            MarkLabel(l2);
                            PushTrack(count);                       // mark already on track
                        }
                        TrackUnique2(Branchcountback2);
                        break;
                    }

                case RegexCode.Branchcount | RegexCode.Back:
                    //: Trackframe(1);
                    //: Stackframe(2);
                    //: if (Stacked(1) > 0)                     // Positive -> can go straight
                    //: {
                    //:     Textto(Stacked(0));                 // Zap to mark
                    //:     Track2(Tracked(0), Stacked(1) - 1); // Save old mark, old count
                    //:     Advance(2);                         // Straight
                    //:     continue Forward;
                    //: }
                    //: Stack(Tracked(0), Stacked(1) - 1);      // recall old mark, old count
                    //: break Backward;
                    {
                        using (RentedLocalBuilder count = RentInt32Local())
                        {
                            Label l1 = DefineLabel();
                            PopStack();
                            Ldc(1);
                            Sub();
                            Stloc(count);
                            Ldloc(count);
                            Ldc(0);
                            Blt(l1);

                            // if (count >= 0)
                            PopStack();
                            Stloc(_runtextposLocal!);
                            PushTrack(count);                       // Tracked(0) is already on the track
                            TrackUnique2(Branchcountback2);
                            Advance();

                            // else
                            MarkLabel(l1);
                            ReadyReplaceStack(0);
                            PopTrack();
                            DoReplace();
                            PushStack(count);
                        }
                        Back();
                        break;
                    }

                case RegexCode.Branchcount | RegexCode.Back2:
                    //: Trackframe(2);
                    //: Stack(Tracked(0), Tracked(1));      // Recall old mark, old count
                    //: break Backward;                     // Backtrack

                    PopTrack();
                    using (RentedLocalBuilder tmp = RentInt32Local())
                    {
                        Stloc(tmp);
                        ReadyPushStack();
                        PopTrack();
                        DoPush();
                        PushStack(tmp);
                    }
                    Back();
                    break;

                case RegexCode.Lazybranchcount:
                    //: Stackframe(2);
                    //: int mark = Stacked(0);
                    //: int count = Stacked(1);
                    //:
                    //: if (count < 0)
                    //: {                                   // Negative count -> loop now
                    //:     Track2(mark);                   // Save old mark
                    //:     Stack(Textpos(), count + 1);    // Make new mark, incr count
                    //:     Goto(Operand(0));               // Loop
                    //: }
                    //: else
                    //: {                                   // Nonneg count or empty match -> straight now
                    //:     Track(mark, count, Textpos());  // Save mark, count, position
                    //: }
                    {
                        PopStack();
                        using (RentedLocalBuilder count = RentInt32Local())
                        {
                            Stloc(count);                           // count -> temp
                            PopStack();
                            using (RentedLocalBuilder mark = RentInt32Local())
                            {
                                Stloc(mark);                            // mark -> temp2

                                Label l1 = DefineLabel();
                                Ldloc(count);
                                Ldc(0);
                                Bge(l1);                                // count >= 0 -> l1

                                // if (count < 0)
                                PushTrack(mark);
                                PushStack(_runtextposLocal!);
                                ReadyPushStack();
                                Ldloc(count);
                                Ldc(1);
                                Add();
                                DoPush();
                                TrackUnique2(Lazybranchcountback2);
                                Goto(Operand(0));

                                // else
                                MarkLabel(l1);
                                PushTrack(mark);
                            }
                            PushTrack(count);
                        }
                        PushTrack(_runtextposLocal!);
                        Track();
                        break;
                    }

                case RegexCode.Lazybranchcount | RegexCode.Back:
                    //: Trackframe(3);
                    //: int mark = Tracked(0);
                    //: int textpos = Tracked(2);
                    //: if (Tracked(1) < Operand(1) && textpos != mark)
                    //: {                                       // Under limit and not empty match -> loop
                    //:     Textto(Tracked(2));                 // Recall position
                    //:     Stack(Textpos(), Tracked(1) + 1);   // Make new mark, incr count
                    //:     Track2(Tracked(0));                 // Save old mark
                    //:     Goto(Operand(0));                   // Loop
                    //:     continue Forward;
                    //: }
                    //: else
                    //: {
                    //:     Stack(Tracked(0), Tracked(1));      // Recall old mark, count
                    //:     break Backward;                     // backtrack
                    //: }
                    {
                        using (RentedLocalBuilder cLocal = RentInt32Local())
                        {
                            Label l1 = DefineLabel();

                            PopTrack();
                            Stloc(_runtextposLocal!);
                            PopTrack();
                            Stloc(cLocal);
                            Ldloc(cLocal);
                            Ldc(Operand(1));
                            Bge(l1);                                // Tracked(1) >= Operand(1) -> l1

                            Ldloc(_runtextposLocal!);
                            TopTrack();
                            Beq(l1);                                // textpos == mark -> l1

                            PushStack(_runtextposLocal!);
                            ReadyPushStack();
                            Ldloc(cLocal);
                            Ldc(1);
                            Add();
                            DoPush();
                            TrackUnique2(Lazybranchcountback2);
                            Goto(Operand(0));

                            MarkLabel(l1);
                            ReadyPushStack();
                            PopTrack();
                            DoPush();
                            PushStack(cLocal);
                        }
                        Back();
                        break;
                    }

                case RegexCode.Lazybranchcount | RegexCode.Back2:
                    // <
                    ReadyReplaceStack(1);
                    PopTrack();
                    DoReplace();
                    ReadyReplaceStack(0);
                    TopStack();
                    Ldc(1);
                    Sub();
                    DoReplace();
                    Back();
                    break;

                case RegexCode.Setjump:
                    //: Stack(Trackpos(), Crawlpos());
                    //: Track();
                    ReadyPushStack();
                    Ldthisfld(s_runtrackField);
                    Ldlen();
                    Ldloc(_runtrackposLocal!);
                    Sub();
                    DoPush();
                    ReadyPushStack();
                    Ldthis();
                    Call(s_crawlposMethod);
                    DoPush();
                    TrackUnique(Stackpop2);
                    break;

                case RegexCode.Setjump | RegexCode.Back:
                    //: Stackframe(2);
                    PopDiscardStack(2);
                    Back();
                    break;

                case RegexCode.Backjump:
                    //: Stackframe(2);
                    //: Trackto(Stacked(0));
                    //: while (Crawlpos() != Stacked(1))
                    //:     Uncapture();
                    //: break Backward;
                    {
                        Label l1 = DefineLabel();
                        Label l2 = DefineLabel();

                        using (RentedLocalBuilder stackedLocal = RentInt32Local())
                        {
                            PopStack();
                            Stloc(stackedLocal);
                            Ldthisfld(s_runtrackField);
                            Ldlen();
                            PopStack();
                            Sub();
                            Stloc(_runtrackposLocal!);

                            MarkLabel(l1);
                            Ldthis();
                            Call(s_crawlposMethod);
                            Ldloc(stackedLocal);
                            Beq(l2);
                            Ldthis();
                            Call(s_uncaptureMethod);
                            Br(l1);
                        }

                        MarkLabel(l2);
                        Back();
                        break;
                    }

                case RegexCode.Forejump:
                    //: Stackframe(2);
                    //: Trackto(Stacked(0));
                    //: Track(Stacked(1));
                    PopStack();
                    using (RentedLocalBuilder tmp = RentInt32Local())
                    {
                        Stloc(tmp);
                        Ldthisfld(s_runtrackField);
                        Ldlen();
                        PopStack();
                        Sub();
                        Stloc(_runtrackposLocal!);
                        PushTrack(tmp);
                    }
                    TrackUnique(Forejumpback);
                    break;

                case RegexCode.Forejump | RegexCode.Back:
                    //: Trackframe(1);
                    //: while (Crawlpos() != Tracked(0))
                    //:     Uncapture();
                    //: break Backward;
                    {
                        Label l1 = DefineLabel();
                        Label l2 = DefineLabel();

                        using (RentedLocalBuilder trackedLocal = RentInt32Local())
                        {
                            PopTrack();
                            Stloc(trackedLocal);

                            MarkLabel(l1);
                            Ldthis();
                            Call(s_crawlposMethod);
                            Ldloc(trackedLocal);
                            Beq(l2);
                            Ldthis();
                            Call(s_uncaptureMethod);
                            Br(l1);
                        }

                        MarkLabel(l2);
                        Back();
                        break;
                    }

                case RegexCode.Bol:
                    //: if (Leftchars() > 0 && CharAt(Textpos() - 1) != '\n')
                    //:     break Backward;
                    {
                        Label l1 = _labels![NextCodepos()];
                        Ldloc(_runtextposLocal!);
                        Ldloc(_runtextbegLocal!);
                        Ble(l1);
                        Leftchar();
                        Ldc('\n');
                        BneFar(_backtrack);
                        break;
                    }

                case RegexCode.Eol:
                    //: if (Rightchars() > 0 && CharAt(Textpos()) != '\n')
                    //:     break Backward;
                    {
                        Label l1 = _labels![NextCodepos()];
                        Ldloc(_runtextposLocal!);
                        Ldloc(_runtextendLocal!);
                        Bge(l1);
                        Rightchar();
                        Ldc('\n');
                        BneFar(_backtrack);
                        break;
                    }

                case RegexCode.Boundary:
                case RegexCode.NonBoundary:
                    //: if (!IsBoundary(Textpos(), _textbeg, _textend))
                    //:     break Backward;
                    Ldthis();
                    Ldloc(_runtextposLocal!);
                    Ldloc(_runtextbegLocal!);
                    Ldloc(_runtextendLocal!);
                    Call(s_isBoundaryMethod);
                    if (Code() == RegexCode.Boundary)
                    {
                        BrfalseFar(_backtrack);
                    }
                    else
                    {
                        BrtrueFar(_backtrack);
                    }
                    break;

                case RegexCode.ECMABoundary:
                case RegexCode.NonECMABoundary:
                    //: if (!IsECMABoundary(Textpos(), _textbeg, _textend))
                    //:     break Backward;
                    Ldthis();
                    Ldloc(_runtextposLocal!);
                    Ldloc(_runtextbegLocal!);
                    Ldloc(_runtextendLocal!);
                    Call(s_isECMABoundaryMethod);
                    if (Code() == RegexCode.ECMABoundary)
                    {
                        BrfalseFar(_backtrack);
                    }
                    else
                    {
                        BrtrueFar(_backtrack);
                    }
                    break;

                case RegexCode.Beginning:
                    //: if (Leftchars() > 0)
                    //:    break Backward;
                    Ldloc(_runtextposLocal!);
                    Ldloc(_runtextbegLocal!);
                    BgtFar(_backtrack);
                    break;

                case RegexCode.Start:
                    //: if (Textpos() != Textstart())
                    //:    break Backward;
                    Ldloc(_runtextposLocal!);
                    Ldthisfld(s_runtextstartField);
                    BneFar(_backtrack);
                    break;

                case RegexCode.EndZ:
                    //: if (Rightchars() > 1 || Rightchars() == 1 && CharAt(Textpos()) != '\n')
                    //:    break Backward;
                    Ldloc(_runtextposLocal!);
                    Ldloc(_runtextendLocal!);
                    Ldc(1);
                    Sub();
                    BltFar(_backtrack);
                    Ldloc(_runtextposLocal!);
                    Ldloc(_runtextendLocal!);
                    Bge(_labels![NextCodepos()]);
                    Rightchar();
                    Ldc('\n');
                    BneFar(_backtrack);
                    break;

                case RegexCode.End:
                    //: if (Rightchars() > 0)
                    //:    break Backward;
                    Ldloc(_runtextposLocal!);
                    Ldloc(_runtextendLocal!);
                    BltFar(_backtrack);
                    break;

                case RegexCode.One:
                case RegexCode.Notone:
                case RegexCode.Set:
                case RegexCode.One | RegexCode.Rtl:
                case RegexCode.Notone | RegexCode.Rtl:
                case RegexCode.Set | RegexCode.Rtl:
                case RegexCode.One | RegexCode.Ci:
                case RegexCode.Notone | RegexCode.Ci:
                case RegexCode.Set | RegexCode.Ci:
                case RegexCode.One | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Notone | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Set | RegexCode.Ci | RegexCode.Rtl:

                    //: if (Rightchars() < 1 || Rightcharnext() != (char)Operand(0))
                    //:    break Backward;

                    Ldloc(_runtextposLocal!);

                    if (!IsRightToLeft())
                    {
                        Ldloc(_runtextendLocal!);
                        BgeFar(_backtrack);
                        Rightcharnext();
                    }
                    else
                    {
                        Ldloc(_runtextbegLocal!);
                        BleFar(_backtrack);
                        Leftcharnext();
                    }

                    if (Code() == RegexCode.Set)
                    {
                        EmitMatchCharacterClass(_strings![Operand(0)], IsCaseInsensitive());
                        BrfalseFar(_backtrack);
                    }
                    else
                    {
                        if (IsCaseInsensitive())
                        {
                            CallToLower();
                        }

                        Ldc(Operand(0));
                        if (Code() == RegexCode.One)
                        {
                            BneFar(_backtrack);
                        }
                        else
                        {
                            BeqFar(_backtrack);
                        }
                    }
                    break;

                case RegexCode.Multi:
                case RegexCode.Multi | RegexCode.Ci:
                    //: String Str = _strings[Operand(0)];
                    //: int i, c;
                    //: if (Rightchars() < (c = Str.Length))
                    //:     break Backward;
                    //: for (i = 0; c > 0; i++, c--)
                    //:     if (Str[i] != Rightcharnext())
                    //:         break Backward;
                    {
                        string str = _strings![Operand(0)];

                        Ldc(str.Length);
                        Ldloc(_runtextendLocal!);
                        Ldloc(_runtextposLocal!);
                        Sub();
                        BgtFar(_backtrack);

                        // unroll the string
                        for (int i = 0; i < str.Length; i++)
                        {
                            Ldloc(_runtextLocal!);
                            Ldloc(_runtextposLocal!);
                            if (i != 0)
                            {
                                Ldc(i);
                                Add();
                            }
                            Call(s_stringGetCharsMethod);
                            if (IsCaseInsensitive())
                            {
                                CallToLower();
                            }

                            Ldc(str[i]);
                            BneFar(_backtrack);
                        }

                        Ldloc(_runtextposLocal!);
                        Ldc(str.Length);
                        Add();
                        Stloc(_runtextposLocal!);
                        break;
                    }

                case RegexCode.Multi | RegexCode.Rtl:
                case RegexCode.Multi | RegexCode.Ci | RegexCode.Rtl:
                    //: String Str = _strings[Operand(0)];
                    //: int c;
                    //: if (Leftchars() < (c = Str.Length))
                    //:     break Backward;
                    //: while (c > 0)
                    //:     if (Str[--c] != Leftcharnext())
                    //:         break Backward;
                    {
                        string str = _strings![Operand(0)];

                        Ldc(str.Length);
                        Ldloc(_runtextposLocal!);
                        Ldloc(_runtextbegLocal!);
                        Sub();
                        BgtFar(_backtrack);

                        // unroll the string
                        for (int i = str.Length; i > 0;)
                        {
                            i--;
                            Ldloc(_runtextLocal!);
                            Ldloc(_runtextposLocal!);
                            Ldc(str.Length - i);
                            Sub();
                            Call(s_stringGetCharsMethod);
                            if (IsCaseInsensitive())
                            {
                                CallToLower();
                            }
                            Ldc(str[i]);
                            BneFar(_backtrack);
                        }

                        Ldloc(_runtextposLocal!);
                        Ldc(str.Length);
                        Sub();
                        Stloc(_runtextposLocal!);

                        break;
                    }

                case RegexCode.Ref:
                case RegexCode.Ref | RegexCode.Rtl:
                case RegexCode.Ref | RegexCode.Ci:
                case RegexCode.Ref | RegexCode.Ci | RegexCode.Rtl:
                    //: int capnum = Operand(0);
                    //: int j, c;
                    //: if (!_match.IsMatched(capnum)) {
                    //:     if (!RegexOptions.ECMAScript)
                    //:         break Backward;
                    //: } else {
                    //:     if (Rightchars() < (c = _match.MatchLength(capnum)))
                    //:         break Backward;
                    //:     for (j = _match.MatchIndex(capnum); c > 0; j++, c--)
                    //:         if (CharAt(j) != Rightcharnext())
                    //:             break Backward;
                    //: }
                    {
                        using RentedLocalBuilder lenLocal = RentInt32Local();
                        using RentedLocalBuilder indexLocal = RentInt32Local();
                        Label l1 = DefineLabel();

                        Ldthis();
                        Ldc(Operand(0));
                        Call(s_isMatchedMethod);
                        if ((_options & RegexOptions.ECMAScript) != 0)
                        {
                            Brfalse(AdvanceLabel());
                        }
                        else
                        {
                            BrfalseFar(_backtrack); // !IsMatched() -> back
                        }

                        Ldthis();
                        Ldc(Operand(0));
                        Call(s_matchLengthMethod);
                        Stloc(lenLocal);
                        Ldloc(lenLocal);
                        if (!IsRightToLeft())
                        {
                            Ldloc(_runtextendLocal!);
                            Ldloc(_runtextposLocal!);
                        }
                        else
                        {
                            Ldloc(_runtextposLocal!);
                            Ldloc(_runtextbegLocal!);
                        }
                        Sub();
                        BgtFar(_backtrack);         // Matchlength() > Rightchars() -> back

                        Ldthis();
                        Ldc(Operand(0));
                        Call(s_matchIndexMethod);
                        if (!IsRightToLeft())
                        {
                            Ldloc(lenLocal);
                            Add(IsRightToLeft());
                        }
                        Stloc(indexLocal);              // index += len

                        Ldloc(_runtextposLocal!);
                        Ldloc(lenLocal);
                        Add(IsRightToLeft());
                        Stloc(_runtextposLocal!);           // texpos += len

                        MarkLabel(l1);
                        Ldloc(lenLocal);
                        Ldc(0);
                        Ble(AdvanceLabel());
                        Ldloc(_runtextLocal!);
                        Ldloc(indexLocal);
                        Ldloc(lenLocal);
                        if (IsRightToLeft())
                        {
                            Ldc(1);
                            Sub();
                            Stloc(lenLocal);
                            Ldloc(lenLocal);
                        }
                        Sub(IsRightToLeft());
                        Call(s_stringGetCharsMethod);
                        if (IsCaseInsensitive())
                        {
                            CallToLower();
                        }

                        Ldloc(_runtextLocal!);
                        Ldloc(_runtextposLocal!);
                        Ldloc(lenLocal);
                        if (!IsRightToLeft())
                        {
                            Ldloc(lenLocal);
                            Ldc(1);
                            Sub();
                            Stloc(lenLocal);
                        }
                        Sub(IsRightToLeft());
                        Call(s_stringGetCharsMethod);
                        if (IsCaseInsensitive())
                        {
                            CallToLower();
                        }

                        Beq(l1);
                        Back();
                        break;
                    }

                case RegexCode.Onerep:
                case RegexCode.Notonerep:
                case RegexCode.Setrep:
                case RegexCode.Onerep | RegexCode.Rtl:
                case RegexCode.Notonerep | RegexCode.Rtl:
                case RegexCode.Setrep | RegexCode.Rtl:
                case RegexCode.Onerep | RegexCode.Ci:
                case RegexCode.Notonerep | RegexCode.Ci:
                case RegexCode.Setrep | RegexCode.Ci:
                case RegexCode.Onerep | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Notonerep | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Setrep | RegexCode.Ci | RegexCode.Rtl:
                    //: int c = Operand(1);
                    //: if (Rightchars() < c)
                    //:     break Backward;
                    //: char ch = (char)Operand(0);
                    //: while (c-- > 0)
                    //:     if (Rightcharnext() != ch)
                    //:         break Backward;
                    {
                        int c = Operand(1);
                        if (c == 0)
                            break;

                        Ldc(c);
                        if (!IsRightToLeft())
                        {
                            Ldloc(_runtextendLocal!);
                            Ldloc(_runtextposLocal!);
                        }
                        else
                        {
                            Ldloc(_runtextposLocal!);
                            Ldloc(_runtextbegLocal!);
                        }
                        Sub();
                        BgtFar(_backtrack);         // Matchlength() > Rightchars() -> back

                        Ldloc(_runtextposLocal!);
                        Ldc(c);
                        Add(IsRightToLeft());
                        Stloc(_runtextposLocal!);           // texpos += len

                        using RentedLocalBuilder lenLocal = RentInt32Local();
                        Label l1 = DefineLabel();
                        Ldc(c);
                        Stloc(lenLocal);

                        MarkLabel(l1);
                        Ldloc(_runtextLocal!);
                        Ldloc(_runtextposLocal!);
                        Ldloc(lenLocal);
                        if (IsRightToLeft())
                        {
                            Ldc(1);
                            Sub();
                            Stloc(lenLocal);
                            Ldloc(lenLocal);
                            Add();
                        }
                        else
                        {
                            Ldloc(lenLocal);
                            Ldc(1);
                            Sub();
                            Stloc(lenLocal);
                            Sub();
                        }
                        Call(s_stringGetCharsMethod);

                        if (Code() == RegexCode.Setrep)
                        {
                            EmitTimeoutCheck();
                            EmitMatchCharacterClass(_strings![Operand(0)], IsCaseInsensitive());
                            BrfalseFar(_backtrack);
                        }
                        else
                        {
                            if (IsCaseInsensitive())
                            {
                                CallToLower();
                            }

                            Ldc(Operand(0));
                            if (Code() == RegexCode.Onerep)
                            {
                                BneFar(_backtrack);
                            }
                            else
                            {
                                BeqFar(_backtrack);
                            }
                        }
                        Ldloc(lenLocal);
                        Ldc(0);
                        if (Code() == RegexCode.Setrep)
                        {
                            BgtFar(l1);
                        }
                        else
                        {
                            Bgt(l1);
                        }
                        break;
                    }

                case RegexCode.Oneloop:
                case RegexCode.Notoneloop:
                case RegexCode.Setloop:
                case RegexCode.Oneloop | RegexCode.Rtl:
                case RegexCode.Notoneloop | RegexCode.Rtl:
                case RegexCode.Setloop | RegexCode.Rtl:
                case RegexCode.Oneloop | RegexCode.Ci:
                case RegexCode.Notoneloop | RegexCode.Ci:
                case RegexCode.Setloop | RegexCode.Ci:
                case RegexCode.Oneloop | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Notoneloop | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Setloop | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Oneloopatomic:
                case RegexCode.Notoneloopatomic:
                case RegexCode.Setloopatomic:
                case RegexCode.Oneloopatomic | RegexCode.Rtl:
                case RegexCode.Notoneloopatomic | RegexCode.Rtl:
                case RegexCode.Setloopatomic | RegexCode.Rtl:
                case RegexCode.Oneloopatomic | RegexCode.Ci:
                case RegexCode.Notoneloopatomic | RegexCode.Ci:
                case RegexCode.Setloopatomic | RegexCode.Ci:
                case RegexCode.Oneloopatomic | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Notoneloopatomic | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Setloopatomic | RegexCode.Ci | RegexCode.Rtl:
                    //: int len = Operand(1);
                    //: if (len > Rightchars())
                    //:     len = Rightchars();
                    //: char ch = (char)Operand(0);
                    //: int i;
                    //: for (i = len; i > 0; i--)
                    //: {
                    //:     if (Rightcharnext() != ch)
                    //:     {
                    //:         Leftnext();
                    //:         break;
                    //:     }
                    //: }
                    //: if (len > i)
                    //:     Track(len - i - 1, Textpos() - 1);
                    {
                        int c = Operand(1);
                        if (c == 0)
                        {
                            break;
                        }

                        using RentedLocalBuilder lenLocal = RentInt32Local();
                        using RentedLocalBuilder iLocal = RentInt32Local();

                        if (!IsRightToLeft())
                        {
                            Ldloc(_runtextendLocal!);
                            Ldloc(_runtextposLocal!);
                        }
                        else
                        {
                            Ldloc(_runtextposLocal!);
                            Ldloc(_runtextbegLocal!);
                        }
                        Sub();
                        Stloc(lenLocal);
                        if (c != int.MaxValue)
                        {
                            Label l4 = DefineLabel();
                            Ldloc(lenLocal);
                            Ldc(c);
                            Blt(l4);
                            Ldc(c);
                            Stloc(lenLocal);
                            MarkLabel(l4);
                        }

                        Label loopEnd = DefineLabel();
                        string? set = Code() == RegexCode.Setloop || Code() == RegexCode.Setloopatomic ? _strings![Operand(0)] : null;
                        Span<char> setChars = stackalloc char[5]; // max optimized by IndexOfAny today
                        int numSetChars;

                        // If this is a notoneloop{atomic} and we're left-to-right and case-sensitive,
                        // we can use the vectorized IndexOf to search for the target character.
                        if ((Code() == RegexCode.Notoneloop || Code() == RegexCode.Notoneloopatomic) &&
                            !IsRightToLeft() &&
                            (!IsCaseInsensitive()))
                        {
                            // i = runtext.AsSpan(runtextpos, len).IndexOf(ch);
                            Ldloc(_runtextLocal!);
                            Ldloc(_runtextposLocal!);
                            Ldloc(lenLocal);
                            Call(s_stringAsSpanIntIntMethod);
                            Ldc(Operand(0));
                            Call(s_spanIndexOfChar);
                            Stloc(iLocal);

                            Label charFound = DefineLabel();

                            // if (i != -1) goto charFound;
                            Ldloc(iLocal);
                            Ldc(-1);
                            Bne(charFound);

                            // runtextpos += len;
                            // i = 0;
                            // goto loopEnd;
                            Ldloc(_runtextposLocal!);
                            Ldloc(lenLocal);
                            Add();
                            Stloc(_runtextposLocal!);
                            Ldc(0);
                            Stloc(iLocal);
                            BrFar(loopEnd);

                            // charFound:
                            // runtextpos += i;
                            // i = len - i;
                            // goto loopEnd;
                            MarkLabel(charFound);
                            Ldloc(_runtextposLocal!);
                            Ldloc(iLocal);
                            Add();
                            Stloc(_runtextposLocal!);
                            Ldloc(lenLocal);
                            Ldloc(iLocal);
                            Sub();
                            Stloc(iLocal);
                            BrFar(loopEnd);
                        }
                        else if ((Code() == RegexCode.Setloop || Code() == RegexCode.Setloopatomic) &&
                            !IsRightToLeft() &&
                            !IsCaseInsensitive() &&
                            (numSetChars = RegexCharClass.GetSetChars(set!, setChars)) != 0 &&
                            RegexCharClass.IsNegated(set!))
                        {
                            // Similarly, if this is a setloop{atomic} and we're left-to-right and case-sensitive,
                            // and if the set contains only a few negated chars, we can use the vectorized IndexOfAny
                            // to search for those chars.
                            Debug.Assert(numSetChars > 1);

                            // i = runtext.AsSpan(runtextpos, len).IndexOfAny(ch1, ch2{, ch3});
                            Ldloc(_runtextLocal!);
                            Ldloc(_runtextposLocal!);
                            Ldloc(lenLocal);
                            Call(s_stringAsSpanIntIntMethod);
                            switch (numSetChars)
                            {
                                case 2:
                                    Ldc(setChars[0]);
                                    Ldc(setChars[1]);
                                    Call(s_spanIndexOfAnyCharChar);
                                    break;

                                case 3:
                                    Ldc(setChars[0]);
                                    Ldc(setChars[1]);
                                    Ldc(setChars[2]);
                                    Call(s_spanIndexOfAnyCharCharChar);
                                    break;

                                default:
                                    Ldstr(setChars.Slice(0, numSetChars).ToString());
                                    Call(s_stringAsSpanMethod);
                                    Call(s_spanIndexOfSpan);
                                    break;
                            }
                            Stloc(iLocal);

                            Label charFound = DefineLabel();

                            // if (i != -1) goto charFound;
                            Ldloc(iLocal);
                            Ldc(-1);
                            Bne(charFound);

                            // runtextpos += len;
                            // i = 0;
                            // goto loopEnd;
                            Ldloc(_runtextposLocal!);
                            Ldloc(lenLocal);
                            Add();
                            Stloc(_runtextposLocal!);
                            Ldc(0);
                            Stloc(iLocal);
                            BrFar(loopEnd);

                            // charFound:
                            // runtextpos += i;
                            // i = len - i;
                            // goto loopEnd;
                            MarkLabel(charFound);
                            Ldloc(_runtextposLocal!);
                            Ldloc(iLocal);
                            Add();
                            Stloc(_runtextposLocal!);
                            Ldloc(lenLocal);
                            Ldloc(iLocal);
                            Sub();
                            Stloc(iLocal);
                            BrFar(loopEnd);
                        }
                        else if ((Code() == RegexCode.Setloop || Code() == RegexCode.Setloopatomic) &&
                            !IsRightToLeft() &&
                            set == RegexCharClass.AnyClass)
                        {
                            // If someone uses .* along with RegexOptions.Singleline, that becomes [anycharacter]*, which means it'll
                            // consume everything.  As such, we can simply update our position to be the last allowed, without
                            // actually checking anything.

                            // runtextpos += len;
                            // i = 0;
                            // goto loopEnd;
                            Ldloc(_runtextposLocal!);
                            Ldloc(lenLocal);
                            Add();
                            Stloc(_runtextposLocal!);
                            Ldc(0);
                            Stloc(iLocal);
                            BrFar(loopEnd);
                        }
                        else
                        {
                            // Otherwise, we emit the open-coded loop.

                            Ldloc(lenLocal);
                            Ldc(1);
                            Add();
                            Stloc(iLocal);

                            Label loopCondition = DefineLabel();
                            MarkLabel(loopCondition);
                            Ldloc(iLocal);
                            Ldc(1);
                            Sub();
                            Stloc(iLocal);
                            Ldloc(iLocal);
                            Ldc(0);
                            if (Code() == RegexCode.Setloop || Code() == RegexCode.Setloopatomic)
                            {
                                BleFar(loopEnd);
                            }
                            else
                            {
                                Ble(loopEnd);
                            }

                            if (IsRightToLeft())
                            {
                                Leftcharnext();
                            }
                            else
                            {
                                Rightcharnext();
                            }

                            if (Code() == RegexCode.Setloop || Code() == RegexCode.Setloopatomic)
                            {
                                EmitTimeoutCheck();
                                EmitMatchCharacterClass(_strings![Operand(0)], IsCaseInsensitive());
                                BrtrueFar(loopCondition);
                            }
                            else
                            {
                                if (IsCaseInsensitive())
                                {
                                    CallToLower();
                                }

                                Ldc(Operand(0));
                                if (Code() == RegexCode.Oneloop || Code() == RegexCode.Oneloopatomic)
                                {
                                    Beq(loopCondition);
                                }
                                else
                                {
                                    Debug.Assert(Code() == RegexCode.Notoneloop || Code() == RegexCode.Notoneloopatomic);
                                    Bne(loopCondition);
                                }
                            }

                            Ldloc(_runtextposLocal!);
                            Ldc(1);
                            Sub(IsRightToLeft());
                            Stloc(_runtextposLocal!);
                        }

                        // loopEnd:
                        MarkLabel(loopEnd);
                        if (Code() != RegexCode.Oneloopatomic && Code() != RegexCode.Notoneloopatomic && Code() != RegexCode.Setloopatomic)
                        {
                            // if (len <= i) goto advance;
                            Ldloc(lenLocal);
                            Ldloc(iLocal);
                            Ble(AdvanceLabel());

                            // TrackPush(len - i - 1, runtextpos - Bump())
                            ReadyPushTrack();
                            Ldloc(lenLocal);
                            Ldloc(iLocal);
                            Sub();
                            Ldc(1);
                            Sub();
                            DoPush();

                            ReadyPushTrack();
                            Ldloc(_runtextposLocal!);
                            Ldc(1);
                            Sub(IsRightToLeft());
                            DoPush();

                            Track();
                        }
                        break;
                    }

                case RegexCode.Oneloop | RegexCode.Back:
                case RegexCode.Notoneloop | RegexCode.Back:
                case RegexCode.Setloop | RegexCode.Back:
                case RegexCode.Oneloop | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Notoneloop | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Setloop | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Oneloop | RegexCode.Ci | RegexCode.Back:
                case RegexCode.Notoneloop | RegexCode.Ci | RegexCode.Back:
                case RegexCode.Setloop | RegexCode.Ci | RegexCode.Back:
                case RegexCode.Oneloop | RegexCode.Ci | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Notoneloop | RegexCode.Ci | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Setloop | RegexCode.Ci | RegexCode.Rtl | RegexCode.Back:
                    //: Trackframe(2);
                    //: int i   = Tracked(0);
                    //: int pos = Tracked(1);
                    //: Textto(pos);
                    //: if (i > 0)
                    //:     Track(i - 1, pos - 1);
                    //: Advance(2);
                    PopTrack();
                    Stloc(_runtextposLocal!);
                    PopTrack();
                    using (RentedLocalBuilder posLocal = RentInt32Local())
                    {
                        Stloc(posLocal);
                        Ldloc(posLocal);
                        Ldc(0);
                        BleFar(AdvanceLabel());
                        ReadyPushTrack();
                        Ldloc(posLocal);
                    }
                    Ldc(1);
                    Sub();
                    DoPush();
                    ReadyPushTrack();
                    Ldloc(_runtextposLocal!);
                    Ldc(1);
                    Sub(IsRightToLeft());
                    DoPush();
                    Trackagain();
                    Advance();
                    break;

                case RegexCode.Onelazy:
                case RegexCode.Notonelazy:
                case RegexCode.Setlazy:
                case RegexCode.Onelazy | RegexCode.Rtl:
                case RegexCode.Notonelazy | RegexCode.Rtl:
                case RegexCode.Setlazy | RegexCode.Rtl:
                case RegexCode.Onelazy | RegexCode.Ci:
                case RegexCode.Notonelazy | RegexCode.Ci:
                case RegexCode.Setlazy | RegexCode.Ci:
                case RegexCode.Onelazy | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Notonelazy | RegexCode.Ci | RegexCode.Rtl:
                case RegexCode.Setlazy | RegexCode.Ci | RegexCode.Rtl:
                    //: int c = Operand(1);
                    //: if (c > Rightchars())
                    //:     c = Rightchars();
                    //: if (c > 0)
                    //:     Track(c - 1, Textpos());
                    {
                        int c = Operand(1);
                        if (c == 0)
                        {
                            break;
                        }

                        if (!IsRightToLeft())
                        {
                            Ldloc(_runtextendLocal!);
                            Ldloc(_runtextposLocal!);
                        }
                        else
                        {
                            Ldloc(_runtextposLocal!);
                            Ldloc(_runtextbegLocal!);
                        }
                        Sub();
                        using (RentedLocalBuilder cLocal = RentInt32Local())
                        {
                            Stloc(cLocal);
                            if (c != int.MaxValue)
                            {
                                Label l4 = DefineLabel();
                                Ldloc(cLocal);
                                Ldc(c);
                                Blt(l4);
                                Ldc(c);
                                Stloc(cLocal);
                                MarkLabel(l4);
                            }
                            Ldloc(cLocal);
                            Ldc(0);
                            Ble(AdvanceLabel());
                            ReadyPushTrack();
                            Ldloc(cLocal);
                        }
                        Ldc(1);
                        Sub();
                        DoPush();
                        PushTrack(_runtextposLocal!);
                        Track();
                        break;
                    }

                case RegexCode.Onelazy | RegexCode.Back:
                case RegexCode.Notonelazy | RegexCode.Back:
                case RegexCode.Setlazy | RegexCode.Back:
                case RegexCode.Onelazy | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Notonelazy | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Setlazy | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Onelazy | RegexCode.Ci | RegexCode.Back:
                case RegexCode.Notonelazy | RegexCode.Ci | RegexCode.Back:
                case RegexCode.Setlazy | RegexCode.Ci | RegexCode.Back:
                case RegexCode.Onelazy | RegexCode.Ci | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Notonelazy | RegexCode.Ci | RegexCode.Rtl | RegexCode.Back:
                case RegexCode.Setlazy | RegexCode.Ci | RegexCode.Rtl | RegexCode.Back:
                    //: Trackframe(2);
                    //: int pos = Tracked(1);
                    //: Textto(pos);
                    //: if (Rightcharnext() != (char)Operand(0))
                    //:     break Backward;
                    //: int i = Tracked(0);
                    //: if (i > 0)
                    //:     Track(i - 1, pos + 1);

                    PopTrack();
                    Stloc(_runtextposLocal!);
                    PopTrack();
                    using (RentedLocalBuilder iLocal = RentInt32Local())
                    {
                        Stloc(iLocal);

                        if (!IsRightToLeft())
                        {
                            Rightcharnext();
                        }
                        else
                        {
                            Leftcharnext();
                        }

                        if (Code() == RegexCode.Setlazy)
                        {
                            EmitMatchCharacterClass(_strings![Operand(0)], IsCaseInsensitive());
                            BrfalseFar(_backtrack);
                        }
                        else
                        {
                            if (IsCaseInsensitive())
                            {
                                CallToLower();
                            }

                            Ldc(Operand(0));
                            if (Code() == RegexCode.Onelazy)
                            {
                                BneFar(_backtrack);
                            }
                            else
                            {
                                BeqFar(_backtrack);
                            }
                        }

                        Ldloc(iLocal);
                        Ldc(0);
                        BleFar(AdvanceLabel());
                        ReadyPushTrack();
                        Ldloc(iLocal);
                    }
                    Ldc(1);
                    Sub();
                    DoPush();
                    PushTrack(_runtextposLocal!);
                    Trackagain();
                    Advance();
                    break;

                default:
                    Debug.Fail($"Unimplemented state: {_regexopcode:X8}");
                    break;
            }
        }

        /// <summary>Emits a a check for whether the character is in the specified character class.</summary>
        /// <remarks>The character to be checked has already been loaded onto the stack.</remarks>
        private void EmitMatchCharacterClass(string charClass, bool caseInsensitive)
        {
            // We need to perform the equivalent of calling RegexRunner.CharInClass(ch, charClass),
            // but that call is relatively expensive.  Before we fall back to it, we try to optimize
            // some common cases for which we can do much better, such as known character classes
            // for which we can call a dedicated method, or a fast-path for ASCII using a lookup table.

            // First, see if the char class is a built-in one for which there's a better function
            // we can just call directly.  Everything in this section must work correctly for both
            // case-sensitive and case-insensitive modes, regardless of culture.
            switch (charClass)
            {
                case RegexCharClass.AnyClass:
                    // true
                    Pop();
                    Ldc(1);
                    return;

                case RegexCharClass.DigitClass:
                    // char.IsDigit(ch)
                    Call(s_charIsDigitMethod);
                    return;

                case RegexCharClass.NotDigitClass:
                    // !char.IsDigit(ch)
                    Call(s_charIsDigitMethod);
                    Ldc(0);
                    Ceq();
                    return;

                case RegexCharClass.SpaceClass:
                    // char.IsWhiteSpace(ch)
                    Call(s_charIsWhiteSpaceMethod);
                    return;

                case RegexCharClass.NotSpaceClass:
                    // !char.IsWhiteSpace(ch)
                    Call(s_charIsWhiteSpaceMethod);
                    Ldc(0);
                    Ceq();
                    return;
            }

            // If we're meant to be doing a case-insensitive lookup, and if we're not using the invariant culture,
            // lowercase the input.  If we're using the invariant culture, we may still end up calling ToLower later
            // on, but we may also be able to avoid it, in particular in the case of our lookup table, where we can
            // generate the lookup table already factoring in the invariant case sensitivity.  There are multiple
            // special-code paths between here and the lookup table, but we only take those if invariant is false;
            // if it were true, they'd need to use CallToLower().
            bool invariant = false;
            if (caseInsensitive)
            {
                invariant = UseToLowerInvariant;
                if (!invariant)
                {
                    CallToLower();
                }
            }

            // Next, handle simple sets of one range, e.g. [A-Z], [0-9], etc.  This includes some built-in classes, like ECMADigitClass.
            if (!invariant && RegexCharClass.TryGetSingleRange(charClass, out char lowInclusive, out char highInclusive))
            {
                if (lowInclusive == highInclusive)
                {
                    // ch == charClass[3]
                    Ldc(lowInclusive);
                    Ceq();
                }
                else
                {
                    // (uint)ch - lowInclusive < highInclusive - lowInclusive + 1
                    Ldc(lowInclusive);
                    Sub();
                    Ldc(highInclusive - lowInclusive + 1);
                    CltUn();
                }

                // Negate the answer if the negation flag was set
                if (RegexCharClass.IsNegated(charClass))
                {
                    Ldc(0);
                    Ceq();
                }

                return;
            }

            // Next if the character class contains nothing but a single Unicode category, we can calle char.GetUnicodeCategory and
            // compare against it.  It has a fast-lookup path for ASCII, so is as good or better than any lookup we'd generate (plus
            // we get smaller code), and it's what we'd do for the fallback (which we get to avoid generating) as part of CharInClass.
            if (!invariant && RegexCharClass.TryGetSingleUnicodeCategory(charClass, out UnicodeCategory category, out bool negated))
            {
                // char.GetUnicodeCategory(ch) == category
                Call(s_charGetUnicodeInfo);
                Ldc((int)category);
                Ceq();
                if (negated)
                {
                    Ldc(0);
                    Ceq();
                }

                return;
            }

            // All checks after this point require reading the input character multiple times,
            // so we store it into a temporary local.
            using RentedLocalBuilder tempLocal = RentInt32Local();
            Stloc(tempLocal);

            // Next, if there's only 2 or 3 chars in the set (fairly common due to the sets we create for prefixes),
            // it's cheaper and smaller to compare against each than it is to use a lookup table.
            if (!invariant && !RegexCharClass.IsNegated(charClass))
            {
                Span<char> setChars = stackalloc char[4];
                int numChars = RegexCharClass.GetSetChars(charClass, setChars);
                if (numChars is 2 or 3)
                {
                    if ((setChars[0] | 0x20) == setChars[1]) // special-case common case of an upper and lowercase ASCII letter combination
                    {
                        // ((ch | 0x20) == setChars[1])
                        Ldloc(tempLocal);
                        Ldc(0x20);
                        Or();
                        Ldc(setChars[1]);
                        Ceq();
                    }
                    else
                    {
                        // (ch == setChars[0]) | (ch == setChars[1])
                        Ldloc(tempLocal);
                        Ldc(setChars[0]);
                        Ceq();
                        Ldloc(tempLocal);
                        Ldc(setChars[1]);
                        Ceq();
                        Or();
                    }

                    // | (ch == setChars[2])
                    if (numChars == 3)
                    {
                        Ldloc(tempLocal);
                        Ldc(setChars[2]);
                        Ceq();
                        Or();
                    }

                    return;
                }
                else if (numChars == 4 &&
                         (setChars[0] | 0x20) == setChars[1] &&
                         (setChars[2] | 0x20) == setChars[3])
                {
                    // ((ch | 0x20) == setChars[1])
                    Ldloc(tempLocal);
                    Ldc(0x20);
                    Or();
                    Ldc(setChars[1]);
                    Ceq();

                    // ((ch | 0x20) == setChars[3])
                    Ldloc(tempLocal);
                    Ldc(0x20);
                    Or();
                    Ldc(setChars[3]);
                    Ceq();

                    Or();
                    return;
                }
            }

            using RentedLocalBuilder resultLocal = RentInt32Local();

            // Analyze the character set more to determine what code to generate.
            RegexCharClass.CharClassAnalysisResults analysis = RegexCharClass.Analyze(charClass);

            // Helper method that emits a call to RegexRunner.CharInClass(ch{.ToLowerInvariant()}, charClass)
            void EmitCharInClass()
            {
                Ldloc(tempLocal);
                if (invariant)
                {
                    CallToLower();
                }
                Ldstr(charClass);
                Call(s_charInClassMethod);
                Stloc(resultLocal);
            }

            Label doneLabel = DefineLabel();
            Label comparisonLabel = DefineLabel();

            if (!invariant) // if we're being asked to do a case insensitive, invariant comparison, use the lookup table
            {
                if (analysis.ContainsNoAscii)
                {
                    // We determined that the character class contains only non-ASCII,
                    // for example if the class were [\p{IsGreek}\p{IsGreekExtended}], which is
                    // the same as [\u0370-\u03FF\u1F00-1FFF]. (In the future, we could possibly
                    // extend the analysis to produce a known lower-bound and compare against
                    // that rather than always using 128 as the pivot point.)

                    // ch >= 128 && RegexRunner.CharInClass(ch, "...")
                    Ldloc(tempLocal);
                    Ldc(128);
                    Blt(comparisonLabel);
                    EmitCharInClass();
                    Br(doneLabel);
                    MarkLabel(comparisonLabel);
                    Ldc(0);
                    Stloc(resultLocal);
                    MarkLabel(doneLabel);
                    Ldloc(resultLocal);
                    return;
                }

                if (analysis.AllAsciiContained)
                {
                    // We determined that every ASCII character is in the class, for example
                    // if the class were the negated example from case 1 above:
                    // [^\p{IsGreek}\p{IsGreekExtended}].

                    // ch < 128 || RegexRunner.CharInClass(ch, "...")
                    Ldloc(tempLocal);
                    Ldc(128);
                    Blt(comparisonLabel);
                    EmitCharInClass();
                    Br(doneLabel);
                    MarkLabel(comparisonLabel);
                    Ldc(1);
                    Stloc(resultLocal);
                    MarkLabel(doneLabel);
                    Ldloc(resultLocal);
                    return;
                }
            }

            // Now, our big hammer is to generate a lookup table that lets us quickly index by character into a yes/no
            // answer as to whether the character is in the target character class.  However, we don't want to store
            // a lookup table for every possible character for every character class in the regular expression; at one
            // bit for each of 65K characters, that would be an 8K bitmap per character class.  Instead, we handle the
            // common case of ASCII input via such a lookup table, which at one bit for each of 128 characters is only
            // 16 bytes per character class.  We of course still need to be able to handle inputs that aren't ASCII, so
            // we check the input against 128, and have a fallback if the input is >= to it.  Determining the right
            // fallback could itself be expensive.  For example, if it's possible that a value >= 128 could match the
            // character class, we output a call to RegexRunner.CharInClass, but we don't want to have to enumerate the
            // entire character class evaluating every character against it, just to determine whether it's a match.
            // Instead, we employ some quick heuristics that will always ensure we provide a correct answer even if
            // we could have sometimes generated better code to give that answer.

            // Generate the lookup table to store 128 answers as bits. We use a const string instead of a byte[] / static
            // data property because it lets IL emit handle all the details for us.
            string bitVectorString = string.Create(8, (charClass, invariant), static (dest, state) => // String length is 8 chars == 16 bytes == 128 bits.
            {
                for (int i = 0; i < 128; i++)
                {
                    char c = (char)i;
                    bool isSet = state.invariant ?
                        RegexCharClass.CharInClass(char.ToLowerInvariant(c), state.charClass) :
                        RegexCharClass.CharInClass(c, state.charClass);
                    if (isSet)
                    {
                        dest[i >> 4] |= (char)(1 << (i & 0xF));
                    }
                }
            });

            // We determined that the character class may contain ASCII, so we
            // output the lookup against the lookup table.

            // ch < 128 ? (bitVectorString[ch >> 4] & (1 << (ch & 0xF))) != 0 :
            Ldloc(tempLocal);
            Ldc(128);
            Bge(comparisonLabel);
            Ldstr(bitVectorString);
            Ldloc(tempLocal);
            Ldc(4);
            Shr();
            Call(s_stringGetCharsMethod);
            Ldc(1);
            Ldloc(tempLocal);
            Ldc(15);
            And();
            Ldc(31);
            And();
            Shl();
            And();
            Ldc(0);
            CgtUn();
            Stloc(resultLocal);
            Br(doneLabel);
            MarkLabel(comparisonLabel);

            if (analysis.ContainsOnlyAscii)
            {
                // We know that all inputs that could match are ASCII, for example if the
                // character class were [A-Za-z0-9], so since the ch is now known to be >= 128, we
                // can just fail the comparison.
                Ldc(0);
                Stloc(resultLocal);
            }
            else if (analysis.AllNonAsciiContained)
            {
                // We know that all non-ASCII inputs match, for example if the character
                // class were [^\r\n], so since we just determined the ch to be >= 128, we can just
                // give back success.
                Ldc(1);
                Stloc(resultLocal);
            }
            else
            {
                // We know that the whole class wasn't ASCII, and we don't know anything about the non-ASCII
                // characters other than that some might be included, for example if the character class
                // were [\w\d], so since ch >= 128, we need to fall back to calling CharInClass.
                EmitCharInClass();
            }
            MarkLabel(doneLabel);
            Ldloc(resultLocal);
        }

        /// <summary>Emits a timeout check.</summary>
        private void EmitTimeoutCheck()
        {
            if (!_hasTimeout)
            {
                return;
            }

            Debug.Assert(_loopTimeoutCounterLocal != null);

            // Increment counter for each loop iteration.
            Ldloc(_loopTimeoutCounterLocal);
            Ldc(1);
            Add();
            Stloc(_loopTimeoutCounterLocal);

            // Emit code to check the timeout every 2048th iteration.
            Label label = DefineLabel();
            Ldloc(_loopTimeoutCounterLocal);
            Ldc(LoopTimeoutCheckCount);
            RemUn();
            Brtrue(label);
            Ldthis();
            Call(s_checkTimeoutMethod);
            MarkLabel(label);
        }

#if DEBUG
        /// <summary>Emit code to print out the current state of the runner.</summary>
        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        private void DumpBacktracking()
        {
            Mvlocfld(_runtextposLocal!, s_runtextposField);
            Mvlocfld(_runtrackposLocal!, s_runtrackposField);
            Mvlocfld(_runstackposLocal!, s_runstackposField);
            Ldthis();
            Call(s_dumpStateM);

            var sb = new StringBuilder();
            if (_backpos > 0)
            {
                sb.Append($"{_backpos:D6} ");
            }
            else
            {
                sb.Append("       ");
            }
            sb.Append(_code!.OpcodeDescription(_codepos));

            if ((_regexopcode & RegexCode.Back) != 0)
            {
                sb.Append(" Back");
            }

            if ((_regexopcode & RegexCode.Back2) != 0)
            {
                sb.Append(" Back2");
            }

            Ldstr(sb.ToString());
            Call(s_debugWriteLine!);
        }
#endif
    }
}
