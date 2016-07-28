#|
//------------------------------------------------------------------------------
// <copyright file="ConstantFold.p" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">akimball</owner>
// <spec>http://webdata/xml/query/docs/QIL%20Normalizer%20Functional%20Specification.doc</spec>
//------------------------------------------------------------------------------
|#


#|
###############################################################################################
###
### ConstantFold.p
###
###   These patterns are clear, risk-free performance improvements that reduce the
###   query size by eliminating some computations (eliminating an operator, or
###   folding several operators into one).
###
###   These patterns are based on intrinsic properties of the operators and/or
###   their arguments.  For example, some operators are idempotent (in which case
###   (Op (Op x)) => (Op x)), have an identity value (such that (Op x y) = y for
###   some x), have a zero value (such that (Op x y) = x for all y), etc.
###
###############################################################################################
|#


#|
###-----------------------------------------------------------------------------------------------
### Required Patterns
###-----------------------------------------------------------------------------------------------
|#

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Required                                                         |
 | Description:  Every operator allows operands of type None.  However, since this type is      |
 |               fully abstract and can never actually be instantiated, operators which accept  |
 |               None can usually be folded away.  The only cases which do not fold are:        |
 |               Sequence operands, and the return operand of Loop.                             |
 | Rationale:    Back-ends don't have to test each operand for type None.                       |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[FoldNone] ((DataSource $x:* ^ (None? (TypeOf $x)) *)            => (Nop $x))
