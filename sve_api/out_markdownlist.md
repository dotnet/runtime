
### [Sve stores](https://github.com/dotnet/runtime/issues/94011)
- [ ] Store
- [ ] StoreNarrowing
- [ ] StoreNonTemporal


### [Sve scatterstores](https://github.com/dotnet/runtime/issues/94014)
- [ ] Scatter
- [ ] Scatter16BitNarrowing
- [ ] Scatter16BitWithByteOffsetsNarrowing
- [ ] Scatter32BitNarrowing
- [ ] Scatter32BitWithByteOffsetsNarrowing
- [ ] Scatter8BitNarrowing
- [ ] Scatter8BitWithByteOffsetsNarrowing


### [Sve maths](https://github.com/dotnet/runtime/issues/94009)
- [ ] Abs
- [ ] AbsoluteDifference
- [ ] Add
- [ ] AddAcross
- [ ] AddSaturate
- [ ] Divide
- [ ] DotProduct
- [ ] DotProductBySelectedScalar
- [ ] FusedMultiplyAdd
- [ ] FusedMultiplyAddBySelectedScalar
- [ ] FusedMultiplyAddNegated
- [ ] FusedMultiplySubtract
- [ ] FusedMultiplySubtractBySelectedScalar
- [ ] FusedMultiplySubtractNegated
- [ ] Max
- [ ] MaxAcross
- [ ] MaxNumber
- [ ] MaxNumberAcross
- [ ] Min
- [ ] MinAcross
- [ ] MinNumber
- [ ] MinNumberAcross
- [ ] Multiply
- [ ] MultiplyAdd
- [ ] MultiplyBySelectedScalar
- [ ] MultiplyExtended
- [ ] MultiplySubtract
- [ ] Negate
- [ ] SignExtend16
- [ ] SignExtend32
- [ ] SignExtend8
- [ ] SignExtendWideningLower
- [ ] SignExtendWideningUpper
- [ ] Subtract
- [ ] SubtractSaturate
- [ ] ZeroExtend16
- [ ] ZeroExtend32
- [ ] ZeroExtend8
- [ ] ZeroExtendWideningLower
- [ ] ZeroExtendWideningUpper


### [Sve mask](https://github.com/dotnet/runtime/issues/93964)
- [ ] AbsoluteCompareGreaterThan
- [ ] AbsoluteCompareGreaterThanOrEqual
- [ ] AbsoluteCompareLessThan
- [ ] AbsoluteCompareLessThanOrEqual
- [ ] Compact
- [ ] CompareEqual
- [ ] CompareGreaterThan
- [ ] CompareGreaterThanOrEqual
- [ ] CompareLessThan
- [ ] CompareLessThanOrEqual
- [ ] CompareNotEqualTo
- [ ] CompareUnordered
- [ ] ConditionalExtractAfterLastActiveElement
- [ ] ConditionalExtractAfterLastActiveElementAndReplicate
- [ ] ConditionalExtractLastActiveElement
- [ ] ConditionalExtractLastActiveElementAndReplicate
- [ ] ConditionalSelect
- [ ] CreateBreakAfterMask
- [ ] CreateBreakAfterPropagateMask
- [ ] CreateBreakBeforeMask
- [ ] CreateBreakBeforePropagateMask
- [ ] CreateBreakPropagateMask
- [ ] CreateFalseMaskByte
- [ ] CreateFalseMaskDouble
- [ ] CreateFalseMaskInt16
- [ ] CreateFalseMaskInt32
- [ ] CreateFalseMaskInt64
- [ ] CreateFalseMaskSByte
- [ ] CreateFalseMaskSingle
- [ ] CreateFalseMaskUInt16
- [ ] CreateFalseMaskUInt32
- [ ] CreateFalseMaskUInt64
- [ ] CreateMaskForFirstActiveElement
- [ ] CreateMaskForNextActiveElement
- [ ] CreateTrueMaskByte
- [ ] CreateTrueMaskDouble
- [ ] CreateTrueMaskInt16
- [ ] CreateTrueMaskInt32
- [ ] CreateTrueMaskInt64
- [ ] CreateTrueMaskSByte
- [ ] CreateTrueMaskSingle
- [ ] CreateTrueMaskUInt16
- [ ] CreateTrueMaskUInt32
- [ ] CreateTrueMaskUInt64
- [ ] CreateWhileLessThanMask16Bit
- [ ] CreateWhileLessThanMask32Bit
- [ ] CreateWhileLessThanMask64Bit
- [ ] CreateWhileLessThanMask8Bit
- [ ] CreateWhileLessThanOrEqualMask16Bit
- [ ] CreateWhileLessThanOrEqualMask32Bit
- [ ] CreateWhileLessThanOrEqualMask64Bit
- [ ] CreateWhileLessThanOrEqualMask8Bit
- [ ] ExtractAfterLastScalar
- [ ] ExtractAfterLastVector
- [ ] ExtractLastScalar
- [ ] ExtractLastVector
- [ ] ExtractVector
- [ ] TestAnyTrue
- [ ] TestFirstTrue
- [ ] TestLastTrue


