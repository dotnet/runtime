// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;

namespace Internal.TypeSystem.Ecma
{
    public sealed partial class EcmaMethod : MethodDesc, EcmaModule.IEntityHandleObject
    {
        private static class MethodFlags
        {
            public const int BasicMetadataCache     = 0x00001;
            public const int Virtual                = 0x00002;
            public const int NewSlot                = 0x00004;
            public const int Abstract               = 0x00008;
            public const int Final                  = 0x00010;
            public const int NoInlining             = 0x00020;
            public const int AggressiveInlining     = 0x00040;
            public const int RuntimeImplemented     = 0x00080;
            public const int InternalCall           = 0x00100;
            public const int Synchronized           = 0x00200;
            public const int AggressiveOptimization = 0x00400;
            public const int NoOptimization         = 0x00800;
            public const int RequireSecObject       = 0x01000;

            public const int AttributeMetadataCache = 0x02000;
            public const int Intrinsic              = 0x04000;
            public const int UnmanagedCallersOnly   = 0x08000;
            public const int RuntimeExport          = 0x10000;
        };

        private EcmaType _type;
        private MethodDefinitionHandle _handle;

        // Cached values
        private ThreadSafeFlags _methodFlags;
        private MethodSignature _signature;
        private string _name;
        private TypeDesc[] _genericParameters; // TODO: Optional field?

        internal EcmaMethod(EcmaType type, MethodDefinitionHandle handle)
        {
            _type = type;
            _handle = handle;

#if DEBUG
            // Initialize name eagerly in debug builds for convenience
            InitializeName();
#endif
        }

        EntityHandle EcmaModule.IEntityHandleObject.Handle
        {
            get
            {
                return _handle;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _type.Module.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _type;
            }
        }

        private MethodSignature InitializeSignature()
        {
            var metadataReader = MetadataReader;
            BlobReader signatureReader = metadataReader.GetBlobReader(metadataReader.GetMethodDefinition(_handle).Signature);

            EcmaSignatureParser parser = new EcmaSignatureParser(Module, signatureReader, NotFoundBehavior.Throw);
            var signature = parser.ParseMethodSignature();
            return (_signature = signature);
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                    return InitializeSignature();
                return _signature;
            }
        }

        public EcmaModule Module
        {
            get
            {
                return _type.EcmaModule;
            }
        }

        public MetadataReader MetadataReader
        {
            get
            {
                return _type.MetadataReader;
            }
        }

