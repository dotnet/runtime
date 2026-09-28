#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: the pairwise tensor comparisons (EqualsAll/Any, GreaterThanAll/Any, LessThanAll/Any, ...) broadcast x and y,
// but TensorOperation.Invoke(x, y) iterates over the shape of whichever operand has the larger FlattenedLength instead of the
// broadcast shape. Bidirectional broadcasts give wrong answers; when the chosen operand has the lower rank, Debug/Checked builds
// hit Debug.Assert(indexes.Length >= Rank) in TensorShape.AdjustToNextIndex.
// Run: dotnet run 06-Tensor-PairwiseComparisons-WrongShape.cs
#pragma warning disable SYSLIB5001
using System.Numerics.Tensors;

int[] column = [1, 2, 3], row = [9, 9, 9, 3];
// column [3,1] vs row [1,4] broadcast to [3,4]; column[2] == 3 == row[3].
bool equalsAny = Tensor.EqualsAny<int>(new ReadOnlyTensorSpan<int>(column, [3, 1], []), new ReadOnlyTensorSpan<int>(row, [1, 4], []));
bool lessThanAll = Tensor.LessThanAll<int>(new ReadOnlyTensorSpan<int>(column, [3, 1], []), new ReadOnlyTensorSpan<int>(row, [1, 4], []));
Console.WriteLine($"EqualsAny(column 1,2,3 as [3,1]; row 9,9,9,3 as [1,4])   = {equalsAny}, expected True (3 == 3)");
Console.WriteLine($"LessThanAll(column 1,2,3 as [3,1]; row 9,9,9,3 as [1,4]) = {lessThanAll}, expected False (3 < 3 is false)");

Console.WriteLine(equalsAny != true || lessThanAll != false ? "REPRODUCED: comparisons don't iterate the broadcast shape." : "NOT REPRODUCED");