### [Sve loads](https://github.com/dotnet/runtime/issues/94006)
- [ ] Compute16BitAddresses
- [ ] Compute32BitAddresses
- [ ] Compute64BitAddresses
- [ ] Compute8BitAddresses
- [ ] LoadVector
- [ ] LoadVector128AndReplicateToVector
- [ ] LoadVectorByteNonFaultingZeroExtendToInt16
- [ ] LoadVectorByteNonFaultingZeroExtendToInt32
- [ ] LoadVectorByteNonFaultingZeroExtendToInt64
- [ ] LoadVectorByteNonFaultingZeroExtendToUInt16
- [ ] LoadVectorByteNonFaultingZeroExtendToUInt32
- [ ] LoadVectorByteNonFaultingZeroExtendToUInt64
- [ ] LoadVectorByteZeroExtendToInt16
- [ ] LoadVectorByteZeroExtendToInt32
- [ ] LoadVectorByteZeroExtendToInt64
- [ ] LoadVectorByteZeroExtendToUInt16
- [ ] LoadVectorByteZeroExtendToUInt32
- [ ] LoadVectorByteZeroExtendToUInt64
- [ ] LoadVectorInt16NonFaultingSignExtendToInt32
- [ ] LoadVectorInt16NonFaultingSignExtendToInt64
- [ ] LoadVectorInt16NonFaultingSignExtendToUInt32
- [ ] LoadVectorInt16NonFaultingSignExtendToUInt64
- [ ] LoadVectorInt16SignExtendToInt32
- [ ] LoadVectorInt16SignExtendToInt64
- [ ] LoadVectorInt16SignExtendToUInt32
- [ ] LoadVectorInt16SignExtendToUInt64
- [ ] LoadVectorInt32NonFaultingSignExtendToInt64
- [ ] LoadVectorInt32NonFaultingSignExtendToUInt64
- [ ] LoadVectorInt32SignExtendToInt64
- [ ] LoadVectorInt32SignExtendToUInt64
- [ ] LoadVectorNonFaulting
- [ ] LoadVectorNonTemporal
- [ ] LoadVectorSByteNonFaultingSignExtendToInt16
- [ ] LoadVectorSByteNonFaultingSignExtendToInt32
- [ ] LoadVectorSByteNonFaultingSignExtendToInt64
- [ ] LoadVectorSByteNonFaultingSignExtendToUInt16
- [ ] LoadVectorSByteNonFaultingSignExtendToUInt32
- [ ] LoadVectorSByteNonFaultingSignExtendToUInt64
- [ ] LoadVectorSByteSignExtendToInt16
- [ ] LoadVectorSByteSignExtendToInt32
- [ ] LoadVectorSByteSignExtendToInt64
- [ ] LoadVectorSByteSignExtendToUInt16
- [ ] LoadVectorSByteSignExtendToUInt32
- [ ] LoadVectorSByteSignExtendToUInt64
- [ ] LoadVectorUInt16NonFaultingZeroExtendToInt32
- [ ] LoadVectorUInt16NonFaultingZeroExtendToInt64
- [ ] LoadVectorUInt16NonFaultingZeroExtendToUInt32
- [ ] LoadVectorUInt16NonFaultingZeroExtendToUInt64
- [ ] LoadVectorUInt16ZeroExtendToInt32
- [ ] LoadVectorUInt16ZeroExtendToInt64
- [ ] LoadVectorUInt16ZeroExtendToUInt32
- [ ] LoadVectorUInt16ZeroExtendToUInt64
- [ ] LoadVectorUInt32NonFaultingZeroExtendToInt64
- [ ] LoadVectorUInt32NonFaultingZeroExtendToUInt64
- [ ] LoadVectorUInt32ZeroExtendToInt64
- [ ] LoadVectorUInt32ZeroExtendToUInt64
- [ ] LoadVectorx2
- [ ] LoadVectorx3
- [ ] LoadVectorx4
- [ ] PrefetchBytes
- [ ] PrefetchInt16
- [ ] PrefetchInt32
- [ ] PrefetchInt64


