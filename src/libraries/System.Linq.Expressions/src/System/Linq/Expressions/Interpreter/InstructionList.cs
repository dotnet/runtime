// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Enables instruction counting and displaying stats at process exit.
// #define STATS

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Dynamic.Utils;

namespace System.Linq.Expressions.Interpreter
{
    [DebuggerTypeProxy(typeof(InstructionArray.DebugView))]
    internal readonly struct InstructionArray
    {
        internal readonly int MaxStackDepth;
        internal readonly int MaxContinuationDepth;
        internal readonly Instruction[] Instructions;
        internal readonly object[]? Objects;
        internal readonly RuntimeLabel[] Labels;

        // list of (instruction index, cookie) sorted by instruction index:
        internal readonly List<KeyValuePair<int, object?>>? DebugCookies;

        internal InstructionArray(int maxStackDepth, int maxContinuationDepth, Instruction[] instructions,
            object[]? objects, RuntimeLabel[] labels, List<KeyValuePair<int, object?>>? debugCookies)
        {
            MaxStackDepth = maxStackDepth;
            MaxContinuationDepth = maxContinuationDepth;
            Instructions = instructions;
            DebugCookies = debugCookies;
            Objects = objects;
            Labels = labels;
        }

        #region Debug View

        internal sealed class DebugView
        {
            private readonly InstructionArray _array;

            public DebugView(InstructionArray array)
            {
                ArgumentNullException.ThrowIfNull(array);
                _array = array;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public InstructionList.DebugView.InstructionView[]/*!*/ A0 => GetInstructionViews(includeDebugCookies: true);

            public InstructionList.DebugView.InstructionView[] GetInstructionViews(bool includeDebugCookies = false)
            {
                return InstructionList.DebugView.GetInstructionViews(
                    _array.Instructions,
                    _array.Objects,
                    (index) => _array.Labels[index].Index,
                    includeDebugCookies ? _array.DebugCookies : null
                );
            }
        }
        #endregion
    }

    [DebuggerTypeProxy(typeof(InstructionList.DebugView))]
    internal sealed class InstructionList
    {
        private readonly List<Instruction> _instructions = new List<Instruction>();
        private List<object>? _objects;

        private int _currentStackDepth;
        private int _maxStackDepth;
        private int _currentContinuationsDepth;
        private int _maxContinuationDepth;
        private int _runtimeLabelCount;
        private List<BranchLabel>? _labels;

#if DEBUG
        // list of (instruction index, cookie) sorted by instruction index:
        private List<KeyValuePair<int, object?>>? _debugCookies;
#endif
        #region Debug View

        internal sealed class DebugView
        {
            private readonly InstructionList _list;

            public DebugView(InstructionList list)
            {
                ArgumentNullException.ThrowIfNull(list);
                _list = list;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public InstructionView[]/*!*/ A0 => GetInstructionViews(includeDebugCookies: true);

            public InstructionView[] GetInstructionViews(bool includeDebugCookies = false)
            {
                return GetInstructionViews(
                        _list._instructions,
                        _list._objects,
                        (index) => _list._labels![index].TargetIndex,
#if DEBUG
                        includeDebugCookies ? _list._debugCookies :
#endif
                            null
                    );
            }

            internal static InstructionView[] GetInstructionViews(IReadOnlyList<Instruction> instructions, IReadOnlyList<object>? objects,
                Func<int, int> labelIndexer, IReadOnlyList<KeyValuePair<int, object?>>? debugCookies)
            {
                var result = new List<InstructionView>();
                int index = 0;
                int stackDepth = 0;
                int continuationsDepth = 0;

                IEnumerator<KeyValuePair<int, object?>> cookieEnumerator = (debugCookies ?? Array.Empty<KeyValuePair<int, object?>>()).GetEnumerator();
                bool hasCookie = cookieEnumerator.MoveNext();

                for (int i = 0, n = instructions.Count; i < n; i++)
                {
                    Instruction instruction = instructions[i];

                    object? cookie = null;
                    while (hasCookie && cookieEnumerator.Current.Key == i)
                    {
                        cookie = cookieEnumerator.Current.Value;
                        hasCookie = cookieEnumerator.MoveNext();
                    }

                    int stackDiff = instruction.StackBalance;
                    int contDiff = instruction.ContinuationsBalance;
                    string name = instruction.ToDebugString(i, cookie, labelIndexer, objects);
                    result.Add(new InstructionView(instruction, name, i, stackDepth, continuationsDepth));

                    index++;
                    stackDepth += stackDiff;
                    continuationsDepth += contDiff;
                }
                return result.ToArray();
            }

            [DebuggerDisplay("{GetValue(),nq}", Name = "{GetName(),nq}", Type = "{GetDisplayType(), nq}")]
            internal readonly struct InstructionView
            {
                private readonly int _index;
                private readonly int _stackDepth;
                private readonly int _continuationsDepth;
                private readonly string _name;
                private readonly Instruction _instruction;

                internal string GetName()
                {
                    return _index +
                        (_continuationsDepth == 0 ? "" : " C(" + _continuationsDepth + ")") +
                        (_stackDepth == 0 ? "" : " S(" + _stackDepth + ")");
                }

                internal string GetValue()
                {
                    return _name;
                }

                internal string GetDisplayType()
                {
                    return _instruction.ContinuationsBalance + "/" + _instruction.StackBalance;
                }

                public InstructionView(Instruction instruction, string name, int index, int stackDepth, int continuationsDepth)
                {
                    _instruction = instruction;
                    _name = name;
                    _index = index;
                    _stackDepth = stackDepth;
                    _continuationsDepth = continuationsDepth;
                }
            }
        }

        #endregion

        #region Core Emit Ops

        public void Emit(Instruction instruction)
        {
            _instructions.Add(instruction);
            UpdateStackDepth(instruction);
        }

        private void UpdateStackDepth(Instruction instruction)
        {
            Debug.Assert(instruction.ConsumedStack >= 0 && instruction.ProducedStack >= 0 &&
                instruction.ConsumedContinuations >= 0 && instruction.ProducedContinuations >= 0, "bad instruction " + instruction.ToString());

            _currentStackDepth -= instruction.ConsumedStack;
            Debug.Assert(_currentStackDepth >= 0, $"negative stack depth {instruction}");
            _currentStackDepth += instruction.ProducedStack;
            if (_currentStackDepth > _maxStackDepth)
            {
                _maxStackDepth = _currentStackDepth;
            }

            _currentContinuationsDepth -= instruction.ConsumedContinuations;
            Debug.Assert(_currentContinuationsDepth >= 0, $"negative continuations {instruction}");
            _currentContinuationsDepth += instruction.ProducedContinuations;
            if (_currentContinuationsDepth > _maxContinuationDepth)
            {
                _maxContinuationDepth = _currentContinuationsDepth;
            }
        }

        // "Un-emit" the previous instruction.
        // Useful if the instruction was emitted in the calling method, and covers the more usual case.
        // In particular, calling this after an EmitPush() or EmitDup() costs about the same as adding
        // an EmitPop() to undo it at compile time, and leaves a slightly leaner instruction list.
        public void UnEmit()
        {
            Instruction instruction = _instructions[_instructions.Count - 1];
            _instructions.RemoveAt(_instructions.Count - 1);

            _currentContinuationsDepth -= instruction.ProducedContinuations;
            _currentContinuationsDepth += instruction.ConsumedContinuations;
            _currentStackDepth -= instruction.ProducedStack;
            _currentStackDepth += instruction.ConsumedStack;
        }

#pragma warning disable CA1822
        /// <summary>
        /// Attaches a cookie to the last emitted instruction.
        /// </summary>
        [Conditional("DEBUG")]
        public void SetDebugCookie(object? cookie)
        {
#if DEBUG
            _debugCookies ??= new List<KeyValuePair<int, object?>>();

            Debug.Assert(Count > 0);
            _debugCookies.Add(new KeyValuePair<int, object?>(Count - 1, cookie));
#endif
        }
#pragma warning restore CA1822

        public int Count => _instructions.Count;
        public int CurrentStackDepth => _currentStackDepth;
        public int CurrentContinuationsDepth => _currentContinuationsDepth;
        public int MaxStackDepth => _maxStackDepth;

        internal Instruction GetInstruction(int index) => _instructions[index];

#if STATS
        private static Dictionary<string, int> _executedInstructions = new Dictionary<string, int>();
        private static Dictionary<string, Dictionary<object, bool>> _instances = new Dictionary<string, Dictionary<object, bool>>();

        static InstructionList()
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler((_, __) =>
            {
                PerfTrack.DumpHistogram(_executedInstructions);
                Console.WriteLine("-- Total executed: {0}", _executedInstructions.Values.Aggregate(0, (sum, value) => sum + value));
                Console.WriteLine("-----");

                var referenced = new Dictionary<string, int>();
                int total = 0;
                foreach (var entry in _instances)
                {
                    referenced[entry.Key] = entry.Value.Count;
                    total += entry.Value.Count;
                }

                PerfTrack.DumpHistogram(referenced);
                Console.WriteLine("-- Total referenced: {0}", total);
                Console.WriteLine("-----");
            });
        }
#endif
        public InstructionArray ToArray()
        {
#if STATS
            lock (_executedInstructions)
            {
                _instructions.ForEach((instr) =>
                {
                    int value = 0;
                    var name = instr.GetType().Name;
                    _executedInstructions.TryGetValue(name, out value);
                    _executedInstructions[name] = value + 1;

                    Dictionary<object, bool> dict;
                    if (!_instances.TryGetValue(name, out dict))
                    {
                        _instances[name] = dict = new Dictionary<object, bool>();
                    }
                    dict[instr] = true;
                });
            }
#endif
            return new InstructionArray(
                _maxStackDepth,
                _maxContinuationDepth,
                _instructions.ToArray(),
                _objects?.ToArray(),
                BuildRuntimeLabels(),
#if DEBUG
                _debugCookies
#else
                null
#endif
            );
        }

        #endregion

        #region Stack Operations

        private const int PushIntMinCachedValue = -100;
        private const int PushIntMaxCachedValue = 100;
        private const int CachedObjectCount = 256;

        private static Instruction? s_null;
        private static Instruction? s_true;
        private static Instruction? s_false;
        private static Instruction[]? s_Ints;
        private static Instruction[]? s_loadObjectCached;

        public void EmitLoad(object? value)
        {
            EmitLoad(value, type: null);
        }

        public void EmitLoad(bool value)
        {
            if (value)
            {
                Emit(s_true ?? (s_true = new LoadObjectInstruction(Utils.BoxedTrue)));
            }
            else
            {
                Emit(s_false ?? (s_false = new LoadObjectInstruction(Utils.BoxedFalse)));
            }
        }

        public void EmitLoad(object? value, Type? type)
        {
            if (value == null)
            {
                Emit(s_null ?? (s_null = new LoadObjectInstruction(null)));
                return;
            }

            if (type == null || type.IsValueType)
            {
                if (value is bool)
                {
                    EmitLoad((bool)value);
                    return;
                }

                if (value is int)
                {
                    int i = (int)value;
                    if (i >= PushIntMinCachedValue && i <= PushIntMaxCachedValue)
                    {
                        if (s_Ints == null)
                        {
                            s_Ints = new Instruction[PushIntMaxCachedValue - PushIntMinCachedValue + 1];
                        }
                        i -= PushIntMinCachedValue;
                        Emit(s_Ints[i] ?? (s_Ints[i] = new LoadObjectInstruction(value)));
                        return;
                    }
                }
            }

            if (_objects == null)
            {
                _objects = new List<object>();
                if (s_loadObjectCached == null)
                {
                    s_loadObjectCached = new Instruction[CachedObjectCount];
                }
            }

            if (_objects.Count < s_loadObjectCached!.Length)
            {
                uint index = (uint)_objects.Count;
                _objects.Add(value);
                Emit(s_loadObjectCached[index] ?? (s_loadObjectCached[index] = new LoadCachedObjectInstruction(index)));
            }
            else
            {
                Emit(new LoadObjectInstruction(value));
            }
        }

        public void EmitDup()
        {
            Emit(DupInstruction.Instance);
        }

        public void EmitPop()
        {
            Emit(PopInstruction.Instance);
        }

        #endregion

        #region Locals

        internal void SwitchToBoxed(int index, int instructionIndex)
        {
            if (_instructions[instructionIndex] is IBoxableInstruction instruction)
            {
                Instruction? newInstruction = instruction.BoxIfIndexMatches(index);
                if (newInstruction != null)
                {
                    _instructions[instructionIndex] = newInstruction;
                }
            }
        }

        private const int LocalInstrCacheSize = 64;

        private static Instruction[]? s_loadLocal;
        private static Instruction[]? s_loadLocalBoxed;
        private static Instruction[]? s_loadLocalFromClosure;
        private static Instruction[]? s_loadLocalFromClosureBoxed;
        private static Instruction[]? s_assignLocal;
        private static Instruction[]? s_storeLocal;
        private static Instruction[]? s_assignLocalBoxed;
        private static Instruction[]? s_storeLocalBoxed;
        private static Instruction[]? s_assignLocalToClosure;

        public void EmitLoadLocal(int index)
        {
            if (s_loadLocal == null)
            {
                s_loadLocal = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_loadLocal.Length)
            {
                Emit(s_loadLocal[index] ?? (s_loadLocal[index] = new LoadLocalInstruction(index)));
            }
            else
            {
                Emit(new LoadLocalInstruction(index));
            }
        }

        public void EmitLoadLocalBoxed(int index)
        {
            Emit(LoadLocalBoxed(index));
        }

        internal static Instruction LoadLocalBoxed(int index)
        {
            if (s_loadLocalBoxed == null)
            {
                s_loadLocalBoxed = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_loadLocalBoxed.Length)
            {
                return s_loadLocalBoxed[index] ?? (s_loadLocalBoxed[index] = new LoadLocalBoxedInstruction(index));
            }
            else
            {
                return new LoadLocalBoxedInstruction(index);
            }
        }

        public void EmitLoadLocalFromClosure(int index)
        {
            if (s_loadLocalFromClosure == null)
            {
                s_loadLocalFromClosure = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_loadLocalFromClosure.Length)
            {
                Emit(s_loadLocalFromClosure[index] ?? (s_loadLocalFromClosure[index] = new LoadLocalFromClosureInstruction(index)));
            }
            else
            {
                Emit(new LoadLocalFromClosureInstruction(index));
            }
        }

        public void EmitLoadLocalFromClosureBoxed(int index)
        {
            if (s_loadLocalFromClosureBoxed == null)
            {
                s_loadLocalFromClosureBoxed = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_loadLocalFromClosureBoxed.Length)
            {
                Emit(s_loadLocalFromClosureBoxed[index] ?? (s_loadLocalFromClosureBoxed[index] = new LoadLocalFromClosureBoxedInstruction(index)));
            }
            else
            {
                Emit(new LoadLocalFromClosureBoxedInstruction(index));
            }
        }

        public void EmitAssignLocal(int index)
        {
            if (s_assignLocal == null)
            {
                s_assignLocal = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_assignLocal.Length)
            {
                Emit(s_assignLocal[index] ?? (s_assignLocal[index] = new AssignLocalInstruction(index)));
            }
            else
            {
                Emit(new AssignLocalInstruction(index));
            }
        }

        public void EmitStoreLocal(int index)
        {
            if (s_storeLocal == null)
            {
                s_storeLocal = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_storeLocal.Length)
            {
                Emit(s_storeLocal[index] ?? (s_storeLocal[index] = new StoreLocalInstruction(index)));
            }
            else
            {
                Emit(new StoreLocalInstruction(index));
            }
        }

        public void EmitAssignLocalBoxed(int index)
        {
            Emit(AssignLocalBoxed(index));
        }

        internal static Instruction AssignLocalBoxed(int index)
        {
            if (s_assignLocalBoxed == null)
            {
                s_assignLocalBoxed = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_assignLocalBoxed.Length)
            {
                return s_assignLocalBoxed[index] ?? (s_assignLocalBoxed[index] = new AssignLocalBoxedInstruction(index));
            }
            else
            {
                return new AssignLocalBoxedInstruction(index);
            }
        }

        public void EmitStoreLocalBoxed(int index)
        {
            Emit(StoreLocalBoxed(index));
        }

        internal static Instruction StoreLocalBoxed(int index)
        {
            if (s_storeLocalBoxed == null)
            {
                s_storeLocalBoxed = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_storeLocalBoxed.Length)
            {
                return s_storeLocalBoxed[index] ?? (s_storeLocalBoxed[index] = new StoreLocalBoxedInstruction(index));
            }
            else
            {
                return new StoreLocalBoxedInstruction(index);
            }
        }

        public void EmitAssignLocalToClosure(int index)
        {
            if (s_assignLocalToClosure == null)
            {
                s_assignLocalToClosure = new Instruction[LocalInstrCacheSize];
            }

            if (index < s_assignLocalToClosure.Length)
            {
                Emit(s_assignLocalToClosure[index] ?? (s_assignLocalToClosure[index] = new AssignLocalToClosureInstruction(index)));
            }
            else
            {
                Emit(new AssignLocalToClosureInstruction(index));
            }
        }

        public void EmitStoreLocalToClosure(int index)
        {
            EmitAssignLocalToClosure(index);
            EmitPop();
        }

        public void EmitInitializeLocal(int index, Type type)
        {
            object? value = ScriptingRuntimeHelpers.GetPrimitiveDefaultValue(type);
            if (value != null)
            {
                Emit(new InitializeLocalInstruction.ImmutableValue(index, value));
            }
            else if (type.IsValueType)
            {
                Emit(new InitializeLocalInstruction.MutableValue(index, type));
            }
            else
            {
                Emit(InitReference(index));
            }
        }

        internal void EmitInitializeParameter(int index)
        {
            Emit(Parameter(index));
        }

        internal static Instruction Parameter(int index)
        {
            return new InitializeLocalInstruction.Parameter(index);
        }

        internal static Instruction ParameterBox(int index)
        {
            return new InitializeLocalInstruction.ParameterBox(index);
        }

        internal static Instruction InitReference(int index)
        {
            return new InitializeLocalInstruction.Reference(index);
        }

        internal static Instruction InitImmutableRefBox(int index)
        {
            return new InitializeLocalInstruction.ImmutableRefBox(index);
        }

        public void EmitNewRuntimeVariables(int count)
        {
            Emit(new RuntimeVariablesInstruction(count));
        }

        #endregion

        #region Array Operations

        public void EmitGetArrayItem()
        {
            Emit(GetArrayItemInstruction.Instance);
        }

        public void EmitSetArrayItem()
        {
            Emit(SetArrayItemInstruction.Instance);
        }

        public void EmitNewArray(Type elementType)
        {
            Emit(new NewArrayInstruction(elementType));
        }

        public void EmitNewArrayBounds(Type elementType, int rank)
        {
            Emit(new NewArrayBoundsInstruction(elementType, rank));
        }

        public void EmitNewArrayInit(Type elementType, int elementCount)
        {
            Emit(new NewArrayInitInstruction(elementType, elementCount));
        }

        #endregion

        #region Arithmetic Operations

        public void EmitAdd(Type type, bool @checked)
        {
            Emit(@checked ? AddOvfInstruction.Create(type) : AddInstruction.Create(type));
        }

        public void EmitSub(Type type, bool @checked)
        {
            Emit(@checked ? SubOvfInstruction.Create(type) : SubInstruction.Create(type));
        }

        public void EmitMul(Type type, bool @checked)
        {
            Emit(@checked ? MulOvfInstruction.Create(type) : MulInstruction.Create(type));
        }

        public void EmitDiv(Type type)
        {
            Emit(DivInstruction.Create(type));
        }

        public void EmitModulo(Type type)
        {
            Emit(ModuloInstruction.Create(type));
        }

        #endregion

        #region Comparisons

        public void EmitExclusiveOr(Type type)
        {
            Emit(ExclusiveOrInstruction.Create(type));
        }

        public void EmitAnd(Type type)
        {
            Emit(AndInstruction.Create(type));
        }

        public void EmitOr(Type type)
        {
            Emit(OrInstruction.Create(type));
        }

        public void EmitLeftShift(Type type)
        {
            Emit(LeftShiftInstruction.Create(type));
        }

        public void EmitRightShift(Type type)
        {
            Emit(RightShiftInstruction.Create(type));
        }

        public void EmitEqual(Type type, bool liftedToNull = false)
        {
            Emit(EqualInstruction.Create(type, liftedToNull));
        }

        public void EmitNotEqual(Type type, bool liftedToNull = false)
        {
            Emit(NotEqualInstruction.Create(type, liftedToNull));
        }

        public void EmitLessThan(Type type, bool liftedToNull)
        {
            Emit(LessThanInstruction.Create(type, liftedToNull));
        }

        public void EmitLessThanOrEqual(Type type, bool liftedToNull)
        {
            Emit(LessThanOrEqualInstruction.Create(type, liftedToNull));
        }

        public void EmitGreaterThan(Type type, bool liftedToNull)
        {
            Emit(GreaterThanInstruction.Create(type, liftedToNull));
        }

        public void EmitGreaterThanOrEqual(Type type, bool liftedToNull)
        {
            Emit(GreaterThanOrEqualInstruction.Create(type, liftedToNull));
        }

        #endregion

        #region Conversions

        public void EmitNumericConvertChecked(TypeCode from, TypeCode to, bool isLiftedToNull)
        {
            Emit(new NumericConvertInstruction.Checked(from, to, isLiftedToNull));
        }

        public void EmitNumericConvertUnchecked(TypeCode from, TypeCode to, bool isLiftedToNull)
        {
            Emit(new NumericConvertInstruction.Unchecked(from, to, isLiftedToNull));
        }

        public void EmitConvertToUnderlying(TypeCode to, bool isLiftedToNull)
        {
            Emit(new NumericConvertInstruction.ToUnderlying(to, isLiftedToNull));
        }

        public void EmitCast(Type toType)
        {
            Emit(CastInstruction.Create(toType));
        }

        public void EmitCastToEnum(Type toType)
        {
            Emit(new CastToEnumInstruction(toType));
        }

        public void EmitCastReferenceToEnum(Type toType)
        {
            Debug.Assert(_instructions[_instructions.Count - 1] == NullCheckInstruction.Instance);
            Emit(new CastReferenceToEnumInstruction(toType));
        }

        #endregion

        #region Boolean Operators

        public void EmitNot(Type type)
        {
            Emit(NotInstruction.Create(type));
        }

        #endregion

        #region Types

        public void EmitDefaultValue(Type type)
        {
            Emit(new DefaultValueInstruction(type));
        }

        public void EmitNew(ConstructorInfo constructorInfo, ParameterInfo[] parameters)
        {
            Emit(new NewInstruction(constructorInfo, parameters.Length));
        }

        public void EmitByRefNew(ConstructorInfo constructorInfo, ParameterInfo[] parameters, ByRefUpdater[] updaters)
        {
            Emit(new ByRefNewInstruction(constructorInfo, parameters.Length, updaters));
        }

        internal void EmitCreateDelegate(LightDelegateCreator creator)
        {
            Emit(new CreateDelegateInstruction(creator));
        }

        public void EmitTypeEquals()
        {
            Emit(TypeEqualsInstruction.Instance);
        }

        public void EmitArrayLength()
        {
            Emit(ArrayLengthInstruction.Instance);
        }

        public void EmitNegate(Type type)
        {
            Emit(NegateInstruction.Create(type));
        }

        public void EmitNegateChecked(Type type)
        {
            Emit(NegateCheckedInstruction.Create(type));
        }

        public void EmitIncrement(Type type)
        {
            Emit(IncrementInstruction.Create(type));
        }

        public void EmitDecrement(Type type)
        {
            Emit(DecrementInstruction.Create(type));
        }

        public void EmitTypeIs(Type type)
        {
            Emit(new TypeIsInstruction(type));
        }

        public void EmitTypeAs(Type type)
        {
            Emit(new TypeAsInstruction(type));
        }

        #endregion

        #region Fields and Methods

        private static readonly Dictionary<FieldInfo, Instruction> s_loadFields = new Dictionary<FieldInfo, Instruction>();

        public void EmitLoadField(FieldInfo field)
        {
            Emit(GetLoadField(field));
        }

        private static Instruction GetLoadField(FieldInfo field)
        {
            lock (s_loadFields)
            {
                if (!s_loadFields.TryGetValue(field, out Instruction? instruction))
                {
                    if (field.IsStatic)
                    {
                        instruction = new LoadStaticFieldInstruction(field);
                    }
                    else
                    {
                        instruction = new LoadFieldInstruction(field);
                    }
                    s_loadFields.Add(field, instruction);
                }
                return instruction;
            }
        }

        public void EmitStoreField(FieldInfo field)
        {
            if (field.IsStatic)
            {
                Emit(new StoreStaticFieldInstruction(field));
            }
            else
            {
                Emit(new StoreFieldInstruction(field));
            }
        }

        public void EmitCall(MethodInfo method)
        {
            EmitCall(method, method.GetParametersCached());
        }

        public void EmitCall(MethodInfo method, ParameterInfo[] parameters)
        {
            Emit(CallInstruction.Create(method, parameters));
        }

        public void EmitByRefCall(MethodInfo method, ParameterInfo[] parameters, ByRefUpdater[] byrefArgs)
        {
            Emit(new ByRefMethodInfoCallInstruction(method, method.IsStatic ? parameters.Length : parameters.Length + 1, byrefArgs));
        }

        public void EmitNullableCall(MethodInfo method, ParameterInfo[] parameters)
        {
            Emit(NullableMethodCallInstruction.Create(method.Name, parameters.Length, method));
        }

        #endregion

        #region Control Flow

        private static readonly RuntimeLabel[] s_emptyRuntimeLabels = new RuntimeLabel[] { new RuntimeLabel(Interpreter.RethrowOnReturn, 0, 0) };

        private RuntimeLabel[] BuildRuntimeLabels()
        {
            if (_runtimeLabelCount == 0)
            {
                return s_emptyRuntimeLabels;
            }

            var result = new RuntimeLabel[_runtimeLabelCount + 1];
            foreach (BranchLabel label in _labels!)
            {
                if (label.HasRuntimeLabel)
                {
                    result[label.LabelIndex] = label.ToRuntimeLabel();
                }
            }
            // "return and rethrow" label:
            result[result.Length - 1] = new RuntimeLabel(Interpreter.RethrowOnReturn, 0, 0);
            return result;
        }

        public BranchLabel MakeLabel()
        {
            if (_labels == null)
            {
                _labels = new List<BranchLabel>();
            }

            var label = new BranchLabel();
            _labels.Add(label);
            return label;
        }

        internal void FixupBranch(int branchIndex, int offset)
        {
            _instructions[branchIndex] = ((OffsetInstruction)_instructions[branchIndex]).Fixup(offset);
        }

        private int EnsureLabelIndex(BranchLabel label)
        {
            if (label.HasRuntimeLabel)
            {
                return label.LabelIndex;
            }

            label.LabelIndex = _runtimeLabelCount;
            _runtimeLabelCount++;
            return label.LabelIndex;
        }

        public int MarkRuntimeLabel()
        {
            BranchLabel handlerLabel = MakeLabel();
            MarkLabel(handlerLabel);
            return EnsureLabelIndex(handlerLabel);
        }

        public void MarkLabel(BranchLabel label)
        {
            label.Mark(this);
        }

        public void EmitGoto(BranchLabel label, bool hasResult, bool hasValue, bool labelTargetGetsValue)
        {
            Emit(GotoInstruction.Create(EnsureLabelIndex(label), hasResult, hasValue, labelTargetGetsValue));
        }

        private void EmitBranch(OffsetInstruction instruction, BranchLabel label)
        {
            Emit(instruction);
            label.AddBranch(this, Count - 1);
        }

        public void EmitBranch(BranchLabel label)
        {
            EmitBranch(new BranchInstruction(), label);
        }

        public void EmitBranch(BranchLabel label, bool hasResult, bool hasValue)
        {
            EmitBranch(new BranchInstruction(hasResult, hasValue), label);
        }

        public void EmitCoalescingBranch(BranchLabel leftNotNull)
        {
            EmitBranch(new CoalescingBranchInstruction(), leftNotNull);
        }

        public void EmitBranchTrue(BranchLabel elseLabel)
        {
            EmitBranch(new BranchTrueInstruction(), elseLabel);
        }

        public void EmitBranchFalse(BranchLabel elseLabel)
        {
            EmitBranch(new BranchFalseInstruction(), elseLabel);
        }

        public void EmitThrow()
        {
            Emit(ThrowInstruction.Throw);
        }

        public void EmitThrowVoid()
        {
            Emit(ThrowInstruction.VoidThrow);
        }

        public void EmitRethrow()
        {
            Emit(ThrowInstruction.Rethrow);
        }

        public void EmitRethrowVoid()
        {
            Emit(ThrowInstruction.VoidRethrow);
        }

        public void EmitEnterTryFinally(BranchLabel finallyStartLabel)
        {
            Emit(EnterTryCatchFinallyInstruction.CreateTryFinally(EnsureLabelIndex(finallyStartLabel)));
        }

        public void EmitEnterTryCatch()
        {
            Emit(EnterTryCatchFinallyInstruction.CreateTryCatch());
        }

        public EnterTryFaultInstruction EmitEnterTryFault(BranchLabel tryEnd)
        {
            var instruction = new EnterTryFaultInstruction(EnsureLabelIndex(tryEnd));
            Emit(instruction);
            return instruction;
        }

        public void EmitEnterFinally(BranchLabel finallyStartLabel)
        {
            Emit(EnterFinallyInstruction.Create(EnsureLabelIndex(finallyStartLabel)));
        }

        public void EmitLeaveFinally()
        {
            Emit(LeaveFinallyInstruction.Instance);
        }

        public void EmitEnterFault(BranchLabel faultStartLabel)
        {
            Emit(EnterFaultInstruction.Create(EnsureLabelIndex(faultStartLabel)));
        }

        public void EmitLeaveFault()
        {
            Emit(LeaveFaultInstruction.Instance);
        }

        public void EmitEnterExceptionFilter()
        {
            Emit(EnterExceptionFilterInstruction.Instance);
        }

        public void EmitLeaveExceptionFilter()
        {
            Emit(LeaveExceptionFilterInstruction.Instance);
        }

        public void EmitEnterExceptionHandlerNonVoid()
        {
            Emit(EnterExceptionHandlerInstruction.NonVoid);
        }

        public void EmitEnterExceptionHandlerVoid()
        {
            Emit(EnterExceptionHandlerInstruction.Void);
        }

        public void EmitLeaveExceptionHandler(bool hasValue, BranchLabel tryExpressionEndLabel)
        {
            Emit(LeaveExceptionHandlerInstruction.Create(EnsureLabelIndex(tryExpressionEndLabel), hasValue));
        }

        public void EmitIntSwitch<T>(Dictionary<T, int> cases) where T : notnull
        {
            Emit(new IntSwitchInstruction<T>(cases));
        }

        public void EmitStringSwitch(Dictionary<string, int> cases, StrongBox<int> nullCase)
        {
            Emit(new StringSwitchInstruction(cases, nullCase));
        }

        #endregion
    }
}
