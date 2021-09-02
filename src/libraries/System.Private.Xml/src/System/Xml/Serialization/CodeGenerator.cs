// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Serialization.Configuration;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml.Extensions;

namespace System.Xml.Serialization
{
    internal sealed class CodeGenerator
    {
        internal const BindingFlags InstancePublicBindingFlags = BindingFlags.Instance | BindingFlags.Public;
        internal const BindingFlags InstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal const BindingFlags StaticBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        internal const MethodAttributes PublicMethodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig;
        internal const MethodAttributes PublicOverrideMethodAttributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig;
        internal const MethodAttributes ProtectedOverrideMethodAttributes = MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig;
        internal const MethodAttributes PrivateMethodAttributes = MethodAttributes.Private | MethodAttributes.HideBySig;

        private readonly TypeBuilder _typeBuilder;
        private MethodBuilder? _methodBuilder;
        private ILGenerator? _ilGen;
        private Dictionary<string, ArgBuilder>? _argList;
        private LocalScope? _currentScope;
        // Stores a queue of free locals available in the context of the method, keyed by
        // type and name of the local
        private Dictionary<(Type, string), Queue<LocalBuilder>>? _freeLocals;
        private Stack<object>? _blockStack;
        private Label _methodEndLabel;

        internal CodeGenerator(TypeBuilder typeBuilder)
        {
            System.Diagnostics.Debug.Assert(typeBuilder != null);
            _typeBuilder = typeBuilder;
        }

        internal static bool IsNullableGenericType(Type type)
        {
            return type.Name == "Nullable`1";
        }