### [Sve gatherloads](https://github.com/dotnet/runtime/issues/94007)
- [ ] GatherPrefetch16Bit
- [ ] GatherPrefetch32Bit
- [ ] GatherPrefetch64Bit
- [ ] GatherPrefetch8Bit
- [ ] GatherVector
- [ ] GatherVectorByteZeroExtend
- [ ] GatherVectorInt16SignExtend
- [ ] GatherVectorInt16WithByteOffsetsSignExtend
- [ ] GatherVectorInt32SignExtend
- [ ] GatherVectorInt32WithByteOffsetsSignExtend
- [ ] GatherVectorSByteSignExtend
- [ ] GatherVectorUInt16WithByteOffsetsZeroExtend
- [ ] GatherVectorUInt16ZeroExtend
- [ ] GatherVectorUInt32WithByteOffsetsZeroExtend
- [ ] GatherVectorUInt32ZeroExtend
- [ ] GatherVectorWithByteOffsets


### [Sve fp](https://github.com/dotnet/runtime/issues/94005)
- [ ] AddRotateComplex
- [ ] AddSequentialAcross
- [ ] ConvertToDouble
- [ ] ConvertToInt32
- [ ] ConvertToInt64
- [ ] ConvertToSingle
- [ ] ConvertToUInt32
- [ ] ConvertToUInt64
- [ ] FloatingPointExponentialAccelerator
- [ ] MultiplyAddRotateComplex
- [ ] MultiplyAddRotateComplexBySelectedScalar
- [ ] ReciprocalEstimate
- [ ] ReciprocalExponent
- [ ] ReciprocalSqrtEstimate
- [ ] ReciprocalSqrtStep
- [ ] ReciprocalStep
- [ ] RoundAwayFromZero
- [ ] RoundToNearest
- [ ] RoundToNegativeInfinity
- [ ] RoundToPositiveInfinity
- [ ] RoundToZero
- [ ] Scale
- [ ] Sqrt
- [ ] TrigonometricMultiplyAddCoefficient
- [ ] TrigonometricSelectCoefficient
- [ ] TrigonometricStartingValue


### [Sve firstfaulting](https://github.com/dotnet/runtime/issues/94004)
- [ ] GatherVectorByteZeroExtendFirstFaulting
- [ ] GatherVectorFirstFaulting
- [ ] GatherVectorInt16SignExtendFirstFaulting
- [ ] GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting
- [ ] GatherVectorInt32SignExtendFirstFaulting
- [ ] GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting
- [ ] GatherVectorSByteSignExtendFirstFaulting
- [ ] GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting
- [ ] GatherVectorUInt16ZeroExtendFirstFaulting
- [ ] GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting
- [ ] GatherVectorUInt32ZeroExtendFirstFaulting
- [ ] GatherVectorWithByteOffsetFirstFaulting
- [ ] GetFfr
- [ ] LoadVectorByteZeroExtendFirstFaulting
- [ ] LoadVectorFirstFaulting
- [ ] LoadVectorInt16SignExtendFirstFaulting
- [ ] LoadVectorInt32SignExtendFirstFaulting
- [ ] LoadVectorSByteSignExtendFirstFaulting
- [ ] LoadVectorUInt16ZeroExtendFirstFaulting
- [ ] LoadVectorUInt32ZeroExtendFirstFaulting
- [ ] SetFfr


