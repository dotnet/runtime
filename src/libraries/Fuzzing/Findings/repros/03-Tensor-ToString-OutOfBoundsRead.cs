#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: Tensor.ToString reads the innermost dimension contiguously and ignores its stride.
//  * stride 0 (broadcast): reads past the end of the storage and prints adjacent heap memory;
//  * stride > 1: prints the wrong elements.
// Run: dotnet run 03-Tensor-ToString-OutOfBoundsRead.cs
#pragma warning disable SYSLIB5001
using System.Numerics.Tensors;

int[] storage = [1, 2];
int[] neighbour = [0x5EC4E7, 0x5EC4E7, 0x5EC4E7, 0x5EC4E7]; // allocated next to 'storage'

// Every element of this view is storage[0]; the indexer agrees.
var broadcast = new ReadOnlyTensorSpan<int>(storage, [8], [0]);
string text = broadcast.ToString([8]);
Console.WriteLine($"view of [1, 2] with lengths [8], strides [0]: indexer [0]={broadcast[0]}, [7]={broadcast[7]}");
Console.WriteLine($"ToString([8]) = {text.ReplaceLineEndings(" ")}");
Console.WriteLine("expected      = [1, 1, 1, 1, 1, 1, 1, 1]");

int[] nine = [0, 1, 2, 3, 4, 5, 6, 7, 8];
var strided = new ReadOnlyTensorSpan<int>(nine, [3], [4]);
string stridedText = strided.ToString([3]);
Console.WriteLine($"view of 0..8 with lengths [3], strides [4]: indexer = {strided[0]}, {strided[1]}, {strided[2]}; ToString = {stridedText.ReplaceLineEndings(" ")}");

bool reproduced = !text.Contains("[1, 1, 1, 1, 1, 1, 1, 1]") || !stridedText.Contains("[0, 4, 8]");
Console.WriteLine(reproduced ? "REPRODUCED: ToString does not honour the innermost stride." : "NOT REPRODUCED");
GC.KeepAlive(neighbour);
