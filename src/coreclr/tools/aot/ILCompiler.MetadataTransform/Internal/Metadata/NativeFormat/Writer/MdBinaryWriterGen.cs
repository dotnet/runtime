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

        public static void Write(this NativeWriter writer, AssemblyFlags value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, CallingConventions value)
        {
            writer.WriteUnsigned((uint)value);
        } // Write

        public static void Write(this NativeWriter writer, ConstantTypeCode value)
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

        public static void Write(this NativeWriter writer, Constant record)
        {
            if (record != null)
                writer.WriteUnsigned((uint)record.Handle.Offset);
            else
                writer.WriteUnsigned(0);
        } // Write

        public static void Write(this NativeWriter writer, List<Constant> values)
        {
            if (values == null)
            {
                writer.WriteUnsigned(0);
                return;
            }
            writer.WriteUnsigned((uint)values.Count);
            foreach (Constant value in values)
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