        [Conditional("DEBUG")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Debug only code, we don't ship debug binaries.")]
        internal static void AssertHasInterface(Type type, Type iType)
        {
            Debug.Assert(iType.IsInterface);
            foreach (Type iFace in type.GetInterfaces())
            {
                if (iFace == iType)
                    return;
            }
            Debug.Fail("Interface not found");
        }

        internal void BeginMethod(Type returnType, string methodName, Type[] argTypes, string[] argNames, MethodAttributes methodAttributes)
        {
            _methodBuilder = _typeBuilder.DefineMethod(methodName, methodAttributes, returnType, argTypes);
            _ilGen = _methodBuilder.GetILGenerator();
            InitILGeneration(argTypes, argNames, (_methodBuilder.Attributes & MethodAttributes.Static) == MethodAttributes.Static);
        }

        internal void BeginMethod(Type returnType, MethodBuilderInfo methodBuilderInfo, Type[] argTypes, string[] argNames, MethodAttributes methodAttributes)
        {
#if DEBUG
            methodBuilderInfo.Validate(returnType, argTypes, methodAttributes);
#endif
            _methodBuilder = methodBuilderInfo.MethodBuilder;
            _ilGen = _methodBuilder.GetILGenerator();
            InitILGeneration(argTypes, argNames, (_methodBuilder.Attributes & MethodAttributes.Static) == MethodAttributes.Static);
        }

        private void InitILGeneration(Type[] argTypes, string[] argNames, bool isStatic)
        {
            _methodEndLabel = _ilGen!.DefineLabel();
            this.retLabel = _ilGen.DefineLabel();
            _blockStack = new Stack<object>();
            _whileStack = new Stack<WhileState>();
            _currentScope = new LocalScope();
            _freeLocals = new Dictionary<(Type, string), Queue<LocalBuilder>>();
            _argList = new Dictionary<string, ArgBuilder>();
            // this ptr is arg 0 for non static, assuming ref type (not value type)
            if (!isStatic)
                _argList.Add("this", new ArgBuilder("this", 0, _typeBuilder.BaseType!));
            for (int i = 0; i < argTypes.Length; i++)
            {
                ArgBuilder arg = new ArgBuilder(argNames[i], _argList.Count, argTypes[i]);
                _argList.Add(arg.Name, arg);
                _methodBuilder!.DefineParameter(arg.Index, ParameterAttributes.None, arg.Name);
            }
        }

        internal MethodBuilder? EndMethod()
        {
            MarkLabel(_methodEndLabel);
            Ret();

            MethodBuilder? retVal = null;
            retVal = _methodBuilder;
            _methodBuilder = null;
            _ilGen = null;
            _freeLocals = null;
            _blockStack = null;
            _whileStack = null;
            _argList = null;
            _currentScope = null;
            retLocal = null;
            return retVal;
        }

        internal MethodBuilder? MethodBuilder
        {
            get { return _methodBuilder; }
        }

        internal ArgBuilder GetArg(string name)
        {
            System.Diagnostics.Debug.Assert(_argList != null && _argList.ContainsKey(name));
            return (ArgBuilder)_argList[name];
        }

        internal LocalBuilder GetLocal(string name)
        {
            System.Diagnostics.Debug.Assert(_currentScope != null && _currentScope.ContainsKey(name));
            return _currentScope[name]!;
        }

        internal LocalBuilder? retLocal;
        internal Label retLabel;
        internal LocalBuilder ReturnLocal
        {
            get
            {
                if (retLocal == null)
                    retLocal = DeclareLocal(_methodBuilder!.ReturnType, "_ret");
                return retLocal;
            }
        }
        internal Label ReturnLabel
        {
            get { return retLabel; }
        }

        private readonly Dictionary<Type, LocalBuilder> _tmpLocals = new Dictionary<Type, LocalBuilder>();
        internal LocalBuilder GetTempLocal(Type type)
        {
            LocalBuilder? localTmp;
            if (!_tmpLocals.TryGetValue(type, out localTmp))
            {
                localTmp = DeclareLocal(type, "_tmp" + _tmpLocals.Count);
                _tmpLocals.Add(type, localTmp);
            }
            return localTmp;
        }

        internal Type GetVariableType(object var)
        {
            if (var is ArgBuilder)
                return ((ArgBuilder)var).ArgType;
            else if (var is LocalBuilder)
                return ((LocalBuilder)var).LocalType;
            else
                return var.GetType();
        }

        internal object GetVariable(string name)
        {
            object? var;
            if (TryGetVariable(name, out var))
                return var;
            System.Diagnostics.Debug.Fail("Variable not found");
            return null;
        }

        internal bool TryGetVariable(string name, [NotNullWhen(true)] out object? variable)
        {
            LocalBuilder? loc;
            if (_currentScope != null && _currentScope.TryGetValue(name, out loc))
            {
                variable = loc;
                return true;
            }
            ArgBuilder? arg;
            if (_argList != null && _argList.TryGetValue(name, out arg))
            {
                variable = arg;
                return true;
            }
            int val;
            if (int.TryParse(name, out val))
            {
                variable = val;
                return true;
            }
            variable = null;
            return false;
        }

        internal void EnterScope()
        {
            LocalScope newScope = new LocalScope(_currentScope);
            _currentScope = newScope;
        }

        internal void ExitScope()
        {
            Debug.Assert(_currentScope!.parent != null);
            _currentScope.AddToFreeLocals(_freeLocals!);
            _currentScope = _currentScope.parent;
        }

        private bool TryDequeueLocal(Type type, string name, [NotNullWhen(true)] out LocalBuilder? local)
        {
            // This method can only be called between BeginMethod and EndMethod (i.e.
            // while we are emitting code for a method
            Debug.Assert(_freeLocals != null);

            Queue<LocalBuilder>? freeLocalQueue;
            (Type, string) key = (type, name);
            if (_freeLocals.TryGetValue(key, out freeLocalQueue))
            {
                local = freeLocalQueue.Dequeue();

                // If the queue is empty, remove this key from the dictionary
                // of free locals
                if (freeLocalQueue.Count == 0)
                {
                    _freeLocals.Remove(key);
                }
                return true;
            }
            local = null;
            return false;
        }

        internal LocalBuilder DeclareLocal(Type type, string name)
        {
            Debug.Assert(!_currentScope!.ContainsKey(name));
            LocalBuilder? local;
            if (!TryDequeueLocal(type, name, out local))
            {
                local = _ilGen!.DeclareLocal(type, false);
            }
            _currentScope[name] = local;
            return local;
        }

        internal LocalBuilder DeclareOrGetLocal(Type type, string name)
        {
            LocalBuilder? local;
            if (!_currentScope!.TryGetValue(name, out local))
                local = DeclareLocal(type, name);
            else
                Debug.Assert(local.LocalType == type);
            return local;
        }

        internal object For(LocalBuilder local, object start, object end)
        {
            ForState forState = new ForState(local, DefineLabel(), DefineLabel(), end);
            if (forState.Index != null)
            {
                Load(start);
                Stloc(forState.Index);
                Br(forState.TestLabel);
            }
            MarkLabel(forState.BeginLabel);
            _blockStack!.Push(forState);
            return forState;
        }

        internal void EndFor()
        {
            object stackTop = _blockStack!.Pop();
            ForState? forState = stackTop as ForState;
            Debug.Assert(forState != null);
            if (forState.Index != null)
            {
                Ldloc(forState.Index);
                Ldc(1);
                Add();
                Stloc(forState.Index);
                MarkLabel(forState.TestLabel);
                Ldloc(forState.Index);
                Load(forState.End);
                Type varType = GetVariableType(forState.End);
                if (varType.IsArray)
                {
                    Ldlen();
                }
                else
                {
#if DEBUG
                    CodeGenerator.AssertHasInterface(varType, typeof(ICollection));
#endif
                    MethodInfo ICollection_get_Count = typeof(ICollection).GetMethod(
                          "get_Count",
                          CodeGenerator.InstanceBindingFlags,
                          Type.EmptyTypes
                          )!;
                    Call(ICollection_get_Count);
                }
                Blt(forState.BeginLabel);
            }
            else
                Br(forState.BeginLabel);
        }

        internal void If()
        {
            InternalIf(false);
        }

        internal void IfNot()
        {
            InternalIf(true);
        }

        private static readonly OpCode[] s_branchCodes = new OpCode[] {
            OpCodes.Bge,
            OpCodes.Bne_Un,
            OpCodes.Bgt,
            OpCodes.Ble,
            OpCodes.Beq,
            OpCodes.Blt,
        };

        private OpCode GetBranchCode(Cmp cmp)
        {
            return s_branchCodes[(int)cmp];
        }

        internal void If(Cmp cmpOp)
        {
            IfState ifState = new IfState();
            ifState.EndIf = DefineLabel();
            ifState.ElseBegin = DefineLabel();
            _ilGen!.Emit(GetBranchCode(cmpOp), ifState.ElseBegin);
            _blockStack!.Push(ifState);
        }

        internal void If(object value1, Cmp cmpOp, object? value2)
        {
            Load(value1);
            Load(value2);
            If(cmpOp);
        }

        internal void Else()
        {
            IfState ifState = PopIfState();
            Br(ifState.EndIf);
            MarkLabel(ifState.ElseBegin);

            ifState.ElseBegin = ifState.EndIf;
            _blockStack!.Push(ifState);
        }

        internal void EndIf()
        {
            IfState ifState = PopIfState();
            if (!ifState.ElseBegin.Equals(ifState.EndIf))
                MarkLabel(ifState.ElseBegin);
            MarkLabel(ifState.EndIf);
        }

        private readonly Stack<Label> _leaveLabels = new Stack<Label>();
        internal void BeginExceptionBlock()
        {
            _leaveLabels.Push(DefineLabel());
            _ilGen!.BeginExceptionBlock();
        }

        internal void BeginCatchBlock(Type exception)
        {
            _ilGen!.BeginCatchBlock(exception);
        }

        internal void EndExceptionBlock()
        {
            _ilGen!.EndExceptionBlock();
            _ilGen.MarkLabel(_leaveLabels.Pop());
        }

        internal void Leave()
        {
            _ilGen!.Emit(OpCodes.Leave, _leaveLabels.Peek());
        }

        internal void Call(MethodInfo methodInfo)
        {
            Debug.Assert(methodInfo != null);
            if (methodInfo.IsVirtual && !methodInfo.DeclaringType!.IsValueType)
                _ilGen!.Emit(OpCodes.Callvirt, methodInfo);
            else
                _ilGen!.Emit(OpCodes.Call, methodInfo);
        }

        internal void Call(ConstructorInfo ctor)
        {
            Debug.Assert(ctor != null);
            _ilGen!.Emit(OpCodes.Call, ctor);
        }

        internal void New(ConstructorInfo constructorInfo)
        {
            Debug.Assert(constructorInfo != null);
            _ilGen!.Emit(OpCodes.Newobj, constructorInfo);
        }

        internal void InitObj(Type valueType)
        {
            _ilGen!.Emit(OpCodes.Initobj, valueType);
        }

        internal void NewArray(Type elementType, object len)
        {
            Load(len);
            _ilGen!.Emit(OpCodes.Newarr, elementType);
        }

        internal void LoadArrayElement(object obj, object arrayIndex)
        {
            Type objType = GetVariableType(obj).GetElementType()!;
            Load(obj);
            Load(arrayIndex);
            if (IsStruct(objType))
            {
                Ldelema(objType);
                Ldobj(objType);
            }
            else
                Ldelem(objType);
        }

        internal void StoreArrayElement(object obj, object arrayIndex, object value)
        {
            Type arrayType = GetVariableType(obj);
            if (arrayType == typeof(Array))
            {
                Load(obj);
                Call(typeof(Array).GetMethod("SetValue", new Type[] { typeof(object), typeof(int) })!);
            }
            else
            {
                Type objType = arrayType.GetElementType()!;
                Load(obj);
                Load(arrayIndex);
                if (IsStruct(objType))
                    Ldelema(objType);
                Load(value);
                ConvertValue(GetVariableType(value), objType);
                if (IsStruct(objType))
                    Stobj(objType);
                else
                    Stelem(objType);
            }
        }

        private static bool IsStruct(Type objType)
        {
            return objType.IsValueType && !objType.IsPrimitive;
        }

        [RequiresUnreferencedCode("calls LoadMember")]
        internal Type LoadMember(object obj, MemberInfo memberInfo)
        {
            if (GetVariableType(obj).IsValueType)
                LoadAddress(obj);
            else
                Load(obj);
            return LoadMember(memberInfo);
        }

        [RequiresUnreferencedCode("GetProperty on PropertyInfo type's base type")]
        private static MethodInfo? GetPropertyMethodFromBaseType(PropertyInfo propertyInfo, bool isGetter)
        {
            // we only invoke this when the propertyInfo does not have a GET or SET method on it

            Type? currentType = propertyInfo.DeclaringType!.BaseType;
            PropertyInfo? currentProperty;
            string propertyName = propertyInfo.Name;
            MethodInfo? result = null;

            while (currentType != null)
            {
                currentProperty = currentType.GetProperty(propertyName);

                if (currentProperty != null)
                {
                    if (isGetter)
                    {
                        result = currentProperty.GetMethod;
                    }
                    else
                    {
                        result = currentProperty.SetMethod;
                    }

                    if (result != null)
                    {
                        // we found the GetMethod/SetMethod on the type closest to the current declaring type
                        break;
                    }
                }

                // keep looking at the base type like compiler does
                currentType = currentType.BaseType;
            }

            return result;
        }

        [RequiresUnreferencedCode("calls GetPropertyMethodFromBaseType")]
        internal Type LoadMember(MemberInfo memberInfo)
        {
            Type? memberType = null;
            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                memberType = fieldInfo.FieldType;
                if (fieldInfo.IsStatic)
                {
                    _ilGen!.Emit(OpCodes.Ldsfld, fieldInfo);
                }
                else
                {
                    _ilGen!.Emit(OpCodes.Ldfld, fieldInfo);
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(memberInfo is PropertyInfo);
                PropertyInfo property = (PropertyInfo)memberInfo;
                memberType = property.PropertyType;
                if (property != null)
                {
                    MethodInfo? getMethod = property.GetMethod;

                    if (getMethod == null)
                    {
                        getMethod = GetPropertyMethodFromBaseType(property, true);
                    }

                    System.Diagnostics.Debug.Assert(getMethod != null);
                    Call(getMethod);
                }
            }

            return memberType;
        }

        [RequiresUnreferencedCode("calls GetPropertyMethodFromBaseType")]
        internal Type LoadMemberAddress(MemberInfo memberInfo)
        {
            Type? memberType = null;
            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                memberType = fieldInfo.FieldType;
                if (fieldInfo.IsStatic)
                {
                    _ilGen!.Emit(OpCodes.Ldsflda, fieldInfo);
                }
                else
                {
                    _ilGen!.Emit(OpCodes.Ldflda, fieldInfo);
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(memberInfo is PropertyInfo);
                PropertyInfo property = (PropertyInfo)memberInfo;
                memberType = property.PropertyType;
                if (property != null)
                {
                    MethodInfo? getMethod = property.GetMethod;

                    if (getMethod == null)
                    {
                        getMethod = GetPropertyMethodFromBaseType(property, true);
                    }

                    System.Diagnostics.Debug.Assert(getMethod != null);
                    Call(getMethod);

                    LocalBuilder tmpLoc = GetTempLocal(memberType);
                    Stloc(tmpLoc);
                    Ldloca(tmpLoc);
                }
            }

            return memberType;
        }

        [RequiresUnreferencedCode("calls GetPropertyMethodFromBaseType")]
        internal void StoreMember(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo)
            {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                if (fieldInfo.IsStatic)
                {
                    _ilGen!.Emit(OpCodes.Stsfld, fieldInfo);
                }
                else
                {
                    _ilGen!.Emit(OpCodes.Stfld, fieldInfo);
                }
            }
            else
            {
                System.Diagnostics.Debug.Assert(memberInfo is PropertyInfo);
                PropertyInfo property = (PropertyInfo)memberInfo;
                if (property != null)
                {
                    MethodInfo? setMethod = property.SetMethod;

                    if (setMethod == null)
                    {
                        setMethod = GetPropertyMethodFromBaseType(property, false);
                    }

                    System.Diagnostics.Debug.Assert(setMethod != null);
                    Call(setMethod);
                }
            }
        }

