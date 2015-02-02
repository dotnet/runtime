// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal class EXPRFIELDINFO : EXPR
    {
        public FieldSymbol Field()
        {
            return _field;
        }
        public AggregateType FieldType()
        {
            return _fieldType;
        }
        public void Init(FieldSymbol f, AggregateType ft)
        {
            _field = f;
            _fieldType = ft;
        }
        private FieldSymbol _field;
        private AggregateType _fieldType;
    }
}