        public MethodDefinitionHandle Handle
        {
            get
            {
                return _handle;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int InitializeMethodFlags(int mask)
        {
            int flags = 0;

            if ((mask & MethodFlags.BasicMetadataCache) != 0)
            {
                var methodAttributes = Attributes;
                var methodImplAttributes = ImplAttributes;

                if ((methodAttributes & MethodAttributes.Virtual) != 0)
                    flags |= MethodFlags.Virtual;

                if ((methodAttributes & MethodAttributes.NewSlot) != 0)
                    flags |= MethodFlags.NewSlot;

                if ((methodAttributes & MethodAttributes.Abstract) != 0)
                    flags |= MethodFlags.Abstract;

                if ((methodAttributes & MethodAttributes.Final) != 0)
                    flags |= MethodFlags.Final;

                if ((methodAttributes & MethodAttributes.RequireSecObject) != 0)
                    flags |= MethodFlags.RequireSecObject;

                if ((methodImplAttributes & MethodImplAttributes.NoInlining) != 0)
                    flags |= MethodFlags.NoInlining;

                // System.Reflection.Primitives we build against doesn't define AggressiveOptimization
                const MethodImplAttributes MethodImplAttributes_AggressiveOptimization = (MethodImplAttributes)0x0200;

                // No optimization bit beats aggressive optimization bit (CLR compatible behavior)
                if ((methodImplAttributes & MethodImplAttributes.NoOptimization) != 0)
                    flags |= MethodFlags.NoOptimization;
                else if ((methodImplAttributes & MethodImplAttributes_AggressiveOptimization) != 0)
                    flags |= MethodFlags.AggressiveOptimization;

                if ((methodImplAttributes & MethodImplAttributes.AggressiveInlining) != 0)
                    flags |= MethodFlags.AggressiveInlining;

                if ((methodImplAttributes & MethodImplAttributes.Runtime) != 0)
                    flags |= MethodFlags.RuntimeImplemented;

                if ((methodImplAttributes & MethodImplAttributes.InternalCall) != 0)
                    flags |= MethodFlags.InternalCall;

                if ((methodImplAttributes & MethodImplAttributes.Synchronized) != 0)
                    flags |= MethodFlags.Synchronized;

                flags |= MethodFlags.BasicMetadataCache;
            }

            // Fetching custom attribute based properties is more expensive, so keep that under
            // a separate cache that might not be accessed very frequently.
            if ((mask & MethodFlags.AttributeMetadataCache) != 0)
            {
                var metadataReader = this.MetadataReader;
                var methodDefinition = metadataReader.GetMethodDefinition(_handle);

                foreach (var attributeHandle in methodDefinition.GetCustomAttributes())
                {
                    StringHandle namespaceHandle, nameHandle;
                    if (!metadataReader.GetAttributeNamespaceAndName(attributeHandle, out namespaceHandle, out nameHandle))
                        continue;

                    if (metadataReader.StringComparer.Equals(namespaceHandle, "System.Runtime.CompilerServices"))
                    {
                        if (metadataReader.StringComparer.Equals(nameHandle, "IntrinsicAttribute"))
                        {
                            flags |= MethodFlags.Intrinsic;
                        }
                    }
                    else
                    if (metadataReader.StringComparer.Equals(namespaceHandle, "System.Runtime.InteropServices"))
                    {
                        if (metadataReader.StringComparer.Equals(nameHandle, "UnmanagedCallersOnlyAttribute"))
                        {
                            flags |= MethodFlags.UnmanagedCallersOnly;
                        }
                    }
                    else
                    if (metadataReader.StringComparer.Equals(namespaceHandle, "System.Runtime"))
                    {
                        if (metadataReader.StringComparer.Equals(nameHandle, "RuntimeExportAttribute"))
                        {
                            flags |= MethodFlags.RuntimeExport;
                        }
                    }
                }

                flags |= MethodFlags.AttributeMetadataCache;
            }

            Debug.Assert((flags & mask) != 0);
            _methodFlags.AddFlags(flags);

            return flags & mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetMethodFlags(int mask)
        {
            int flags = _methodFlags.Value & mask;
            if (flags != 0)
                return flags;
            return InitializeMethodFlags(mask);
        }

        public override bool IsVirtual
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.Virtual) & MethodFlags.Virtual) != 0;
            }
        }

        public override bool IsNewSlot
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.NewSlot) & MethodFlags.NewSlot) != 0;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.Abstract) & MethodFlags.Abstract) != 0;
            }
        }

        public override bool IsFinal
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.Final) & MethodFlags.Final) != 0;
            }
        }

        public override bool IsNoInlining
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.NoInlining) & MethodFlags.NoInlining) != 0;
            }
        }

        public override bool RequireSecObject
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.RequireSecObject) & MethodFlags.RequireSecObject) != 0;
            }
        }

        public override bool IsAggressiveOptimization
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.AggressiveOptimization) & MethodFlags.AggressiveOptimization) != 0;
            }
        }

        public override bool IsNoOptimization
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.NoOptimization) & MethodFlags.NoOptimization) != 0;
            }
        }

        public override bool IsAggressiveInlining
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.AggressiveInlining) & MethodFlags.AggressiveInlining) != 0;
            }
        }

        public override bool IsRuntimeImplemented
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.RuntimeImplemented) & MethodFlags.RuntimeImplemented) != 0;
            }
        }

        public override bool IsIntrinsic
        {
            get
            {
                return (GetMethodFlags(MethodFlags.AttributeMetadataCache | MethodFlags.Intrinsic) & MethodFlags.Intrinsic) != 0;
            }
        }

        public override bool IsInternalCall
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.InternalCall) & MethodFlags.InternalCall) != 0;
            }
        }

        public override bool IsSynchronized
        {
            get
            {
                return (GetMethodFlags(MethodFlags.BasicMetadataCache | MethodFlags.Synchronized) & MethodFlags.Synchronized) != 0;
            }
        }

        public override bool IsUnmanagedCallersOnly
        {
            get
            {
                return (GetMethodFlags(MethodFlags.AttributeMetadataCache | MethodFlags.UnmanagedCallersOnly) & MethodFlags.UnmanagedCallersOnly) != 0;
            }
        }

        public override bool IsRuntimeExport
        {
            get
            {
                return (GetMethodFlags(MethodFlags.AttributeMetadataCache | MethodFlags.RuntimeExport) & MethodFlags.RuntimeExport) != 0;
            }
        }

        public override bool IsSpecialName
        {
            get
            {
                return (Attributes & MethodAttributes.SpecialName) != 0;
            }
        }

        public override bool IsDefaultConstructor
        {
            get
            {
                MethodAttributes attributes = Attributes;
                return attributes.IsRuntimeSpecialName() 
                    && attributes.IsPublic()
                    && Signature.Length == 0
                    && Name == ".ctor"
                    && !_type.IsAbstract;
            }
        }

        public MethodAttributes Attributes
        {
            get
            {
                return MetadataReader.GetMethodDefinition(_handle).Attributes;
            }
        }

        public MethodImplAttributes ImplAttributes
        {
            get
            {
                return MetadataReader.GetMethodDefinition(_handle).ImplAttributes;
            }
        }

        private string InitializeName()
        {
            var metadataReader = MetadataReader;
            var name = metadataReader.GetString(metadataReader.GetMethodDefinition(_handle).Name);
            return (_name = name);
        }

        public override string Name
        {
            get
            {
                if (_name == null)
                    return InitializeName();
                return _name;
            }
        }

        private void ComputeGenericParameters()
        {
            var genericParameterHandles = MetadataReader.GetMethodDefinition(_handle).GetGenericParameters();
            int count = genericParameterHandles.Count;
            if (count > 0)
            {
                TypeDesc[] genericParameters = new TypeDesc[count];
                int i = 0;
                foreach (var genericParameterHandle in genericParameterHandles)
                {
                    genericParameters[i++] = new EcmaGenericParameter(Module, genericParameterHandle);
                }
                Interlocked.CompareExchange(ref _genericParameters, genericParameters, null);
            }
            else
            {
                _genericParameters = TypeDesc.EmptyTypes;
            }
        }

        public override Instantiation Instantiation
        {
            get
            {
                if (_genericParameters == null)
                    ComputeGenericParameters();
                return new Instantiation(_genericParameters);
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return !MetadataReader.GetCustomAttributeHandle(MetadataReader.GetMethodDefinition(_handle).GetCustomAttributes(),
                attributeNamespace, attributeName).IsNil;
        }

        public override bool IsPInvoke
        {
            get
            {
                return (((int)Attributes & (int)MethodAttributes.PinvokeImpl) != 0);
            }
        }

        public override PInvokeMetadata GetPInvokeMethodMetadata()
        {
            if (!IsPInvoke)
                return default(PInvokeMetadata);

            MetadataReader metadataReader = MetadataReader;
            MethodDefinition methodDef = metadataReader.GetMethodDefinition(_handle);
            MethodImport import = methodDef.GetImport();
            string name = metadataReader.GetString(import.Name);

            ModuleReference moduleRef = metadataReader.GetModuleReference(import.Module);
            string moduleName = metadataReader.GetString(moduleRef.Name);

            MethodImportAttributes importAttributes = import.Attributes;

            // If either BestFitMapping or ThrowOnUnmappable wasn't set on the p/invoke,
            // look for the value in the owning type or assembly.
            if ((importAttributes & MethodImportAttributes.BestFitMappingMask) == 0 ||
                (importAttributes & MethodImportAttributes.ThrowOnUnmappableCharMask) == 0)
            {
                TypeDefinition declaringType = metadataReader.GetTypeDefinition(methodDef.GetDeclaringType());

                // Start with owning type
                MethodImportAttributes fromCA = GetImportAttributesFromBestFitMappingAttribute(declaringType.GetCustomAttributes());
                if ((importAttributes & MethodImportAttributes.BestFitMappingMask) == 0)
                    importAttributes |= fromCA & MethodImportAttributes.BestFitMappingMask;
                if ((importAttributes & MethodImportAttributes.ThrowOnUnmappableCharMask) == 0)
                    importAttributes |= fromCA & MethodImportAttributes.ThrowOnUnmappableCharMask;

                // If we still don't know, check the assembly
                if ((importAttributes & MethodImportAttributes.BestFitMappingMask) == 0 ||
                    (importAttributes & MethodImportAttributes.ThrowOnUnmappableCharMask) == 0)
                {
                    fromCA = GetImportAttributesFromBestFitMappingAttribute(metadataReader.GetAssemblyDefinition().GetCustomAttributes());
                    if ((importAttributes & MethodImportAttributes.BestFitMappingMask) == 0)
                        importAttributes |= fromCA & MethodImportAttributes.BestFitMappingMask;
                    if ((importAttributes & MethodImportAttributes.ThrowOnUnmappableCharMask) == 0)
                        importAttributes |= fromCA & MethodImportAttributes.ThrowOnUnmappableCharMask;
                }
            }

            // Spot check the enums match
            Debug.Assert((int)MethodImportAttributes.CallingConventionStdCall == (int)PInvokeAttributes.CallingConventionStdCall);
            Debug.Assert((int)MethodImportAttributes.CharSetAuto == (int)PInvokeAttributes.CharSetAuto);
            Debug.Assert((int)MethodImportAttributes.CharSetUnicode == (int)PInvokeAttributes.CharSetUnicode);
            Debug.Assert((int)MethodImportAttributes.SetLastError == (int)PInvokeAttributes.SetLastError);

            PInvokeAttributes attributes = (PInvokeAttributes)importAttributes;

            if ((ImplAttributes & MethodImplAttributes.PreserveSig) != 0)
                attributes |= PInvokeAttributes.PreserveSig;

            return new PInvokeMetadata(moduleName, name, attributes);
        }

        private MethodImportAttributes GetImportAttributesFromBestFitMappingAttribute(CustomAttributeHandleCollection attributeHandles)
        {
            // Look for the [BestFitMapping(BestFitMapping: x, ThrowOnUnmappableChar = y)] attribute and
            // translate that to MethodImportAttributes

            MethodImportAttributes result = 0;
            MetadataReader reader = MetadataReader;

            CustomAttributeHandle attributeHandle = reader.GetCustomAttributeHandle(
                attributeHandles, "System.Runtime.InteropServices", "BestFitMappingAttribute");
            if (!attributeHandle.IsNil)
            {
                CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                CustomAttributeValue<TypeDesc> decoded = attribute.DecodeValue(
                    new CustomAttributeTypeProvider(_type.EcmaModule));

                if (decoded.FixedArguments.Length != 1 || !(decoded.FixedArguments[0].Value is bool))
                    ThrowHelper.ThrowBadImageFormatException();
                if ((bool)decoded.FixedArguments[0].Value)
                    result |= MethodImportAttributes.BestFitMappingEnable;
                else
                    result |= MethodImportAttributes.BestFitMappingDisable;

                foreach (CustomAttributeNamedArgument<TypeDesc> namedArg in decoded.NamedArguments)
                {
                    if (namedArg.Name == "ThrowOnUnmappableChar")
                    {
                        if (!(namedArg.Value is bool))
                            ThrowHelper.ThrowBadImageFormatException();
                        if ((bool)namedArg.Value)
                            result |= MethodImportAttributes.ThrowOnUnmappableCharEnable;
                        else
                            result |= MethodImportAttributes.ThrowOnUnmappableCharDisable;
                        break;
                    }
                }
            }

            return result;
        }

        public override ParameterMetadata[] GetParameterMetadata()
        {
            MetadataReader metadataReader = MetadataReader;
            
            // Spot check the enums match
            Debug.Assert((int)ParameterAttributes.In == (int)ParameterMetadataAttributes.In);
            Debug.Assert((int)ParameterAttributes.Out == (int)ParameterMetadataAttributes.Out);
            Debug.Assert((int)ParameterAttributes.Optional == (int)ParameterMetadataAttributes.Optional);
            Debug.Assert((int)ParameterAttributes.HasDefault == (int)ParameterMetadataAttributes.HasDefault);
            Debug.Assert((int)ParameterAttributes.HasFieldMarshal == (int)ParameterMetadataAttributes.HasFieldMarshal);

            ParameterHandleCollection parameterHandles = metadataReader.GetMethodDefinition(_handle).GetParameters();
            ParameterMetadata[] parameterMetadataArray = new ParameterMetadata[parameterHandles.Count];
            int index = 0;
            foreach (ParameterHandle parameterHandle in parameterHandles)
            {
                Parameter parameter = metadataReader.GetParameter(parameterHandle);
                MarshalAsDescriptor marshalAsDescriptor = GetMarshalAsDescriptor(parameter);
                ParameterMetadata data = new ParameterMetadata(parameter.SequenceNumber, (ParameterMetadataAttributes)parameter.Attributes, marshalAsDescriptor);
                parameterMetadataArray[index++] = data;
            }
            return parameterMetadataArray;
        }

        private MarshalAsDescriptor GetMarshalAsDescriptor(Parameter parameter)
        {
            if ((parameter.Attributes & ParameterAttributes.HasFieldMarshal) == ParameterAttributes.HasFieldMarshal)
            {
                MetadataReader metadataReader = MetadataReader;
                BlobReader marshalAsReader = metadataReader.GetBlobReader(parameter.GetMarshallingDescriptor());
                EcmaSignatureParser parser = new EcmaSignatureParser(Module, marshalAsReader, NotFoundBehavior.Throw);
                MarshalAsDescriptor marshalAs = parser.ParseMarshalAsDescriptor();
                Debug.Assert(marshalAs != null);
                return marshalAs;
            }
            return null;
        }
    }
}
