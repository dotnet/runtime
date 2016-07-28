#|
//------------------------------------------------------------------------------
// <copyright file="Normalize.p" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner>akimball</owner>
//------------------------------------------------------------------------------
|#


#|
###############################################################################################
###
### Normalize.p
###
###   These rules normalize the tree so that certain patterns are guaranteed not to arise
###   and others are encouraged to arise.  They are less certain than the previous rewrites
###   in that different backends might prefer different normalization forms.
###
###   When adding a required normalization, take care the line number information will be
###   correctly preserved, as these normalizations will execute even when generating debug
###   code.
###
###############################################################################################
|#


#|
###-----------------------------------------------------------------------------------------------
### Required Patterns
###-----------------------------------------------------------------------------------------------
|#

#|----------------------------------------------------------------------------------------------|
 |Groups:       Normalize, Required                                                             |
 |Description:  Ensure that arguments of Union are in document order with no duplicates.        |
 |Rationale:    If arguments are DocOrderDistinct, then Union can be implemented using a merge  |
 |              sort, which avoids caching large sets in memory.                                |
 |Dependencies: None.                                                                           |
 |----------------------------------------------------------------------------------------------|#
[NormalizeUnion]
((Union
    $left:*
    $right:* ^ (~(DocOrderDistinct? $left) | ~(DocOrderDistinct? $right))
) =>
(Union (DocOrderDistinct $left) (DocOrderDistinct $right)))

#|----------------------------------------------------------------------------------------------|
 |Groups:       Normalize, Required                                                             |
 |Description:  Ensure that arguments of Intersection are in document order with no duplicates. |
 |Rationale:    If arguments are DocOrderDistinct, then Intersection can be implemented in      |
 |              linear time, while avoiding caching large sets in memory.                       |
 |Dependencies: None.                                                                           |
 |----------------------------------------------------------------------------------------------|#
[NormalizeIntersect]
((Intersection
    $left:*
    $right:* ^ (~(DocOrderDistinct? $left) | ~(DocOrderDistinct? $right))
) =>
(Intersection (DocOrderDistinct $left) (DocOrderDistinct $right)))

#|----------------------------------------------------------------------------------------------|
 |Groups:       Normalize, Required                                                             |
 |Description:  Ensure that arguments of Difference are in document order with no duplicates.   |
 |Rationale:    If arguments are DocOrderDistinct, then Difference can be implemented in        |
 |              linear time, while avoiding caching large sets in memory.                       |
 |Dependencies: None.                                                                           |
 |----------------------------------------------------------------------------------------------|#
[NormalizeDifference]
((Difference
    $left:*
    $right:* ^ (~(DocOrderDistinct? $left) | ~(DocOrderDistinct? $right))
) =>
(Difference (DocOrderDistinct $left) (DocOrderDistinct $right)))


#|
###-----------------------------------------------------------------------------------------------
### Optional Patterns
###-----------------------------------------------------------------------------------------------
|#

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Normalize iteration over cardinality one sequences to use For rather than Let. |
 |               This pattern should not normalize global variable Let iterators.               |
 | Rationale:    Other patterns do not have to match singleton Let as an operand.  For example, |
 |               the FoldNameOf pattern must only match For bound to an ElementCtor, and does   |
 |               not have to consider Let bound to an ElementCtor.                              |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeSingletonLet]
($iter:(Let $bind:*) ^ (Single? (TypeOf $iter)) ^ ~(GlobalVariable? $iter) => {
    {$iter}.NodeType = QilNodeType.For;
    VisitFor({$iter});
})

#|----------------------------------------------------------------------------------------------|
 | Groups:       ConstantFold, Optional                                                         |
 | Description:  A sequence that contains one or more sequences can be flattened.               |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeNestedSequences]
($seq:(Sequence) ^ (NestedSequences? $seq) =>
$result:(Sequence) ^
{
    foreach (QilNode nd in {$seq}) {
        if (nd.NodeType == QilNodeType.Sequence)
            {$result}.Add((IList<QilNode>) nd);
        else
            {$result}.Add(nd);
    }

    // Match patterns on new sequence
    {$result} = VisitSequence((QilList) {$result});
} ^
$result)

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Factor TextCtor out of Conditional so that Conditional returns a string.       |
 | Rationale:    It is common to use a Conditional that returns one of two text nodes within an |
 |               Xslt Rtf.  This pattern encourages Rtf's that consist of a single TextCtor, so |
 |               that the AnnotateSingleText pattern can be matched.                            |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeConditionalText]
