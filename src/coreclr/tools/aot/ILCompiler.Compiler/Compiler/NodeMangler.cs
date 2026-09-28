// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler
{
    //
    // NodeMangler is responsible for producing mangled names for specific nodes
    // and for node-related purposes, where the name needs to be in a special format
    // on some platform
    //
    public abstract class NodeMangler
    {
        public NameMangler NameMangler;

        protected static readonly Utf8String GenericDictionaryNamePrefix = new Utf8String("__GenericDict_");

        // Mangled name of boxed version of a type
        public abstract Utf8String MangledBoxedTypeName(TypeDesc type);

        public abstract Utf8String MethodTable(TypeDesc type);
        public abstract Utf8String GCStatics(TypeDesc type);
        public abstract Utf8String NonGCStatics(TypeDesc type);
        public abstract Utf8String ThreadStatics(TypeDesc type);
        public abstract Utf8String ThreadStaticsIndex(TypeDesc type);
        public abstract Utf8String TypeGenericDictionary(TypeDesc type);
        public abstract Utf8String MethodGenericDictionary(MethodDesc method);
        public abstract Utf8String ExternMethod(Utf8String unmangledName, MethodDesc method);
        public abstract Utf8String ExternVariable(Utf8String unmangledName);
    }
}
