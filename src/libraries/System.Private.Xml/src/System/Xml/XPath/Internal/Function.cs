// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml.XPath;

namespace MS.Internal.Xml.XPath
{
    internal sealed class Function : AstNode
    {
        public enum FunctionType
        {
            FuncLast,
            FuncPosition,
            FuncCount,
            FuncID,
            FuncLocalName,
            FuncNameSpaceUri,
            FuncName,
            FuncString,
            FuncBoolean,
            FuncNumber,
            FuncTrue,
            FuncFalse,
            FuncNot,
            FuncConcat,
            FuncStartsWith,
            FuncContains,
            FuncSubstringBefore,
            FuncSubstringAfter,
            FuncSubstring,
            FuncStringLength,
            FuncNormalize,
            FuncTranslate,
            FuncLang,
            FuncSum,
            FuncFloor,
            FuncCeiling,
            FuncRound,
            FuncUserDefined,
        };

        private readonly FunctionType _functionType;
        private readonly List<AstNode> _argumentList;

        private readonly string? _name;
        private readonly string? _prefix;

        public Function(FunctionType ftype, List<AstNode> argumentList)
        {
            _functionType = ftype;
            _argumentList = new List<AstNode>(argumentList);
        }

        public Function(string prefix, string name, List<AstNode> argumentList)
        {
            _functionType = FunctionType.FuncUserDefined;
            _prefix = prefix;
            _name = name;
            _argumentList = new List<AstNode>(argumentList);
        }

        public Function(FunctionType ftype, AstNode arg)
        {
            _functionType = ftype;
            _argumentList = new List<AstNode>();
            _argumentList.Add(arg);
        }

        public override AstType Type { get { return AstType.Function; } }

        public override XPathResultType ReturnType
        {
            get
            {
                return ReturnTypes[(int)_functionType];
            }
        }

        public FunctionType TypeOfFunction { get { return _functionType; } }
        public List<AstNode> ArgumentList { get { return _argumentList; } }
        public string? Prefix { get { return _prefix; } }
        public string? Name { get { return _name; } }

        internal static ReadOnlySpan<XPathResultType> ReturnTypes =>
        [
            /* FunctionType.FuncLast            */ XPathResultType.Number,
            /* FunctionType.FuncPosition        */ XPathResultType.Number,
            /* FunctionType.FuncCount           */ XPathResultType.Number,
            /* FunctionType.FuncID              */ XPathResultType.NodeSet,
            /* FunctionType.FuncLocalName       */ XPathResultType.String,
            /* FunctionType.FuncNameSpaceUri    */ XPathResultType.String,
            /* FunctionType.FuncName            */ XPathResultType.String,
            /* FunctionType.FuncString          */ XPathResultType.String,
            /* FunctionType.FuncBoolean         */ XPathResultType.Boolean,
            /* FunctionType.FuncNumber          */ XPathResultType.Number,
            /* FunctionType.FuncTrue            */ XPathResultType.Boolean,
            /* FunctionType.FuncFalse           */ XPathResultType.Boolean,
            /* FunctionType.FuncNot             */ XPathResultType.Boolean,
            /* FunctionType.FuncConcat          */ XPathResultType.String,
            /* FunctionType.FuncStartsWith      */ XPathResultType.Boolean,
            /* FunctionType.FuncContains        */ XPathResultType.Boolean,
            /* FunctionType.FuncSubstringBefore */ XPathResultType.String,
            /* FunctionType.FuncSubstringAfter  */ XPathResultType.String,
            /* FunctionType.FuncSubstring       */ XPathResultType.String,
            /* FunctionType.FuncStringLength    */ XPathResultType.Number,
            /* FunctionType.FuncNormalize       */ XPathResultType.String,
            /* FunctionType.FuncTranslate       */ XPathResultType.String,
            /* FunctionType.FuncLang            */ XPathResultType.Boolean,
            /* FunctionType.FuncSum             */ XPathResultType.Number,
            /* FunctionType.FuncFloor           */ XPathResultType.Number,
            /* FunctionType.FuncCeiling         */ XPathResultType.Number,
            /* FunctionType.FuncRound           */ XPathResultType.Number,
            /* FunctionType.FuncUserDefined     */ XPathResultType.Any
        ];
    }
}
