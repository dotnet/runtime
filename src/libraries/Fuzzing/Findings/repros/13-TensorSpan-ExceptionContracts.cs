#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: the tensor span constructors throw OverflowException (from checked arithmetic in TensorShape) for very large lengths,
// and Slice(params ReadOnlySpan<nint>) throws IndexOutOfRangeException for an out-of-range start index, although both are
// documented to throw ArgumentOutOfRangeException.
// Run: dotnet run 13-TensorSpan-ExceptionContracts.cs
#pragma warning disable SYSLIB5001
using System.Numerics.Tensors;

int[] data = new int[16];
string ctor, slice;
try { _ = new ReadOnlyTensorSpan<int>(data, [(nint)(1L << 32), (nint)(1L << 32)], []); ctor = "no exception"; }
catch (Exception ex) { ctor = ex.GetType().Name; }
try { _ = new ReadOnlyTensorSpan<int>(data, [4, 4], []).Slice([5, 0]); slice = "no exception"; }
catch (Exception ex) { slice = ex.GetType().Name; }

Console.WriteLine($"new ReadOnlyTensorSpan(int[16], lengths [2^32, 2^32]) -> {ctor}, documented ArgumentOutOfRangeException");
Console.WriteLine($"[4,4] tensor .Slice(5, 0)                            -> {slice}, documented ArgumentOutOfRangeException");
Console.WriteLine(ctor != nameof(ArgumentOutOfRangeException) || slice != nameof(ArgumentOutOfRangeException) ? "REPRODUCED: undocumented exception types." : "NOT REPRODUCED");
