#|
//------------------------------------------------------------------------------
// <copyright file="Macros.p" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">akimball</owner>
// <spec>http://webdata/xml/query/docs/QIL%20Normalizer%20Functional%20Specification.doc</spec>
//------------------------------------------------------------------------------
|#


#|
###############################################################################################
###
### Macros.p
###
###     Macros are inlined functions which allow code reuse and modularization with the
###     pattern language.  These macros are used by Annotate.p, ConstantFold.p, and ILGen.p.
###
###############################################################################################
|#


#|----------------------------------------------------------------------------------------------|
 | Utility Macros                                                                               |
 |----------------------------------------------------------------------------------------------|#

; ----- Returns the Nth child of a QilNode
(Nth $expr $idx) = { (QilNode) ({$expr})[{$idx}] }

; ----- Returns the first child of a QilNode
(First $expr) = { (QilNode) ({$expr})[0] }

; ----- Evaluate to true if $left is equal to $right
(Equal? $left $right) = { ({$left}) == ({$right}) }

; ----- Returns true if the number of children of a QilNode is equal to $n
(Count? $expr $n) = { ({$expr}).Count == ({$n}) }

#|----------------------------------------------------------------------------------------------|
 | QilNode Macros                                                                               |
 |----------------------------------------------------------------------------------------------|#

; ----- True if the QilNodeType of $expr is Let
(NodeType? $expr $nodeType) = { ({$expr}).NodeType == QilNodeType.{$nodeType} }

; ----- True if the QilIterator $iter is a global variable rather than a local iterator
(GlobalVariable? $iter) = { IsGlobalVariable({$iter}) }

; ----- True if the QilNodeType of $expr is a literal string, number, QName, etc.
(Literal? $expr) = { IsLiteral(({$expr})) }

; ----- True if the QilNodeType of the children of $expr are literal string, number, QName, etc.
(LiteralArgs? $expr) = { AreLiteralArgs({$expr}) }

; ----- True if $expr has children of type Sequence
(NestedSequences? $expr) = { HasNestedSequence({$expr}) }

; ----- Reuse the iterator node $iter, replacing its binding expression with $replace
(ReplaceBinding $iter $replace) = { ReplaceBinding((QilIterator) {$iter}, {$replace}) }

#|----------------------------------------------------------------------------------------------|
 | XmlQueryType Macros                                                                          |
 |----------------------------------------------------------------------------------------------|#

; ----- Returns the XmlQueryType of the $expr node
(TypeOf $expr) = { ({$expr}).XmlType }

; ---- True if type $derived is a subtype of type $base
(SubtypeOf? $derived $base) = { {$derived}.IsSubtypeOf({$base}) }

; ---- True if an instance of type $derived can never be a subtype of type $base
(NeverSubtypeOf? $derived $base) = { {$derived}.NeverSubtypeOf({$base}) }

; ----- Returns the Prime of XmlQueryType $typ
(Prime $typ) = { {$typ}.Prime }

; ----- True if the cardinality of $typ is guaranteed to be zero
(Empty? $typ) = { ({$typ}).Cardinality == XmlQueryCardinality.Zero }

; ----- True if the cardinality of $typ cannot be zero
(NonEmpty? $typ) = { !({$typ}).MaybeEmpty }

; ----- True if the cardinality of $typ is guaranteed to be one
(Single? $typ) = { ({$typ}).IsSingleton }

; ----- True if the cardinality of $typ is guaranteed to always be zero or one, never more
(AtMostOne? $typ)  = { !({$typ}).MaybeMany }

; ----- True if the datatype and cardinality of $typ is None (meaning the expression will never be
;       constructed at run-time because of an error condition or guaranteed short-circuit)
(None? $typ) = { (object) ({$typ}) == (object) XmlQueryTypeFactory.None }

; ----- True if the datatype of $typ only allows atomic values (no nodes)
(AtomicValue? $typ) = { ({$typ}).IsAtomicValue }

; ----- True if the datatype of $typ only allows text nodes
(OnlyText? $typ) = { ({$typ}.NodeKinds & ~XmlNodeKindFlags.Text) == XmlNodeKindFlags.None }

; ----- True if $typ is one of the XPath content node tests: *, text(), comment(), pi(), or node()
(ContentTest? $typ) = { MatchesContentTest({$typ}) }