### [Sve counting](https://github.com/dotnet/runtime/issues/94003)
- [ ] Count16BitElements
- [ ] Count32BitElements
- [ ] Count64BitElements
- [ ] Count8BitElements
- [ ] GetActiveElementCount
- [ ] LeadingSignCount
- [ ] LeadingZeroCount
- [ ] PopCount
- [ ] SaturatingDecrementBy16BitElementCount
- [ ] SaturatingDecrementBy32BitElementCount
- [ ] SaturatingDecrementBy64BitElementCount
- [ ] SaturatingDecrementBy8BitElementCount
- [ ] SaturatingDecrementByActiveElementCount
- [ ] SaturatingIncrementBy16BitElementCount
- [ ] SaturatingIncrementBy32BitElementCount
- [ ] SaturatingIncrementBy64BitElementCount
- [ ] SaturatingIncrementBy8BitElementCount
- [ ] SaturatingIncrementByActiveElementCount


### [Sve bitwise](https://github.com/dotnet/runtime/issues/93887)
- [ ] And
- [ ] AndAcross
- [ ] AndNot
- [ ] BitwiseClear
- [ ] BooleanNot
- [ ] InsertIntoShiftedVector
- [ ] Not
- [ ] Or
- [ ] OrAcross
- [ ] OrNot
- [ ] ShiftLeftLogical
- [ ] ShiftRightArithmetic
- [ ] ShiftRightArithmeticForDivide
- [ ] ShiftRightLogical
- [ ] Xor
- [ ] XorAcross


### [Sve bitmanipulate](https://github.com/dotnet/runtime/issues/94008)
- [ ] DuplicateSelectedScalarToVector
- [ ] ReverseBits
- [ ] ReverseElement
- [ ] ReverseElement16
- [ ] ReverseElement32
- [ ] ReverseElement8
- [ ] Splice
- [ ] TransposeEven
- [ ] TransposeOdd
- [ ] UnzipEven
- [ ] UnzipOdd
- [ ] VectorTableLookup
- [ ] ZipHigh
- [ ] ZipLow


### [Sve2 scatterstores](https://github.com/dotnet/runtime/issues/94023)
- [ ] Scatter16BitNarrowing
- [ ] Scatter16BitWithByteOffsetsNarrowing
- [ ] Scatter32BitNarrowing
- [ ] Scatter32BitWithByteOffsetsNarrowing
- [ ] Scatter8BitNarrowing
- [ ] Scatter8BitWithByteOffsetsNarrowing
- [ ] ScatterNonTemporal


