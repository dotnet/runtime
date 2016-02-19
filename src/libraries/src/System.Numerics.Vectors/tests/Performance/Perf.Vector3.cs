// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using Xunit;
using System.Numerics;
using System.Collections.Generic;

namespace System.Numerics.Tests
{
    public class Perf_Vector3
    {
        public static IEnumerable<object[]> TestOperations()
        {
            foreach (Operations op in Enum.GetValues(typeof(Operations)))
                yield return new object[] { op };
        }

        [Benchmark]
        [MemberData(nameof(TestOperations))]
        public void Operation(Operations operation)
        {
            Random rand = new Random(84329);
            Vector3 v1 = new Vector3(Convert.ToSingle(rand.NextDouble()), Convert.ToSingle(rand.NextDouble()), Convert.ToSingle(rand.NextDouble()));
            Vector3 v2 = new Vector3(Convert.ToSingle(rand.NextDouble()), Convert.ToSingle(rand.NextDouble()), Convert.ToSingle(rand.NextDouble()));
            foreach (var iteration in Benchmark.Iterations)
                using (iteration.StartMeasurement())
                    ExecuteTest(operation, 1000000, v1, v2);
        }

        public void ExecuteTest(Operations operation, int innerIterations, Vector3 v1, Vector3 v2)
        {
            Vector3 res;
            switch (operation)
            {
                case Operations.Add_Operator:
                    for (int i = 0; i < innerIterations; i++)
                    { res = v1 + v2; res = v1 + v2; res = v1 + v2; res = v1 + v2; res = v1 + v2; res = v1 + v2; res = v1 + v2; res = v1 + v2; res = v1 + v2; res = v1 + v2; }
                    break;
                case Operations.Add_Function:
                    for (int i = 0; i < innerIterations; i++)
                    { Vector3.Add(v1, v2); Vector3.Add(v1, v2); Vector3.Add(v1, v2); Vector3.Add(v1, v2); Vector3.Add(v1, v2); Vector3.Add(v1, v2); Vector3.Add(v1, v2); Vector3.Add(v1, v2); Vector3.Add(v1, v2); Vector3.Add(v1, v2); }
                    break;
                case Operations.Sub_Operator:
                    for (int i = 0; i < innerIterations; i++)
                    { res = v1 - v2; res = v1 - v2; res = v1 - v2; res = v1 - v2; res = v1 - v2; res = v1 - v2; res = v1 - v2; res = v1 - v2; res = v1 - v2; res = v1 - v2; }
                    break;
                case Operations.Sub_Function:
                    for (int i = 0; i < innerIterations; i++)
                    { Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); Vector3.Subtract(v1, v2); }
                    break;
                case Operations.Mul_Operator:
                    for (int i = 0; i < innerIterations; i++)
                    { res = v1 * v2; res = v1 * v2; res = v1 * v2; res = v1 * v2; res = v1 * v2; res = v1 * v2; res = v1 * v2; res = v1 * v2; res = v1 * v2; res = v1 * v2; }
                    break;
                case Operations.Mul_Function:
                    for (int i = 0; i < innerIterations; i++)
                    { Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); Vector3.Multiply(v1, v2); }
                    break;
                case Operations.Dot:
                    for (int i = 0; i < innerIterations; i++)
                    { Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); Vector3.Dot(v1, v2); }
                    break;
                case Operations.SquareRoot:
                    for (int i = 0; i < innerIterations; i++)
                    { Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); Vector3.SquareRoot(v1); }
                    break;
                case Operations.Length_Squared:
                    for (int i = 0; i < innerIterations; i++)
                    { v1.LengthSquared(); v1.LengthSquared(); v1.LengthSquared(); v1.LengthSquared(); v1.LengthSquared(); v1.LengthSquared(); v1.LengthSquared(); v1.LengthSquared(); v1.LengthSquared(); v1.LengthSquared(); }
                    break;
                case Operations.Normalize:
                    for (int i = 0; i < innerIterations; i++)
                    { Vector3.Normalize(v1); Vector3.Normalize(v1); Vector3.Normalize(v1); Vector3.Normalize(v1); Vector3.Normalize(v1); Vector3.Normalize(v1); Vector3.Normalize(v1); Vector3.Normalize(v1); Vector3.Normalize(v1); Vector3.Normalize(v1); }
                    break;
                case Operations.Distance_Squared:
                    for (int i = 0; i < innerIterations; i++)
                    { Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); Vector3.DistanceSquared(v1, v2); }
                    break;
                case Operations.Cross:
                    for (int i = 0; i < innerIterations; i++)
                    { Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); Vector3.Cross(v1, v2); }
                    break;
            }
        }

        public enum Operations
        {
            Add_Operator = 1,
            Add_Function = 2,
            Sub_Operator = 3,
            Sub_Function = 4,
            Mul_Operator = 5,
            Mul_Function = 6,
            Dot = 7,
            SquareRoot = 8,
            Length_Squared = 9,
            Normalize = 10,
            Distance_Squared = 11,
            Cross = 12
        }
    }
}
