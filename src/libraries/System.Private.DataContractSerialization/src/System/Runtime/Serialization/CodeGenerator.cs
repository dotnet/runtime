// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Security;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.Json;

namespace System.Runtime.Serialization
{
    internal sealed class CodeGenerator
    {
        private static MethodInfo? s_getTypeFromHandle;
        private static MethodInfo GetTypeFromHandle
        {
            get
            {
                if (s_getTypeFromHandle == null)
                {
                    s_getTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                    Debug.Assert(s_getTypeFromHandle != null);
                }
                return s_getTypeFromHandle;
            }
        }

        private static MethodInfo? s_objectEquals;
        private static MethodInfo ObjectEquals
        {
            get
            {
                if (s_objectEquals == null)
                {
                    s_objectEquals = typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Static);
                    Debug.Assert(s_objectEquals != null);
                }
                return s_objectEquals;
            }
        }

        private static MethodInfo? s_arraySetValue;
        private static MethodInfo ArraySetValue
        {
            get
            {
                if (s_arraySetValue == null)
                {
                    s_arraySetValue = typeof(Array).GetMethod("SetValue", new Type[] { typeof(object), typeof(int) });
                    Debug.Assert(s_arraySetValue != null);
                }
                return s_arraySetValue;
            }
        }

        private static MethodInfo? s_objectToString;
        private static MethodInfo ObjectToString
        {
            get
            {
                if (s_objectToString == null)
                {
                    s_objectToString = typeof(object).GetMethod("ToString", Type.EmptyTypes);
                    Debug.Assert(s_objectToString != null);
                }
                return s_objectToString;
            }
        }

        private static MethodInfo? s_stringFormat;
        private static MethodInfo StringFormat
        {
            get
            {
                if (s_stringFormat == null)
                {
                    s_stringFormat = typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object[]) });
                    Debug.Assert(s_stringFormat != null);
                }
                return s_stringFormat;
            }
        }

#if smolloy_keep_codegentrace
// These are only used in code which comes back with CodeGenTrace
        private static MethodInfo? s_stringConcat2;
        private static MethodInfo StringConcat2
        {
            get
            {
                if (s_stringConcat2 == null)
                {
                    s_stringConcat2 = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string) });
                    Debug.Assert(s_stringConcat2 != null);
                }
                return s_stringConcat2;
            }
        }

        private static MethodInfo? s_stringConcat3;
        private static MethodInfo StringConcat3
        {
            get
            {
                if (s_stringConcat3 == null)
                {
                    s_stringConcat3 = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string), typeof(string) });
                    Debug.Assert(s_stringConcat3 != null);
                }
                return s_stringConcat3;
            }
        }
#endif

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The trimmer will never remove the Invoke method from delegates.")]
        internal static MethodInfo GetInvokeMethod(Type delegateType)
        {
            Debug.Assert(typeof(Delegate).IsAssignableFrom(delegateType));
            return delegateType.GetMethod("Invoke")!;
        }

        private Type _delegateType = null!; // initialized in BeginMethod

        private static Module? s_serializationModule;
        private static Module SerializationModule
        {
            get
            {
                if (s_serializationModule == null)
                {
                    s_serializationModule = typeof(CodeGenerator).Module;   // could to be replaced by different dll that has SkipVerification set to false
                }
                return s_serializationModule;
            }
        }
        private DynamicMethod _dynamicMethod = null!; // initialized in BeginMethod

        private ILGenerator _ilGen = null!; // initialized in BeginMethod
        private List<ArgBuilder> _argList = null!; // initialized in BeginMethod
        private Stack<object> _blockStack = null!; // initialized in BeginMethod
        private Label _methodEndLabel;

#if smolloy_keep_codegentrace
// NOTE TODO smolloy - _codeGenTrace is never anything but 'None' in Core since SerializationTrace is gone. Could rip out a lot of conditions below.
        private readonly Dictionary<LocalBuilder, string> _localNames = new Dictionary<LocalBuilder, string>();
        private int _lineNo = 1;

        private enum CodeGenTrace { None, Save, Tron };
        private readonly CodeGenTrace _codeGenTrace;