[FoldNone] ((DataSource * $x:* ^ (None? (TypeOf $x)))            => (Nop $x))
[FoldNone] ((Error $x:* ^ (None? (TypeOf $x)))                   => (Nop $x))
[FoldNone] ((Warning $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((And $x:* ^ (None? (TypeOf $x)) *)                   => (Nop $x))
[FoldNone] ((And * $x:* ^ (None? (TypeOf $x)))                   => (Nop $x))
[FoldNone] ((Or $x:* ^ (None? (TypeOf $x)) *)                    => (Nop $x))
[FoldNone] ((Or * $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((Not $x:* ^ (None? (TypeOf $x)))                     => (Nop $x))
[FoldNone] ((Conditional $x:* ^ (None? (TypeOf $x)) * *)         => (Nop $x))
[FoldNone] ((Length $x:* ^ (None? (TypeOf $x)))                  => (Nop $x))
[FoldNone] ((Union $x:* ^ (None? (TypeOf $x)) *)                 => (Nop $x))
[FoldNone] ((Union * $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((Intersection $x:* ^ (None? (TypeOf $x)) *)          => (Nop $x))
[FoldNone] ((Intersection * $x:* ^ (None? (TypeOf $x)))          => (Nop $x))
[FoldNone] ((Difference $x:* ^ (None? (TypeOf $x)) *)            => (Nop $x))
[FoldNone] ((Difference * $x:* ^ (None? (TypeOf $x)))            => (Nop $x))
[FoldNone] ((Average $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((Sum $x:* ^ (None? (TypeOf $x)))                     => (Nop $x))
[FoldNone] ((Minimum $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((Maximum $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((Negate $x:* ^ (None? (TypeOf $x)))                  => (Nop $x))
[FoldNone] ((Add $x:* ^ (None? (TypeOf $x)) *)                   => (Nop $x))
[FoldNone] ((Add * $x:* ^ (None? (TypeOf $x)))                   => (Nop $x))
[FoldNone] ((Subtract $x:* ^ (None? (TypeOf $x)) *)              => (Nop $x))
[FoldNone] ((Subtract * $x:* ^ (None? (TypeOf $x)))              => (Nop $x))
[FoldNone] ((Multiply $x:* ^ (None? (TypeOf $x)) *)              => (Nop $x))
[FoldNone] ((Multiply * $x:* ^ (None? (TypeOf $x)))              => (Nop $x))
[FoldNone] ((Divide $x:* ^ (None? (TypeOf $x)) *)                => (Nop $x))
[FoldNone] ((Divide * $x:* ^ (None? (TypeOf $x)))                => (Nop $x))
[FoldNone] ((Modulo $x:* ^ (None? (TypeOf $x)) *)                => (Nop $x))
[FoldNone] ((Modulo * $x:* ^ (None? (TypeOf $x)))                => (Nop $x))
[FoldNone] ((StrLength $x:* ^ (None? (TypeOf $x)))               => (Nop $x))
[FoldNone] ((StrConcat $x:* ^ (None? (TypeOf $x)) *)             => (Nop $x))
[FoldNone] ((StrConcat * $x:* ^ (None? (TypeOf $x)))             => (Nop $x))
[FoldNone] ((StrParseQName $x:* ^ (None? (TypeOf $x)) *)         => (Nop $x))
[FoldNone] ((StrParseQName * $x:* ^ (None? (TypeOf $x)))         => (Nop $x))
[FoldNone] ((Eq $x:* ^ (None? (TypeOf $x)) *)                    => (Nop $x))
[FoldNone] ((Eq * $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((Ne $x:* ^ (None? (TypeOf $x)) *)                    => (Nop $x))
[FoldNone] ((Ne * $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((Gt $x:* ^ (None? (TypeOf $x)) *)                    => (Nop $x))
[FoldNone] ((Gt * $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((Ge $x:* ^ (None? (TypeOf $x)) *)                    => (Nop $x))
[FoldNone] ((Ge * $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((Lt $x:* ^ (None? (TypeOf $x)) *)                    => (Nop $x))
[FoldNone] ((Lt * $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((Le $x:* ^ (None? (TypeOf $x)) *)                    => (Nop $x))
[FoldNone] ((Le * $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((Is $x:* ^ (None? (TypeOf $x)) *)                    => (Nop $x))
[FoldNone] ((Is * $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((After $x:* ^ (None? (TypeOf $x)) *)                 => (Nop $x))
[FoldNone] ((After * $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((Before $x:* ^ (None? (TypeOf $x)) *)                => (Nop $x))
[FoldNone] ((Before * $x:* ^ (None? (TypeOf $x)))                => (Nop $x))
[FoldNone] ((Loop $i:* ^ (None? (TypeOf $i)) *)                  => (Nop (First $i)))
[FoldNone] ((Filter $i:* ^ (None? (TypeOf $i)) *)                => (Nop (First $i)))
[FoldNone] ((Filter $i:* $w:* ^ (None? (TypeOf $w)))             => (Loop $i $w))
[FoldNone] ((Sort $i:* ^ (None? (TypeOf $i)) *)                  => (Nop (First $i)))
[FoldNone] ((DocOrderDistinct $x:* ^ (None? (TypeOf $x)))        => (Nop $x))
[FoldNone] ((Content $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((Attribute $x:* ^ (None? (TypeOf $x)) *)             => (Nop $x))
[FoldNone] ((Attribute * $x:* ^ (None? (TypeOf $x)))             => (Nop $x))
[FoldNone] ((Parent $x:* ^ (None? (TypeOf $x)))                  => (Nop $x))
[FoldNone] ((Root $x:* ^ (None? (TypeOf $x)))                    => (Nop $x))
[FoldNone] ((Descendant $x:* ^ (None? (TypeOf $x)))              => (Nop $x))
[FoldNone] ((DescendantOrSelf $x:* ^ (None? (TypeOf $x)))        => (Nop $x))
[FoldNone] ((Ancestor $x:* ^ (None? (TypeOf $x)))                => (Nop $x))
[FoldNone] ((AncestorOrSelf $x:* ^ (None? (TypeOf $x)))          => (Nop $x))
[FoldNone] ((Preceding $x:* ^ (None? (TypeOf $x)))               => (Nop $x))
[FoldNone] ((FollowingSibling $x:* ^ (None? (TypeOf $x)))        => (Nop $x))
[FoldNone] ((PrecedingSibling $x:* ^ (None? (TypeOf $x)))        => (Nop $x))
[FoldNone] ((NodeRange $x:* ^ (None? (TypeOf $x)) *)             => (Nop $x))
[FoldNone] ((NodeRange * $x:* ^ (None? (TypeOf $x)))             => (Nop $x))
[FoldNone] ((Deref $x:* ^ (None? (TypeOf $x)) *)                 => (Nop $x))
[FoldNone] ((Deref * $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((ElementCtor $x:* ^ (None? (TypeOf $x)) *)           => (Nop $x))
[FoldNone] ((ElementCtor * $x:* ^ (None? (TypeOf $x)))           => (Nop $x))
[FoldNone] ((AttributeCtor $x:* ^ (None? (TypeOf $x)) *)         => (Nop $x))
[FoldNone] ((AttributeCtor * $x:* ^ (None? (TypeOf $x)))         => (Nop $x))
[FoldNone] ((CommentCtor $x:* ^ (None? (TypeOf $x)))             => (Nop $x))
[FoldNone] ((PICtor $x:* ^ (None? (TypeOf $x)) *)                => (Nop $x))
[FoldNone] ((PICtor * $x:* ^ (None? (TypeOf $x)))                => (Nop $x))
[FoldNone] ((TextCtor $x:* ^ (None? (TypeOf $x)))                => (Nop $x))
[FoldNone] ((RawTextCtor $x:* ^ (None? (TypeOf $x)))             => (Nop $x))
[FoldNone] ((DocumentCtor $x:* ^ (None? (TypeOf $x)))            => (Nop $x))
[FoldNone] ((NamespaceDecl $x:* ^ (None? (TypeOf $x)) *)         => (Nop $x))
[FoldNone] ((NamespaceDecl * $x:* ^ (None? (TypeOf $x)))         => (Nop $x))
[FoldNone] ((RtfCtor $x:* ^ (None? (TypeOf $x)) *)               => (Nop $x))
[FoldNone] ((NameOf $x:* ^ (None? (TypeOf $x)))                  => (Nop $x))
[FoldNone] ((LocalNameOf $x:* ^ (None? (TypeOf $x)))             => (Nop $x))
[FoldNone] ((NamespaceUriOf $x:* ^ (None? (TypeOf $x)))          => (Nop $x))
[FoldNone] ((PrefixOf $x:* ^ (None? (TypeOf $x)))                => (Nop $x))
[FoldNone] ((TypeAssert $x:* ^ (None? (TypeOf $x)) *)            => (Nop $x))
[FoldNone] ((IsType $x:* ^ (None? (TypeOf $x)) *)                => (Nop $x))
[FoldNone] ((IsEmpty $x:* ^ (None? (TypeOf $x)))                 => (Nop $x))
[FoldNone] ((XPathNodeValue $x:* ^ (None? (TypeOf $x)))          => (Nop $x))
[FoldNone] ((XPathFollowing $x:* ^ (None? (TypeOf $x)))          => (Nop $x))
[FoldNone] ((XPathPreceding $x:* ^ (None? (TypeOf $x)))          => (Nop $x))
[FoldNone] ((XPathNamespace $x:* ^ (None? (TypeOf $x)))          => (Nop $x))
[FoldNone] ((XsltGenerateId $x:* ^ (None? (TypeOf $x)))          => (Nop $x))
[FoldNone] ((XsltCopy $x:* ^ (None? (TypeOf $x)) *)              => (Nop $x))
[FoldNone] ((XsltCopy * $x:* ^ (None? (TypeOf $x)))              => (Nop $x))
[FoldNone] ((XsltCopyOf $x:* ^ (None? (TypeOf $x)))              => (Nop $x))
[FoldNone] ((XsltConvert $x:* ^ (None? (TypeOf $x)) *)           => (Nop $x))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Required                                                         |
 | Description:  If the argument to PositionOf is a Let iterator or a singleton For iterator,   |
 |               then PositionOf is constant 1.                                                 |
 | Rationale:    Elimination of special case that back-ends would otherwise need to detect.     |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminatePositionOf] ((PositionOf $x:* ^ ~(NodeType? $x {For}))       => (LiteralInt32 1))
[EliminatePositionOf] ((PositionOf (For $x:* ^ (Single? (TypeOf $x)))) => (LiteralInt32 1))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalization, Required                                                        |
 | Description:  IsType is a complex operator that combines a cardinality test (operates over   |
 |               sequence as a whole) with a prime type test (operates over individual items in |
 |               the sequence).  These patterns attempt to remove IsType expressions that are   |
 |               degenerate in some way:                                                        |
 |                                                                                              |
 |                 1. If the source expression's type is statically a subtype of the target     |
 |                    type, then IsType can be normalized to True.  (e.g. 1 is xs:decimal)      |
 |                                                                                              |
 |                 2. If the source expression's type can never be a subtype of the target      |
 |                    type, then IsType can be normalized to False.  (e.g. () is xs:string)     |
 |                                                                                              |
 |                 3. If the source expression's prime can never be subtype of the target       |
 |                    prime, then IsType is False if the dynamic cardinality of the source      |
 |                    expression is anything other than 0 (empty sequence).                     |
 |                    (e.g. xs:int? is xs:string*)                                              |
 |                                                                                              |
 | Rationale:    Elimination of special cases that back-ends would otherwise need to detect.    |
 | Dependencies: These patterns must all be enabled together.                                   |
 |----------------------------------------------------------------------------------------------|#
[EliminateIsType]
((IsType
    $opnd:* ^ (NoSideEffects? $opnd)
    (LiteralType $base:*) ^ (SubtypeOf? (TypeOf $opnd) $base)
) =>
(True))

[EliminateIsType]
((IsType
    $opnd:* ^ (NoSideEffects? $opnd)
    (LiteralType $typ:*) ^ (NeverSubtypeOf? (TypeOf $opnd) $typ)
) =>
(False))

[EliminateIsType]
((IsType
    $opnd:*
    (LiteralType $typ:*) ^ (NeverSubtypeOf? (Prime (TypeOf $opnd)) (Prime $typ))
) =>
(IsEmpty $opnd))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalization, Required                                                        |
 | Description:  These patterns are exactly the same as those above, except that they preserve  |
 |               side effects in the IsType operand.                                            |
 | Rationale:    Elimination of special cases that back-ends would otherwise need to detect.    |
 | Dependencies: These patterns must all be enabled together.                                   |
 |----------------------------------------------------------------------------------------------|#
[EliminateIsType]
((IsType
    $opnd:* ^ ~(NoSideEffects? $opnd)
    (LiteralType $base:*) ^ (SubtypeOf? (TypeOf $opnd) $base)
) =>
(Loop (Let $opnd) (True)))

[EliminateIsType]
((IsType
    $opnd:* ^ ~(NoSideEffects? $opnd)
    (LiteralType $typ:*) ^ (NeverSubtypeOf? (TypeOf $opnd) $typ)
) =>
(Loop (Let $opnd) (False)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Required                                                         |
 | Description:  Same as EliminateIsType patterns #2 and #3, but applied to TypeAssert.         |
 |               Pattern #1 applied to TypeAssert is an optional pattern, since there are       |
 |               legitimate cases where the compiler might perform a static upcast.  But        |
 |               patterns #2 and #3 will never actually be executed, and replacing them with    |
 |               errors will reduce special cases that have to be handled later.                |
 | Rationale:    Same as IsType.                                                               |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateTypeAssert]
((TypeAssert $opnd:* (LiteralType $typ:*) ^ (NeverSubtypeOf? (TypeOf $opnd) $typ)) =>
(Error (LiteralString ""))
)

[EliminateTypeAssert]
((TypeAssert $opnd:* (LiteralType $typ:*) ^ (NeverSubtypeOf? (Prime (TypeOf $opnd)) (Prime $typ))) =>
(Conditional
    (IsEmpty $opnd)
    (Sequence)
    (Error (LiteralString ""))
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Required                                                         |
 | Description:  IsEmpty applied to a statically empty or statically non-empty expression is    |
 |               always true or always false, respectively.  Take care to preserve any side     |
 |               effects.                                                                       |
 | Rationale:    Elimination of special case that back-ends would otherwise need to detect.     |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateIsEmpty] ((IsEmpty $expr:(Sequence) ^ (Count? $expr 0)) => (True))

[EliminateIsEmpty]
((IsEmpty
    $expr:* ^ (NonEmpty? (TypeOf $expr)) ^ (NoSideEffects? $expr)
) =>
(False))

[EliminateIsEmpty]
((IsEmpty
    $expr:* ^ (NonEmpty? (TypeOf $expr))
) =>
(Loop (Let $expr) (False)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Required                                                         |
 | Description:  When aggregate operations are applied to the empty sequence, the result is the |
 |               empty sequence.  So if the static type is empty, the aggregate can be removed. |
 | Rationale:    Elimination of special case that back-ends would otherwise need to detect.     |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateAverage] ((Average $x:* ^ (Empty? (TypeOf $x))) => (Nop $x))
[EliminateSum]     ((Sum $x:* ^ (Empty? (TypeOf $x)))     => (Nop $x))
[EliminateMinimum] ((Minimum $x:* ^ (Empty? (TypeOf $x))) => (Nop $x))
[EliminateMaximum] ((Maximum $x:* ^ (Empty? (TypeOf $x))) => (Nop $x))

#|----------------------------------------------------------------------------------------------|
 |Groups:       Normalization, Required                                                         |
 |Description:  Sorting a sequence that has cardinality one is a no-op and can be eliminated.   |
 |Rationale:    The no-op sort keeps other patterns from matching.                              |
 |Dependencies: None.                                                                           |
 |----------------------------------------------------------------------------------------------|#
[EliminateSort]
((Sort (For $bind:* ^ (Single? (TypeOf $bind))) *) => (Nop $bind))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  StrConcat applied to a singleton string is a no-op and can be removed.         |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateStrConcatSingle] ((StrConcat * $x:*) ^ (Single? (TypeOf $x)) => (Nop $x))


#|
###-----------------------------------------------------------------------------------------------
### Optional Patterns
###-----------------------------------------------------------------------------------------------
|#

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Remove no-operation (Nop) nodes from the tree.  These exist only to make the   |
 |               compiler's job easier and can safely be removed.                               |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateNop] ((Nop $x:*) => $x)

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Required                                                         |
 | Description:  Same as EliminateIsType pattern #1, but applied to TypeAssert.  This pattern   |
 |               is optional so that debug code can upcast to less-specific types.              |
 | Rationale:    Same as IsType.                                                               |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateTypeAssertOptional]
((TypeAssert $opnd:* (LiteralType $base:*) ^ (SubtypeOf? (TypeOf $opnd) $base)) => $opnd)

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalization, Optional                                                        |
 | Description:  A For iterator that directly references another For iterator is a no-op, as    |
 |               long as no position is required.                                               |
 | Rationale:    The no-op iterator keeps other patterns from matching.                         |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateIterator]
($outer:
(Loop
    $iter:(For $iterRef:(For *)) ^ (NonPositionalIterator? $iter)
    $ret:*
) =>
(Subs $ret $iter $iterRef))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  These patterns eliminate boolean operators with one or more constant operands. |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateAnd]         ((And (True) $x:*)                 => $x)
[EliminateAnd]         ((And $x:(False) *)                => $x)
[EliminateAnd]         ((And $x:* (True))                 => $x)
[EliminateAnd]         ((And * $x:(False))                => $x)

[EliminateOr]          ((Or $x:(True) *)                  => $x)
[EliminateOr]          ((Or (False) $x:*)                 => $x)
[EliminateOr]          ((Or * $x:(True))                  => $x)
[EliminateOr]          ((Or $x:* (False))                 => $x)

[EliminateNot]         ((Not (True))                      => (False))
[EliminateNot]         ((Not (False))                     => (True))

[EliminateConditional] ((Conditional (True) $x:* *)       => $x)
[EliminateConditional] ((Conditional (False) * $x:*)      => $x)
[EliminateConditional] ((Conditional $x:* (True) (False)) => $x)
[EliminateConditional] ((Conditional $x:* (False) (True)) => (Not $x))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Toggle the true and false branches of a conditional if the condition contains  |
 |               a Not operator.                                                                |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[FoldConditionalNot] ((Conditional (Not $x:*) $t:* $f:*) => (Conditional $x $f $t))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Length can be eliminated when it is applied to an operand with a statically    |
 |               known cardinality (inferred by static typing rules).                           |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateLength] ((Length $x:(Sequence) ^ (Count? $x 0))                      => (LiteralInt32 0))
[EliminateLength] ((Length $x:* ^ (Single? (TypeOf $x)) ^ (NoSideEffects? $x)) => (LiteralInt32 1))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  A sequence that contains a single expression is a no-op and can be eliminated. |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateSequence] ($x:(Sequence) ^ (Count? $x 1) => (First $x))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Eliminate set operations when one or both operands are empty or are references |
 |               to the same iterator.  Note that Union, Intersection, and Difference           |
 |               implicitly sort in document order and remove duplicate nodes, and so even when |
 |               they are eliminated, the DocOrderDistinct operation must be performed.         |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateUnion]        ((Union $x:* $x)                                 => (DocOrderDistinct $x))
[EliminateUnion]        ((Union $x:(Sequence) ^ (Count? $x 0) $y:*)      => (DocOrderDistinct $y))
[EliminateUnion]        ((Union $x:* $y:(Sequence) ^ (Count? $y 0))      => (DocOrderDistinct $x))
[EliminateUnion]        ((Union $x:XmlContext XmlContext)                => $x)

[EliminateIntersection] ((Intersection $x:* $x)                          => (DocOrderDistinct $x))
[EliminateIntersection] ((Intersection $x:(Sequence) ^ (Count? $x 0) *)  => $x)
[EliminateIntersection] ((Intersection * $y:(Sequence) ^ (Count? $y 0))  => $y)
[EliminateIntersection] ((Intersection $x:XmlContext XmlContext)         => $x)

[EliminateDifference]   ((Difference $x:(Sequence) ^ (Count? $x 0) $y:*) => $x)
[EliminateDifference]   ((Difference $x:* $y:(Sequence) ^ (Count? $y 0)) => (DocOrderDistinct $x))
[EliminateDifference]   ((Difference $x:* $x)                            => (Sequence))
[EliminateDifference]   ((Difference XmlContext XmlContext)              => (Sequence))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Fold arithmetic operations with literal operands.  Take care to detect         |
 |               overflow and divide by zero conditions, so that these errors will not be       |
 |               raised at compile-time.                                                        |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateNegate]   ((Negate (LiteralDecimal $x:*))                       => (LiteralDecimal { -{$x} }))
[EliminateNegate]   ((Negate (LiteralDouble $x:*))                        => (LiteralDouble{ -{$x} }))
[EliminateNegate]   ((Negate (LiteralInt32 $x:*))                         => (LiteralInt32 { -{$x} }))
[EliminateNegate]   ((Negate (LiteralInt64 $x:*))                         => (LiteralInt64 { -{$x} }))

[EliminateAdd]      ((Add $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldAdd? $x $y))      => (Add! $x $y))
[EliminateSubtract] ((Subtract $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldSub? $x $y)) => (Sub! $x $y))
[EliminateMultiply] ((Multiply $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldMul? $x $y)) => (Mul! $x $y))
[EliminateDivide]   ((Divide $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldDiv? $x $y))   => (Div! $x $y))
[EliminateModulo]   ((Modulo $x:* ^ (Literal? $x) $y:* ^ (Literal? $y) ^ (CanFoldMod? $x $y))   => (Mod! $x $y))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  The length of a constant string can be computed at compile-time.               |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateStrLength] ((StrLength (LiteralString $x:*)) => (LiteralInt32 { {$x}.Length }))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  StrConcat containing constant strings can be folded away.                      |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateStrConcat]
((StrConcat (LiteralString $delim:*) $values:(Sequence) ^ (LiteralArgs? $values)) =>
{
    // Concatenate all constant arguments
    StringConcat concat = new StringConcat();
    concat.Delimiter = {$delim};

    foreach (QilLiteral lit in {$values})
        concat.Concat((string) lit);
} ^
(LiteralString { concat.GetResult() }))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Value comparison operators can be folded away when applied to constant         |
 |               operands.                                                                      |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateEq] ((Eq $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Eq! $x $y))
[EliminateNe] ((Ne $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Ne! $x $y))
[EliminateGt] ((Gt $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Gt! $x $y))
[EliminateGe] ((Ge $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Ge! $x $y))
[EliminateLt] ((Lt $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Lt! $x $y))
[EliminateLe] ((Le $x:* ^ (Literal? $x) $y:* ^ (Literal? $y)) => (Le! $x $y))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Node comparison operators can be eliminated if both operands refer to the      |
 |               same node.                                                                     |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateIs]     ((Is $x:* $x)     => (True))
[EliminateAfter]  ((After $x:* $x)  => (False))
[EliminateBefore] ((Before $x:* $x) => (False))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Loops which iterate over the empty sequence or return the empty sequence       |
 |               reduce to the empty sequence.  Loops which simply return the iterator can be   |
 |               eliminated.                                                                    |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateLoop]   ((Loop (For $x:(Sequence) ^ (Count? $x 0)) *)                    => (Sequence))
[EliminateLoop]   ((Loop $i:* ^ (NoSideEffects? $i) $x:(Sequence) ^ (Count? $x 0)) => (Sequence))
[EliminateLoop]   ((Loop $iter:* $iter)                                            => (First $iter))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Filters which are always false reduce to the empty sequence.  Filters which    |
 |               are always true reduce to the iterator.                                        |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateFilter] ((Filter $i:* ^ (NoSideEffects? $i) (False)) => (Sequence))
[EliminateFilter] ((Filter $i:* (True))                        => (First $i))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  If the argument to DocOrderDistinct is already in document order with no       |
 |               duplicates, then DocOrderDistinct is a no-op.                                  |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: This rule will fire in non-trivial cases only when the Annotation patterns are |
 |               enabled.                                                                       |
 |----------------------------------------------------------------------------------------------|#
[EliminateDod]
((DocOrderDistinct $arg:* ^ (DocOrderDistinct? $arg)) => $arg)

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Fold a XsltConvert operator that is applied to a Literal by performing the     |
 |               conversion at compile-time.                                                    |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[FoldXsltConvertLiteral]
((XsltConvert $lit:* ^ (Literal? $lit) (LiteralType $typ:*) ^ (CanFoldXsltConvert? $lit $typ)) => (FoldXsltConvert $lit $typ))

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  Converting an expression to the same type is a no-op and can be eliminated.    |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateXsltConvert] ((XsltConvert $expr:* (LiteralType $typ:*) ^ (Equal? (TypeOf $expr) $typ)) => $expr)

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalization, Optional, OptimizedConstruction                                 |
 | Description:  Eliminate namespace declarations that were previously marked as redundant      |
 |               by namespace analysis.                                                         |
 | Rationale:    Redundant NamespaceDecls are common.  Eliminating them reduces the number of   |
 |               of run-time namespace scope checks that will need to be performed.             |
 | Dependencies: The XmlILNamespaceAnalyzer must be invoked to first mark redundant namespace   |
 |               declarations before this rule will ever fire.                                  |
 |----------------------------------------------------------------------------------------------|#
[EliminateNamespaceDecl]
($nmsp:(NamespaceDecl * *) ^ (NamespaceInScope? $nmsp) => (Sequence))