((Conditional $cond:*
    $left:(TextCtor $leftText:*)
    $right:(TextCtor $rightText:*)
) =>
(TextCtor (Conditional $cond $leftText $rightText)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Factor TextCtor out of a singleton Loop so that the Loop returns a string.     |
 | Rationale:    This pattern often occurs within an Xslt Rtf that contains variables and       |
 |               returns a singleton TextCtor.  This pattern encourages Rtf's that consist of a |
 |               single TextCtor, so that the AnnotateSingleText pattern can be matched.        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeLoopText]
((Loop
    $iter:(For $bind:* ^ (Single? (TypeOf $bind)))
    $ctor:(TextCtor $text:*)
) =>
(TextCtor (Loop $iter $text)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Ensure that when comparing an expression to a literal, that the literal        |
 |               appears on the right-hand side.                                                |
 | Rationale:    Other patterns, such as NormalizeConvertEq, do not need to handle literals on  |
 |               the left-hand side.                                                            |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeEqLiteral]
((Eq $left:* ^ (Literal? $left) $right:* ^ ~(Literal? $right)) => (Eq $right $left))

[NormalizeNeLiteral]
((Ne $left:* ^ (Literal? $left) $right:* ^ ~(Literal? $right)) => (Ne $right $left))

[NormalizeLtLiteral]
((Lt $left:* ^ (Literal? $left) $right:* ^ ~(Literal? $right)) => (Gt $right $left))

[NormalizeLeLiteral]
((Le $left:* ^ (Literal? $left) $right:* ^ ~(Literal? $right)) => (Ge $right $left))

[NormalizeGtLiteral]
((Gt $left:* ^ (Literal? $left) $right:* ^ ~(Literal? $right)) => (Lt $right $left))

[NormalizeGeLiteral]
((Ge $left:* ^ (Literal? $left) $right:* ^ ~(Literal? $right)) => (Le $right $left))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Ensure that when performing arithmetic where at least one operand is a         |
 |               literal, that the literal appears on the right-hand side (if possible).        |
 | Rationale:    Other patterns, such as NormalizeAddEq, do not need to handle literals on the  |
 |               left-hand side.                                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeAddLiteral]
((Add $left:* ^ (Literal? $left) $right:* ^ ~(Literal? $right)) => (Add $right $left))

[NormalizeMultiplyLiteral]
((Multiply $left:* ^ (Literal? $left) $right:* ^ ~(Literal? $right)) => (Multiply $right $left))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  If relational operators compare a XsltConvert operator with a literal, then it |
 |               may be possible to eliminate the XsltConvert on one side by converting the     |
 |               literal on the other side.                                                     |
 | Rationale:    Xslt often uses Double comparisons.                                            |
 |               For example, the Xslt expression "position() = 1" is compiled into:            |
 |                  (Eq (XsltConvert (PositionOf $i) Double) (LiteralDouble 1))                 |
 |               This can be simplified to eliminate conversions and allow other patterns, like |
 |               MaxPositionEq to fire.                                                         |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeXsltConvertEq]
((Eq
    (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ)
    $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))
) =>
(Eq $expr (FoldXsltConvert $lit (TypeOf $expr))))

[NormalizeXsltConvertNe]
((Ne
    (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ)
    $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))
) =>
(Ne $expr (FoldXsltConvert $lit (TypeOf $expr))))

[NormalizeXsltConvertLt]
((Lt
    (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ)
    $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))
) =>
(Lt $expr (FoldXsltConvert $lit (TypeOf $expr))))

[NormalizeXsltConvertLe]
((Le
    (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ)
    $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))
) =>
(Le $expr (FoldXsltConvert $lit (TypeOf $expr))))

[NormalizeXsltConvertGt]
((Gt
    (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ)
    $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))
) =>
(Gt $expr (FoldXsltConvert $lit (TypeOf $expr))))