#endif
        private LocalBuilder? _stringFormatArray;

        internal CodeGenerator()
        {
#if smolloy_keep_codegentrace
            //Defaulting to None as thats the default value in WCF
            _codeGenTrace = CodeGenTrace.None;
#endif
        }

        internal void BeginMethod(DynamicMethod dynamicMethod, Type delegateType, string methodName, Type[] argTypes, bool allowPrivateMemberAccess)
        {
            _dynamicMethod = dynamicMethod;
            _ilGen = _dynamicMethod.GetILGenerator();
            _delegateType = delegateType;

            InitILGeneration(methodName, argTypes);
        }

        internal void BeginMethod(string methodName, Type delegateType, bool allowPrivateMemberAccess)
        {
            MethodInfo signature = GetInvokeMethod(delegateType);
            ParameterInfo[] parameters = signature.GetParameters();
            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                paramTypes[i] = parameters[i].ParameterType;
            BeginMethod(signature.ReturnType, methodName, paramTypes, allowPrivateMemberAccess);
            _delegateType = delegateType;
        }

        private void BeginMethod(Type returnType, string methodName, Type[] argTypes, bool allowPrivateMemberAccess)
        {
            _dynamicMethod = new DynamicMethod(methodName, returnType, argTypes, SerializationModule, allowPrivateMemberAccess);

            _ilGen = _dynamicMethod.GetILGenerator();

            InitILGeneration(methodName, argTypes);
        }

        private void InitILGeneration(string methodName, Type[] argTypes)
        {
            _methodEndLabel = _ilGen.DefineLabel();
            _blockStack = new Stack<object>();
            _argList = new List<ArgBuilder>();
            for (int i = 0; i < argTypes.Length; i++)
                _argList.Add(new ArgBuilder(i, argTypes[i]));
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceLabel("Begin method " + methodName + " {");
#endif
        }

        internal Delegate EndMethod()
        {
            MarkLabel(_methodEndLabel);
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceLabel("} End method");
#endif
            Ret();

            Delegate? retVal;
            retVal = _dynamicMethod.CreateDelegate(_delegateType);
            _dynamicMethod = null!;
            _delegateType = null!;

            _ilGen = null!;
            _blockStack = null!;
            _argList = null!;
            return retVal;
        }

        internal MethodInfo CurrentMethod
        {
            get
            {
                return _dynamicMethod;
            }
        }

        internal ArgBuilder GetArg(int index)
        {
            return (ArgBuilder)_argList[index];
        }

        internal static Type GetVariableType(object var)
        {
            if (var is ArgBuilder)
                return ((ArgBuilder)var).ArgType;
            else if (var is LocalBuilder)
                return ((LocalBuilder)var).LocalType;
            else
                return var.GetType();
        }

        internal LocalBuilder DeclareLocal(Type type, string name, object initialValue)
        {
            LocalBuilder local = DeclareLocal(type, name);
            Load(initialValue);
            Store(local);
            return local;
        }

        internal LocalBuilder DeclareLocal(Type type, string name)
        {
            return DeclareLocal(type, name, false);
        }

        internal LocalBuilder DeclareLocal(Type type, string name, bool isPinned)
        {
            LocalBuilder local = _ilGen.DeclareLocal(type, isPinned);
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
            {
                _localNames[local] = name;
                EmitSourceComment("Declare local '" + name + "' of type " + type);
            }
#endif
            return local;
        }

        internal void Set(LocalBuilder local, object value)
        {
            Load(value);
            Store(local);
        }

        internal object For(LocalBuilder? local, object? start, object? end)
        {
            ForState forState = new ForState(local, DefineLabel(), DefineLabel(), end);
            if (forState.Index != null)
            {
                Load(start);
                Stloc(forState.Index);
                Br(forState.TestLabel);
            }
            MarkLabel(forState.BeginLabel);
            _blockStack.Push(forState);
            return forState;
        }

        internal void EndFor()
        {
            object stackTop = _blockStack.Pop();
            ForState? forState = stackTop as ForState;
            if (forState == null)
                ThrowMismatchException(stackTop);

            if (forState.Index != null)
            {
                Ldloc(forState.Index);
                Ldc(1);
                Add();
                Stloc(forState.Index);
                MarkLabel(forState.TestLabel);
                Ldloc(forState.Index);
                Load(forState.End);
                if (GetVariableType(forState.End!).IsArray)
                    Ldlen();
                Blt(forState.BeginLabel);
            }
            else
                Br(forState.BeginLabel);
            if (forState.RequiresEndLabel)
                MarkLabel(forState.EndLabel);
        }

        internal void Break(object forState)
        {
            InternalBreakFor(forState, OpCodes.Br);
        }

        // NOTE TODO smolloy - These were there in NetFx, but apparently unused there or here.
        //internal void IfTrueBreak(object forState)
        //{
        //    InternalBreakFor(forState, OpCodes.Brtrue);
        //}

        internal void IfFalseBreak(object forState)
        {
            InternalBreakFor(forState, OpCodes.Brfalse);
        }

        internal void InternalBreakFor(object userForState, OpCode branchInstruction)
        {
            foreach (object block in _blockStack)
            {
                ForState? forState = block as ForState;
                if (forState != null && (object)forState == userForState)
                {
                    if (!forState.RequiresEndLabel)
                    {
                        forState.EndLabel = DefineLabel();
                        forState.RequiresEndLabel = true;
                    }
#if smolloy_keep_codegentrace
                    if (_codeGenTrace != CodeGenTrace.None)
                        EmitSourceInstruction(branchInstruction + " " + forState.EndLabel.GetHashCode());
#endif
                    _ilGen.Emit(branchInstruction, forState.EndLabel);
                    break;
                }
            }
        }

        internal void ForEach(LocalBuilder local, Type elementType, Type enumeratorType,
            LocalBuilder enumerator, MethodInfo getCurrentMethod)
        {
            ForState forState = new ForState(local, DefineLabel(), DefineLabel(), enumerator);

            Br(forState.TestLabel);
            MarkLabel(forState.BeginLabel);

            Call(enumerator, getCurrentMethod);

            ConvertValue(elementType, GetVariableType(local));
            Stloc(local);
            _blockStack.Push(forState);
        }

        internal void EndForEach(MethodInfo moveNextMethod)
        {
            object stackTop = _blockStack.Pop();
            ForState? forState = stackTop as ForState;
            if (forState == null)
                ThrowMismatchException(stackTop);

            MarkLabel(forState.TestLabel);

            object? enumerator = forState.End;
            Call(enumerator, moveNextMethod);


            Brtrue(forState.BeginLabel);
            if (forState.RequiresEndLabel)
                MarkLabel(forState.EndLabel);
        }

        internal void IfNotDefaultValue(object value)
        {
            Type type = GetVariableType(value);
            TypeCode typeCode = Type.GetTypeCode(type);
            if ((typeCode == TypeCode.Object && type.IsValueType) ||
                typeCode == TypeCode.DateTime || typeCode == TypeCode.Decimal)
            {
                LoadDefaultValue(type);
                ConvertValue(type, Globals.TypeOfObject);
                Load(value);
                ConvertValue(type, Globals.TypeOfObject);
                Call(ObjectEquals);
                IfNot();
            }
            else
            {
                LoadDefaultValue(type);
                Load(value);
                If(Cmp.NotEqualTo);
            }
        }

        internal void If()
        {
            InternalIf(false);
        }

        internal void IfNot()
        {
            InternalIf(true);
        }

        private static OpCode GetBranchCode(Cmp cmp)
        {
            switch (cmp)
            {
                case Cmp.LessThan:
                    return OpCodes.Bge;
                case Cmp.EqualTo:
                    return OpCodes.Bne_Un;
                case Cmp.LessThanOrEqualTo:
                    return OpCodes.Bgt;
                case Cmp.GreaterThan:
                    return OpCodes.Ble;
                case Cmp.NotEqualTo:
                    return OpCodes.Beq;
                default:
                    DiagnosticUtility.DebugAssert(cmp == Cmp.GreaterThanOrEqualTo, "Unexpected cmp");
                    return OpCodes.Blt;
            }
        }

#if smolloy_keep_codegentrace
        private static Cmp GetCmpInverse(Cmp cmp)
        {
            switch (cmp)
            {
                case Cmp.LessThan:
                    return Cmp.GreaterThanOrEqualTo;
                case Cmp.EqualTo:
                    return Cmp.NotEqualTo;
                case Cmp.LessThanOrEqualTo:
                    return Cmp.GreaterThan;
                case Cmp.GreaterThan:
                    return Cmp.LessThanOrEqualTo;
                case Cmp.NotEqualTo:
                    return Cmp.EqualTo;
                default:
                    DiagnosticUtility.DebugAssert(cmp == Cmp.GreaterThanOrEqualTo, "Unexpected cmp");
                    return Cmp.LessThan;
            }
        }
