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
    /// RegexCompiler translates a block of RegexCode to MSIL, and creates a subclass of the RegexRunner type.
    /// </summary>
    [RequiresDynamicCode("Compiling a RegEx requires dynamic code.")]
    internal abstract class RegexCompiler
    {
        private static readonly FieldInfo s_runtextstartField = RegexRunnerField("runtextstart");
        private static readonly FieldInfo s_runtextposField = RegexRunnerField("runtextpos");
        private static readonly FieldInfo s_runtrackposField = RegexRunnerField("runtrackpos");
        private static readonly FieldInfo s_runstackField = RegexRunnerField("runstack");
        private static readonly FieldInfo s_cultureField = typeof(CompiledRegexRunner).GetField("_culture", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo s_caseBehaviorField = typeof(CompiledRegexRunner).GetField("_caseBehavior", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly MethodInfo s_captureMethod = RegexRunnerMethod("Capture");
        private static readonly MethodInfo s_transferCaptureMethod = RegexRunnerMethod("TransferCapture");
        private static readonly MethodInfo s_uncaptureMethod = RegexRunnerMethod("Uncapture");
        private static readonly MethodInfo s_isMatchedMethod = RegexRunnerMethod("IsMatched");
        private static readonly MethodInfo s_matchLengthMethod = RegexRunnerMethod("MatchLength");
        private static readonly MethodInfo s_matchIndexMethod = RegexRunnerMethod("MatchIndex");
        private static readonly MethodInfo s_isBoundaryMethod = typeof(RegexRunner).GetMethod("IsBoundary", BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(ReadOnlySpan<char>), typeof(int) })!;
        private static readonly MethodInfo s_isWordCharMethod = RegexRunnerMethod("IsWordChar");
        private static readonly MethodInfo s_isECMABoundaryMethod = typeof(RegexRunner).GetMethod("IsECMABoundary", BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(ReadOnlySpan<char>), typeof(int) })!;
        private static readonly MethodInfo s_crawlposMethod = RegexRunnerMethod("Crawlpos");
        private static readonly MethodInfo s_charInClassMethod = RegexRunnerMethod("CharInClass");
        private static readonly MethodInfo s_checkTimeoutMethod = RegexRunnerMethod("CheckTimeout");

        private static readonly MethodInfo s_regexCaseEquivalencesTryFindCaseEquivalencesForCharWithIBehaviorMethod = typeof(RegexCaseEquivalences).GetMethod("TryFindCaseEquivalencesForCharWithIBehavior", BindingFlags.Static | BindingFlags.Public)!;
        private static readonly MethodInfo s_charIsDigitMethod = typeof(char).GetMethod("IsDigit", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charIsWhiteSpaceMethod = typeof(char).GetMethod("IsWhiteSpace", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charGetUnicodeInfo = typeof(char).GetMethod("GetUnicodeCategory", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_spanGetItemMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Item", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_spanGetLengthMethod = typeof(ReadOnlySpan<char>).GetMethod("get_Length")!;
        private static readonly MethodInfo s_spanIndexOfChar = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfSpan = typeof(MemoryExtensions).GetMethod("IndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnyCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnyCharCharChar = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanIndexOfAnySpan = typeof(MemoryExtensions).GetMethod("IndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanLastIndexOfChar = typeof(MemoryExtensions).GetMethod("LastIndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanLastIndexOfAnyCharChar = typeof(MemoryExtensions).GetMethod("LastIndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanLastIndexOfAnyCharCharChar = typeof(MemoryExtensions).GetMethod("LastIndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanLastIndexOfAnySpan = typeof(MemoryExtensions).GetMethod("LastIndexOfAny", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanLastIndexOfSpan = typeof(MemoryExtensions).GetMethod("LastIndexOf", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanSliceIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_spanSliceIntIntMethod = typeof(ReadOnlySpan<char>).GetMethod("Slice", new Type[] { typeof(int), typeof(int) })!;
        private static readonly MethodInfo s_spanStartsWithSpan = typeof(MemoryExtensions).GetMethod("StartsWith", new Type[] { typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(ReadOnlySpan<>).MakeGenericType(Type.MakeGenericMethodParameter(0)) })!.MakeGenericMethod(typeof(char));
        private static readonly MethodInfo s_spanStartsWithSpanComparison = typeof(MemoryExtensions).GetMethod("StartsWith", new Type[] { typeof(ReadOnlySpan<char>), typeof(ReadOnlySpan<char>), typeof(StringComparison) })!;
        private static readonly MethodInfo s_stringAsSpanMethod = typeof(MemoryExtensions).GetMethod("AsSpan", new Type[] { typeof(string) })!;
        private static readonly MethodInfo s_stringGetCharsMethod = typeof(string).GetMethod("get_Chars", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_arrayResize = typeof(Array).GetMethod("Resize")!.MakeGenericMethod(typeof(int));
        private static readonly MethodInfo s_mathMinIntInt = typeof(Math).GetMethod("Min", new Type[] { typeof(int), typeof(int) })!;

        /// <summary>The ILGenerator currently in use.</summary>
        protected ILGenerator? _ilg;
        /// <summary>The options for the expression.</summary>
        protected RegexOptions _options;
        /// <summary>The <see cref="RegexTree"/> written for the expression.</summary>
        protected RegexTree? _regexTree;
        /// <summary>Whether this expression has a non-infinite timeout.</summary>
        protected bool _hasTimeout;

        /// <summary>Pool of Int32 LocalBuilders.</summary>
        private Stack<LocalBuilder>? _int32LocalsPool;
        /// <summary>Pool of ReadOnlySpan of char locals.</summary>
        private Stack<LocalBuilder>? _readOnlySpanCharLocalsPool;

        private static FieldInfo RegexRunnerField(string fieldname) => typeof(RegexRunner).GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;

        private static MethodInfo RegexRunnerMethod(string methname) => typeof(RegexRunner).GetMethod(methname, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)!;

        /// <summary>
        /// Entry point to dynamically compile a regular expression.  The expression is compiled to
        /// an in-memory assembly.
        /// </summary>
        internal static RegexRunnerFactory? Compile(string pattern, RegexTree regexTree, RegexOptions options, bool hasTimeout) =>
            new RegexLWCGCompiler().FactoryInstanceFromCode(pattern, regexTree, options, hasTimeout);

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

        /// <summary>A macro for _ilgEmit(OpCodes.Ldarg_1) </summary>
        private void Ldarg_1() => _ilg!.Emit(OpCodes.Ldarg_1);

        /// <summary>A macro for Ldthis(); Ldfld();</summary>
        protected void Ldthisfld(FieldInfo ft)
        {
            Ldthis();
            _ilg!.Emit(OpCodes.Ldfld, ft);
        }

        /// <summary>A macro for Ldthis(); Ldflda();</summary>
        protected void Ldthisflda(FieldInfo ft)
        {
            Ldthis();
            _ilg!.Emit(OpCodes.Ldflda, ft);
        }

        /// <summary>Fetches the address of argument in passed in <paramref name="position"/></summary>
        /// <param name="position">The position of the argument which address needs to be fetched.</param>
        private void Ldarga_s(int position) => _ilg!.Emit(OpCodes.Ldarga_S, position);

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

        /// <summary>A macro for _ilg.Emit(OpCodes.Brfalse) (short jump).</summary>
        private void Brfalse(Label l) => _ilg!.Emit(OpCodes.Brfalse_S, l);

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

        /// <summary>Declares a local bool.</summary>
        private LocalBuilder DeclareBool() => _ilg!.DeclareLocal(typeof(bool));

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
            private readonly Stack<LocalBuilder> _pool;
            private readonly LocalBuilder _local;

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

        /// <summary>Generates the implementation for TryFindNextPossibleStartingPosition.</summary>
        protected void EmitTryFindNextPossibleStartingPosition()
        {
            Debug.Assert(_regexTree != null);
            _int32LocalsPool?.Clear();
            _readOnlySpanCharLocalsPool?.Clear();

            LocalBuilder inputSpan = DeclareReadOnlySpanChar();
            LocalBuilder pos = DeclareInt32();
            bool rtl = (_options & RegexOptions.RightToLeft) != 0;

            // Load necessary locals
            // int pos = base.runtextpos;
            // ReadOnlySpan<char> inputSpan = dynamicMethodArg; // TODO: We can reference the arg directly rather than using another local.
            Mvfldloc(s_runtextposField, pos);
            Ldarg_1();
            Stloc(inputSpan);

            // Generate length check.  If the input isn't long enough to possibly match, fail quickly.
            // It's rare for min required length to be 0, so we don't bother special-casing the check,
            // especially since we want the "return false" code regardless.  This differs from the source
            // generator, where the return false is emitted at the end of the find method, and thus
            // avoids the branch for the 0 case.
            int minRequiredLength = _regexTree.FindOptimizations.MinRequiredLength;
            Debug.Assert(minRequiredLength >= 0);
            Label returnFalse = DefineLabel();
            Label finishedLengthCheck = DefineLabel();

            // if (pos > inputSpan.Length - minRequiredLength) // or pos < minRequiredLength for rtl
            // {
            //     base.runtextpos = inputSpan.Length; // or 0 for rtl
            //     return false;
            // }
            Ldloc(pos);
            if (!rtl)
            {
                Ldloca(inputSpan);
                Call(s_spanGetLengthMethod);
                if (minRequiredLength > 0)
                {
                    Ldc(minRequiredLength);
                    Sub();
                }
                Ble(finishedLengthCheck);
            }
            else
            {
                Ldc(minRequiredLength);
                Bge(finishedLengthCheck);
            }

            MarkLabel(returnFalse);
            Ldthis();
            if (!rtl)
            {
                Ldloca(inputSpan);
                Call(s_spanGetLengthMethod);
            }
            else
            {
                Ldc(0);
            }
            Stfld(s_runtextposField);
            Ldc(0);
            Ret();
            MarkLabel(finishedLengthCheck);

            // Emit any anchors.
            if (EmitAnchors())
            {
                return;
            }

            // Either anchors weren't specified, or they don't completely root all matches to a specific location.
            switch (_regexTree.FindOptimizations.FindMode)
            {
                case FindNextStartingPositionMode.LeadingString_LeftToRight:
                case FindNextStartingPositionMode.FixedDistanceString_LeftToRight:
                    EmitIndexOf_LeftToRight();
                    break;

                case FindNextStartingPositionMode.LeadingString_RightToLeft:
                    EmitIndexOf_RightToLeft();
                    break;

                case FindNextStartingPositionMode.LeadingSet_LeftToRight:
                case FindNextStartingPositionMode.FixedDistanceSets_LeftToRight:
                    EmitFixedSet_LeftToRight();
                    break;

                case FindNextStartingPositionMode.LeadingSet_RightToLeft:
                    EmitFixedSet_RightToLeft();
                    break;

                case FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight:
                    EmitLiteralAfterAtomicLoop();
                    break;

                default:
                    Debug.Fail($"Unexpected mode: {_regexTree.FindOptimizations.FindMode}");
                    goto case FindNextStartingPositionMode.NoSearch;

                case FindNextStartingPositionMode.NoSearch:
                    // return true;
                    Ldc(1);
                    Ret();
                    break;
            }

            // Emits any anchors.  Returns true if the anchor roots any match to a specific location and thus no further
            // searching is required; otherwise, false.
            bool EmitAnchors()
            {
                Label label;

                // Anchors that fully implement TryFindNextPossibleStartingPosition, with a check that leads to immediate success or failure determination.
                switch (_regexTree.FindOptimizations.FindMode)
                {
                    case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning:
                        // if (pos != 0) goto returnFalse;
                        // return true;
                        Ldloc(pos);
                        Ldc(0);
                        Bne(returnFalse);
                        Ldc(1);
                        Ret();
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start:
                    case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start:
                        // if (pos != base.runtextstart) goto returnFalse;
                        // return true;
                        Ldloc(pos);
                        Ldthisfld(s_runtextstartField);
                        Bne(returnFalse);
                        Ldc(1);
                        Ret();
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_EndZ:
                        // if (pos < inputSpan.Length - 1) base.runtextpos = inputSpan.Length - 1;
                        // return true;
                        label = DefineLabel();
                        Ldloc(pos);
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        Ldc(1);
                        Sub();
                        Bge(label);
                        Ldthis();
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        Ldc(1);
                        Sub();
                        Stfld(s_runtextposField);
                        MarkLabel(label);
                        Ldc(1);
                        Ret();
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_LeftToRight_End:
                        // if (pos < inputSpan.Length) base.runtextpos = inputSpan.Length;
                        // return true;
                        label = DefineLabel();
                        Ldloc(pos);
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        Bge(label);
                        Ldthis();
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        Stfld(s_runtextposField);
                        MarkLabel(label);
                        Ldc(1);
                        Ret();
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Beginning:
                        // if (pos != 0) base.runtextpos = 0;
                        // return true;
                        label = DefineLabel();
                        Ldloc(pos);
                        Ldc(0);
                        Beq(label);
                        Ldthis();
                        Ldc(0);
                        Stfld(s_runtextposField);
                        MarkLabel(label);
                        Ldc(1);
                        Ret();
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_EndZ:
                        // if (pos < inputSpan.Length - 1 || ((uint)pos < (uint)inputSpan.Length && inputSpan[pos] != '\n') goto returnFalse;
                        // return true;
                        label = DefineLabel();
                        Ldloc(pos);
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        Ldc(1);
                        Sub();
                        Blt(returnFalse);
                        Ldloc(pos);
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        BgeUn(label);
                        Ldloca(inputSpan);
                        Ldloc(pos);
                        Call(s_spanGetItemMethod);
                        LdindU2();
                        Ldc('\n');
                        Bne(returnFalse);
                        MarkLabel(label);
                        Ldc(1);
                        Ret();
                        return true;

                    case FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End:
                        // if (pos < inputSpan.Length) goto returnFalse;
                        // return true;
                        Ldloc(pos);
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        Blt(returnFalse);
                        Ldc(1);
                        Ret();
                        return true;

                    case FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_End:
                    case FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ:
                        // Jump to the end, minus the min required length, which in this case is actually the fixed length.
                        {
                            int extraNewlineBump = _regexTree.FindOptimizations.FindMode == FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ ? 1 : 0;
                            label = DefineLabel();
                            Ldloc(pos);
                            Ldloca(inputSpan);
                            Call(s_spanGetLengthMethod);
                            Ldc(_regexTree.FindOptimizations.MinRequiredLength + extraNewlineBump);
                            Sub();
                            Bge(label);
                            Ldthis();
                            Ldloca(inputSpan);
                            Call(s_spanGetLengthMethod);
                            Ldc(_regexTree.FindOptimizations.MinRequiredLength + extraNewlineBump);
                            Sub();
                            Stfld(s_runtextposField);
                            MarkLabel(label);
                            Ldc(1);
                            Ret();
                            return true;
                        }
                }

                // Now handle anchors that boost the position but don't determine immediate success or failure.
                if (!rtl) // we haven't done the work to validate these optimizations for RightToLeft
                {
                    switch (_regexTree.FindOptimizations.LeadingAnchor)
                    {
                        case RegexNodeKind.Bol:
                            {
                                // Optimize the handling of a Beginning-Of-Line (BOL) anchor.  BOL is special, in that unlike
                                // other anchors like Beginning, there are potentially multiple places a BOL can match.  So unlike
                                // the other anchors, which all skip all subsequent processing if found, with BOL we just use it
                                // to boost our position to the next line, and then continue normally with any prefix or char class searches.

                                label = DefineLabel();

                                // if (pos > 0...
                                Ldloc(pos!);
                                Ldc(0);
                                Ble(label);

                                // ... && inputSpan[pos - 1] != '\n') { ... }
                                Ldloca(inputSpan);
                                Ldloc(pos);
                                Ldc(1);
                                Sub();
                                Call(s_spanGetItemMethod);
                                LdindU2();
                                Ldc('\n');
                                Beq(label);

                                // int tmp = inputSpan.Slice(pos).IndexOf('\n');
                                Ldloca(inputSpan);
                                Ldloc(pos);
                                Call(s_spanSliceIntMethod);
                                Ldc('\n');
                                Call(s_spanIndexOfChar);
                                using (RentedLocalBuilder newlinePos = RentInt32Local())
                                {
                                    Stloc(newlinePos);

                                    // if (newlinePos < 0 || newlinePos + pos + 1 > inputSpan.Length)
                                    // {
                                    //     base.runtextpos = inputSpan.Length;
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
                                    Ldloca(inputSpan);
                                    Call(s_spanGetLengthMethod);
                                    Bgt(returnFalse);

                                    // pos += newlinePos + 1;
                                    Ldloc(pos);
                                    Ldloc(newlinePos);
                                    Add();
                                    Ldc(1);
                                    Add();
                                    Stloc(pos);

                                    // We've updated the position.  Make sure there's still enough room in the input for a possible match.
                                    // if (pos > inputSpan.Length - minRequiredLength) returnFalse;
                                    Ldloca(inputSpan);
                                    Call(s_spanGetLengthMethod);
                                    if (minRequiredLength != 0)
                                    {
                                        Ldc(minRequiredLength);
                                        Sub();
                                    }
                                    Ldloc(pos);
                                    BltFar(returnFalse);
                                }

                                MarkLabel(label);
                            }
                            break;
                    }

                    switch (_regexTree.FindOptimizations.TrailingAnchor)
                    {
                        case RegexNodeKind.End or RegexNodeKind.EndZ when _regexTree.FindOptimizations.MaxPossibleLength is int maxLength:
                            // Jump to the end, minus the max allowed length.
                            {
                                int extraNewlineBump = _regexTree.FindOptimizations.FindMode == FindNextStartingPositionMode.TrailingAnchor_FixedLength_LeftToRight_EndZ ? 1 : 0;
                                label = DefineLabel();
                                Ldloc(pos);
                                Ldloca(inputSpan);
                                Call(s_spanGetLengthMethod);
                                Ldc(maxLength + extraNewlineBump);
                                Sub();
                                Bge(label);
                                Ldloca(inputSpan);
                                Call(s_spanGetLengthMethod);
                                Ldc(maxLength + extraNewlineBump);
                                Sub();
                                Stloc(pos);
                                MarkLabel(label);
                                break;
                            }
                    }
                }

                return false;
            }

            // Emits a case-sensitive left-to-right search for a substring.
            void EmitIndexOf_LeftToRight()
            {
                RegexFindOptimizations opts = _regexTree.FindOptimizations;
                Debug.Assert(opts.FindMode is FindNextStartingPositionMode.LeadingString_LeftToRight or FindNextStartingPositionMode.FixedDistanceString_LeftToRight);

                using RentedLocalBuilder i = RentInt32Local();

                // int i = inputSpan.Slice(pos).IndexOf(prefix);
                Ldloca(inputSpan);
                Ldloc(pos);
                if (opts.FindMode == FindNextStartingPositionMode.FixedDistanceString_LeftToRight &&
                    opts.FixedDistanceLiteral is { Distance: > 0 } literal)
                {
                    Ldc(literal.Distance);
                    Add();
                }
                Call(s_spanSliceIntMethod);
                Ldstr(opts.FindMode == FindNextStartingPositionMode.LeadingString_LeftToRight ?
                    opts.LeadingPrefix :
                    opts.FixedDistanceLiteral.String!);
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

            // Emits a case-sensitive right-to-left search for a substring.
            void EmitIndexOf_RightToLeft()
            {
                string prefix = _regexTree.FindOptimizations.LeadingPrefix;
                Debug.Assert(!string.IsNullOrEmpty(prefix));

                // pos = inputSpan.Slice(0, pos).LastIndexOf(prefix);
                Ldloca(inputSpan);
                Ldc(0);
                Ldloc(pos);
                Call(s_spanSliceIntIntMethod);
                Ldstr(prefix);
                Call(s_stringAsSpanMethod);
                Call(s_spanLastIndexOfSpan);
                Stloc(pos);

                // if (pos < 0) goto ReturnFalse;
                Ldloc(pos);
                Ldc(0);
                BltFar(returnFalse);

                // base.runtextpos = pos + prefix.Length;
                // return true;
                Ldthis();
                Ldloc(pos);
                Ldc(prefix.Length);
                Add();
                Stfld(s_runtextposField);
                Ldc(1);
                Ret();
            }

            // Emits a search for a set at a fixed position from the start of the pattern,
            // and potentially other sets at other fixed positions in the pattern.
            void EmitFixedSet_LeftToRight()
            {
                Debug.Assert(_regexTree.FindOptimizations.FixedDistanceSets is { Count: > 0 });

                List<(char[]? Chars, string Set, int Distance)>? sets = _regexTree.FindOptimizations.FixedDistanceSets;
                (char[]? Chars, string Set, int Distance) primarySet = sets![0];
                const int MaxSets = 4;
                int setsToUse = Math.Min(sets.Count, MaxSets);

                using RentedLocalBuilder iLocal = RentInt32Local();
                using RentedLocalBuilder textSpanLocal = RentReadOnlySpanCharLocal();

                // ReadOnlySpan<char> span = inputSpan.Slice(pos);
                Ldloca(inputSpan);
                Ldloc(pos);
                Call(s_spanSliceIntMethod);
                Stloc(textSpanLocal);

                // If we can use IndexOf{Any}, try to accelerate the skip loop via vectorization to match the first prefix.
                // We can use it if this is a case-sensitive class with a small number of characters in the class.
                int setIndex = 0;
                bool canUseIndexOf = primarySet.Chars is not null;
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
                Debug.Assert(setIndex is 0 or 1);
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
                    EmitMatchCharacterClass(sets[setIndex].Set);
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

                    // base.runtextpos = inputSpan.Length;
                    // return false;
                    BrFar(returnFalse);
                }
            }

            // Emits a right-to-left search for a set at a fixed position from the start of the pattern.
            // (Currently that position will always be a distance of 0, meaning the start of the pattern itself.)
            void EmitFixedSet_RightToLeft()
            {
                Debug.Assert(_regexTree.FindOptimizations.FixedDistanceSets is { Count: > 0 });

                (char[]? Chars, string Set, int Distance) set = _regexTree.FindOptimizations.FixedDistanceSets![0];
                Debug.Assert(set.Distance == 0);

                if (set.Chars is { Length: 1 })
                {
                    // pos = inputSpan.Slice(0, pos).LastIndexOf(set.Chars[0]);
                    Ldloca(inputSpan);
                    Ldc(0);
                    Ldloc(pos);
                    Call(s_spanSliceIntIntMethod);
                    Ldc(set.Chars[0]);
                    Call(s_spanLastIndexOfChar);
                    Stloc(pos);

                    // if (pos < 0) goto returnFalse;
                    Ldloc(pos);
                    Ldc(0);
                    BltFar(returnFalse);

                    // base.runtextpos = pos + 1;
                    // return true;
                    Ldthis();
                    Ldloc(pos);
                    Ldc(1);
                    Add();
                    Stfld(s_runtextposField);
                    Ldc(1);
                    Ret();
                }
                else
                {
                    Label condition = DefineLabel();

                    // while ((uint)--pos < (uint)inputSpan.Length)
                    MarkLabel(condition);
                    Ldloc(pos);
                    Ldc(1);
                    Sub();
                    Stloc(pos);
                    Ldloc(pos);
                    Ldloca(inputSpan);
                    Call(s_spanGetLengthMethod);
                    BgeUnFar(returnFalse);

                    // if (!MatchCharacterClass(inputSpan[i], set.Set)) goto condition;
                    Ldloca(inputSpan);
                    Ldloc(pos);
                    Call(s_spanGetItemMethod);
                    LdindU2();
                    EmitMatchCharacterClass(set.Set);
                    Brfalse(condition);

                    // base.runtextpos = pos + 1;
                    // return true;
                    Ldthis();
                    Ldloc(pos);
                    Ldc(1);
                    Add();
                    Stfld(s_runtextposField);
                    Ldc(1);
                    Ret();
                }
            }

            // Emits a search for a literal following a leading atomic single-character loop.
            void EmitLiteralAfterAtomicLoop()
            {
                Debug.Assert(_regexTree.FindOptimizations.LiteralAfterLoop is not null);
                (RegexNode LoopNode, (char Char, string? String, char[]? Chars) Literal) target = _regexTree.FindOptimizations.LiteralAfterLoop.Value;

                Debug.Assert(target.LoopNode.Kind is RegexNodeKind.Setloop or RegexNodeKind.Setlazy or RegexNodeKind.Setloopatomic);
                Debug.Assert(target.LoopNode.N == int.MaxValue);

                // while (true)
                Label loopBody = DefineLabel();
                Label loopEnd = DefineLabel();
                MarkLabel(loopBody);

                // ReadOnlySpan<char> slice = inputSpan.Slice(pos);
                using RentedLocalBuilder slice = RentReadOnlySpanCharLocal();
                Ldloca(inputSpan);
                Ldloc(pos);
                Call(s_spanSliceIntMethod);
                Stloc(slice);

                // Find the literal.  If we can't find it, we're done searching.
                // int i = slice.IndexOf(literal);
                // if (i < 0) break;
                using RentedLocalBuilder i = RentInt32Local();
                Ldloc(slice);
                if (target.Literal.String is string literalString)
                {
                    Ldstr(literalString);
                    Call(s_stringAsSpanMethod);
                    Call(s_spanIndexOfSpan);
                }
                else if (target.Literal.Chars is not char[] literalChars)
                {
                    Ldc(target.Literal.Char);
                    Call(s_spanIndexOfChar);
                }
                else
                {
                    switch (literalChars.Length)
                    {
                        case 2:
                            Ldc(literalChars[0]);
                            Ldc(literalChars[1]);
                            Call(s_spanIndexOfAnyCharChar);
                            break;
                        case 3:
                            Ldc(literalChars[0]);
                            Ldc(literalChars[1]);
                            Ldc(literalChars[2]);
                            Call(s_spanIndexOfAnyCharCharChar);
                            break;
                        default:
                            Ldstr(new string(literalChars));
                            Call(s_stringAsSpanMethod);
                            Call(s_spanIndexOfAnySpan);
                            break;
                    }
                }
                Stloc(i);
                Ldloc(i);
                Ldc(0);
                BltFar(loopEnd);

                // We found the literal.  Walk backwards from it finding as many matches as we can against the loop.

                // int prev = i;
                using RentedLocalBuilder prev = RentInt32Local();
                Ldloc(i);
                Stloc(prev);

                // while ((uint)--prev < (uint)slice.Length) && MatchCharClass(slice[prev]));
                Label innerLoopBody = DefineLabel();
                Label innerLoopEnd = DefineLabel();
                MarkLabel(innerLoopBody);
                Ldloc(prev);
                Ldc(1);
                Sub();
                Stloc(prev);
                Ldloc(prev);
                Ldloca(slice);
                Call(s_spanGetLengthMethod);
                BgeUn(innerLoopEnd);
                Ldloca(slice);
                Ldloc(prev);
                Call(s_spanGetItemMethod);
                LdindU2();
                EmitMatchCharacterClass(target.LoopNode.Str!);
                BrtrueFar(innerLoopBody);
                MarkLabel(innerLoopEnd);

                if (target.LoopNode.M > 0)
                {
                    // If we found fewer than needed, loop around to try again.  The loop doesn't overlap with the literal,
                    // so we can start from after the last place the literal matched.
                    // if ((i - prev - 1) < target.LoopNode.M)
                    // {
                    //     pos += i + 1;
                    //     continue;
                    // }
                    Label metMinimum = DefineLabel();
                    Ldloc(i);
                    Ldloc(prev);
                    Sub();
                    Ldc(1);
                    Sub();
                    Ldc(target.LoopNode.M);
                    Bge(metMinimum);
                    Ldloc(pos);
                    Ldloc(i);
                    Add();
                    Ldc(1);
                    Add();
                    Stloc(pos);
                    BrFar(loopBody);
                    MarkLabel(metMinimum);
                }

                // We have a winner.  The starting position is just after the last position that failed to match the loop.
                // We also store the position after the loop into runtrackpos (an extra, unused field on RegexRunner) in order
                // to communicate this position to the match algorithm such that it can skip the loop.

                // base.runtextpos = pos + prev + 1;
                Ldthis();
                Ldloc(pos);
                Ldloc(prev);
                Add();
                Ldc(1);
                Add();
                Stfld(s_runtextposField);

                // base.runtrackpos = pos + i;
                Ldthis();
                Ldloc(pos);
                Ldloc(i);
                Add();
                Stfld(s_runtrackposField);

                // return true;
                Ldc(1);
                Ret();

                // }
                MarkLabel(loopEnd);

                // base.runtextpos = inputSpan.Length;
                // return false;
                BrFar(returnFalse);
            }
        }

        /// <summary>Generates the implementation for TryMatchAtCurrentPosition.</summary>
        protected void EmitTryMatchAtCurrentPosition()
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
            // "doneLabel" is simply the final return location from the TryMatchAtCurrentPosition method that will undo any captures and exit, signaling to
            // the calling scan loop that nothing was matched.

            Debug.Assert(_regexTree != null);
            _int32LocalsPool?.Clear();
            _readOnlySpanCharLocalsPool?.Clear();

            // Get the root Capture node of the tree.
            RegexNode node = _regexTree.Root;
            Debug.Assert(node.Kind == RegexNodeKind.Capture, "Every generated tree should begin with a capture node");
            Debug.Assert(node.ChildCount() == 1, "Capture nodes should have one child");

            // Skip the Capture node. We handle the implicit root capture specially.
            node = node.Child(0);

            // In some limited cases, TryFindNextPossibleStartingPosition will only return true if it successfully matched the whole expression.
            // We can special case these to do essentially nothing in TryMatchAtCurrentPosition other than emit the capture.
            switch (node.Kind)
            {
                case RegexNodeKind.Multi or RegexNodeKind.Notone or RegexNodeKind.One or RegexNodeKind.Set:
                    // This is the case for single and multiple characters, though the whole thing is only guaranteed
                    // to have been validated in TryFindNextPossibleStartingPosition when doing case-sensitive comparison.
                    // base.Capture(0, base.runtextpos, base.runtextpos + node.Str.Length);
                    // base.runtextpos = base.runtextpos + node.Str.Length;
                    // return true;
                    int length = node.Kind == RegexNodeKind.Multi ? node.Str!.Length : 1;
                    if ((node.Options & RegexOptions.RightToLeft) != 0)
                    {
                        length = -length;
                    }
                    Ldthis();
                    Dup();
                    Ldc(0);
                    Ldthisfld(s_runtextposField);
                    Dup();
                    Ldc(length);
                    Add();
                    Call(s_captureMethod);
                    Ldthisfld(s_runtextposField);
                    Ldc(length);
                    Add();
                    Stfld(s_runtextposField);
                    Ldc(1);
                    Ret();
                    return;

                    // The source generator special-cases RegexNode.Empty, for purposes of code learning rather than
                    // performance.  Since that's not applicable to RegexCompiler, that code isn't mirrored here.
            }

            AnalysisResults analysis = RegexTreeAnalyzer.Analyze(_regexTree);

            // Initialize the main locals used throughout the implementation.
            LocalBuilder inputSpan = DeclareReadOnlySpanChar();
            LocalBuilder originalPos = DeclareInt32();
            LocalBuilder pos = DeclareInt32();
            LocalBuilder slice = DeclareReadOnlySpanChar();
            Label doneLabel = DefineLabel();
            Label originalDoneLabel = doneLabel;

            // ReadOnlySpan<char> inputSpan = input;
            Ldarg_1();
            Stloc(inputSpan);

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

            // Check whether there are captures anywhere in the expression. If there isn't, we can skip all
            // the boilerplate logic around uncapturing, as there won't be anything to uncapture.
            bool expressionHasCaptures = analysis.MayContainCapture(node);

            // Emit the code for all nodes in the tree.
            EmitNode(node);

            // pos += sliceStaticPos;
            // base.runtextpos = pos;
            // Capture(0, originalpos, pos);
            // return true;
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
            Ldc(1);
            Ret();

            // NOTE: The following is a difference from the source generator.  The source generator emits:
            //     UncaptureUntil(0);
            //     return false;
            // at every location where the all-up match is known to fail. In contrast, the compiler currently
            // emits this uncapture/return code in one place and jumps to it upon match failure.  The difference
            // stems primarily from the return-at-each-location pattern resulting in cleaner / easier to read
            // source code, which is not an issue for RegexCompiler emitting IL instead of C#.

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

            // return false;
            Ldc(0);
            Ret();

            // Generated code successfully.
            return;

            // Slices the inputSpan starting at pos until end and stores it into slice.
            void SliceInputSpan()
            {
                // slice = inputSpan.Slice(pos);
                Ldloca(inputSpan);
                Ldloc(pos);
                Call(s_spanSliceIntMethod);
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

            // Adds the value of sliceStaticPos into the pos local, zeros out sliceStaticPos,
            // and resets slice to be inputSpan.Slice(pos).
            void TransferSliceStaticPosToPos(bool forceSliceReload = false)
            {
                if (sliceStaticPos > 0)
                {
                    // pos += sliceStaticPos;
                    // sliceStaticPos = 0;
                    Ldloc(pos);
                    Ldc(sliceStaticPos);
                    Add();
                    Stloc(pos);
                    sliceStaticPos = 0;

                    // slice = inputSpan.Slice(pos);
                    SliceInputSpan();
                }
                else if (forceSliceReload)
                {
                    // slice = inputSpan.Slice(pos);
                    SliceInputSpan();
                }
            }

            // Emits the code for an alternation.
            void EmitAlternation(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Alternate, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() >= 2, $"Expected at least 2 children, found {node.ChildCount()}");

                int childCount = node.ChildCount();
                Debug.Assert(childCount >= 2);

                Label originalDoneLabel = doneLabel;

                // Both atomic and non-atomic are supported.  While a parent RegexNode.Atomic node will itself
                // successfully prevent backtracking into this child node, we can emit better / cheaper code
                // for an Alternate when it is atomic, so we still take it into account here.
                Debug.Assert(node.Parent is not null);
                bool isAtomic = analysis.IsAtomicByAncestor(node);

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
                if (expressionHasCaptures && (analysis.MayContainCapture(node) || !isAtomic))
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
                bool canUseLocalsForAllState = !isAtomic && !analysis.IsInLoop(node);
                var labelMap = new Label[childCount];
                Label backtrackLabel = DefineLabel();
                LocalBuilder? currentBranch = canUseLocalsForAllState ? DeclareInt32() : null;

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
                        // If we're inside of a loop, push the state we need to preserve on to the
                        // the backtracking stack.  If we're not inside of a loop, simply ensure all
                        // the relevant state is stored in our locals.
                        if (currentBranch is null)
                        {
                            // if (stackpos + 3 >= base.runstack.Length) Array.Resize(ref base.runstack, base.runstack.Length * 2);
                            // base.runstack[stackpos++] = i;
                            // base.runstack[stackpos++] = startingCapturePos;
                            // base.runstack[stackpos++] = startingPos;
                            EmitStackResizeIfNeeded(2 + (startingCapturePos is not null ? 1 : 0));
                            EmitStackPush(() => Ldc(i));
                            if (startingCapturePos is not null)
                            {
                                EmitStackPush(() => Ldloc(startingCapturePos));
                            }
                            EmitStackPush(() => Ldloc(startingPos));
                        }
                        else
                        {
                            // currentBranch = i;
                            Ldc(i);
                            Stloc(currentBranch);
                        }
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
                        // slice = inputSpan.Slice(pos);
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

                    // We're backtracking.  Check the timeout.
                    EmitTimeoutCheckIfNeeded();

                    if (currentBranch is null)
                    {
                        // We're in a loop, so we use the backtracking stack to persist our state. Pop it off.

                        // startingPos = base.runstack[--stackpos];
                        // startingCapturePos = base.runstack[--stackpos];
                        // switch (base.runstack[--stackpos])
                        EmitStackPop();
                        Stloc(startingPos);
                        if (startingCapturePos is not null)
                        {
                            EmitStackPop();
                            Stloc(startingCapturePos);
                        }
                        EmitStackPop();
                    }
                    else
                    {
                        // We're not in a loop, so our locals already store the state we need.
                        // switch (currentBranch)
                        Ldloc(currentBranch);
                    }
                    Switch(labelMap);
                }

                // Successfully completed the alternate.
                MarkLabel(matchLabel);
                Debug.Assert(sliceStaticPos == 0);
            }

            // Emits the code to handle a backreference.
            void EmitBackreference(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Backreference, $"Unexpected type: {node.Kind}");

                int capnum = RegexParser.MapCaptureNumber(node.M, _regexTree!.CaptureNumberSparseMapping);
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;

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

                if (!rtl)
                {
                    // if (slice.Length < matchLength) goto doneLabel;
                    Ldloca(slice);
                    Call(s_spanGetLengthMethod);
                }
                else
                {
                    // if (pos < matchLength) goto doneLabel;
                    Ldloc(pos);
                }
                Ldloc(matchLength);
                BltFar(doneLabel);

                // int matchIndex = base.MatchIndex(capnum);
                Ldthis();
                Ldc(capnum);
                Call(s_matchIndexMethod);
                Stloc(matchIndex);

                Label condition = DefineLabel();
                Label body = DefineLabel();
                Label charactersMatched = DefineLabel();
                LocalBuilder backreferenceCharacter = _ilg!.DeclareLocal(typeof(char));
                LocalBuilder currentCharacter = _ilg!.DeclareLocal(typeof(char));

                // for (int i = 0; ...)
                Ldc(0);
                Stloc(i);
                Br(condition);

                MarkLabel(body);

                // char backreferenceChar = inputSpan[matchIndex + i];
                Ldloca(inputSpan);
                Ldloc(matchIndex);
                Ldloc(i);
                Add();
                Call(s_spanGetItemMethod);
                LdindU2();
                Stloc(backreferenceCharacter);
                if (!rtl)
                {
                    // char currentChar = slice[i];
                    Ldloca(slice);
                    Ldloc(i);
                }
                else
                {
                    // char currentChar = inputSpan[pos - matchLength + i];
                    Ldloca(inputSpan);
                    Ldloc(pos);
                    Ldloc(matchLength);
                    Sub();
                    Ldloc(i);
                    Add();
                }
                Call(s_spanGetItemMethod);
                LdindU2();
                Stloc(currentCharacter);

                if ((node.Options & RegexOptions.IgnoreCase) != 0)
                {
                    LocalBuilder caseEquivalences = DeclareReadOnlySpanChar();

                    // if (backreferenceChar != currentChar)
                    Ldloc(backreferenceCharacter);
                    Ldloc(currentCharacter);
                    Ceq();
                    BrtrueFar(charactersMatched);

                    // if (RegexCaseEquivalences.TryFindCaseEquivalencesForCharWithIBehavior(backreferenceChar, _culture, ref _caseBehavior, out ReadOnlySpan<char> equivalences))
                    Ldloc(backreferenceCharacter);
                    Ldthisfld(s_cultureField);
                    Ldthisflda(s_caseBehaviorField);
                    Ldloca(caseEquivalences);
                    Call(s_regexCaseEquivalencesTryFindCaseEquivalencesForCharWithIBehaviorMethod);
                    BrfalseFar(doneLabel);

                    // if (equivalences.IndexOf(slice[i]) == -1) // Or if (equivalences.IndexOf(inputSpan[pos - matchLength + i]) == -1) when rtl
                    Ldloc(caseEquivalences);
                    if (!rtl)
                    {
                        Ldloca(slice);
                        Ldloc(i);
                    }
                    else
                    {
                        Ldloca(inputSpan);
                        Ldloc(pos);
                        Ldloc(matchLength);
                        Sub();
                        Ldloc(i);
                        Add();
                    }
                    Call(s_spanGetItemMethod);
                    LdindU2();
                    Call(s_spanIndexOfChar);
                    Ldc(-1);
                    Ceq();
                    // return false; // input didn't match.
                    BrtrueFar(doneLabel);
                }
                else
                {
                    // if (backreferenceCharacter != currentCharacter)
                    Ldloc(backreferenceCharacter);
                    Ldloc(currentCharacter);
                    Ceq();
                    // return false; // input didn't match.
                    BrfalseFar(doneLabel);
                }

                MarkLabel(charactersMatched);

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

                // pos += matchLength; // or -= for rtl
                Ldloc(pos);
                Ldloc(matchLength);
                if (!rtl)
                {
                    Add();
                }
                else
                {
                    Sub();
                }
                Stloc(pos);

                if (!rtl)
                {
                    SliceInputSpan();
                }

                MarkLabel(backreferenceEnd);
            }

            // Emits the code for an if(backreference)-then-else conditional.
            void EmitBackreferenceConditional(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.BackreferenceConditional, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 2, $"Expected 2 children, found {node.ChildCount()}");

                bool isAtomic = analysis.IsAtomicByAncestor(node);

                // We're branching in a complicated fashion.  Make sure sliceStaticPos is 0.
                TransferSliceStaticPosToPos();

                // Get the capture number to test.
                int capnum = RegexParser.MapCaptureNumber(node.M, _regexTree!.CaptureNumberSparseMapping);

                // Get the "yes" branch and the "no" branch.  The "no" branch is optional in syntax and is thus
                // somewhat likely to be Empty.
                RegexNode yesBranch = node.Child(0);
                RegexNode? noBranch = node.Child(1) is { Kind: not RegexNodeKind.Empty } childNo ? childNo : null;
                Label originalDoneLabel = doneLabel;

                Label refNotMatched = DefineLabel();
                Label endConditional = DefineLabel();

                // As with alternations, we have potentially multiple branches, each of which may contain
                // backtracking constructs, but the expression after the conditional needs a single target
                // to backtrack to.  So, we expose a single Backtrack label and track which branch was
                // followed in this resumeAt local.
                LocalBuilder resumeAt = DeclareInt32();
                bool isInLoop = analysis.IsInLoop(node);

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
                    // If we're not in a loop, though, we won't have pushed it on to the stack as nothing will
                    // have been able to overwrite it in the interim, so we can just trust the value already in
                    // the local.
                    if (isInLoop)
                    {
                        // resumeAt = base.runstack[--stackpos];
                        EmitStackPop();
                        Stloc(resumeAt);
                    }

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

                    if (isInLoop)
                    {
                        // We're not atomic and at least one of the yes or no branches contained backtracking constructs,
                        // so finish outputting our backtracking logic, which involves pushing onto the stack which
                        // branch to backtrack into.  If we're not in a loop, though, nothing else can overwrite this local
                        // in the interim, so we can avoid pushing it.
                        // if (stackpos + 1 >= base.runstack.Length) Array.Resize(ref base.runstack, base.runstack.Length * 2);
                        // base.runstack[stackpos++] = resumeAt;
                        EmitStackResizeIfNeeded(1);
                        EmitStackPush(() => Ldloc(resumeAt));
                    }
                }
            }

            // Emits the code for an if(expression)-then-else conditional.
            void EmitExpressionConditional(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.ExpressionConditional, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 3, $"Expected 3 children, found {node.ChildCount()}");

                bool isAtomic = analysis.IsAtomicByAncestor(node);

                // We're branching in a complicated fashion.  Make sure sliceStaticPos is 0.
                TransferSliceStaticPosToPos();

                // The first child node is the condition expression.  If this matches, then we branch to the "yes" branch.
                // If it doesn't match, then we branch to the optional "no" branch if it exists, or simply skip the "yes"
                // branch, otherwise. The condition is treated as a positive lookaround.
                RegexNode condition = node.Child(0);

                // Get the "yes" branch and the "no" branch.  The "no" branch is optional in syntax and is thus
                // somewhat likely to be Empty.
                RegexNode yesBranch = node.Child(1);
                RegexNode? noBranch = node.Child(2) is { Kind: not RegexNodeKind.Empty } childNo ? childNo : null;
                Label originalDoneLabel = doneLabel;

                Label expressionNotMatched = DefineLabel();
                Label endConditional = DefineLabel();

                // As with alternations, we have potentially multiple branches, each of which may contain
                // backtracking constructs, but the expression after the condition needs a single target
                // to backtrack to.  So, we expose a single Backtrack label and track which branch was
                // followed in this resumeAt local.
                bool isInLoop = false;
                LocalBuilder? resumeAt = null;
                if (!isAtomic)
                {
                    isInLoop = analysis.IsInLoop(node);
                    resumeAt = DeclareInt32();
                }

                // If the condition expression has captures, we'll need to uncapture them in the case of no match.
                LocalBuilder? startingCapturePos = null;
                if (analysis.MayContainCapture(condition))
                {
                    // int startingCapturePos = base.Crawlpos();
                    startingCapturePos = DeclareInt32();
                    Ldthis();
                    Call(s_crawlposMethod);
                    Stloc(startingCapturePos);
                }

                // Emit the condition expression.  Route any failures to after the yes branch.  This code is almost
                // the same as for a positive lookaround; however, a positive lookaround only needs to reset the position
                // on a successful match, as a failed match fails the whole expression; here, we need to reset the
                // position on completion, regardless of whether the match is successful or not.
                doneLabel = expressionNotMatched;

                // Save off pos.  We'll need to reset this upon successful completion of the lookaround.
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
                // Do not reset captures, which persist beyond the lookaround.
                // pos = startingPos;
                // slice = inputSpan.Slice(pos);
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

                    if (isInLoop)
                    {
                        // If we're not in a loop, the local will maintain its value until backtracking occurs.
                        // If we are in a loop, multiple iterations need their own value, so we need to use the stack.

                        // resumeAt = StackPop();
                        EmitStackPop();
                        Stloc(resumeAt);
                    }

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

                    if (isInLoop)
                    {
                        // if (stackpos + 1 >= base.runstack.Length) Array.Resize(ref base.runstack, base.runstack.Length * 2);
                        // base.runstack[stackpos++] = resumeAt;
                        EmitStackResizeIfNeeded(1);
                        EmitStackPush(() => Ldloc(resumeAt!));
                    }
                }
            }

            // Emits the code for a Capture node.
            void EmitCapture(RegexNode node, RegexNode? subsequent = null)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Capture, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                int capnum = RegexParser.MapCaptureNumber(node.M, _regexTree!.CaptureNumberSparseMapping);
                int uncapnum = RegexParser.MapCaptureNumber(node.N, _regexTree.CaptureNumberSparseMapping);
                bool isAtomic = analysis.IsAtomicByAncestor(node);
                bool isInLoop = analysis.IsInLoop(node);

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

                if (isAtomic || !childBacktracks)
                {
                    // If the capture is atomic and nothing can backtrack into it, we're done.
                    // Similarly, even if the capture isn't atomic, if the captured expression
                    // doesn't do any backtracking, we're done.
                    doneLabel = originalDoneLabel;
                }
                else
                {
                    // We're not atomic and the child node backtracks.  When it does, we need
                    // to ensure that the starting position for the capture is appropriately
                    // reset to what it was initially (it could have changed as part of being
                    // in a loop or similar).  So, we emit a backtracking section that
                    // pushes/pops the starting position before falling through.

                    if (isInLoop)
                    {
                        // If we're in a loop, different iterations of the loop need their own
                        // starting position, so push it on to the stack.  If we're not in a loop,
                        // the local will maintain its value and will suffice.

                        // if (stackpos + 1 >= base.runstack.Length) Array.Resize(ref base.runstack, base.runstack.Length * 2);
                        // base.runstack[stackpos++] = startingPos;
                        EmitStackResizeIfNeeded(1);
                        EmitStackPush(() => Ldloc(startingPos));
                    }

                    // Skip past the backtracking section
                    // goto backtrackingEnd;
                    Label backtrackingEnd = DefineLabel();
                    Br(backtrackingEnd);

                    // Emit a backtracking section that restores the capture's state and then jumps to the previous done label
                    Label backtrack = DefineLabel();
                    MarkLabel(backtrack);
                    if (isInLoop)
                    {
                        EmitStackPop();
                        Stloc(startingPos);
                    }

                    // goto doneLabel;
                    BrFar(doneLabel);

                    doneLabel = backtrack;
                    MarkLabel(backtrackingEnd);
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

            // Emits the code to handle a positive lookaround assertion. This is a positive lookahead
            // for left-to-right and a positive lookbehind for right-to-left.
            void EmitPositiveLookaroundAssertion(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.PositiveLookaround, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                if (analysis.HasRightToLeft)
                {
                    // Lookarounds are the only places in the node tree where we might change direction,
                    // i.e. where we might go from RegexOptions.None to RegexOptions.RightToLeft, or vice
                    // versa.  This is because lookbehinds are implemented by making the whole subgraph be
                    // RegexOptions.RightToLeft and reversed.  Since we use static position to optimize left-to-right
                    // and don't use it in support of right-to-left, we need to resync the static position
                    // to the current position when entering a lookaround, just in case we're changing direction.
                    TransferSliceStaticPosToPos(forceSliceReload: true);
                }

                // Save off pos.  We'll need to reset this upon successful completion of the lookaround.
                // startingPos = pos;
                LocalBuilder startingPos = DeclareInt32();
                Ldloc(pos);
                Stloc(startingPos);
                int startingTextSpanPos = sliceStaticPos;

                // Check for timeout. Lookarounds result in re-processing the same input, so while not
                // technically backtracking, it's appropriate to have a timeout check.
                EmitTimeoutCheckIfNeeded();

                // Emit the child.
                RegexNode child = node.Child(0);
                if (analysis.MayBacktrack(child))
                {
                    // Lookarounds are implicitly atomic, so we need to emit the node as atomic if it might backtrack.
                    EmitAtomic(node, null);
                }
                else
                {
                    EmitNode(child);
                }

                // After the child completes successfully, reset the text positions.
                // Do not reset captures, which persist beyond the lookaround.
                // pos = startingPos;
                // slice = inputSpan.Slice(pos);
                Ldloc(startingPos);
                Stloc(pos);
                SliceInputSpan();
                sliceStaticPos = startingTextSpanPos;
            }

            // Emits the code to handle a negative lookaround assertion. This is a negative lookahead
            // for left-to-right and a negative lookbehind for right-to-left.
            void EmitNegativeLookaroundAssertion(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.NegativeLookaround, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                if (analysis.HasRightToLeft)
                {
                    // Lookarounds are the only places in the node tree where we might change direction,
                    // i.e. where we might go from RegexOptions.None to RegexOptions.RightToLeft, or vice
                    // versa.  This is because lookbehinds are implemented by making the whole subgraph be
                    // RegexOptions.RightToLeft and reversed.  Since we use static position to optimize left-to-right
                    // and don't use it in support of right-to-left, we need to resync the static position
                    // to the current position when entering a lookaround, just in case we're changing direction.
                    TransferSliceStaticPosToPos(forceSliceReload: true);
                }

                Label originalDoneLabel = doneLabel;

                // Save off pos.  We'll need to reset this upon successful completion of the lookaround.
                // startingPos = pos;
                LocalBuilder startingPos = DeclareInt32();
                Ldloc(pos);
                Stloc(startingPos);
                int startingTextSpanPos = sliceStaticPos;

                Label negativeLookaheadDoneLabel = DefineLabel();
                doneLabel = negativeLookaheadDoneLabel;

                // Check for timeout. Lookarounds result in re-processing the same input, so while not
                // technically backtracking, it's appropriate to have a timeout check.
                EmitTimeoutCheckIfNeeded();

                // Emit the child.
                RegexNode child = node.Child(0);
                if (analysis.MayBacktrack(child))
                {
                    // Lookarounds are implicitly atomic, so we need to emit the node as atomic if it might backtrack.
                    EmitAtomic(node, null);
                }
                else
                {
                    EmitNode(child);
                }

                // If the generated code ends up here, it matched the lookaround, which actually
                // means failure for a _negative_ lookaround, so we need to jump to the original done.
                // goto originalDoneLabel;
                BrFar(originalDoneLabel);

                // Failures (success for a negative lookaround) jump here.
                MarkLabel(negativeLookaheadDoneLabel);
                if (doneLabel == negativeLookaheadDoneLabel)
                {
                    doneLabel = originalDoneLabel;
                }

                // After the child completes in failure (success for negative lookaround), reset the text positions.
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
                // Before we handle general-purpose matching logic for nodes, handle any special-casing.
                // -
                if (_regexTree!.FindOptimizations.FindMode == FindNextStartingPositionMode.LiteralAfterLoop_LeftToRight &&
                    _regexTree!.FindOptimizations.LiteralAfterLoop?.LoopNode == node)
                {
                    Debug.Assert(sliceStaticPos == 0, "This should be the first node and thus static position shouldn't have advanced.");

                    // pos = base.runtrackpos;
                    Mvfldloc(s_runtrackposField, pos);

                    SliceInputSpan();
                    return;
                }

                if (!StackHelper.TryEnsureSufficientExecutionStack())
                {
                    StackHelper.CallOnEmptyStack(EmitNode, node, subsequent, emitLengthChecksIfRequired);
                    return;
                }

                // RightToLeft doesn't take advantage of static positions.  While RightToLeft won't update static
                // positions, a previous operation may have left us with a non-zero one.  Make sure it's zero'd out
                // such that pos and slice are up-to-date.  Note that RightToLeft also shouldn't use the slice span,
                // as it's not kept up-to-date; any RightToLeft implementation that wants to use it must first update
                // it from pos.
                if ((node.Options & RegexOptions.RightToLeft) != 0)
                {
                    TransferSliceStaticPosToPos();
                }

                switch (node.Kind)
                {
                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Start:
                    case RegexNodeKind.Bol:
                    case RegexNodeKind.Eol:
                    case RegexNodeKind.End:
                    case RegexNodeKind.EndZ:
                        EmitAnchors(node);
                        return;

                    case RegexNodeKind.Boundary:
                    case RegexNodeKind.NonBoundary:
                    case RegexNodeKind.ECMABoundary:
                    case RegexNodeKind.NonECMABoundary:
                        EmitBoundary(node);
                        return;

                    case RegexNodeKind.Multi:
                        EmitMultiChar(node, emitLengthChecksIfRequired);
                        return;

                    case RegexNodeKind.One:
                    case RegexNodeKind.Notone:
                    case RegexNodeKind.Set:
                        EmitSingleChar(node, emitLengthChecksIfRequired);
                        return;

                    case RegexNodeKind.Oneloop:
                    case RegexNodeKind.Notoneloop:
                    case RegexNodeKind.Setloop:
                        EmitSingleCharLoop(node, subsequent, emitLengthChecksIfRequired);
                        return;

                    case RegexNodeKind.Onelazy:
                    case RegexNodeKind.Notonelazy:
                    case RegexNodeKind.Setlazy:
                        EmitSingleCharLazy(node, subsequent, emitLengthChecksIfRequired);
                        return;

                    case RegexNodeKind.Oneloopatomic:
                    case RegexNodeKind.Notoneloopatomic:
                    case RegexNodeKind.Setloopatomic:
                        EmitSingleCharAtomicLoop(node);
                        return;

                    case RegexNodeKind.Loop:
                        EmitLoop(node);
                        return;

                    case RegexNodeKind.Lazyloop:
                        EmitLazy(node);
                        return;

                    case RegexNodeKind.Alternate:
                        EmitAlternation(node);
                        return;

                    case RegexNodeKind.Concatenate:
                        EmitConcatenation(node, subsequent, emitLengthChecksIfRequired);
                        return;

                    case RegexNodeKind.Atomic:
                        EmitAtomic(node, subsequent);
                        return;

                    case RegexNodeKind.Backreference:
                        EmitBackreference(node);
                        return;

                    case RegexNodeKind.BackreferenceConditional:
                        EmitBackreferenceConditional(node);
                        return;

                    case RegexNodeKind.ExpressionConditional:
                        EmitExpressionConditional(node);
                        return;

                    case RegexNodeKind.Capture:
                        EmitCapture(node, subsequent);
                        return;

                    case RegexNodeKind.PositiveLookaround:
                        EmitPositiveLookaroundAssertion(node);
                        return;

                    case RegexNodeKind.NegativeLookaround:
                        EmitNegativeLookaroundAssertion(node);
                        return;

                    case RegexNodeKind.Nothing:
                        BrFar(doneLabel);
                        return;

                    case RegexNodeKind.Empty:
                        // Emit nothing.
                        return;

                    case RegexNodeKind.UpdateBumpalong:
                        EmitUpdateBumpalong(node);
                        return;
                }

                // All nodes should have been handled.
                Debug.Fail($"Unexpected node type: {node.Kind}");
            }

            // Emits the node for an atomic.
            void EmitAtomic(RegexNode node, RegexNode? subsequent)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Atomic or RegexNodeKind.PositiveLookaround or RegexNodeKind.NegativeLookaround, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                RegexNode child = node.Child(0);

                if (!analysis.MayBacktrack(child))
                {
                    // If the child has no backtracking, the atomic is a nop and we can just skip it.
                    // Note that the source generator equivalent for this is in the top-level EmitNode, in order to avoid
                    // outputting some extra comments and scopes.  As such formatting isn't a concern for the compiler,
                    // the logic is instead here in EmitAtomic.
                    EmitNode(child, subsequent);
                    return;
                }

                // Grab the current done label and the current backtracking position.  The purpose of the atomic node
                // is to ensure that nodes after it that might backtrack skip over the atomic, which means after
                // rendering the atomic's child, we need to reset the label so that subsequent backtracking doesn't
                // see any label left set by the atomic's child.  We also need to reset the backtracking stack position
                // so that the state on the stack remains consistent.
                Label originalDoneLabel = doneLabel;

                // int startingStackpos = stackpos;
                using RentedLocalBuilder startingStackpos = RentInt32Local();
                Ldloc(stackpos);
                Stloc(startingStackpos);

                // Emit the child.
                EmitNode(child, subsequent);

                // Reset the stack position and done label.
                // stackpos = startingStackpos;
                Ldloc(startingStackpos);
                Stloc(stackpos);
                doneLabel = originalDoneLabel;
            }

            // Emits the code to handle updating base.runtextpos to pos in response to
            // an UpdateBumpalong node.  This is used when we want to inform the scan loop that
            // it should bump from this location rather than from the original location.
            void EmitUpdateBumpalong(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.UpdateBumpalong, $"Unexpected type: {node.Kind}");

                // if (base.runtextpos < pos)
                // {
                //     base.runtextpos = pos;
                // }
                TransferSliceStaticPosToPos();
                Ldthisfld(s_runtextposField);
                Ldloc(pos);
                Label skipUpdate = DefineLabel();
                Bge(skipUpdate);
                Ldthis();
                Ldloc(pos);
                Stfld(s_runtextposField);
                MarkLabel(skipUpdate);
            }

            // Emits code for a concatenation
            void EmitConcatenation(RegexNode node, RegexNode? subsequent, bool emitLengthChecksIfRequired)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Concatenate, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.ChildCount() >= 2, $"Expected at least 2 children, found {node.ChildCount()}");

                // Emit the code for each child one after the other.
                int childCount = node.ChildCount();
                for (int i = 0; i < childCount; i++)
                {
                    // If we can find a subsequence of fixed-length children, we can emit a length check once for that sequence
                    // and then skip the individual length checks for each. We can also discover case-insensitive sequences that
                    // can be checked efficiently with methods like StartsWith.
                    if ((node.Options & RegexOptions.RightToLeft) == 0 &&
                        emitLengthChecksIfRequired &&
                        node.TryGetJoinableLengthCheckChildRange(i, out int requiredLength, out int exclusiveEnd))
                    {
                        EmitSpanLengthCheck(requiredLength);
                        for (; i < exclusiveEnd; i++)
                        {
                            if (node.TryGetOrdinalCaseInsensitiveString(i, exclusiveEnd, out int nodesConsumed, out string? caseInsensitiveString))
                            {
                                // if (!sliceSpan.Slice(sliceStaticPause).StartsWith(caseInsensitiveString, StringComparison.OrdinalIgnoreCase)) goto doneLabel;
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
                                Ldstr(caseInsensitiveString);
                                Call(s_stringAsSpanMethod);
                                Ldc((int)StringComparison.OrdinalIgnoreCase);
                                Call(s_spanStartsWithSpanComparison);
                                BrfalseFar(doneLabel);

                                sliceStaticPos += caseInsensitiveString.Length;
                                i += nodesConsumed - 1;
                                continue;
                            }

                            EmitNode(node.Child(i), GetSubsequent(i, node, subsequent), emitLengthChecksIfRequired: false);
                        }

                        i--;
                        continue;
                    }

                    EmitNode(node.Child(i), GetSubsequent(i, node, subsequent));
                }

                // Gets the node to treat as the subsequent one to node.Child(index)
                static RegexNode? GetSubsequent(int index, RegexNode node, RegexNode? subsequent)
                {
                    int childCount = node.ChildCount();
                    for (int i = index + 1; i < childCount; i++)
                    {
                        RegexNode next = node.Child(i);
                        if (next.Kind is not RegexNodeKind.UpdateBumpalong) // skip node types that don't have a semantic impact
                        {
                            return next;
                        }
                    }

                    return subsequent;
                }
            }

            // Emits the code to handle a single-character match.
            void EmitSingleChar(RegexNode node, bool emitLengthCheck = true, LocalBuilder? offset = null)
            {
                Debug.Assert(node.IsOneFamily || node.IsNotoneFamily || node.IsSetFamily, $"Unexpected type: {node.Kind}");

                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;
                Debug.Assert(!rtl || offset is null);

                if (emitLengthCheck)
                {
                    if (!rtl)
                    {
                        // if ((uint)(sliceStaticPos + offset) >= slice.Length) goto Done;
                        EmitSpanLengthCheck(1, offset);
                    }
                    else
                    {
                        // if ((uint)(pos - 1) >= inputSpan.Length) goto Done;
                        Ldloc(pos);
                        Ldc(1);
                        Sub();
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        BgeUnFar(doneLabel);
                    }
                }

                if (!rtl)
                {
                    // slice[staticPos + offset]
                    Ldloca(slice);
                    EmitSum(sliceStaticPos, offset);
                }
                else
                {
                    // inputSpan[pos - 1]
                    Ldloca(inputSpan);
                    EmitSum(-1, pos);
                }
                Call(s_spanGetItemMethod);
                LdindU2();

                // if (loadedChar != ch) goto doneLabel;
                if (node.IsSetFamily)
                {
                    EmitMatchCharacterClass(node.Str!);
                    BrfalseFar(doneLabel);
                }
                else
                {
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

                if (!rtl)
                {
                    sliceStaticPos++;
                }
                else
                {
                    // pos--;
                    Ldloc(pos);
                    Ldc(1);
                    Sub();
                    Stloc(pos);
                }
            }

            // Emits the code to handle a boundary check on a character.
            void EmitBoundary(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Boundary or RegexNodeKind.NonBoundary or RegexNodeKind.ECMABoundary or RegexNodeKind.NonECMABoundary, $"Unexpected type: {node.Kind}");

                if ((node.Options & RegexOptions.RightToLeft) != 0)
                {
                    // RightToLeft doesn't use static position.  This ensures it's 0.
                    TransferSliceStaticPosToPos();
                }

                // if (!IsBoundary(inputSpan, pos + sliceStaticPos)) goto doneLabel;
                Ldloc(inputSpan);
                Ldloc(pos);
                if (sliceStaticPos > 0)
                {
                    Ldc(sliceStaticPos);
                    Add();
                }
                switch (node.Kind)
                {
                    case RegexNodeKind.Boundary:
                        Call(s_isBoundaryMethod);
                        BrfalseFar(doneLabel);
                        break;

                    case RegexNodeKind.NonBoundary:
                        Call(s_isBoundaryMethod);
                        BrtrueFar(doneLabel);
                        break;

                    case RegexNodeKind.ECMABoundary:
                        Call(s_isECMABoundaryMethod);
                        BrfalseFar(doneLabel);
                        break;

                    default:
                        Debug.Assert(node.Kind == RegexNodeKind.NonECMABoundary);
                        Call(s_isECMABoundaryMethod);
                        BrtrueFar(doneLabel);
                        break;
                }
            }

            // Emits the code to handle various anchors.
            void EmitAnchors(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Beginning or RegexNodeKind.Start or RegexNodeKind.Bol or RegexNodeKind.End or RegexNodeKind.EndZ or RegexNodeKind.Eol, $"Unexpected type: {node.Kind}");
                Debug.Assert((node.Options & RegexOptions.RightToLeft) == 0 || sliceStaticPos == 0);
                Debug.Assert(sliceStaticPos >= 0);

                Debug.Assert(sliceStaticPos >= 0);
                switch (node.Kind)
                {
                    case RegexNodeKind.Beginning:
                    case RegexNodeKind.Start:
                        if (sliceStaticPos > 0)
                        {
                            // If we statically know we've already matched part of the regex, there's no way we're at the
                            // beginning or start, as we've already progressed past it.
                            BrFar(doneLabel);
                        }
                        else
                        {
                            // if (pos > 0/start) goto doneLabel;
                            Ldloc(pos);
                            if (node.Kind == RegexNodeKind.Beginning)
                            {
                                Ldc(0);
                            }
                            else
                            {
                                Ldthisfld(s_runtextstartField);
                            }
                            BneFar(doneLabel);
                        }
                        break;

                    case RegexNodeKind.Bol:
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
                            // We can't use our slice in this case, because we'd need to access slice[-1], so we access the inputSpan directly:
                            // if (pos > 0 && inputSpan[pos - 1] != '\n') goto doneLabel;
                            Label success = DefineLabel();
                            Ldloc(pos);
                            Ldc(0);
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

                    case RegexNodeKind.End:
                        if (sliceStaticPos > 0)
                        {
                            // if (sliceStaticPos < slice.Length) goto doneLabel;
                            Ldc(sliceStaticPos);
                            Ldloca(slice);
                        }
                        else
                        {
                            // if (pos < inputSpan.Length) goto doneLabel;
                            Ldloc(pos);
                            Ldloca(inputSpan);
                        }
                        Call(s_spanGetLengthMethod);
                        BltUnFar(doneLabel);
                        break;

                    case RegexNodeKind.EndZ:
                        if (sliceStaticPos > 0)
                        {
                            // if (sliceStaticPos < slice.Length - 1) goto doneLabel;
                            Ldc(sliceStaticPos);
                            Ldloca(slice);
                        }
                        else
                        {
                            // if (pos < inputSpan.Length - 1) goto doneLabel
                            Ldloc(pos);
                            Ldloca(inputSpan);
                        }
                        Call(s_spanGetLengthMethod);
                        Ldc(1);
                        Sub();
                        BltFar(doneLabel);
                        goto case RegexNodeKind.Eol;

                    case RegexNodeKind.Eol:
                        if (sliceStaticPos > 0)
                        {
                            // if (sliceStaticPos < slice.Length && slice[sliceStaticPos] != '\n') goto doneLabel;
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
                        else
                        {
                            // if ((uint)pos < (uint)inputSpan.Length && inputSpan[pos] != '\n') goto doneLabel;
                            Label success = DefineLabel();
                            Ldloc(pos);
                            Ldloca(inputSpan);
                            Call(s_spanGetLengthMethod);
                            BgeUn(success);
                            Ldloca(inputSpan);
                            Ldloc(pos);
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
            void EmitMultiChar(RegexNode node, bool emitLengthCheck)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Multi, $"Unexpected type: {node.Kind}");
                EmitMultiCharString(node.Str!, emitLengthCheck, (node.Options & RegexOptions.RightToLeft) != 0);
            }

            void EmitMultiCharString(string str, bool emitLengthCheck, bool rightToLeft)
            {
                Debug.Assert(str.Length >= 2);

                if (rightToLeft)
                {
                    Debug.Assert(emitLengthCheck);
                    TransferSliceStaticPosToPos();

                    // if ((uint)(pos - str.Length) >= inputSpan.Length) goto doneLabel;
                    Ldloc(pos);
                    Ldc(str.Length);
                    Sub();
                    Ldloca(inputSpan);
                    Call(s_spanGetLengthMethod);
                    BgeUnFar(doneLabel);

                    for (int i = str.Length - 1; i >= 0; i--)
                    {
                        // if (inputSpan[--pos] != str[str.Length - 1 - i]) goto doneLabel
                        Ldloc(pos);
                        Ldc(1);
                        Sub();
                        Stloc(pos);
                        Ldloca(inputSpan);
                        Ldloc(pos);
                        Call(s_spanGetItemMethod);
                        LdindU2();
                        Ldc(str[i]);
                        BneFar(doneLabel);
                    }

                    return;
                }

                Ldloca(slice);
                Ldc(sliceStaticPos);
                Call(s_spanSliceIntMethod);
                Ldstr(str);
                Call(s_stringAsSpanMethod);
                Call(s_spanStartsWithSpan);
                BrfalseFar(doneLabel);
                sliceStaticPos += str.Length;
            }

            // Emits the code to handle a backtracking, single-character loop.
            void EmitSingleCharLoop(RegexNode node, RegexNode? subsequent = null, bool emitLengthChecksIfRequired = true)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Notoneloop or RegexNodeKind.Setloop, $"Unexpected type: {node.Kind}");

                // If this is actually atomic based on its parent, emit it as atomic instead; no backtracking necessary.
                if (analysis.IsAtomicByAncestor(node))
                {
                    EmitSingleCharAtomicLoop(node);
                    return;
                }

                // If this is actually a repeater, emit that instead; no backtracking necessary.
                if (node.M == node.N)
                {
                    EmitSingleCharRepeater(node, emitLengthChecksIfRequired);
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
                LocalBuilder? capturePos = expressionHasCaptures ? DeclareInt32() : null;
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;
                bool isInLoop = analysis.IsInLoop(node);

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

                // int endingPos = pos;
                TransferSliceStaticPosToPos();
                Ldloc(pos);
                Stloc(endingPos);

                // startingPos += node.M; // or -= for rtl
                if (node.M > 0)
                {
                    Ldloc(startingPos);
                    Ldc(!rtl ? node.M : -node.M);
                    Add();
                    Stloc(startingPos);
                }

                // goto endLoop;
                BrFar(endLoop);

                // Backtracking section. Subsequent failures will jump to here, at which
                // point we decrement the matched count as long as it's above the minimum
                // required, and try again by flowing to everything that comes after this.

                MarkLabel(backtrackingLabel);
                if (isInLoop)
                {
                    // This loop is inside of another loop, which means we persist state
                    // on the backtracking stack rather than relying on locals to always
                    // hold the right state (if we didn't do that, another iteration of the
                    // outer loop could have resulted in the locals being overwritten).
                    // Pop the relevant state from the stack.

                    if (capturePos is not null)
                    {
                        // Note that this differs ever so slightly from the source generator.  The source
                        // generator only defines a local for capturePos if not in a loop, but the compiler
                        // needs to store the popped stack value somewhere so that it can repeatedly compare
                        // that value against Crawlpos, so capturePos is always declared if there are captures.

                        // capturepos = base.runstack[--stackpos];
                        // while (base.Crawlpos() > capturepos) base.Uncapture();
                        EmitStackPop();
                        Stloc(capturePos);
                        EmitUncaptureUntil(capturePos);
                    }

                    // endingPos = base.runstack[--stackpos];
                    // startingPos = base.runstack[--stackpos];
                    EmitStackPop();
                    Stloc(endingPos);
                    EmitStackPop();
                    Stloc(startingPos);
                }
                else if (capturePos is not null)
                {
                    // Since we're not in a loop, we're using a local to track the crawl position.
                    // Unwind back to the position we were at prior to running the code after this loop.
                    EmitUncaptureUntil(capturePos);
                }

                // We're backtracking.  Check the timeout.
                EmitTimeoutCheckIfNeeded();

                // if (startingPos >= endingPos) goto doneLabel; // or <= for rtl
                Ldloc(startingPos);
                Ldloc(endingPos);
                if (!rtl)
                {
                    BgeFar(doneLabel);
                }
                else
                {
                    BleFar(doneLabel);
                }

                if (!rtl && subsequent?.FindStartingLiteral() is ValueTuple<char, string?, string?> literal)
                {
                    // endingPos = inputSpan.Slice(startingPos, Math.Min(inputSpan.Length, endingPos + literal.Length - 1) - startingPos).LastIndexOf(literal);
                    // if (endingPos < 0)
                    // {
                    //     goto doneLabel;
                    // }
                    Ldloca(inputSpan);
                    Ldloc(startingPos);
                    if (literal.Item2 is not null)
                    {
                        Ldloca(inputSpan);
                        Call(s_spanGetLengthMethod);
                        Ldloc(endingPos);
                        Ldc(literal.Item2.Length - 1);
                        Add();
                        Call(s_mathMinIntInt);
                        Ldloc(startingPos);
                        Sub();
                        Call(s_spanSliceIntIntMethod);
                        Ldstr(literal.Item2);
                        Call(s_stringAsSpanMethod);
                        Call(s_spanLastIndexOfSpan);
                    }
                    else
                    {
                        Ldloc(endingPos);
                        Ldloc(startingPos);
                        Sub();
                        Call(s_spanSliceIntIntMethod);
                        if (literal.Item3 is not null)
                        {
                            switch (literal.Item3.Length)
                            {
                                case 2:
                                    Ldc(literal.Item3[0]);
                                    Ldc(literal.Item3[1]);
                                    Call(s_spanLastIndexOfAnyCharChar);
                                    break;

                                case 3:
                                    Ldc(literal.Item3[0]);
                                    Ldc(literal.Item3[1]);
                                    Ldc(literal.Item3[2]);
                                    Call(s_spanLastIndexOfAnyCharCharChar);
                                    break;

                                default:
                                    Ldstr(literal.Item3);
                                    Call(s_stringAsSpanMethod);
                                    Call(s_spanLastIndexOfAnySpan);
                                    break;
                            }
                        }
                        else
                        {
                            Ldc(literal.Item1);
                            Call(s_spanLastIndexOfChar);
                        }
                    }
                    Stloc(endingPos);
                    Ldloc(endingPos);
                    Ldc(0);
                    BltFar(doneLabel);

                    // endingPos += startingPos;
                    Ldloc(endingPos);
                    Ldloc(startingPos);
                    Add();
                    Stloc(endingPos);
                }
                else
                {
                    // endingPos--; // or ++ for rtl
                    Ldloc(endingPos);
                    Ldc(!rtl ? 1 : -1);
                    Sub();
                    Stloc(endingPos);
                }

                // pos = endingPos;
                Ldloc(endingPos);
                Stloc(pos);

                if (!rtl)
                {
                    // slice = inputSpan.Slice(pos);
                    SliceInputSpan();
                }

                MarkLabel(endLoop);
                if (isInLoop)
                {
                    // We're in a loop and thus can't rely on locals correctly holding the state we
                    // need (the locals could be overwritten by a subsequent iteration).  Push the state
                    // on to the backtracking stack.
                    EmitStackResizeIfNeeded(2 + (capturePos is not null ? 1 : 0));
                    EmitStackPush(() => Ldloc(startingPos));
                    EmitStackPush(() => Ldloc(endingPos));
                    if (capturePos is not null)
                    {
                        EmitStackPush(() =>
                        {
                            // base.Crawlpos();
                            Ldthis();
                            Call(s_crawlposMethod);
                        });
                    }
                }
                else if (capturePos is not null)
                {
                    // We're not in a loop and so can trust our locals.  Store the current capture position
                    // into the capture position local; we'll uncapture back to this when backtracking to
                    // remove any captures from after this loop that we need to throw away.

                    // capturePos = base.Crawlpos();
                    Ldthis();
                    Call(s_crawlposMethod);
                    Stloc(capturePos);
                }

                doneLabel = backtrackingLabel; // leave set to the backtracking label for all subsequent nodes
            }

            void EmitSingleCharLazy(RegexNode node, RegexNode? subsequent = null, bool emitLengthChecksIfRequired = true)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Onelazy or RegexNodeKind.Notonelazy or RegexNodeKind.Setlazy, $"Unexpected type: {node.Kind}");

                // Emit the min iterations as a repeater.  Any failures here don't necessitate backtracking,
                // as the lazy itself failed to match, and there's no backtracking possible by the individual
                // characters/iterations themselves.
                if (node.M > 0)
                {
                    EmitSingleCharRepeater(node, emitLengthChecksIfRequired);
                }

                // If the whole thing was actually that repeater, we're done. Similarly, if this is actually an atomic
                // lazy loop, nothing will ever backtrack into this node, so we never need to iterate more than the minimum.
                if (node.M == node.N || analysis.IsAtomicByAncestor(node))
                {
                    return;
                }

                Debug.Assert(node.M < node.N);
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;

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

                // We're backtracking.  Check the timeout.
                EmitTimeoutCheckIfNeeded();

                // Now match the next item in the lazy loop.  We need to reset the pos to the position
                // just after the last character in this loop was matched, and we need to store the resulting position
                // for the next time we backtrack.
                // pos = startingPos;
                // Match single char;
                Ldloc(startingPos);
                Stloc(pos);
                SliceInputSpan();
                EmitSingleChar(node);
                TransferSliceStaticPosToPos();

                // Now that we've appropriately advanced by one character and are set for what comes after the loop,
                // see if we can skip ahead more iterations by doing a search for a following literal.
                if (!rtl &&
                    iterationCount is null &&
                    node.Kind is RegexNodeKind.Notonelazy &&
                    subsequent?.FindStartingLiteral(4) is ValueTuple<char, string?, string?> literal) // 5 == max optimized by IndexOfAny, and we need to reserve 1 for node.Ch
                {
                    // e.g. "<[^>]*?>"

                    // Whether the not'd character matches the subsequent literal. This impacts whether we need to search
                    // for both or just the literal, as well as what assumptions we can make once a match is found.
                    bool overlap;

                    // This lazy loop will consume all characters other than node.Ch until the subsequent literal.
                    // We can implement it to search for either that char or the literal, whichever comes first.
                    Ldloc(slice);
                    if (literal.Item2 is not null) // string literal
                    {
                        overlap = literal.Item2[0] == node.Ch;
                        if (overlap)
                        {
                            // startingPos = slice.IndexOf(node.Ch);
                            Ldc(node.Ch);
                            Call(s_spanIndexOfChar);
                        }
                        else
                        {
                            // startingPos = slice.IndexOfAny(node.Ch, literal.Item2[0]);
                            Ldc(node.Ch);
                            Ldc(literal.Item2[0]);
                            Call(s_spanIndexOfAnyCharChar);
                        }
                    }
                    else if (literal.Item3 is null) // char literal
                    {
                        overlap = literal.Item1 == node.Ch;
                        if (overlap)
                        {
                            // startingPos = slice.IndexOf(node.Ch);
                            Ldc(node.Ch);
                            Call(s_spanIndexOfChar);
                        }
                        else
                        {
                            // startingPos = slice.IndexOfAny(node.Ch, literal.Item1);
                            Ldc(node.Ch);
                            Ldc(literal.Item1);
                            Call(s_spanIndexOfAnyCharChar);
                        }
                    }
                    else // set literal
                    {
                        overlap = literal.Item3.Contains(node.Ch);
                        switch ((overlap, literal.Item3.Length))
                        {
                            case (true, 2):
                                // startingPos = slice.IndexOfAny(literal.Item3[0], literal.Item3[1]);
                                Ldc(literal.Item3[0]);
                                Ldc(literal.Item3[1]);
                                Call(s_spanIndexOfAnyCharChar);
                                break;

                            case (true, 3):
                                // startingPos = slice.IndexOfAny(literal.Item3[0], literal.Item3[1], literal.Item3[2]);
                                Ldc(literal.Item3[0]);
                                Ldc(literal.Item3[1]);
                                Ldc(literal.Item3[2]);
                                Call(s_spanIndexOfAnyCharCharChar);
                                break;

                            case (true, _):
                                // startingPos = slice.IndexOfAny(literal.Item3);
                                Ldstr(literal.Item3);
                                Call(s_stringAsSpanMethod);
                                Call(s_spanIndexOfAnySpan);
                                break;

                            case (false, 2):
                                // startingPos = slice.IndexOfAny(node.Ch, literal.Item3[0], literal.Item3[1]);
                                Ldc(node.Ch);
                                Ldc(literal.Item3[0]);
                                Ldc(literal.Item3[1]);
                                Call(s_spanIndexOfAnyCharCharChar);
                                break;

                            case (false, _):
                                // startingPos = slice.IndexOfAny($"{node.Ch}{literal.Item3}");
                                Ldstr($"{node.Ch}{literal.Item3}");
                                Call(s_stringAsSpanMethod);
                                Call(s_spanIndexOfAnySpan);
                                break;
                        }
                    }
                    Stloc(startingPos);

                    // If the search didn't find anything, fail the match.  If it did find something, then we need to consider whether
                    // that something is the loop character.  If it's not, we've successfully backtracked to the next lazy location
                    // where we should evaluate the rest of the pattern.  If it does match, then we need to consider whether there's
                    // overlap between the loop character and the literal.  If there is overlap, this is also a place to check.  But
                    // if there's not overlap, and if the found character is the loop character, we also want to fail the match here
                    // and now, as this means the loop ends before it gets to what needs to come after the loop, and thus the pattern
                    // can't possibly match here.
                    if (overlap)
                    {
                        // if (startingPos < 0) goto doneLabel;
                        Ldloc(startingPos);
                        Ldc(0);
                        BltFar(doneLabel);
                    }
                    else
                    {
                        // if ((uint)startingPos >= (uint)slice.Length) goto doneLabel;
                        Ldloc(startingPos);
                        Ldloca(slice);
                        Call(s_spanGetLengthMethod);
                        BgeUnFar(doneLabel);

                        // if (slice[startingPos] == node.Ch) goto doneLabel;
                        Ldloca(slice);
                        Ldloc(startingPos);
                        Call(s_spanGetItemMethod);
                        LdindU2();
                        Ldc(node.Ch);
                        BeqFar(doneLabel);
                    }

                    // pos += startingPos;
                    // slice = inputSpace.Slice(pos);
                    Ldloc(pos);
                    Ldloc(startingPos);
                    Add();
                    Stloc(pos);
                    SliceInputSpan();
                }
                else if (!rtl &&
                    iterationCount is null &&
                    node.Kind is RegexNodeKind.Setlazy &&
                    node.Str == RegexCharClass.AnyClass &&
                    subsequent?.FindStartingLiteral() is ValueTuple<char, string?, string?> literal2)
                {
                    // e.g. ".*?string" with RegexOptions.Singleline
                    // This lazy loop will consume all characters until the subsequent literal. If the subsequent literal
                    // isn't found, the loop fails. We can implement it to just search for that literal.

                    // startingPos = slice.IndexOf(literal);
                    Ldloc(slice);
                    if (literal2.Item2 is not null)
                    {
                        Ldstr(literal2.Item2);
                        Call(s_stringAsSpanMethod);
                        Call(s_spanIndexOfSpan);
                    }
                    else if (literal2.Item3 is not null)
                    {
                        switch (literal2.Item3.Length)
                        {
                            case 2:
                                Ldc(literal2.Item3[0]);
                                Ldc(literal2.Item3[1]);
                                Call(s_spanIndexOfAnyCharChar);
                                break;

                            case 3:
                                Ldc(literal2.Item3[0]);
                                Ldc(literal2.Item3[1]);
                                Ldc(literal2.Item3[2]);
                                Call(s_spanIndexOfAnyCharCharChar);
                                break;

                            default:
                                Ldstr(literal2.Item3);
                                Call(s_stringAsSpanMethod);
                                Call(s_spanIndexOfAnySpan);
                                break;
                        }
                    }
                    else
                    {
                        Ldc(literal2.Item1);
                        Call(s_spanIndexOfChar);
                    }
                    Stloc(startingPos);

                    // if (startingPos < 0) goto doneLabel;
                    Ldloc(startingPos);
                    Ldc(0);
                    BltFar(doneLabel);

                    // pos += startingPos;
                    // slice = inputSpace.Slice(pos);
                    Ldloc(pos);
                    Ldloc(startingPos);
                    Add();
                    Stloc(pos);
                    SliceInputSpan();
                }

                // Store the position we've left off at in case we need to iterate again.
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

                // If this loop is itself not in another loop, nothing more needs to be done:
                // upon backtracking, locals being used by this loop will have retained their
                // values and be up-to-date.  But if this loop is inside another loop, multiple
                // iterations of this loop each need their own state, so we need to use the stack
                // to hold it, and we need a dedicated backtracking section to handle restoring
                // that state before jumping back into the loop itself.
                if (analysis.IsInLoop(node))
                {
                    // Store the loop's state
                    // base.runstack[stackpos++] = startingPos;
                    // base.runstack[stackpos++] = capturepos;
                    // base.runstack[stackpos++] = iterationCount;
                    EmitStackResizeIfNeeded(1 + (capturepos is not null ? 1 : 0) + (iterationCount is not null ? 1 : 0));
                    EmitStackPush(() => Ldloc(startingPos));
                    if (capturepos is not null)
                    {
                        EmitStackPush(() => Ldloc(capturepos));
                    }
                    if (iterationCount is not null)
                    {
                        EmitStackPush(() => Ldloc(iterationCount));
                    }

                    // Skip past the backtracking section.
                    Label backtrackingEnd = DefineLabel();
                    BrFar(backtrackingEnd);

                    // Emit a backtracking section that restores the loop's state and then jumps to the previous done label.
                    Label backtrack = DefineLabel();
                    MarkLabel(backtrack);

                    // Restore the loop's state.
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
                Debug.Assert(node.Kind is RegexNodeKind.Lazyloop, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.M < int.MaxValue, $"Unexpected M={node.M}");
                Debug.Assert(node.N >= node.M, $"Unexpected M={node.M}, N={node.N}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");

                RegexNode child = node.Child(0);
                int minIterations = node.M;
                int maxIterations = node.N;
                Label originalDoneLabel = doneLabel;
                bool isAtomic = analysis.IsAtomicByAncestor(node);

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
                            EmitNode(child);
                            return;
                    }
                }

                // If this is actually a repeater and the child doesn't have any backtracking in it that might
                // cause us to need to unwind already taken iterations, just output it as a repeater loop.
                if (minIterations == maxIterations && !analysis.MayBacktrack(child))
                {
                    EmitNonBacktrackingRepeater(node);
                    return;
                }

                // We might loop any number of times.  In order to ensure this loop and subsequent code sees sliceStaticPos
                // the same regardless, we always need it to contain the same value, and the easiest such value is 0.
                // So, we transfer sliceStaticPos to pos, and ensure that any path out of here has sliceStaticPos as 0.
                TransferSliceStaticPosToPos();

                Label body = DefineLabel();
                Label endLoop = DefineLabel();

                // iterationCount = 0;
                LocalBuilder iterationCount = DeclareInt32();
                Ldc(0);
                Stloc(iterationCount);

                // startingPos = pos;
                // sawEmpty = 0; // false
                bool iterationMayBeEmpty = child.ComputeMinLength() == 0;
                LocalBuilder? startingPos = null;
                LocalBuilder? sawEmpty = null;
                if (iterationMayBeEmpty)
                {
                    startingPos = DeclareInt32();
                    Ldloc(pos);
                    Stloc(startingPos);

                    sawEmpty = DeclareInt32();
                    Ldc(0);
                    Stloc(sawEmpty);
                }

                // If the min count is 0, start out by jumping right to what's after the loop.  Backtracking
                // will then bring us back in to do further iterations.
                if (minIterations == 0)
                {
                    // goto endLoop;
                    BrFar(endLoop);
                }

                // Iteration body
                MarkLabel(body);

                // In case iterations are backtracked through and unwound, we need to store the current position (so that
                // matching can resume from that location), the current crawl position if captures are possible (so that
                // we can uncapture back to that position), and both the starting position from the iteration we're leaving
                // and whether we've seen an empty iteration (if iterations may be empty).  Since there can be multiple
                // iterations, this state needs to be stored on to the backtracking stack.
                // base.runstack[stackpos++] = pos;
                // base.runstack[stackpos++] = startingPos;
                // base.runstack[stackpos++] = sawEmpty;
                // base.runstack[stackpos++] = base.Crawlpos();
                int entriesPerIteration = 1/*pos*/ + (iterationMayBeEmpty ? 2/*startingPos+sawEmpty*/ : 0) + (expressionHasCaptures ? 1/*Crawlpos*/ : 0);
                EmitStackResizeIfNeeded(entriesPerIteration);
                EmitStackPush(() => Ldloc(pos));
                if (iterationMayBeEmpty)
                {
                    EmitStackPush(() => Ldloc(startingPos!));
                    EmitStackPush(() => Ldloc(sawEmpty!));
                }
                if (expressionHasCaptures)
                {
                    EmitStackPush(() => { Ldthis(); Call(s_crawlposMethod); });
                }

                if (iterationMayBeEmpty)
                {
                    // We need to store the current pos so we can compare it against pos after the iteration, in order to
                    // determine whether the iteration was empty.
                    // startingPos = pos;
                    Ldloc(pos);
                    Stloc(startingPos!);
                }

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
                EmitNode(child);
                TransferSliceStaticPosToPos(); // ensure sliceStaticPos remains 0
                if (doneLabel == iterationFailedLabel)
                {
                    doneLabel = originalDoneLabel;
                }

                // Loop condition.  Continue iterating if we've not yet reached the minimum.  We just successfully
                // matched an iteration, so the only reason we'd need to forcefully loop around again is if the
                // minimum were at least 2.
                if (minIterations >= 2)
                {
                    // if (iterationCount < minIterations) goto body;
                    Ldloc(iterationCount);
                    Ldc(minIterations);
                    BltFar(body);
                }

                if (iterationMayBeEmpty)
                {
                    // If the last iteration was empty, we need to prevent further iteration from this point
                    // unless we backtrack out of this iteration.
                    // if (pos == startingPos) sawEmpty = 1; // true
                    Label skipSawEmptySet = DefineLabel();
                    Ldloc(pos);
                    Ldloc(startingPos!);
                    Bne(skipSawEmptySet);
                    Ldc(1);
                    Stloc(sawEmpty!);
                    MarkLabel(skipSawEmptySet);
                }

                // We matched the next iteration.  Jump to the subsequent code.
                // goto endLoop;
                BrFar(endLoop);

                // Now handle what happens when an iteration fails (and since a lazy loop only executes an iteration
                // when it's required to satisfy the loop by definition of being lazy, the loop is failing).  We need
                // to reset state to what it was before just that iteration started.  That includes resetting pos and
                // clearing out any captures from that iteration.
                MarkLabel(iterationFailedLabel);

                // Fail this loop iteration, including popping state off the backtracking stack that was pushed
                // on as part of the failing iteration.

                // iterationCount--;
                Ldloc(iterationCount);
                Ldc(1);
                Sub();
                Stloc(iterationCount);

                if (expressionHasCaptures)
                {
                    // capturepos = base.runstack[--stackpos];
                    // while (base.Crawlpos() > capturepos) base.Uncapture();
                    using RentedLocalBuilder poppedCrawlPos = RentInt32Local();
                    EmitStackPop();
                    Stloc(poppedCrawlPos);
                    EmitUncaptureUntil(poppedCrawlPos);
                }

                // sawEmpty = base.runstack[--stackpos];
                // startingPos = base.runstack[--stackpos];
                // pos = base.runstack[--stackpos];
                // slice = inputSpan.Slice(pos);
                if (iterationMayBeEmpty)
                {
                    EmitStackPop();
                    Stloc(sawEmpty!);
                    EmitStackPop();
                    Stloc(startingPos!);
                }
                EmitStackPop();
                Stloc(pos);
                SliceInputSpan();

                // If the loop's child doesn't backtrack, then this loop has failed.
                // If the loop's child does backtrack, we need to backtrack back into the previous iteration if there was one.
                if (doneLabel == originalDoneLabel)
                {
                    // Since the only reason we'd end up revisiting previous iterations of the lazy loop is if the child had backtracking constructs
                    // we'd backtrack into, and the child doesn't, the whole loop is failed and done. If we successfully processed any iterations,
                    // we thus need to pop all of the state we pushed onto the stack for those iterations, as we're exiting out to the parent who
                    // will expect the stack to be cleared of any child state.

                    // stackpos -= iterationCount * entriesPerIteration;
                    Debug.Assert(entriesPerIteration >= 1);
                    Ldloc(stackpos);
                    Ldloc(iterationCount);
                    if (entriesPerIteration > 1)
                    {
                        Ldc(entriesPerIteration);
                        Mul();
                    }
                    Sub();
                    Stloc(stackpos);
                }
                else
                {
                    // The child has backtracking constructs.  If we have no successful iterations previously processed, just bail.
                    // If we do have successful iterations previously processed, however, we need to backtrack back into the last one.

                    // if (iterationCount != 0) goto doneLabel;
                    Ldloc(iterationCount);
                    Ldc(0);
                    BneFar(doneLabel);
                }

                // goto originalDoneLabel;
                BrFar(originalDoneLabel);

                MarkLabel(endLoop);

                // If the lazy loop is not atomic, then subsequent code may backtrack back into this lazy loop, either
                // causing it to add additional iterations, or backtracking into existing iterations and potentially
                // unwinding them.  We need to do a timeout check, and then determine whether to branch back to add more
                // iterations (if we haven't hit the loop's maximum iteration count and haven't seen an empty iteration)
                // or unwind by branching back to the last backtracking location.  Either way, we need a dedicated
                // backtracking section that a subsequent construct will see as its backtracking target.
                if (!isAtomic)
                {
                    // We need to ensure that some state (e.g. iteration count) is persisted if we're backtracked to.
                    // If we're not inside of a loop, the local's used for this construct are sufficient, as nothing
                    // else will overwrite them between now and when backtracking occurs.  If, however, we are inside
                    // of another loop, then any number of iterations might have such state that needs to be stored,
                    // and thus it needs to be pushed on to the backtracking stack.
                    if (analysis.IsInLoop(node))
                    {
                        EmitStackResizeIfNeeded(1 + (iterationMayBeEmpty ? 2 : 0));
                        EmitStackPush(() => Ldloc(iterationCount));
                        if (iterationMayBeEmpty)
                        {
                            EmitStackPush(() => Ldloc(startingPos!));
                            EmitStackPush(() => Ldloc(sawEmpty!));
                        }
                    }

                    Label skipBacktrack = DefineLabel();
                    BrFar(skipBacktrack);

                    // Emit a backtracking section that checks the timeout, restores the loop's state, and jumps to
                    // the appropriate label.
                    Label backtrack = DefineLabel();
                    MarkLabel(backtrack);

                    // We're backtracking.  Check the timeout.
                    EmitTimeoutCheckIfNeeded();

                    if (analysis.IsInLoop(node))
                    {
                        // sawEmpty = base.runstack[--stackpos];
                        // startingPos = base.runstack[--stackpos];
                        // iterationCount = base.runstack[--stackpos];
                        if (iterationMayBeEmpty)
                        {
                            EmitStackPop();
                            Stloc(sawEmpty!);
                            EmitStackPop();
                            Stloc(startingPos!);
                        }
                        EmitStackPop();
                        Stloc(iterationCount);
                    }

                    // Determine where to branch, either back to the lazy loop body to add an additional iteration,
                    // or to the last backtracking label.

                    if (iterationMayBeEmpty)
                    {
                        // if (sawEmpty != 0) goto doneLabel;
                        Ldloc(sawEmpty!);
                        Ldc(0);
                        BneFar(doneLabel);
                    }

                    if (maxIterations != int.MaxValue)
                    {
                        // if (iterationCount >= maxIterations) goto doneLabel;
                        Ldloc(iterationCount);
                        Ldc(maxIterations);
                        BgeFar(doneLabel);
                    }

                    // goto body;
                    BrFar(body);

                    doneLabel = backtrack;
                    MarkLabel(skipBacktrack);
                }
            }

            // Emits the code to handle a loop (repeater) with a fixed number of iterations.
            // RegexNode.M is used for the number of iterations (RegexNode.N is ignored), as this
            // might be used to implement the required iterations of other kinds of loops.
            void EmitSingleCharRepeater(RegexNode node, bool emitLengthChecksIfRequired = true)
            {
                Debug.Assert(node.IsOneFamily || node.IsNotoneFamily || node.IsSetFamily, $"Unexpected type: {node.Kind}");

                int iterations = node.M;
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;

                switch (iterations)
                {
                    case 0:
                        // No iterations, nothing to do.
                        return;

                    case 1:
                        // Just match the individual item
                        EmitSingleChar(node, emitLengthChecksIfRequired);
                        return;

                    case <= RegexNode.MultiVsRepeaterLimit when node.IsOneFamily:
                        // This is a repeated case-sensitive character; emit it as a multi in order to get all the optimizations
                        // afforded to a multi, e.g. unrolling the loop with multi-char reads/comparisons at a time.
                        EmitMultiCharString(new string(node.Ch, iterations), emitLengthChecksIfRequired, rtl);
                        return;
                }

                if (rtl)
                {
                    TransferSliceStaticPosToPos(); // we don't use static position with rtl
                    Label conditionLabel = DefineLabel();
                    Label bodyLabel = DefineLabel();

                    // for (int i = 0; ...)
                    using RentedLocalBuilder iterationLocal = RentInt32Local();
                    Ldc(0);
                    Stloc(iterationLocal);
                    BrFar(conditionLabel);

                    // TimeoutCheck();
                    // HandleSingleChar();
                    MarkLabel(bodyLabel);
                    EmitSingleChar(node);

                    // for (...; ...; i++)
                    Ldloc(iterationLocal);
                    Ldc(1);
                    Add();
                    Stloc(iterationLocal);

                    // for (...; i < iterations; ...)
                    MarkLabel(conditionLabel);
                    Ldloc(iterationLocal);
                    Ldc(iterations);
                    BltFar(bodyLabel);

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
                Debug.Assert(node.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic, $"Unexpected type: {node.Kind}");

                // If this is actually a repeater, emit that instead.
                if (node.M == node.N)
                {
                    EmitSingleCharRepeater(node);
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
                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;

                using RentedLocalBuilder iterationLocal = RentInt32Local();

                Label atomicLoopDoneLabel = DefineLabel();

                Span<char> setChars = stackalloc char[5]; // max optimized by IndexOfAny today
                int numSetChars = 0;

                if (rtl)
                {
                    TransferSliceStaticPosToPos(); // we don't use static position for rtl

                    Label conditionLabel = DefineLabel();
                    Label bodyLabel = DefineLabel();

                    // int i = 0;
                    Ldc(0);
                    Stloc(iterationLocal);
                    BrFar(conditionLabel);

                    // Body:
                    // TimeoutCheck();
                    MarkLabel(bodyLabel);

                    // if (pos <= iterationLocal) goto atomicLoopDoneLabel;
                    Ldloc(pos);
                    Ldloc(iterationLocal);
                    BleFar(atomicLoopDoneLabel);

                    // if (inputSpan[pos - i - 1] != ch) goto atomicLoopDoneLabel;
                    Ldloca(inputSpan);
                    Ldloc(pos);
                    Ldloc(iterationLocal);
                    Sub();
                    Ldc(1);
                    Sub();
                    Call(s_spanGetItemMethod);
                    LdindU2();
                    if (node.IsSetFamily)
                    {
                        EmitMatchCharacterClass(node.Str!);
                        BrfalseFar(atomicLoopDoneLabel);
                    }
                    else
                    {
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
                else if (node.IsNotoneFamily &&
                    maxIterations == int.MaxValue)
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
                            Call(s_spanIndexOfAnySpan);
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

                    // int i = inputSpan.Length - pos;
                    TransferSliceStaticPosToPos();
                    Ldloca(inputSpan);
                    Call(s_spanGetLengthMethod);
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
                        EmitMatchCharacterClass(node.Str!);
                        BrfalseFar(atomicLoopDoneLabel);
                    }
                    else
                    {
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

                if (!rtl)
                {
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
                else
                {
                    // pos -= i;
                    Ldloc(pos);
                    Ldloc(iterationLocal);
                    Sub();
                    Stloc(pos);
                }
            }

            // Emits the code to handle a non-backtracking optional zero-or-one loop.
            void EmitAtomicSingleCharZeroOrOne(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Oneloop or RegexNodeKind.Oneloopatomic or RegexNodeKind.Notoneloop or RegexNodeKind.Notoneloopatomic or RegexNodeKind.Setloop or RegexNodeKind.Setloopatomic, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.M == 0 && node.N == 1);

                bool rtl = (node.Options & RegexOptions.RightToLeft) != 0;
                if (rtl)
                {
                    TransferSliceStaticPosToPos(); // we don't use static pos for rtl
                }

                Label skipUpdatesLabel = DefineLabel();

                if (!rtl)
                {
                    // if ((uint)sliceStaticPos >= (uint)slice.Length) goto skipUpdatesLabel;
                    Ldc(sliceStaticPos);
                    Ldloca(slice);
                    Call(s_spanGetLengthMethod);
                    BgeUnFar(skipUpdatesLabel);
                }
                else
                {
                    // if (pos == 0) goto skipUpdatesLabel;
                    Ldloc(pos);
                    Ldc(0);
                    BeqFar(skipUpdatesLabel);
                }

                if (!rtl)
                {
                    // if (slice[sliceStaticPos] != ch) goto skipUpdatesLabel;
                    Ldloca(slice);
                    Ldc(sliceStaticPos);
                }
                else
                {
                    // if (inputSpan[pos - 1] != ch) goto skipUpdatesLabel;
                    Ldloca(inputSpan);
                    Ldloc(pos);
                    Ldc(1);
                    Sub();
                }
                Call(s_spanGetItemMethod);
                LdindU2();
                if (node.IsSetFamily)
                {
                    EmitMatchCharacterClass(node.Str!);
                    BrfalseFar(skipUpdatesLabel);
                }
                else
                {
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

                if (!rtl)
                {
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
                }
                else
                {
                    // pos--;
                    Ldloc(pos);
                    Ldc(1);
                    Sub();
                    Stloc(pos);
                }

                MarkLabel(skipUpdatesLabel);
            }

            void EmitNonBacktrackingRepeater(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Loop or RegexNodeKind.Lazyloop, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.M < int.MaxValue, $"Unexpected M={node.M}");
                Debug.Assert(node.M == node.N, $"Unexpected M={node.M} == N={node.N}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");
                Debug.Assert(!analysis.MayBacktrack(node.Child(0)), $"Expected non-backtracking node {node.Kind}");

                // Ensure every iteration of the loop sees a consistent value.
                TransferSliceStaticPosToPos();

                // Loop M==N times to match the child exactly that numbers of times.
                Label condition = DefineLabel();
                Label body = DefineLabel();

                // for (int i = 0; ...)
                using RentedLocalBuilder i = RentInt32Local();
                Ldc(0);
                Stloc(i);
                BrFar(condition);

                MarkLabel(body);
                EmitNode(node.Child(0));
                TransferSliceStaticPosToPos(); // make sure static the static position remains at 0 for subsequent constructs

                // for (...; ...; i++)
                Ldloc(i);
                Ldc(1);
                Add();
                Stloc(i);

                // for (...; i < node.M; ...)
                MarkLabel(condition);
                Ldloc(i);
                Ldc(node.M);
                BltFar(body);
            }

            void EmitLoop(RegexNode node)
            {
                Debug.Assert(node.Kind is RegexNodeKind.Loop or RegexNodeKind.Lazyloop, $"Unexpected type: {node.Kind}");
                Debug.Assert(node.M < int.MaxValue, $"Unexpected M={node.M}");
                Debug.Assert(node.N >= node.M, $"Unexpected M={node.M}, N={node.N}");
                Debug.Assert(node.ChildCount() == 1, $"Expected 1 child, found {node.ChildCount()}");
                RegexNode child = node.Child(0);

                int minIterations = node.M;
                int maxIterations = node.N;
                bool isAtomic = analysis.IsAtomicByAncestor(node);

                // If this is actually a repeater and the child doesn't have any backtracking in it that might
                // cause us to need to unwind already taken iterations, just output it as a repeater loop.
                if (minIterations == maxIterations && !analysis.MayBacktrack(child))
                {
                    EmitNonBacktrackingRepeater(node);
                    return;
                }

                // We might loop any number of times.  In order to ensure this loop and subsequent code sees sliceStaticPos
                // the same regardless, we always need it to contain the same value, and the easiest such value is 0.
                // So, we transfer sliceStaticPos to pos, and ensure that any path out of here has sliceStaticPos as 0.
                TransferSliceStaticPosToPos();

                Label originalDoneLabel = doneLabel;

                Label body = DefineLabel();
                Label endLoop = DefineLabel();
                LocalBuilder iterationCount = DeclareInt32();

                // Loops that match empty iterations need additional checks in place to prevent infinitely matching (since
                // you could end up looping an infinite number of times at the same location).  We can avoid those
                // additional checks if we can prove that the loop can never match empty, which we can do by computing
                // the minimum length of the child; only if it's 0 might iterations be empty.
                bool iterationMayBeEmpty = child.ComputeMinLength() == 0;
                LocalBuilder? startingPos = iterationMayBeEmpty ? DeclareInt32() : null;

                // iterationCount = 0;
                // startingPos = 0;
                Ldc(0);
                Stloc(iterationCount);
                if (startingPos is not null)
                {
                    Ldc(0);
                    Stloc(startingPos);
                }

                // Iteration body
                MarkLabel(body);

                // We need to store the starting pos and crawl position so that it may be backtracked through later.
                // This needs to be the starting position from the iteration we're leaving, so it's pushed before updating
                // it to pos. Note that unlike some other constructs that only need to push state on to the stack if
                // they're inside of a loop (because if they're not inside of a loop, nothing would overwrite the locals),
                // here we still need the stack, because each iteration of _this_ loop may have its own state, e.g. we
                // need to know where each iteration began so when backtracking we can jump back to that location.
                EmitStackResizeIfNeeded(1 + (expressionHasCaptures ? 1 : 0) + (startingPos is not null ? 1 : 0));
                if (expressionHasCaptures)
                {
                    // base.runstack[stackpos++] = base.Crawlpos();
                    EmitStackPush(() => { Ldthis(); Call(s_crawlposMethod); });
                }
                if (startingPos is not null)
                {
                    EmitStackPush(() => Ldloc(startingPos));
                }
                EmitStackPush(() => Ldloc(pos));

                // Save off some state.  We need to store the current pos so we can compare it against
                // pos after the iteration, in order to determine whether the iteration was empty. Empty
                // iterations are allowed as part of min matches, but once we've met the min quote, empty matches
                // are considered match failures.
                if (startingPos is not null)
                {
                    // startingPos = pos;
                    Ldloc(pos);
                    Stloc(startingPos);
                }

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
                EmitNode(child);
                TransferSliceStaticPosToPos(); // ensure sliceStaticPos remains 0
                bool childBacktracks = doneLabel != iterationFailedLabel;

                // Loop condition.  Continue iterating greedily if we've not yet reached the maximum.  We also need to stop
                // iterating if the iteration matched empty and we already hit the minimum number of iterations. Otherwise,
                // we've matched as many iterations as we can with this configuration.  Jump to what comes after the loop.
                switch ((minIterations > 0, maxIterations == int.MaxValue, iterationMayBeEmpty))
                {
                    case (true, true, true):
                        // if (pos != startingPos || iterationCount < minIterations) goto body;
                        // goto endLoop;
                        Ldloc(pos);
                        Ldloc(startingPos!);
                        BneFar(body);
                        Ldloc(iterationCount);
                        Ldc(minIterations);
                        BltFar(body);
                        BrFar(endLoop);
                        break;

                    case (true, false, true):
                        // if ((pos != startingPos || iterationCount < minIterations) && iterationCount < maxIterations) goto body;
                        // goto endLoop;
                        Ldloc(iterationCount);
                        Ldc(maxIterations);
                        BgeFar(endLoop);
                        Ldloc(pos);
                        Ldloc(startingPos!);
                        BneFar(body);
                        Ldloc(iterationCount);
                        Ldc(minIterations);
                        BltFar(body);
                        BrFar(endLoop);
                        break;

                    case (false, true, true):
                        // if (pos != startingPos) goto body;
                        // goto endLoop;
                        Ldloc(pos);
                        Ldloc(startingPos!);
                        BneFar(body);
                        BrFar(endLoop);
                        break;

                    case (false, false, true):
                        // if (pos == startingPos || iterationCount >= maxIterations) goto endLoop;
                        // goto body;
                        Ldloc(pos);
                        Ldloc(startingPos!);
                        BeqFar(endLoop);
                        Ldloc(iterationCount);
                        Ldc(maxIterations);
                        BgeFar(endLoop);
                        BrFar(body);
                        break;

                    // Iterations won't be empty, but there is an upper bound. Whether or not there's a min iterations required, we need to keep
                    // iterating until we're at the maximum, and since the min is never more than the max, we don't need to check the min.
                    case (_, false, false):
                        // if (iterationCount >= maxIterations) goto endLoop;
                        // goto body;
                        Ldloc(iterationCount);
                        Ldc(maxIterations);
                        BgeFar(endLoop);
                        BrFar(body);
                        break;

                    // The loop has no upper bound and iterations can't be empty; regardless of whether there's a min iterations required,
                    // we need to loop again.
                    default:
                        // goto body;
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
                if (startingPos is not null)
                {
                    EmitStackPop();
                    Stloc(startingPos);
                }
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

                // If there's a required minimum iteration count, validate now that we've processed enough iterations.
                if (minIterations > 0)
                {
                    if (childBacktracks)
                    {
                        // The child backtracks.  If we don't have any iterations, there's nothing to backtrack into,
                        // and at least one iteration is required, so fail the loop.
                        // if (iterationCount == 0) goto originalDoneLabel;
                        Ldloc(iterationCount);
                        Ldc(0);
                        BeqFar(originalDoneLabel);

                        // We have at least one iteration; if that's insufficient to meet the minimum, backtrack
                        // into the previous iteration.  We only need to do this check if the min iteration requirement
                        // is more than one, since the above check already handles the case where the min count is 1,
                        // since the only value that wouldn't meet that is 0.
                        if (minIterations > 1)
                        {
                            // if (iterationCount < minIterations) goto doneLabel/originalDoneLabel;
                            Ldloc(iterationCount);
                            Ldc(minIterations);
                            BltFar(doneLabel);
                        }
                    }
                    else
                    {
                        // The child doesn't backtrack, which means there's no other way the matched iterations could
                        // match differently, so if we haven't already greedily processed enough iterations, fail the loop.
                        // if (iterationCount < minIterations) goto doneLabel/originalDoneLabel;
                        Ldloc(iterationCount);
                        Ldc(minIterations);
                        BltFar(originalDoneLabel);
                    }
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

                        // We're backtracking.  Check the timeout.
                        EmitTimeoutCheckIfNeeded();

                        // if (iterationCount == 0) goto originalDoneLabel;
                        Ldloc(iterationCount);
                        Ldc(0);
                        BeqFar(originalDoneLabel);

                        // goto doneLabel;
                        BrFar(doneLabel);

                        doneLabel = backtrack;
                    }

                    MarkLabel(endLoop);

                    // If this loop is itself not in another loop, nothing more needs to be done:
                    // upon backtracking, locals being used by this loop will have retained their
                    // values and be up-to-date.  But if this loop is inside another loop, multiple
                    // iterations of this loop each need their own state, so we need to use the stack
                    // to hold it, and we need a dedicated backtracking section to handle restoring
                    // that state before jumping back into the loop itself.
                    if (analysis.IsInLoop(node))
                    {
                        // Store the loop's state
                        EmitStackResizeIfNeeded(1 + (startingPos is not null ? 1 : 0));
                        if (startingPos is not null)
                        {
                            EmitStackPush(() => Ldloc(startingPos));
                        }
                        EmitStackPush(() => Ldloc(iterationCount));

                        // Skip past the backtracking section
                        // goto backtrackingEnd;
                        Label backtrackingEnd = DefineLabel();
                        BrFar(backtrackingEnd);

                        // Emit a backtracking section that restores the loop's state and then jumps to the previous done label
                        Label backtrack = DefineLabel();
                        MarkLabel(backtrack);

                        // We're backtracking.  Check the timeout.
                        EmitTimeoutCheckIfNeeded();

                        // iterationCount = base.runstack[--runstack];
                        // startingPos = base.runstack[--runstack];
                        EmitStackPop();
                        Stloc(iterationCount);
                        if (startingPos is not null)
                        {
                            EmitStackPop();
                            Stloc(startingPos);
                        }

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

        protected void EmitScan(RegexOptions options, DynamicMethod tryFindNextStartingPositionMethod, DynamicMethod tryMatchAtCurrentPositionMethod)
        {
            // As with the source generator, we can emit special code for common circumstances rather than always emitting
            // the most general purpose scan loop.  Unlike the source generator, however, code appearance isn't important
            // here, so we don't handle all of the same cases, e.g. we don't special case Empty or Nothing, as they're
            // not worth spending any code on.

            bool rtl = (options & RegexOptions.RightToLeft) != 0;
            RegexNode root = _regexTree!.Root.Child(0);
            Label returnLabel = DefineLabel();

            if (root.Kind is RegexNodeKind.Multi or RegexNodeKind.One or RegexNodeKind.Notone or RegexNodeKind.Set)
            {
                // If the whole expression is just one or more characters, we can rely on the FindOptimizations spitting out
                // an IndexOf that will find the exact sequence or not, and we don't need to do additional checking beyond that.

                // if (!TryFindNextPossibleStartingPosition(inputSpan)) return;
                Ldthis();
                Ldarg_1();
                Call(tryFindNextStartingPositionMethod);
                Brfalse(returnLabel);

                // int start = base.runtextpos;
                LocalBuilder start = DeclareInt32();
                Mvfldloc(s_runtextposField, start);

                // int end = base.runtextpos = start +/- length;
                LocalBuilder end = DeclareInt32();
                Ldloc(start);
                Ldc((root.Kind == RegexNodeKind.Multi ? root.Str!.Length : 1) * (!rtl ? 1 : -1));
                Add();
                Stloc(end);
                Ldthis();
                Ldloc(end);
                Stfld(s_runtextposField);

                // base.Capture(0, start, end);
                Ldthis();
                Ldc(0);
                Ldloc(start);
                Ldloc(end);
                Call(s_captureMethod);
            }
            else if (_regexTree.FindOptimizations.FindMode is
                    FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Beginning or
                    FindNextStartingPositionMode.LeadingAnchor_LeftToRight_Start or
                    FindNextStartingPositionMode.LeadingAnchor_RightToLeft_Start or
                    FindNextStartingPositionMode.LeadingAnchor_RightToLeft_End)
            {
                // If the expression is anchored in such a way that there's one and only one possible position that can match,
                // we don't need a scan loop, just a single check and match.

                // if (!TryFindNextPossibleStartingPosition(inputSpan)) return;
                Ldthis();
                Ldarg_1();
                Call(tryFindNextStartingPositionMethod);
                Brfalse(returnLabel);

                // if (TryMatchAtCurrentPosition(inputSpan)) return;
                Ldthis();
                Ldarg_1();
                Call(tryMatchAtCurrentPositionMethod);
                Brtrue(returnLabel);

                // base.runtextpos = inputSpan.Length; // or 0 for rtl
                Ldthis();
                if (!rtl)
                {
                    Ldarga_s(1);
                    Call(s_spanGetLengthMethod);
                }
                else
                {
                    Ldc(0);
                }
                Stfld(s_runtextposField);
            }
            else
            {
                // while (TryFindNextPossibleStartingPosition(text))
                Label whileLoopBody = DefineLabel();
                MarkLabel(whileLoopBody);
                Ldthis();
                Ldarg_1();
                Call(tryFindNextStartingPositionMethod);
                BrfalseFar(returnLabel);

                // if (TryMatchAtCurrentPosition(text) || runtextpos == text.length) // or == 0 for rtl
                //   return;
                Ldthis();
                Ldarg_1();
                Call(tryMatchAtCurrentPositionMethod);
                BrtrueFar(returnLabel);
                Ldthisfld(s_runtextposField);
                if (!rtl)
                {
                    Ldarga_s(1);
                    Call(s_spanGetLengthMethod);
                }
                else
                {
                    Ldc(0);
                }
                Ceq();
                BrtrueFar(returnLabel);

                // runtextpos++ // or -- for rtl
                Ldthis();
                Ldthisfld(s_runtextposField);
                Ldc(!rtl ? 1 : -1);
                Add();
                Stfld(s_runtextposField);

                // Check the timeout every time we run the whole match logic at a new starting location, as each such
                // operation could do work at least linear in the length of the input.
                EmitTimeoutCheckIfNeeded();

                // End loop body.
                BrFar(whileLoopBody);
            }

            // return;
            MarkLabel(returnLabel);
            Ret();
        }

        /// <summary>Emits a a check for whether the character is in the specified character class.</summary>
        /// <remarks>The character to be checked has already been loaded onto the stack.</remarks>
        private void EmitMatchCharacterClass(string charClass)
        {
            // We need to perform the equivalent of calling RegexRunner.CharInClass(ch, charClass),
            // but that call is relatively expensive.  Before we fall back to it, we try to optimize
            // some common cases for which we can do much better, such as known character classes
            // for which we can call a dedicated method, or a fast-path for ASCII using a lookup table.
            // In some cases, multiple optimizations are possible for a given character class: the checks
            // in this method are generally ordered from fastest / simplest to slowest / most complex so
            // that we get the best optimization for a given char class.

            // First, see if the char class is a built-in one for which there's a better function
            // we can just call directly.
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

            // Next, handle simple sets of one range, e.g. [A-Z], [0-9], etc.  This includes some built-in classes, like ECMADigitClass.
            if (RegexCharClass.TryGetSingleRange(charClass, out char lowInclusive, out char highInclusive))
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

            // Next, if the character class contains nothing but Unicode categories, we can call char.GetUnicodeCategory and
            // compare against it.  It has a fast-lookup path for ASCII, so is as good or better than any lookup we'd generate (plus
            // we get smaller code), and it's what we'd do for the fallback (which we get to avoid generating) as part of CharInClass.
            // Unlike the source generator, however, we only handle the case of a single UnicodeCategory: the source generator is able
            // to rely on C# compiler optimizations to handle dealing with multiple values efficiently.
            Span<UnicodeCategory> categories = stackalloc UnicodeCategory[1]; // handle the case of one and only one category
            if (RegexCharClass.TryGetOnlyCategories(charClass, categories, out int numCategories, out bool negated))
            {
                // char.GetUnicodeCategory(ch) == category
                Call(s_charGetUnicodeInfo);
                Ldc((int)categories[0]);
                Ceq();
                if (negated)
                {
                    Ldc(0);
                    Ceq();
                }

                return;
            }

            // Checks after this point require reading the input character multiple times,
            // so we store it into a temporary local.
            using RentedLocalBuilder tempLocal = RentInt32Local();
            Stloc(tempLocal);

            // Next, if there's only 2 or 3 chars in the set (fairly common due to the sets we create for prefixes),
            // it's cheaper and smaller to compare against each than it is to use a lookup table.
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

                if (RegexCharClass.IsNegated(charClass))
                {
                    Ldc(0);
                    Ceq();
                }
                return;
            }

            // Next, handle simple sets of two ASCII letter ranges that are cased versions of each other, e.g. [A-Za-z].
            // This can be implemented as if it were a single range, with an additional bitwise operation.
            if (RegexCharClass.TryGetDoubleRange(charClass, out (char LowInclusive, char HighInclusive) rangeLower, out (char LowInclusive, char HighInclusive) rangeUpper) &&
                RegexCharClass.IsAsciiLetter(rangeUpper.LowInclusive) &&
                RegexCharClass.IsAsciiLetter(rangeUpper.HighInclusive) &&
                (rangeLower.LowInclusive | 0x20) == rangeUpper.LowInclusive &&
                (rangeLower.HighInclusive | 0x20) == rangeUpper.HighInclusive)
            {
                Debug.Assert(rangeLower.LowInclusive != rangeUpper.LowInclusive);
                bool negate = RegexCharClass.IsNegated(charClass);

                // (uint)((ch | 0x20) - lowInclusive) < highInclusive - lowInclusive + 1
                Ldloc(tempLocal);
                Ldc(0x20);
                Or();
                Ldc(rangeUpper.LowInclusive);
                Sub();
                Ldc(rangeUpper.HighInclusive - rangeUpper.LowInclusive + 1);
                CltUn();
                if (negate)
                {
                    Ldc(0);
                    Ceq();
                }
                return;
            }

            // Analyze the character set more to determine what code to generate.
            RegexCharClass.CharClassAnalysisResults analysis = RegexCharClass.Analyze(charClass);

            // Next, handle sets where the high - low + 1 range is <= 64.  In that case, we can emit
            // a branchless lookup in a ulong that does not rely on loading any objects (e.g. the string-based
            // lookup we use later).  This nicely handles common sets like [0-9A-Fa-f], [0-9a-f], [A-Za-z], etc.
            if (analysis.OnlyRanges && (analysis.UpperBoundExclusiveIfOnlyRanges - analysis.LowerBoundInclusiveIfOnlyRanges) <= 64)
            {
                // Create the 64-bit value with 1s at indices corresponding to every character in the set,
                // where the bit is computed to be the char value minus the lower bound starting from
                // most significant bit downwards.
                ulong bitmap = 0;
                bool negatedClass = RegexCharClass.IsNegated(charClass);
                for (int i = analysis.LowerBoundInclusiveIfOnlyRanges; i < analysis.UpperBoundExclusiveIfOnlyRanges; i++)
                {
                    if (RegexCharClass.CharInClass((char)i, charClass) ^ negatedClass)
                    {
                        bitmap |= (1ul << (63 - (i - analysis.LowerBoundInclusiveIfOnlyRanges)));
                    }
                }

                // To determine whether a character is in the set, we subtract the lowest char (casting to
                // uint to account for any smaller values); this subtraction happens before the result is
                // zero-extended to ulong, meaning that `charMinusLow` will always have upper 32 bits equal to 0.
                // We then left shift the constant with this offset, and apply a bitmask that has the highest
                // bit set (the sign bit) if and only if `chExpr` is in the [low, low + 64) range.
                // Then we only need to check whether this final result is less than 0: this will only be
                // the case if both `charMinusLow` was in fact the index of a set bit in the constant, and also
                // `chExpr` was in the allowed range (this ensures that false positive bit shifts are ignored).

                // ulong charMinusLow = (uint)ch - lowInclusive;
                LocalBuilder charMinusLow = _ilg!.DeclareLocal(typeof(ulong));
                Ldloc(tempLocal);
                Ldc(analysis.LowerBoundInclusiveIfOnlyRanges);
                Sub();
                _ilg!.Emit(OpCodes.Conv_U8);
                Stloc(charMinusLow);

                // ulong shift = bitmap << (int)charMinusLow;
                LdcI8((long)bitmap);
                Ldloc(charMinusLow);
                _ilg!.Emit(OpCodes.Conv_I4);
                Ldc(63);
                And();
                Shl();

                // ulong mask = charMinusLow - 64;
                Ldloc(charMinusLow);
                Ldc(64);
                _ilg!.Emit(OpCodes.Conv_I8);
                Sub();

                // (long)(shift & mask) < 0 // or >= for a negated character class
                And();
                Ldc(0);
                _ilg!.Emit(OpCodes.Conv_I8);
                _ilg!.Emit(OpCodes.Clt);
                if (negatedClass)
                {
                    Ldc(0);
                    Ceq();
                }

                return;
            }

            // Next, handle simple sets of two ranges, e.g. [\p{IsGreek}\p{IsGreekExtended}].
            if (RegexCharClass.TryGetDoubleRange(charClass, out (char LowInclusive, char HighInclusive) range0, out (char LowInclusive, char HighInclusive) range1))
            {
                bool negate = RegexCharClass.IsNegated(charClass);

                if (range0.LowInclusive == range0.HighInclusive)
                {
                    // ch == lowInclusive
                    Ldloc(tempLocal);
                    Ldc(range0.LowInclusive);
                    Ceq();
                }
                else
                {
                    // (uint)(ch - lowInclusive) < (uint)(highInclusive - lowInclusive + 1)
                    Ldloc(tempLocal);
                    Ldc(range0.LowInclusive);
                    Sub();
                    Ldc(range0.HighInclusive - range0.LowInclusive + 1);
                    CltUn();
                }
                if (negate)
                {
                    Ldc(0);
                    Ceq();
                }

                if (range1.LowInclusive == range1.HighInclusive)
                {
                    // ch == lowInclusive
                    Ldloc(tempLocal);
                    Ldc(range1.LowInclusive);
                    Ceq();
                }
                else
                {
                    // (uint)(ch - lowInclusive) < (uint)(highInclusive - lowInclusive + 1)
                    Ldloc(tempLocal);
                    Ldc(range1.LowInclusive);
                    Sub();
                    Ldc(range1.HighInclusive - range1.LowInclusive + 1);
                    CltUn();
                }
                if (negate)
                {
                    Ldc(0);
                    Ceq();
                }

                if (negate)
                {
                    And();
                }
                else
                {
                    Or();
                }

                return;
            }

            using RentedLocalBuilder resultLocal = RentInt32Local();

            // Helper method that emits a call to RegexRunner.CharInClass(ch, charClass)
            void EmitCharInClass()
            {
                Ldloc(tempLocal);
                Ldstr(charClass);
                Call(s_charInClassMethod);
                Stloc(resultLocal);
            }

            Label doneLabel = DefineLabel();
            Label comparisonLabel = DefineLabel();

            if (analysis.ContainsNoAscii)
            {
                // We determined that the character class contains only non-ASCII,
                // for example if the class were [\u1000-\u2000\u3000-\u4000\u5000-\u6000].
                // (In the future, we could possibly extend the analysis to produce a known
                // lower-bound and compare against that rather than always using 128 as the
                // pivot point.)

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
            string bitVectorString = string.Create(8, charClass, static (dest, charClass) => // String length is 8 chars == 16 bytes == 128 bits.
            {
                for (int i = 0; i < 128; i++)
                {
                    char c = (char)i;
                    if (RegexCharClass.CharInClass(c, charClass))
                    {
                        dest[i >> 4] |= (char)(1 << (i & 0xF));
                    }
                }
            });

            // We determined that the character class may contain ASCII, so we
            // output the lookup against the lookup table.

            // ch < 128 ? (bitVectorString[ch >> 4] & (1 << (ch & 0xF))) != 0 :
            Ldloc(tempLocal);
            Ldc(analysis.ContainsOnlyAscii ? analysis.UpperBoundExclusiveIfOnlyRanges : 128);
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

        /// <summary>Emits a timeout check if one has been set explicitly or implicitly via a default setting.</summary>
        /// Regex timeouts exist to avoid catastrophic backtracking.  The goal with timeouts isn't to be accurate to the timeout value,
        /// but to ensure that significant backtracking can be stopped.  As such, we allow for up to O(n) work in the length of the input
        /// between checks, which means we emit checks anywhere backtracking is introduced, such that every check can have O(n) work
        /// associated with it.  This means checks:
        /// - when restarting the whole match evaluation at a new index. Every match could end up doing O(n) work without a timeout
        ///   check, and since this could then result in O(n) matches, we need a timeout check on each new position in order to
        ///   avoid O(n^2) work without a timeout check.
        /// - when backtracking backwards in a loop. Every backtracking step through the loop could evaluate the remainder of the
        ///   pattern, which can lead to O(2^n) work if unchecked.
        /// - when backtracking forwards in a lazy loop. Every backtracking step through the loop could evaluate the remainder of the
        ///   pattern, which can lead to O(2^n) work if unchecked.
        /// - when backtracking to the next branch of an alternation. Every branch of the alternation could evaluate the remainder of the
        ///   pattern, which can lead to O(2^n) work if unchecked.
        /// - when performing a lookaround.  Each lookaround can result in doing O(n) work, which means m lookarounds can result in
        ///   O(m*n) work.  Lookarounds can be in loops, so without timeout checks in a lookaround, a pattern like `((?=(?>a*))a)+`
        ///   could do O(n^2) work without a timeout check.
        /// Note that some other constructs have code that needs to deal with backtracking, e.g. conditionals needing to ensure
        /// that if any of their children have backtracking that code which backtracks back into the conditional is appropriately
        /// routed to the correct child, but such constructs aren't actually introducing backtracking and thus don't need to be
        /// instrumented for timeouts.
        private void EmitTimeoutCheckIfNeeded()
        {
            if (_hasTimeout)
            {
                // base.CheckTimeout();
                Ldthis();
                Call(s_checkTimeoutMethod);
            }
        }
    }
}
