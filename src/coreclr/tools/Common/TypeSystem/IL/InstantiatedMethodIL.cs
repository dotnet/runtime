// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL
{
    public sealed partial class InstantiatedMethodIL : MethodIL
    {
        private MethodDesc _method;
        private MethodIL _methodIL;
        private Instantiation _typeInstantiation;
        private Instantiation _methodInstantiation;

        public InstantiatedMethodIL(MethodDesc owningMethod, MethodIL methodIL)
        {
            Debug.Assert(methodIL.GetMethodILDefinition() == methodIL);
            Debug.Assert(owningMethod.HasInstantiation || owningMethod.OwningType.HasInstantiation);
            Debug.Assert(owningMethod.GetTypicalMethodDefinition() == methodIL.OwningMethod);

            _methodIL = methodIL;
            _method = owningMethod;

            _typeInstantiation = owningMethod.OwningType.Instantiation;
            _methodInstantiation = owningMethod.Instantiation;
        }

        public override MethodDesc OwningMethod
        {
            get
            {
                return _method;
            }
        }

        public override byte[] GetILBytes()
        {
            return _methodIL.GetILBytes();
        }

        public override int MaxStack
        {
            get
            {
                return _methodIL.MaxStack;
            }
        }

        public override ILExceptionRegion[] GetExceptionRegions()
        {
            return _methodIL.GetExceptionRegions();
        }

        public override bool IsInitLocals
        {
            get
            {
                return _methodIL.IsInitLocals;
            }
        }

        public override LocalVariableDefinition[] GetLocals()
        {
            LocalVariableDefinition[] locals = _methodIL.GetLocals();
            LocalVariableDefinition[] clone = null;

            for (int i = 0; i < locals.Length; i++)
            {
                TypeDesc uninst = locals[i].Type;
                TypeDesc inst = uninst.InstantiateSignature(_typeInstantiation, _methodInstantiation);
                if (uninst != inst)
                {
                    if (clone == null)
                    {
                        clone = new LocalVariableDefinition[locals.Length];
                        for (int j = 0; j < clone.Length; j++)
                        {
                            clone[j] = locals[j];
                        }
                    }
                    clone[i] = new LocalVariableDefinition(inst, locals[i].IsPinned);
                }
            }

            return clone ?? locals;
        }

        public override object GetObject(int token, NotFoundBehavior notFoundBehavior)
        {
            object o = _methodIL.GetObject(token, notFoundBehavior);

            if (o is MethodDesc)
            {
                o = ((MethodDesc)o).InstantiateSignature(_typeInstantiation, _methodInstantiation);
            }
            else if (o is TypeDesc)
            {
                o = ((TypeDesc)o).InstantiateSignature(_typeInstantiation, _methodInstantiation);
            }
            else if (o is FieldDesc)
            {
                o = ((FieldDesc)o).InstantiateSignature(_typeInstantiation, _methodInstantiation);
            }
            else if (o is MethodSignature template)
            {
                MethodSignatureBuilder builder = new MethodSignatureBuilder(template);

                builder.ReturnType = template.ReturnType.InstantiateSignature(_typeInstantiation, _methodInstantiation);
                for (int i = 0; i < template.Length; i++)
                    builder[i] = template[i].InstantiateSignature(_typeInstantiation, _methodInstantiation);

                o = builder.ToSignature();
            }


            return o;
        }

        public override MethodIL GetMethodILDefinition()
        {
            return _methodIL;
        }
    }
}
