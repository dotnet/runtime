// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.IL;
using Internal.IL.Stubs;
using Debug = System.Diagnostics.Debug;
using System.Threading;

namespace Internal.TypeSystem.Interop
{
    /// <summary>
    /// This generates a class which inherits from System.Runtime.InteropServices.NativeFunctionPointerWrapper. It has a constructror and
    /// a second instance method which marshalls arguments in the forward direction in order to call the native function pointer.
    /// </summary>
    public partial class PInvokeDelegateWrapper : MetadataType
    {
        // The type of delegate that will be created from the native function pointer
        public MetadataType DelegateType
        {
            get;
        }

        public override ModuleDesc Module
        {
            get;
        }

        public override string Name
        {
            get
            {
                return "PInvokeDelegateWrapper__" + DelegateType.Name;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return "PInvokeDelegateWrapper__" + DelegateType.DiagnosticName;
            }
        }

        public override string Namespace
        {
            get
            {
                return "Internal.CompilerGenerated";
            }
        }

        public override string DiagnosticNamespace
        {
            get
            {
                return "Internal.CompilerGenerated";
            }
        }

        public override bool IsExplicitLayout
        {
            get
            {
                return false;
            }
        }

        public override PInvokeStringFormat PInvokeStringFormat
        {
            get
            {
                return PInvokeStringFormat.AnsiClass;
            }
        }

        public override bool IsSequentialLayout
        {
            get
            {
                return false;
            }
        }

        public override bool IsBeforeFieldInit
        {
            get
            {
                return false;
            }
        }

        public override MetadataType MetadataBaseType
        {
            get
            {
                return InteropTypes.GetNativeFunctionPointerWrapper(Context);
            }
        }

        public override DefType BaseType
        {
            get
            {
                return InteropTypes.GetNativeFunctionPointerWrapper(Context);
            }
        }

        public override bool IsSealed
        {
            get
            {
                return true;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override DefType ContainingType
        {
            get
            {
                return null;
            }
        }

        public override DefType[] ExplicitlyImplementedInterfaces
        {
            get
            {
                return Array.Empty<DefType>();
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return DelegateType.Context;
            }
        }

        private InteropStateManager _interopStateManager;

        public PInvokeDelegateWrapper(ModuleDesc owningModule, MetadataType delegateType, InteropStateManager interopStateManager)
        {
            Debug.Assert(delegateType.IsDelegate);

            Module = owningModule;
            DelegateType = delegateType;            
            _interopStateManager = interopStateManager;
        }

        public override ClassLayoutMetadata GetClassLayout()
        {
            return default(ClassLayoutMetadata);
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return false;
        }

        public override IEnumerable<MetadataType> GetNestedTypes()
        {
            return Array.Empty<MetadataType>();
        }

        public override MetadataType GetNestedType(string name)
        {
            return null;
        }

        protected override MethodImplRecord[] ComputeVirtualMethodImplsForType()
        {
            return Array.Empty<MethodImplRecord>();
        }

        public override MethodImplRecord[] FindMethodsImplWithMatchingDeclName(string name)
        {
            return Array.Empty<MethodImplRecord>();
        }

        private int _hashCode;

        private void InitializeHashCode()
        {
            var hashCodeBuilder = new Internal.NativeFormat.TypeHashingAlgorithms.HashCodeBuilder(Namespace);

            if (Namespace.Length > 0)
            {
                hashCodeBuilder.Append(".");
            }

            hashCodeBuilder.Append(Name);
            _hashCode = hashCodeBuilder.ToHashCode();
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                InitializeHashCode();
            }
            return _hashCode;
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.HasGenericVarianceComputed) != 0)
            {
                flags |= TypeFlags.HasGenericVarianceComputed;
            }

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                flags |= TypeFlags.Class;
            }

            flags |= TypeFlags.HasFinalizerComputed;
            flags |= TypeFlags.AttributeCacheComputed;

            return flags;
        }

        private MethodDesc[] _methods;

        private void InitializeMethods()
        {
            MethodDesc[] methods = new MethodDesc[] {
                     new PInvokeDelegateWrapperConstructor(this),                             // Constructor
                     new DelegateMarshallingMethodThunk(DelegateType, this, _interopStateManager, 
                        DelegateMarshallingMethodThunkKind.ForwardNativeFunctionWrapper)     // a forward marshalling instance method
                };

            Interlocked.CompareExchange(ref _methods, methods, null);
        }

        public override IEnumerable<MethodDesc> GetMethods()
        {
            if (_methods == null)
            {
                InitializeMethods();
            }
            return _methods;
        }

        public MethodDesc GetPInvokeDelegateWrapperMethod(PInvokeDelegateWrapperMethodKind kind)
        {
            if (_methods == null)
            {
                InitializeMethods();
            }

            Debug.Assert((int)kind < _methods.Length);

            return _methods[(int)kind];
        }
    }

    public enum PInvokeDelegateWrapperMethodKind : byte
    {
        Constructor = 0,
        Invoke = 1
    }

}
