#|
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
|#


#|
###############################################################################################
###
### Annotate.p
###
###   These patterns do not change the structure of the Qil graph.  Instead, they add
###   annotations to nodes in the graph containing information about the subtree of
###   that node.  Other patterns are applied based on this information.
###
###############################################################################################
|#


#|
###-----------------------------------------------------------------------------------------------
### Required Patterns
###-----------------------------------------------------------------------------------------------
|#

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotation, Required                                                           |
 | Description:  Mark every iterator that is the argument of a PositionOf operator.             |
 | Rationale:    Back-ends need to know when iteration position must be tracked, as it is too   |
 |               expensive to do when the position is not needed.  Also, many patterns cannot   |
 |               be matched if position is required (barrier to optimization).                  |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotatePositionalIterator]
((PositionOf $iter:*) => (AddPattern $iter {IsPositional}) ^ { })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Required                                                             |
 | Description:  Annotates every Qil function with a list of Invoke nodes that reference it.    |
 | Rationale:    Some optimizations are global in nature and require all callers to be          |
 |               considered before the optimization can be made.  Annotating every function     |
 |               with its callers enables these optimizations.                                  |
 | Dependencies: The AnnnotateConstruction patterns requires this annotation.                   |
 |----------------------------------------------------------------------------------------------|#
[AnnotateTrackCallers]
($caller:(Invoke $func:* *) => (AddCaller $func $caller) ^ { })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Required                                                             |
 | Description:  Mark DocOrderDistinct node as returning nodes in document order.               |
 | Rationale:    This is required because other patterns avoid recursing forever by testing     |
 |               the IsDocOrderDistinct property and inserting a DocOrderDistinct node if       |
 |               the property is false.                                                         |
 | Dependencies: Any pattern which uses the DocOrderDistinct? macro requires that               |
 |               IsDocOrderDistinct = true for the DocOrderDistinct QIL node.                   |
 |----------------------------------------------------------------------------------------------|#