### [Sve2 maths](https://github.com/dotnet/runtime/issues/94022)
- [ ] AbsoluteDifferenceAdd
- [ ] AbsoluteDifferenceAddWideningLower
- [ ] AbsoluteDifferenceAddWideningUpper
- [ ] AbsoluteDifferenceWideningLower
- [ ] AbsoluteDifferenceWideningUpper
- [ ] AddCarryWideningLower
- [ ] AddCarryWideningUpper
- [ ] AddHighNarowingLower
- [ ] AddHighNarowingUpper
- [ ] AddPairwise
- [ ] AddPairwiseWidening
- [ ] AddSaturate
- [ ] AddSaturateWithSignedAddend
- [ ] AddSaturateWithUnsignedAddend
- [ ] AddWideLower
- [ ] AddWideUpper
- [ ] AddWideningLower
- [ ] AddWideningLowerUpper
- [ ] AddWideningUpper
- [ ] DotProductComplex
- [ ] HalvingAdd
- [ ] HalvingSubtract
- [ ] HalvingSubtractReversed
- [ ] MaxNumberPairwise
- [ ] MaxPairwise
- [ ] MinNumberPairwise
- [ ] MinPairwise
- [ ] MultiplyAddBySelectedScalar
- [ ] MultiplyAddWideningLower
- [ ] MultiplyAddWideningUpper
- [ ] MultiplyBySelectedScalar
- [ ] MultiplySubtractBySelectedScalar
- [ ] MultiplySubtractWideningLower
- [ ] MultiplySubtractWideningUpper
- [ ] MultiplyWideningLower
- [ ] MultiplyWideningUpper
- [ ] PolynomialMultiply
- [ ] PolynomialMultiplyWideningLower
- [ ] PolynomialMultiplyWideningUpper
- [ ] RoundingAddHighNarowingLower
- [ ] RoundingAddHighNarowingUpper
- [ ] RoundingHalvingAdd
- [ ] RoundingSubtractHighNarowingLower
- [ ] RoundingSubtractHighNarowingUpper
- [ ] SaturatingAbs
- [ ] SaturatingDoublingMultiplyAddWideningLower
- [ ] SaturatingDoublingMultiplyAddWideningLowerUpper
- [ ] SaturatingDoublingMultiplyAddWideningUpper
- [ ] SaturatingDoublingMultiplyHigh
- [ ] SaturatingDoublingMultiplySubtractWideningLower
- [ ] SaturatingDoublingMultiplySubtractWideningLowerUpper
- [ ] SaturatingDoublingMultiplySubtractWideningUpper
- [ ] SaturatingDoublingMultiplyWideningLower
- [ ] SaturatingDoublingMultiplyWideningUpper
- [ ] SaturatingNegate
- [ ] SaturatingRoundingDoublingMultiplyAddHigh
- [ ] SaturatingRoundingDoublingMultiplyHigh
- [ ] SaturatingRoundingDoublingMultiplySubtractHigh
- [ ] SubtractHighNarowingLower
- [ ] SubtractHighNarowingUpper
- [ ] SubtractSaturate
- [ ] SubtractSaturateReversed
- [ ] SubtractWideLower
- [ ] SubtractWideUpper
- [ ] SubtractWideningLower
- [ ] SubtractWideningLowerUpper
- [ ] SubtractWideningUpper
- [ ] SubtractWideningUpperLower
- [ ] SubtractWithBorrowWideningLower
- [ ] SubtractWithBorrowWideningUpper


### [Sve2 mask](https://github.com/dotnet/runtime/issues/94021)
- [ ] CreateWhileGreaterThanMask
- [ ] CreateWhileGreaterThanOrEqualMask
- [ ] CreateWhileReadAfterWriteMask
- [ ] CreateWhileWriteAfterReadMask
- [ ] Match
- [ ] NoMatch
- [ ] SaturatingExtractNarrowingLower
- [ ] SaturatingExtractNarrowingUpper
- [ ] SaturatingExtractUnsignedNarrowingLower
- [ ] SaturatingExtractUnsignedNarrowingUpper


### [Sve2 gatherloads](https://github.com/dotnet/runtime/issues/94019)
- [ ] GatherVectorByteZeroExtendNonTemporal
- [ ] GatherVectorInt16SignExtendNonTemporal
- [ ] GatherVectorInt16WithByteOffsetsSignExtendNonTemporal
- [ ] GatherVectorInt32SignExtendNonTemporal
- [ ] GatherVectorInt32WithByteOffsetsSignExtendNonTemporal
- [ ] GatherVectorNonTemporal
- [ ] GatherVectorSByteSignExtendNonTemporal
- [ ] GatherVectorUInt16WithByteOffsetsZeroExtendNonTemporal
- [ ] GatherVectorUInt16ZeroExtendNonTemporal
- [ ] GatherVectorUInt32WithByteOffsetsZeroExtendNonTemporal
- [ ] GatherVectorUInt32ZeroExtendNonTemporal


### [Sve2 fp](https://github.com/dotnet/runtime/issues/94018)
- [ ] AddRotateComplex
- [ ] DownConvertNarrowingUpper
- [ ] DownConvertRoundingOdd
- [ ] DownConvertRoundingOddUpper
- [ ] Log2
- [ ] MultiplyAddRotateComplex
- [ ] MultiplyAddRotateComplexBySelectedScalar
- [ ] ReciprocalEstimate
- [ ] ReciprocalSqrtEstimate
- [ ] SaturatingComplexAddRotate
- [ ] SaturatingRoundingDoublingComplexMultiplyAddHighRotate
- [ ] UpConvertWideningUpper


### [Sve2 counting](https://github.com/dotnet/runtime/issues/94017)
- [ ] CountMatchingElements
- [ ] CountMatchingElementsIn128BitSegments