; ----- True if $typ is strictly one of the primitive XQuery numeric types: xs:decimal=, xs:integer=, xs:int=, xs:double=, xs:float=
(PrimitiveNumeric? $x) = { IsPrimitiveNumeric({$x}) }

; ----- Returns a pre-defined literal type from the type factory
(ConstructType $name) = { XmlQueryTypeFactory.{$name} }

; ----- Create an XmlQueryType from an XmlTypeCode
(ItemType $code) = { CreateItemType(XmlTypeCode.{$code}) }

; ----- Create a choice of $left and $right types
(ChoiceType $left $right) = { CreateChoiceType({$left}, {$right}) }

; ----- Create a product of $typ and cardinality $card
(ProductType $typ $card) = { CreateProductType({$typ}, XmlQueryCardinality.{$card}) }

#|----------------------------------------------------------------------------------------------|
 | Generic Pattern Macros                                                                       |
 |----------------------------------------------------------------------------------------------|#

; ----- Evaluate to true if the $expr node matches the $pattname pattern
(Pattern? $expr $pattname) = { OptimizerPatterns.Read((QilNode) ({$expr})).MatchesPattern(OptimizerPatternName.{$pattname}) }

; ----- Add pattern $pattname to the $expr node
(AddPattern $expr $pattname) = { OptimizerPatterns.Write((QilNode) ({$expr})).AddPattern(OptimizerPatternName.{$pattname}); }

; ----- If the $src node matches pattern $pattname, then add $pattname to the $dst node
(InheritPattern $dst $src $pattname) = { OptimizerPatterns.Inherit((QilNode) ({$src}), (QilNode) ({$dst}), OptimizerPatternName.{$pattname}); }

; ----- Return the pattern argument identified by $argname, on node $expr
(Argument $expr $argname) = { OptimizerPatterns.Read((QilNode) ({$expr})).GetArgument(OptimizerPatternArgument.{$argname}) }

; ----- Add the pattern argument $arg, identified by $argname, to node $expr
(AddArgument $expr $argname $arg) = { OptimizerPatterns.Write((QilNode) ({$expr})).AddArgument(OptimizerPatternArgument.{$argname}, {$arg}); }

#|----------------------------------------------------------------------------------------------|
 | Specific Pattern Macros                                                                      |
 |----------------------------------------------------------------------------------------------|#

; ----- True if the $expr node matches the Step pattern and if the StepNode argument's node type is $steptype
(StepPattern? $expr $steptype) = { IsStepPattern({$expr}, QilNodeType.{$steptype}) }

; ----- Add the Step pattern to the $expr node, with StepNode = $expr and StepInput = $input
(AddStepPattern $expr $input) = { AddStepPattern((QilNode) ({$expr}), (QilNode) ({$input})); }

; ----- True if the IsDocOrderDistinct pattern exists on the $expr node
(DocOrderDistinct? $expr) = { IsDocOrderDistinct({$expr}) }

; ----- True if the $expr node selects descendants, content, preceding, following, or following-sibling nodes,
; ----- filtered by either an element name or node kind test
(JoinAndDod? $expr) = { AllowJoinAndDod({$expr}) }

; ----- True if the $expr node selects ancestors, preceding, or preceding-sibling nodes, filtered by either
; ----- an element name or node kind test
(DodReverse? $expr) = { AllowDodReverse({$expr}) }

; ----- True if the $nmsp NamespaceDecl node is redundant and can be eliminated
(NamespaceInScope? $nmsp) = { XmlILConstructInfo.Read({$nmsp}).IsNamespaceInScope }

; ----- True if position is *not* required of the $iter iterator
(NonPositionalIterator? $iter) = { !OptimizerPatterns.Read({$iter}).MatchesPattern(OptimizerPatternName.IsPositional) }

; ----- True if the MaybeSideEffects pattern does not exist on the $expr node
(NoSideEffects? $expr) = { !OptimizerPatterns.Read({$expr}).MatchesPattern(OptimizerPatternName.MaybeSideEffects) }

; ----- Add $caller to the list of Invoke nodes which reference the Qil function $func
(AddCaller $func $caller) = { XmlILConstructInfo.Write({$func}).CallersInfo.Add(XmlILConstructInfo.Write({$caller})); }