[AnnotateDod]
($outer:
(DocOrderDistinct
    $inner:*
) =>
(AddPattern $outer {IsDocOrderDistinct}) ^      ; DocOrderDistinct always returns nodes in document order with no duplicates
(InheritPattern $outer $inner {SameDepth}) ^    ; If $inner matches SameDepth, then applying DocOrderDistinct does not change this
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotation, Required                                                           |
 | Description:  These rules perform extensive analysis in order to reduce the number of        |
 |               run-time well-formedness checks that must be made, like xml state checks,      |
 |               duplicate attribute checks, and namespace scope checks.  The analysis also     |
 |               determines when top-down tree construction is desirable, rather than the       |
 |               default bottom-up tree construction that XQuery semantics specify.  This set   |
 |               of rules will annotate the tree with the minimum annotations necessary to      |
 |               guarantee well-formedness.                                                     |
 | Rationale:    Qil implicitly assumes that back-ends will ensure well-formedness.  Also,      |
 |               avoiding run-time checks and building the tree top-down improves performance.  |
 | Dependencies: Depends on the AnnotateTrackCallers pattern.                                   |
 |----------------------------------------------------------------------------------------------|#
[AnnotateConstruction]
($ctor:(ElementCtor * $content:*) => {
    // The analysis occasionally makes small changes to the content of constructors, which is
    // why the result of Analyze is assigned to $ctor.Right.
    {$ctor}.Right = this.elemAnalyzer.Analyze({$ctor}, {$content});
})

[AnnotateConstruction]
($ctor:(AttributeCtor * $content:*) => {
    {$ctor}.Right = this.contentAnalyzer.Analyze({$ctor}, {$content});
})

[AnnotateConstruction]
($ctor:(NamespaceDecl * *) => {
    this.contentAnalyzer.Analyze({$ctor}, null);
})

[AnnotateConstruction]
($ctor:(TextCtor *) => {
    this.contentAnalyzer.Analyze({$ctor}, null);
})

[AnnotateConstruction]
($ctor:(RawTextCtor *) => {
    this.contentAnalyzer.Analyze({$ctor}, null);
})

[AnnotateConstruction]
($ctor:(CommentCtor $content:*) => {
    {$ctor}.Child = this.contentAnalyzer.Analyze({$ctor}, {$content});
})

[AnnotateConstruction]
($ctor:(PICtor * $content:*) => {
    {$ctor}.Right = this.contentAnalyzer.Analyze({$ctor}, {$content});
})

[AnnotateConstruction]
($ctor:(DocumentCtor $content:*) => {
    {$ctor}.Child = this.contentAnalyzer.Analyze({$ctor}, {$content});
})

[AnnotateConstruction]
($ctor:(RtfCtor $content:* *) => {
    {$ctor}.Left = this.contentAnalyzer.Analyze({$ctor}, {$content});
})

[AnnotateConstruction]
($ctor:(XsltCopy * $content:*) => {
    {$ctor}.Right = this.contentAnalyzer.Analyze({$ctor}, {$content});
})

[AnnotateConstruction]
($ctor:(XsltCopyOf *) => {
    this.contentAnalyzer.Analyze({$ctor}, null);
})

[AnnotateConstruction]
($qil:(QilExpression *) => {
    foreach (QilFunction ndFunc in {$qil}.FunctionList) {
        // Functions that construct Xml trees should stream output to writer; otherwise, results should
        // be cached and returned.
        if (IsConstructedExpression(ndFunc.Definition)) {
            // Perform state analysis on function's content
            ndFunc.Definition = this.contentAnalyzer.Analyze(ndFunc, ndFunc.Definition);
        }
    }

    // Perform state analysis on the root expression
    {$qil}.Root = this.contentAnalyzer.Analyze(null, {$qil}.Root);

    // Make sure that root expression is pushed to writer
    XmlILConstructInfo.Write({$qil}.Root).PushToWriterLast = true;
})

[AnnotateConstruction]
($ctor:(Choice * *) => {
    this.contentAnalyzer.Analyze({$ctor}, null);
})


#|
###-----------------------------------------------------------------------------------------------
### Optional Patterns
###-----------------------------------------------------------------------------------------------
|#

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Determine the maximum possible position of an iterator.  For example, 5 is     |
 |               the maximum position in the query foo[position() < 6].                         |
 | Rationale:    The execution engine should not waste time checking items beyond the max       |
 |               position, since they will always be filtered.                                  |
 | Dependencies: The NormalizeEq, NormalizeLe, NormalizeLt patterns will normalize the literal  |
 |               to be on the right-hand side of the expression.  The NormalizeXsltConvert and  |
 |               FoldXsltConvert patterns will normalize literal position patterns to use Int32.|
 |----------------------------------------------------------------------------------------------|#
[AnnotateMaxPositionEq]
($outer:
(Filter
    $iter:*
    (Eq (PositionOf $iter) (LiteralInt32 $num:*))
) =>
(AddPattern $iter {MaxPosition}) ^
(AddArgument $iter {MaxPosition} $num) ^ { })

[AnnotateMaxPositionLe]
($outer:
(Filter
    $iter:*
    (Le (PositionOf $iter) (LiteralInt32 $num:*))
) =>
(AddPattern $iter {MaxPosition}) ^
(AddArgument $iter {MaxPosition} $num) ^ { })

[AnnotateMaxPositionLt]
($outer:
(Filter
    $iter:*
    (Lt (PositionOf $iter) (LiteralInt32 $num:*))
) =>
(AddPattern $iter {MaxPosition}) ^
(AddArgument $iter {MaxPosition} { {$num} - 1 }) ^ { })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  If the length of a sequence is compared against a literal, then determine the  |
 |               point after which iteration can stop while still allowing comparison to be     |
 |               correctly evaluated.                                                           |
 | Rationale:    The execution engine should not waste time iterating items beyond the max      |
 |               position, since they do not affect the outcome of the comparison.              |
 | Dependencies: The NormalizeEq, NormalizeLe, NormalizeLt patterns will normalize the literal  |
 |               to be on the right-hand side of the expression.                                |
 |----------------------------------------------------------------------------------------------|#
[AnnotateMaxLengthEq]
((Eq
    $len:(Length *)
    (LiteralInt32 $num:*)
) =>
(AddPattern $len {MaxPosition}) ^
(AddArgument $len {MaxPosition} $num) ^ { })

[AnnotateMaxLengthLe]
((Le
    $len:(Length *)
    (LiteralInt32 $num:*)
) =>
(AddPattern $len {MaxPosition}) ^
(AddArgument $len {MaxPosition} $num) ^ { })

[AnnotateMaxLengthLt]
((Lt
    $len:(Length *)
    (LiteralInt32 $num:*)
) =>
(AddPattern $len {MaxPosition}) ^
(AddArgument $len {MaxPosition} $num) ^ { })

[AnnotateMaxLengthNe]
((Ne
    $len:(Length *)
    (LiteralInt32 $num:*)
) =>
(AddPattern $len {MaxPosition}) ^
(AddArgument $len {MaxPosition} $num) ^ { })

[AnnotateMaxLengthGe]
((Ge
    $len:(Length *)
    (LiteralInt32 $num:*)
) =>
(AddPattern $len {MaxPosition}) ^
(AddArgument $len {MaxPosition} $num) ^ { })

[AnnotateMaxLengthGt]
((Gt
    $len:(Length *)
    (LiteralInt32 $num:*)
) =>
(AddPattern $len {MaxPosition}) ^
(AddArgument $len {MaxPosition} $num) ^ { })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Annotate simple Qil operation with its path properties:                        |
 |                   1. Whether it returns nodes in document order with no duplicates           |
 |                   2. Whether it always returns nodes at the same document depth              |
 | Rationale:    These simple Qil operations are the basic building blocks for more complex     |
 |               patterns, so annotating them helps to identify the more complex patterns.      |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateContent]
