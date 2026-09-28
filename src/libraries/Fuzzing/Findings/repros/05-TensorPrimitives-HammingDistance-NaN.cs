#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: TensorPrimitives.HammingDistance<float/double> is documented to count !EqualityComparer<T>.Default.Equals(x[i], y[i])
// (NaN equals NaN), but the vectorized path uses Vector.Equals (NaN != NaN) while the scalar path follows the documentation,
// so the same data gives different answers depending on the span length.
// Run: dotnet run 05-TensorPrimitives-HammingDistance-NaN.cs
using System.Numerics.Tensors;

bool reproduced = false;
foreach (int length in new[] { 3, 64 })
{
    float[] x = new float[length], y = new float[length];
    x.AsSpan().Fill(float.NaN);
    y.AsSpan().Fill(float.NaN);
    int actual = TensorPrimitives.HammingDistance<float>(x, y);
    Console.WriteLine($"{length} x (NaN vs NaN): HammingDistance = {actual}, documented = 0");
    reproduced |= actual != 0;
}

Console.WriteLine(reproduced ? "REPRODUCED: NaN vs NaN counts as a difference in the vectorized path." : "NOT REPRODUCED");
