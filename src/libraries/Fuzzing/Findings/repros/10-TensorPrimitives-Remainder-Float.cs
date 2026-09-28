#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: TensorPrimitives.Remainder<float/double/Half> doesn't implement IEEE fmod (the C# % operator) in its vectorized path:
// x % Infinity returns NaN instead of x, and when x / y overflows the result is Infinity instead of a value smaller than |y|.
// Run: dotnet run 10-TensorPrimitives-Remainder-Float.cs
using System.Numerics.Tensors;

float[] x = [1f, 2.5f, -1.5f, -2147483648f, 5f, 7f, 9f, 11f, 13f, 15f, 17f, 19f, 21f, 23f, 25f, 27f];
float[] y = [float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, 1e-30f, 3f, 3f, 3f, 3f, 3f, 3f, 3f, 3f, 3f, 3f, 3f, 3f];
float[] actual = new float[x.Length];
TensorPrimitives.Remainder<float>(x, y, actual);

bool reproduced = false;
for (int i = 0; i < 4; i++)
{
    float expected = x[i] % y[i];
    Console.WriteLine($"{x[i]} % {y[i]}: TensorPrimitives = {actual[i]}, scalar % = {expected}");
    reproduced |= !actual[i].Equals(expected);
}

Console.WriteLine(reproduced ? "REPRODUCED: vectorized Remainder differs from IEEE fmod." : "NOT REPRODUCED");
