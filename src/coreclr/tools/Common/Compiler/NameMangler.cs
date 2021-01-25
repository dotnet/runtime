// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler
{
    //
    // NameMangler is responsible for giving extern C/C++ names to managed types, methods and fields
    //
    // The key invariant is that the mangled names are independent on the compilation order.
    //
    public abstract class NameMangler
    {
#if !READYTORUN
        public NameMangler(NodeMangler nodeMangler)
        {
            nodeMangler.NameMangler = this;
            NodeMangler = nodeMangler;
        }

        public NodeMangler NodeMangler { get; private set; }
#endif

        public abstract string CompilationUnitPrefix { get; set; }

        public abstract string SanitizeName(string s, bool typeName = false);

        public abstract string GetMangledTypeName(TypeDesc type);

        public abstract Utf8String GetMangledMethodName(MethodDesc method);

        public abstract Utf8String GetMangledFieldName(FieldDesc field);

        public abstract string GetMangledStringName(string literal);
    }
}