#endif

        internal void If(Cmp cmpOp)
        {
            IfState ifState = new IfState();
            ifState.EndIf = DefineLabel();
            ifState.ElseBegin = DefineLabel();
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Branch if " + GetCmpInverse(cmpOp).ToString() + " to " + ifState.ElseBegin.GetHashCode().ToString(NumberFormatInfo.InvariantInfo));
#endif
            _ilGen.Emit(GetBranchCode(cmpOp), ifState.ElseBegin);
            _blockStack.Push(ifState);
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
            _blockStack.Push(ifState);
        }

        internal void ElseIf(object value1, Cmp cmpOp, object value2)
        {
            IfState ifState = (IfState)_blockStack.Pop();
            Br(ifState.EndIf);
            MarkLabel(ifState.ElseBegin);

            Load(value1);
            Load(value2);
            ifState.ElseBegin = DefineLabel();

#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Branch if " + GetCmpInverse(cmpOp).ToString() + " to " + ifState.ElseBegin.GetHashCode().ToString(NumberFormatInfo.InvariantInfo));
#endif

            _ilGen.Emit(GetBranchCode(cmpOp), ifState.ElseBegin);
            _blockStack.Push(ifState);
        }


        internal void EndIf()
        {
            IfState ifState = PopIfState();
            if (!ifState.ElseBegin.Equals(ifState.EndIf))
                MarkLabel(ifState.ElseBegin);
            MarkLabel(ifState.EndIf);
        }

        internal static void VerifyParameterCount(MethodInfo methodInfo, int expectedCount)
        {
            if (methodInfo.GetParameters().Length != expectedCount)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ParameterCountMismatch, methodInfo.Name, methodInfo.GetParameters().Length, expectedCount)));
        }

        internal void Call(object? thisObj, MethodInfo methodInfo)
        {
            VerifyParameterCount(methodInfo, 0);
            LoadThis(thisObj, methodInfo);
            Call(methodInfo);
        }

        internal void Call(object? thisObj, MethodInfo methodInfo, object? param1)
        {
            VerifyParameterCount(methodInfo, 1);
            LoadThis(thisObj, methodInfo);
            LoadParam(param1, 1, methodInfo);
            Call(methodInfo);
        }

        internal void Call(object? thisObj, MethodInfo methodInfo, object? param1, object? param2)
        {
            VerifyParameterCount(methodInfo, 2);
            LoadThis(thisObj, methodInfo);
            LoadParam(param1, 1, methodInfo);
            LoadParam(param2, 2, methodInfo);
            Call(methodInfo);
        }

        internal void Call(object? thisObj, MethodInfo methodInfo, object? param1, object? param2, object? param3)
        {
            VerifyParameterCount(methodInfo, 3);
            LoadThis(thisObj, methodInfo);
            LoadParam(param1, 1, methodInfo);
            LoadParam(param2, 2, methodInfo);
            LoadParam(param3, 3, methodInfo);
            Call(methodInfo);
        }

        internal void Call(object? thisObj, MethodInfo methodInfo, object? param1, object? param2, object? param3, object? param4)
        {
            VerifyParameterCount(methodInfo, 4);
            LoadThis(thisObj, methodInfo);
            LoadParam(param1, 1, methodInfo);
            LoadParam(param2, 2, methodInfo);
            LoadParam(param3, 3, methodInfo);
            LoadParam(param4, 4, methodInfo);
            Call(methodInfo);
        }

        internal void Call(object? thisObj, MethodInfo methodInfo, object? param1, object? param2, object? param3, object? param4, object? param5)
        {
            VerifyParameterCount(methodInfo, 5);
            LoadThis(thisObj, methodInfo);
            LoadParam(param1, 1, methodInfo);
            LoadParam(param2, 2, methodInfo);
            LoadParam(param3, 3, methodInfo);
            LoadParam(param4, 4, methodInfo);
            LoadParam(param5, 5, methodInfo);
            Call(methodInfo);
        }

        internal void Call(object? thisObj, MethodInfo methodInfo, object? param1, object? param2, object? param3, object? param4, object? param5, object? param6)
        {
            VerifyParameterCount(methodInfo, 6);
            LoadThis(thisObj, methodInfo);
            LoadParam(param1, 1, methodInfo);
            LoadParam(param2, 2, methodInfo);
            LoadParam(param3, 3, methodInfo);
            LoadParam(param4, 4, methodInfo);
            LoadParam(param5, 5, methodInfo);
            LoadParam(param6, 6, methodInfo);
            Call(methodInfo);
        }

        internal void Call(MethodInfo methodInfo)
        {
            if (methodInfo.IsVirtual && !methodInfo.DeclaringType!.IsValueType)
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction("Callvirt " + methodInfo.ToString() + " on type " + methodInfo.DeclaringType.ToString());
#endif
                _ilGen.Emit(OpCodes.Callvirt, methodInfo);
            }
            else if (methodInfo.IsStatic)
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction("Static Call " + methodInfo.ToString() + " on type " + methodInfo.DeclaringType!.ToString());
#endif
                _ilGen.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction("Call " + methodInfo.ToString() + " on type " + methodInfo.DeclaringType!.ToString());
#endif
                _ilGen.Emit(OpCodes.Call, methodInfo);
            }
        }

        internal void Call(ConstructorInfo ctor)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Call " + ctor.ToString() + " on type " + ctor.DeclaringType!.ToString());
#endif
            _ilGen.Emit(OpCodes.Call, ctor);
        }

        internal void New(ConstructorInfo constructorInfo)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Newobj " + constructorInfo.ToString() + " on type " + constructorInfo.DeclaringType!.ToString());
#endif
            _ilGen.Emit(OpCodes.Newobj, constructorInfo);
        }

        // NOTE TODO smolloy - This was there in NetFx, but apparently unused there or here.
        //internal void New(ConstructorInfo constructorInfo, object param1)
        //{
        //    LoadParam(param1, 1, constructorInfo);
        //    New(constructorInfo);
        //}

        internal void InitObj(Type valueType)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Initobj " + valueType);
