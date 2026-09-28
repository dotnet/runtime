#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: TensorPrimitives.CopySign<signed integer> silently wraps where the scalar T.CopySign throws OverflowException
// (MinValue with a positive sign cannot be represented), so the vectorized and scalar semantics differ.
// Run: dotnet run 11-TensorPrimitives-CopySign-Overflow.cs
using System.Numerics.Tensors;

int[] x = new int[16], sign = new int[16], destination = new int[16];
x.AsSpan().Fill(int.MinValue);
sign.AsSpan().Fill(1);

string scalar;
try { scalar = int.CopySign(int.MinValue, 1).ToString(); } catch (OverflowException) { scalar = "OverflowException"; }

string vectorized;
try { TensorPrimitives.CopySign<int>(x, sign, destination); vectorized = destination[0].ToString(); } catch (OverflowException) { vectorized = "OverflowException"; }

Console.WriteLine($"int.CopySign(int.MinValue, 1) = {scalar}; TensorPrimitives.CopySign over 16 x int.MinValue = {vectorized}");
Console.WriteLine(scalar != vectorized ? "REPRODUCED: scalar throws, vectorized wraps." : "NOT REPRODUCED");