        internal void Load(object? obj)
        {
            if (obj == null)
                _ilGen!.Emit(OpCodes.Ldnull);
            else if (obj is ArgBuilder)
                Ldarg((ArgBuilder)obj);
            else if (obj is LocalBuilder)
                Ldloc((LocalBuilder)obj);
            else
                Ldc(obj);
        }

        internal void LoadAddress(object obj)
        {
            if (obj is ArgBuilder)
                LdargAddress((ArgBuilder)obj);
            else if (obj is LocalBuilder)
                LdlocAddress((LocalBuilder)obj);
            else
                Load(obj);
        }


        internal void ConvertAddress(Type source, Type target)
        {
            InternalConvert(source, target, true);
        }

        internal void ConvertValue(Type source, Type target)
        {
            InternalConvert(source, target, false);
        }

        internal void Castclass(Type target)
        {
            _ilGen!.Emit(OpCodes.Castclass, target);
        }

        internal void Box(Type type)
        {
            _ilGen!.Emit(OpCodes.Box, type);
        }

        internal void Unbox(Type type)
        {
            _ilGen!.Emit(OpCodes.Unbox, type);
        }

        private static readonly OpCode[] s_ldindOpCodes = new OpCode[] {
            OpCodes.Nop, //Empty = 0,
            OpCodes.Nop, //Object = 1,
            OpCodes.Nop, //DBNull = 2,
            OpCodes.Ldind_I1, //Boolean = 3,
            OpCodes.Ldind_I2, //Char = 4,
            OpCodes.Ldind_I1, //SByte = 5,
            OpCodes.Ldind_U1, //Byte = 6,
            OpCodes.Ldind_I2, //Int16 = 7,
            OpCodes.Ldind_U2, //UInt16 = 8,
            OpCodes.Ldind_I4, //Int32 = 9,
            OpCodes.Ldind_U4, //UInt32 = 10,
            OpCodes.Ldind_I8, //Int64 = 11,
            OpCodes.Ldind_I8, //UInt64 = 12,
            OpCodes.Ldind_R4, //Single = 13,
            OpCodes.Ldind_R8, //Double = 14,
            OpCodes.Nop, //Decimal = 15,
            OpCodes.Nop, //DateTime = 16,
            OpCodes.Nop, //17
            OpCodes.Ldind_Ref, //String = 18,
        };