#endif
            _ilGen.Emit(OpCodes.Initobj, valueType);
        }

        internal void NewArray(Type elementType, object len)
        {
            Load(len);
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Newarr " + elementType);
#endif
            _ilGen.Emit(OpCodes.Newarr, elementType);
        }

        // NOTE TODO smolloy - This was there in NetFx, but apparently unused there or here.
        //internal void IgnoreReturnValue()
        //{
        //    Pop();
        //}

        internal void LoadArrayElement(object obj, object? arrayIndex)
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
            if (arrayType == Globals.TypeOfArray)
            {
                Call(obj, ArraySetValue, value, arrayIndex);
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

        internal Type LoadMember(MemberInfo memberInfo)
        {
            Type? memberType;
            if (memberInfo is FieldInfo fieldInfo)
            {
                memberType = fieldInfo.FieldType;
                if (fieldInfo.IsStatic)
                {
#if smolloy_keep_codegentrace
                    if (_codeGenTrace != CodeGenTrace.None)
                        EmitSourceInstruction("Ldsfld " + fieldInfo + " on type " + fieldInfo.DeclaringType);
#endif
                    _ilGen.Emit(OpCodes.Ldsfld, fieldInfo);
                }
                else
                {
#if smolloy_keep_codegentrace
                    if (_codeGenTrace != CodeGenTrace.None)
                        EmitSourceInstruction("Ldfld " + fieldInfo + " on type " + fieldInfo.DeclaringType);
#endif
                    _ilGen.Emit(OpCodes.Ldfld, fieldInfo);
                }
            }
            else if (memberInfo is PropertyInfo property)
            {
                memberType = property.PropertyType;
                MethodInfo? getMethod = property.GetMethod;
                if (getMethod == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.NoGetMethodForProperty, property.DeclaringType, property)));
                Call(getMethod);
            }
            else if (memberInfo is MethodInfo method)
            {
                memberType = method.ReturnType;
                Call(method);
            }
            else
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CannotLoadMemberType, "Unknown", memberInfo.DeclaringType, memberInfo.Name)));

            EmitStackTop(memberType);
            return memberType;
        }

        internal void StoreMember(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                if (fieldInfo.IsStatic)
                {
#if smolloy_keep_codegentrace
                    if (_codeGenTrace != CodeGenTrace.None)
                        EmitSourceInstruction("Stsfld " + fieldInfo + " on type " + fieldInfo.DeclaringType);
#endif
                    _ilGen.Emit(OpCodes.Stsfld, fieldInfo);
                }
                else
                {
#if smolloy_keep_codegentrace
                    if (_codeGenTrace != CodeGenTrace.None)
                        EmitSourceInstruction("Stfld " + fieldInfo + " on type " + fieldInfo.DeclaringType);
#endif
                    _ilGen.Emit(OpCodes.Stfld, fieldInfo);
                }
            }
            else if (memberInfo is PropertyInfo property)
            {
                MethodInfo? setMethod = property.SetMethod;
                if (setMethod == null)
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.NoSetMethodForProperty, property.DeclaringType, property)));
                Call(setMethod);
            }
            else if (memberInfo is MethodInfo method)
                Call(method);
            else
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CannotLoadMemberType, "Unknown")));
        }

        internal void LoadDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                        Ldc(false);
                        break;
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        Ldc(0);
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        Ldc(0L);
                        break;
                    case TypeCode.Single:
                        Ldc(0.0F);
                        break;
                    case TypeCode.Double:
                        Ldc(0.0);
                        break;
                    case TypeCode.Decimal:
                    case TypeCode.DateTime:
                    default:
                        LocalBuilder zero = DeclareLocal(type, "zero");
                        LoadAddress(zero);
                        InitObj(type);
                        Load(zero);
                        break;
                }
            }
            else
                Load(null);
        }

        internal void Load(object? obj)
        {
            if (obj == null)
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction("Ldnull");
#endif
                _ilGen.Emit(OpCodes.Ldnull);
            }
            else if (obj is ArgBuilder)
                Ldarg((ArgBuilder)obj);
            else if (obj is LocalBuilder)
                Ldloc((LocalBuilder)obj);
            else
                Ldc(obj);
        }

        internal void Store(object var)
        {
            if (var is ArgBuilder)
                Starg((ArgBuilder)var);
            else if (var is LocalBuilder)
                Stloc((LocalBuilder)var);
            else
            {
                DiagnosticUtility.DebugAssert("Data can only be stored into ArgBuilder or LocalBuilder.");
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CanOnlyStoreIntoArgOrLocGot0, DataContract.GetClrTypeFullName(var.GetType()))));
            }
        }

        internal void Dec(object var)
        {
            Load(var);
            Load(1);
            Subtract();
            Store(var);
        }

        // NOTE TODO smolloy - This was there in NetFx, but apparently unused there or here.
        //internal void Inc(object var)
        //{
        //    Load(var);
        //    Load(1);
        //    Add();
        //    Store(var);
        //}

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
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Castclass " + target);
#endif
            _ilGen.Emit(OpCodes.Castclass, target);
        }

        internal void Box(Type type)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Box " + type);
#endif
            _ilGen.Emit(OpCodes.Box, type);
        }

        internal void Unbox(Type type)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Unbox " + type);
#endif
            _ilGen.Emit(OpCodes.Unbox, type);
        }

        private static OpCode GetLdindOpCode(TypeCode typeCode) =>
            typeCode switch
            {
                TypeCode.Boolean => OpCodes.Ldind_I1, // TypeCode.Boolean:
                TypeCode.Char => OpCodes.Ldind_I2,    // TypeCode.Char:
                TypeCode.SByte => OpCodes.Ldind_I1,   // TypeCode.SByte:
                TypeCode.Byte => OpCodes.Ldind_U1,    // TypeCode.Byte:
                TypeCode.Int16 => OpCodes.Ldind_I2,   // TypeCode.Int16:
                TypeCode.UInt16 => OpCodes.Ldind_U2,  // TypeCode.UInt16:
                TypeCode.Int32 => OpCodes.Ldind_I4,   // TypeCode.Int32:
                TypeCode.UInt32 => OpCodes.Ldind_U4,  // TypeCode.UInt32:
                TypeCode.Int64 => OpCodes.Ldind_I8,   // TypeCode.Int64:
                TypeCode.UInt64 => OpCodes.Ldind_I8,  // TypeCode.UInt64:
                TypeCode.Single => OpCodes.Ldind_R4,  // TypeCode.Single:
                TypeCode.Double => OpCodes.Ldind_R8,  // TypeCode.Double:
                TypeCode.String => OpCodes.Ldind_Ref, // TypeCode.String:
                _ => OpCodes.Nop,
            };

        internal void Ldobj(Type type)
        {
            OpCode opCode = GetLdindOpCode(Type.GetTypeCode(type));
            if (!opCode.Equals(OpCodes.Nop))
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction(opCode.ToString()!);
#endif
                _ilGen.Emit(opCode);
            }
            else
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction("Ldobj " + type);
#endif
                _ilGen.Emit(OpCodes.Ldobj, type);
            }
        }

        internal void Stobj(Type type)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Stobj " + type);
