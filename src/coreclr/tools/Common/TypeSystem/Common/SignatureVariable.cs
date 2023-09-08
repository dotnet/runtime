// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeFormat;

namespace Internal.TypeSystem
{
    public abstract partial class SignatureVariable : TypeDesc
    {
        private TypeSystemContext _context;
        private int _index;

        internal SignatureVariable(TypeSystemContext context, int index)
        {
            _context = context;
            _index = index;
        }

        public int Index
        {
            get
            {
                return _index;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _context;
            }
        }

        public abstract bool IsMethodSignatureVariable
        {
            get;
        }
    }

    public sealed partial class SignatureTypeVariable : SignatureVariable
    {
        internal SignatureTypeVariable(TypeSystemContext context, int index) : base(context, index)
        {
        }

        public override bool IsMethodSignatureVariable
        {
            get
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return TypeHashingAlgorithms.ComputeSignatureVariableHashCode(Index, false);
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                flags |= TypeFlags.SignatureTypeVariable;
            }

            // ******************************************************
            // Do not add other flags here. If you're hitting asserts
            // because a flag wasn't set, this is a bug in the
            // calling code. The fix is not here.
            //
            // The calling code is asking questions that are not
            // possible to answer for signature variables
            // (like: is this ByRef-like? We won't know until
            // a substitution happens. Any answer would be wrong.)
            // ******************************************************

            return flags;
        }

        public override TypeDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            return typeInstantiation.IsNull ? this : typeInstantiation[Index];
        }
    }

    public sealed partial class SignatureMethodVariable : SignatureVariable
    {
        internal SignatureMethodVariable(TypeSystemContext context, int index) : base(context, index)
        {
        }

        public override bool IsMethodSignatureVariable
        {
            get
            {
                return true;
            }
        }

        public override int GetHashCode()
        {
            return TypeHashingAlgorithms.ComputeSignatureVariableHashCode(Index, true);
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = 0;

            if ((mask & TypeFlags.CategoryMask) != 0)
            {
                flags |= TypeFlags.SignatureMethodVariable;
            }

            // ******************************************************
            // Do not add other flags here. If you're hitting asserts
            // because a flag wasn't set, this is a bug in the
            // calling code. The fix is not here.
            //
            // The calling code is asking questions that are not
            // possible to answer for signature variables
            // (like: is this ByRef-like? We won't know until
            // a substitution happens. Any answer would be wrong.)
            // ******************************************************

            return flags;
        }

        public override TypeDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            return methodInstantiation.IsNull ? this : methodInstantiation[Index];
        }
    }
}
