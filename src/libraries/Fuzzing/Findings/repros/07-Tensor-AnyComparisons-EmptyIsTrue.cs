#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: the *Any tensor comparisons (EqualsAny, GreaterThanAny, ...) return true for empty tensors, where there is no
// element for which the comparison holds.
// Run: dotnet run 07-Tensor-AnyComparisons-EmptyIsTrue.cs
#pragma warning disable SYSLIB5001
using System.Numerics.Tensors;

bool equalsAny = Tensor.EqualsAny<int>(new ReadOnlyTensorSpan<int>(new int[4], [2, 0], []), new ReadOnlyTensorSpan<int>(new int[4], [2, 0], []));
bool greaterThanAny = Tensor.GreaterThanAny<int>(new ReadOnlyTensorSpan<int>(new int[4], [2, 0], []), new ReadOnlyTensorSpan<int>(new int[4], [2, 0], []));
Console.WriteLine($"EqualsAny(empty [2,0], empty [2,0])      = {equalsAny}, expected False");
Console.WriteLine($"GreaterThanAny(empty [2,0], empty [2,0]) = {greaterThanAny}, expected False");

Console.WriteLine(equalsAny || greaterThanAny ? "REPRODUCED: *Any is true for empty tensors." : "NOT REPRODUCED");
