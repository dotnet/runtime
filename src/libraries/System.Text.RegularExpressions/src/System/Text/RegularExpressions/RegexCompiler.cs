// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// RegexCompiler translates a block of RegexCode to MSIL, and creates a subclass of the RegexRunner type.
    /// </summary>
    internal abstract class RegexCompiler
    {
        private static readonly FieldInfo s_runtextbegField = RegexRunnerField("runtextbeg");
        private static readonly FieldInfo s_runtextendField = RegexRunnerField("runtextend");
        private static readonly FieldInfo s_runtextstartField = RegexRunnerField("runtextstart");
        private static readonly FieldInfo s_runtextposField = RegexRunnerField("runtextpos");
        private static readonly FieldInfo s_runtextField = RegexRunnerField("runtext");
        private static readonly FieldInfo s_runstackField = RegexRunnerField("runstack");

        private static readonly MethodInfo s_captureMethod = RegexRunnerMethod("Capture");
        private static readonly MethodInfo s_transferCaptureMethod = RegexRunnerMethod("TransferCapture");
        private static readonly MethodInfo s_uncaptureMethod = RegexRunnerMethod("Uncapture");
        private static readonly MethodInfo s_isMatchedMethod = RegexRunnerMethod("IsMatched");
        private static readonly MethodInfo s_matchLengthMethod = RegexRunnerMethod("MatchLength");
        private static readonly MethodInfo s_matchIndexMethod = RegexRunnerMethod("MatchIndex");
        private static readonly MethodInfo s_isBoundaryMethod = RegexRunnerMethod("IsBoundary");
        private static readonly MethodInfo s_isWordCharMethod = RegexRunnerMethod("IsWordChar");
        private static readonly MethodInfo s_isECMABoundaryMethod = RegexRunnerMethod("IsECMABoundary");
        private static readonly MethodInfo s_crawlposMethod = RegexRunnerMethod("Crawlpos");
        private static readonly MethodInfo s_charInClassMethod = RegexRunnerMethod("CharInClass");
        private static readonly MethodInfo s_checkTimeoutMethod = RegexRunnerMethod("CheckTimeout");

        private static readonly MethodInfo s_charIsDigitMethod = typeof(char).GetMethod("IsDigit", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charIsWhiteSpaceMethod = typeof(char).GetMethod("IsWhiteSpace", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charGetUnicodeInfo = typeof(char).GetMethod("GetUnicodeCategory", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charToLowerInvariantMethod = typeof(char).GetMethod("ToLowerInvariant", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_cultureInfoGetCurrentCultureMethod = typeof(CultureInfo).GetMethod("get_CurrentCulture")!;
        private static readonly MethodInfo s_cultureInfoGetTextInfoMethod = typeof(CultureInfo).GetMethod("get_TextInfo")!;
        private static readonly MethodInfo s_spanGetItemMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Item", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_spanGetLengthMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Length")!;
        private static readonly MethodInfo s_memoryMarshalGetReference = typeof(MemoryMarshal).GetMethod("GetReference", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfChar = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfSpan = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnyCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnyCharCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnySpan = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanLastIndexOfChar = typeof(MemoryExtensions).GetMethod("LastIndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanSliceIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_spanSliceIntIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[] { typeof(int), typeof(int) })!;
        private static readonly MethodInfo s_spanStartsWith = typeof(MemoryExtensions).GetMethod("StartsWith", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_stringAsSpanMethod = typeof(MemoryExtensions).GetMethod("AsSpan", new Type[] { typeof(string) })!;
        private static readonly MethodInfo s_stringGetCharsMethod = typeof(string).GetMethod("get_Chars", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_textInfoToLowerMethod = typeof(TextInfo).GetMethod("ToLower", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_arrayResize = typeof(Array).GetMethod("Resize")!.MakeGenericMethod(typeof(int));

        /// <summary>The ILGenerator currently in use.</summary>
        protected ILGenerator? _ilg;
        /// <summary>The options for the expression.</summary>
        protected RegexOptions _options;
        /// <summary>The code written for the expression.</summary>
        protected RegexCode? _code;
        /// <summary>Whether this expression has a non-infinite timeout.</summary>
        protected bool _hasTimeout;

        /// <summary>Pool of Int32 LocalBuilders.</summary>
        private Stack<LocalBuilder>? _int32LocalsPool;
        /// <summary>Pool of ReadOnlySpan of char locals.</summary>
        private Stack<LocalBuilder>? _readOnlySpanCharLocalsPool;

        /// <summary>Local representing a cached TextInfo for the culture to use for all case-insensitive operations.</summary>
        private LocalBuilder? _textInfo;
        /// <summary>Local representing a timeout counter for loops (set loops and node loops).</summary>
        private LocalBuilder? _loopTimeoutCounter;
        /// <summary>A frequency with which the timeout should be validated.</summary>
        private const int LoopTimeoutCheckCount = 2048;

        private static FieldInfo RegexRunnerField(string fieldname) => typeof(RegexRunner).GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;

        private static MethodInfo RegexRunnerMethod(string methname) => typeof(RegexRunner).GetMethod(methname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;

        /// <summary>
        /// Entry point to dynamically compile a regular expression.  The expression is compiled to
        /// an in-memory assembly.
        /// </summary>
        internal static RegexRunnerFactory? Compile(string pattern, RegexCode code, RegexOptions options, bool hasTimeout) =>
            new RegexLWCGCompiler().FactoryInstanceFromCode(pattern, code, options, hasTimeout);

        /// <summary>A macro for _ilg.DefineLabel</summary>
        private Label DefineLabel() => _ilg!.DefineLabel();

        /// <summary>A macro for _ilg.MarkLabel</summary>
        private void MarkLabel(Label l) => _ilg!.MarkLabel(l);

        /// <summary>A macro for _ilg.Emit(Opcodes.Ldstr, str)</summary>
        protected void Ldstr(string str) => _ilg!.Emit(OpCodes.Ldstr, str);

        /// <summary>A macro for the various forms of Ldc.</summary>
        protected void Ldc(int i) => _ilg!.Emit(OpCodes.Ldc_I4, i);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldc_I8).</summary>
        protected void LdcI8(long i) => _ilg!.Emit(OpCodes.Ldc_I8, i);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ret).</summary>
        protected void Ret() => _ilg!.Emit(OpCodes.Ret);

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

        /// <summary>A macro for _ilg.Emit(OpCodes.Sub).</summary>
        private void Sub() => _ilg!.Emit(OpCodes.Sub);

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
            _ilg!.Emit(OpCodes.Ldfld, ft);
        }

        /// <summary>A macro for Ldthis(); Ldfld(); Stloc();</summary>
        private void Mvfldloc(FieldInfo ft, LocalBuilder lt)
        {
            Ldthisfld(ft);
            Stloc(lt);
        }

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

        /// <summary>A macro for _ilg.Emit(OpCodes.Bne) (long form).</summary>
        private void BneFar(Label l) => _ilg!.Emit(OpCodes.Bne_Un, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Beq) (long form).</summary>
        private void BeqFar(Label l) => _ilg!.Emit(OpCodes.Beq, l);

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

        /// <summary>Sets the culture local to CultureInfo.CurrentCulture.</summary>
        private void InitLocalCultureInfo()
        {
            Debug.Assert(_textInfo != null);
            Call(s_cultureInfoGetCurrentCultureMethod);
            Callvirt(s_cultureInfoGetTextInfoMethod);
            Stloc(_textInfo);
        }

        /// <summary>Whether ToLower operations should be performed with the invariant culture as opposed to the one in <see cref="_textInfo"/>.</summary>
        private bool UseToLowerInvariant => _textInfo == null || (_options & RegexOptions.CultureInvariant) != 0;

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
                Ldloc(_textInfo!);
                Ldloc(currentCharLocal);
                Callvirt(s_textInfoToLowerMethod);
            }
        }

        /// <summary>Generates the implementation for FindFirstChar.</summary>
        protected void EmitFindFirstChar()
        {
            Debug.Assert(_code != null);
            _int32LocalsPool?.Clear();
            _readOnlySpanCharLocalsPool?.Clear();

            LocalBuilder inputSpan = DeclareReadOnlySpanChar();
            LocalBuilder pos = DeclareInt32();
            LocalBuilder end = DeclareInt32();

            _textInfo = null;
            if ((_options & RegexOptions.CultureInvariant) == 0)
            {
                bool needsCulture = _code.FindOptimizations.FindMode switch
                {
                    FindNextStartingPositionMode.FixedLiteral_LeftToRight_CaseInsensitive or
                    FindNextStartingPositionMode.FixedSets_LeftToRight_CaseInsensitive or
                    FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseInsensitive => true,

                    _ when _code.FindOptimizations.FixedDistanceSets is List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)> sets => sets.Exists(set => set.CaseInsensitive),

                    _ => false,
                };

                if (needsCulture)
                {
                    _textInfo = DeclareTextInfo();
                    InitLocalCultureInfo();
                }
            }

            // Load necessary locals
            // int pos = base.runtextpos;
            // int end = base.runtextend;
            // ReadOnlySpan<char> inputSpan = base.runtext.AsSpan();
            Mvfldloc(s_runtextposField, pos);
            Mvfldloc(s_runtextendField, end);
            Ldthisfld(s_runtextField);
            Call(s_stringAsSpanMethod);
            Stloc(inputSpan);

            // Generate length check.  If the input isn't long enough to possibly match, fail quickly.
            // It's rare for min required length to be 0, so we don't bother special-casing the check,
            // especially since we want the "return false" code regardless.
            int minRequiredLength = _code.Tree.MinRequiredLength;
            Debug.Assert(minRequiredLength >= 0);
            Label returnFalse = DefineLabel();
            Label finishedLengthCheck = DefineLabel();

            // if (pos > end - _code.Tree.MinRequiredLength)
            // {
            //     base.runtextpos = end;
            //     return false;
            // }
            Ldloc(pos);
            Ldloc(end);
            if (minRequiredLength > 0)
            {
                Ldc(minRequiredLength);
                Sub();
            }
            Ble(finishedLengthCheck);

            MarkLabel(returnFalse);
            Ldthis();
            Ldloc(end);

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
                    EmitIndexOf_LeftToRight(_code.FindOptimizations.LeadingCaseSensitivePrefix);
                    break;

                case FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseSensitive:
                case FindNextStartingPositionMode.LeadingSet_LeftToRight_CaseInsensitive:
                case FindNextStartingPositionMode.FixedSets_LeftToRight_CaseSensitive:
                case FindNextStartingPositionMode.FixedSets_LeftToRight_CaseInsensitive:
                    Debug.Assert(_code.FindOptimizations.FixedDistanceSets is { Count: > 0 });
                    EmitFixedSet_LeftToRight();
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
                                Ldloc(pos);
                                Ldthisfld(s_runtextbegField);
                                Ble(l1);
                                Br(returnFalse);
                                MarkLabel(l1);
                            }
                            Ldc(1);
                            Ret();
                            return true;

                        case RegexPrefixAnalyzer.Start:
                            {
                                Label l1 = DefineLabel();
                                Ldloc(pos);
                                Ldthisfld(s_runtextstartField);
                                Ble(l1);
                                Br(returnFalse);
                                MarkLabel(l1);
                            }
                            Ldc(1);
                            Ret();
                            return true;

                        case RegexPrefixAnalyzer.EndZ:
                            {
                                Label l1 = DefineLabel();
                                Ldloc(pos);
                                Ldloc(end);
                                Ldc(1);
                                Sub();
                                Bge(l1);
                                Ldthis();
                                Ldloc(end);
                                Ldc(1);
                                Sub();
                                Stfld(s_runtextposField);
                                MarkLabel(l1);
                            }
                            Ldc(1);
                            Ret();
                            return true;

                        case RegexPrefixAnalyzer.End:
                            {
                                Label l1 = DefineLabel();
                                Ldloc(pos);
                                Ldloc(end);
                                Bge(l1);
                                Ldthis();
                                Ldloc(end);
                                Stfld(s_runtextposField);
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

                                Label atBeginningOfLine = DefineLabel();

                                // if (pos > runtextbeg...
                                Ldloc(pos!);
                                Ldthisfld(s_runtextbegField);
                                Ble(atBeginningOfLine);

                                // ... && inputSpan[pos - 1] != '\n') { ... }
                                Ldloca(inputSpan);
                                Ldloc(pos);
                                Ldc(1);
                                Sub();
                                Call(s_spanGetItemMethod);
                                LdindU2();
                                Ldc('\n');
                                Beq(atBeginningOfLine);

                                // int tmp = inputSpan.Slice(pos).IndexOf('\n');
                                Ldloca(inputSpan);
                                Ldloc(pos);
                                Call(s_spanSliceIntMethod);
                                Ldc('\n');
                                Call(s_spanIndexOfChar);
                                using (RentedLocalBuilder newlinePos = RentInt32Local())
                                {
                                    Stloc(newlinePos);

                                    // if (newlinePos < 0 || newlinePos + pos + 1 > end)
                                    // {
                                    //     base.runtextpos = end;
                                    //     return false;
                                    // }
                                    Ldloc(newlinePos);
                                    Ldc(0);
                                    Blt(returnFalse);
                                    Ldloc(newlinePos);
                                    Ldloc(pos);
                                    Add();
                                    Ldc(1);
                                    Add();
                                    Ldloc(end);
                                    Bgt(returnFalse);

                                    // pos += newlinePos + 1;
                                    Ldloc(pos);
                                    Ldloc(newlinePos);
                                    Add();
                                    Ldc(1);
                                    Add();
                                    Stloc(pos);
                                }

                                MarkLabel(atBeginningOfLine);
                            }
                            break;
                    }
                }

                return false;
            }

            void EmitIndexOf_LeftToRight(string prefix)
            {
                using RentedLocalBuilder i = RentInt32Local();

                // int i = inputSpan.Slice(pos, end - pos).IndexOf(prefix);
                Ldloca(inputSpan);
                Ldloc(pos);
                Ldloc(end);
                Ldloc(pos);
                Sub();
                Call(s_spanSliceIntIntMethod);
                Ldstr(prefix);
                Call(s_stringAsSpanMethod);
                Call(s_spanIndexOfSpan);
                Stloc(i);

                // if (i < 0) goto ReturnFalse;
                Ldloc(i);
                Ldc(0);
                BltFar(returnFalse);

                // base.runtextpos = pos + i;
                // return true;
                Ldthis();
                Ldloc(pos);
                Ldloc(i);
                Add();
                Stfld(s_runtextposField);
                Ldc(1);
                Ret();
            }

            void EmitFixedSet_LeftToRight()
            {
                List<(char[]? Chars, string Set, int Distance, bool CaseInsensitive)>? sets = _code.FindOptimizations.FixedDistanceSets;
                (char[]? Chars, string Set, int Distance, bool CaseInsensitive) primarySet = sets![0];
                const int MaxSets = 4;
                int setsToUse = Math.Min(sets.Count, MaxSets);

                using RentedLocalBuilder iLocal = RentInt32Local();
                using RentedLocalBuilder textSpanLocal = RentReadOnlySpanCharLocal();

                // ReadOnlySpan<char> span = inputSpan.Slice(pos, end - pos);
                Ldloca(inputSpan);
                Ldloc(pos);
                Ldloc(end);
                Ldloc(pos);
                Sub();
                Call(s_spanSliceIntIntMethod);
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
                        // slice.Slice(iLocal + primarySet.Distance);
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
                        // slice.Slice(primarySet.Distance)
                        Ldloca(textSpanLocal);
                        Ldc(primarySet.Distance);
                        Call(s_spanSliceIntMethod);
                    }
                    else
                    {
                        // slice
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

                    // if (i >= slice.Length - (minRequiredLength - 1)) goto returnFalse;
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

                // if (!CharInClass(slice[i], prefix[0], "...")) continue;
                // if (!CharInClass(slice[i + 1], prefix[1], "...")) continue;
                // if (!CharInClass(slice[i + 2], prefix[2], "...")) continue;
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

                // base.runtextpos = pos + i;
                // return true;
                Ldthis();
                Ldloc(pos);
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

                    // base.runtextpos = end;
                    // return false;
                    BrFar(returnFalse);
                }
            }
        }

        /// <summary>Generates the implementation for Go.</summary>
        protected void EmitGo()
        {
            // In .NET Framework and up through .NET Core 3.1, the code generated for RegexOptions.Compiled was effectively an unrolled
            // version of what RegexInterpreter would process.  The RegexNode tree would be turned into a series of opcodes via
            // RegexWriter; the interpreter would then sit in a loop processing those opcodes, and the RegexCompiler iterated through the
            // opcodes generating code for each equivalent to what the interpreter would do albeit with some decisions made at compile-time
            // rather than at run-time.  This approach, however, lead to complicated code that wasn't pay-for-play (e.g. a big backtracking
            // jump table that all compilations went through even if there was no backtracking), that didn't factor in the shape of the
            // tree (e.g. it's difficult to add optimizations based on interactions between nodes in the graph), and that didn't read well
            // when decompiled from IL to C# or when directly emitted as C# as part of a source generator.
            //
            // This implementation is instead based on directly walking the RegexNode tree and outputting code for each node in the graph.
            // A dedicated for each kind of RegexNode emits the code necessary to handle that node's processing, including recursively
            // calling the relevant function for any of its children nodes.  Backtracking is handled not via a giant jump table, but instead
            // by emitting direct jumps to each backtracking construct.  This is achieved by having all match failures jump to a "done"
            // label that can be changed by a previous emitter, e.g. before EmitLoop returns, it ensures that "doneLabel" is set to the
            // label that code should jump back to when backtracking.  That way, a subsequent EmitXx function doesn't need to know exactly
            // where to jump: it simply always jumps to "doneLabel" on match failure, and "doneLabel" is always configured to point to
            // the right location.  In an expression without backtracking, or before any backtracking constructs have been encountered,
            // "doneLabel" is simply the final return location from the Go method that will undo any captures and exit, signaling to
            // the calling scan loop that nothing was matched.

            Debug.Assert(_code != null);
            _int32LocalsPool?.Clear();
            _readOnlySpanCharLocalsPool?.Clear();

            // Get the root Capture node of the tree.
            RegexNode node = _code.Tree.Root;
            Debug.Assert(node.Type == RegexNode.Capture, "Every generated tree should begin with a capture node");
            Debug.Assert(node.ChildCount() == 1, "Capture nodes should have one child");

            // Skip the Capture node. We handle the implicit root capture specially.
            node = node.Child(0);


            // In some limited cases, FindFirstChar will only return true if it successfully matched the whole expression.
            // We can special case these to do essentially nothing in Go other than emit the capture.
            switch (node.Type)
            {
                case RegexNode.Multi or RegexNode.Notone or RegexNode.One or RegexNode.Set when !IsCaseInsensitive(node):
                    // This is the case for single and multiple characters, though the whole thing is only guaranteed
                    // to have been validated in FindFirstChar when doing case-sensitive comparison.
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
                    return;

                // The source generator special-cases RegexNode.Empty, for purposes of code learning rather than
                // performance.  Since that's not applicable to RegexCompiler, that code isn't mirrored here.
            }

            // Initialize the main locals used throughout the implementation.
            LocalBuilder inputSpan = DeclareReadOnlySpanChar();
            LocalBuilder originalPos = DeclareInt32();
            LocalBuilder pos = DeclareInt32();
            LocalBuilder slice = DeclareReadOnlySpanChar();
            LocalBuilder end = DeclareInt32();
            Label stopSuccessLabel = DefineLabel();
            Label doneLabel = DefineLabel();
            Label originalDoneLabel = doneLabel;
            if (_hasTimeout)
            {
                _loopTimeoutCounter = DeclareInt32();
            }

            // CultureInfo culture = CultureInfo.CurrentCulture; // only if the whole expression or any subportion is ignoring case, and we're not using invariant
            InitializeCultureForGoIfNecessary();

            // ReadOnlySpan<char> inputSpan = base.runtext.AsSpan();
            // int end = base.runtextend;
            Ldthisfld(s_runtextField);
            Call(s_stringAsSpanMethod);
            Stloc(inputSpan);
            Mvfldloc(s_runtextendField, end);

            // int pos = base.runtextpos;
            // int originalpos = pos;
            Ldthisfld(s_runtextposField);
            Stloc(pos);
            Ldloc(pos);
            Stloc(originalPos);

            // int stackpos = 0;
            LocalBuilder stackpos = DeclareInt32();
            Ldc(0);
            Stloc(stackpos);

            // The implementation tries to use const indexes into the span wherever possible, which we can do
            // for all fixed-length constructs.  In such cases (e.g. single chars, repeaters, strings, etc.)
            // we know at any point in the regex exactly how far into it we are, and we can use that to index
            // into the span created at the beginning of the routine to begin at exactly where we're starting
            // in the input.  When we encounter a variable-length construct, we transfer the static value to
            // pos, slicing the inputSpan appropriately, and then zero out the static position.
            int sliceStaticPos = 0;
            SliceInputSpan();

            // Emit the code for all nodes in the tree.
            bool expressionHasCaptures = (node.Options & RegexNode.HasCapturesFlag) != 0;
            EmitNode(node);

            // Success:
            // pos += sliceStaticPos;
            // base.runtextpos = pos;
            // Capture(0, originalpos, pos);
            MarkLabel(stopSuccessLabel);
            Ldthis();
            Ldloc(pos);
            if (sliceStaticPos > 0)
            {
                Ldc(sliceStaticPos);
                Add();
                Stloc(pos);
                Ldloc(pos);
            }
            Stfld(s_runtextposField);
            Ldthis();
            Ldc(0);
            Ldloc(originalPos);
            Ldloc(pos);
            Call(s_captureMethod);

            // If the graph contained captures, undo any remaining to handle failed matches.
            if (expressionHasCaptures)
            {
                // while (base.Crawlpos() != 0) base.Uncapture();

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

            // Generated code successfully.
            return;

            static bool IsCaseInsensitive(RegexNode node) => (node.Options & RegexOptions.IgnoreCase) != 0;

            // Slices the inputSpan starting at pos until end and stores it into slice.
            void SliceInputSpan()
            {
                // slice = inputSpan.Slice(pos, end - pos);
                Ldloca(inputSpan);
                Ldloc(pos);
                Ldloc(end);
                Ldloc(pos);
                Sub();
                Call(s_spanSliceIntIntMethod);
                Stloc(slice);
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
                // if ((uint)(sliceStaticPos + requiredLength + dynamicRequiredLength - 1) >= (uint)slice.Length) goto Done;
                Debug.Assert(requiredLength > 0);
                EmitSum(sliceStaticPos + requiredLength - 1, dynamicRequiredLength);
                Ldloca(slice);
                Call(s_spanGetLengthMethod);
                BgeUnFar(doneLabel);
            }

            // Emits code to get ref slice[sliceStaticPos]
            void EmitTextSpanOffset()
            {
                Ldloc(slice);
                Call(s_memoryMarshalGetReference);
                if (sliceStaticPos > 0)
                {
                    Ldc(sliceStaticPos * sizeof(char));
                    Add();
                }
            }

            // Adds the value of sliceStaticPos into the pos local, slices textspan by the corresponding amount,
            // and zeros out sliceStaticPos.
            void TransferSliceStaticPosToPos()
            {
                if (sliceStaticPos > 0)
                {
                    // pos += sliceStaticPos;
                    Ldloc(pos);
                    Ldc(sliceStaticPos);
                    Add();
                    Stloc(pos);

                    // slice = slice.Slice(sliceStaticPos);
                    Ldloca(slice);
                    Ldc(sliceStaticPos);
                    Call(s_spanSliceIntMethod);
                    Stloc(slice);

                    // sliceStaticPos = 0;
                    sliceStaticPos = 0;
                }
            }

            // Emits the code for an alternation.
            void EmitAlternation(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Alternate, $"Unexpected type: {node.Type}");
                Debug.Assert(node.ChildCount() >= 2, $"Expected at least 2 children, found {node.ChildCount()}");

                int childCount = node.ChildCount();
                Debug.Assert(childCount >= 2);

                Label originalDoneLabel = doneLabel;

                // Both atomic and non-atomic are supported.  While a parent RegexNode.Atomic node will itself
                // successfully prevent backtracking into this child node, we can emit better / cheaper code
                // for an Alternate when it is atomic, so we still take it into account here.
                Debug.Assert(node.Next is not null);
                bool isAtomic = node.IsAtomicByParent();

                // Label to jump to when any branch completes successfully.
                Label matchLabel = DefineLabel();

                // Save off pos.  We'll need to reset this each time a branch fails.
                // startingPos = pos;
                LocalBuilder startingPos = DeclareInt32();
                Ldloc(pos);
                Stloc(startingPos);
                int startingTextSpanPos = sliceStaticPos;

                // We need to be able to undo captures in two situations:
                // - If a branch of the alternation itself contains captures, then if that branch
                //   fails to match, any captures from that branch until that failure point need to
                //   be uncaptured prior to jumping to the next branch.
                // - If the expression after the alternation contains captures, then failures
                //   to match in those expressions could trigger backtracking back into the
                //   alternation, and thus we need uncapture any of them.
                // As such, if the alternation contains captures or if it's not atomic, we need
                // to grab the current crawl position so we can unwind back to it when necessary.
                // We can do all of the uncapturing as part of falling through to the next branch.
                // If we fail in a branch, then such uncapturing will unwind back to the position
                // at the start of the alternation.  If we fail after the alternation, and the
                // matched branch didn't contain any backtracking, then the failure will end up
                // jumping to the next branch, which will unwind the captures.  And if we fail after
                // the alternation and the matched branch did contain backtracking, that backtracking
                // construct is responsible for unwinding back to its starting crawl position. If
                // it eventually ends up failing, that failure will result in jumping to the next branch
                // of the alternation, which will again dutifully unwind the remaining captures until
                // what they were at the start of the alternation.  Of course, if there are no captures
                // anywhere in the regex, we don't have to do any of that.
                LocalBuilder? startingCapturePos = null;
                if (expressionHasCaptures && ((node.Options & RegexNode.HasCapturesFlag) != 0 || !isAtomic))
                {
                    // startingCapturePos = base.Crawlpos();
                    startingCapturePos = DeclareInt32();
                    Ldthis();
                    Call(s_crawlposMethod);
                    Stloc(startingCapturePos);
                }

                // After executing the alternation, subsequent matching may fail, at which point execution
                // will need to backtrack to the alternation.  We emit a branching table at the end of the
                // alternation, with a label that will be left as the "doneLabel" upon exiting emitting the
                // alternation.  The branch table is populated with an entry for each branch of the alternation,
                // containing either the label for the last backtracking construct in the branch if such a construct
                // existed (in which case the doneLabel upon emitting that node will be different from before it)
                // or the label for the next branch.
                var labelMap = new Label[childCount];
                Label backtrackLabel = DefineLabel();

                for (int i = 0; i < childCount; i++)
                {
                    bool isLastBranch = i == childCount - 1;

                    Label nextBranch = default;
                    if (!isLastBranch)
                    {
                        // Failure to match any branch other than the last one should result
                        // in jumping to process the next branch.
                        nextBranch = DefineLabel();
                        doneLabel = nextBranch;
                    }
                    else
                    {
                        // Failure to match the last branch is equivalent to failing to match
                        // the whole alternation, which means those failures should jump to
                        // what "doneLabel" was defined as when starting the alternation.
                        doneLabel = originalDoneLabel;
                    }

                    // Emit the code for each branch.
                    EmitNode(node.Child(i));

                    // Add this branch to the backtracking table.  At this point, either the child
                    // had backtracking constructs, in which case doneLabel points to the last one
                    // and that's where we'll want to jump to, or it doesn't, in which case doneLabel
                    // still points to the nextBranch, which similarly is where we'll want to jump to.
                    if (!isAtomic)
                    {
                        // if (stackpos + 3 >= base.runstack.Length) Array.Resize(ref base.runstack, base.runstack.Length * 2);
                        // base.runstack[stackpos++] = i;
                        // base.runstack[stackpos++] = startingCapturePos;
                        // base.runstack[stackpos++] = startingPos;
                        EmitStackResizeIfNeeded(3);
                        EmitStackPush(() => Ldc(i));
                        if (startingCapturePos is not null)
                        {
                            EmitStackPush(() => Ldloc(startingCapturePos));
                        }
                        EmitStackPush(() => Ldloc(startingPos));
                    }
                    labelMap[i] = doneLabel;

                    // If we get here in the generated code, the branch completed successfully.
                    // Before jumping to the end, we need to zero out sliceStaticPos, so that no
                    // matter what the value is after the branch, whatever follows the alternate
                    // will see the same sliceStaticPos.
                    // pos += sliceStaticPos;
                    // sliceStaticPos = 0;
                    // goto matchLabel;
                    TransferSliceStaticPosToPos();
                    BrFar(matchLabel);

                    // Reset state for next branch and loop around to generate it.  This includes
                    // setting pos back to what it was at the beginning of the alternation,
                    // updating slice to be the full length it was, and if there's a capture that
                    // needs to be reset, uncapturing it.
                    if (!isLastBranch)
                    {
                        // NextBranch:
                        // pos = startingPos;
                        // slice = inputSpan.Slice(pos, end - pos);
                        // while (base.Crawlpos() > startingCapturePos) base.Uncapture();
                        MarkLabel(nextBranch);
                        Ldloc(startingPos);
                        Stloc(pos);
                        SliceInputSpan();
                        sliceStaticPos = startingTextSpanPos;
                        if (startingCapturePos is not null)
                        {
                            EmitUncaptureUntil(startingCapturePos);
                        }
                    }
                }

                // We should never fall through to this location in the generated code.  Either
                // a branch succeeded in matching and jumped to the end, or a branch failed in
                // matching and jumped to the next branch location.  We only get to this code
                // if backtracking occurs and the code explicitly jumps here based on our setting
                // "doneLabel" to the label for this section.  Thus, we only need to emit it if
                // something can backtrack to us, which can't happen if we're inside of an atomic
                // node. Thus, emit the backtracking section only if we're non-atomic.
                if (isAtomic)
                {
                    doneLabel = originalDoneLabel;
                }
                else
                {
                    doneLabel = backtrackLabel;
                    MarkLabel(backtrackLabel);

                    // startingPos = base.runstack[--stackpos];
                    // startingCapturePos = base.runstack[--stackpos];
                    // switch (base.runstack[--stackpos]) { ... } // branch number
                    EmitStackPop();
                    Stloc(startingPos);
                    if (startingCapturePos is not null)
                    {
                        EmitStackPop();
                        Stloc(startingCapturePos);
                    }
                    EmitStackPop();
                    Switch(labelMap);
                }

                // Successfully completed the alternate.
                MarkLabel(matchLabel);
                Debug.Assert(sliceStaticPos == 0);
            }

            // Emits the code to handle a backreference.
            void EmitBackreference(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Ref, $"Unexpected type: {node.Type}");

                int capnum = RegexParser.MapCaptureNumber(node.M, _code!.Caps);

                TransferSliceStaticPosToPos();

                Label backreferenceEnd = DefineLabel();

                // if (!base.IsMatched(capnum)) goto (ecmascript ? end : doneLabel);
                Ldthis();
                Ldc(capnum);
                Call(s_isMatchedMethod);
                BrfalseFar((node.Options & RegexOptions.ECMAScript) == 0 ? doneLabel : backreferenceEnd);

                using RentedLocalBuilder matchLength = RentInt32Local();
                using RentedLocalBuilder matchIndex = RentInt32Local();
                using RentedLocalBuilder i = RentInt32Local();

                // int matchLength = base.MatchLength(capnum);
                Ldthis();
                Ldc(capnum);
                Call(s_matchLengthMethod);
                Stloc(matchLength);

                // if (slice.Length < matchLength) goto doneLabel;
                Ldloca(slice);
                Call(s_spanGetLengthMethod);
                Ldloc(matchLength);
                BltFar(doneLabel);

                // int matchIndex = base.MatchIndex(capnum);
                Ldthis();
                Ldc(capnum);
                Call(s_matchIndexMethod);
                Stloc(matchIndex);

                Label condition = DefineLabel();
                Label body = DefineLabel();

                // for (int i = 0; ...)
                Ldc(0);
                Stloc(i);
                Br(condition);

                MarkLabel(body);

                // if (inputSpan[matchIndex + i] != slice[i]) goto doneLabel;
                Ldloca(inputSpan);
                Ldloc(matchIndex);
                Ldloc(i);
                Add();
                Call(s_spanGetItemMethod);
                LdindU2();
                if (IsCaseInsensitive(node))
                {
                    CallToLower();
                }
                Ldloca(slice);
                Ldloc(i);
                Call(s_spanGetItemMethod);
                LdindU2();
                if (IsCaseInsensitive(node))
                {
                    CallToLower();
                }
                BneFar(doneLabel);

                // for (...; ...; i++)
                Ldloc(i);
                Ldc(1);
                Add();
                Stloc(i);

                // for (...; i < matchLength; ...)
                MarkLabel(condition);
                Ldloc(i);
                Ldloc(matchLength);
                Blt(body);

                // pos += matchLength;
                Ldloc(pos);
                Ldloc(matchLength);
                Add();
                Stloc(pos);
                SliceInputSpan();

                MarkLabel(backreferenceEnd);
            }

            // Emits the code for an if(backreference)-then-else conditional.
            void EmitBackreferenceConditional(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Testref, $"Unexpected type: {node.Type}");
                Debug.Assert(node.ChildCount() == 2, $"Expected 2 children, found {node.ChildCount()}");

                bool isAtomic = node.IsAtomicByParent();

                // We're branching in a complicated fashion.  Make sure sliceStaticPos is 0.
                TransferSliceStaticPosToPos();

                // Get the capture number to test.
                int capnum = RegexParser.MapCaptureNumber(node.M, _code!.Caps);

                // Get the "yes" branch and the "no" branch.  The "no" branch is optional in syntax and is thus
                // somewhat likely to be Empty.
                RegexNode yesBranch = node.Child(0);
                RegexNode? noBranch = node.Child(1) is { Type: not RegexNode.Empty } childNo ? childNo : null;
                Label originalDoneLabel = doneLabel;

                Label refNotMatched = DefineLabel();
                Label endConditional = DefineLabel();

                // As with alternations, we have potentially multiple branches, each of which may contain
                // backtracking constructs, but the expression after the conditional needs a single target
                // to backtrack to.  So, we expose a single Backtrack label and track which branch was
                // followed in this resumeAt local.
                LocalBuilder resumeAt = DeclareInt32();

                // if (!base.IsMatched(capnum)) goto refNotMatched;
                Ldthis();
                Ldc(capnum);
                Call(s_isMatchedMethod);
                BrfalseFar(refNotMatched);

                // The specified capture was captured.  Run the "yes" branch.
                // If it successfully matches, jump to the end.
                EmitNode(yesBranch);
                TransferSliceStaticPosToPos();
                Label postYesDoneLabel = doneLabel;
                if (!isAtomic && postYesDoneLabel != originalDoneLabel)
                {
                    // resumeAt = 0;
                    Ldc(0);
                    Stloc(resumeAt);
                }

                bool needsEndConditional = postYesDoneLabel != originalDoneLabel || noBranch is not null;
                if (needsEndConditional)
                {
                    // goto endConditional;
                    BrFar(endConditional);
                }

                MarkLabel(refNotMatched);
                Label postNoDoneLabel = originalDoneLabel;
                if (noBranch is not null)
                {
                    // Output the no branch.
                    doneLabel = originalDoneLabel;
                    EmitNode(noBranch);
                    TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                    postNoDoneLabel = doneLabel;
                    if (!isAtomic && postNoDoneLabel != originalDoneLabel)
                    {
                        // resumeAt = 1;
                        Ldc(1);
                        Stloc(resumeAt);
                    }
                }
                else
                {
                    // There's only a yes branch.  If it's going to cause us to output a backtracking
                    // label but code may not end up taking the yes branch path, we need to emit a resumeAt
                    // that will cause the backtracking to immediately pass through this node.
                    if (!isAtomic && postYesDoneLabel != originalDoneLabel)
                    {
                        // resumeAt = 2;
                        Ldc(2);
                        Stloc(resumeAt);
                    }
                }

                if (isAtomic || (postYesDoneLabel == originalDoneLabel && postNoDoneLabel == originalDoneLabel))
                {
                    // We're atomic by our parent, so even if either child branch has backtracking constructs,
                    // we don't need to emit any backtracking logic in support, as nothing will backtrack in.
                    // Instead, we just ensure we revert back to the original done label so that any backtracking
                    // skips over this node.
                    doneLabel = originalDoneLabel;
                    if (needsEndConditional)
                    {
                        MarkLabel(endConditional);
                    }
                }
                else
                {
                    // Subsequent expressions might try to backtrack to here, so output a backtracking map based on resumeAt.

                    // Skip the backtracking section
                    // goto endConditional;
                    Debug.Assert(needsEndConditional);
                    Br(endConditional);

                    // Backtrack section
                    Label backtrack = DefineLabel();
                    doneLabel = backtrack;
                    MarkLabel(backtrack);

                    // Pop from the stack the branch that was used and jump back to its backtracking location.

                    // resumeAt = base.runstack[--stackpos];
                    EmitStackPop();
                    Stloc(resumeAt);

                    if (postYesDoneLabel != originalDoneLabel)
                    {
                        // if (resumeAt == 0) goto postIfDoneLabel;
                        Ldloc(resumeAt);
                        Ldc(0);
                        BeqFar(postYesDoneLabel);
                    }

                    if (postNoDoneLabel != originalDoneLabel)
                    {
                        // if (resumeAt == 1) goto postNoDoneLabel;
                        Ldloc(resumeAt);
                        Ldc(1);
                        BeqFar(postNoDoneLabel);
                    }

                    // goto originalDoneLabel;
                    BrFar(originalDoneLabel);

                    if (needsEndConditional)
                    {
                        MarkLabel(endConditional);
                    }

                    // if (stackpos + 1 >= base.runstack.Length) Array.Resize(ref base.runstack, base.runstack.Length * 2);
                    // base.runstack[stackpos++] = resumeAt;
                    EmitStackResizeIfNeeded(1);
                    EmitStackPush(() => Ldloc(resumeAt));
                }
            }

            // Emits the code for an if(expression)-then-else conditional.
            void EmitExpressionConditional(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Testgroup, $"Unexpected type: {node.Type}");
                Debug.Assert(node.ChildCount() == 3, $"Expected 3 children, found {node.ChildCount()}");

                bool isAtomic = node.IsAtomicByParent();

                // We're branching in a complicated fashion.  Make sure sliceStaticPos is 0.
                TransferSliceStaticPosToPos();

                // The first child node is the condition expression.  If this matches, then we branch to the "yes" branch.
                // If it doesn't match, then we branch to the optional "no" branch if it exists, or simply skip the "yes"
                // branch, otherwise. The condition is treated as a positive lookahead.
                RegexNode condition = node.Child(0);

                // Get the "yes" branch and the "no" branch.  The "no" branch is optional in syntax and is thus
                // somewhat likely to be Empty.
                RegexNode yesBranch = node.Child(1);
                RegexNode? noBranch = node.Child(2) is { Type: not RegexNode.Empty } childNo ? childNo : null;
                Label originalDoneLabel = doneLabel;

                Label expressionNotMatched = DefineLabel();
                Label endConditional = DefineLabel();

                // As with alternations, we have potentially multiple branches, each of which may contain
                // backtracking constructs, but the expression after the condition needs a single target
                // to backtrack to.  So, we expose a single Backtrack label and track which branch was
                // followed in this resumeAt local.
                LocalBuilder? resumeAt = null;
                if (!isAtomic)
                {
                    resumeAt = DeclareInt32();
                }

                // If the condition expression has captures, we'll need to uncapture them in the case of no match.
                LocalBuilder? startingCapturePos = null;
                if ((condition.Options & RegexNode.HasCapturesFlag) != 0)
                {
                    // int startingCapturePos = base.Crawlpos();
                    startingCapturePos = DeclareInt32();
                    Ldthis();
                    Call(s_crawlposMethod);
                    Stloc(startingCapturePos);
                }

                // Emit the condition expression.  Route any failures to after the yes branch.  This code is almost
                // the same as for a positive lookahead; however, a positive lookahead only needs to reset the position
                // on a successful match, as a failed match fails the whole expression; here, we need to reset the
                // position on completion, regardless of whether the match is successful or not.
                doneLabel = expressionNotMatched;

                // Save off pos.  We'll need to reset this upon successful completion of the lookahead.
                // startingPos = pos;
                LocalBuilder startingPos = DeclareInt32();
                Ldloc(pos);
                Stloc(startingPos);
                int startingSliceStaticPos = sliceStaticPos;

                // Emit the child. The condition expression is a zero-width assertion, which is atomic,
                // so prevent backtracking into it.
                EmitNode(condition);
                doneLabel = originalDoneLabel;

                // After the condition completes successfully, reset the text positions.
                // Do not reset captures, which persist beyond the lookahead.
                // pos = startingPos;
                // slice = inputSpan.Slice(pos, end - pos);
                Ldloc(startingPos);
                Stloc(pos);
                SliceInputSpan();
                sliceStaticPos = startingSliceStaticPos;

                // The expression matched.  Run the "yes" branch. If it successfully matches, jump to the end.
                EmitNode(yesBranch);
                TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                Label postYesDoneLabel = doneLabel;
                if (!isAtomic && postYesDoneLabel != originalDoneLabel)
                {
                    // resumeAt = 0;
                    Ldc(0);
                    Stloc(resumeAt!);
                }

                // goto endConditional;
                BrFar(endConditional);

                // After the condition completes unsuccessfully, reset the text positions
                // _and_ reset captures, which should not persist when the whole expression failed.
                // pos = startingPos;
                MarkLabel(expressionNotMatched);
                Ldloc(startingPos);
                Stloc(pos);
                SliceInputSpan();
                sliceStaticPos = startingSliceStaticPos;
                if (startingCapturePos is not null)
                {
                    EmitUncaptureUntil(startingCapturePos);
                }

                Label postNoDoneLabel = originalDoneLabel;
                if (noBranch is not null)
                {
                    // Output the no branch.
                    doneLabel = originalDoneLabel;
                    EmitNode(noBranch);
                    TransferSliceStaticPosToPos(); // make sure sliceStaticPos is 0 after each branch
                    postNoDoneLabel = doneLabel;
                    if (!isAtomic && postNoDoneLabel != originalDoneLabel)
                    {
                        // resumeAt = 1;
                        Ldc(1);
                        Stloc(resumeAt!);
                    }
                }
                else
                {
                    // There's only a yes branch.  If it's going to cause us to output a backtracking
                    // label but code may not end up taking the yes branch path, we need to emit a resumeAt
                    // that will cause the backtracking to immediately pass through this node.
                    if (!isAtomic && postYesDoneLabel != originalDoneLabel)
                    {
                        // resumeAt = 2;
                        Ldc(2);
                        Stloc(resumeAt!);
                    }
                }

                // If either the yes branch or the no branch contained backtracking, subsequent expressions
                // might try to backtrack to here, so output a backtracking map based on resumeAt.
                if (isAtomic || (postYesDoneLabel == originalDoneLabel && postNoDoneLabel == originalDoneLabel))
                {
                    // EndConditional:
                    doneLabel = originalDoneLabel;
                    MarkLabel(endConditional);
                }
                else
                {
                    Debug.Assert(resumeAt is not null);

                    // Skip the backtracking section.
                    BrFar(endConditional);

                    Label backtrack = DefineLabel();
                    doneLabel = backtrack;
                    MarkLabel(backtrack);

                    // resumeAt = StackPop();
                    EmitStackPop();
                    Stloc(resumeAt);

                    if (postYesDoneLabel != originalDoneLabel)
                    {
                        // if (resumeAt == 0) goto postYesDoneLabel;
                        Ldloc(resumeAt);
                        Ldc(0);
                        BeqFar(postYesDoneLabel);
                    }

                    if (postNoDoneLabel != originalDoneLabel)
                    {
                        // if (resumeAt == 1) goto postNoDoneLabel;
                        Ldloc(resumeAt);
                        Ldc(1);
                        BeqFar(postNoDoneLabel);
                    }

                    // goto postConditionalDoneLabel;
                    BrFar(originalDoneLabel);

                    // EndConditional:
                    MarkLabel(endConditional);

                    // if (stackpos + 1 >= base.runstack.Length) Array.Resize(ref base.runstack, base.runstack.Length * 2);
                    // base.runstack[stackpos++] = resumeAt;
                    EmitStackResizeIfNeeded(1);
                    EmitStackPush(() => Ldloc(resumeAt!));
                }
            }

            // Emits the code for a Capture node.
            void EmitCapture(RegexNode node, RegexNode? subsequent = null)
            {
                Debug.Assert(node.Type is RegexNode.Capture, $"Unexpected type: {node.Type}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                int capnum = RegexParser.MapCaptureNumber(node.M, _code!.Caps);
                int uncapnum = RegexParser.MapCaptureNumber(node.N, _code.Caps);
                bool isAtomic = node.IsAtomicByParent();

                // pos += sliceStaticPos;
                // slice = slice.Slice(sliceStaticPos);
                // startingPos = pos;
                TransferSliceStaticPosToPos();
                LocalBuilder startingPos = DeclareInt32();
                Ldloc(pos);
                Stloc(startingPos);

                RegexNode child = node.Child(0);

                if (uncapnum != -1)
                {
                    // if (!IsMatched(uncapnum)) goto doneLabel;
                    Ldthis();
                    Ldc(uncapnum);
                    Call(s_isMatchedMethod);
                    BrfalseFar(doneLabel);
                }


                // Emit child node.
                Label originalDoneLabel = doneLabel;
                EmitNode(child, subsequent);
                bool childBacktracks = doneLabel != originalDoneLabel;

                // pos += sliceStaticPos;
                // slice = slice.Slice(sliceStaticPos);
                TransferSliceStaticPosToPos();

                if (uncapnum == -1)
                {
                    // Capture(capnum, startingPos, pos);
                    Ldthis();
                    Ldc(capnum);
                    Ldloc(startingPos);
                    Ldloc(pos);
                    Call(s_captureMethod);
                }
                else
                {
                    // TransferCapture(capnum, uncapnum, startingPos, pos);
                    Ldthis();
                    Ldc(capnum);
                    Ldc(uncapnum);
                    Ldloc(startingPos);
                    Ldloc(pos);
                    Call(s_transferCaptureMethod);
                }

                if (!isAtomic && (childBacktracks || node.IsInLoop()))
                {
                    // if (stackpos + 1 >= base.runstack.Length) Array.Resize(ref base.runstack, base.runstack.Length * 2);
                    // base.runstack[stackpos++] = startingPos;
                    EmitStackResizeIfNeeded(1);
                    EmitStackPush(() => Ldloc(startingPos));

                    // Skip past the backtracking section
                    // goto backtrackingEnd;
                    Label backtrackingEnd = DefineLabel();
                    Br(backtrackingEnd);

                    // Emit a backtracking section that restores the capture's state and then jumps to the previous done label
                    Label backtrack = DefineLabel();
                    MarkLabel(backtrack);
                    EmitStackPop();
                    Stloc(startingPos);
                    if (!childBacktracks)
                    {
                        // pos = startingPos
                        Ldloc(startingPos);
                        Stloc(pos);
                        SliceInputSpan();
                    }

                    // goto doneLabel;
                    BrFar(doneLabel);

                    doneLabel = backtrack;
                    MarkLabel(backtrackingEnd);
                }
                else
                {
                    doneLabel = originalDoneLabel;
                }
            }

            // Emits code to unwind the capture stack until the crawl position specified in the provided local.
            void EmitUncaptureUntil(LocalBuilder startingCapturePos)
            {
                Debug.Assert(startingCapturePos != null);

                // while (base.Crawlpos() > startingCapturePos) base.Uncapture();
                Label condition = DefineLabel();
                Label body = DefineLabel();
                Br(condition);

                MarkLabel(body);
                Ldthis();
                Call(s_uncaptureMethod);

                MarkLabel(condition);
                Ldthis();
                Call(s_crawlposMethod);
                Ldloc(startingCapturePos);
                Bgt(body);
            }

            // Emits the code to handle a positive lookahead assertion.
            void EmitPositiveLookaheadAssertion(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Require, $"Unexpected type: {node.Type}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                // Lookarounds are implicitly atomic.  Store the original done label to reset at the end.
                Label originalDoneLabel = doneLabel;

                // Save off pos.  We'll need to reset this upon successful completion of the lookahead.
                // startingPos = pos;
                LocalBuilder startingPos = DeclareInt32();
                Ldloc(pos);
                Stloc(startingPos);
                int startingTextSpanPos = sliceStaticPos;

                // Emit the child.
                EmitNode(node.Child(0));

                // After the child completes successfully, reset the text positions.
                // Do not reset captures, which persist beyond the lookahead.
                // pos = startingPos;
                // slice = inputSpan.Slice(pos, end - pos);
                Ldloc(startingPos);
                Stloc(pos);
                SliceInputSpan();
                sliceStaticPos = startingTextSpanPos;

                doneLabel = originalDoneLabel;
            }

            // Emits the code to handle a negative lookahead assertion.
            void EmitNegativeLookaheadAssertion(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Prevent, $"Unexpected type: {node.Type}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                // Lookarounds are implicitly atomic.  Store the original done label to reset at the end.
                Label originalDoneLabel = doneLabel;

                // Save off pos.  We'll need to reset this upon successful completion of the lookahead.
                // startingPos = pos;
                LocalBuilder startingPos = DeclareInt32();
                Ldloc(pos);
                Stloc(startingPos);
                int startingTextSpanPos = sliceStaticPos;

                Label negativeLookaheadDoneLabel = DefineLabel();
                doneLabel = negativeLookaheadDoneLabel;

                // Emit the child.
                EmitNode(node.Child(0));

                // If the generated code ends up here, it matched the lookahead, which actually
                // means failure for a _negative_ lookahead, so we need to jump to the original done.
                // goto originalDoneLabel;
                BrFar(originalDoneLabel);

                // Failures (success for a negative lookahead) jump here.
                MarkLabel(negativeLookaheadDoneLabel);
                if (doneLabel == negativeLookaheadDoneLabel)
                {
                    doneLabel = originalDoneLabel;
                }

                // After the child completes in failure (success for negative lookahead), reset the text positions.
                // pos = startingPos;
                Ldloc(startingPos);
                Stloc(pos);
                SliceInputSpan();
                sliceStaticPos = startingTextSpanPos;

                doneLabel = originalDoneLabel;
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
                    case RegexNode.Beginning:
                    case RegexNode.Start:
                    case RegexNode.Bol:
                    case RegexNode.Eol:
                    case RegexNode.End:
                    case RegexNode.EndZ:
                        EmitAnchors(node);
                        break;

                    case RegexNode.Boundary:
                    case RegexNode.NonBoundary:
                    case RegexNode.ECMABoundary:
                    case RegexNode.NonECMABoundary:
                        EmitBoundary(node);
                        break;

                    case RegexNode.Multi:
                        EmitMultiChar(node, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.One:
                    case RegexNode.Notone:
                    case RegexNode.Set:
                        EmitSingleChar(node, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Oneloop:
                    case RegexNode.Notoneloop:
                    case RegexNode.Setloop:
                        EmitSingleCharLoop(node, subsequent, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Onelazy:
                    case RegexNode.Notonelazy:
                    case RegexNode.Setlazy:
                        EmitSingleCharLazy(node, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Oneloopatomic:
                    case RegexNode.Notoneloopatomic:
                    case RegexNode.Setloopatomic:
                        EmitSingleCharAtomicLoop(node);
                        break;

                    case RegexNode.Loop:
                        EmitLoop(node);
                        break;

                    case RegexNode.Lazyloop:
                        EmitLazy(node);
                        break;

                    case RegexNode.Alternate:
                        EmitAlternation(node);
                        break;

                    case RegexNode.Concatenate:
                        EmitConcatenation(node, subsequent, emitLengthChecksIfRequired);
                        break;

                    case RegexNode.Atomic:
                        EmitAtomic(node, subsequent);
                        break;

                    case RegexNode.Ref:
                        EmitBackreference(node);
                        break;

                    case RegexNode.Testref:
                        EmitBackreferenceConditional(node);
                        break;

                    case RegexNode.Testgroup:
                        EmitExpressionConditional(node);
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
                        EmitUpdateBumpalong(node);
                        break;

                    default:
                        Debug.Fail($"Unexpected node type: {node.Type}");
                        break;
                }
            }

            // Emits the node for an atomic.
            void EmitAtomic(RegexNode node, RegexNode? subsequent)
            {
                Debug.Assert(node.Type is RegexNode.Atomic, $"Unexpected type: {node.Type}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                // Atomic simply outputs the code for the child, but it ensures that any done label left
                // set by the child is reset to what it was prior to the node's processing.  That way,
                // anything later that tries to jump back won't see labels set inside the atomic.
                Label originalDoneLabel = doneLabel;
                EmitNode(node.Child(0), subsequent);
                doneLabel = originalDoneLabel;
            }

            // Emits the code to handle updating base.runtextpos to pos in response to
            // an UpdateBumpalong node.  This is used when we want to inform the scan loop that
            // it should bump from this location rather than from the original location.
            void EmitUpdateBumpalong(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.UpdateBumpalong, $"Unexpected type: {node.Type}");

                // base.runtextpos = pos;
                TransferSliceStaticPosToPos();
                Ldthis();
                Ldloc(pos);
                Stfld(s_runtextposField);
            }

            // Emits code for a concatenation
            void EmitConcatenation(RegexNode node, RegexNode? subsequent, bool emitLengthChecksIfRequired)
            {
                Debug.Assert(node.Type is RegexNode.Concatenate, $"Unexpected type: {node.Type}");
                Debug.Assert(node.ChildCount() >= 2, $"Expected at least 2 children, found {node.ChildCount()}");

                // Emit the code for each child one after the other.
                int childCount = node.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    // If we can find a subsequence of fixed-length children, we can emit a length check once for that sequence
                    // and then skip the individual length checks for each.
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
                Debug.Assert(node.IsOneFamily || node.IsNotoneFamily || node.IsSetFamily, $"Unexpected type: {node.Type}");

                // This only emits a single check, but it's called from the looping constructs in a loop
                // to generate the code for a single check, so we check for each "family" (one, notone, set)
                // rather than only for the specific single character nodes.

                // if ((uint)(sliceStaticPos + offset) >= slice.Length || slice[sliceStaticPos + offset] != ch) goto Done;
                if (emitLengthCheck)
                {
                    EmitSpanLengthCheck(1, offset);
                }
                Ldloca(slice);
                EmitSum(sliceStaticPos, offset);
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

                sliceStaticPos++;
            }

            // Emits the code to handle a boundary check on a character.
            void EmitBoundary(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Boundary or RegexNode.NonBoundary or RegexNode.ECMABoundary or RegexNode.NonECMABoundary, $"Unexpected type: {node.Type}");

                // if (!IsBoundary(pos + sliceStaticPos, base.runtextbeg, end)) goto doneLabel;
                Ldthis();
                Ldloc(pos);
                if (sliceStaticPos > 0)
                {
                    Ldc(sliceStaticPos);
                    Add();
                }
                Ldthisfld(s_runtextbegField);
                Ldloc(end);
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
                Debug.Assert(node.Type is RegexNode.Beginning or RegexNode.Start or RegexNode.Bol or RegexNode.End or RegexNode.EndZ or RegexNode.Eol, $"Unexpected type: {node.Type}");

                Debug.Assert(sliceStaticPos >= 0);
                switch (node.Type)
                {
                    case RegexNode.Beginning:
                    case RegexNode.Start:
                        if (sliceStaticPos > 0)
                        {
                            // If we statically know we've already matched part of the regex, there's no way we're at the
                            // beginning or start, as we've already progressed past it.
                            BrFar(doneLabel);
                        }
                        else
                        {
                            // if (pos > base.runtextbeg/start) goto doneLabel;
                            Ldloc(pos);
                            Ldthisfld(node.Type == RegexNode.Beginning ? s_runtextbegField : s_runtextstartField);
                            BneFar(doneLabel);
                        }
                        break;

                    case RegexNode.Bol:
                        if (sliceStaticPos > 0)
                        {
                            // if (slice[sliceStaticPos - 1] != '\n') goto doneLabel;
                            Ldloca(slice);
                            Ldc(sliceStaticPos - 1);
                            Call(s_spanGetItemMethod);
                            LdindU2();
                            Ldc('\n');
                            BneFar(doneLabel);
                        }
                        else
                        {
                            // We can't use our slice in this case, because we'd need to access slice[-1], so we access the runtext field directly:
                            // if (pos > base.runtextbeg && base.runtext[pos - 1] != '\n') goto doneLabel;
                            Label success = DefineLabel();
                            Ldloc(pos);
                            Ldthisfld(s_runtextbegField);
                            Ble(success);
                            Ldloca(inputSpan);
                            Ldloc(pos);
                            Ldc(1);
                            Sub();
                            Call(s_spanGetItemMethod);
                            LdindU2();
                            Ldc('\n');
                            BneFar(doneLabel);
                            MarkLabel(success);
                        }
                        break;

                    case RegexNode.End:
                        // if (sliceStaticPos < slice.Length) goto doneLabel;
                        Ldc(sliceStaticPos);
                        Ldloca(slice);
                        Call(s_spanGetLengthMethod);
                        BltUnFar(doneLabel);
                        break;

                    case RegexNode.EndZ:
                        // if (sliceStaticPos < slice.Length - 1) goto doneLabel;
                        Ldc(sliceStaticPos);
                        Ldloca(slice);
                        Call(s_spanGetLengthMethod);
                        Ldc(1);
                        Sub();
                        BltFar(doneLabel);
                        goto case RegexNode.Eol;

                    case RegexNode.Eol:
                        // if (sliceStaticPos < slice.Length && slice[sliceStaticPos] != '\n') goto doneLabel;
                        {
                            Label success = DefineLabel();
                            Ldc(sliceStaticPos);
                            Ldloca(slice);
                            Call(s_spanGetLengthMethod);
                            BgeUn(success);
                            Ldloca(slice);
                            Ldc(sliceStaticPos);
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
                Debug.Assert(node.Type is RegexNode.Multi, $"Unexpected type: {node.Type}");

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
                    // if (!slice.Slice(sliceStaticPos).StartsWith("...") goto doneLabel;
                    Ldloca(slice);
                    Ldc(sliceStaticPos);
                    Call(s_spanSliceIntMethod);
                    Ldstr(node.Str);
                    Call(s_stringAsSpanMethod);
                    Call(s_spanStartsWith);
                    BrfalseFar(doneLabel);
                    sliceStaticPos += node.Str.Length;
                    return;
                }

                // Emit the length check for the whole string.  If the generated code gets past this point,
                // we know the span is at least sliceStaticPos + s.Length long.
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
                            // if (Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref MemoryMarshal.GetReference(slice), sliceStaticPos)) != value) goto doneLabel;
                            EmitTextSpanOffset();
                            Unaligned(1);
                            LdindI8();
                            LdcI8(MemoryMarshal.Read<long>(MemoryMarshal.AsBytes(s)));
                            BneFar(doneLabel);
                            sliceStaticPos += CharsPerInt64;
                            s = s.Slice(CharsPerInt64);
                        }
                    }

                    // Of what remains, process 2 characters at a time until the string isn't at least 2 characters long.
                    const int CharsPerInt32 = 2;
                    while (s.Length >= CharsPerInt32)
                    {
                        // if (Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref MemoryMarshal.GetReference(slice), sliceStaticPos)) != value) goto doneLabel;
                        EmitTextSpanOffset();
                        Unaligned(1);
                        LdindI4();
                        Ldc(MemoryMarshal.Read<int>(MemoryMarshal.AsBytes(s)));
                        BneFar(doneLabel);
                        sliceStaticPos += CharsPerInt32;
                        s = s.Slice(CharsPerInt32);
                    }
                }

                // Finally, process all of the remaining characters one by one.
                for (int i = 0; i < s.Length; i++)
                {
                    // if (s[i] != slice[sliceStaticPos++]) goto doneLabel;
                    EmitTextSpanOffset();
                    sliceStaticPos++;
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
                Debug.Assert(node.Type is RegexNode.Oneloop or RegexNode.Notoneloop or RegexNode.Setloop, $"Unexpected type: {node.Type}");

                // If this is actually a repeater, emit that instead; no backtracking necessary.
                if (node.M == node.N)
                {
                    EmitSingleCharFixedRepeater(node, emitLengthChecksIfRequired);
                    return;
                }

                // Emit backtracking around an atomic single char loop.  We can then implement the backtracking
                // as an afterthought, since we know exactly how many characters are accepted by each iteration
                // of the wrapped loop (1) and that there's nothing captured by the loop.

                Debug.Assert(node.M < node.N);
                Label backtrackingLabel = DefineLabel();
                Label endLoop = DefineLabel();
                LocalBuilder startingPos = DeclareInt32();
                LocalBuilder endingPos = DeclareInt32();
                LocalBuilder? capturepos = expressionHasCaptures ? DeclareInt32() : null;

                // We're about to enter a loop, so ensure our text position is 0.
                TransferSliceStaticPosToPos();

                // Grab the current position, then emit the loop as atomic, and then
                // grab the current position again.  Even though we emit the loop without
                // knowledge of backtracking, we can layer it on top by just walking back
                // through the individual characters (a benefit of the loop matching exactly
                // one character per iteration, no possible captures within the loop, etc.)

                // int startingPos = pos;
                Ldloc(pos);
                Stloc(startingPos);

                EmitSingleCharAtomicLoop(node);

                // pos += sliceStaticPos;
                // int endingPos = pos;
                TransferSliceStaticPosToPos();
                Ldloc(pos);
                Stloc(endingPos);

                // int capturepos = base.Crawlpos();
                if (capturepos is not null)
                {
                    Ldthis();
                    Call(s_crawlposMethod);
                    Stloc(capturepos);
                }

                // startingPos += node.M;
                if (node.M > 0)
                {
                    Ldloc(startingPos);
                    Ldc(node.M);
                    Add();
                    Stloc(startingPos);
                }

                // goto endLoop;
                BrFar(endLoop);

                // Backtracking section. Subsequent failures will jump to here, at which
                // point we decrement the matched count as long as it's above the minimum
                // required, and try again by flowing to everything that comes after this.

                MarkLabel(backtrackingLabel);
                if (capturepos is not null)
                {
                    // capturepos = base.runstack[--stackpos];
                    // while (base.Crawlpos() > capturepos) base.Uncapture();
                    EmitStackPop();
                    Stloc(capturepos);
                    EmitUncaptureUntil(capturepos);
                }

                // endingPos = base.runstack[--stackpos];
                // startingPos = base.runstack[--stackpos];
                EmitStackPop();
                Stloc(endingPos);
                EmitStackPop();
                Stloc(startingPos);

                // if (startingPos >= endingPos) goto originalDoneLabel;
                Label originalDoneLabel = doneLabel;
                Ldloc(startingPos);
                Ldloc(endingPos);
                BgeFar(originalDoneLabel);
                doneLabel = backtrackingLabel; // leave set to the backtracking label for all subsequent nodes

                if (subsequent?.FindStartingCharacter() is char subsequentCharacter)
                {
                    // endingPos = inputSpan.Slice(startingPos, endingPos - startingPos).LastIndexOf(subsequentCharacter);
                    // if (endingPos < 0)
                    // {
                    //     goto originalDoneLabel;
                    // }
                    Ldloca(inputSpan);
                    Ldloc(startingPos);
                    Ldloc(endingPos);
                    Ldloc(startingPos);
                    Sub();
                    Call(s_spanSliceIntIntMethod);
                    Ldc(subsequentCharacter);
                    Call(s_spanLastIndexOfChar);
                    Stloc(endingPos);
                    Ldloc(endingPos);
                    Ldc(0);
                    BltFar(originalDoneLabel);

                    // endingPos += startingPos;
                    Ldloc(endingPos);
                    Ldloc(startingPos);
                    Add();
                    Stloc(endingPos);
                }
                else
                {
                    // endingPos--;
                    Ldloc(endingPos);
                    Ldc(1);
                    Sub();
                    Stloc(endingPos);
                }

                // pos = endingPos;
                Ldloc(endingPos);
                Stloc(pos);

                // slice = inputSpan.Slice(pos, end - pos);
                SliceInputSpan();

                MarkLabel(endLoop);
                EmitStackResizeIfNeeded(expressionHasCaptures ? 3 : 2);
                EmitStackPush(() => Ldloc(startingPos));
                EmitStackPush(() => Ldloc(endingPos));
                if (capturepos is not null)
                {
                    EmitStackPush(() => Ldloc(capturepos!));
                }
            }

            void EmitSingleCharLazy(RegexNode node, bool emitLengthChecksIfRequired = true)
            {
                Debug.Assert(node.Type is RegexNode.Onelazy or RegexNode.Notonelazy or RegexNode.Setlazy, $"Unexpected type: {node.Type}");

                // Emit the min iterations as a repeater.  Any failures here don't necessitate backtracking,
                // as the lazy itself failed to match, and there's no backtracking possible by the individual
                // characters/iterations themselves.
                if (node.M > 0)
                {
                    EmitSingleCharFixedRepeater(node, emitLengthChecksIfRequired);
                }

                // If the whole thing was actually that repeater, we're done. Similarly, if this is actually an atomic
                // lazy loop, nothing will ever backtrack into this node, so we never need to iterate more than the minimum.
                if (node.M == node.N || node.IsAtomicByParent())
                {
                    return;
                }

                Debug.Assert(node.M < node.N);

                // We now need to match one character at a time, each time allowing the remainder of the expression
                // to try to match, and only matching another character if the subsequent expression fails to match.

                // We're about to enter a loop, so ensure our text position is 0.
                TransferSliceStaticPosToPos();

                // If the loop isn't unbounded, track the number of iterations and the max number to allow.
                LocalBuilder? iterationCount = null;
                int? maxIterations = null;
                if (node.N != int.MaxValue)
                {
                    maxIterations = node.N - node.M;

                    // int iterationCount = 0;
                    iterationCount = DeclareInt32();
                    Ldc(0);
                    Stloc(iterationCount);
                }

                // Track the current crawl position.  Upon backtracking, we'll unwind any captures beyond this point.
                LocalBuilder? capturepos = expressionHasCaptures ? DeclareInt32() : null;

                // Track the current pos.  Each time we backtrack, we'll reset to the stored position, which
                // is also incremented each time we match another character in the loop.
                // int startingPos = pos;
                LocalBuilder startingPos = DeclareInt32();
                Ldloc(pos);
                Stloc(startingPos);

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
                if (capturepos is not null)
                {
                    // while (base.Crawlpos() > capturepos) base.Uncapture();
                    EmitUncaptureUntil(capturepos);
                }

                // If there's a max number of iterations, see if we've exceeded the maximum number of characters
                // to match.  If we haven't, increment the iteration count.
                if (maxIterations is not null)
                {
                    // if (iterationCount >= maxIterations) goto doneLabel;
                    Ldloc(iterationCount!);
                    Ldc(maxIterations.Value);
                    BgeFar(doneLabel);

                    // iterationCount++;
                    Ldloc(iterationCount!);
                    Ldc(1);
                    Add();
                    Stloc(iterationCount!);
                }

                // Now match the next item in the lazy loop.  We need to reset the pos to the position
                // just after the last character in this loop was matched, and we need to store the resulting position
                // for the next time we backtrack.

                // pos = startingPos;
                Ldloc(startingPos);
                Stloc(pos);
                SliceInputSpan();

                // Match single character
                EmitSingleChar(node);
                TransferSliceStaticPosToPos();

                // startingPos = pos;
                Ldloc(pos);
                Stloc(startingPos);

                // Update the done label for everything that comes after this node.  This is done after we emit the single char
                // matching, as that failing indicates the loop itself has failed to match.
                Label originalDoneLabel = doneLabel;
                doneLabel = backtrackingLabel; // leave set to the backtracking label for all subsequent nodes

                MarkLabel(endLoopLabel);
                if (capturepos is not null)
                {
                    // capturepos = base.CrawlPos();
                    Ldthis();
                    Call(s_crawlposMethod);
                    Stloc(capturepos);
                }

                if (node.IsInLoop())
                {
                    // Store the capture's state
                    // base.runstack[stackpos++] = startingPos;
                    // base.runstack[stackpos++] = capturepos;
                    // base.runstack[stackpos++] = iterationCount;
                    EmitStackResizeIfNeeded(3);
                    EmitStackPush(() => Ldloc(startingPos));
                    if (capturepos is not null)
                    {
                        EmitStackPush(() => Ldloc(capturepos));
                    }
                    if (iterationCount is not null)
                    {
                        EmitStackPush(() => Ldloc(iterationCount));
                    }

                    // Skip past the backtracking section
                    Label backtrackingEnd = DefineLabel();
                    BrFar(backtrackingEnd);

                    // Emit a backtracking section that restores the capture's state and then jumps to the previous done label
                    Label backtrack = DefineLabel();
                    MarkLabel(backtrack);

                    // iterationCount = base.runstack[--stackpos];
                    // capturepos = base.runstack[--stackpos];
                    // startingPos = base.runstack[--stackpos];
                    if (iterationCount is not null)
                    {
                        EmitStackPop();
                        Stloc(iterationCount);
                    }
                    if (capturepos is not null)
                    {
                        EmitStackPop();
                        Stloc(capturepos);
                    }
                    EmitStackPop();
                    Stloc(startingPos);

                    // goto doneLabel;
                    BrFar(doneLabel);

                    doneLabel = backtrack;
                    MarkLabel(backtrackingEnd);
                }
            }

            void EmitLazy(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Lazyloop, $"Unexpected type: {node.Type}");
                Debug.Assert(node.M < int.MaxValue, $"Unexpected M={node.M}");
                Debug.Assert(node.N >= node.M, $"Unexpected M={node.M}, N={node.N}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                int minIterations = node.M;
                int maxIterations = node.N;
                Label originalDoneLabel = doneLabel;
                bool isAtomic = node.IsAtomicByParent();

                // If this is actually an atomic lazy loop, we need to output just the minimum number of iterations,
                // as nothing will backtrack into the lazy loop to get it progress further.
                if (isAtomic)
                {
                    switch (minIterations)
                    {
                        case 0:
                            // Atomic lazy with a min count of 0: nop.
                            return;

                        case 1:
                            // Atomic lazy with a min count of 1: just output the child, no looping required.
                            EmitNode(node.Child(0));
                            return;
                    }
                }

                // We might loop any number of times.  In order to ensure this loop and subsequent code sees sliceStaticPos
                // the same regardless, we always need it to contain the same value, and the easiest such value is 0.
                // So, we transfer sliceStaticPos to pos, and ensure that any path out of here has sliceStaticPos as 0.
                TransferSliceStaticPosToPos();

                LocalBuilder startingPos = DeclareInt32();
                LocalBuilder iterationCount = DeclareInt32();
                LocalBuilder sawEmpty = DeclareInt32();
                Label body = DefineLabel();
                Label endLoop = DefineLabel();

                // iterationCount = 0;
                // startingPos = pos;
                // sawEmpty = 0; // false
                Ldc(0);
                Stloc(iterationCount);
                Ldloc(pos);
                Stloc(startingPos);
                Ldc(0);
                Stloc(sawEmpty);

                // If the min count is 0, start out by jumping right to what's after the loop.  Backtracking
                // will then bring us back in to do further iterations.
                if (minIterations == 0)
                {
                    // goto endLoop;
                    BrFar(endLoop);
                }

                // Iteration body
                MarkLabel(body);
                EmitTimeoutCheck();

                // We need to store the starting pos and crawl position so that it may
                // be backtracked through later.  This needs to be the starting position from
                // the iteration we're leaving, so it's pushed before updating it to pos.
                // base.runstack[stackpos++] = base.Crawlpos();
                // base.runstack[stackpos++] = startingPos;
                // base.runstack[stackpos++] = pos;
                // base.runstack[stackpos++] = sawEmpty;
                EmitStackResizeIfNeeded(3);
                if (expressionHasCaptures)
                {
                    EmitStackPush(() =>
                    {
                        Ldthis();
                        Call(s_crawlposMethod);
                    });
                }
                EmitStackPush(() => Ldloc(startingPos));
                EmitStackPush(() => Ldloc(pos));
                EmitStackPush(() => Ldloc(sawEmpty));

                // Save off some state.  We need to store the current pos so we can compare it against
                // pos after the iteration, in order to determine whether the iteration was empty. Empty
                // iterations are allowed as part of min matches, but once we've met the min quote, empty matches
                // are considered match failures.
                // startingPos = pos;
                Ldloc(pos);
                Stloc(startingPos);

                // Proactively increase the number of iterations.  We do this prior to the match rather than once
                // we know it's successful, because we need to decrement it as part of a failed match when
                // backtracking; it's thus simpler to just always decrement it as part of a failed match, even
                // when initially greedily matching the loop, which then requires we increment it before trying.
                // iterationCount++;
                Ldloc(iterationCount);
                Ldc(1);
                Add();
                Stloc(iterationCount);

                // Last but not least, we need to set the doneLabel that a failed match of the body will jump to.
                // Such an iteration match failure may or may not fail the whole operation, depending on whether
                // we've already matched the minimum required iterations, so we need to jump to a location that
                // will make that determination.
                Label iterationFailedLabel = DefineLabel();
                doneLabel = iterationFailedLabel;

                // Finally, emit the child.
                Debug.Assert(sliceStaticPos == 0);
                EmitNode(node.Child(0));
                TransferSliceStaticPosToPos(); // ensure sliceStaticPos remains 0
                if (doneLabel == iterationFailedLabel)
                {
                    doneLabel = originalDoneLabel;
                }

                // Loop condition.  Continue iterating if we've not yet reached the minimum.
                if (minIterations > 0)
                {
                    // if (iterationCount < minIterations) goto body;
                    Ldloc(iterationCount);
                    Ldc(minIterations);
                    BltFar(body);
                }

                // If the last iteration was empty, we need to prevent further iteration from this point
                // unless we backtrack out of this iteration.  We can do that easily just by pretending
                // we reached the max iteration count.
                // if (pos == startingPos) sawEmpty = 1; // true
                Label skipSawEmptySet = DefineLabel();
                Ldloc(pos);
                Ldloc(startingPos);
                Bne(skipSawEmptySet);
                Ldc(1);
                Stloc(sawEmpty);
                MarkLabel(skipSawEmptySet);

                // We matched the next iteration.  Jump to the subsequent code.
                // goto endLoop;
                BrFar(endLoop);

                // Now handle what happens when an iteration fails.  We need to reset state to what it was before just that iteration
                // started.  That includes resetting pos and clearing out any captures from that iteration.
                MarkLabel(iterationFailedLabel);

                // iterationCount--;
                Ldloc(iterationCount);
                Ldc(1);
                Sub();
                Stloc(iterationCount);

                // if (iterationCount < 0) goto originalDoneLabel;
                Ldloc(iterationCount);
                Ldc(0);
                BltFar(originalDoneLabel);

                // sawEmpty = base.runstack[--stackpos];
                // pos = base.runstack[--stackpos];
                // startingPos = base.runstack[--stackpos];
                // capturepos = base.runstack[--stackpos];
                // while (base.Crawlpos() > capturepos) base.Uncapture();
                EmitStackPop();
                Stloc(sawEmpty);
                EmitStackPop();
                Stloc(pos);
                EmitStackPop();
                Stloc(startingPos);
                if (expressionHasCaptures)
                {
                    using RentedLocalBuilder poppedCrawlPos = RentInt32Local();
                    EmitStackPop();
                    Stloc(poppedCrawlPos);
                    EmitUncaptureUntil(poppedCrawlPos);
                }
                SliceInputSpan();

                if (doneLabel == originalDoneLabel)
                {
                    // goto originalDoneLabel;
                    BrFar(originalDoneLabel);
                }
                else
                {
                    // if (iterationCount == 0) goto originalDoneLabel;
                    // goto doneLabel;
                    Ldloc(iterationCount);
                    Ldc(0);
                    BeqFar(originalDoneLabel);
                    BrFar(doneLabel);
                }

                MarkLabel(endLoop);

                if (!isAtomic)
                {
                    // Store the capture's state and skip the backtracking section
                    EmitStackResizeIfNeeded(3);
                    EmitStackPush(() => Ldloc(startingPos));
                    EmitStackPush(() => Ldloc(iterationCount));
                    EmitStackPush(() => Ldloc(sawEmpty));
                    Label skipBacktrack = DefineLabel();
                    BrFar(skipBacktrack);

                    // Emit a backtracking section that restores the capture's state and then jumps to the previous done label
                    Label backtrack = DefineLabel();
                    MarkLabel(backtrack);

                    // sawEmpty = base.runstack[--stackpos];
                    // iterationCount = base.runstack[--stackpos];
                    // startingPos = base.runstack[--stackpos];
                    EmitStackPop();
                    Stloc(sawEmpty);
                    EmitStackPop();
                    Stloc(iterationCount);
                    EmitStackPop();
                    Stloc(startingPos);

                    if (maxIterations == int.MaxValue)
                    {
                        // if (sawEmpty != 0) goto doneLabel;
                        Ldloc(sawEmpty);
                        Ldc(0);
                        BneFar(doneLabel);
                    }
                    else
                    {
                        // if (iterationCount >= maxIterations || sawEmpty != 0) goto doneLabel;
                        Ldloc(iterationCount);
                        Ldc(maxIterations);
                        BgeFar(doneLabel);
                        Ldloc(sawEmpty);
                        Ldc(0);
                        BneFar(doneLabel);
                    }

                    // goto body;
                    BrFar(body);

                    doneLabel = backtrack;
                    MarkLabel(skipBacktrack);
                }
            }

            // Emits the code to handle a loop (repeater) with a fixed number of iterations.
            // RegexNode.M is used for the number of iterations; RegexNode.N is ignored.
            void EmitSingleCharFixedRepeater(RegexNode node, bool emitLengthChecksIfRequired = true)
            {
                Debug.Assert(node.IsOneFamily || node.IsNotoneFamily || node.IsSetFamily, $"Unexpected type: {node.Type}");

                int iterations = node.M;
                if (iterations == 0)
                {
                    // No iterations, nothing to do.
                    return;
                }

                // if ((uint)(sliceStaticPos + iterations - 1) >= (uint)slice.Length) goto doneLabel;
                if (emitLengthChecksIfRequired)
                {
                    EmitSpanLengthCheck(iterations);
                }

                // Arbitrary limit for unrolling vs creating a loop.  We want to balance size in the generated
                // code with other costs, like the (small) overhead of slicing to create the temp span to iterate.
                const int MaxUnrollSize = 16;

                if (iterations <= MaxUnrollSize)
                {
                    // if (slice[sliceStaticPos] != c1 ||
                    //     slice[sliceStaticPos + 1] != c2 ||
                    //     ...)
                    //       goto doneLabel;
                    for (int i = 0; i < iterations; i++)
                    {
                        EmitSingleChar(node, emitLengthCheck: false);
                    }
                }
                else
                {
                    // ReadOnlySpan<char> tmp = slice.Slice(sliceStaticPos, iterations);
                    // for (int i = 0; i < tmp.Length; i++)
                    // {
                    //     TimeoutCheck();
                    //     if (tmp[i] != ch) goto Done;
                    // }
                    // sliceStaticPos += iterations;

                    Label conditionLabel = DefineLabel();
                    Label bodyLabel = DefineLabel();

                    using RentedLocalBuilder spanLocal = RentReadOnlySpanCharLocal();
                    Ldloca(slice);
                    Ldc(sliceStaticPos);
                    Ldc(iterations);
                    Call(s_spanSliceIntIntMethod);
                    Stloc(spanLocal);

                    using RentedLocalBuilder iterationLocal = RentInt32Local();
                    Ldc(0);
                    Stloc(iterationLocal);
                    BrFar(conditionLabel);

                    MarkLabel(bodyLabel);
                    EmitTimeoutCheck();

                    LocalBuilder tmpTextSpanLocal = slice; // we want EmitSingleChar to refer to this temporary
                    int tmpTextSpanPos = sliceStaticPos;
                    slice = spanLocal;
                    sliceStaticPos = 0;
                    EmitSingleChar(node, emitLengthCheck: false, offset: iterationLocal);
                    slice = tmpTextSpanLocal;
                    sliceStaticPos = tmpTextSpanPos;

                    Ldloc(iterationLocal);
                    Ldc(1);
                    Add();
                    Stloc(iterationLocal);

                    MarkLabel(conditionLabel);
                    Ldloc(iterationLocal);
                    Ldloca(spanLocal);
                    Call(s_spanGetLengthMethod);
                    BltFar(bodyLabel);

                    sliceStaticPos += iterations;
                }
            }

            // Emits the code to handle a non-backtracking, variable-length loop around a single character comparison.
            void EmitSingleCharAtomicLoop(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Oneloop or RegexNode.Oneloopatomic or RegexNode.Notoneloop or RegexNode.Notoneloopatomic or RegexNode.Setloop or RegexNode.Setloopatomic, $"Unexpected type: {node.Type}");

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

                    // int i = slice.Slice(sliceStaticPos).IndexOf(char);
                    if (sliceStaticPos > 0)
                    {
                        Ldloca(slice);
                        Ldc(sliceStaticPos);
                        Call(s_spanSliceIntMethod);
                    }
                    else
                    {
                        Ldloc(slice);
                    }
                    Ldc(node.Ch);
                    Call(s_spanIndexOfChar);
                    Stloc(iterationLocal);

                    // if (i >= 0) goto atomicLoopDoneLabel;
                    Ldloc(iterationLocal);
                    Ldc(0);
                    BgeFar(atomicLoopDoneLabel);

                    // i = slice.Length - sliceStaticPos;
                    Ldloca(slice);
                    Call(s_spanGetLengthMethod);
                    if (sliceStaticPos > 0)
                    {
                        Ldc(sliceStaticPos);
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

                    // int i = slice.Slice(sliceStaticPos).IndexOfAny(ch1, ch2, ...);
                    if (sliceStaticPos > 0)
                    {
                        Ldloca(slice);
                        Ldc(sliceStaticPos);
                        Call(s_spanSliceIntMethod);
                    }
                    else
                    {
                        Ldloc(slice);
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

                    // i = slice.Length - sliceStaticPos;
                    Ldloca(slice);
                    Call(s_spanGetLengthMethod);
                    if (sliceStaticPos > 0)
                    {
                        Ldc(sliceStaticPos);
                        Sub();
                    }
                    Stloc(iterationLocal);
                }
                else if (node.IsSetFamily && maxIterations == int.MaxValue && node.Str == RegexCharClass.AnyClass)
                {
                    // .* was used with RegexOptions.Singleline, which means it'll consume everything.  Just jump to the end.
                    // The unbounded constraint is the same as in the Notone case above, done purely for simplicity.

                    // int i = end - pos;
                    TransferSliceStaticPosToPos();
                    Ldloc(end);
                    Ldloc(pos);
                    Sub();
                    Stloc(iterationLocal);
                }
                else
                {
                    // For everything else, do a normal loop.

                    // Transfer sliceStaticPos to pos to help with bounds check elimination on the loop.
                    TransferSliceStaticPosToPos();

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

                    // if ((uint)i >= (uint)slice.Length) goto atomicLoopDoneLabel;
                    Ldloc(iterationLocal);
                    Ldloca(slice);
                    Call(s_spanGetLengthMethod);
                    BgeUnFar(atomicLoopDoneLabel);

                    // if (slice[i] != ch) goto atomicLoopDoneLabel;
                    Ldloca(slice);
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
                // and pos by the number of iterations completed.

                // slice = slice.Slice(i);
                Ldloca(slice);
                Ldloc(iterationLocal);
                Call(s_spanSliceIntMethod);
                Stloc(slice);

                // pos += i;
                Ldloc(pos);
                Ldloc(iterationLocal);
                Add();
                Stloc(pos);
            }

            // Emits the code to handle a non-backtracking optional zero-or-one loop.
            void EmitAtomicSingleCharZeroOrOne(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Oneloop or RegexNode.Oneloopatomic or RegexNode.Notoneloop or RegexNode.Notoneloopatomic or RegexNode.Setloop or RegexNode.Setloopatomic, $"Unexpected type: {node.Type}");
                Debug.Assert(node.M == 0 && node.N == 1);

                Label skipUpdatesLabel = DefineLabel();

                // if ((uint)sliceStaticPos >= (uint)slice.Length) goto skipUpdatesLabel;
                Ldc(sliceStaticPos);
                Ldloca(slice);
                Call(s_spanGetLengthMethod);
                BgeUnFar(skipUpdatesLabel);

                // if (slice[sliceStaticPos] != ch) goto skipUpdatesLabel;
                Ldloca(slice);
                Ldc(sliceStaticPos);
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

                // slice = slice.Slice(1);
                Ldloca(slice);
                Ldc(1);
                Call(s_spanSliceIntMethod);
                Stloc(slice);

                // pos++;
                Ldloc(pos);
                Ldc(1);
                Add();
                Stloc(pos);

                MarkLabel(skipUpdatesLabel);
            }

            void EmitLoop(RegexNode node)
            {
                Debug.Assert(node.Type is RegexNode.Loop or RegexNode.Lazyloop, $"Unexpected type: {node.Type}");
                Debug.Assert(node.M < int.MaxValue, $"Unexpected M={node.M}");
                Debug.Assert(node.N >= node.M, $"Unexpected M={node.M}, N={node.N}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                int minIterations = node.M;
                int maxIterations = node.N;
                bool isAtomic = node.IsAtomicByParent();

                // We might loop any number of times.  In order to ensure this loop and subsequent code sees sliceStaticPos
                // the same regardless, we always need it to contain the same value, and the easiest such value is 0.
                // So, we transfer sliceStaticPos to pos, and ensure that any path out of here has sliceStaticPos as 0.
                TransferSliceStaticPosToPos();

                Label originalDoneLabel = doneLabel;

                LocalBuilder startingPos = DeclareInt32();
                LocalBuilder iterationCount = DeclareInt32();
                Label body = DefineLabel();
                Label endLoop = DefineLabel();

                // iterationCount = 0;
                // startingPos = 0;
                Ldc(0);
                Stloc(iterationCount);
                Ldc(0);
                Stloc(startingPos);

                // Iteration body
                MarkLabel(body);
                EmitTimeoutCheck();

                // We need to store the starting pos and crawl position so that it may
                // be backtracked through later.  This needs to be the starting position from
                // the iteration we're leaving, so it's pushed before updating it to pos.
                EmitStackResizeIfNeeded(3);
                if (expressionHasCaptures)
                {
                    // base.runstack[stackpos++] = base.Crawlpos();
                    EmitStackPush(() => { Ldthis(); Call(s_crawlposMethod); });
                }
                EmitStackPush(() => Ldloc(startingPos));
                EmitStackPush(() => Ldloc(pos));

                // Save off some state.  We need to store the current pos so we can compare it against
                // pos after the iteration, in order to determine whether the iteration was empty. Empty
                // iterations are allowed as part of min matches, but once we've met the min quote, empty matches
                // are considered match failures.
                // startingPos = pos;
                Ldloc(pos);
                Stloc(startingPos);

                // Proactively increase the number of iterations.  We do this prior to the match rather than once
                // we know it's successful, because we need to decrement it as part of a failed match when
                // backtracking; it's thus simpler to just always decrement it as part of a failed match, even
                // when initially greedily matching the loop, which then requires we increment it before trying.
                // iterationCount++;
                Ldloc(iterationCount);
                Ldc(1);
                Add();
                Stloc(iterationCount);

                // Last but not least, we need to set the doneLabel that a failed match of the body will jump to.
                // Such an iteration match failure may or may not fail the whole operation, depending on whether
                // we've already matched the minimum required iterations, so we need to jump to a location that
                // will make that determination.
                Label iterationFailedLabel = DefineLabel();
                doneLabel = iterationFailedLabel;

                // Finally, emit the child.
                Debug.Assert(sliceStaticPos == 0);
                EmitNode(node.Child(0));
                TransferSliceStaticPosToPos(); // ensure sliceStaticPos remains 0
                bool childBacktracks = doneLabel != iterationFailedLabel;

                // Loop condition.  Continue iterating greedily if we've not yet reached the maximum.  We also need to stop
                // iterating if the iteration matched empty and we already hit the minimum number of iterations. Otherwise,
                // we've matched as many iterations as we can with this configuration.  Jump to what comes after the loop.
                switch ((minIterations > 0, maxIterations == int.MaxValue))
                {
                    case (true, true):
                        // if (pos != startingPos || iterationCount < minIterations) goto body;
                        // goto endLoop;
                        Ldloc(pos);
                        Ldloc(startingPos);
                        BneFar(body);
                        Ldloc(iterationCount);
                        Ldc(minIterations);
                        BltFar(body);
                        BrFar(endLoop);
                        break;

                    case (true, false):
                        // if ((pos != startingPos || iterationCount < minIterations) && iterationCount < maxIterations) goto body;
                        // goto endLoop;
                        Ldloc(iterationCount);
                        Ldc(maxIterations);
                        BgeFar(endLoop);
                        Ldloc(pos);
                        Ldloc(startingPos);
                        BneFar(body);
                        Ldloc(iterationCount);
                        Ldc(minIterations);
                        BltFar(body);
                        BrFar(endLoop);
                        break;

                    case (false, true):
                        // if (pos != startingPos) goto body;
                        // goto endLoop;
                        Ldloc(pos);
                        Ldloc(startingPos);
                        BneFar(body);
                        BrFar(endLoop);
                        break;

                    case (false, false):
                        // if (pos == startingPos || iterationCount >= maxIterations) goto endLoop;
                        // goto body;
                        Ldloc(pos);
                        Ldloc(startingPos);
                        BeqFar(endLoop);
                        Ldloc(iterationCount);
                        Ldc(maxIterations);
                        BgeFar(endLoop);
                        BrFar(body);
                        break;
                }

                // Now handle what happens when an iteration fails, which could be an initial failure or it
                // could be while backtracking.  We need to reset state to what it was before just that iteration
                // started.  That includes resetting pos and clearing out any captures from that iteration.
                MarkLabel(iterationFailedLabel);

                // iterationCount--;
                Ldloc(iterationCount);
                Ldc(1);
                Sub();
                Stloc(iterationCount);

                // if (iterationCount < 0) goto originalDoneLabel;
                Ldloc(iterationCount);
                Ldc(0);
                BltFar(originalDoneLabel);

                // pos = base.runstack[--stackpos];
                // startingPos = base.runstack[--stackpos];
                EmitStackPop();
                Stloc(pos);
                EmitStackPop();
                Stloc(startingPos);
                if (expressionHasCaptures)
                {
                    // int poppedCrawlPos = base.runstack[--stackpos];
                    // while (base.Crawlpos() > poppedCrawlPos) base.Uncapture();
                    using RentedLocalBuilder poppedCrawlPos = RentInt32Local();
                    EmitStackPop();
                    Stloc(poppedCrawlPos);
                    EmitUncaptureUntil(poppedCrawlPos);
                }
                SliceInputSpan();

                if (minIterations > 0)
                {
                    // if (iterationCount == 0) goto originalDoneLabel;
                    Ldloc(iterationCount);
                    Ldc(0);
                    BeqFar(originalDoneLabel);

                    // if (iterationCount < minIterations) goto doneLabel/originalDoneLabel;
                    Ldloc(iterationCount);
                    Ldc(minIterations);
                    BltFar(childBacktracks ? doneLabel : originalDoneLabel);
                }

                if (isAtomic)
                {
                    doneLabel = originalDoneLabel;
                    MarkLabel(endLoop);
                }
                else
                {
                    if (childBacktracks)
                    {
                        // goto endLoop;
                        BrFar(endLoop);

                        // Backtrack:
                        Label backtrack = DefineLabel();
                        MarkLabel(backtrack);

                        // if (iterationCount == 0) goto originalDoneLabel;
                        Ldloc(iterationCount);
                        Ldc(0);
                        BeqFar(originalDoneLabel);

                        // goto doneLabel;
                        BrFar(doneLabel);

                        doneLabel = backtrack;
                    }

                    MarkLabel(endLoop);

                    if (node.IsInLoop())
                    {
                        // Store the capture's state
                        EmitStackResizeIfNeeded(3);
                        EmitStackPush(() => Ldloc(startingPos));
                        EmitStackPush(() => Ldloc(iterationCount));

                        // Skip past the backtracking section
                        // goto backtrackingEnd;
                        Label backtrackingEnd = DefineLabel();
                        BrFar(backtrackingEnd);

                        // Emit a backtracking section that restores the capture's state and then jumps to the previous done label
                        Label backtrack = DefineLabel();
                        MarkLabel(backtrack);

                        // iterationCount = base.runstack[--runstack];
                        // startingPos = base.runstack[--runstack];
                        EmitStackPop();
                        Stloc(iterationCount);
                        EmitStackPop();
                        Stloc(startingPos);

                        // goto doneLabel;
                        BrFar(doneLabel);

                        doneLabel = backtrack;
                        MarkLabel(backtrackingEnd);
                    }
                }
            }

            void EmitStackResizeIfNeeded(int count)
            {
                Debug.Assert(count >= 1);

                // if (stackpos >= base.runstack!.Length - (count - 1))
                // {
                //     Array.Resize(ref base.runstack, base.runstack.Length * 2);
                // }

                Label skipResize = DefineLabel();

                Ldloc(stackpos);
                Ldthisfld(s_runstackField);
                Ldlen();
                if (count > 1)
                {
                    Ldc(count - 1);
                    Sub();
                }
                Blt(skipResize);

                Ldthis();
                _ilg!.Emit(OpCodes.Ldflda, s_runstackField);
                Ldthisfld(s_runstackField);
                Ldlen();
                Ldc(2);
                Mul();
                Call(s_arrayResize);

                MarkLabel(skipResize);
            }

            void EmitStackPush(Action load)
            {
                // base.runstack[stackpos] = load();
                Ldthisfld(s_runstackField);
                Ldloc(stackpos);
                load();
                StelemI4();

                // stackpos++;
                Ldloc(stackpos);
                Ldc(1);
                Add();
                Stloc(stackpos);
            }

            void EmitStackPop()
            {
                // ... = base.runstack[--stackpos];
                Ldthisfld(s_runstackField);
                Ldloc(stackpos);
                Ldc(1);
                Sub();
                Stloc(stackpos);
                Ldloc(stackpos);
                LdelemI4();
            }
        }

        private void InitializeCultureForGoIfNecessary()
        {
            _textInfo = null;
            if ((_options & RegexOptions.CultureInvariant) == 0)
            {
                bool needsCulture = (_options & RegexOptions.IgnoreCase) != 0;
                if (!needsCulture)
                {
                    int[] codes = _code!.Codes;
                    for (int codepos = 0; codepos < codes.Length; codepos += RegexCode.OpcodeSize(codes[codepos]))
                    {
                        if ((codes[codepos] & RegexCode.Ci) == RegexCode.Ci)
                        {
                            needsCulture = true;
                            break;
                        }
                    }
                }

                if (needsCulture)
                {
                    // cache CultureInfo in local variable which saves excessive thread local storage accesses
                    _textInfo = DeclareTextInfo();
                    InitLocalCultureInfo();
                }
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

                case RegexCharClass.WordClass:
                    // RegexRunner.IsWordChar(ch)
                    Call(s_isWordCharMethod);
                    return;

                case RegexCharClass.NotWordClass:
                    // !RegexRunner.IsWordChar(ch)
                    Call(s_isWordCharMethod);
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
                Span<char> setChars = stackalloc char[3];
                int numChars = RegexCharClass.GetSetChars(charClass, setChars);
                if (numChars is 2 or 3)
                {
                    if (RegexCharClass.DifferByOneBit(setChars[0], setChars[1], out int mask)) // special-case common case of an upper and lowercase ASCII letter combination
                    {
                        // ((ch | mask) == setChars[1])
                        Ldloc(tempLocal);
                        Ldc(mask);
                        Or();
                        Ldc(setChars[1] | mask);
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

            Debug.Assert(_loopTimeoutCounter != null);

            // Increment counter for each loop iteration.
            Ldloc(_loopTimeoutCounter);
            Ldc(1);
            Add();
            Stloc(_loopTimeoutCounter);

            // Emit code to check the timeout every 2048th iteration.
            Label label = DefineLabel();
            Ldloc(_loopTimeoutCounter);
            Ldc(LoopTimeoutCheckCount);
            RemUn();
            Brtrue(label);
            Ldthis();
            Call(s_checkTimeoutMethod);
            MarkLabel(label);
        }
    }
}