### [Sve2 bitwise](https://github.com/dotnet/runtime/issues/94015)
- [ ] BitwiseClearXor
- [ ] BitwiseSelect
- [ ] BitwiseSelectLeftInverted
- [ ] BitwiseSelectRightInverted
- [ ] ShiftArithmeticRounded
- [ ] ShiftArithmeticRoundedSaturate
- [ ] ShiftArithmeticSaturate
- [ ] ShiftLeftAndInsert
- [ ] ShiftLeftLogicalSaturate
- [ ] ShiftLeftLogicalSaturateUnsigned
- [ ] ShiftLeftLogicalWideningEven
- [ ] ShiftLeftLogicalWideningOdd
- [ ] ShiftLogicalRounded
- [ ] ShiftLogicalRoundedSaturate
- [ ] ShiftRightAndInsert
- [ ] ShiftRightArithmeticAdd
- [ ] ShiftRightArithmeticNarrowingSaturateEven
- [ ] ShiftRightArithmeticNarrowingSaturateOdd
- [ ] ShiftRightArithmeticNarrowingSaturateUnsignedEven
- [ ] ShiftRightArithmeticNarrowingSaturateUnsignedOdd
- [ ] ShiftRightArithmeticRounded
- [ ] ShiftRightArithmeticRoundedAdd
- [ ] ShiftRightArithmeticRoundedNarrowingSaturateEven
- [ ] ShiftRightArithmeticRoundedNarrowingSaturateOdd
- [ ] ShiftRightArithmeticRoundedNarrowingSaturateUnsignedEven
- [ ] ShiftRightArithmeticRoundedNarrowingSaturateUnsignedOdd
- [ ] ShiftRightLogicalAdd
- [ ] ShiftRightLogicalNarrowingEven
- [ ] ShiftRightLogicalNarrowingOdd
- [ ] ShiftRightLogicalRounded
- [ ] ShiftRightLogicalRoundedAdd
- [ ] ShiftRightLogicalRoundedNarrowingEven
- [ ] ShiftRightLogicalRoundedNarrowingOdd
- [ ] ShiftRightLogicalRoundedNarrowingSaturateEven
- [ ] ShiftRightLogicalRoundedNarrowingSaturateOdd
- [ ] Xor
- [ ] XorRotateRight


### [Sve2 bitmanipulate](https://github.com/dotnet/runtime/issues/94020)
- [ ] InterleavingXorLowerUpper
- [ ] InterleavingXorUpperLower
- [ ] MoveWideningLower
- [ ] MoveWideningUpper
- [ ] VectorTableLookup
- [ ] VectorTableLookupExtension


### [SveBf16](https://github.com/dotnet/runtime/issues/94028)
- [ ] Bfloat16DotProduct
- [ ] Bfloat16MatrixMultiplyAccumulate
- [ ] Bfloat16MultiplyAddWideningToSinglePrecisionLower
- [ ] Bfloat16MultiplyAddWideningToSinglePrecisionUpper
- [ ] ConcatenateEvenInt128FromTwoInputs
- [ ] ConcatenateOddInt128FromTwoInputs
- [ ] ConditionalExtractAfterLastActiveElement
- [ ] ConditionalExtractAfterLastActiveElementAndReplicate
- [ ] ConditionalExtractLastActiveElement
- [ ] ConditionalExtractLastActiveElementAndReplicate
- [ ] ConditionalSelect
- [ ] ConvertToBFloat16
- [ ] CreateFalseMaskBFloat16
- [ ] CreateTrueMaskBFloat16
- [ ] CreateWhileReadAfterWriteMask
- [ ] CreateWhileWriteAfterReadMask
- [ ] DotProductBySelectedScalar
- [ ] DownConvertNarrowingUpper
- [ ] DuplicateSelectedScalarToVector
- [ ] ExtractAfterLastScalar
- [ ] ExtractAfterLastVector
- [ ] ExtractLastScalar
- [ ] ExtractLastVector
- [ ] ExtractVector
- [ ] GetActiveElementCount
- [ ] InsertIntoShiftedVector
- [ ] InterleaveEvenInt128FromTwoInputs
- [ ] InterleaveInt128FromHighHalvesOfTwoInputs
- [ ] InterleaveInt128FromLowHalvesOfTwoInputs
- [ ] InterleaveOddInt128FromTwoInputs
- [ ] LoadVector
- [ ] LoadVector128AndReplicateToVector
- [ ] LoadVector256AndReplicateToVector
- [ ] LoadVectorFirstFaulting
- [ ] LoadVectorNonFaulting
- [ ] LoadVectorNonTemporal
- [ ] LoadVectorx2
- [ ] LoadVectorx3
- [ ] LoadVectorx4
- [ ] PopCount
- [ ] ReverseElement
- [ ] Splice
- [ ] Store
- [ ] StoreNonTemporal
- [ ] TransposeEven
- [ ] TransposeOdd
- [ ] UnzipEven
- [ ] UnzipOdd
- [ ] VectorTableLookup
- [ ] VectorTableLookupExtension
- [ ] ZipHigh
- [ ] ZipLow