($outer:(Content $input:*) =>
(AddStepPattern $outer $input) ^
(AddPattern $outer {Axis}) ^
(AddPattern $outer {IsDocOrderDistinct}) ^
(AddPattern $outer {SameDepth}) ^ { })

[AnnotateAttribute]
($outer:(Attribute $input:* *) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {IsDocOrderDistinct}) ^
(AddPattern $outer {SameDepth}) ^ { })

[AnnotateParent]
($outer:(Parent $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {IsDocOrderDistinct}) ^
(AddPattern $outer {SameDepth}) ^ { })

[AnnotateRoot]
($outer:(Root $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {IsDocOrderDistinct}) ^
(AddPattern $outer {SameDepth}) ^ { })

[AnnotateAncestorSelf]
($outer:(AncestorOrSelf $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^ { })

[AnnotateAncestor]
($outer:(Ancestor $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^ { })

[AnnotateDescendantSelf]
($outer:(DescendantOrSelf $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {IsDocOrderDistinct}) ^ { })

[AnnotateDescendant]
($outer:(Descendant $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {IsDocOrderDistinct}) ^ { })

[AnnotateFollowingSibling]
($outer:(FollowingSibling $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {IsDocOrderDistinct}) ^
(AddPattern $outer {SameDepth}) ^ { })

[AnnotatePrecedingSibling]
($outer:(PrecedingSibling $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {SameDepth}) ^ { })

[AnnotatePreceding]
($outer:(Preceding $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^ { })

[AnnotateXPathFollowing]
($outer:(XPathFollowing $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {IsDocOrderDistinct}) ^ { })

[AnnotateXPathPreceding]
($outer:(XPathPreceding $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^ { })

[AnnotateNodeRange]
($outer:(NodeRange $start:* *) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $start) ^
(AddPattern $outer {IsDocOrderDistinct}) ^ { })

[AnnotateNamespace]
($outer:(XPathNamespace $input:*) =>
(AddPattern $outer {Axis}) ^
(AddStepPattern $outer $input) ^
(AddPattern $outer {IsDocOrderDistinct}) ^
(AddPattern $outer {SameDepth}) ^ { })

[AnnotateUnion]
($outer:(Union * *) =>
(AddPattern $outer {IsDocOrderDistinct}) ^ { })

[AnnotateIntersect]
($outer:(Intersection * *) =>
(AddPattern $outer {IsDocOrderDistinct}) ^ { })

[AnnotateDifference]
($outer:(Difference * *) =>
(AddPattern $outer {IsDocOrderDistinct}) ^ { })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Let iterators should inherit some of the path properties of its binding.       |
 | Rationale:    When let variables are used in paths, as in "$var/foo/bar", it improves        |
 |               performance if the path properties of $var are derived.                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateLet]
($outer:(Let $bind:*) =>
(InheritPattern $outer $bind {Step}) ^
(InheritPattern $outer $bind {IsDocOrderDistinct}) ^
(InheritPattern $outer $bind {SameDepth}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Invoke nodes should inherit some of the path properties of the called function.|
 | Rationale:    When invoke nodes are used in paths, as in "key(...)/foo/bar", it improves     |
 |               performance if the path properties of key(...) are derived.                    |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateInvoke]
($outer:(Invoke (Function * $defn:* * *) *) =>
(InheritPattern $outer $defn {IsDocOrderDistinct}) ^
(InheritPattern $outer $defn {SameDepth}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  OptimizeBarrier nodes should inherit some of the path properties of their      |
 |               argument expression.                                                           |
 | Rationale:    The OptimizeBarrier node should not keep path properties from surfacing.       |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateBarrier]
($outer:(OptimizeBarrier $expr:*) =>
(InheritPattern $outer $expr {IsDocOrderDistinct}) ^
(InheritPattern $outer $expr {SameDepth}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Identify pattern in which two content paths are unioned together, as in        |
 |               "foo | bar".  In this common case, the Union expression will always return     |
 |               nodes at the same level.                                                       |
 | Rationale:    When this pattern is combined in a path, as in "(foo | bar)/baz", a            |
 |               DocOrderDistinct node can be eliminated, since Content of nodes on the same    |
 |               level is always in document order, with no duplicates.                         |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateUnionContent]
($outer:
(Union
    $left:* ^ ((StepPattern? $left {Content}) | (StepPattern? $left {Union}))
    $right:* ^ ((StepPattern? $right {Content}) | (StepPattern? $right {Union}))
             ^ (Equal? (Argument $left {StepInput}) (Argument $right {StepInput}))
) =>
(AddStepPattern $outer (Argument $left {StepInput})) ^
(AddPattern $outer {SameDepth}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Filters propagate the Step, IsDocOrderDistinct, and SameDepth properties of    |
 |               the filtered expression.  They also propagate simple path step information.    |
 | Rationale:    Annotations need to be propagated up the tree so they can be used to make      |
 |               decisions in outer expressions.                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateFilter]
($outer:
(Filter
    $iter:(For $bind:*)
    *
) =>
(InheritPattern $outer $bind {Step}) ^
(InheritPattern $outer $bind {IsDocOrderDistinct}) ^
(InheritPattern $outer $bind {SameDepth}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Identify Filters that are being used to filter all but Element items having a  |
 |               literal QName.  This identifies patterns like "child::foo".                    |
 | Rationale:    Many other patterns are based upon this pattern.  Also, ILGen heavily          |
 |               optimizes this pattern, since it is so common.                                 |
 | Dependencies: Depends on AnnotateFilter to add the Step pattern and to inherit other         |
 |               patterns from $bind.
 |----------------------------------------------------------------------------------------------|#
[AnnotateFilterElements]
($outer:
(Filter
    $iter:(For $bind:* ^ (Pattern? $bind {Axis}))
    (And
        (IsType
            $iter
            (LiteralType $typ:* ^ (Equal? $typ (ConstructType {Element})))
        )
        (Eq
            (NameOf $iter)
            $qname:(LiteralQName * * *)
        )
    )
) =>
(AddPattern $outer {FilterElements}) ^           ; AnnotateFilter already added other patterns
(AddArgument $outer {ElementQName} $qname) ^     ; Add $qname as first argument
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Identify Filter nodes that are being used to filter all but items of some      |
 |               particular kind(s).  This identifies patterns like "child::text()".            |
 | Rationale:    Many other patterns are based upon this pattern.  Also, ILGen heavily          |
 |               optimizes this pattern, since it is so common.                                 |
 | Dependencies: Depends on AnnotateFilter to add the Step pattern and to inherit other         |
 |               patterns from $bind.                                                           |
 |----------------------------------------------------------------------------------------------|#
[AnnotateFilterContentKind]
($outer:
(Filter
    $iter:(For $bind:* ^ (Pattern? $bind {Axis}))
    (IsType
        $iter
        (LiteralType $kind:* ^ (ContentTest? $kind))
    )
) =>
(AddPattern $outer {FilterContentKind}) ^       ; AnnotateFilter already added other patterns
(AddArgument $outer {KindTestType} $kind) ^     ; Add $kind as first argument
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Identify Filter nodes that are being used to select all attributes.  This      |
 |               identifies the pattern "attribute::*".                                         |
 | Rationale:    ILGen heavily optimizes this pattern, since it is so common.                   |
 | Dependencies: Depends on AnnotateFilter to add the Step pattern and to inherit other         |
 |               patterns from the Content operator.                                            |
 |----------------------------------------------------------------------------------------------|#
[AnnotateFilterAttributeKind]
($outer:
(Filter
    $iter:(For (Content *))
    (IsType
        $iter
        (LiteralType $kind:*) ^ (Equal? $kind (ConstructType {Attribute}))
    )
) =>
(AddPattern $outer {FilterAttributeKind}) ^     ; AnnotateFilter already added other patterns
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  If looping zero or one times and constructing a result, the result will retain |
 |               its Dod and SameDetph properties.                                              |
 | Rationale:    Annotations need to be propagated up the tree so they can be used to make      |
 |               decisions in outer expressions.                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateSingletonLoop]
($outer:
(Loop
    (For $bind:* ^ (AtMostOne? (TypeOf $bind)))
    $ret:*
) =>
(InheritPattern $outer $ret {IsDocOrderDistinct}) ^
(InheritPattern $outer $ret {SameDepth}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Root "/" queries always return nodes at the same tree depth (but duplicates    |
 |               possible).                                                                     |
 | Rationale:    Annotations need to be propagated up the tree so they can be used to make      |
 |               decisions in outer expressions.                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateRootLoop]
($outer:
(Loop
    *
    $ret:* ^ (StepPattern? $ret {Root})
) =>
(AddPattern $outer {SameDepth}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  If selecting Content of nodes which are at the same tree depth, the results    |
 |               will also be at the same depth.  If input nodes are in doc-order-distinct,     |
 |               then results will also be doc-order-distinct.                                  |
 | Rationale:    Annotations need to be propagated up the tree so they can be used to make      |
 |               decisions in outer expressions.                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateContentLoop]
($outer:
(Loop
    $iter:(For $bind:* ^ (Pattern? $bind {SameDepth}))
    $ret:* ^ ((StepPattern? $ret {Content}) | (StepPattern? $ret {Union})) ^ (Equal? $iter (Argument $ret {StepInput}))
) =>
(AddPattern $outer {SameDepth}) ^
(InheritPattern $outer $bind {IsDocOrderDistinct}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  If selecting Attributes or Namespaces of doc-order-distinct nodes, then        |
 |               results will also be doc-order-distinct.                                       |
 | Rationale:    Annotations need to be propagated up the tree so they can be used to make      |
 |               decisions in outer expressions.                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateAttrNmspLoop]
($outer:
(Loop
    $iter:(For $bind:*)
    $ret:* ^ ((StepPattern? $ret {Attribute}) | (StepPattern? $ret {XPathNamespace}) | (Pattern? $ret {FilterAttributeKind})) ^ (Equal? $iter (Argument $ret {StepInput}))
) =>
(InheritPattern $outer $bind {SameDepth}) ^
(InheritPattern $outer $bind {IsDocOrderDistinct}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  If selecting Descendants of nodes which are at the same tree depth and which   |
 |               are doc-order-distinct, then results will also be doc-order-distinct.          |
 | Rationale:    Annotations need to be propagated up the tree so they can be used to make      |
 |               decisions in outer expressions.                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateDescendantLoop]
($outer:
(Loop
    $iter:(For $bind:* ^ (Pattern? $bind {SameDepth}))
    $ret:* ^ ((StepPattern? $ret {Descendant}) | (StepPattern? $ret {DescendantOrSelf})) ^ (Equal? $iter (Argument $ret {StepInput}))
) =>
(InheritPattern $outer $bind {IsDocOrderDistinct}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Identify patterns that combine doc-order-distinct operation with reverse       |
 |               document order navigation and filtering.                                       |
 | Rationale:    Applying the DocOrderDistinct operation to a reverse axis can be implemented   |
 |               by simply reversing the result set.  This is a cheaper operation than          |
 |               performing a full doc-order sort with duplicate removal, so ILGen makes this   |
 |               a special-case.                                                                |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateDodReverse]
($outer:
(DocOrderDistinct
    $inner:* ^ (DodReverse? $inner)
) =>
(AddPattern $outer {DodReverse}) ^
(AddArgument $outer {DodStep} $inner) ^ { })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotation, Optional, PathPattern                                              |
 | Description:  Identify patterns that combine doc-order-distinct operation with simple        |
 |               navigation and filtering.  In MSXML 3.0, davidsch, akimball, and neetur worked |
 |               out a series of algorithms that combine navigation with the doc-order-distinct |
 |               operation.  These rule identifies cases where these algorithms can be used.    |
 |               For example, in the expression "descendant::foo/child::bar", child::bar can    |
 |               be implemented using a stack that maintains document order.  This avoids the   |
 |               need for creating a cache of all result nodes and sorting them by doc-order    |
 |               and removing duplicates.                                                       |
 | Rationale:    The navigation/doc-order-distinct algorithms improve performance and reduce    |
 |               working set, so ILGen carries them forward in modified form from MSXML.        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateJoinAndDod]
($outer:
(DocOrderDistinct
    $join:
    (Loop
        $iter:(For $bind:*) ^ (DocOrderDistinct? $bind)
        $ret:* ^ (JoinAndDod? $ret) ^ (Equal? $iter (Argument $ret {StepInput}))
    )
) =>
(AddPattern $outer {JoinAndDod}) ^
(AddArgument $outer {DodStep} $ret) ^     ; Add $ret as first argument
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Required                                                             |
 | Description:  If DocOrderDistinct is applied to a loop which returns sorted function         |
 |               results, then perform a merge sort on 0 or more sorted sets.                   |
 | Rationale:    The Xslt key() function causes this pattern to be generated, in which multiple |
 |               sorted sets need to be merged into a single sorted set.  A merge sort is a     |
 |               much more efficient way of doing this.  Furthermore, in the common case that   |
 |               only one set is returned, no sorting at all needs to be performed.             |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateDodMerge]
($outer:
(DocOrderDistinct
    (Loop
        *
        $ret:(Invoke * *) ^ (DocOrderDistinct? $ret)
    )
) =>
(AddPattern $outer {DodMerge}) ^
{ })

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Identify functions of the form "lookupIndex(Node ndCtxt, string key)", where   |
 |               the ndCtxt document is searched for nodes having the specified key value.  The |
 |               results of such functions are cached and indexed by key value.                 |
 | Rationale:    The XSLT compiler generates functions that match this pattern for xsl:key      |
 |               definitions.  ILGen special-cases these patterns by building indexes that      |
 |               ensure faster access to the results of these functions.                        |
 | Dependencies: Since the pattern matching tool does not allow function headers to be matched, |
 |               this pattern is not complete.  Special-case C# code in XmlILOptimizerVisitor   |
 |               must perform additional analysis in order to determine that an index should    |
 |               be built.                                                                      |
 |----------------------------------------------------------------------------------------------|#
[AnnotateIndex1]
($outer:
(Function
    $args:* ^ { {$args}.Count == 2 } ^ (SubtypeOf? (TypeOf (Nth $args 0)) (ConstructType {Node})) ^ (Equal? (TypeOf (Nth $args 1)) (ConstructType {StringX}))
    $def:
    (Filter
        $iterNodes:(For $bindingNodes:*)
        (Not
            (IsEmpty
                (Filter
                    $iterKeys:(For $bindingKeys:*)
                    (Eq
                        $iterKeys
                        $keyParam:(Parameter * * *) ^ (Equal? $keyParam (Nth $args 1))
                    )
                )
            )
        )
    ) ^ (DocOrderDistinct? $def)
    *
    *
) ^ (SubtypeOf? (TypeOf $outer) (ConstructType {NodeS})) => {
    // The following conditions must be true for this pattern to match:
    //   1. The function must have exactly two arguments
    //   2. The type of the first argument must be a subtype of Node
    //   3. The type of the second argument must be String
    //   4. The return type must be a subtype of Node*
    //   5. The function must return nodes in document order
    //   6. Every reference to $args[0] (context document) must be wrapped in an (Root *) function
    //   7. $keyParam cannot be used with the $bindingNodes and $bindingKeys expressions

    EqualityIndexVisitor visitor = new EqualityIndexVisitor();
    if (visitor.Scan({$bindingNodes}, {$args}[0], {$keyParam}) && visitor.Scan({$bindingKeys}, {$args}[0], {$keyParam})) {
        // All conditions were true, so annotate Filter with the EqualityIndex pattern
        OptimizerPatterns patt = OptimizerPatterns.Write({$def});
        patt.AddPattern(OptimizerPatternName.EqualityIndex);
        patt.AddArgument(OptimizerPatternArgument.IndexedNodes, {$iterNodes});
        patt.AddArgument(OptimizerPatternArgument.KeyExpression, {$bindingKeys});
    }
})

[AnnotateIndex2]
($outer:
(Function
    $args:* ^ { {$args}.Count == 2 } ^ (Equal? (TypeOf (Nth $args 0)) (ConstructType {Node})) ^ (Equal? (TypeOf (Nth $args 1)) (ConstructType {StringX}))
    $def:
    (Filter
        $iterNodes:(For $bindingNodes:*)
        (Eq
            $keyExpr:*
            $keyParam:(Parameter * * *) ^ (Equal? $keyParam (Nth $args 1))
        )
    ) ^ (DocOrderDistinct? $def)
    *
    *
) ^ (SubtypeOf? (TypeOf $outer) (ConstructType {NodeS})) => {
    // Same as EqualityIndex1, except that each nodes has at most one key value

    EqualityIndexVisitor visitor = new EqualityIndexVisitor();
    if (visitor.Scan({$bindingNodes}, {$args}[0], {$keyParam}) && visitor.Scan({$keyExpr}, {$args}[0], {$keyParam})) {
        // All conditions were true, so annotate Filter with the EqualityIndex pattern
        OptimizerPatterns patt = OptimizerPatterns.Write({$def});
        patt.AddPattern(OptimizerPatternName.EqualityIndex);
        patt.AddArgument(OptimizerPatternArgument.IndexedNodes, {$iterNodes});
        patt.AddArgument(OptimizerPatternArgument.KeyExpression, {$keyExpr});
    }
})

#|----------------------------------------------------------------------------------------------|
 | Groups:       Annotate, Optional                                                             |
 | Description:  Identify construction of an Rtf containing a single text node.                 |
 | Rationale:    This is a common pattern in Xslt and so is optimized for performance. An       |
 |               example is <xsl:variable name="foo">bar</xsl:variable>.                        |
 | Dependencies: None.                                                                          |
 |----------------------------------------------------------------------------------------------|#
[AnnotateSingleTextRtf]
($outer:
(RtfCtor
    $ctor:(TextCtor $text:*)
    *
) =>
(AddPattern $outer {SingleTextRtf}) ^
(AddArgument $outer {RtfText} $text) ^ {
    // In this case, Rtf will be pushed onto the stack rather than pushed to the writer
    XmlILConstructInfo.Write({$outer}).PullFromIteratorFirst = true;
})