        private OpCode GetLdindOpCode(TypeCode typeCode)
        {
            return s_ldindOpCodes[(int)typeCode];
        }

        internal void Ldobj(Type type)
        {
            OpCode opCode = GetLdindOpCode(Type.GetTypeCode(type));
            if (!opCode.Equals(OpCodes.Nop))
            {
                _ilGen!.Emit(opCode);
            }
            else
            {
                _ilGen!.Emit(OpCodes.Ldobj, type);
            }
        }

        internal void Stobj(Type type)
        {
            _ilGen!.Emit(OpCodes.Stobj, type);
        }

        internal void Ceq()
        {
            _ilGen!.Emit(OpCodes.Ceq);
        }

        internal void Clt()
        {
            _ilGen!.Emit(OpCodes.Clt);
        }

        internal void Cne()
        {
            Ceq();
            Ldc(0);
            Ceq();
        }

        internal void Ble(Label label)
        {
            _ilGen!.Emit(OpCodes.Ble, label);
        }

        internal void Throw()
        {
            _ilGen!.Emit(OpCodes.Throw);
        }

        internal void Ldtoken(Type t)
        {
            _ilGen!.Emit(OpCodes.Ldtoken, t);
        }

        internal void Ldc(object o)
        {
            Type valueType = o.GetType();
            if (o is Type)
            {
                Ldtoken((Type)o);
                Call(typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(RuntimeTypeHandle) })!);
            }
            else if (valueType.IsEnum)
            {
                Ldc(Convert.ChangeType(o, Enum.GetUnderlyingType(valueType), null));
            }
            else
            {
                switch (Type.GetTypeCode(valueType))
                {
                    case TypeCode.Boolean:
                        Ldc((bool)o);
                        break;
                    case TypeCode.Char:
                        Debug.Fail("Char is not a valid schema primitive and should be treated as int in DataContract");
                        throw new NotSupportedException(SR.XmlInvalidCharSchemaPrimitive);
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        Ldc(Convert.ToInt32(o, CultureInfo.InvariantCulture));
                        break;
                    case TypeCode.Int32:
                        Ldc((int)o);
                        break;
                    case TypeCode.UInt32:
                        Ldc((int)(uint)o);
                        break;
                    case TypeCode.UInt64:
                        Ldc((long)(ulong)o);
                        break;
                    case TypeCode.Int64:
                        Ldc((long)o);
                        break;
                    case TypeCode.Single:
                        Ldc((float)o);
                        break;
                    case TypeCode.Double:
                        Ldc((double)o);
                        break;
                    case TypeCode.String:
                        Ldstr((string)o);
                        break;
                    case TypeCode.Decimal:
                        ConstructorInfo Decimal_ctor = typeof(decimal).GetConstructor(
                             CodeGenerator.InstanceBindingFlags,
                             new Type[] { typeof(int), typeof(int), typeof(int), typeof(bool), typeof(byte) }
                             )!;
                        int[] bits = decimal.GetBits((decimal)o);
                        Ldc(bits[0]); // digit
                        Ldc(bits[1]); // digit
                        Ldc(bits[2]); // digit
                        Ldc((bits[3] & 0x80000000) == 0x80000000); // sign
                        Ldc((byte)((bits[3] >> 16) & 0xFF)); // decimal location
                        New(Decimal_ctor);
                        break;
                    case TypeCode.DateTime:
                        ConstructorInfo DateTime_ctor = typeof(DateTime).GetConstructor(
                            CodeGenerator.InstanceBindingFlags,
                            new Type[] { typeof(long) }
                            )!;
                        Ldc(((DateTime)o).Ticks); // ticks
                        New(DateTime_ctor);
                        break;
                    case TypeCode.Object:
                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                    default:
                        if (valueType == typeof(TimeSpan))
                        {
                            ConstructorInfo TimeSpan_ctor = typeof(TimeSpan).GetConstructor(
                            CodeGenerator.InstanceBindingFlags,
                            null,
                            new Type[] { typeof(long) },
                            null
                            )!;
                            Ldc(((TimeSpan)o).Ticks); // ticks
                            New(TimeSpan_ctor);
                            break;
                        }
                        else if (valueType == typeof(DateTimeOffset))
                        {
                            ConstructorInfo DateTimeOffset_ctor = typeof(DateTimeOffset).GetConstructor(
                            CodeGenerator.InstanceBindingFlags,
                            null,
                            new Type[] { typeof(long), typeof(TimeSpan) },
                            null
                            )!;
                            Ldc(((DateTimeOffset)o).Ticks); // ticks
                            Ldc(((DateTimeOffset)o).Offset); // offset
                            New(DateTimeOffset_ctor);
                            break;
                        }
                        else
                        {
                            throw new NotSupportedException(SR.Format(SR.UnknownConstantType, valueType.AssemblyQualifiedName));
                        }
                }
            }
        }

        internal void Ldc(bool boolVar)
        {
            if (boolVar)
            {
                _ilGen!.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                _ilGen!.Emit(OpCodes.Ldc_I4_0);
            }
        }

        internal void Ldc(int intVar)
        {
            _ilGen!.Emit(OpCodes.Ldc_I4, intVar);
        }

        internal void Ldc(long l)
        {
            _ilGen!.Emit(OpCodes.Ldc_I8, l);
        }

        internal void Ldc(float f)
        {
            _ilGen!.Emit(OpCodes.Ldc_R4, f);
        }

        internal void Ldc(double d)
        {
            _ilGen!.Emit(OpCodes.Ldc_R8, d);
        }

        internal void Ldstr(string? strVar)
        {
            if (strVar == null)
                _ilGen!.Emit(OpCodes.Ldnull);
            else
                _ilGen!.Emit(OpCodes.Ldstr, strVar);
        }

        internal void LdlocAddress(LocalBuilder localBuilder)
        {
            if (localBuilder.LocalType.IsValueType)
                Ldloca(localBuilder);
            else
                Ldloc(localBuilder);
        }

        internal void Ldloc(LocalBuilder localBuilder)
        {
            _ilGen!.Emit(OpCodes.Ldloc, localBuilder);
        }

        internal void Ldloc(string name)
        {
            Debug.Assert(_currentScope!.ContainsKey(name));
            LocalBuilder local = _currentScope[name]!;
            Ldloc(local);
        }

        internal void Stloc(Type type, string name)
        {
            LocalBuilder? local = null;
            if (!_currentScope!.TryGetValue(name, out local))
            {
                local = DeclareLocal(type, name);
            }
            Debug.Assert(local.LocalType == type);
            Stloc(local);
        }

        internal void Stloc(LocalBuilder local)
        {
            _ilGen!.Emit(OpCodes.Stloc, local);
        }

        internal void Ldloc(Type type, string name)
        {
            Debug.Assert(_currentScope!.ContainsKey(name));
            LocalBuilder local = _currentScope[name]!;
            Debug.Assert(local.LocalType == type);
            Ldloc(local);
        }

        internal void Ldloca(LocalBuilder localBuilder)
        {
            _ilGen!.Emit(OpCodes.Ldloca, localBuilder);
        }

        internal void LdargAddress(ArgBuilder argBuilder)
        {
            if (argBuilder.ArgType.IsValueType)
                Ldarga(argBuilder);
            else
                Ldarg(argBuilder);
        }

        internal void Ldarg(string arg)
        {
            Ldarg(GetArg(arg));
        }

        internal void Ldarg(ArgBuilder arg)
        {
            Ldarg(arg.Index);
        }

        internal void Ldarg(int slot)
        {
            _ilGen!.Emit(OpCodes.Ldarg, slot);
        }

        internal void Ldarga(ArgBuilder argBuilder)
        {
            Ldarga(argBuilder.Index);
        }

        internal void Ldarga(int slot)
        {
            _ilGen!.Emit(OpCodes.Ldarga, slot);
        }

        internal void Ldlen()
        {
            _ilGen!.Emit(OpCodes.Ldlen);
            _ilGen.Emit(OpCodes.Conv_I4);
        }

        private static readonly OpCode[] s_ldelemOpCodes = new OpCode[] {
            OpCodes.Nop, //Empty = 0,
            OpCodes.Ldelem_Ref, //Object = 1,
            OpCodes.Ldelem_Ref, //DBNull = 2,
            OpCodes.Ldelem_I1, //Boolean = 3,
            OpCodes.Ldelem_I2, //Char = 4,
            OpCodes.Ldelem_I1, //SByte = 5,
            OpCodes.Ldelem_U1, //Byte = 6,
            OpCodes.Ldelem_I2, //Int16 = 7,
            OpCodes.Ldelem_U2, //UInt16 = 8,
            OpCodes.Ldelem_I4, //Int32 = 9,
            OpCodes.Ldelem_U4, //UInt32 = 10,
            OpCodes.Ldelem_I8, //Int64 = 11,
            OpCodes.Ldelem_I8, //UInt64 = 12,
            OpCodes.Ldelem_R4, //Single = 13,
            OpCodes.Ldelem_R8, //Double = 14,
            OpCodes.Nop, //Decimal = 15,
            OpCodes.Nop, //DateTime = 16,
            OpCodes.Nop, //17
            OpCodes.Ldelem_Ref, //String = 18,
        };

        private OpCode GetLdelemOpCode(TypeCode typeCode)
        {
            return s_ldelemOpCodes[(int)typeCode];
        }

        internal void Ldelem(Type arrayElementType)
        {
            if (arrayElementType.IsEnum)
            {
                Ldelem(Enum.GetUnderlyingType(arrayElementType));
            }
            else
            {
                OpCode opCode = GetLdelemOpCode(Type.GetTypeCode(arrayElementType));
                Debug.Assert(!opCode.Equals(OpCodes.Nop));
                if (opCode.Equals(OpCodes.Nop))
                    throw new InvalidOperationException(SR.Format(SR.ArrayTypeIsNotSupported, arrayElementType.AssemblyQualifiedName));
                _ilGen!.Emit(opCode);
            }
        }
        internal void Ldelema(Type arrayElementType)
        {
            OpCode opCode = OpCodes.Ldelema;
            _ilGen!.Emit(opCode, arrayElementType);
        }

        private static readonly OpCode[] s_stelemOpCodes = new OpCode[] {
            OpCodes.Nop, //Empty = 0,
            OpCodes.Stelem_Ref, //Object = 1,
            OpCodes.Stelem_Ref, //DBNull = 2,
            OpCodes.Stelem_I1, //Boolean = 3,
            OpCodes.Stelem_I2, //Char = 4,
            OpCodes.Stelem_I1, //SByte = 5,
            OpCodes.Stelem_I1, //Byte = 6,
            OpCodes.Stelem_I2, //Int16 = 7,
            OpCodes.Stelem_I2, //UInt16 = 8,
            OpCodes.Stelem_I4, //Int32 = 9,
            OpCodes.Stelem_I4, //UInt32 = 10,
            OpCodes.Stelem_I8, //Int64 = 11,
            OpCodes.Stelem_I8, //UInt64 = 12,
            OpCodes.Stelem_R4, //Single = 13,
            OpCodes.Stelem_R8, //Double = 14,
            OpCodes.Nop, //Decimal = 15,
            OpCodes.Nop, //DateTime = 16,
            OpCodes.Nop, //17
            OpCodes.Stelem_Ref, //String = 18,
        };

        private OpCode GetStelemOpCode(TypeCode typeCode)
        {
            return s_stelemOpCodes[(int)typeCode];
        }

        internal void Stelem(Type arrayElementType)
        {
            if (arrayElementType.IsEnum)
                Stelem(Enum.GetUnderlyingType(arrayElementType));
            else
            {
                OpCode opCode = GetStelemOpCode(Type.GetTypeCode(arrayElementType));
                if (opCode.Equals(OpCodes.Nop))
                    throw new InvalidOperationException(SR.Format(SR.ArrayTypeIsNotSupported, arrayElementType.AssemblyQualifiedName));
                _ilGen!.Emit(opCode);
            }
        }

        internal Label DefineLabel()
        {
            return _ilGen!.DefineLabel();
        }

        internal void MarkLabel(Label label)
        {
            _ilGen!.MarkLabel(label);
        }

        internal void Nop()
        {
            _ilGen!.Emit(OpCodes.Nop);
        }

        internal void Add()
        {
            _ilGen!.Emit(OpCodes.Add);
        }

        internal void Ret()
        {
            _ilGen!.Emit(OpCodes.Ret);
        }

        internal void Br(Label label)
        {
            _ilGen!.Emit(OpCodes.Br, label);
        }

        internal void Br_S(Label label)
        {
            _ilGen!.Emit(OpCodes.Br_S, label);
        }

        internal void Blt(Label label)
        {
            _ilGen!.Emit(OpCodes.Blt, label);
        }

        internal void Brfalse(Label label)
        {
            _ilGen!.Emit(OpCodes.Brfalse, label);
        }

        internal void Brtrue(Label label)
        {
            _ilGen!.Emit(OpCodes.Brtrue, label);
        }

        internal void Pop()
        {
            _ilGen!.Emit(OpCodes.Pop);
        }

        internal void Dup()
        {
            _ilGen!.Emit(OpCodes.Dup);
        }

        private void InternalIf(bool negate)
        {
            IfState ifState = new IfState();
            ifState.EndIf = DefineLabel();
            ifState.ElseBegin = DefineLabel();
            if (negate)
                Brtrue(ifState.ElseBegin);
            else
                Brfalse(ifState.ElseBegin);
            _blockStack!.Push(ifState);
        }

        private static readonly OpCode[] s_convOpCodes = new OpCode[] {
            OpCodes.Nop, //Empty = 0,
            OpCodes.Nop, //Object = 1,
            OpCodes.Nop, //DBNull = 2,
            OpCodes.Conv_I1, //Boolean = 3,
            OpCodes.Conv_I2, //Char = 4,
            OpCodes.Conv_I1, //SByte = 5,
            OpCodes.Conv_U1, //Byte = 6,
            OpCodes.Conv_I2, //Int16 = 7,
            OpCodes.Conv_U2, //UInt16 = 8,
            OpCodes.Conv_I4, //Int32 = 9,
            OpCodes.Conv_U4, //UInt32 = 10,
            OpCodes.Conv_I8, //Int64 = 11,
            OpCodes.Conv_U8, //UInt64 = 12,
            OpCodes.Conv_R4, //Single = 13,
            OpCodes.Conv_R8, //Double = 14,
            OpCodes.Nop, //Decimal = 15,
            OpCodes.Nop, //DateTime = 16,
            OpCodes.Nop, //17
            OpCodes.Nop, //String = 18,
        };

        private OpCode GetConvOpCode(TypeCode typeCode)
        {
            return s_convOpCodes[(int)typeCode];
        }

        private void InternalConvert(Type source, Type target, bool isAddress)
        {
            if (target == source)
                return;
            if (target.IsValueType)
            {
                if (source.IsValueType)
                {
                    OpCode opCode = GetConvOpCode(Type.GetTypeCode(target));
                    if (opCode.Equals(OpCodes.Nop))
                    {
                        throw new CodeGeneratorConversionException(source, target, isAddress, "NoConversionPossibleTo");
                    }
                    else
                    {
                        _ilGen!.Emit(opCode);
                    }
                }
                else if (source.IsAssignableFrom(target))
                {
                    Unbox(target);
                    if (!isAddress)
                        Ldobj(target);
                }
                else
                {
                    throw new CodeGeneratorConversionException(source, target, isAddress, "IsNotAssignableFrom");
                }
            }
            else if (target.IsAssignableFrom(source))
            {
                if (source.IsValueType)
                {
                    if (isAddress)
                        Ldobj(source);
                    Box(source);
                }
            }
            else if (source.IsAssignableFrom(target))
            {
                Castclass(target);
            }
            else if (target.IsInterface || source.IsInterface)
            {
                Castclass(target);
            }
            else
            {
                throw new CodeGeneratorConversionException(source, target, isAddress, "IsNotAssignableFrom");
            }
        }

        private IfState PopIfState()
        {
            object stackTop = _blockStack!.Pop();
            IfState? ifState = stackTop as IfState;
            Debug.Assert(ifState != null);
            return ifState;
        }

        internal static AssemblyBuilder CreateAssemblyBuilder(string name)
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = name;
            assemblyName.Version = new Version(1, 0, 0, 0);
            return AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        }

        internal static ModuleBuilder CreateModuleBuilder(AssemblyBuilder assemblyBuilder, string name)
        {
            return assemblyBuilder.DefineDynamicModule(name);
        }

        internal static TypeBuilder CreateTypeBuilder(
            ModuleBuilder moduleBuilder,
            string name,
            TypeAttributes attributes,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type parent,
            Type[] interfaces)
        {
            // parent is nullable if no base class
            return moduleBuilder.DefineType(TempAssembly.GeneratedAssemblyNamespace + "." + name,
                attributes, parent, interfaces);
        }

        private int _initElseIfStack = -1;
        private IfState? _elseIfState;
        internal void InitElseIf()
        {
            Debug.Assert(_initElseIfStack == -1);
            _elseIfState = (IfState)_blockStack!.Pop();
            _initElseIfStack = _blockStack.Count;
            Br(_elseIfState.EndIf);
            MarkLabel(_elseIfState.ElseBegin);
        }

        private int _initIfStack = -1;
        internal void InitIf()
        {
            Debug.Assert(_initIfStack == -1);
            _initIfStack = _blockStack!.Count;
        }

        internal void AndIf(Cmp cmpOp)
        {
            if (_initIfStack == _blockStack!.Count)
            {
                _initIfStack = -1;
                If(cmpOp);
                return;
            }
            if (_initElseIfStack == _blockStack.Count)
            {
                _initElseIfStack = -1;
                _elseIfState!.ElseBegin = DefineLabel();
                _ilGen!.Emit(GetBranchCode(cmpOp), _elseIfState.ElseBegin);
                _blockStack.Push(_elseIfState);
                return;
            }
            Debug.Assert(_initIfStack == -1 && _initElseIfStack == -1);
            IfState ifState = (IfState)_blockStack.Peek();
            _ilGen!.Emit(GetBranchCode(cmpOp), ifState.ElseBegin);
        }

        internal void AndIf()
        {
            if (_initIfStack == _blockStack!.Count)
            {
                _initIfStack = -1;
                If();
                return;
            }
            if (_initElseIfStack == _blockStack.Count)
            {
                _initElseIfStack = -1;
                _elseIfState!.ElseBegin = DefineLabel();
                Brfalse(_elseIfState.ElseBegin);
                _blockStack.Push(_elseIfState);
                return;
            }
            Debug.Assert(_initIfStack == -1 && _initElseIfStack == -1);
            IfState ifState = (IfState)_blockStack.Peek();
            Brfalse(ifState.ElseBegin);
        }

        internal void IsInst(Type type)
        {
            _ilGen!.Emit(OpCodes.Isinst, type);
        }

        internal void Beq(Label label)
        {
            _ilGen!.Emit(OpCodes.Beq, label);
        }

        internal void Bne(Label label)
        {
            _ilGen!.Emit(OpCodes.Bne_Un, label);
        }

        internal void GotoMethodEnd()
        {
            //limit to only short forward (127 CIL instruction)
            //Br_S(this.methodEndLabel);
            Br(_methodEndLabel);
        }

        internal sealed class WhileState
        {
            public Label StartLabel;
            public Label CondLabel;
            public Label EndLabel;
            public WhileState(CodeGenerator ilg)
            {
                StartLabel = ilg.DefineLabel();
                CondLabel = ilg.DefineLabel();
                EndLabel = ilg.DefineLabel();
            }
        }

        // Usage:
        // WhileBegin()
        //  WhileBreak()
        //  WhileContinue()
        // WhileBeginCondition()
        // (bool on stack)
        // WhileEndCondition()
        // WhileEnd()
        private Stack<WhileState>? _whileStack;
        internal void WhileBegin()
        {
            WhileState whileState = new WhileState(this);
            // ECMA-335 III.1.7.5 states that code blocks after unconditional
            // branch which could only be reached by backward branch are assumed
            // to have empty stack on the entrance.
            //
            // Since we don't have control over the current stack contents here
            // we have to generate conditional branch here instead of
            // Br(whileState.CondLabel) to make the IL verifiable.
            Ldc(true);
            Brtrue(whileState.CondLabel);
            MarkLabel(whileState.StartLabel);
            _whileStack!.Push(whileState);
        }

        internal void WhileEnd()
        {
            WhileState whileState = _whileStack!.Pop();
            MarkLabel(whileState.EndLabel);
        }

        internal void WhileContinue()
        {
            WhileState whileState = _whileStack!.Peek();
            Br(whileState.CondLabel);
        }

        internal void WhileBeginCondition()
        {
            WhileState whileState = _whileStack!.Peek();
            // If there are two MarkLabel ILs consecutively, Labels will converge to one label.
            // This could cause the code to look different.  We insert Nop here specifically
            // that the While label stands out.
            Nop();
            MarkLabel(whileState.CondLabel);
        }

        internal void WhileEndCondition()
        {
            WhileState whileState = _whileStack!.Peek();
            Brtrue(whileState.StartLabel);
        }
    }


    internal sealed class ArgBuilder
    {
        internal string Name;
        internal int Index;
        internal Type ArgType;
        internal ArgBuilder(string name, int index, Type argType)
        {
            this.Name = name;
            this.Index = index;
            this.ArgType = argType;
        }
    }

    internal sealed class ForState
    {
        private readonly LocalBuilder _indexVar;
        private readonly Label _beginLabel;
        private readonly Label _testLabel;
        private readonly object _end;

        internal ForState(LocalBuilder indexVar, Label beginLabel, Label testLabel, object end)
        {
            _indexVar = indexVar;
            _beginLabel = beginLabel;
            _testLabel = testLabel;
            _end = end;
        }

        internal LocalBuilder Index
        {
            get
            {
                return _indexVar;
            }
        }

        internal Label BeginLabel
        {
            get
            {
                return _beginLabel;
            }
        }

        internal Label TestLabel
        {
            get
            {
                return _testLabel;
            }
        }

        internal object End
        {
            get
            {
                return _end;
            }
        }
    }

    internal enum Cmp : int
    {
        LessThan = 0,
        EqualTo,
        LessThanOrEqualTo,
        GreaterThan,
        NotEqualTo,
        GreaterThanOrEqualTo
    }

    internal sealed class IfState
    {
        private Label _elseBegin;
        private Label _endIf;

        internal Label EndIf
        {
            get
            {
                return _endIf;
            }
            set
            {
                _endIf = value;
            }
        }

        internal Label ElseBegin
        {
            get
            {
                return _elseBegin;
            }
            set
            {
                _elseBegin = value;
            }
        }
    }

    internal sealed class LocalScope
    {
        public readonly LocalScope? parent;
        private readonly Dictionary<string, LocalBuilder> _locals;

        // Root scope
        public LocalScope()
        {
            _locals = new Dictionary<string, LocalBuilder>();
        }

        public LocalScope(LocalScope? parent) : this()
        {
            this.parent = parent;
        }

        public bool ContainsKey(string key)
        {
            return _locals.ContainsKey(key) || (parent != null && parent.ContainsKey(key));
        }

        public bool TryGetValue(string key, [NotNullWhen(true)] out LocalBuilder? value)
        {
            if (_locals.TryGetValue(key, out value))
            {
                return true;
            }
            else if (parent != null)
            {
                return parent.TryGetValue(key, out value);
            }
            else
            {
                value = null;
                return false;
            }
        }

        [DisallowNull]
        public LocalBuilder? this[string key]
        {
            get
            {
                LocalBuilder? value;
                TryGetValue(key, out value);
                return value;
            }
            set
            {
                _locals[key] = value;
            }
        }

        public void AddToFreeLocals(Dictionary<(Type, string), Queue<LocalBuilder>> freeLocals)
        {
            foreach (var item in _locals)
            {
                (Type, string) key = (item.Value.LocalType, item.Key);
                Queue<LocalBuilder>? freeLocalQueue;
                if (freeLocals.TryGetValue(key, out freeLocalQueue))
                {
                    // Add to end of the queue so that it will be re-used in
                    // FIFO manner
                    freeLocalQueue.Enqueue(item.Value);
                }
                else
                {
                    freeLocalQueue = new Queue<LocalBuilder>();
                    freeLocalQueue.Enqueue(item.Value);
                    freeLocals.Add(key, freeLocalQueue);
                }
            }
        }
    }

    internal sealed class MethodBuilderInfo
    {
        public readonly MethodBuilder MethodBuilder;
        public readonly Type[] ParameterTypes;
        public MethodBuilderInfo(MethodBuilder methodBuilder, Type[] parameterTypes)
        {
            this.MethodBuilder = methodBuilder;
            this.ParameterTypes = parameterTypes;
        }

        public void Validate(Type? returnType, Type[] parameterTypes, MethodAttributes attributes)
        {
#if DEBUG
            Debug.Assert(this.MethodBuilder.ReturnType == returnType);
            Debug.Assert(this.MethodBuilder.Attributes == attributes);
            Debug.Assert(this.ParameterTypes.Length == parameterTypes.Length);
            for (int i = 0; i < parameterTypes.Length; ++i)
            {
                Debug.Assert(this.ParameterTypes[i] == parameterTypes[i]);
            }
#endif
        }
    }

    internal sealed class CodeGeneratorConversionException : Exception
    {
        private readonly Type _sourceType;
        private readonly Type _targetType;
        private readonly bool _isAddress;
        private readonly string _reason;

        public CodeGeneratorConversionException(Type sourceType, Type targetType, bool isAddress, string reason)
            : base()
        {
            _sourceType = sourceType;
            _targetType = targetType;
            _isAddress = isAddress;
            _reason = reason;
        }
    }
}
