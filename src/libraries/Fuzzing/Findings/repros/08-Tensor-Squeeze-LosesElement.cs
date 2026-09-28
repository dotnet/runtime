#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: Squeeze() of a tensor whose lengths are all 1 removes every dimension, and the resulting rank-0 view reports
// FlattenedLength 0, so the single element disappears.
// Run: dotnet run 08-Tensor-Squeeze-LosesElement.cs
#pragma warning disable SYSLIB5001
using System.Numerics.Tensors;

var tensor = new ReadOnlyTensorSpan<int>(new[] { 42 }, [1, 1], []);
var squeezed = tensor.Squeeze();
Console.WriteLine($"[1,1] tensor holding 42: FlattenedLength {tensor.FlattenedLength}; Squeeze() -> rank {squeezed.Rank}, FlattenedLength {squeezed.FlattenedLength}, IsEmpty {squeezed.IsEmpty}");

Console.WriteLine(squeezed.FlattenedLength != 1 ? "REPRODUCED: Squeeze drops the only element." : "NOT REPRODUCED");