#endif
            _ilGen.Emit(OpCodes.Stobj, type);
        }


        internal void Ceq()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ceq");
#endif
            _ilGen.Emit(OpCodes.Ceq);
        }

        // NOTE TODO smolloy - These were there in NetFx, but apparently unused there or here.
//        internal void Bgt(Label label)
//        {
//#if smolloy_keep_codegentrace
//            if (_codeGenTrace != CodeGenTrace.None)
//                EmitSourceInstruction("Bgt " + label.GetHashCode());
//#endif
//            _ilGen.Emit(OpCodes.Bgt, label);
//        }

//        internal void Ble(Label label)
//        {
//#if smolloy_keep_codegentrace
//            if (_codeGenTrace != CodeGenTrace.None)
//                EmitSourceInstruction("Ble " + label.GetHashCode());
//#endif
//            _ilGen.Emit(OpCodes.Ble, label);
//        }

        internal void Throw()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Throw");
#endif
            _ilGen.Emit(OpCodes.Throw);
        }

        internal void Ldtoken(Type t)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldtoken " + t);
#endif
            _ilGen.Emit(OpCodes.Ldtoken, t);
        }

        internal void Ldc(object o)
        {
            Type valueType = o.GetType();
            if (o is Type)
            {
                Ldtoken((Type)o);
                Call(GetTypeFromHandle);
            }
            else if (valueType.IsEnum)
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceComment("Ldc " + o.GetType() + "." + o);
#endif
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
                        DiagnosticUtility.DebugAssert("Char is not a valid schema primitive and should be treated as int in DataContract");
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SR.CharIsInvalidPrimitive));
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
                    case TypeCode.Object:
                    case TypeCode.Decimal:
                    case TypeCode.DateTime:
                    case TypeCode.Empty:
                    case TypeCode.DBNull:
                    default:
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.UnknownConstantType, DataContract.GetClrTypeFullName(valueType))));
                }
            }
        }

        internal void Ldc(bool boolVar)
        {
            if (boolVar)
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction("Ldc.i4 1");
#endif
                _ilGen.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction("Ldc.i4 0");
#endif
                _ilGen.Emit(OpCodes.Ldc_I4_0);
            }
        }

        internal void Ldc(int intVar)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldc.i4 " + intVar);
#endif
            _ilGen.Emit(OpCodes.Ldc_I4, intVar);
        }

        internal void Ldc(long l)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldc.i8 " + l);
#endif
            _ilGen.Emit(OpCodes.Ldc_I8, l);
        }

        internal void Ldc(float f)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldc.r4 " + f);
#endif
            _ilGen.Emit(OpCodes.Ldc_R4, f);
        }

        internal void Ldc(double d)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldc.r8 " + d);
#endif
            _ilGen.Emit(OpCodes.Ldc_R8, d);
        }

        internal void Ldstr(string strVar)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldstr " + strVar);
#endif
            _ilGen.Emit(OpCodes.Ldstr, strVar);
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
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldloc " + _localNames[localBuilder]);
#endif
            _ilGen.Emit(OpCodes.Ldloc, localBuilder);
            EmitStackTop(localBuilder.LocalType);
        }

        internal void Stloc(LocalBuilder local)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Stloc " + _localNames[local]);
#endif
            EmitStackTop(local.LocalType);
            _ilGen.Emit(OpCodes.Stloc, local);
        }

        // NOTE TODO smolloy - These were there in NetFx, but apparently unused there or here.
//        internal void Ldloc(int slot)
//        {
//#if smolloy_keep_codegentrace
//            if (_codeGenTrace != CodeGenTrace.None)
//                EmitSourceInstruction("Ldloc " + slot);
//#endif
//
//            switch (slot)
//            {
//                case 0:
//                    _ilGen.Emit(OpCodes.Ldloc_0);
//                    break;
//                case 1:
//                    _ilGen.Emit(OpCodes.Ldloc_1);
//                    break;
//                case 2:
//                    _ilGen.Emit(OpCodes.Ldloc_2);
//                    break;
//                case 3:
//                    _ilGen.Emit(OpCodes.Ldloc_3);
//                    break;
//                default:
//                    if (slot <= 255)
//                        _ilGen.Emit(OpCodes.Ldloc_S, slot);
//                    else
//                        _ilGen.Emit(OpCodes.Ldloc, slot);
//                    break;
//            }
//        }
//
//        internal void Stloc(int slot)
//        {
//#if smolloy_keep_codegentrace
//            if (_codeGenTrace != CodeGenTrace.None)
//                EmitSourceInstruction("Stloc " + slot);
//#endif
//            switch (slot)
//            {
//                case 0:
//                    _ilGen.Emit(OpCodes.Stloc_0);
//                    break;
//                case 1:
//                    _ilGen.Emit(OpCodes.Stloc_1);
//                    break;
//                case 2:
//                    _ilGen.Emit(OpCodes.Stloc_2);
//                    break;
//                case 3:
//                    _ilGen.Emit(OpCodes.Stloc_3);
//                    break;
//                default:
//                    if (slot <= 255)
//                        _ilGen.Emit(OpCodes.Stloc_S, slot);
//                    else
//                        _ilGen.Emit(OpCodes.Stloc, slot);
//                    break;
//            }
//        }
//
//        internal void Ldloca(int slot)
//        {
//#if smolloy_keep_codegentrace
//            if (_codeGenTrace != CodeGenTrace.None)
//                EmitSourceInstruction("Ldloca " + slot);
//#endif
//            if (slot <= 255)
//                _ilGen.Emit(OpCodes.Ldloca_S, slot);
//            else
//                _ilGen.Emit(OpCodes.Ldloca, slot);
//        }

        internal void Ldloca(LocalBuilder localBuilder)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldloca " + _localNames[localBuilder]);
