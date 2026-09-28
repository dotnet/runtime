#:package System.Numerics.Tensors@11.0.0-rc.1.26425.128
// Finding: the vectorized TensorPrimitives.SinPi/CosPi compute Sin/Cos(x * Pi). Rounding x * Pi to float throws away the
// exactness the *Pi functions exist for: CosPi of integers/half-integers is off, increasingly so for larger x.
// Run: dotnet run 12-TensorPrimitives-CosPi-Accuracy.cs
using System.Numerics.Tensors;

float[] x = [0.5f, 2.5f, 1000.5f, 100000.5f, 1000000f, 65504f, 4097f, 8302.455f];
float[] actual = new float[x.Length];
TensorPrimitives.CosPi<float>(x, actual);

bool reproduced = false;
for (int i = 0; i < x.Length; i++)
{
    float expected = float.CosPi(x[i]);
    Console.WriteLine($"CosPi({x[i]}): TensorPrimitives = {actual[i]}, float.CosPi = {expected}, abs error = {Math.Abs(actual[i] - expected)}");
    reproduced |= Math.Abs(actual[i] - expected) > 1e-4f;
}

Console.WriteLine(reproduced ? "REPRODUCED: vectorized CosPi loses accuracy." : "NOT REPRODUCED");
