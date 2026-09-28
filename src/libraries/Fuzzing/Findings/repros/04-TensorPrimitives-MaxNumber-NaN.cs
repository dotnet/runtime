#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: TensorPrimitives.MaxNumber / MinNumber / MaxMagnitudeNumber return NaN whenever the input contains a NaN,
// although they are documented to match IEEE 754 maximumNumber/minimumNumber, which ignore NaN.
// Cause: they share MinMaxCore with Max/Min, which bails out on the first NaN.
// Run: dotnet run 04-TensorPrimitives-MaxNumber-NaN.cs
using System.Numerics.Tensors;

bool reproduced = false;
float[][] inputs = [[float.NaN, 2f, 1f], [2f, float.NaN, 1f], [1f, 2f, float.NaN], [3f, float.NaN], [.. Enumerable.Range(0, 64).Select(i => i == 0 ? float.NaN : (float)i)]];
foreach (float[] x in inputs)
{
    float maxNumber = TensorPrimitives.MaxNumber<float>(x), minNumber = TensorPrimitives.MinNumber<float>(x);
    float maxMagnitudeNumber = TensorPrimitives.MaxMagnitudeNumber<float>(x);
    float expectedMax = x.Aggregate(float.MaxNumber), expectedMin = x.Aggregate(float.MinNumber), expectedMaxMagnitude = x.Aggregate(float.MaxMagnitudeNumber);
    Console.WriteLine($"[{string.Join(", ", x.Take(4))}{(x.Length > 4 ? ", ..." : "")}] MaxNumber={maxNumber} (expected {expectedMax}), MinNumber={minNumber} (expected {expectedMin}), MaxMagnitudeNumber={maxMagnitudeNumber} (expected {expectedMaxMagnitude})");
    reproduced |= !maxNumber.Equals(expectedMax) || !minNumber.Equals(expectedMin) || !maxMagnitudeNumber.Equals(expectedMaxMagnitude);
}

Console.WriteLine(reproduced ? "REPRODUCED: the *Number reductions propagate NaN." : "NOT REPRODUCED");