#endif
            _ilGen.Emit(OpCodes.Ldloca, localBuilder);
            EmitStackTop(localBuilder.LocalType);
        }

        internal void LdargAddress(ArgBuilder argBuilder)
        {
            if (argBuilder.ArgType.IsValueType)
                Ldarga(argBuilder);
            else
                Ldarg(argBuilder);
        }

        internal void Ldarg(ArgBuilder arg)
        {
            Ldarg(arg.Index);
        }

        internal void Starg(ArgBuilder arg)
        {
            Starg(arg.Index);
        }

        internal void Ldarg(int slot)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldarg " + slot);
#endif

            _ilGen.Emit(OpCodes.Ldarg, slot);
        }

        internal void Starg(int slot)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Starg " + slot);
#endif

            _ilGen.Emit(OpCodes.Starg, slot);
        }

        internal void Ldarga(ArgBuilder argBuilder)
        {
            Ldarga(argBuilder.Index);
        }

        internal void Ldarga(int slot)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldarga " + slot);
#endif

            _ilGen.Emit(OpCodes.Ldarga, slot);
        }

        internal void Ldlen()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ldlen");
#endif
            _ilGen.Emit(OpCodes.Ldlen);
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Conv.i4");
#endif
            _ilGen.Emit(OpCodes.Conv_I4);
        }

        private static OpCode GetLdelemOpCode(TypeCode typeCode) =>
            typeCode switch
            {
                TypeCode.Object or TypeCode.DBNull => OpCodes.Ldelem_Ref, // TypeCode.Object:
                TypeCode.Boolean => OpCodes.Ldelem_I1, // TypeCode.Boolean:
                TypeCode.Char => OpCodes.Ldelem_I2,    // TypeCode.Char:
                TypeCode.SByte => OpCodes.Ldelem_I1,   // TypeCode.SByte:
                TypeCode.Byte => OpCodes.Ldelem_U1,    // TypeCode.Byte:
                TypeCode.Int16 => OpCodes.Ldelem_I2,   // TypeCode.Int16:
                TypeCode.UInt16 => OpCodes.Ldelem_U2,  // TypeCode.UInt16:
                TypeCode.Int32 => OpCodes.Ldelem_I4,   // TypeCode.Int32:
                TypeCode.UInt32 => OpCodes.Ldelem_U4,  // TypeCode.UInt32:
                TypeCode.Int64 => OpCodes.Ldelem_I8,   // TypeCode.Int64:
                TypeCode.UInt64 => OpCodes.Ldelem_I8,  // TypeCode.UInt64:
                TypeCode.Single => OpCodes.Ldelem_R4,  // TypeCode.Single:
                TypeCode.Double => OpCodes.Ldelem_R8,  // TypeCode.Double:
                TypeCode.String => OpCodes.Ldelem_Ref, // TypeCode.String:
                _ => OpCodes.Nop,
            };

        internal void Ldelem(Type arrayElementType)
        {
            if (arrayElementType.IsEnum)
            {
                Ldelem(Enum.GetUnderlyingType(arrayElementType));
            }
            else
            {
                OpCode opCode = GetLdelemOpCode(Type.GetTypeCode(arrayElementType));
                if (opCode.Equals(OpCodes.Nop))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayTypeIsNotSupported_GeneratingCode, DataContract.GetClrTypeFullName(arrayElementType))));
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction(opCode.ToString()!);
#endif
                _ilGen.Emit(opCode);
                EmitStackTop(arrayElementType);
            }
        }
        internal void Ldelema(Type arrayElementType)
        {
            OpCode opCode = OpCodes.Ldelema;
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction(opCode.ToString()!);
#endif
            _ilGen.Emit(opCode, arrayElementType);

            EmitStackTop(arrayElementType);
        }

        private static OpCode GetStelemOpCode(TypeCode typeCode) =>
            typeCode switch
            {
                TypeCode.Object or TypeCode.DBNull => OpCodes.Stelem_Ref, // TypeCode.Object:
                TypeCode.Boolean => OpCodes.Stelem_I1, // TypeCode.Boolean:
                TypeCode.Char => OpCodes.Stelem_I2,    // TypeCode.Char:
                TypeCode.SByte => OpCodes.Stelem_I1,   // TypeCode.SByte:
                TypeCode.Byte => OpCodes.Stelem_I1,    // TypeCode.Byte:
                TypeCode.Int16 => OpCodes.Stelem_I2,   // TypeCode.Int16:
                TypeCode.UInt16 => OpCodes.Stelem_I2,  // TypeCode.UInt16:
                TypeCode.Int32 => OpCodes.Stelem_I4,   // TypeCode.Int32:
                TypeCode.UInt32 => OpCodes.Stelem_I4,  // TypeCode.UInt32:
                TypeCode.Int64 => OpCodes.Stelem_I8,   // TypeCode.Int64:
                TypeCode.UInt64 => OpCodes.Stelem_I8,  // TypeCode.UInt64:
                TypeCode.Single => OpCodes.Stelem_R4,  // TypeCode.Single:
                TypeCode.Double => OpCodes.Stelem_R8,  // TypeCode.Double:
                TypeCode.String => OpCodes.Stelem_Ref, // TypeCode.String:
                _ => OpCodes.Nop,
            };

        internal void Stelem(Type arrayElementType)
        {
            if (arrayElementType.IsEnum)
                Stelem(Enum.GetUnderlyingType(arrayElementType));
            else
            {
                OpCode opCode = GetStelemOpCode(Type.GetTypeCode(arrayElementType));
                if (opCode.Equals(OpCodes.Nop))
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayTypeIsNotSupported_GeneratingCode, DataContract.GetClrTypeFullName(arrayElementType))));
#if smolloy_keep_codegentrace
                if (_codeGenTrace != CodeGenTrace.None)
                    EmitSourceInstruction(opCode.ToString()!);
#endif
                EmitStackTop(arrayElementType);
                _ilGen.Emit(opCode);
            }
        }

        internal Label DefineLabel()
        {
            return _ilGen.DefineLabel();
        }

        internal void MarkLabel(Label label)
        {
            _ilGen.MarkLabel(label);
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceLabel(label.GetHashCode() + ":");
#endif
        }

        internal void Add()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Add");
#endif
            _ilGen.Emit(OpCodes.Add);
        }

        internal void Subtract()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Sub");
#endif
            _ilGen.Emit(OpCodes.Sub);
        }

        internal void And()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("And");
#endif
            _ilGen.Emit(OpCodes.And);
        }
        internal void Or()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Or");
#endif
            _ilGen.Emit(OpCodes.Or);
        }

        internal void Not()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Not");