#|----------------------------------------------------------------------------------------------|
 | Visitor Macros                                                                               |
 |----------------------------------------------------------------------------------------------|#

; ----- Evaluate to true if position is required of the specified iterator somewhere within the $expr subtree
(NonPositional? $expr $iter) = { NonPositional({$expr}, {$iter}) }

; ----- Replace each occurrence of $match within the $expr subtree with $replace
(Subs $expr $match $replace)     = { Subs({$expr}, {$match}, {$replace}) }

; ----- Return the number of occurrences of $find within the $expr subtree
(RefCount $expr $find)           = { nodeCounter.Count({$expr}, {$find}) }

; ----- True if RefCount is <= 1
(RefCountZeroOrOne? $expr $find) = { nodeCounter.Count({$expr}, {$find}) <= 1 }

#|----------------------------------------------------------------------------------------------|
 | Conversion Constant Folding Macros                                                           |
 |----------------------------------------------------------------------------------------------|#

; ----- True if it is possible to convert the literal $lit to type $typ
(CanFoldXsltConvert? $lit $typ) = { CanFoldXsltConvert({$lit}, {$typ}) }

; ----- True if it is possible to convert the literal $lit to type $typ without any loss of information (round-tripping guaranteed)
(CanFoldXsltConvertNonLossy? $lit $typ) = { CanFoldXsltConvertNonLossy({$lit}, {$typ}) }

; ----- Convert the literal $lit to type $typ; if this is not possible, return an explicit XsltConvert node having operand $lit
(FoldXsltConvert $lit $typ)     = { FoldXsltConvert({$lit}, {$typ}) }

#|----------------------------------------------------------------------------------------------|
 | Arithmetic Constant Folding Macros                                                           |
 |----------------------------------------------------------------------------------------------|#

; ----- Constant fold arithmetic operators having literal arguments
(Add! $x $y) = { FoldArithmetic(QilNodeType.Add, (QilLiteral) {$x}, (QilLiteral) {$y}) }
(Sub! $x $y) = { FoldArithmetic(QilNodeType.Subtract, (QilLiteral) {$x}, (QilLiteral) {$y}) }
(Mul! $x $y) = { FoldArithmetic(QilNodeType.Multiply, (QilLiteral) {$x}, (QilLiteral) {$y}) }
(Div! $x $y) = { FoldArithmetic(QilNodeType.Divide, (QilLiteral) {$x}, (QilLiteral) {$y}) }
(Mod! $x $y) = { FoldArithmetic(QilNodeType.Modulo, (QilLiteral) {$x}, (QilLiteral) {$y}) }

; ----- True if corresponding constant folding macro is guaranteed to succeed
(CanFoldAdd? $x $y) = { CanFoldArithmetic(QilNodeType.Add, (QilLiteral) {$x}, (QilLiteral) {$y}) }
(CanFoldSub? $x $y) = { CanFoldArithmetic(QilNodeType.Subtract, (QilLiteral) {$x}, (QilLiteral) {$y}) }
(CanFoldMul? $x $y) = { CanFoldArithmetic(QilNodeType.Multiply, (QilLiteral) {$x}, (QilLiteral) {$y}) }
(CanFoldDiv? $x $y) = { CanFoldArithmetic(QilNodeType.Divide, (QilLiteral) {$x}, (QilLiteral) {$y}) }
(CanFoldMod? $x $y) = { CanFoldArithmetic(QilNodeType.Modulo, (QilLiteral) {$x}, (QilLiteral) {$y}) }

#|----------------------------------------------------------------------------------------------|
 | Value Comparison Folding Macros                                                              |
 |----------------------------------------------------------------------------------------------|#

; ----- Constant fold comparison operators having literal arguments
(Eq! $x $y) = { FoldComparison(QilNodeType.Eq, {$x}, {$y}) }
(Ne! $x $y) = { FoldComparison(QilNodeType.Ne, {$x}, {$y}) }
(Gt! $x $y) = { FoldComparison(QilNodeType.Gt, {$x}, {$y}) }
(Ge! $x $y) = { FoldComparison(QilNodeType.Ge, {$x}, {$y}) }
(Lt! $x $y) = { FoldComparison(QilNodeType.Lt, {$x}, {$y}) }
(Le! $x $y) = { FoldComparison(QilNodeType.Le, {$x}, {$y}) }

