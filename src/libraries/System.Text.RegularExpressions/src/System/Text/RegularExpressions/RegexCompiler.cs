// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Text.RegularExpressions
{
    /// <summary>
    /// RegexCompiler translates a block of RegexCode to MSIL, and creates a
    /// subclass of the RegexRunner type.
    /// </summary>
    internal abstract class RegexCompiler
    {
        private static readonly FieldInfo s_textbegF = RegexRunnerField("runtextbeg");
        private static readonly FieldInfo s_textendF = RegexRunnerField("runtextend");
        private static readonly FieldInfo s_textstartF = RegexRunnerField("runtextstart");
        private static readonly FieldInfo s_textposF = RegexRunnerField("runtextpos");
        private static readonly FieldInfo s_textF = RegexRunnerField("runtext");
        private static readonly FieldInfo s_trackposF = RegexRunnerField("runtrackpos");
        private static readonly FieldInfo s_trackF = RegexRunnerField("runtrack");
        private static readonly FieldInfo s_stackposF = RegexRunnerField("runstackpos");
        private static readonly FieldInfo s_stackF = RegexRunnerField("runstack");
        private static readonly FieldInfo s_trackcountF = RegexRunnerField("runtrackcount");

        private static readonly MethodInfo s_ensurestorageM = RegexRunnerMethod("EnsureStorage");
        private static readonly MethodInfo s_captureM = RegexRunnerMethod("Capture");
        private static readonly MethodInfo s_transferM = RegexRunnerMethod("TransferCapture");
        private static readonly MethodInfo s_uncaptureM = RegexRunnerMethod("Uncapture");
        private static readonly MethodInfo s_ismatchedM = RegexRunnerMethod("IsMatched");
        private static readonly MethodInfo s_matchlengthM = RegexRunnerMethod("MatchLength");
        private static readonly MethodInfo s_matchindexM = RegexRunnerMethod("MatchIndex");
        private static readonly MethodInfo s_isboundaryM = RegexRunnerMethod("IsBoundary");
        private static readonly MethodInfo s_isECMABoundaryM = RegexRunnerMethod("IsECMABoundary");
        private static readonly MethodInfo s_chartolowerM = typeof(char).GetMethod("ToLower", new Type[] { typeof(char), typeof(CultureInfo) })!;
        private static readonly MethodInfo s_chartolowerinvariantM = typeof(char).GetMethod("ToLowerInvariant", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charIsDigitM = typeof(char).GetMethod("IsDigit", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_charIsWhiteSpaceM = typeof(char).GetMethod("IsWhiteSpace", new Type[] { typeof(char) })!;
        private static readonly MethodInfo s_getcharM = typeof(string).GetMethod("get_Chars", new Type[] { typeof(int) })!;
        private static readonly MethodInfo s_crawlposM = RegexRunnerMethod("Crawlpos");
        private static readonly MethodInfo s_charInClassM = RegexRunnerMethod("CharInClass");
        private static readonly MethodInfo s_getCurrentCulture = typeof(CultureInfo).GetMethod("get_CurrentCulture")!;
        private static readonly MethodInfo s_checkTimeoutM = RegexRunnerMethod("CheckTimeout");
#if DEBUG
        private static readonly MethodInfo s_dumpstateM = RegexRunnerMethod("DumpState");
#endif

        protected ILGenerator? _ilg;

        // tokens representing local variables
        private LocalBuilder? _textstartV;
        private LocalBuilder? _textbegV;
        private LocalBuilder? _textendV;
        private LocalBuilder? _textposV;
        private LocalBuilder? _textV;
        private LocalBuilder? _trackposV;
        private LocalBuilder? _trackV;
        private LocalBuilder? _stackposV;
        private LocalBuilder? _stackV;
        private LocalBuilder? _tempV;
        private LocalBuilder? _temp2V;
        private LocalBuilder? _temp3V;
        private LocalBuilder? _cultureV;      // current culture is cached in local variable to prevent many thread local storage accesses for CultureInfo.CurrentCulture
        private LocalBuilder? _loopV;         // counter for setrep and setloop

        protected RegexCode? _code;           // the RegexCode object (used for debugging only)
        protected int[]? _codes;              // the RegexCodes being translated
        protected string[]? _strings;         // the stringtable associated with the RegexCodes
        protected RegexPrefix? _fcPrefix;     // the possible first chars computed by RegexFCD
        protected RegexBoyerMoore? _bmPrefix; // a prefix as a boyer-moore machine
        protected int _anchors;               // the set of anchors
        protected bool _hasTimeout;           // whether the regex has a non-infinite timeout

        private Label[]? _labels;             // a label for every operation in _codes
        private BacktrackNote[]? _notes;      // a list of the backtracking states to be generated
        private int _notecount;               // true count of _notes (allocation grows exponentially)
        protected int _trackcount;            // count of backtracking states (used to reduce allocations)

        private Label _backtrack;             // label for backtracking


        private int _regexopcode;             // the current opcode being processed
        private int _codepos;                 // the current code being translated
        private int _backpos;                 // the current backtrack-note being translated

        protected RegexOptions _options;      // options

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
        internal static RegexRunnerFactory Compile(RegexCode code, RegexOptions options, bool hasTimeout) => new RegexLWCGCompiler().FactoryInstanceFromCode(code, options, hasTimeout);

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
        private bool IsRtl() => (_regexopcode & RegexCode.Rtl) != 0;

        /// <summary>True if the current operation is marked for case insensitive operation.</summary>
        private bool IsCi() => (_regexopcode & RegexCode.Ci) != 0;

#if DEBUG
        /// <summary>True if we need to do the backtrack logic for the current operation.</summary>
        private bool IsBack() => (_regexopcode & RegexCode.Back) != 0;

        /// <summary>True if we need to do the second-backtrack logic for the current operation.</summary>
        private bool IsBack2() => (_regexopcode & RegexCode.Back2) != 0;
#endif

        /// <summary>Returns the raw regex opcode (masking out Back and Rtl).</summary>
        private int Code() => _regexopcode & RegexCode.Mask;

        /// <summary>A macro for _ilg.Emit(Opcodes.Ldstr, str)</summary>
        private void Ldstr(string str) => _ilg!.Emit(OpCodes.Ldstr, str);

        /// <summary>A macro for the various forms of Ldc.</summary>
        private void Ldc(int i)
        {
            Debug.Assert(_ilg != null);

            if ((uint)i < 8)
            {
                _ilg.Emit(i switch
                {
                    0 => OpCodes.Ldc_I4_0,
                    1 => OpCodes.Ldc_I4_1,
                    2 => OpCodes.Ldc_I4_2,
                    3 => OpCodes.Ldc_I4_3,
                    4 => OpCodes.Ldc_I4_4,
                    5 => OpCodes.Ldc_I4_5,
                    6 => OpCodes.Ldc_I4_6,
                    _ => OpCodes.Ldc_I4_7,
                });
            }
            else if (i <= 127 && i >= -128)
            {
                _ilg.Emit(OpCodes.Ldc_I4_S, (byte)i);
            }
            else
            {
                _ilg.Emit(OpCodes.Ldc_I4, i);
            }
        }

        /// <summary>A macro for the various forms of LdcI8.</summary>
        private void LdcI8(long i)
        {
            if (i <= int.MaxValue && i >= int.MinValue)
            {
                Ldc((int)i);
                _ilg!.Emit(OpCodes.Conv_I8);
            }
            else
            {
                _ilg!.Emit(OpCodes.Ldc_I8, i);
            }
        }

        /// <summary>A macro for _ilg.Emit(OpCodes.Dup).</summary>
        private void Dup() => _ilg!.Emit(OpCodes.Dup);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ret).</summary>
        private void Ret() => _ilg!.Emit(OpCodes.Ret);

        /// <summary>A macro for _ilg.Emit(OpCodes.Rem).</summary>
        private void Rem() => _ilg!.Emit(OpCodes.Rem);

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

        /// <summary>A macro for _ilg.Emit(OpCodes.Div).</summary>
        private void Div() => _ilg!.Emit(OpCodes.Div);

        /// <summary>A macro for _ilg.Emit(OpCodes.And).</summary>
        private void And() => _ilg!.Emit(OpCodes.And);

        /// <summary>A macro for _ilg.Emit(OpCodes.Shl).</summary>
        private void Shl() => _ilg!.Emit(OpCodes.Shl);

        /// <summary>A macro for _ilg.Emit(OpCodes.Shr).</summary>
        private void Shr() => _ilg!.Emit(OpCodes.Shr);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldloc_S).</summary>
        private void Ldloc(LocalBuilder lt) => _ilg!.Emit(OpCodes.Ldloc_S, lt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Stloc).</summary>
        private void Stloc(LocalBuilder lt) => _ilg!.Emit(OpCodes.Stloc_S, lt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ldarg_0).</summary>
        private void Ldthis() => _ilg!.Emit(OpCodes.Ldarg_0);

        /// <summary>A macro for Ldthis(); Ldfld();</summary>
        private void Ldthisfld(FieldInfo ft)
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

        /// <summary>A macro for Ldthis(); Ldloc(); Stfld();</summary>
        private void Mvlocfld(LocalBuilder lt, FieldInfo ft)
        {
            Ldthis();
            Ldloc(lt);
            Stfld(ft);
        }

        /// <summary>A macro for _ilg.Emit(OpCodes.Stfld).</summary>
        private void Stfld(FieldInfo ft) => _ilg!.Emit(OpCodes.Stfld, ft);

        /// <summary>A macro for _ilg.Emit(OpCodes.Callvirt, mt).</summary>
        private void Callvirt(MethodInfo mt) => _ilg!.Emit(OpCodes.Callvirt, mt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Call, mt).</summary>
        private void Call(MethodInfo mt) => _ilg!.Emit(OpCodes.Call, mt);

        /// <summary>A macro for _ilg.Emit(OpCodes.Newobj, ct).</summary>
        private void Newobj(ConstructorInfo ct) => _ilg!.Emit(OpCodes.Newobj, ct);

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

        /// <summary>A macro for _ilg.Emit(OpCodes.Bge) (long form).</summary>
        private void BgeFar(Label l) => _ilg!.Emit(OpCodes.Bge, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bgt) (long form).</summary>
        private void BgtFar(Label l) => _ilg!.Emit(OpCodes.Bgt, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bne) (long form).</summary>
        private void BneFar(Label l) => _ilg!.Emit(OpCodes.Bne_Un, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Beq) (long form).</summary>
        private void BeqFar(Label l) => _ilg!.Emit(OpCodes.Beq, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Brfalse_S) (short jump).</summary>
        private void Brfalse(Label l) => _ilg!.Emit(OpCodes.Brfalse_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Br_S) (short jump).</summary>
        private void Br(Label l) => _ilg!.Emit(OpCodes.Br_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Ble_S) (short jump).</summary>
        private void Ble(Label l) => _ilg!.Emit(OpCodes.Ble_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Blt_S) (short jump).</summary>
        private void Blt(Label l) => _ilg!.Emit(OpCodes.Blt_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bge_S) (short jump).</summary>
        private void Bge(Label l) => _ilg!.Emit(OpCodes.Bge_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bgt_S) (short jump).</summary>
        private void Bgt(Label l) => _ilg!.Emit(OpCodes.Bgt_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bleun_S) (short jump).</summary>
        private void Bgtun(Label l) => _ilg!.Emit(OpCodes.Bgt_Un_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Bne_S) (short jump).</summary>
        private void Bne(Label l) => _ilg!.Emit(OpCodes.Bne_Un_S, l);

        /// <summary>A macro for _ilg.Emit(OpCodes.Beq_S) (short jump).</summary>
        private void Beq(Label l) => _ilg!.Emit(OpCodes.Beq_S, l);

        /// <summary>A macro for the Ldlen instruction).</summary>
        private void Ldlen() => _ilg!.Emit(OpCodes.Ldlen);

        /// <summary>Loads the char to the right of the current position.</summary>
        private void Rightchar()
        {
            Ldloc(_textV!);
            Ldloc(_textposV!);
            Callvirt(s_getcharM);
        }

        /// <summary>Loads the char to the right of the current position and advances the current position.</summary>
        private void Rightcharnext()
        {
            Ldloc(_textV!);
            Ldloc(_textposV!);
            Dup();
            Ldc(1);
            Add();
            Stloc(_textposV!);
            Callvirt(s_getcharM);
        }

        /// <summary>Loads the char to the left of the current position.</summary>
        private void Leftchar()
        {
            Ldloc(_textV!);
            Ldloc(_textposV!);
            Ldc(1);
            Sub();
            Callvirt(s_getcharM);
        }

        /// <summary>Loads the char to the left of the current position and advances (leftward).</summary>
        private void Leftcharnext()
        {
            Ldloc(_textV!);
            Ldloc(_textposV!);
            Ldc(1);
            Sub();
            Dup();
            Stloc(_textposV!);
            Callvirt(s_getcharM);
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
            _ilg!.Emit(OpCodes.Ldloc_S, _trackV!);
            _ilg.Emit(OpCodes.Ldloc_S, _trackposV!);
            _ilg.Emit(OpCodes.Ldc_I4_1);
            _ilg.Emit(OpCodes.Sub);
            _ilg.Emit(OpCodes.Dup);
            _ilg.Emit(OpCodes.Stloc_S, _trackposV!);
        }

        /// <summary>Pops an element off the tracking stack (leave it on the operand stack).</summary>
        private void PopTrack()
        {
            _ilg!.Emit(OpCodes.Ldloc_S, _trackV!);
            _ilg.Emit(OpCodes.Ldloc_S, _trackposV!);
            _ilg.Emit(OpCodes.Dup);
            _ilg.Emit(OpCodes.Ldc_I4_1);
            _ilg.Emit(OpCodes.Add);
            _ilg.Emit(OpCodes.Stloc_S, _trackposV!);
            _ilg.Emit(OpCodes.Ldelem_I4);
        }

        /// <summary>Retrieves the top entry on the tracking stack without popping.</summary>
        private void TopTrack()
        {
            _ilg!.Emit(OpCodes.Ldloc_S, _trackV!);
            _ilg.Emit(OpCodes.Ldloc_S, _trackposV!);
            _ilg.Emit(OpCodes.Ldelem_I4);
        }

        /// <summary>Saves the value of a local variable on the grouping stack.</summary>
        private void PushStack(LocalBuilder lt)
        {
            ReadyPushStack();
            _ilg!.Emit(OpCodes.Ldloc_S, lt);
            DoPush();
        }

        /// <summary>Prologue to code that will replace the ith element on the grouping stack.</summary>
        internal void ReadyReplaceStack(int i)
        {
            _ilg!.Emit(OpCodes.Ldloc_S, _stackV!);
            _ilg.Emit(OpCodes.Ldloc_S, _stackposV!);
            if (i != 0)
            {
                Ldc(i);
                _ilg.Emit(OpCodes.Add);
            }
        }

        /// <summary>Prologue to code that will push an element on the grouping stack.</summary>
        private void ReadyPushStack()
        {
            _ilg!.Emit(OpCodes.Ldloc_S, _stackV!);
            _ilg.Emit(OpCodes.Ldloc_S, _stackposV!);
            _ilg.Emit(OpCodes.Ldc_I4_1);
            _ilg.Emit(OpCodes.Sub);
            _ilg.Emit(OpCodes.Dup);
            _ilg.Emit(OpCodes.Stloc_S, _stackposV!);
        }

        /// <summary>Retrieves the top entry on the stack without popping.</summary>
        private void TopStack()
        {
            _ilg!.Emit(OpCodes.Ldloc_S, _stackV!);
            _ilg.Emit(OpCodes.Ldloc_S, _stackposV!);
            _ilg.Emit(OpCodes.Ldelem_I4);
        }

        /// <summary>Pops an element off the grouping stack (leave it on the operand stack).</summary>
        private void PopStack()
        {
            _ilg!.Emit(OpCodes.Ldloc_S, _stackV!);
            _ilg.Emit(OpCodes.Ldloc_S, _stackposV!);
            _ilg.Emit(OpCodes.Dup);
            _ilg.Emit(OpCodes.Ldc_I4_1);
            _ilg.Emit(OpCodes.Add);
            _ilg.Emit(OpCodes.Stloc_S, _stackposV!);
            _ilg.Emit(OpCodes.Ldelem_I4);
        }

        /// <summary>Pops 1 element off the grouping stack and discards it.</summary>
        private void PopDiscardStack() => PopDiscardStack(1);

        /// <summary>Pops i elements off the grouping stack and discards them.</summary>
        private void PopDiscardStack(int i)
        {
            _ilg!.Emit(OpCodes.Ldloc_S, _stackposV!);
            Ldc(i);
            _ilg.Emit(OpCodes.Add);
            _ilg.Emit(OpCodes.Stloc_S, _stackposV!);
        }

        /// <summary>Epilogue to code that will replace an element on a stack (use Ld* in between).</summary>
        private void DoReplace() => _ilg!.Emit(OpCodes.Stelem_I4);

        /// <summary>Epilogue to code that will push an element on a stack (use Ld* in between).</summary>
        private void DoPush() => _ilg!.Emit(OpCodes.Stelem_I4);

        /// <summary>Jump to the backtracking switch.</summary>
        private void Back() => _ilg!.Emit(OpCodes.Br, _backtrack);

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
                Ldloc(_trackposV!);
                Ldc(_trackcount * 4);
                Ble(l1);
                Ldloc(_stackposV!);
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
        private void Advance() => _ilg!.Emit(OpCodes.Br, AdvanceLabel());

        /// <summary>Sets the culture local to CultureInfo.CurrentCulture.</summary>
        private void InitLocalCultureInfo()
        {
            Debug.Assert(_cultureV != null);
            Call(s_getCurrentCulture);
            Stloc(_cultureV);
        }

        /// <summary>Invokes either char.ToLower(..., _culture) or char.ToLowerInvariant(...).</summary>
        private void CallToLower()
        {
            if (_cultureV == null || _options.HasFlag(RegexOptions.CultureInvariant))
            {
                Call(s_chartolowerinvariantM);
            }
            else
            {
                Ldloc(_cultureV!);
                Call(s_chartolowerM);
            }
        }

        /// <summary>
        /// Generates the first section of the MSIL. This section contains all
        /// the forward logic, and corresponds directly to the regex codes.
        /// In the absence of backtracking, this is all we would need.
        /// </summary>
        private void GenerateForwardSection()
        {
            _labels = new Label[_codes!.Length];
            _goto = new int[_codes.Length];

            // initialize

            int codepos;
            for (codepos = 0; codepos < _codes.Length; codepos += RegexCode.OpcodeSize(_codes[codepos]))
            {
                _goto[codepos] = -1;
                _labels[codepos] = _ilg!.DefineLabel();
            }

            _uniquenote = new int[Uniquecount];
            Array.Fill(_uniquenote, -1);

            // emit variable initializers

            Mvfldloc(s_textF, _textV!);
            Mvfldloc(s_textstartF, _textstartV!);
            Mvfldloc(s_textbegF, _textbegV!);
            Mvfldloc(s_textendF, _textendV!);
            Mvfldloc(s_textposF, _textposV!);
            Mvfldloc(s_trackF, _trackV!);
            Mvfldloc(s_trackposF, _trackposV!);
            Mvfldloc(s_stackF, _stackV!);
            Mvfldloc(s_stackposF, _stackposV!);

            _backpos = -1;

            for (codepos = 0; codepos < _codes.Length; codepos += RegexCode.OpcodeSize(_codes[codepos]))
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
            // Backtrack switch
            MarkLabel(_backtrack);

            // first call EnsureStorage
            Mvlocfld(_trackposV!, s_trackposF);
            Mvlocfld(_stackposV!, s_stackposF);
            Ldthis();
            Callvirt(s_ensurestorageM);
            Mvfldloc(s_trackposF, _trackposV!);
            Mvfldloc(s_stackposF, _stackposV!);
            Mvfldloc(s_trackF, _trackV!);
            Mvfldloc(s_stackF, _stackV!);

            PopTrack();

            var table = new Label[_notecount];
            for (int i = 0; i < _notecount; i++)
            {
                table[i] = _notes![i]._label;
            }

            _ilg!.Emit(OpCodes.Switch, table);
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
                    _ilg!.MarkLabel(n._label);
                    _codepos = n._codepos;
                    _backpos = i;
                    _regexopcode = _codes![n._codepos] | n._flags;
                    GenerateOneCode();
                }
            }
        }

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !!!! This function must be kept synchronized with FindFirstChar in      !!!!
        // !!!! RegexInterpreter.cs                                                !!!!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        /// <summary>
        /// Generates FindFirstChar.
        /// </summary>
        protected void GenerateFindFirstChar()
        {
            _textposV = DeclareInt();
            _textV = DeclareString();
            _tempV = DeclareInt();
            _temp2V = DeclareInt();
            _cultureV = null;
            if (!_options.HasFlag(RegexOptions.CultureInvariant))
            {
                if (_options.HasFlag(RegexOptions.IgnoreCase) ||
                    _bmPrefix?.CaseInsensitive == true ||
                    _fcPrefix.GetValueOrDefault().CaseInsensitive)
                {
                    _cultureV = DeclareCultureInfo();
                    InitLocalCultureInfo();
                }
            }

            if ((_anchors & (RegexFCD.Beginning | RegexFCD.Start | RegexFCD.EndZ | RegexFCD.End)) != 0)
            {
                if (!_code!.RightToLeft)
                {
                    if ((_anchors & RegexFCD.Beginning) != 0)
                    {
                        Label l1 = DefineLabel();
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textbegF);
                        Ble(l1);
                        Ldthis();
                        Ldthisfld(s_textendF);
                        Stfld(s_textposF);
                        Ldc(0);
                        Ret();
                        MarkLabel(l1);
                    }

                    if ((_anchors & RegexFCD.Start) != 0)
                    {
                        Label l1 = DefineLabel();
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textstartF);
                        Ble(l1);
                        Ldthis();
                        Ldthisfld(s_textendF);
                        Stfld(s_textposF);
                        Ldc(0);
                        Ret();
                        MarkLabel(l1);
                    }

                    if ((_anchors & RegexFCD.EndZ) != 0)
                    {
                        Label l1 = DefineLabel();
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textendF);
                        Ldc(1);
                        Sub();
                        Bge(l1);
                        Ldthis();
                        Ldthisfld(s_textendF);
                        Ldc(1);
                        Sub();
                        Stfld(s_textposF);
                        MarkLabel(l1);
                    }

                    if ((_anchors & RegexFCD.End) != 0)
                    {
                        Label l1 = DefineLabel();
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textendF);
                        Bge(l1);
                        Ldthis();
                        Ldthisfld(s_textendF);
                        Stfld(s_textposF);
                        MarkLabel(l1);
                    }
                }
                else
                {
                    if ((_anchors & RegexFCD.End) != 0)
                    {
                        Label l1 = DefineLabel();
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textendF);
                        Bge(l1);
                        Ldthis();
                        Ldthisfld(s_textbegF);
                        Stfld(s_textposF);
                        Ldc(0);
                        Ret();
                        MarkLabel(l1);
                    }

                    if ((_anchors & RegexFCD.EndZ) != 0)
                    {
                        Label l1 = DefineLabel();
                        Label l2 = DefineLabel();
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textendF);
                        Ldc(1);
                        Sub();
                        Blt(l1);
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textendF);
                        Beq(l2);
                        Ldthisfld(s_textF);
                        Ldthisfld(s_textposF);
                        Callvirt(s_getcharM);
                        Ldc('\n');
                        Beq(l2);
                        MarkLabel(l1);
                        Ldthis();
                        Ldthisfld(s_textbegF);
                        Stfld(s_textposF);
                        Ldc(0);
                        Ret();
                        MarkLabel(l2);
                    }

                    if ((_anchors & RegexFCD.Start) != 0)
                    {
                        Label l1 = DefineLabel();
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textstartF);
                        Bge(l1);
                        Ldthis();
                        Ldthisfld(s_textbegF);
                        Stfld(s_textposF);
                        Ldc(0);
                        Ret();
                        MarkLabel(l1);
                    }

                    if ((_anchors & RegexFCD.Beginning) != 0)
                    {
                        Label l1 = DefineLabel();
                        Ldthisfld(s_textposF);
                        Ldthisfld(s_textbegF);
                        Ble(l1);
                        Ldthis();
                        Ldthisfld(s_textbegF);
                        Stfld(s_textposF);
                        MarkLabel(l1);
                    }
                }

                Ldc(1);
                Ret();
            }
            else if (_bmPrefix != null && _bmPrefix.NegativeUnicode == null)
            {
                // Compiled Boyer-Moore string matching

                LocalBuilder chV = _tempV;
                LocalBuilder testV = _tempV;
                LocalBuilder limitV = _temp2V;
                Label lDefaultAdvance = DefineLabel();
                Label lAdvance = DefineLabel();
                Label lFail = DefineLabel();
                Label lStart = DefineLabel();
                Label lPartialMatch = DefineLabel();

                int beforefirst;
                int last;
                if (!_code!.RightToLeft)
                {
                    beforefirst = -1;
                    last = _bmPrefix.Pattern.Length - 1;
                }
                else
                {
                    beforefirst = _bmPrefix.Pattern.Length;
                    last = 0;
                }

                int chLast = _bmPrefix.Pattern[last];

                Mvfldloc(s_textF, _textV);
                Ldthisfld(_code.RightToLeft ? s_textbegF : s_textendF);
                Stloc(limitV);

                Ldthisfld(s_textposF);
                if (!_code.RightToLeft)
                {
                    Ldc(_bmPrefix.Pattern.Length - 1);
                    Add();
                }
                else
                {
                    Ldc(_bmPrefix.Pattern.Length);
                    Sub();
                }
                Stloc(_textposV);
                Br(lStart);

                MarkLabel(lDefaultAdvance);

                Ldc(_code.RightToLeft ? -_bmPrefix.Pattern.Length : _bmPrefix.Pattern.Length);

                MarkLabel(lAdvance);

                Ldloc(_textposV);
                Add();
                Stloc(_textposV);

                MarkLabel(lStart);

                Ldloc(_textposV);
                Ldloc(limitV);
                if (!_code.RightToLeft)
                {
                    BgeFar(lFail);
                }
                else
                {
                    BltFar(lFail);
                }

                Rightchar();
                if (_bmPrefix.CaseInsensitive)
                {
                    CallToLower();
                }

                Dup();
                Stloc(chV);
                Ldc(chLast);
                BeqFar(lPartialMatch);

                Ldloc(chV);
                Ldc(_bmPrefix.LowASCII);
                Sub();
                Dup();
                Stloc(chV);
                Ldc(_bmPrefix.HighASCII - _bmPrefix.LowASCII);
                Bgtun(lDefaultAdvance);

                var table = new Label[_bmPrefix.HighASCII - _bmPrefix.LowASCII + 1];

                for (int i = _bmPrefix.LowASCII; i <= _bmPrefix.HighASCII; i++)
                {
                    table[i - _bmPrefix.LowASCII] = (_bmPrefix.NegativeASCII[i] == beforefirst) ?
                        lDefaultAdvance :
                        DefineLabel();
                }

                Ldloc(chV);
                _ilg!.Emit(OpCodes.Switch, table);

                for (int i = _bmPrefix.LowASCII; i <= _bmPrefix.HighASCII; i++)
                {
                    if (_bmPrefix.NegativeASCII[i] == beforefirst)
                    {
                        continue;
                    }

                    MarkLabel(table[i - _bmPrefix.LowASCII]);

                    Ldc(_bmPrefix.NegativeASCII[i]);
                    BrFar(lAdvance);
                }

                MarkLabel(lPartialMatch);

                Ldloc(_textposV);
                Stloc(testV);

                for (int i = _bmPrefix.Pattern.Length - 2; i >= 0; i--)
                {
                    Label lNext = DefineLabel();
                    int charindex = _code.RightToLeft ?
                        _bmPrefix.Pattern.Length - 1 - i :
                        i;

                    Ldloc(_textV);
                    Ldloc(testV);
                    Ldc(1);
                    Sub(_code.RightToLeft);
                    Dup();
                    Stloc(testV);
                    Callvirt(s_getcharM);
                    if (_bmPrefix.CaseInsensitive)
                    {
                        CallToLower();
                    }

                    Ldc(_bmPrefix.Pattern[charindex]);
                    Beq(lNext);
                    Ldc(_bmPrefix.Positive[charindex]);
                    BrFar(lAdvance);

                    MarkLabel(lNext);
                }

                Ldthis();
                Ldloc(testV);
                if (_code.RightToLeft)
                {
                    Ldc(1);
                    Add();
                }
                Stfld(s_textposF);
                Ldc(1);
                Ret();

                MarkLabel(lFail);

                Ldthis();
                Ldthisfld(_code.RightToLeft ? s_textbegF : s_textendF);
                Stfld(s_textposF);
                Ldc(0);
                Ret();
            }
            else if (!_fcPrefix.HasValue)
            {
                Ldc(1);
                Ret();
            }
            else
            {
                LocalBuilder charInClassV = _tempV;
                LocalBuilder cV = _temp2V;
                Label l1 = DefineLabel();
                Label l2 = DefineLabel();
                Label l3 = DefineLabel();
                Label l4 = DefineLabel();
                Label l5 = DefineLabel();

                Mvfldloc(s_textposF, _textposV);
                Mvfldloc(s_textF, _textV);

                if (!_code!.RightToLeft)
                {
                    Ldthisfld(s_textendF);
                    Ldloc(_textposV);
                }
                else
                {
                    Ldloc(_textposV);
                    Ldthisfld(s_textbegF);
                }
                Sub();
                Stloc(cV);

                Ldloc(cV);
                Ldc(0);
                BleFar(l4);

                MarkLabel(l1);

                Ldloc(cV);
                Ldc(1);
                Sub();
                Stloc(cV);

                if (_code.RightToLeft)
                {
                    Leftcharnext();
                }
                else
                {
                    Rightcharnext();
                }

                if (_fcPrefix.GetValueOrDefault().CaseInsensitive)
                {
                    CallToLower();
                }

                EmitCallCharInClass(_fcPrefix.GetValueOrDefault().Prefix, charInClassV);
                BrtrueFar(l2);

                MarkLabel(l5);

                Ldloc(cV);
                Ldc(0);
                BgtFar(l1);

                Ldc(0);
                BrFar(l3);

                MarkLabel(l2);

                Ldloc(_textposV);
                Ldc(1);
                Sub(_code.RightToLeft);
                Stloc(_textposV);
                Ldc(1);

                MarkLabel(l3);

                Mvlocfld(_textposV, s_textposF);
                Ret();

                MarkLabel(l4);
                Ldc(0);
                Ret();
            }

        }

        /// <summary>Generates a very simple method that sets the _trackcount field.</summary>
        protected void GenerateInitTrackCount()
        {
            Ldthis();
            Ldc(_trackcount);
            Stfld(s_trackcountF);
            Ret();
        }

        /// <summary>Declares a local int.</summary>
        private LocalBuilder DeclareInt() => _ilg!.DeclareLocal(typeof(int));

        /// <summary>Declares a local CultureInfo.</summary>
        private LocalBuilder? DeclareCultureInfo() => _ilg!.DeclareLocal(typeof(CultureInfo)); // cache local variable to avoid unnecessary TLS

        /// <summary>Declares a local int[].</summary>
        private LocalBuilder DeclareIntArray() => _ilg!.DeclareLocal(typeof(int[]));

        /// <summary>Declares a local string.</summary>
        private LocalBuilder DeclareString() => _ilg!.DeclareLocal(typeof(string));

        /// <summary>Generates the code for "RegexRunner.Go".</summary>
        protected void GenerateGo()
        {
            // declare some locals

            _textposV = DeclareInt();
            _textV = DeclareString();
            _trackposV = DeclareInt();
            _trackV = DeclareIntArray();
            _stackposV = DeclareInt();
            _stackV = DeclareIntArray();
            _tempV = DeclareInt();
            _temp2V = DeclareInt();
            _temp3V = DeclareInt();
            if (_hasTimeout)
            {
                _loopV = DeclareInt();
            }
            _textbegV = DeclareInt();
            _textendV = DeclareInt();
            _textstartV = DeclareInt();

            _cultureV = null;
            if (!_options.HasFlag(RegexOptions.CultureInvariant))
            {
                bool needsCulture = _options.HasFlag(RegexOptions.IgnoreCase);
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
                    _cultureV = DeclareCultureInfo();
                }
            }

            // clear some tables

            _labels = null;
            _notes = null;
            _notecount = 0;

            // globally used labels

            _backtrack = DefineLabel();

            // emit the code!

            // cache CultureInfo in local variable which saves excessive thread local storage accesses
            if (_cultureV != null)
            {
                InitLocalCultureInfo();
            }

            GenerateForwardSection();
            GenerateMiddleSection();
            GenerateBacktrackSection();
        }

