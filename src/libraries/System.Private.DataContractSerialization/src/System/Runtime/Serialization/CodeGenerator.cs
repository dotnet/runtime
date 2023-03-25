// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.DataContracts;

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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The trimmer will never remove the Invoke method from delegates.")]
        internal static MethodInfo GetInvokeMethod(Type delegateType)
        {
            Debug.Assert(typeof(Delegate).IsAssignableFrom(delegateType));
            return delegateType.GetMethod("Invoke")!;
        }

        private Type _delegateType = null!; // initialized in BeginMethod

        private static Module? s_serializationModule;
        private static Module SerializationModule => s_serializationModule ??= typeof(CodeGenerator).Module;   // could to be replaced by different dll that has SkipVerification set to false

        private DynamicMethod _dynamicMethod = null!; // initialized in BeginMethod

        private ILGenerator _ilGen = null!; // initialized in BeginMethod
        private List<ArgBuilder> _argList = null!; // initialized in BeginMethod
        private Stack<object> _blockStack = null!; // initialized in BeginMethod
        private Label _methodEndLabel;

        private LocalBuilder? _stringFormatArray;

        internal CodeGenerator() { }

        internal void BeginMethod(DynamicMethod dynamicMethod, Type delegateType, Type[] argTypes)
        {
            _dynamicMethod = dynamicMethod;
            _ilGen = _dynamicMethod.GetILGenerator();
            _delegateType = delegateType;

            InitILGeneration(argTypes);
        }

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
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

        [RequiresDynamicCode(DataContract.SerializerAOTWarning)]
        private void BeginMethod(Type returnType, string methodName, Type[] argTypes, bool allowPrivateMemberAccess)
        {
            _dynamicMethod = new DynamicMethod(methodName, returnType, argTypes, SerializationModule, allowPrivateMemberAccess);

            _ilGen = _dynamicMethod.GetILGenerator();

            InitILGeneration(argTypes);
        }

        private void InitILGeneration(Type[] argTypes)
        {
            _methodEndLabel = _ilGen.DefineLabel();
            _blockStack = new Stack<object>();
            _argList = new List<ArgBuilder>();
            for (int i = 0; i < argTypes.Length; i++)
                _argList.Add(new ArgBuilder(i, argTypes[i]));
        }

        internal Delegate EndMethod()
        {
            MarkLabel(_methodEndLabel);
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
            return _argList[index];
        }

        internal static Type GetVariableType(object var)
        {
            if (var is ArgBuilder argBuilder)
                return argBuilder.ArgType;
            else if (var is LocalBuilder localBuilder)
                return localBuilder.LocalType;
            else
                return var.GetType();
        }

        internal LocalBuilder DeclareLocal(Type type, object initialValue)
        {
            LocalBuilder local = DeclareLocal(type);
            Load(initialValue);
            Store(local);
            return local;
        }

        internal LocalBuilder DeclareLocal(Type type)
        {
            return DeclareLocal(type, false);
        }

        internal LocalBuilder DeclareLocal(Type type, bool isPinned)
        {
            return _ilGen.DeclareLocal(type, isPinned);
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

        internal void IfFalseBreak(object forState)
        {
            InternalBreakFor(forState, OpCodes.Brfalse);
        }

        internal void InternalBreakFor(object userForState, OpCode branchInstruction)
        {
            foreach (object block in _blockStack)
            {
                if (block == userForState && block is ForState forState)
                {
                    if (!forState.RequiresEndLabel)
                    {
                        forState.EndLabel = DefineLabel();
                        forState.RequiresEndLabel = true;
                    }

                    _ilGen.Emit(branchInstruction, forState.EndLabel);
                    break;
                }
            }
        }

        internal void ForEach(LocalBuilder local, Type elementType,
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
                    Debug.Assert(cmp == Cmp.GreaterThanOrEqualTo, "Unexpected cmp");
                    return OpCodes.Blt;
            }
        }

        internal void If(Cmp cmpOp)
        {
            IfState ifState = new IfState();
            ifState.EndIf = DefineLabel();
            ifState.ElseBegin = DefineLabel();
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
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ParameterCountMismatch, methodInfo.Name, methodInfo.GetParameters().Length, expectedCount));
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
                _ilGen.Emit(OpCodes.Callvirt, methodInfo);
            }
            else if (methodInfo.IsStatic)
            {
                _ilGen.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                _ilGen.Emit(OpCodes.Call, methodInfo);
            }
        }

        internal void Call(ConstructorInfo ctor)
        {
            _ilGen.Emit(OpCodes.Call, ctor);
        }

        internal void New(ConstructorInfo constructorInfo)
        {
            _ilGen.Emit(OpCodes.Newobj, constructorInfo);
        }

        internal void InitObj(Type valueType)
        {
            _ilGen.Emit(OpCodes.Initobj, valueType);
        }

        internal void NewArray(Type elementType, object len)
        {
            Load(len);
            _ilGen.Emit(OpCodes.Newarr, elementType);
        }

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
                    _ilGen.Emit(OpCodes.Ldsfld, fieldInfo);
                }
                else
                {
                    _ilGen.Emit(OpCodes.Ldfld, fieldInfo);
                }
            }
            else if (memberInfo is PropertyInfo property)
            {
                memberType = property.PropertyType;
                MethodInfo? getMethod = property.GetMethod;
                if (getMethod == null)
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.NoGetMethodForProperty, property.DeclaringType, property));
                Call(getMethod);
            }
            else if (memberInfo is MethodInfo method)
            {
                memberType = method.ReturnType;
                Call(method);
            }
            else
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CannotLoadMemberType, "Unknown", memberInfo.DeclaringType, memberInfo.Name));

            return memberType;
        }

        internal void StoreMember(MemberInfo memberInfo)
        {
            if (memberInfo is FieldInfo fieldInfo)
            {
                if (fieldInfo.IsStatic)
                {
                    _ilGen.Emit(OpCodes.Stsfld, fieldInfo);
                }
                else
                {
                    _ilGen.Emit(OpCodes.Stfld, fieldInfo);
                }
            }
            else if (memberInfo is PropertyInfo property)
            {
                MethodInfo? setMethod = property.SetMethod;
                if (setMethod == null)
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.NoSetMethodForProperty, property.DeclaringType, property));
                Call(setMethod);
            }
            else if (memberInfo is MethodInfo method)
                Call(method);
            else
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CannotLoadMemberType, "Unknown"));
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
                        LocalBuilder zero = DeclareLocal(type);
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
                _ilGen.Emit(OpCodes.Ldnull);
            }
            else if (obj is ArgBuilder argBuilder)
                Ldarg(argBuilder);
            else if (obj is LocalBuilder localBuilder)
                Ldloc(localBuilder);
            else
                Ldc(obj);
        }

        internal void Store(object var)
        {
            if (var is ArgBuilder argBuilder)
                Starg(argBuilder);
            else if (var is LocalBuilder localBuilder)
                Stloc(localBuilder);
            else
            {
                Debug.Fail("Data can only be stored into ArgBuilder or LocalBuilder.");
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.CanOnlyStoreIntoArgOrLocGot0, DataContract.GetClrTypeFullName(var.GetType())));
            }
        }

        internal void Dec(object var)
        {
            Load(var);
            Load(1);
            Subtract();
            Store(var);
        }

        internal void LoadAddress(object obj)
        {
            if (obj is ArgBuilder argBuilder)
                LdargAddress(argBuilder);
            else if (obj is LocalBuilder localBuilder)
                LdlocAddress(localBuilder);
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
            _ilGen.Emit(OpCodes.Castclass, target);
        }

        internal void Box(Type type)
        {
            _ilGen.Emit(OpCodes.Box, type);
        }

        internal void Unbox(Type type)
        {
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
                _ilGen.Emit(opCode);
            }
            else
            {
                _ilGen.Emit(OpCodes.Ldobj, type);
            }
        }

        internal void Stobj(Type type)
        {
            _ilGen.Emit(OpCodes.Stobj, type);
        }


        internal void Ceq()
        {
            _ilGen.Emit(OpCodes.Ceq);
        }

        internal void Throw()
        {
            _ilGen.Emit(OpCodes.Throw);
        }

        internal void Ldtoken(Type t)
        {
            _ilGen.Emit(OpCodes.Ldtoken, t);
        }

        internal void Ldc(object o)
        {
            Type valueType = o.GetType();
            if (o is Type t)
            {
                Ldtoken(t);
                Call(GetTypeFromHandle);
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
                        throw new NotSupportedException(SR.CharIsInvalidPrimitive);
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
                        throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.UnknownConstantType, DataContract.GetClrTypeFullName(valueType)));
                }
            }
        }

        internal void Ldc(bool boolVar)
        {
            if (boolVar)
            {
                _ilGen.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                _ilGen.Emit(OpCodes.Ldc_I4_0);
            }
        }

        internal void Ldc(int intVar)
        {
            _ilGen.Emit(OpCodes.Ldc_I4, intVar);
        }

        internal void Ldc(long l)
        {
            _ilGen.Emit(OpCodes.Ldc_I8, l);
        }

        internal void Ldc(float f)
        {
            _ilGen.Emit(OpCodes.Ldc_R4, f);
        }

        internal void Ldc(double d)
        {
            _ilGen.Emit(OpCodes.Ldc_R8, d);
        }

        internal void Ldstr(string strVar)
        {
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
            _ilGen.Emit(OpCodes.Ldloc, localBuilder);
        }

        internal void Stloc(LocalBuilder local)
        {
            _ilGen.Emit(OpCodes.Stloc, local);
        }

        internal void Ldloca(LocalBuilder localBuilder)
        {
            _ilGen.Emit(OpCodes.Ldloca, localBuilder);
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
            _ilGen.Emit(OpCodes.Ldarg, slot);
        }

        internal void Starg(int slot)
        {
            _ilGen.Emit(OpCodes.Starg, slot);
        }

        internal void Ldarga(ArgBuilder argBuilder)
        {
            Ldarga(argBuilder.Index);
        }

        internal void Ldarga(int slot)
        {
            _ilGen.Emit(OpCodes.Ldarga, slot);
        }

        internal void Ldlen()
        {
            _ilGen.Emit(OpCodes.Ldlen);
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
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayTypeIsNotSupported_GeneratingCode, DataContract.GetClrTypeFullName(arrayElementType)));
                _ilGen.Emit(opCode);
            }
        }
        internal void Ldelema(Type arrayElementType)
        {
            OpCode opCode = OpCodes.Ldelema;
            _ilGen.Emit(opCode, arrayElementType);
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
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ArrayTypeIsNotSupported_GeneratingCode, DataContract.GetClrTypeFullName(arrayElementType)));
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
        }

        internal void Add()
        {
            _ilGen.Emit(OpCodes.Add);
        }

        internal void Subtract()
        {
            _ilGen.Emit(OpCodes.Sub);
        }

        internal void And()
        {
            _ilGen.Emit(OpCodes.And);
        }
        internal void Or()
        {
            _ilGen.Emit(OpCodes.Or);
        }

        internal void Not()
        {
            _ilGen.Emit(OpCodes.Not);
        }

        internal void Ret()
        {
            _ilGen.Emit(OpCodes.Ret);
        }

        internal void Br(Label label)
        {
            _ilGen.Emit(OpCodes.Br, label);
        }

        internal void Blt(Label label)
        {
            _ilGen.Emit(OpCodes.Blt, label);
        }

        internal void Brfalse(Label label)
        {
            _ilGen.Emit(OpCodes.Brfalse, label);
        }

        internal void Brtrue(Label label)
        {
            _ilGen.Emit(OpCodes.Brtrue, label);
        }

        internal void Pop()
        {
            _ilGen.Emit(OpCodes.Pop);
        }

        internal void Dup()
        {
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

        private void LoadParam(object? arg, int oneBasedArgIndex, MethodInfo methodInfo)
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
                        throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.NoConversionPossibleTo, DataContract.GetClrTypeFullName(target)));
                    else
                    {
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
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.IsNotAssignableFrom, DataContract.GetClrTypeFullName(target), DataContract.GetClrTypeFullName(source)));
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
                throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.IsNotAssignableFrom, DataContract.GetClrTypeFullName(target), DataContract.GetClrTypeFullName(source)));
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
            throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.ExpectingEnd, expected.ToString()));
        }

        internal Label[] Switch(int labelCount)
        {
            SwitchState switchState = new SwitchState(DefineLabel(), DefineLabel());
            Label[] caseLabels = new Label[labelCount];
            for (int i = 0; i < caseLabels.Length; i++)
                caseLabels[i] = DefineLabel();

            _ilGen.Emit(OpCodes.Switch, caseLabels);
            Br(switchState.DefaultLabel);
            _blockStack.Push(switchState);
            return caseLabels;
        }
        internal void Case(Label caseLabel1)
        {
            MarkLabel(caseLabel1);
        }

        internal void EndCase()
        {
            object stackTop = _blockStack.Peek();
            SwitchState? switchState = stackTop as SwitchState;
            if (switchState == null)
                ThrowMismatchException(stackTop);
            Br(switchState.EndOfSwitchLabel);
        }

        internal void EndSwitch()
        {
            object stackTop = _blockStack.Pop();
            SwitchState? switchState = stackTop as SwitchState;
            if (switchState == null)
                ThrowMismatchException(stackTop);
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
            _stringFormatArray ??= DeclareLocal(typeof(object[]));
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
            Index = index;
            ArgType = argType;
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
