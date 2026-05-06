// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Resource-only modules created by a MetadataLoadContext.
    /// </summary>
    internal sealed class RoResourceModule : RoModule
    {
        private readonly RoAssembly _assembly;

        //
        // "fullyQualifiedName" determines the string returned by Module.FullyQualifiedName. It is typically set to the full path of the
        // file on disk containing the module.
        //
        internal RoResourceModule(RoAssembly assembly, string fullyQualifiedName)
            : base(fullyQualifiedName)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(fullyQualifiedName != null);
            _assembly = assembly;
        }

        internal sealed override RoAssembly GetRoAssembly() => _assembly;

        public sealed override int MDStreamVersion => throw new InvalidOperationException(SR.ResourceOnlyModule);
        public sealed override int MetadataToken => 0x00000000;
        public sealed override Guid ModuleVersionId => throw new InvalidOperationException(SR.ResourceOnlyModule);

        [UnconditionalSuppressMessage("SingleFile", "IL3002:RequiresAssemblyFiles on Name",
            Justification = "https://github.com/dotnet/runtime/issues/56519")]
        public sealed override string ScopeName => Name;
        public sealed override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            peKind = PortableExecutableKinds.NotAPortableExecutableImage;
            machine = default;
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => Array.Empty<CustomAttributeData>();

        public sealed override FieldInfo? GetField(string name, BindingFlags bindingAttr) => null;
        public sealed override FieldInfo[] GetFields(BindingFlags bindingFlags) => Array.Empty<FieldInfo>();
        public sealed override MethodInfo[] GetMethods(BindingFlags bindingFlags) => Array.Empty<MethodInfo>();
        protected sealed override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers) => null;

        public sealed override bool IsResource() => true;

        public sealed override Type[] GetTypes() => Type.EmptyTypes;
        protected sealed override RoDefinitionType? GetTypeCoreNoCache(ReadOnlySpan<byte> ns, ReadOnlySpan<byte> name, out Exception? e)
        {
            e = new TypeLoadException(SR.Format(SR.TypeNotFound, ns.ToUtf16().AppendTypeName(name.ToUtf16()), Assembly));
            return null;
        }

        internal sealed override IEnumerable<RoType>? GetDefinedRoTypes() => null;
    }
}