#if DEBUG
        /// <summary>Debug.WriteLine</summary>
        private static readonly MethodInfo? s_debugWriteLine = typeof(Debug).GetMethod("WriteLine", new Type[] { typeof(string) });

        /// <summary>Debug only: emit code to print out a message.</summary>
        private void Message(string str)
        {
            Ldstr(str);
            Call(s_debugWriteLine!);
        }

#endif

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
            {
                Mvlocfld(_textposV!, s_textposF);
                Mvlocfld(_trackposV!, s_trackposF);
                Mvlocfld(_stackposV!, s_stackposF);
                Ldthis();
                Callvirt(s_dumpstateM);

                var sb = new StringBuilder();
                if (_backpos > 0)
                {
                    sb.AppendFormat("{0:D6} ", _backpos);
                }
                else
                {
                    sb.Append("       ");
                }
                sb.Append(_code!.OpcodeDescription(_codepos));

                if (IsBack())
                {
                    sb.Append(" Back");
                }

                if (IsBack2())
                {
                    sb.Append(" Back2");
                }

                Message(sb.ToString());
            }
#endif
            LocalBuilder charInClassV;

            // Before executing any RegEx code in the unrolled loop,
            // we try checking for the match timeout:

            if (_hasTimeout)
            {
                Ldthis();
                Callvirt(s_checkTimeoutM);
            }

            // Now generate the IL for the RegEx code saved in _regexopcode.
            // We unroll the loop done by the RegexCompiler creating as very long method
            // that is longer if the pattern is longer:

            switch (_regexopcode)
            {
                case RegexCode.Stop:
                    //: return;
                    Mvlocfld(_textposV!, s_textposF);       // update _textpos
                    Ret();
                    break;

                case RegexCode.Nothing:
                    //: break Backward;
                    Back();
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
                    Callvirt(s_ismatchedM);
                    BrfalseFar(_backtrack);
                    break;

                case RegexCode.Lazybranch:
                    //: Track(Textpos());
                    PushTrack(_textposV!);
                    Track();
                    break;

                case RegexCode.Lazybranch | RegexCode.Back:
                    //: Trackframe(1);
                    //: Textto(Tracked(0));
                    //: Goto(Operand(0));
                    PopTrack();
                    Stloc(_textposV!);
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
                    PushStack(_textposV!);
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
                    Dup();
                    Stloc(_textposV!);
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
                        Callvirt(s_ismatchedM);
                        BrfalseFar(_backtrack);
                    }

                    PopStack();
                    Stloc(_tempV!);

                    if (Operand(1) != -1)
                    {
                        Ldthis();
                        Ldc(Operand(0));
                        Ldc(Operand(1));
                        Ldloc(_tempV!);
                        Ldloc(_textposV!);
                        Callvirt(s_transferM);
                    }
                    else
                    {
                        Ldthis();
                        Ldc(Operand(0));
                        Ldloc(_tempV!);
                        Ldloc(_textposV!);
                        Callvirt(s_captureM);
                    }

                    PushTrack(_tempV!);

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
                    Callvirt(s_uncaptureM);
                    if (Operand(0) != -1 && Operand(1) != -1)
                    {
                        Ldthis();
                        Callvirt(s_uncaptureM);
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
                        LocalBuilder mark = _tempV!;
                        Label l1 = DefineLabel();

                        PopStack();
                        Dup();
                        Stloc(mark!);                            // Stacked(0) -> temp
                        PushTrack(mark!);
                        Ldloc(_textposV!);
                        Beq(l1);                                // mark == textpos -> branch

                        // (matched != 0)

                        PushTrack(_textposV!);
                        PushStack(_textposV!);
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
                    Stloc(_textposV!);
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
                        LocalBuilder mark = _tempV!;
                        Label l1 = DefineLabel();
                        Label l2 = DefineLabel();
                        Label l3 = DefineLabel();

                        PopStack();
                        Dup();
                        Stloc(mark!);                      // Stacked(0) -> temp

                        // if (oldMarkPos != -1)
                        Ldloc(mark);
                        Ldc(-1);
                        Beq(l2);                                // mark == -1 -> branch
                        PushTrack(mark);
                        Br(l3);
                        // else
                        MarkLabel(l2);
                        PushTrack(_textposV!);
                        MarkLabel(l3);

                        // if (Textpos() != mark)
                        Ldloc(_textposV!);
                        Beq(l1);                                // mark == textpos -> branch
                        PushTrack(_textposV!);
                        Track();
                        Br(AdvanceLabel());                 // Advance (near)
                                                            // else
                        MarkLabel(l1);
                        ReadyPushStack();                   // push the current textPos on the stack.
                                                            // May be ignored by 'back2' or used by a true empty match.
                        Ldloc(mark);

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
                    Stloc(_textposV!);
                    PushStack(_textposV!);
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
                    PushStack(_textposV!);
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
                        LocalBuilder count = _tempV!;
                        LocalBuilder mark = _temp2V!;
                        Label l1 = DefineLabel();
                        Label l2 = DefineLabel();

                        PopStack();
                        Stloc(count);                           // count -> temp
                        PopStack();
                        Dup();
                        Stloc(mark);                            // mark -> temp2
                        PushTrack(mark);

                        Ldloc(_textposV!);
                        Bne(l1);                                // mark != textpos -> l1
                        Ldloc(count);
                        Ldc(0);
                        Bge(l2);                                // count >= 0 && mark == textpos -> l2

                        MarkLabel(l1);
                        Ldloc(count);
                        Ldc(Operand(1));
                        Bge(l2);                                // count >= Operand(1) -> l2

                        // else
                        PushStack(_textposV!);
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

                        LocalBuilder count = _tempV!;
                        Label l1 = DefineLabel();
                        PopStack();
                        Ldc(1);
                        Sub();
                        Dup();
                        Stloc(count!);
                        Ldc(0);
                        Blt(l1);

                        // if (count >= 0)
                        PopStack();
                        Stloc(_textposV!);
                        PushTrack(count);                       // Tracked(0) is alredy on the track
                        TrackUnique2(Branchcountback2);
                        Advance();

                        // else
                        MarkLabel(l1);
                        ReadyReplaceStack(0);
                        PopTrack();
                        DoReplace();
                        PushStack(count);
                        Back();
                        break;
                    }

                case RegexCode.Branchcount | RegexCode.Back2:
                    //: Trackframe(2);
                    //: Stack(Tracked(0), Tracked(1));      // Recall old mark, old count
                    //: break Backward;                     // Backtrack

                    PopTrack();
                    Stloc(_tempV!);
                    ReadyPushStack();
                    PopTrack();
                    DoPush();
                    PushStack(_tempV!);
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
                        LocalBuilder count = _tempV!;
                        LocalBuilder mark = _temp2V!;
                        Label l1 = DefineLabel();

                        PopStack();
                        Stloc(count);                           // count -> temp
                        PopStack();
                        Stloc(mark);                            // mark -> temp2

                        Ldloc(count);
                        Ldc(0);
                        Bge(l1);                                // count >= 0 -> l1

                        // if (count < 0)
                        PushTrack(mark);
                        PushStack(_textposV!);
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
                        PushTrack(count);
                        PushTrack(_textposV!);
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
                        Label l1 = DefineLabel();
                        LocalBuilder cV = _tempV!;
                        PopTrack();
                        Stloc(_textposV!);
                        PopTrack();
                        Dup();
                        Stloc(cV);
                        Ldc(Operand(1));
                        Bge(l1);                                // Tracked(1) >= Operand(1) -> l1

                        Ldloc(_textposV!);
                        TopTrack();
                        Beq(l1);                                // textpos == mark -> l1

                        PushStack(_textposV!);
                        ReadyPushStack();
                        Ldloc(cV);
                        Ldc(1);
                        Add();
                        DoPush();
                        TrackUnique2(Lazybranchcountback2);
                        Goto(Operand(0));

                        MarkLabel(l1);
                        ReadyPushStack();
                        PopTrack();
                        DoPush();
                        PushStack(cV);
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
                    Ldthisfld(s_trackF);
                    Ldlen();
                    Ldloc(_trackposV!);
                    Sub();
                    DoPush();
                    ReadyPushStack();
                    Ldthis();
                    Callvirt(s_crawlposM);
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

                        PopStack();
                        Ldthisfld(s_trackF);
                        Ldlen();
                        PopStack();
                        Sub();
                        Stloc(_trackposV!);
                        Dup();
                        Ldthis();
                        Callvirt(s_crawlposM);
                        Beq(l2);

                        MarkLabel(l1);
                        Ldthis();
                        Callvirt(s_uncaptureM);
                        Dup();
                        Ldthis();
                        Callvirt(s_crawlposM);
                        Bne(l1);

                        MarkLabel(l2);
                        Pop();
                        Back();
                        break;
                    }

                case RegexCode.Forejump:
                    //: Stackframe(2);
                    //: Trackto(Stacked(0));
                    //: Track(Stacked(1));
                    PopStack();
                    Stloc(_tempV!);
                    Ldthisfld(s_trackF);
                    Ldlen();
                    PopStack();
                    Sub();
                    Stloc(_trackposV!);
                    PushTrack(_tempV!);
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

                        PopTrack();

                        Dup();
                        Ldthis();
                        Callvirt(s_crawlposM);
                        Beq(l2);

                        MarkLabel(l1);
                        Ldthis();
                        Callvirt(s_uncaptureM);
                        Dup();
                        Ldthis();
                        Callvirt(s_crawlposM);
                        Bne(l1);

                        MarkLabel(l2);
                        Pop();
                        Back();
                        break;
                    }

                case RegexCode.Bol:
                    //: if (Leftchars() > 0 && CharAt(Textpos() - 1) != '\n')
                    //:     break Backward;
                    {
                        Label l1 = _labels![NextCodepos()];
                        Ldloc(_textposV!);
                        Ldloc(_textbegV!);
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
                        Ldloc(_textposV!);
                        Ldloc(_textendV!);
                        Bge(l1);
                        Rightchar();
                        Ldc('\n');
                        BneFar(_backtrack);
                        break;
                    }

                case RegexCode.Boundary:
                case RegexCode.Nonboundary:
                    //: if (!IsBoundary(Textpos(), _textbeg, _textend))
                    //:     break Backward;
                    Ldthis();
                    Ldloc(_textposV!);
                    Ldloc(_textbegV!);
                    Ldloc(_textendV!);
                    Callvirt(s_isboundaryM);
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
                    Ldloc(_textposV!);
                    Ldloc(_textbegV!);
                    Ldloc(_textendV!);
                    Callvirt(s_isECMABoundaryM);
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
                    Ldloc(_textposV!);
                    Ldloc(_textbegV!);
                    BgtFar(_backtrack);
                    break;

                case RegexCode.Start:
                    //: if (Textpos() != Textstart())
                    //:    break Backward;
                    Ldloc(_textposV!);
                    Ldthisfld(s_textstartF);
                    BneFar(_backtrack);
                    break;

                case RegexCode.EndZ:
                    //: if (Rightchars() > 1 || Rightchars() == 1 && CharAt(Textpos()) != '\n')
                    //:    break Backward;
                    Ldloc(_textposV!);
                    Ldloc(_textendV!);
                    Ldc(1);
                    Sub();
                    BltFar(_backtrack);
                    Ldloc(_textposV!);
                    Ldloc(_textendV!);
                    Bge(_labels![NextCodepos()]);
                    Rightchar();
                    Ldc('\n');
                    BneFar(_backtrack);
                    break;

                case RegexCode.End:
                    //: if (Rightchars() > 0)
                    //:    break Backward;
                    Ldloc(_textposV!);
                    Ldloc(_textendV!);
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

                    charInClassV = _tempV!;

                    Ldloc(_textposV!);

                    if (!IsRtl())
                    {
                        Ldloc(_textendV!);
                        BgeFar(_backtrack);
                        Rightcharnext();
                    }
                    else
                    {
                        Ldloc(_textbegV!);
                        BleFar(_backtrack);
                        Leftcharnext();
                    }

                    if (IsCi())
                    {
                        CallToLower();
                    }

                    if (Code() == RegexCode.Set)
                    {
                        EmitCallCharInClass(_strings![Operand(0)], charInClassV);
                        BrfalseFar(_backtrack);
                    }
                    else
                    {
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
                        int i;
                        string str;

                        str = _strings![Operand(0)];

                        Ldc(str.Length);
                        Ldloc(_textendV!);
                        Ldloc(_textposV!);
                        Sub();
                        BgtFar(_backtrack);

                        // unroll the string
                        for (i = 0; i < str.Length; i++)
                        {
                            Ldloc(_textV!);
                            Ldloc(_textposV!);
                            if (i != 0)
                            {
                                Ldc(i);
                                Add();
                            }
                            Callvirt(s_getcharM);
                            if (IsCi())
                            {
                                CallToLower();
                            }

                            Ldc(str[i]);
                            BneFar(_backtrack);
                        }

                        Ldloc(_textposV!);
                        Ldc(str.Length);
                        Add();
                        Stloc(_textposV!);
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
                        int i;
                        string str;

                        str = _strings![Operand(0)];

                        Ldc(str.Length);
                        Ldloc(_textposV!);
                        Ldloc(_textbegV!);
                        Sub();
                        BgtFar(_backtrack);

                        // unroll the string
                        for (i = str.Length; i > 0;)
                        {
                            i--;
                            Ldloc(_textV!);
                            Ldloc(_textposV!);
                            Ldc(str.Length - i);
                            Sub();
                            Callvirt(s_getcharM);
                            if (IsCi())
                            {
                                CallToLower();
                            }
                            Ldc(str[i]);
                            BneFar(_backtrack);
                        }

                        Ldloc(_textposV!);
                        Ldc(str.Length);
                        Sub();
                        Stloc(_textposV!);

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
                        LocalBuilder lenV = _tempV!;
                        LocalBuilder indexV = _temp2V!;
                        Label l1 = DefineLabel();

                        Ldthis();
                        Ldc(Operand(0));
                        Callvirt(s_ismatchedM);
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
                        Callvirt(s_matchlengthM);
                        Dup();
                        Stloc(lenV);
                        if (!IsRtl())
                        {
                            Ldloc(_textendV!);
                            Ldloc(_textposV!);
                        }
                        else
                        {
                            Ldloc(_textposV!);
                            Ldloc(_textbegV!);
                        }
                        Sub();
                        BgtFar(_backtrack);         // Matchlength() > Rightchars() -> back

                        Ldthis();
                        Ldc(Operand(0));
                        Callvirt(s_matchindexM);
                        if (!IsRtl())
                        {
                            Ldloc(lenV);
                            Add(IsRtl());
                        }
                        Stloc(indexV);              // index += len

                        Ldloc(_textposV!);
                        Ldloc(lenV);
                        Add(IsRtl());
                        Stloc(_textposV!);           // texpos += len

                        MarkLabel(l1);
                        Ldloc(lenV);
                        Ldc(0);
                        Ble(AdvanceLabel());
                        Ldloc(_textV!);
                        Ldloc(indexV);
                        Ldloc(lenV);
                        if (IsRtl())
                        {
                            Ldc(1);
                            Sub();
                            Dup();
                            Stloc(lenV);
                        }
                        Sub(IsRtl());
                        Callvirt(s_getcharM);
                        if (IsCi())
                        {
                            CallToLower();
                        }

                        Ldloc(_textV!);
                        Ldloc(_textposV!);
                        Ldloc(lenV);
                        if (!IsRtl())
                        {
                            Dup();
                            Ldc(1);
                            Sub();
                            Stloc(lenV);
                        }
                        Sub(IsRtl());
                        Callvirt(s_getcharM);
                        if (IsCi())
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
                        LocalBuilder lenV = _tempV!;
                        charInClassV = _temp2V!;
                        Label l1 = DefineLabel();

                        int c = Operand(1);

                        if (c == 0)
                            break;

                        Ldc(c);
                        if (!IsRtl())
                        {
                            Ldloc(_textendV!);
                            Ldloc(_textposV!);
                        }
                        else
                        {
                            Ldloc(_textposV!);
                            Ldloc(_textbegV!);
                        }
                        Sub();
                        BgtFar(_backtrack);         // Matchlength() > Rightchars() -> back

                        Ldloc(_textposV!);
                        Ldc(c);
                        Add(IsRtl());
                        Stloc(_textposV!);           // texpos += len

                        Ldc(c);
                        Stloc(lenV);

                        MarkLabel(l1);
                        Ldloc(_textV!);
                        Ldloc(_textposV!);
                        Ldloc(lenV);
                        if (IsRtl())
                        {
                            Ldc(1);
                            Sub();
                            Dup();
                            Stloc(lenV);
                            Add();
                        }
                        else
                        {
                            Dup();
                            Ldc(1);
                            Sub();
                            Stloc(lenV);
                            Sub();
                        }
                        Callvirt(s_getcharM);
                        if (IsCi())
                        {
                            CallToLower();
                        }

                        if (Code() == RegexCode.Setrep)
                        {
                            if (_hasTimeout)
                            {
                                EmitTimeoutCheck();
                            }
                            EmitCallCharInClass(_strings![Operand(0)], charInClassV);
                            BrfalseFar(_backtrack);
                        }
                        else
                        {
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
                        Ldloc(lenV);
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
                    //: int c = Operand(1);
                    //: if (c > Rightchars())
                    //:     c = Rightchars();
                    //: char ch = (char)Operand(0);
                    //: int i;
                    //: for (i = c; i > 0; i--)
                    //: {
                    //:     if (Rightcharnext() != ch)
                    //:     {
                    //:         Leftnext();
                    //:         break;
                    //:     }
                    //: }
                    //: if (c > i)
                    //:     Track(c - i - 1, Textpos() - 1);
                    {
                        LocalBuilder cV = _tempV!;
                        LocalBuilder lenV = _temp2V!;
                        charInClassV = _temp3V!;
                        Label l1 = DefineLabel();
                        Label l2 = DefineLabel();

                        int c = Operand(1);
                        if (c == 0)
                        {
                            break;
                        }

                        if (!IsRtl())
                        {
                            Ldloc(_textendV!);
                            Ldloc(_textposV!);
                        }
                        else
                        {
                            Ldloc(_textposV!);
                            Ldloc(_textbegV!);
                        }
                        Sub();
                        if (c != int.MaxValue)
                        {
                            Label l4 = DefineLabel();
                            Dup();
                            Ldc(c);
                            Blt(l4);
                            Pop();
                            Ldc(c);
                            MarkLabel(l4);
                        }
                        Dup();
                        Stloc(lenV);
                        Ldc(1);
                        Add();
                        Stloc(cV);

                        MarkLabel(l1);
                        Ldloc(cV);
                        Ldc(1);
                        Sub();
                        Dup();
                        Stloc(cV);
                        Ldc(0);
                        if (Code() == RegexCode.Setloop)
                        {
                            BleFar(l2);
                        }
                        else
                        {
                            Ble(l2);
                        }

                        if (IsRtl())
                        {
                            Leftcharnext();
                        }
                        else
                        {
                            Rightcharnext();
                        }
                        if (IsCi())
                        {
                            CallToLower();
                        }

                        if (Code() == RegexCode.Setloop)
                        {
                            if (_hasTimeout)
                            {
                                EmitTimeoutCheck();
                            }
                            EmitCallCharInClass(_strings![Operand(0)], charInClassV);
                            BrtrueFar(l1);
                        }
                        else
                        {
                            Ldc(Operand(0));
                            if (Code() == RegexCode.Oneloop)
                            {
                                Beq(l1);
                            }
                            else
                            {
                                Bne(l1);
                            }
                        }

                        Ldloc(_textposV!);
                        Ldc(1);
                        Sub(IsRtl());
                        Stloc(_textposV!);

                        MarkLabel(l2);
                        Ldloc(lenV);
                        Ldloc(cV);
                        Ble(AdvanceLabel());

                        ReadyPushTrack();
                        Ldloc(lenV);
                        Ldloc(cV);
                        Sub();
                        Ldc(1);
                        Sub();
                        DoPush();

                        ReadyPushTrack();
                        Ldloc(_textposV!);
                        Ldc(1);
                        Sub(IsRtl());
                        DoPush();

                        Track();
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
                    Stloc(_textposV!);
                    PopTrack();
                    Stloc(_tempV!);
                    Ldloc(_tempV!);
                    Ldc(0);
                    BleFar(AdvanceLabel());
                    ReadyPushTrack();
                    Ldloc(_tempV!);
                    Ldc(1);
                    Sub();
                    DoPush();
                    ReadyPushTrack();
                    Ldloc(_textposV!);
                    Ldc(1);
                    Sub(IsRtl());
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
                        LocalBuilder cV = _tempV!;

                        int c = Operand(1);
                        if (c == 0)
                        {
                            break;
                        }

                        if (!IsRtl())
                        {
                            Ldloc(_textendV!);
                            Ldloc(_textposV!);
                        }
                        else
                        {
                            Ldloc(_textposV!);
                            Ldloc(_textbegV!);
                        }
                        Sub();
                        if (c != int.MaxValue)
                        {
                            Label l4 = DefineLabel();
                            Dup();
                            Ldc(c);
                            Blt(l4);
                            Pop();
                            Ldc(c);
                            MarkLabel(l4);
                        }
                        Dup();
                        Stloc(cV);
                        Ldc(0);
                        Ble(AdvanceLabel());
                        ReadyPushTrack();
                        Ldloc(cV);
                        Ldc(1);
                        Sub();
                        DoPush();
                        PushTrack(_textposV!);
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

                    charInClassV = _tempV!;

                    PopTrack();
                    Stloc(_textposV!);
                    PopTrack();
                    Stloc(_temp2V!);

                    if (!IsRtl())
                    {
                        Rightcharnext();
                    }
                    else
                    {
                        Leftcharnext();
                    }

                    if (IsCi())
                    {
                        CallToLower();
                    }

                    if (Code() == RegexCode.Setlazy)
                    {
                        EmitCallCharInClass(_strings![Operand(0)], charInClassV);
                        BrfalseFar(_backtrack);
                    }
                    else
                    {
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

                    Ldloc(_temp2V!);
                    Ldc(0);
                    BleFar(AdvanceLabel());
                    ReadyPushTrack();
                    Ldloc(_temp2V!);
                    Ldc(1);
                    Sub();
                    DoPush();
                    PushTrack(_textposV!);
                    Trackagain();
                    Advance();
                    break;

                default:
                    throw new NotImplementedException(SR.UnimplementedState);
            }
        }

        /// <summary>Emits a call to RegexRunner.CharInClass or a functional equivalent.</summary>
        private void EmitCallCharInClass(string charClass, LocalBuilder tempLocal)
        {
            // We need to perform the equivalent of calling RegexRunner.CharInClass(ch, charClass),
            // but that call is relatively expensive.  Before we fall back to it, we try to optimize
            // some common cases for which we can do much better, such as known character classes
            // for which we can call a dedicated method, or a fast-path for ASCII using a lookup table.

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
                    Call(s_charIsDigitM);
                    return;

                case RegexCharClass.NotDigitClass:
                    // !char.IsDigit(ch)
                    Call(s_charIsDigitM);
                    Ldc(0);
                    Ceq();
                    return;

                case RegexCharClass.SpaceClass:
                    // char.IsWhiteSpace(ch)
                    Call(s_charIsWhiteSpaceM);
                    return;

                case RegexCharClass.NotSpaceClass:
                    // !char.IsWhiteSpace(ch)
                    Call(s_charIsWhiteSpaceM);
                    Ldc(0);
                    Ceq();
                    return;
            }

            // Next, handle simple sets of one range, e.g. [A-Z], [0-9], etc.  This includes some built-in classes, like ECMADigitClass.
            if (charClass.Length == RegexCharClass.SetStartIndex + 2 && // one set of two values
                charClass[RegexCharClass.SetLengthIndex] == 2 && // validate we have the right number of ranges
                charClass[RegexCharClass.CategoryLengthIndex] == 0 && // must not have any categories
                charClass[RegexCharClass.SetStartIndex] < charClass[RegexCharClass.SetStartIndex + 1]) // valid range
            {
                // (uint)ch - charClass[3] < charClass[4] - charClass[3]
                Ldc(charClass[RegexCharClass.SetStartIndex]);
                Sub();
                Ldc(charClass[RegexCharClass.SetStartIndex + 1] - charClass[RegexCharClass.SetStartIndex]);
                CltUn();

                // Negate the answer if the negation flag was set
                if (RegexCharClass.IsNegated(charClass))
                {
                    Ldc(0);
                    Ceq();
                }

                return;
            }

            // Next, special-case ASCII inputs.  If the character class contains only ASCII inputs, then we
            // can satisfy the entire operation via a small lookup table, e.g.
            //     ch < 128 && lookup(ch)
            // If the character class contains values outside of the ASCII range, we can still optimize for
            // ASCII inputs, using the table for values < 128, and falling back to calling CharInClass
            // for anything outside of the ASCII range, e.g.
            //     if (ch < 128) lookup(ch)
            //     else ...
            // Either way, we need to generate the lookup table for the ASCII range.
            // We use a const string instead of a byte[] / static data property because
            // it lets IL emit handle all the gory details for us.  It also is ok from an
            // endianness perspective because the compilation happens on the same machine
            // that runs the compiled code.  If that were to ever change, this would need
            // to be revisited. String length is 8 chars == 16 bytes == 128 bits.
            string bitVectorString = string.Create(8, charClass, (dest, charClass) =>
            {
                for (int i = 0; i < 128; i++)
                {
                    if (RegexCharClass.CharInClass((char)i, charClass))
                    {
                        dest[i >> 4] |= (char)(1 << (i & 0xF));
                    }
                }
            });

            // In order to determine whether we need the non-ASCII fallback, we have a few options:
            // 1. Interpret the char class.  This would require fully understanding all of the ins and outs of the design,
            //    and is a lot of code (in the future it's possible the parser could pass this information along).
            // 2. Employ a heuristic to approximate (1), allowing for false positives (saying we need the fallback when
            //    we don't) but no false negatives (saying we don't need the fallback when we do).
            // 3. Evaluate CharInClass on all ~65K inputs.  This is relatively expensive, impacting startup costs.
            // We currently go with (2).  We may sometimes generate a fallback when we don't need one, but the cost of
            // doing so once in a while is minimal.
            bool asciiOnly =
                charClass.Length > RegexCharClass.SetStartIndex &&
                charClass[RegexCharClass.CategoryLengthIndex] == 0 && // if there are any categories, assume there's unicode
                charClass[RegexCharClass.SetLengthIndex] % 2 == 0 && // range limits must come in pairs
                !RegexCharClass.IsNegated(charClass) && // if there's negation, assume there's unicode
                !RegexCharClass.IsSubtraction(charClass); // if it's subtraction, assume there's unicode
            if (asciiOnly)
            {
                for (int i = RegexCharClass.SetStartIndex; i < charClass.Length; i++)
                {
                    if (charClass[i] >= 128) // validate all characters in the set are ASCII
                    {
                        asciiOnly = false;
                        break;
                    }
                }
            }

            Label nonAsciiLabel = DefineLabel(); // jumped to when input is >= 128
            Label doneLabel = DefineLabel(); // jumped to when answer has been computed

            // Store the input character so we can read it multiple times.
            Stloc(tempLocal);

            // ch < 128
            Ldloc(tempLocal);
            Ldc(128);
            Bge(nonAsciiLabel);

            // (bitVectorString[ch >> 4] & (1 << (ch & 0xF))) != 0
            Ldstr(bitVectorString);
            Ldloc(tempLocal);
            Ldc(4);
            Shr();
            Call(s_getcharM);
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
            Br(doneLabel);

            MarkLabel(nonAsciiLabel);
            if (asciiOnly)
            {
                // The whole class was ASCII, so if the character is >= 128, it's not in the class:
                // false
                Ldc(0);
            }
            else
            {
                // The whole class wasn't ASCII, so if the character is >= 128, we need to fall back to calling:
                // CharInClass(ch, charClass)
                Ldloc(tempLocal);
                Ldstr(charClass);
                Call(s_charInClassM);
            }

            MarkLabel(doneLabel);
        }

        /// <summary>Emits a timeout check.</summary>
        private void EmitTimeoutCheck()
        {
            Debug.Assert(_hasTimeout && _loopV != null);

            // Increment counter for each loop iteration.
            Ldloc(_loopV);
            Ldc(1);
            Add();
            Stloc(_loopV);

            // Emit code to check the timeout every 2000th-iteration.
            Label label = DefineLabel();
            Ldloc(_loopV);
            Ldc(LoopTimeoutCheckCount);
            Rem();
            Ldc(0);
            Ceq();
            Brfalse(label);
            Ldthis();
            Callvirt(s_checkTimeoutM);
            MarkLabel(label);
        }
    }
}