[NormalizeXsltConvertGe]
((Ge
    (XsltConvert $expr:* (LiteralType $typ:*)) ^ (PrimitiveNumeric? (TypeOf $expr)) ^ (PrimitiveNumeric? $typ)
    $lit:* ^ (Literal? $lit) ^ (CanFoldXsltConvertNonLossy? $lit (TypeOf $expr))
) =>
(Ge $expr (FoldXsltConvert $lit (TypeOf $expr))))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  If relational operators compare an Add operator with a literal, then it may    |
 |               be possible to eliminate the Add on one side by subtracting from the literal   |
 |               on the other side.                                                             |
 | Rationale:    When position() is used within a match expression, Xslt generates code that    |
 |               computes the number of preceding-siblings + 1.  If this is then compared to a  |
 |               a literal, then this pattern will be encountered.  For example, the Xslt       |
 |               match pattern "foo[3]" is compiled into Qil that matches this pattern.         |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeAddEq]
((Eq
    (Add $exprAdd:* $litAdd:*) ^ (Literal? $litAdd)
    $litEq:* ^ (Literal? $litEq) ^ (CanFoldSub? $litEq $litAdd)
) =>
(Eq $exprAdd (Sub! $litEq $litAdd)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Normalize "generate-id($a) = generate-id($b)".  Using the generate-id function |
 |               is the only way to determine node identity in Xslt.                            |
 | Rationale:    The generate-id() function is expensive to evaluate, since it creates strings  |
 |               that must be unique.  There is no reason to create these strings if the user   |
 |               is simply trying to determine whether one node has the same identity as        |
 |               another.  This is especially important for efficiently compiling the muenchian |
 |               method of grouping in Xslt.                                                    |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeIdEq]
((Eq
    (XsltGenerateId $arg1:*) ^ (Single? (TypeOf $arg1))
    (XsltGenerateId $arg2:*) ^ (Single? (TypeOf $arg2))
) =>
(Is $arg1 $arg2))

[NormalizeIdEq]
((Eq
    (XsltGenerateId $arg:*) ^ (Single? (TypeOf $arg))
    (StrConcat
        *
        (Loop
            $iter:(For $bind:* ^ (AtMostOne? (TypeOf $bind)))
            (XsltGenerateId $iter)
        )
    )
) =>
(Not
    (IsEmpty
        (Filter
            $iterNew:(For $bind)
            (Is $arg $iterNew)
        )
    )
))

[NormalizeIdEq]
((Eq
    (StrConcat
        *
        (Loop
            $iter:(For $bind:* ^ (AtMostOne? (TypeOf $bind)))
            (XsltGenerateId $iter)
        )
    )
    (XsltGenerateId $arg:*) ^ (Single? (TypeOf $arg))
) =>
(Not
    (IsEmpty
        (Filter
            $iterNew:(For $bind)
            (Is $arg $iterNew)
        )
    )
))

[NormalizeIdNe]
((Ne
    (XsltGenerateId $arg1:*) ^ (Single? (TypeOf $arg1))
    (XsltGenerateId $arg2:*) ^ (Single? (TypeOf $arg2))
) =>
(Not (Is $arg1 $arg2)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Normalize "count($a | $b) = 1", where $a is a singleton and $b has zero or one |
 |               nodes.  This expression tests whether $b is empty or is the same node as $a.   |
 | Rationale:    This expression is used to do muenchian grouping in Xslt, and so must be very  |
 |               efficient (see also generate-id($a) = generate-id($b).                         |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeMuenchian]
((Eq
    (Length
        (Union $arg1:* $arg2:*) ^ (Single? (TypeOf $arg1)) ^ (AtMostOne? (TypeOf $arg2))
    )
    (LiteralInt32 1)
) =>
(IsEmpty
    (Filter
        $iterNew:(For $arg2)
        (Not (Is $arg1 $iterNew))
    )
))

[NormalizeMuenchian]
((Eq
    (Length
        (Union $arg1:* $arg2:*) ^ (AtMostOne? (TypeOf $arg1)) ^ (Single? (TypeOf $arg2))
    )
    (LiteralInt32 1)
) =>
(IsEmpty
    (Filter
        $iterNew:(For $arg1)
        (Not (Is $iterNew $arg2))
    )
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Normalize "count($expr) > 0" or "count($expr) != 0" to exists($expr).          |
 | Rationale:    Avoid computing the length of a set when the result can be computed by simply  |
 |               determining whether at least one item exists.                                  |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeLengthNe]
((Ne (Length $expr:*) (LiteralInt32 0)) => (Not (IsEmpty $expr)))

[NormalizeLengthGt]
((Gt (Length $expr:*) (LiteralInt32 0)) => (Not (IsEmpty $expr)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Inline functions with function definitions that are the empty sequence.        |
 | Rationale:    There is no reason to generate a function body if it does nothing.             |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeInvokeEmpty]
((Invoke (Function * $seq:(Sequence) ^ (Count? $seq 0) * *) *) => (Sequence))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  If a sort key converts from xs:int to xs:double, the conversion can be         |
 |               eliminated.                                                                    |
 | Rationale:    Since xs:int <: xs:double, the extra conversion unnecessarily slows            |
 |               performance.                                                                   |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeSortXsltConvert]
((SortKey
    (XsltConvert $expr:* (LiteralType $typ:*)) ^ (Equal? (TypeOf $expr) (ConstructType {IntX})) ^ (Equal? $typ (ConstructType {DoubleX}))
    $coll:*
) =>
(SortKey $expr $coll))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Identify Filters that select attributes of a particular name.  This identifies |
 |               patterns like "@foo".                                                          |
 | Rationale:    Other patterns are based upon this pattern.  Also, ILGen heavily optimizes     |
 |               this pattern, since it is so common.                                           |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeAttribute]
((Filter
    $iter:(For (Content $input:*))
    (And
        (IsType
            $iter
            (LiteralType $typ:* ^ (Equal? $typ (ConstructType {Attribute})))
        )
        (Eq
            (NameOf $iter)
            $qname:(LiteralQName * * *)
        )
    )
) =>
(Attribute $input $qname))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Commute, Optional                                                              |
 | Description:  When a filter is applied to a loop, commute the filter with the loop so that   |
 |               filter expression binds tightly to the loop's return expression.               |
 | Rationale:    Other patterns assume that filters are "pushed" down as far as possible into   |
 |               inner loops.  It is generally better to filter as soon as it is possible.      |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[CommuteFilterLoop]
((Filter
    $iter:
    (For
        $loop: (Loop
            $iter2:*
            $ret2:*
        )
    )
    $cond:* ^ (NonPositional? $cond $iter) ^ ~(DocOrderDistinct? $loop)
) =>
(Loop
    $iter2
    (Filter
        $iter3:(For $ret2)
        (Subs $cond $iter $iter3)
    )
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  If a filter does not reference its iterator, then hoist the filter condition   |
 |               out of the loop (loop-invariant code motion).                                  |
 | Rationale:    There is no reason to repeatedly re-evaluate a boolean expression if its value |
 |               cannot change as iteration proceeds.                                           |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeLoopInvariant]
((Filter
    $iter:* ^ (NoSideEffects? $iter) ^ ~(NodeType? (First $iter) {OptimizeBarrier})
    $cond:* ^ ~($cond >> $iter) ^ (NoSideEffects? $cond)
) =>
(Conditional
    $cond
    (First $iter)
    (Sequence)
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalization, Optional                                                        |
 | Description:  If an iterator (either a Let iterator or a For iterator bound to a singleton)  |
 |               is used zero or one times within its scope, then the iterated expression can   |
 |               be inlined.                                                                    |
 | Rationale:    Reduction in query size and complexity.                                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateIteratorUsedAtMostOnce]
((Loop
    $iter:* ^ ((NodeType? $iter {Let}) | (Single? (TypeOf (First $iter)))) ^ (NoSideEffects? $iter)
    $ret:* ^ (RefCountZeroOrOne? $ret $iter)
) =>
(Subs $ret $iter (First $iter)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  If a loop returns a conditional, and if empty sequence is returned from one of |
 |               the branches, and the iterator returned from the other, then the conditional   |
 |               can be normalized into a Filter operator.                                      |
 | Rationale:    Other patterns assume that filter conditions are normalized into the Filter    |
 |               operator.                                                                      |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeLoopConditional]
((Loop
    $iter:*
    (Conditional
        $cond:*
        $left:(Sequence) ^ (Count? $left 0)
        $iter
    )
) =>
(Filter
    $iter
    (Not $cond)
))

[NormalizeLoopConditional]
((Loop
    $iter:*
    (Conditional
        $cond:*
        $iter
        $right:(Sequence) ^ (Count? $right 0)
    )
) =>
(Filter
    $iter
    $cond
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  If a loop returns a conditional, and if empty sequence is returned from one of |
 |               the branches, then the conditional can be normalized to a loop that iterates   |
 |               over a filter and returns the other branch of the conditional.                 |
 | Rationale:    Other patterns assume that filter conditions are normalized into the Filter    |
 |               operator.                                                                      |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeLoopConditional]
((Loop
    $iter:(For *)
    (Conditional
        $cond:*
        $left:(Sequence) ^ (Count? $left 0)
        $right:* ^ (NonPositional? $right $iter)
    )
) =>
(Loop
    $iter2:(For (Filter $iter (Not $cond)))
    (Subs $right $iter $iter2)
))

[NormalizeLoopConditional]
((Loop
    $iter:(For *)
    (Conditional
        $cond:*
        $left:* ^ (NonPositional? $left $iter)
        $right:(Sequence) ^ (Count? $right 0)
    )
) =>
(Loop
    $iter2:(For (Filter $iter $cond))
    (Subs $left $iter $iter2)
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Attempt to reduce the number of bound variables in scope at any point in the   |
 |               query, by nesting Loops within For expressions rather than Return              |
 |               expressions ("horizontal" nesting rather than "vertical" nesting).             |
 | Rationale:    The path patterns attempt to match paths from left-to-right rather than right- |
 |               to-left.  This normalization facilitates this.                                 |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[NormalizeLoopLoop]
((Loop
    $iter:*
    $ret:
    (Loop
        $iter2:(For $bind2:*)
        $ret2:* ^ ~($ret2 >> $iter) ^ (NonPositional? $ret2 $iter2)
    )
) =>
(Loop
    $iter3:
    (For
        (Loop
            $iter
            $bind2
        )
    )
    (Subs $ret2 $iter2 $iter3)
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional    , PathPattern                                           |
 | Description:  Fold "descendant-or-self::node()/foo" => "descendant::foo".  This pattern is   |
 |               very common, as in "//foo".  Care must be taken not to fold positional         |
 |               patterns, like "//foo[1]".                                                     |
 | Rationale:    ILGen heavily optimizes descendant::foo, so identifying the pattern greatly    |
 |               improves performance.                                                          |
 | Dependencies: The path patterns must be enabled for this pattern to fire.                    |
 |----------------------------------------------------------------------------------------------|#
[FoldNamedDescendants]
((DocOrderDistinct
    $path:
    (Loop
        $iter1:
        (For
            (Loop
                $iter2:*
                $ret2:(DescendantOrSelf $input:*)
            )
        )
        $ret1:
        (Filter
            $iter3:*
            $cond3:*
        ) ^ ((Pattern? $ret1 {FilterElements}) | (Pattern? $ret1 {FilterContentKind})) ^ (StepPattern? $ret1 {Content})
    )
) =>
(DocOrderDistinct
    (Loop
        $iter2
        (Filter
            $iterNew:(For (Descendant $input))
            (Subs $cond3 $iter3 $iterNew)
        )
    )
))

[FoldNamedDescendants]
((DocOrderDistinct
    $path:
    (Loop
        $iter1:
        (For (DescendantOrSelf $input:*))
        $ret1:
        (Filter
            $iter2:*
            $cond2:*
        ) ^ ((Pattern? $ret1 {FilterElements}) | (Pattern? $ret1 {FilterContentKind})) ^ (StepPattern? $ret1 {Content})
    )
) =>
(Filter
    $iterNew:(For (Descendant $input))
    (Subs $cond2 $iter2 $iterNew)
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Hoist filter expressions (all but ElementFilterPath and KindTestPath) out of   |
 |               DocOrderDistinct scope.                                                        |
 | Rationale:    This is a building-block pattern that allows more complex patterns to be       |
 |               identified.  In particular, the JoinAndDod patterns require this pattern in    |
 |               order to break down expressions like "descendant::foo/child::bar[@a = 'val']". |
 |               The basic idea is that DocOrderDistinct should bind more tightly than filter   |
 |               expressions.                                                                   |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[CommuteDodFilter]
((DocOrderDistinct
    $filter:
    (Filter
        $iter:(For $bind:*) ^ (NonPositionalIterator? $iter)
        $cond:*
    ) ^ ~(Pattern? $filter {FilterElements}) ^ ~(Pattern? $filter {FilterContentKind}) ^ ~(Pattern? $filter {FilterAttributeKind})
) =>
(Filter
    $iterNew:(For (DocOrderDistinct $bind))
    (Subs $cond $iter $iterNew)
))

[CommuteDodFilter]
((DocOrderDistinct
    (Loop
        $iter1:*
        $ret1:
        (Filter
            $iter2:(For $bind2:*) ^ (NonPositionalIterator? $iter2)
            $cond2:* ^ ~($cond2 >> $iter1)
        ) ^ ~(Pattern? $ret1 {FilterElements}) ^ ~(Pattern? $ret1 {FilterContentKind}) ^ ~(Pattern? $ret1 {FilterAttributeKind})
    )
) =>
(Filter
    $iterNew:(For (DocOrderDistinct (Loop $iter1 $bind2)))
    (Subs $cond2 $iter2 $iterNew)
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  When DocOrderDistinct is applied to an Loop that is iterating over a path      |
 |               expression, then push the DocOrderDistinct operation down into the iterator.   |
 | Rationale:    This is a building-block pattern that allows more complex patterns to be       |
 |               identified that involve DocOrderDistinct.                                      |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[IntroduceDod]
((DocOrderDistinct
    $loop:
    (Loop
        $iter:(For $bind:* ^ ~(DocOrderDistinct? $bind)) ^ (NonPositionalIterator? $iter) ^ (SubtypeOf? (TypeOf $bind) (ConstructType {NodeNotRtfS}))
        $ret:*
    ) ^ ~(Pattern? $loop {FilterElements}) ^ ~(Pattern? $loop {FilterContentKind}) ^ ~(Pattern? $loop {FilterAttributeKind})
) =>
(DocOrderDistinct
    (Loop
        $iterNew:(For (DocOrderDistinct $bind))
        (Subs $ret $iter $iterNew)
    )
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  When the length of a Preceding or PrecedingSibling expression is computed,     |
 |               first sort the expression in document order.                                   |
 | Rationale:    It is faster for ILGen to enumerate the preceding and preceding-sibling axes   |
 |               in document order.                                                             |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[IntroducePrecedingDod]
((Length
    $expr:* ^ ~(DocOrderDistinct? $expr) ^ ((StepPattern? $expr {XPathPreceding}) | (StepPattern? $expr {PrecedingSibling}))
) =>
(Length (DocOrderDistinct $expr)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  When DocOrderDistinct is applied to an Loop that returns a preceding-sibling   |
 |               path expression, then push the DocOrderDistinct operation down into the return |
 |               expression.                                                                    |
 | Rationale:    It is faster for ILGen to enumerate the preceding-sibling axes in document     |
 |               order.  Introducing DocOrderDistinct enables the AnnotateDodReverse pattern to |
 |               fire more frequently.                                                          |
 | Dependencies: The simple and filter path patterns must be enabled for this pattern to fire.  |
 |----------------------------------------------------------------------------------------------|#
[IntroducePrecedingDod]
((DocOrderDistinct
    (Loop
        $iter:*
        $ret:* ^ ~(DocOrderDistinct? $ret) ^ (StepPattern? $ret {PrecedingSibling})
    )
) =>
(DocOrderDistinct
    (Loop
        $iter
        (DocOrderDistinct $ret)
    )
))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  When DocOrderDistinct is applied to a Loop, remove any DocOrderDistinct        |
 |               that is applied to the return clause, except in the case of PrecedingSibling,  |
 |               which is handled in [IntroducePrecedingDod].                                   |
 | Rationale:    Since DocOrderDistinct is already applied to the entire Loop, it's not         |
 |               good to perform it over the results of each return expression, only to         |
 |               do it again over the aggregated results.  Also, this pattern allows the        |
 |               JoinAndDod patterns to match more frequently.                                  |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateReturnDod]
((DocOrderDistinct
    (Loop
        $iter:*
        $ret:
        (DocOrderDistinct $opnd:*) ^ ~(StepPattern? $opnd {PrecedingSibling})
    )
) =>
(DocOrderDistinct (Loop $iter $opnd)))

#|----------------------------------------------------------------------------------------------|
 | Groups:       Normalize, Optional                                                            |
 | Description:  Eliminate functions, global variables, and parameters, which are not           |
 |               referenced in the query                                                        |
 | Rationale:    If a function, global variable or parameter is never used, do not spend        |
 |               the time generating code for it.  One particular case is when a variable       |
 |               has a literal value, and all references to that variable have been replaced    |
 |               with its value.                                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[EliminateUnusedGlobals]
($qil:(QilExpression *) => {
    EliminateUnusedGlobals({$qil}.GlobalVariableList);
    EliminateUnusedGlobals({$qil}.GlobalParameterList);
    EliminateUnusedGlobals({$qil}.FunctionList);
})