### [SveF32mm](https://github.com/dotnet/runtime/issues/94024)
- [ ] MatrixMultiplyAccumulate


### [SveF64mm](https://github.com/dotnet/runtime/issues/94025)
- [ ] ConcatenateEvenInt128FromTwoInputs
- [ ] ConcatenateOddInt128FromTwoInputs
- [ ] InterleaveEvenInt128FromTwoInputs
- [ ] InterleaveInt128FromHighHalvesOfTwoInputs
- [ ] InterleaveInt128FromLowHalvesOfTwoInputs
- [ ] InterleaveOddInt128FromTwoInputs
- [ ] LoadVector256AndReplicateToVector
- [ ] MatrixMultiplyAccumulate


### [SveFp16](https://github.com/dotnet/runtime/issues/94026)
- [ ] Abs
- [ ] AbsoluteCompareGreaterThan
- [ ] AbsoluteCompareGreaterThanOrEqual
- [ ] AbsoluteCompareLessThan
- [ ] AbsoluteCompareLessThanOrEqual
- [ ] AbsoluteDifference
- [ ] Add
- [ ] AddAcross
- [ ] AddPairwise
- [ ] AddRotateComplex
- [ ] AddSequentialAcross
- [ ] CompareEqual
- [ ] CompareGreaterThan
- [ ] CompareGreaterThanOrEqual
- [ ] CompareLessThan
- [ ] CompareLessThanOrEqual
- [ ] CompareNotEqualTo
- [ ] CompareUnordered
- [ ] ConcatenateEvenInt128FromTwoInputs
- [ ] ConcatenateOddInt128FromTwoInputs
- [ ] ConditionalExtractAfterLastActiveElement
- [ ] ConditionalExtractAfterLastActiveElementAndReplicate
- [ ] ConditionalExtractLastActiveElement
- [ ] ConditionalExtractLastActiveElementAndReplicate
- [ ] ConditionalSelect
- [ ] ConvertToDouble
- [ ] ConvertToHalf
- [ ] ConvertToInt16
- [ ] ConvertToInt32
- [ ] ConvertToInt64
- [ ] ConvertToSingle
- [ ] ConvertToUInt16
- [ ] ConvertToUInt32
- [ ] ConvertToUInt64
- [ ] CreateFalseMaskHalf
- [ ] CreateTrueMaskHalf
- [ ] CreateWhileReadAfterWriteMask
- [ ] CreateWhileWriteAfterReadMask
- [ ] Divide
- [ ] DownConvertNarrowingUpper
- [ ] DuplicateSelectedScalarToVector
- [ ] ExtractAfterLastScalar
- [ ] ExtractAfterLastVector
- [ ] ExtractLastScalar
- [ ] ExtractLastVector
- [ ] ExtractVector
- [ ] FloatingPointExponentialAccelerator
- [ ] FusedMultiplyAdd
- [ ] FusedMultiplyAddBySelectedScalar
- [ ] FusedMultiplyAddNegated
- [ ] FusedMultiplySubtract
- [ ] FusedMultiplySubtractBySelectedScalar
- [ ] FusedMultiplySubtractNegated
- [ ] GetActiveElementCount
- [ ] InsertIntoShiftedVector
- [ ] InterleaveEvenInt128FromTwoInputs
- [ ] InterleaveInt128FromHighHalvesOfTwoInputs
- [ ] InterleaveInt128FromLowHalvesOfTwoInputs
- [ ] InterleaveOddInt128FromTwoInputs
- [ ] LoadVector
- [ ] LoadVector128AndReplicateToVector
- [ ] LoadVector256AndReplicateToVector
- [ ] LoadVectorFirstFaulting
- [ ] LoadVectorNonFaulting
- [ ] LoadVectorNonTemporal
- [ ] LoadVectorx2
- [ ] LoadVectorx3
- [ ] LoadVectorx4
- [ ] Log2
- [ ] Max
- [ ] MaxAcross
- [ ] MaxNumber
- [ ] MaxNumberAcross
- [ ] MaxNumberPairwise
- [ ] MaxPairwise
- [ ] Min
- [ ] MinAcross
- [ ] MinNumber
- [ ] MinNumberAcross
- [ ] MinNumberPairwise
- [ ] MinPairwise
- [ ] Multiply
- [ ] MultiplyAddRotateComplex
- [ ] MultiplyAddRotateComplexBySelectedScalar
- [ ] MultiplyAddWideningLower
- [ ] MultiplyAddWideningUpper
- [ ] MultiplyBySelectedScalar
- [ ] MultiplyExtended
- [ ] MultiplySubtractWideningLower
- [ ] MultiplySubtractWideningUpper
- [ ] Negate
- [ ] PopCount
- [ ] ReciprocalEstimate
- [ ] ReciprocalExponent
- [ ] ReciprocalSqrtEstimate
- [ ] ReciprocalSqrtStep
- [ ] ReciprocalStep
- [ ] ReverseElement
- [ ] RoundAwayFromZero
- [ ] RoundToNearest
- [ ] RoundToNegativeInfinity
- [ ] RoundToPositiveInfinity
- [ ] RoundToZero
- [ ] Scale
- [ ] Splice
- [ ] Sqrt
- [ ] Store
- [ ] StoreNonTemporal
- [ ] Subtract
- [ ] TransposeEven
- [ ] TransposeOdd
- [ ] TrigonometricMultiplyAddCoefficient
- [ ] TrigonometricSelectCoefficient
- [ ] TrigonometricStartingValue
- [ ] UnzipEven
- [ ] UnzipOdd
- [ ] UpConvertWideningUpper
- [ ] VectorTableLookup
- [ ] VectorTableLookupExtension
- [ ] ZipHigh
- [ ] ZipLow


