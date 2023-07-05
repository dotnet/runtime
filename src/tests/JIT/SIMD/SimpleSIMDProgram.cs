// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Numerics;
using Xunit;

namespace SIMDDebugTest
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            Vector4Test.RunTests();
            Vector3Test.RunTests();
            Vector2Test.RunTests();
            FuncEvalTest.RunTests();
            return 100;
        }

        class Vector4Test
        {
            public static int RunTests()
            {
                AddTest.RunTests();
                SubTest.RunTests();
                MulTest.RunTests();
                DivTest.RunTests();
                return 0;
            }
            public class AddTest
            {
                public static int RunTests()
                {
                    Vector4 A = new Vector4(2);
                    Vector4 B = new Vector4(1);
                    Vector4 C = A + B;
                    Vector4 D = VectorAdd(A, B);
                    Vector4 E = VectorAdd(ref A, ref B);
                    Vector4 F = VectorAdd(ref A, B);
                    return 0;
                }

                public static Vector4 VectorAdd(Vector4 v1, Vector4 v2)
                {
                    Vector4 v3 = v1 + v2;
                    return v3;
                }

                public static Vector4 VectorAdd(ref Vector4 v1, ref Vector4 v2)
                {
                    Vector4 v3 = v1 + v2;
                    return v3;
                }

                public static Vector4 VectorAdd(ref Vector4 v1, Vector4 v2)
                {
                    Vector4 v3 = v1 + v2;
                    return v3;
                }

            }

            public class SubTest
            {
                public static int RunTests()
                {
                    Vector4 A = new Vector4(3);
                    Vector4 B = new Vector4(2);
                    Vector4 C = A - B;
                    Vector4 D = VectorSub(A, B);
                    Vector4 E = VectorSub(ref A, ref B);
                    Vector4 F = VectorSub(ref A, B);
                    return 0;
                }
                public static Vector4 VectorSub(Vector4 v1, Vector4 v2)
                {
                    Vector4 v3 = v1 - v2;
                    return v3;
                }
                public static Vector4 VectorSub(ref Vector4 v1, ref Vector4 v2)
                {
                    Vector4 v3 = v1 - v2;
                    return v3;
                }

                public static Vector4 VectorSub(ref Vector4 v1, Vector4 v2)
                {
                    Vector4 v3 = v1 - v2;
                    return v3;
                }
            }

            public class MulTest
            {
                public static int RunTests()
                {
                    Vector4 A = new Vector4(2);
                    Vector4 B = new Vector4(3);
                    Vector4 C = A * B;
                    Vector4 D = VectorMul(A, B);
                    Vector4 E = VectorMul(ref A, ref B);
                    Vector4 F = VectorMul(ref A, B);
                    Vector4 G = VectorMul(A, 2f);
                    Vector4 H = VectorMul(ref A, 2f);
                    return 0;
                }

                public static Vector4 VectorMul(Vector4 v1, Vector4 v2)
                {
                    Vector4 v3 = v1 * v2;
                    return v3;
                }

                public static Vector4 VectorMul(ref Vector4 v1, ref Vector4 v2)
                {
                    Vector4 v3 = v1 * v2;
                    return v3;
                }

                public static Vector4 VectorMul(ref Vector4 v1, Vector4 v2)
                {
                    Vector4 v3 = v1 * v2;
                    return v3;
                }

                public static Vector4 VectorMul(Vector4 v1, float v)
                {
                    Vector4 v2 = v1 * v;
                    return v2;
                }

                public static Vector4 VectorMul(ref Vector4 v1, float v)
                {
                    Vector4 v2 = v1 * v;
                    return v2;
                }

            }

            public class DivTest
            {
                public static int RunTests()
                {
                    Vector4 A = new Vector4(2);
                    Vector4 B = new Vector4(3);
                    Vector4 C = A / B;
                    Vector4 D = VectorDiv(A, B);
                    Vector4 E = VectorDiv(ref A, ref B);
                    Vector4 F = VectorDiv(ref A, B);
                    Vector4 G = VectorDiv(A, 3f);
                    Vector4 H = VectorDiv(ref A, 3f);
                    return 0;
                }

                public static Vector4 VectorDiv(Vector4 v1, Vector4 v2)
                {
                    Vector4 v3 = v1 / v2;
                    return v3;
                }

                public static Vector4 VectorDiv(ref Vector4 v1, ref Vector4 v2)
                {
                    Vector4 v3 = v1 / v2;
                    return v3;
                }

                public static Vector4 VectorDiv(ref Vector4 v1, Vector4 v2)
                {
                    Vector4 v3 = v1 / v2;
                    return v3;
                }

                public static Vector4 VectorDiv(Vector4 v1, float v)
                {
                    Vector4 v2 = v1 / v;
                    return v2;
                }

                public static Vector4 VectorDiv(ref Vector4 v1, float v)
                {
                    Vector4 v2 = v1 / v;
                    return v2;
                }
            }
        }

        class Vector3Test
        {
            public static int RunTests()
            {
                AddTest.RunTests();
                SubTest.RunTests();
                MulTest.RunTests();
                DivTest.RunTests();
                return 0;
            }
            public class AddTest
            {
                public static int RunTests()
                {
                    Vector3 A = new Vector3(2);
                    Vector3 B = new Vector3(1);
                    Vector3 C = A + B;
                    Vector3 D = VectorAdd(A, B);
                    Vector3 E = VectorAdd(ref A, ref B);
                    Vector3 F = VectorAdd(ref A, B);
                    return 0;
                }

                public static Vector3 VectorAdd(Vector3 v1, Vector3 v2)
                {
                    Vector3 v3 = v1 + v2;
                    return v3;
                }

                public static Vector3 VectorAdd(ref Vector3 v1, ref Vector3 v2)
                {
                    Vector3 v3 = v1 + v2;
                    return v3;
                }

                public static Vector3 VectorAdd(ref Vector3 v1, Vector3 v2)
                {
                    Vector3 v3 = v1 + v2;
                    return v3;
                }
            }

            public class SubTest
            {
                public static int RunTests()
                {
                    Vector3 A = new Vector3(3);
                    Vector3 B = new Vector3(2);
                    Vector3 C = A - B;
                    Vector3 D = VectorSub(A, B);
                    Vector3 E = VectorSub(ref A, ref B);
                    Vector3 F = VectorSub(ref A, B);
                    return 0;
                }
                public static Vector3 VectorSub(Vector3 v1, Vector3 v2)
                {
                    Vector3 v3 = v1 - v2;
                    return v3;
                }
                public static Vector3 VectorSub(ref Vector3 v1, ref Vector3 v2)
                {
                    Vector3 v3 = v1 - v2;
                    return v3;
                }

                public static Vector3 VectorSub(ref Vector3 v1, Vector3 v2)
                {
                    Vector3 v3 = v1 - v2;
                    return v3;
                }
            }
            public class MulTest
            {
                public static int RunTests()
                {
                    Vector3 A = new Vector3(2);
                    Vector3 B = new Vector3(3);
                    Vector3 C = A * B;
                    Vector3 D = VectorMul(A, B);
                    Vector3 E = VectorMul(ref A, ref B);
                    Vector3 F = VectorMul(ref A, B);
                    Vector3 G = VectorMul(A, 2f);
                    Vector3 H = VectorMul(ref A, 2f);
                    return 0;
                }

                public static Vector3 VectorMul(Vector3 v1, Vector3 v2)
                {
                    Vector3 v3 = v1 * v2;
                    return v3;
                }

                public static Vector3 VectorMul(ref Vector3 v1, ref Vector3 v2)
                {
                    Vector3 v3 = v1 * v2;
                    return v3;
                }

                public static Vector3 VectorMul(ref Vector3 v1, Vector3 v2)
                {
                    Vector3 v3 = v1 * v2;
                    return v3;
                }

                public static Vector3 VectorMul(Vector3 v1, float v)
                {
                    Vector3 v2 = v1 * v;
                    return v2;
                }

                public static Vector3 VectorMul(ref Vector3 v1, float v)
                {
                    Vector3 v2 = v1 * v;
                    return v2;
                }

            }

            public class DivTest
            {
                public static int RunTests()
                {
                    Vector3 A = new Vector3(2);
                    Vector3 B = new Vector3(3);
                    Vector3 C = A / B;
                    Vector3 D = VectorDiv(A, B);
                    Vector3 E = VectorDiv(ref A, ref B);
                    Vector3 F = VectorDiv(ref A, B);
                    Vector3 G = VectorDiv(A, 3f);
                    Vector3 H = VectorDiv(ref A, 3f);
                    return 0;
                }

                public static Vector3 VectorDiv(Vector3 v1, Vector3 v2)
                {
                    Vector3 v3 = v1 / v2;
                    return v3;
                }

                public static Vector3 VectorDiv(ref Vector3 v1, ref Vector3 v2)
                {
                    Vector3 v3 = v1 / v2;
                    return v3;
                }

                public static Vector3 VectorDiv(ref Vector3 v1, Vector3 v2)
                {
                    Vector3 v3 = v1 / v2;
                    return v3;
                }

                public static Vector3 VectorDiv(Vector3 v1, float v)
                {
                    Vector3 v2 = v1 / v;
                    return v2;
                }

                public static Vector3 VectorDiv(ref Vector3 v1, float v)
                {
                    Vector3 v2 = v1 / v;
                    return v2;
                }
            }
        }

        class Vector2Test
        {
            public static int RunTests()
            {
                AddTest.RunTests();
                SubTest.RunTests();
                MulTest.RunTests();
                DivTest.RunTests();
                return 0;
            }
            public class AddTest
            {
                public static int RunTests()
                {
                    Vector2 A = new Vector2(2);
                    Vector2 B = new Vector2(1);
                    Vector2 C = A + B;
                    Vector2 D = VectorAdd(A, B);
                    Vector2 E = VectorAdd(ref A, ref B);
                    Vector2 F = VectorAdd(ref A, B);
                    return 0;
                }

                public static Vector2 VectorAdd(Vector2 v1, Vector2 v2)
                {
                    Vector2 v3 = v1 + v2;
                    return v3;
                }

                public static Vector2 VectorAdd(ref Vector2 v1, ref Vector2 v2)
                {
                    Vector2 v3 = v1 + v2;
                    return v3;
                }

                public static Vector2 VectorAdd(ref Vector2 v1, Vector2 v2)
                {
                    Vector2 v3 = v1 + v2;
                    return v3;
                }
            }

            public class SubTest
            {
                public static int RunTests()
                {
                    Vector2 A = new Vector2(3);
                    Vector2 B = new Vector2(2);
                    Vector2 C = A - B;
                    Vector2 D = VectorSub(A, B);
                    Vector2 E = VectorSub(ref A, ref B);
                    Vector2 F = VectorSub(ref A, B);
                    return 0;
                }

                public static Vector2 VectorSub(Vector2 v1, Vector2 v2)
                {
                    Vector2 v3 = v1 - v2;
                    return v3;
                }

                public static Vector2 VectorSub(ref Vector2 v1, ref Vector2 v2)
                {
                    Vector2 v3 = v1 - v2;
                    return v3;
                }

                public static Vector2 VectorSub(ref Vector2 v1, Vector2 v2)
                {
                    Vector2 v3 = v1 - v2;
                    return v3;
                }
            }

            public class MulTest
            {
                public static int RunTests()
                {
                    Vector2 A = new Vector2(2);
                    Vector2 B = new Vector2(3);
                    Vector2 C = A * B;
                    Vector2 D = VectorMul(A, B);
                    Vector2 E = VectorMul(ref A, ref B);
                    Vector2 F = VectorMul(ref A, B);
                    Vector2 G = VectorMul(A, 2f);
                    Vector2 H = VectorMul(ref A, 2f);
                    return 0;
                }

                public static Vector2 VectorMul(Vector2 v1, Vector2 v2)
                {
                    Vector2 v3 = v1 * v2;
                    return v3;
                }

                public static Vector2 VectorMul(ref Vector2 v1, ref Vector2 v2)
                {
                    Vector2 v3 = v1 * v2;
                    return v3;
                }

                public static Vector2 VectorMul(ref Vector2 v1, Vector2 v2)
                {
                    Vector2 v3 = v1 * v2;
                    return v3;
                }

                public static Vector2 VectorMul(Vector2 v1, float v)
                {
                    Vector2 v2 = v1 * v;
                    return v2;
                }

                public static Vector2 VectorMul(ref Vector2 v1, float v)
                {
                    Vector2 v2 = v1 * v;
                    return v2;
                }

            }

            public class DivTest
            {
                public static int RunTests()
                {
                    Vector2 A = new Vector2(2);
                    Vector2 B = new Vector2(3);
                    Vector2 C = A / B;
                    Vector2 D = VectorDiv(A, B);
                    Vector2 E = VectorDiv(ref A, ref B);
                    Vector2 F = VectorDiv(ref A, B);
                    Vector2 G = VectorDiv(A, 3f);
                    Vector2 H = VectorDiv(ref A, 3f);
                    return 0;
                }

                public static Vector2 VectorDiv(Vector2 v1, Vector2 v2)
                {
                    Vector2 v3 = v1 / v2;
                    return v3;
                }

                public static Vector2 VectorDiv(ref Vector2 v1, ref Vector2 v2)
                {
                    Vector2 v3 = v1 / v2;
                    return v3;
                }

                public static Vector2 VectorDiv(ref Vector2 v1, Vector2 v2)
                {
                    Vector2 v3 = v1 / v2;
                    return v3;
                }

                public static Vector2 VectorDiv(Vector2 v1, float v)
                {
                    Vector2 v2 = v1 / v;
                    return v2;
                }

                public static Vector2 VectorDiv(ref Vector2 v1, float v)
                {
                    Vector2 v2 = v1 / v;
                    return v2;
                }
            }
        }

        class FuncEvalTest
        {
            public static void RunTests()
            {
                Vector4 v4a = new Vector4(2f);
                Vector4 v4b = new Vector4(3f);
                Vector3 v3a = new Vector3(2f);
                Vector3 v3b = new Vector3(3f);
                Vector2 v2a = new Vector2(2f);
                Vector2 v2b = new Vector2(3f);    
            }
        }
    }
}
