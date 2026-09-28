#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: Tensor.ResizeTo is documented as "If the final shape is bigger it is filled with 0s", but neither the dense nor the
// strided code path clears the part of the destination beyond the source, so stale destination data survives.
// Run: dotnet run 09-Tensor-ResizeTo-NoZeroFill.cs
#pragma warning disable SYSLIB5001
using System.Numerics.Tensors;

int[] destination = [9, 9, 9, 9, 9, 9];
Tensor.ResizeTo(new ReadOnlyTensorSpan<int>(new[] { 1, 2, 3 }, [3], []), new TensorSpan<int>(destination, [2, 3], []));
Console.WriteLine($"ResizeTo([1, 2, 3] -> [2, 3] destination full of 9s) = [{string.Join(", ", destination)}], documented [1, 2, 3, 0, 0, 0]");

Console.WriteLine(destination[3..].Any(v => v != 0) ? "REPRODUCED: the tail isn't zero-filled." : "NOT REPRODUCED");