### [SveI8mm](https://github.com/dotnet/runtime/issues/94027)
- [ ] DotProductSignedUnsigned
- [ ] DotProductUnsignedSigned
- [ ] MatrixMultiplyAccumulate
- [ ] MatrixMultiplyAccumulateUnsignedSigned


### [Sha3](https://github.com/dotnet/runtime/issues/98692)
- [ ] BitwiseClearXor
- [ ] BitwiseRotateLeftBy1AndXor
- [ ] Xor
- [ ] XorRotateRight


### [Sm4](https://github.com/dotnet/runtime/issues/98696)
- [ ] Sm4EncryptionAndDecryption
- [ ] Sm4KeyUpdates


### [SveAes](https://github.com/dotnet/runtime/issues/94423)
- [ ] AesInverseMixColumns
- [ ] AesMixColumns
- [ ] AesSingleRoundDecryption
- [ ] AesSingleRoundEncryption
- [ ] PolynomialMultiplyWideningLower
- [ ] PolynomialMultiplyWideningUpper


### [SveBitperm](https://github.com/dotnet/runtime/issues/94424)
- [ ] GatherLowerBitsFromPositionsSelectedByBitmask
- [ ] GroupBitsToRightOrLeftAsSelectedByBitmask
- [ ] ScatterLowerBitsIntoPositionsSelectedByBitmask


### [SveSha3](https://github.com/dotnet/runtime/issues/94425)
- [ ] BitwiseRotateLeftBy1AndXor


### [SveSm4](https://github.com/dotnet/runtime/issues/94426)
- [ ] Sm4EncryptionAndDecryption
- [ ] Sm4KeyUpdates


