// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class EXPRFIELDINFO : EXPR
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