#endif
            _ilGen.Emit(OpCodes.Not);
        }

        internal void Ret()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Ret");
#endif
            _ilGen.Emit(OpCodes.Ret);
        }

        internal void Br(Label label)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Br " + label.GetHashCode());
#endif
            _ilGen.Emit(OpCodes.Br, label);
        }

        internal void Blt(Label label)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Blt " + label.GetHashCode());
#endif
            _ilGen.Emit(OpCodes.Blt, label);
        }

        internal void Brfalse(Label label)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Brfalse " + label.GetHashCode());
#endif
            _ilGen.Emit(OpCodes.Brfalse, label);
        }

        internal void Brtrue(Label label)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Brtrue " + label.GetHashCode());
#endif
            _ilGen.Emit(OpCodes.Brtrue, label);
        }



        internal void Pop()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Pop");
#endif
            _ilGen.Emit(OpCodes.Pop);
        }

        internal void Dup()
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Dup");
#endif
            _ilGen.Emit(OpCodes.Dup);
        }

        private void LoadThis(object? thisObj, MethodInfo methodInfo)
        {
            if (thisObj != null && !methodInfo.IsStatic)
            {
                LoadAddress(thisObj);
                ConvertAddress(GetVariableType(thisObj), methodInfo.DeclaringType!);
            }
        }

        private void LoadParam(object? arg, int oneBasedArgIndex, MethodBase methodInfo)
        {
            Load(arg);
            if (arg != null)
                ConvertValue(GetVariableType(arg), methodInfo.GetParameters()[oneBasedArgIndex - 1].ParameterType);
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
            _blockStack.Push(ifState);
        }

        private static OpCode GetConvOpCode(TypeCode typeCode) =>
            typeCode switch
            {
                TypeCode.Boolean => OpCodes.Conv_I1, // TypeCode.Boolean:
                TypeCode.Char => OpCodes.Conv_I2,    // TypeCode.Char:
                TypeCode.SByte => OpCodes.Conv_I1,   // TypeCode.SByte:
                TypeCode.Byte => OpCodes.Conv_U1,    // TypeCode.Byte:
                TypeCode.Int16 => OpCodes.Conv_I2,   // TypeCode.Int16:
                TypeCode.UInt16 => OpCodes.Conv_U2,  // TypeCode.UInt16:
                TypeCode.Int32 => OpCodes.Conv_I4,   // TypeCode.Int32:
                TypeCode.UInt32 => OpCodes.Conv_U4,  // TypeCode.UInt32:
                TypeCode.Int64 => OpCodes.Conv_I8,   // TypeCode.Int64:
                TypeCode.UInt64 => OpCodes.Conv_I8,  // TypeCode.UInt64:
                TypeCode.Single => OpCodes.Conv_R4,  // TypeCode.Single:
                TypeCode.Double => OpCodes.Conv_R8,  // TypeCode.Double:
                _ => OpCodes.Nop,
            };

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
                        throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.NoConversionPossibleTo, DataContract.GetClrTypeFullName(target))));
                    else
                    {
#if smolloy_keep_codegentrace
                        if (_codeGenTrace != CodeGenTrace.None)
                            EmitSourceInstruction(opCode.ToString()!);
#endif
                        _ilGen.Emit(opCode);
                    }
                }
                else if (source.IsAssignableFrom(target))
                {
                    Unbox(target);
                    if (!isAddress)
                        Ldobj(target);
                }
                else
                    throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.IsNotAssignableFrom, DataContract.GetClrTypeFullName(target), DataContract.GetClrTypeFullName(source))));
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
                throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.IsNotAssignableFrom, DataContract.GetClrTypeFullName(target), DataContract.GetClrTypeFullName(source))));
        }

        private IfState PopIfState()
        {
            object stackTop = _blockStack.Pop();
            IfState? ifState = stackTop as IfState;
            if (ifState == null)
                ThrowMismatchException(stackTop);
            return ifState;
        }

        [DoesNotReturn]
        private static void ThrowMismatchException(object expected)
        {
            throw System.Runtime.Serialization.DiagnosticUtility.ExceptionUtility.ThrowHelperError(XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExpectingEnd, expected.ToString())));
        }

#if smolloy_keep_codegentrace
        internal void EmitSourceInstruction(string line)
        {
            EmitSourceLine("    " + line);
        }

        internal void EmitSourceLabel(string line)
        {
            EmitSourceLine(line);
        }

        internal void EmitSourceComment(string comment)
        {
            EmitSourceInstruction("// " + comment);
        }

        internal void EmitSourceLine(string line)
        {
            //if (_codeGenTrace != CodeGenTrace.None)
            //    SerializationTrace.WriteInstruction(_lineNo++, line);
            if (_ilGen != null && _codeGenTrace == CodeGenTrace.Tron)
            {
                _ilGen.Emit(OpCodes.Ldstr, string.Format(CultureInfo.InvariantCulture, "{0:00000}: {1}", _lineNo - 1, line));
                _ilGenEmit(OpCodes.Call, XmlFormatGeneratorStatics.TraceInstructionMethod);
            }
    }
#endif

#if !CodeGenTrace
        internal static void EmitStackTop(Type stackTopType)
        {
            return;
        }
#else
        internal void EmitStackTop(Type stackTopType)
        {
            if (_codeGenTrace != CodeGenTrace.Tron)
                return;
            _codeGenTrace = CodeGenTrace.None;
            Dup();
            ToDebuggableString(stackTopType);
            LocalBuilder topValue = DeclareLocal(Globals.TypeOfString, "topValue");
            Store(topValue);
            Load("//value = ");
            Load(topValue);
            Concat2();
            Call(XmlFormatGeneratorStatics.TraceInstructionMethod);
            _codeGenTrace = CodeGenTrace.Tron;
            return;
        }

        internal void ToDebuggableString(Type type)
        {
            if (type.IsValueType)
            {
                Box(type);
                Call(ObjectToString);
            }
            else
            {
                Dup();
                Load(null);
                If(Cmp.EqualTo);
                Pop();
                Load("<null>");
                Else();
                if (type.IsArray)
                {
                    LocalBuilder arrayVar = DeclareLocal(type, "arrayVar");
                    Store(arrayVar);
                    Load("{ ");
                    LocalBuilder arrayValueString = DeclareLocal(typeof(string), "arrayValueString");
                    Store(arrayValueString);
                    LocalBuilder i = DeclareLocal(typeof(int), "i");
                    For(i, 0, arrayVar);
                    Load(arrayValueString);
                    LoadArrayElement(arrayVar, i);
                    ToDebuggableString(arrayVar.LocalType.GetElementType());
                    Load(", ");
                    Concat3();
                    Store(arrayValueString);
                    EndFor();
                    Load(arrayValueString);
                    Load("}");
                    Concat2();
                }
                else
                    Call(ObjectToString);
                EndIf();
            }
        }

        internal void Concat2()
        {
            Call(StringConcat2);
        }

        internal void Concat3()
        {
            Call(StringConcat3);
        }
