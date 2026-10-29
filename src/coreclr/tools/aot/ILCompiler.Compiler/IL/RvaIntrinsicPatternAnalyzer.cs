// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Internal.IL;
using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// State machine for RVA field tokens consumed by InitializeArray or CreateSpan.
    /// </summary>
    internal struct RvaIntrinsicPatternAnalyzer
    {
        private const int MaxInitializeArrayArguments = 64;

        [InlineArray(MaxInitializeArrayArguments)]
        private struct ConstantBuffer
        {
            private int _element0;
        }

        private enum State : byte
        {
            Constants = 1,
            ArrayAllocation,
            ArrayDup,
            LdToken,
            InitializeArrayLdToken,
            IntrinsicCall,
        }

        private State _state;
        private ConstantBuffer _recentConstants;
        private int _recentConstantCount;
        private int _nextConstantIndex;
        private int _fieldToken;
        private int _initializationSize;

        public readonly bool IsRvaIntrinsicCall => _state is State.IntrinsicCall;

        public void Advance(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
        {
            switch (_state)
            {
                case 0:
                case State.Constants:
                    if (TryReadInt32Constant(opcode, reader, out int value))
                    {
                        AddConstant(value);
                        _state = State.Constants;
                    }
                    else if (TryGetInitializeArraySize(opcode, reader, methodIL, out _initializationSize))
                    {
                        ResetConstants();
                        _state = State.ArrayAllocation;
                    }
                    else if (opcode == ILOpcode.ldtoken)
                    {
                        ResetConstants();
                        _fieldToken = reader.PeekILToken();
                        _state = State.LdToken;
                    }
                    else
                    {
                        Reset();
                    }
                    return;

                case State.ArrayAllocation:
                    if (opcode == ILOpcode.dup)
                    {
                        _state = State.ArrayDup;
                        return;
                    }
                    break;

                case State.ArrayDup:
                    if (opcode == ILOpcode.ldtoken)
                    {
                        _fieldToken = reader.PeekILToken();
                        _state = State.InitializeArrayLdToken;
                        return;
                    }
                    break;

                case State.LdToken:
                    if (TryMatchCreateSpan(opcode, reader, methodIL))
                    {
                        _state = State.IntrinsicCall;
                        return;
                    }
                    break;

                case State.InitializeArrayLdToken:
                    if (TryMatchInitializeArray(opcode, reader, methodIL)
                        || TryMatchCreateSpan(opcode, reader, methodIL))
                    {
                        _state = State.IntrinsicCall;
                        return;
                    }
                    break;

                case State.IntrinsicCall:
                    break;

                default:
                    throw new UnreachableException();
            }

            Reset();
            Advance(opcode, reader, methodIL);
        }

        private void AddConstant(int value)
        {
            _recentConstants[_nextConstantIndex] = value;
            _nextConstantIndex = (_nextConstantIndex + 1) % MaxInitializeArrayArguments;
            _recentConstantCount = Math.Min(_recentConstantCount + 1, MaxInitializeArrayArguments);
        }

        private void Reset()
        {
            _state = default;
            _fieldToken = 0;
            _initializationSize = 0;
            ResetConstants();
        }

        private void ResetConstants()
        {
            _recentConstantCount = 0;
            _nextConstantIndex = 0;
        }

        private readonly int GetConstant(int index)
        {
            int firstConstantIndex = _recentConstantCount == MaxInitializeArrayArguments ? _nextConstantIndex : 0;
            return _recentConstants[(firstConstantIndex + index) % MaxInitializeArrayArguments];
        }

        private bool TryGetInitializeArraySize(ILOpcode opcode, in ILReader reader, MethodIL methodIL, out int initializationSize)
        {
            initializationSize = 0;

            TypeDesc elementType;
            int elementCount = 1;
            if (opcode == ILOpcode.newarr)
            {
                if (_recentConstantCount == 0)
                    return false;

                elementType = (TypeDesc)methodIL.GetObject(reader.PeekILToken());
                elementCount = GetConstant(_recentConstantCount - 1);
            }
            else if (opcode == ILOpcode.newobj)
            {
                MethodDesc constructor = (MethodDesc)methodIL.GetObject(reader.PeekILToken());
                if (!constructor.IsConstructor || constructor.OwningType is not ArrayType arrayType)
                    return false;

                elementType = arrayType.ElementType;
                int rank = arrayType.Rank;
                int argumentCount = constructor.Signature.Length;
                if (argumentCount > _recentConstantCount)
                    return false;

                bool hasLowerBounds;
                if (argumentCount == rank)
                {
                    hasLowerBounds = false;
                }
                else if (argumentCount == 2 * rank)
                {
                    hasLowerBounds = true;
                }
                else
                {
                    return false;
                }

                int firstArgumentIndex = _recentConstantCount - argumentCount;
                for (int dimension = 0; dimension < rank; dimension++)
                {
                    int lowerBoundOffset = hasLowerBounds ? 1 : 0;
                    int lengthIndex = firstArgumentIndex + (dimension * (lowerBoundOffset + 1)) + lowerBoundOffset;
                    if (!TryMultiplyPositive(elementCount, GetConstant(lengthIndex), out elementCount))
                        return false;
                }
            }
            else
            {
                return false;
            }

            if (elementType.IsRuntimeDeterminedSubtype
                || !TryGetInitializeArrayElementSize(elementType, out int elementSize))
            {
                return false;
            }

            return TryMultiplyPositive(elementCount, elementSize, out initializationSize);
        }

        private bool TryMatchInitializeArray(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
        {
            if (!TryGetCallTarget(opcode, reader, methodIL, out MethodDesc method)
                || !IsRuntimeHelpersInitializeArray(method))
            {
                return false;
            }

            return methodIL.GetObject(_fieldToken) is FieldDesc field
                && !field.OwningType.IsRuntimeDeterminedSubtype
                && TryGetRvaFieldSize(field, out int fieldSize)
                && _initializationSize <= fieldSize;
        }

        private bool TryMatchCreateSpan(ILOpcode opcode, in ILReader reader, MethodIL methodIL)
        {
            if (!TryGetCallTarget(opcode, reader, methodIL, out MethodDesc method)
                || !IsRuntimeHelpersCreateSpan(method, out TypeDesc elementType))
            {
                return false;
            }

            if (methodIL.GetObject(_fieldToken) is not FieldDesc field
                || field.OwningType.IsRuntimeDeterminedSubtype
                || !TryGetRvaFieldSize(field, out int fieldSize))
            {
                return false;
            }

            LayoutInt elementSize = elementType.GetElementSize();
            return !elementSize.IsIndeterminate
                && elementSize.AsInt > 0
                && fieldSize / elementSize.AsInt > 0;
        }

        private static bool TryGetCallTarget(ILOpcode opcode, in ILReader reader, MethodIL methodIL, out MethodDesc method)
        {
            if (opcode == ILOpcode.call
                && methodIL.GetObject(reader.PeekILToken()) is MethodDesc targetMethod)
            {
                method = targetMethod;
                return true;
            }

            method = null;
            return false;
        }

        private static bool TryReadInt32Constant(ILOpcode opcode, in ILReader reader, out int value)
        {
            ILReader nestedReader = reader;
            switch (opcode)
            {
                case ILOpcode.ldc_i4_m1:
                    value = -1;
                    return true;
                case >= ILOpcode.ldc_i4_0 and <= ILOpcode.ldc_i4_8:
                    value = opcode - ILOpcode.ldc_i4_0;
                    return true;
                case ILOpcode.ldc_i4_s:
                    value = (sbyte)nestedReader.ReadILByte();
                    return true;
                case ILOpcode.ldc_i4:
                    value = (int)nestedReader.ReadILUInt32();
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        private static bool TryGetInitializeArrayElementSize(TypeDesc elementType, out int elementSize)
        {
            if (elementType.IsEnum)
                elementType = elementType.UnderlyingType;

            if ((!elementType.IsPrimitive || elementType.IsVoid)
                && !elementType.IsPointer
                && !elementType.IsFunctionPointer)
            {
                elementSize = 0;
                return false;
            }

            LayoutInt layoutSize = elementType.GetElementSize();
            elementSize = layoutSize.IsIndeterminate ? 0 : layoutSize.AsInt;
            return elementSize > 0;
        }

        private static bool TryGetRvaFieldSize(FieldDesc field, out int fieldSize)
        {
            if (!field.HasRva)
            {
                fieldSize = 0;
                return false;
            }

            LayoutInt layoutSize = field.FieldType.GetElementSize();
            fieldSize = layoutSize.IsIndeterminate ? 0 : layoutSize.AsInt;
            return fieldSize > 0;
        }

        private static bool TryMultiplyPositive(int left, int right, out int product)
        {
            if (left <= 0 || right <= 0 || left > int.MaxValue / right)
            {
                product = 0;
                return false;
            }

            product = left * right;
            return true;
        }

        private static bool IsRuntimeHelpersInitializeArray(MethodDesc method)
        {
            MethodSignature signature = method.Signature;
            return method.Name == "InitializeArray"u8
                && IsRuntimeHelpersIntrinsic(method)
                && method.Instantiation.Length == 0
                && signature.IsStatic
                && signature.Length == 2
                && signature.ReturnType.IsVoid
                && signature[0].IsWellKnownType(WellKnownType.Array)
                && signature[1].IsWellKnownType(WellKnownType.RuntimeFieldHandle);
        }

        private static bool IsRuntimeHelpersCreateSpan(MethodDesc method, out TypeDesc elementType)
        {
            MethodSignature signature = method.Signature;
            if (method.Name == "CreateSpan"u8
                && IsRuntimeHelpersIntrinsic(method)
                && method.Instantiation.Length == 1
                && signature.IsStatic
                && signature.Length == 1
                && signature[0].IsWellKnownType(WellKnownType.RuntimeFieldHandle)
                && (method.Instantiation[0].IsPrimitive || method.Instantiation[0].IsEnum))
            {
                elementType = method.Instantiation[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static bool IsRuntimeHelpersIntrinsic(MethodDesc method)
        {
            return method.IsIntrinsic
                && method.OwningType is MetadataType owningType
                && owningType.Name == "RuntimeHelpers"u8
                && owningType.Namespace == "System.Runtime.CompilerServices"u8
                && owningType.Module == owningType.Context.SystemModule;
        }
    }
}
