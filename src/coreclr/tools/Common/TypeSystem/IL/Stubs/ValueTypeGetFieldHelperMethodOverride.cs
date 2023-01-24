// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    /// <summary>
    /// Synthetic method override of "int ValueType.__GetFieldHelper(Int32, out EETypePtr)". This method is injected
    /// into all value types that cannot have their Equals(object) and GetHashCode() methods operate on individual
    /// bytes. The purpose of the override is to provide access to the value types' fields and their types.
    /// </summary>
    public sealed partial class ValueTypeGetFieldHelperMethodOverride : ILStubMethod
    {
        private DefType _owningType;
        private MethodSignature _signature;

        internal ValueTypeGetFieldHelperMethodOverride(DefType owningType)
        {
            _owningType = owningType;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    TypeSystemContext context = _owningType.Context;
                    TypeDesc int32Type = context.GetWellKnownType(WellKnownType.Int32);
                    TypeDesc eeTypePtrType = context.SystemModule.GetKnownType("System", "EETypePtr");

                    _signature = new MethodSignature(0, 0, int32Type, new[] {
                        int32Type,
                        eeTypePtrType.MakeByRefType()
                    });
                }

                return _signature;
            }
        }

        public override MethodIL EmitIL()
        {
            TypeDesc owningType = _owningType.InstantiateAsOpen();

            ILEmitter emitter = new ILEmitter();

            TypeDesc eeTypePtrType = Context.SystemModule.GetKnownType("System", "EETypePtr");
            MethodDesc eeTypePtrOfMethod = eeTypePtrType.GetKnownMethod("EETypePtrOf", null);
            ILToken eeTypePtrToken = emitter.NewToken(eeTypePtrType);

            var switchStream = emitter.NewCodeStream();
            var getFieldStream = emitter.NewCodeStream();

            ArrayBuilder<ILCodeLabel> fieldGetters = default(ArrayBuilder<ILCodeLabel>);
            foreach (FieldDesc field in owningType.GetFields())
            {
                if (field.IsStatic)
                    continue;

                ILCodeLabel label = emitter.NewCodeLabel();
                fieldGetters.Add(label);

                getFieldStream.EmitLabel(label);
                getFieldStream.EmitLdArg(2);

                // We need something we can instantiate EETypePtrOf over. Also, the classlib
                // code doesn't handle pointers.
                TypeDesc boxableFieldType = field.FieldType;
                if (boxableFieldType.IsPointer || boxableFieldType.IsFunctionPointer)
                    boxableFieldType = Context.GetWellKnownType(WellKnownType.IntPtr);

                // The fact that the type is a reference type is sufficient for the callers.
                // Don't unnecessarily create an MethodTable for the field type.
                if (!boxableFieldType.IsSignatureVariable && !boxableFieldType.IsValueType)
                    boxableFieldType = Context.GetWellKnownType(WellKnownType.Object);

                // If this is an enum, it's okay to Equals/GetHashCode the underlying type.
                // Don't unnecessarily create an MethodTable for the enum.
                boxableFieldType = boxableFieldType.UnderlyingType;

                MethodDesc ptrOfField = eeTypePtrOfMethod.MakeInstantiatedMethod(boxableFieldType);
                getFieldStream.Emit(ILOpcode.call, emitter.NewToken(ptrOfField));

                getFieldStream.Emit(ILOpcode.stobj, eeTypePtrToken);

                getFieldStream.EmitLdArg(0);
                getFieldStream.Emit(ILOpcode.ldflda, emitter.NewToken(field));

                getFieldStream.EmitLdArg(0);

                getFieldStream.Emit(ILOpcode.sub);

                getFieldStream.Emit(ILOpcode.ret);
            }

            if (fieldGetters.Count > 0)
            {
                switchStream.EmitLdArg(1);
                switchStream.EmitSwitch(fieldGetters.ToArray());
            }

            switchStream.EmitLdc(fieldGetters.Count);

            switchStream.Emit(ILOpcode.ret);

            return emitter.Link(this);
        }

        public override Instantiation Instantiation
        {
            get
            {
                return Instantiation.Empty;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return true;
            }
        }

        internal const string MetadataName = "__GetFieldHelper";

        public override string Name
        {
            get
            {
                return MetadataName;
            }
        }

        public override string DiagnosticName
        {
            get
            {
                return MetadataName;
            }
        }
    }
}
