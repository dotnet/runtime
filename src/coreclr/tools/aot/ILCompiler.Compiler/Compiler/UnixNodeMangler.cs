// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;
using System.Diagnostics;
using System.Globalization;

namespace ILCompiler
{
    public sealed class UnixNodeMangler : NodeMangler
    {
        // Mangled name of boxed version of a type
        public sealed override string MangledBoxedTypeName(TypeDesc type)
        {
            Debug.Assert(type.IsValueType);
            return "Boxed_" + NameMangler.GetMangledTypeName(type);
        }

        public sealed override string MethodTable(TypeDesc type)
        {
            string mangledJustTypeName;

            if (type.IsValueType)
                mangledJustTypeName = MangledBoxedTypeName(type);
            else
                mangledJustTypeName = NameMangler.GetMangledTypeName(type);

            return "_ZTV" + mangledJustTypeName.Length.ToString(CultureInfo.InvariantCulture) + mangledJustTypeName;
        }

        public sealed override string GCStatics(TypeDesc type)
        {
            return "__GCSTATICS" + NameMangler.GetMangledTypeName(type);
        }

        public sealed override string NonGCStatics(TypeDesc type)
        {
            return "__NONGCSTATICS" + NameMangler.GetMangledTypeName(type);
        }

        public sealed override string ThreadStatics(TypeDesc type)
        {
            return NameMangler.CompilationUnitPrefix + "__THREADSTATICS" + NameMangler.GetMangledTypeName(type);
        }

        public sealed override string ThreadStaticsIndex(TypeDesc type)
        {
            return "__TypeThreadStaticIndex" + NameMangler.GetMangledTypeName(type);
        }

        public sealed override string TypeGenericDictionary(TypeDesc type)
        {
            return GenericDictionaryNamePrefix + NameMangler.GetMangledTypeName(type);
        }

        public sealed override string MethodGenericDictionary(MethodDesc method)
        {
            return GenericDictionaryNamePrefix + NameMangler.GetMangledMethodName(method);
        }
    }
}
