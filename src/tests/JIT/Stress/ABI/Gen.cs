// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace ABIStress
{
    // This class allows us to generate random values of specified types.
    internal static class Gen
    {
        private static unsafe TVec GenConstantVector<TVec, TElem>(Random rand)
            where TVec : unmanaged where TElem : unmanaged
        {
            int outerSize = sizeof(TVec);
            int innerSize = sizeof(TElem);
            Debug.Assert(outerSize % innerSize == 0);
            Span<TElem> elements = stackalloc TElem[outerSize / innerSize];
            for (int i = 0; i < elements.Length; i++)
                elements[i] = (TElem)GenConstant(typeof(TElem), null, rand);

            return Unsafe.ReadUnaligned<TVec>(ref Unsafe.As<TElem, byte>(ref elements[0]));
        }

        internal static object GenConstant(Type type, FieldInfo[] fields, Random rand)
        {
            if (type == typeof(byte))
                return (byte)rand.Next(byte.MinValue, byte.MaxValue + 1);

            if (type == typeof(short))
                return (short)rand.Next(short.MinValue, short.MaxValue + 1);

            if (type == typeof(int))
                return (int)rand.Next();

            if (type == typeof(long))
                return ((long)rand.Next() << 32) | (uint)rand.Next();

            if (type == typeof(float))
                return (float)rand.Next(short.MaxValue);

            if (type == typeof(double))
                return (double)rand.Next();

            if (type == typeof(Vector<int>))
                return GenConstantVector<Vector<int>, int>(rand);

            if (type == typeof(Vector128<int>))
                return GenConstantVector<Vector128<int>, int>(rand);

            if (type == typeof(Vector256<int>))
                return GenConstantVector<Vector256<int>, int>(rand);

            Debug.Assert(fields != null);
            return Activator.CreateInstance(type, fields.Select(fi => GenConstant(fi.FieldType, null, rand)).ToArray());
        }
    }

    // Values are expressions of a specified type. We allow these values access
    // to incoming arguments and require both that we can compute them and also
    // that we can emit IL code that loads them on the top of the stack.
    internal abstract class Value
    {
        public Value(TypeEx type)
        {
            Type = type;
        }

        public TypeEx Type { get; }

        public abstract object Get(object[] args);
        public abstract void Emit(ILGenerator il);
    }

    internal class ArgValue : Value
    {
        public ArgValue(TypeEx type, int index) : base(type)
        {
            Index = index;
        }

        public int Index { get; }

        public override object Get(object[] args) => args[Index];
        public override void Emit(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg, checked((short)Index));
        }
    }

    internal class FieldValue : Value
    {
        public FieldValue(Value val, int fieldIndex) : base(new TypeEx(val.Type.Fields[fieldIndex].FieldType))
        {
            Value = val;
            FieldIndex = fieldIndex;
        }

        public Value Value { get; }
        public int FieldIndex { get; }

        public override object Get(object[] args)
        {
            object value = Value.Get(args);
            value = Value.Type.Fields[FieldIndex].GetValue(value);
            return value;
        }

        public override void Emit(ILGenerator il)
        {
            Value.Emit(il);
            il.Emit(OpCodes.Ldfld, Value.Type.Fields[FieldIndex]);
        }
    }

    internal class ConstantValue : Value
    {
        public ConstantValue(TypeEx type, object value) : base(type)
        {
            Value = value;
        }

        public object Value { get; }

        public override object Get(object[] args) => Value;
        public override void Emit(ILGenerator il)
        {
            if (Type.Fields == null)
            {
                EmitLoadPrimitive(il, Value);
                return;
            }

            foreach (FieldInfo field in Type.Fields)
                EmitLoadPrimitive(il, field.GetValue(Value));

            il.Emit(OpCodes.Newobj, Type.Ctor);
        }

        private static unsafe void EmitLoadBlittable<T>(ILGenerator il, T val) where T : unmanaged
        {
            LocalBuilder local = il.DeclareLocal(typeof(T));
            for (int i = 0; i < sizeof(T); i++)
            {
                il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Ldc_I4, (int)Unsafe.Add(ref Unsafe.As<T, byte>(ref val), i));
                il.Emit(OpCodes.Stind_I1);
            }

            il.Emit(OpCodes.Ldloc, local);
        }

        internal static void EmitLoadPrimitive(ILGenerator il, object val)
        {
            Type ty = val.GetType();
            if (ty == typeof(byte))
                il.Emit(OpCodes.Ldc_I4, (int)(byte)val);
            else if (ty == typeof(short))
                il.Emit(OpCodes.Ldc_I4, (int)(short)val);
            else if (ty == typeof(int))
                il.Emit(OpCodes.Ldc_I4, (int)val);
            else if (ty == typeof(long))
                il.Emit(OpCodes.Ldc_I8, (long)val);
            else if (ty == typeof(float))
                il.Emit(OpCodes.Ldc_R4, (float)val);
            else if (ty == typeof(double))
                il.Emit(OpCodes.Ldc_R8, (double)val);
            else if (ty == typeof(Vector<int>))
                EmitLoadBlittable(il, (Vector<int>)val);
            else if (ty == typeof(Vector128<int>))
                EmitLoadBlittable(il, (Vector128<int>)val);
            else if (ty == typeof(Vector256<int>))
                EmitLoadBlittable(il, (Vector256<int>)val);
            else
                throw new NotSupportedException("Other primitives are currently not supported");
        }
    }
}
