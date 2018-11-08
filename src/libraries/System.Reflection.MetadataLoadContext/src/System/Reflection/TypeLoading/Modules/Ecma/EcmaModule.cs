// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all Module objects created by a MetadataLoadContext and get its metadata from a PEReader.
    /// </summary>
    internal sealed partial class EcmaModule : RoModule
    {
        private const int ModuleTypeToken = 0x02000001;

        private readonly EcmaAssembly _assembly;
        private readonly GuardedPEReader _guardedPEReader;

        //
        // "fullyQualifiedName" determines the string returned by Module.FullyQualifiedName. It is typically set to the full path of the
        // file on disk containing the module.
        //
        internal EcmaModule(EcmaAssembly assembly, string fullyQualifiedName, PEReader peReader, MetadataReader reader)
            : base(fullyQualifiedName)
        {
            Debug.Assert(assembly != null);
            Debug.Assert(fullyQualifiedName != null);
            Debug.Assert(peReader != null);
            Debug.Assert(reader != null);

            _assembly = assembly;
            _guardedPEReader = new GuardedPEReader(assembly.Loader, peReader, reader);
            _neverAccessThisExceptThroughModuleDefinitionProperty = reader.GetModuleDefinition();
        }

        internal sealed override RoAssembly GetRoAssembly() => _assembly;
        internal EcmaAssembly GetEcmaAssembly() => _assembly;

        public sealed override bool IsResource() => false;
        public sealed override int MDStreamVersion => throw new NotSupportedException(SR.NotSupported_MDStreamVersion);
        public sealed override int MetadataToken => 0x00000001; // The Module Table is exactly 1 record long so the token is always mdtModule | 0x000001 
        public sealed override Guid ModuleVersionId => ModuleDefinition.Mvid.GetGuid(Reader);
        public sealed override string ScopeName => ModuleDefinition.Name.GetString(Reader);

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes  => ModuleDefinition.GetCustomAttributes().ToTrueCustomAttributes(this);

        internal MethodInfo ComputeEntryPoint(bool fileRefEntryPointAllowed)
        {
            PEHeaders peHeaders = PEReader.PEHeaders;
            CorHeader corHeader = peHeaders.CorHeader;

            if ((corHeader.Flags & CorFlags.NativeEntryPoint) != 0)
                return null;

            int entryPointToken = corHeader.EntryPointTokenOrRelativeVirtualAddress;
            Handle handle = entryPointToken.ToHandle();
            if (handle.IsNil)
                return null;

            HandleKind kind = handle.Kind;
            switch (kind)
            {
                case HandleKind.MethodDefinition:
                    {
                        MethodDefinitionHandle mdh = (MethodDefinitionHandle)handle;
                        return mdh.ResolveMethod<MethodInfo>(this, default);
                    }

                case HandleKind.AssemblyFile:
                    {
                        if (!fileRefEntryPointAllowed)
                            throw new BadImageFormatException();

                        MetadataReader reader = Reader;
                        string moduleName = ((AssemblyFileHandle)handle).GetAssemblyFile(reader).Name.GetString(reader);
                        EcmaModule roModule = (EcmaModule)(Assembly.GetModule(moduleName));
                        return roModule.ComputeEntryPoint(fileRefEntryPointAllowed: false);
                    }

                default:
                    throw new BadImageFormatException();
            }
        }

        public sealed override void GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine)
        {
            PEHeaders peHeaders = PEReader.PEHeaders;
            PEMagic peMagic = peHeaders.PEHeader.Magic;
            Machine coffMachine = peHeaders.CoffHeader.Machine;
            CorFlags corFlags = peHeaders.CorHeader.Flags;

            peKind = default;
            if ((corFlags & CorFlags.ILOnly) != 0)
                peKind |= PortableExecutableKinds.ILOnly;

            if ((corFlags & CorFlags.Prefers32Bit) != 0)
                peKind |= PortableExecutableKinds.Preferred32Bit;
            else if ((corFlags & CorFlags.Requires32Bit) != 0)
                peKind |= PortableExecutableKinds.Required32Bit;

            if (peMagic == PEMagic.PE32Plus)
                peKind |= PortableExecutableKinds.PE32Plus;

            machine = (ImageFileMachine)coffMachine;
        }

        //
        // Search for members on <Module> type.
        //
        public sealed override FieldInfo GetField(string name, BindingFlags bindingAttr) => GetModuleType().GetField(name, bindingAttr);
        public sealed override FieldInfo[] GetFields(BindingFlags bindingFlags) => GetModuleType().GetFields(bindingFlags);
        public sealed override MethodInfo[] GetMethods(BindingFlags bindingFlags) => GetModuleType().GetMethods(bindingFlags);
        protected sealed override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers) => GetModuleType().InternalGetMethodImpl(name, bindingAttr, binder, callConvention, types, modifiers);
        private RoType GetModuleType() => ModuleTypeToken.ToTypeDefinitionHandle().ResolveTypeDef(this);

        public sealed override Type[] GetTypes()
        {
            // Note: we don't expect this process to generate any loader exceptions so no need to implement the ReflectionTypeLoadException
            // logic.

            EnsureTypeDefTableFullyFilled();
            return TypeDefTable.ToArray<Type>(skip: 1); // 0x02000001 is the <Module> type which is always skipped by this api.
        }

        internal sealed override IEnumerable<RoType> GetDefinedRoTypes()
        {
            EnsureTypeDefTableFullyFilled();
            return TypeDefTable.EnumerateValues(skip: 1); // 0x02000001 is the <Module> type which is always skipped by this api.
        }

        internal PEReader PEReader => _guardedPEReader.PEReader;
        internal MetadataReader Reader => _guardedPEReader.Reader;

        private ref readonly ModuleDefinition ModuleDefinition { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughModuleDefinitionProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly ModuleDefinition _neverAccessThisExceptThroughModuleDefinitionProperty;
    }
}