#endif

        internal Label[] Switch(int labelCount)
        {
            SwitchState switchState = new SwitchState(DefineLabel(), DefineLabel());
            Label[] caseLabels = new Label[labelCount];
            for (int i = 0; i < caseLabels.Length; i++)
                caseLabels[i] = DefineLabel();

#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
            {
                EmitSourceInstruction("switch (");
                foreach (Label l in caseLabels)
                    EmitSourceInstruction("    " + l.GetHashCode());
                EmitSourceInstruction(") {");
            }
#endif

            _ilGen.Emit(OpCodes.Switch, caseLabels);
            Br(switchState.DefaultLabel);
            _blockStack.Push(switchState);
            return caseLabels;
        }
        internal void Case(Label caseLabel1, string caseLabelName)
        {
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("case " + caseLabelName + "{");
#endif
            MarkLabel(caseLabel1);
        }

        internal void EndCase()
        {
            object stackTop = _blockStack.Peek();
            SwitchState? switchState = stackTop as SwitchState;
            if (switchState == null)
                ThrowMismatchException(stackTop);
            Br(switchState.EndOfSwitchLabel);
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("} //end case ");
#endif
        }

        internal void EndSwitch()
        {
            object stackTop = _blockStack.Pop();
            SwitchState? switchState = stackTop as SwitchState;
            if (switchState == null)
                ThrowMismatchException(stackTop);
#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("} //end switch");
#endif
            if (!switchState.DefaultDefined)
                MarkLabel(switchState.DefaultLabel);
            MarkLabel(switchState.EndOfSwitchLabel);
        }

        private static readonly MethodInfo s_stringLength = typeof(string).GetProperty("Length")!.GetMethod!;
        internal void ElseIfIsEmptyString(LocalBuilder strLocal)
        {
            IfState ifState = (IfState)_blockStack.Pop();
            Br(ifState.EndIf);
            MarkLabel(ifState.ElseBegin);

            Load(strLocal);
            Call(s_stringLength);
            Load(0);
            ifState.ElseBegin = DefineLabel();

#if smolloy_keep_codegentrace
            if (_codeGenTrace != CodeGenTrace.None)
                EmitSourceInstruction("Branch if " + GetCmpInverse(Cmp.EqualTo).ToString() + " to " + ifState.ElseBegin.GetHashCode().ToString(NumberFormatInfo.InvariantInfo));
#endif

            _ilGen.Emit(GetBranchCode(Cmp.EqualTo), ifState.ElseBegin);
            _blockStack.Push(ifState);
        }

        internal void IfNotIsEmptyString(LocalBuilder strLocal)
        {
            Load(strLocal);
            Call(s_stringLength);
            Load(0);
            If(Cmp.NotEqualTo);
        }

        internal void BeginWhileCondition()
        {
            Label startWhile = DefineLabel();
            MarkLabel(startWhile);
            _blockStack.Push(startWhile);
        }

        internal void BeginWhileBody(Cmp cmpOp)
        {
            Label startWhile = (Label)_blockStack.Pop();
            If(cmpOp);
            _blockStack.Push(startWhile);
        }

        internal void EndWhile()
        {
            Label startWhile = (Label)_blockStack.Pop();
            Br(startWhile);
            EndIf();
        }

        internal void CallStringFormat(string msg, params object[] values)
        {
            NewArray(typeof(object), values.Length);
            if (_stringFormatArray == null)
                _stringFormatArray = DeclareLocal(typeof(object[]), "stringFormatArray");
            Stloc(_stringFormatArray);
            for (int i = 0; i < values.Length; i++)
                StoreArrayElement(_stringFormatArray, i, values[i]);

            Load(msg);
            Load(_stringFormatArray);
            Call(StringFormat);
        }

        internal void ToString(Type type)
        {
            if (type != Globals.TypeOfString)
            {
                if (type.IsValueType)
                {
                    Box(type);
                }
                Call(ObjectToString);
            }
        }
    }

    internal sealed class ArgBuilder
    {
        internal int Index;
        internal Type ArgType;
        internal ArgBuilder(int index, Type argType)
        {
            this.Index = index;
            this.ArgType = argType;
        }
    }

    internal sealed class ForState
    {
        private readonly LocalBuilder? _indexVar;
        private readonly Label _beginLabel;
        private readonly Label _testLabel;
        private Label _endLabel;
        private bool _requiresEndLabel;
        private readonly object? _end;

        internal ForState(LocalBuilder? indexVar, Label beginLabel, Label testLabel, object? end)
        {
            _indexVar = indexVar;
            _beginLabel = beginLabel;
            _testLabel = testLabel;
            _end = end;
        }

        internal LocalBuilder? Index
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

        internal Label EndLabel
        {
            get
            {
                return _endLabel;
            }
            set
            {
                _endLabel = value;
            }
        }

        internal bool RequiresEndLabel
        {
            get
            {
                return _requiresEndLabel;
            }
            set
            {
                _requiresEndLabel = value;
            }
        }

        internal object? End
        {
            get
            {
                return _end;
            }
        }
    }

    internal enum Cmp
    {
        LessThan,
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


    internal sealed class SwitchState
    {
        private readonly Label _defaultLabel;
        private readonly Label _endOfSwitchLabel;
        private bool _defaultDefined;
        internal SwitchState(Label defaultLabel, Label endOfSwitchLabel)
        {
            _defaultLabel = defaultLabel;
            _endOfSwitchLabel = endOfSwitchLabel;
            _defaultDefined = false;
        }
        internal Label DefaultLabel
        {
            get
            {
                return _defaultLabel;
            }
        }

        internal Label EndOfSwitchLabel
        {
            get
            {
                return _endOfSwitchLabel;
            }
        }
        internal bool DefaultDefined
        {
            get
            {
                return _defaultDefined;
            }
            set
            {
                _defaultDefined = value;
            }
        }
    }
}
