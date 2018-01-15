// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class Expr
    {
        private CType _type;

        protected Expr(ExpressionKind kind)
        {
            Kind = kind;
        }

        internal object RuntimeObject { get; set; }

        internal CType RuntimeObjectActualType { get; set; }

        public ExpressionKind Kind { get; }

        public EXPRFLAG Flags { get; set; }

        public bool IsOptionalArgument { get; set; }

        public string ErrorString { get; set; }

        public CType Type
        {
            get
            {
                Debug.Assert(!(this is ExprList));
                Debug.Assert(_type != null);
                return _type;
            }
            protected set { _type = value; }
        }
    }
}
