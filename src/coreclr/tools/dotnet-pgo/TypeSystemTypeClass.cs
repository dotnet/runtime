// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.IL;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.IO.Compression;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Diagnostics.CodeAnalysis;
using ILCompiler.Reflection.ReadyToRun;
using Microsoft.Diagnostics.Tools.Pgo;
using Internal.Pgo;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    class TypeSystemTypeClass : IMibcEmitterTypeClass<MethodDesc, TypeDesc, TypeSystemEntityOrUnknown>
    {
        public string GetAssemblyString(TypeDesc type) => ((MetadataType)type).Module.Assembly.GetName().Name;
        public TypeDesc GetExactType(TypeSystemEntityOrUnknown type) => type.AsType;
        public ReadOnlySpan<TypeDesc> GetInstantiation(TypeDesc type) => type.Instantiation;
        public ReadOnlySpan<TypeDesc> GetInstantiation(MethodDesc method) => method.Instantiation;
        public EntityHandle GetMethodRef(TypeSystemMetadataEmitter emitter, MethodDesc method) => emitter.GetMethodRef(method);
        public TypeDesc GetOwningType(MethodDesc method) => method.OwningType;
        public TypeDesc GetParameterType(TypeDesc type) => type.GetParameterType();
        public EntityHandle GetTypeRef(TypeSystemMetadataEmitter emitter, TypeDesc type) => emitter.GetTypeRef(type);
        public int GetUnknownTypeIndex(TypeSystemEntityOrUnknown type) => type.AsUnknown;
        public bool IsCanonicalDefinitionType(TypeDesc type) => type.Context.IsCanonicalDefinitionType(type, CanonicalFormKind.Any);
        public bool IsExactType(TypeSystemEntityOrUnknown type) => type.AsType != null;
        public bool IsParameterizedType(TypeDesc type) => type.IsParameterizedType;
        public bool IsPrimitive(TypeDesc type) => type.IsPrimitive;
    }
}
