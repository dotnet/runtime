// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// NOTE: This is a generated file - do not manually edit!

#pragma warning disable 649, SA1121, IDE0036, SA1129

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Internal.LowLevelLinq;
using Internal.NativeFormat;
using Debug = System.Diagnostics.Debug;

namespace Internal.Metadata.NativeFormat.Writer
{
    internal static partial class MdBinaryWriter
    {
        public static void Write(this NativeWriter writer, bool[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (bool value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, char[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (char value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, byte[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (byte value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, sbyte[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (sbyte value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, short[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (short value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ushort[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (ushort value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, int[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (int value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, uint[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (uint value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, long[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (long value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ulong[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (ulong value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, float[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (float value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, double[] values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Length);
            foreach (double value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, AssemblyFlags value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, AssemblyHashAlgorithm value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, CallingConventions value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, SignatureCallingConvention value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, EventAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, FieldAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, GenericParameterAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, GenericParameterKind value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, MethodAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, MethodImplAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, MethodSemanticsAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, NamedArgumentMemberKind value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, ParameterAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, PInvokeAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, PropertyAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, TypeAttributes value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, List<MetadataRecord> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (MetadataRecord value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ArraySignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ArraySignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ArraySignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ByReferenceSignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ByReferenceSignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ByReferenceSignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantBooleanArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantBooleanArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantBooleanArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantBooleanValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantBooleanValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantBooleanValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantBoxedEnumValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantBoxedEnumValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantBoxedEnumValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantByteArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantByteArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantByteArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantByteValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantByteValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantByteValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantCharArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantCharArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantCharArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantCharValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantCharValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantCharValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantDoubleArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantDoubleArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantDoubleArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantDoubleValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantDoubleValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantDoubleValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantEnumArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantEnumArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantEnumArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantHandleArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantHandleArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantHandleArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantInt16Array record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantInt16Array> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantInt16Array value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantInt16Value record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantInt16Value> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantInt16Value value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantInt32Array record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantInt32Array> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantInt32Array value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantInt32Value record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantInt32Value> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantInt32Value value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantInt64Array record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantInt64Array> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantInt64Array value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantInt64Value record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantInt64Value> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantInt64Value value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantReferenceValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantReferenceValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantReferenceValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantSByteArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantSByteArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantSByteArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantSByteValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantSByteValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantSByteValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantSingleArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantSingleArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantSingleArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantSingleValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantSingleValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantSingleValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantStringArray record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantStringArray> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantStringArray value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantStringValue record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantStringValue> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantStringValue value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantUInt16Array record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantUInt16Array> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantUInt16Array value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantUInt16Value record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantUInt16Value> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantUInt16Value value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantUInt32Array record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantUInt32Array> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantUInt32Array value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantUInt32Value record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantUInt32Value> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantUInt32Value value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantUInt64Array record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantUInt64Array> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantUInt64Array value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ConstantUInt64Value record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ConstantUInt64Value> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ConstantUInt64Value value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, CustomAttribute record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<CustomAttribute> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (CustomAttribute value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, Event record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<Event> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (Event value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, Field record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<Field> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (Field value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, FieldSignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<FieldSignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (FieldSignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, FunctionPointerSignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<FunctionPointerSignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (FunctionPointerSignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, GenericParameter record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<GenericParameter> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (GenericParameter value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, MemberReference record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<MemberReference> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (MemberReference value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, Method record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<Method> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (Method value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, MethodInstantiation record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<MethodInstantiation> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (MethodInstantiation value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, MethodSemantics record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<MethodSemantics> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (MethodSemantics value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, MethodSignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<MethodSignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (MethodSignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, MethodTypeVariableSignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<MethodTypeVariableSignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (MethodTypeVariableSignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ModifiedType record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ModifiedType> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ModifiedType value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, NamedArgument record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<NamedArgument> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (NamedArgument value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, NamespaceDefinition record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<NamespaceDefinition> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (NamespaceDefinition value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, NamespaceReference record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<NamespaceReference> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (NamespaceReference value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, Parameter record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<Parameter> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (Parameter value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, PointerSignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<PointerSignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (PointerSignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, Property record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<Property> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (Property value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, PropertySignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<PropertySignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (PropertySignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, QualifiedField record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<QualifiedField> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (QualifiedField value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, QualifiedMethod record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<QualifiedMethod> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (QualifiedMethod value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, SZArraySignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<SZArraySignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (SZArraySignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ScopeDefinition record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ScopeDefinition> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ScopeDefinition value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, ScopeReference record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<ScopeReference> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (ScopeReference value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, TypeDefinition record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<TypeDefinition> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (TypeDefinition value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, TypeForwarder record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<TypeForwarder> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (TypeForwarder value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, TypeInstantiationSignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<TypeInstantiationSignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (TypeInstantiationSignature value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, TypeReference record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<TypeReference> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (TypeReference value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, TypeSpecification record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<TypeSpecification> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (TypeSpecification value in values)
            {
                writer.Write(value);
            }
        } // Write

        public static void Write(this NativeWriter writer, TypeVariableSignature record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<TypeVariableSignature> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (TypeVariableSignature value in values)
            {
                writer.Write(value);
            }
        } // Write
    } // MdBinaryWriter
} // Internal.Metadata.NativeFormat.Writer
