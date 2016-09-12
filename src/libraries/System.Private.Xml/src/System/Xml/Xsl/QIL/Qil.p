#|
//------------------------------------------------------------------------------
// <copyright file="QIL.p" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">akimball</owner>
//------------------------------------------------------------------------------
Defines the QIL universe, spec at
http://webdata/xml/query/qil/qil.xml

DO NOT MAKE CHANGES TO THIS FILE WITHOUT UPDATING THE SPEC.
|#
(Universe "Qil"

(Generate
    (Prefix        "Qil")
)

(NodeTypes
;                                                                                                       optional
;        nodetype               class           arguments                                               ref or pseudo;
    (Region "meta"
        (QilExpression          Expression      (root))
        (FunctionList           List            ())
        (GlobalVariableList     List            ())
        (GlobalParameterList    List            ())
        (ActualParameterList    List            ())
        (FormalParameterList    List            ())
        (SortKeyList            List            ())
        (BranchList             List            ())
        (OptimizeBarrier        Unary           (child))
        (Unknown                Node            ((xmlType "XmlQueryType")))
    )
    (Region "specials"
        (DataSource             DataSource      (name baseUri))
        (Nop                    Unary           (child))
        (Error                  Unary           (child))
        (Warning                Unary           (child))
    )
    (Region "variables"
        (For                    Iterator        (binding)                                               ref)
        (Let                    Iterator        (binding)                                               ref)
        (Parameter              Parameter       (defaultValue name (xmlType "XmlQueryType"))            ref)
        (PositionOf             Unary           (child))
    )
    (Region "literals"
        (True                   Node            ())
        (False                  Node            ())
        (LiteralString          Literal         ((value "string")))
        (LiteralInt32           Literal         ((value "int")))
        (LiteralInt64           Literal         ((value "long")))
        (LiteralDouble          Literal         ((value "double")))
        (LiteralDecimal         Literal         ((value "decimal")))
        (LiteralQName           Name            ((localName "string") (namespaceUri "string") (prefix "string")))
        (LiteralType            Literal         ((value "XmlQueryType")))
        (LiteralObject          Literal         ((value "object")))
    )
    (Region "boolean operators"
        (And                    Binary          (left right))
        (Or                     Binary          (left right))
        (Not                    Unary           (child))
    )
    (Region "choice"
        (Conditional            Ternary         (left center right))
        (Choice                 Choice          (expression branches))
    )
    (Region "collection operators"
        (Length                 Unary           (child))
        (Sequence               List            ())
        (Union                  Binary          (left right))
        (Intersection           Binary          (left right))
        (Difference             Binary          (left right))
        (Average                Unary           (child))
        (Sum                    Unary           (child))
        (Minimum                Unary           (child))
        (Maximum                Unary           (child))
    )
    (Region "arithmetic operators"
        (Negate                 Unary           (child))
        (Add                    Binary          (left right))
        (Subtract               Binary          (left right))
        (Multiply               Binary          (left right))
        (Divide                 Binary          (left right))
        (Modulo                 Binary          (left right))
    )
    (Region "string operators"
        (StrLength              Unary           (child))
        (StrConcat              StrConcat       (delimiter values))
        (StrParseQName          Binary          (left right))
    )
    (Region "value comparison operators"
        (Ne                     Binary          (left right))
        (Eq                     Binary          (left right))
        (Gt                     Binary          (left right))
        (Ge                     Binary          (left right))
        (Lt                     Binary          (left right))
        (Le                     Binary          (left right))
    )
    (Region "node comparison operators"
        (Is                     Binary          (left right))
        (After                  Binary          (left right))
        (Before                 Binary          (left right))
    )
    (Region "loops"
        (Loop                   Loop            (variable body))
        (Filter                 Loop            (variable body))
    )
    (Region "sorting"
        (Sort                   Loop            (variable body))
        (SortKey                SortKey         (key collation))
        (DocOrderDistinct       Unary           (child))
    )
    (Region "function definition and invocation"
        (Function               Function        (arguments definition sideEffects (xmlType "XmlQueryType")) ref)
        (Invoke                 Invoke          (function arguments))
    )
    (Region "XML navigation"
        (Content                Unary           (child))
        (Attribute              Binary          (left right))
        (Parent                 Unary           (child))
        (Root                   Unary           (child)) 
        (XmlContext             Node            ())
        (Descendant             Unary           (child))
        (DescendantOrSelf       Unary           (child))
        (Ancestor               Unary           (child))
        (AncestorOrSelf         Unary           (child))
        (Preceding              Unary           (child))
        (FollowingSibling       Unary           (child))
        (PrecedingSibling       Unary           (child))
        (NodeRange              Binary          (left right))
        (Deref                  Binary          (left right))
    )
    (Region "XML construction"
        (ElementCtor            Binary          (left right))
        (AttributeCtor          Binary          (left right))
        (CommentCtor            Unary           (child))
        (PICtor                 Binary          (left right))
        (TextCtor               Unary           (child))
        (RawTextCtor            Unary           (child))
        (DocumentCtor           Unary           (child))
        (NamespaceDecl          Binary          (left right))
        (RtfCtor                Binary          (left right))
    )
    (Region "Node properties"
        (NameOf                 Unary           (child))
        (LocalNameOf            Unary           (child))
        (NamespaceUriOf         Unary           (child))
        (PrefixOf               Unary           (child))
    )
    (Region "Type operators"
        (TypeAssert             TargetType      (source targetType))
        (IsType                 TargetType      (source targetType))
        (IsEmpty                Unary           (child))
    )
    (Region "XPath operators"
        (XPathNodeValue         Unary           (child))
        (XPathFollowing         Unary           (child))
        (XPathPreceding         Unary           (child))
        (XPathNamespace         Unary           (child))
     )
     (Region "XSLT"
        (XsltGenerateId         Unary               (child))
        (XsltInvokeLateBound    InvokeLateBound     (name arguments))
        (XsltInvokeEarlyBound   InvokeEarlyBound    (name clrMethod arguments (xmlType "XmlQueryType")))
        (XsltCopy               Binary              (left right))
        (XsltCopyOf             Unary               (child))
        (XsltConvert            TargetType          (source targetType))
     )
); end NodeTypes

); end Universe
