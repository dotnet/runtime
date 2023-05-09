using System;
using System.Numerics;
using Xunit;

// In this test case, we have a struct S that contains a single Vector2 field (Vector),
// with an implicit conversion from an array of float to S.
// We inline a call to this op_Implicit, and then the constructor that it invokes.
// The op_Implicit RET_EXPR is TYP_LONG, since that's how the value is returned from the method.
// It is then stored to the Vector field which is TYP_SIMD8.
//
// Bug: The JIT had code to deal with this kind of retyping (which must be made explicit by
//      Lowering, as the two types require different register types), when it involves locals
//      (GT_LCL_VAR) but not fields of locals (GT_LCL_FLD).
//
// Perf Issue: What we wind up with is this: (V05 & V07 are both TYP_SIMD8, V01 is the struct type S
//          V05 = *(TYP_SIMD8)numbers
//          V07 = V05
//          LCL_FLD(V01) = LCL_FLD(V07)  // Both re-typed as TYP_LONG
// This generates:
//          vmovsd   xmm0, qword ptr [rax+16]
//          vmovsd   qword ptr[rsp + 28H], xmm0
//          vmovsd   xmm0, qword ptr[rsp + 28H]
//          vmovsd   qword ptr[rsp + 20H], xmm0
//          vmovsd   xmm0, qword ptr[rsp + 20H]
//          vmovsd   qword ptr[rsp + 30H], xmm0
//
// We should be able to elide these excessive copies and unnecessary retyping, producing close to this:
//          vmovsd   xmm0, qword ptr [rax+16]
//          vmovsd   qword ptr[rsp + 30H], xmm0

namespace Repro
{
    public class Program
    {
	    struct S
	    {
	        public Vector2 Vector;
	        public S(float[] numbers)
	        {
		        Vector = new Vector2(numbers[0], numbers[1]);
	        }
	        public static implicit operator S(float[] numbers) => new S(numbers);
	    }
	    [Fact]
	    public static int TestEntryPoint()
	    {
	        S s = new float[] { 1.0f, 2.0f };
	        Console.WriteLine(s.Vector);
            if ((s.Vector.X != 1.0f) || (s.Vector.Y != 2.0f))
            {
                return -1;
            }
            else
            {
                return 100;
            }
	    }
    }
}
