// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Threading.Tasks;

namespace Algorithms
{
    // This class contains renderers that use Vector (SIMD) floats
    internal class VectorFloatRenderer : FractalRenderer
    {
        private const float limit = 4.0f;

        public VectorFloatRenderer(Action<int, int, int> dp, Func<bool> abortFunc)
            : base(dp, abortFunc)
        {
        }

        // Render the fractal on a single thread using the ComplexFloatVec data type
        // This is the implementation that has the best comments.
        public void RenderSingleThreadedWithADT(float xmin, float xmax, float ymin, float ymax, float step)
        {
            // Initialize a pile of method constant vectors
            Vector<int> vmax_iters = new Vector<int>(max_iters);
            Vector<float> vlimit = new Vector<float>(limit);
            Vector<float> vstep = new Vector<float>(step);
            Vector<float> vxmax = new Vector<float>(xmax);
            Vector<float> vinc = new Vector<float>((float)Vector<float>.Count * step);
            // Use my little helper routine: it's kind of slow, but I find it pleasantly readable.
            // The alternative would be this:
            //    float[] xmins = new float[Vector<float>.Count];
            //    for (int i = 0; i < xmins.Count; i++)
            //        xmins[i] = xmin + step * i;
            //    Vector<float> vxmin = new Vector<float>(xmins);
            // Both allocate some memory, this one just does it in a separate routine :-)
            Vector<float> vxmin = VectorHelper.Create(i => xmin + step * i);

            float y = ymin;
            int yp = 0;
            for (Vector<float> vy = new Vector<float>(ymin);
                 y <= ymax && !Abort;
                 vy += vstep, y += step, yp++)
            {
                int xp = 0;
                for (Vector<float> vx = vxmin;
                     Vector.LessThanOrEqualAny(vx, vxmax); // Vector.{comparison}Any|All return bools, not masks
                     vx += vinc, xp += Vector<int>.Count)
                {
                    ComplexVecFloat num = new ComplexVecFloat(vx, vy);
                    ComplexVecFloat accum = num;

                    Vector<int> viters = Vector<int>.Zero; // Iteration counts start at all zeros
                    Vector<int> increment = Vector<int>.One; // Increment starts out as all ones
                    do
                    {
                        // This is work that can be vectorized
                        accum = accum.square() + num;
                        // Increment the iteration count Only pixels that haven't already hit the
                        // limit will be incremented because the increment variable gets masked below
                        viters += increment;
                        // Create a mask that correspons to the element-wise logical operation
                        // "accum <= limit && iters <= max_iters" Note that the bitwise and is used,
                        // because the Vector.{comparison} operations return masks, not boolean values
                        Vector<int> vCond = Vector.LessThanOrEqual(accum.sqabs(), vlimit) &
                            Vector.LessThanOrEqual(viters, vmax_iters);
                        // increment becomes zero for the elems that have hit the limit because
                        // vCond is a mask of all zeros or ones, based on the results of the
                        // Vector.{comparison} operations
                        increment = increment & vCond;
                        // Keep going until we have no elements that haven't either hit the value
                        // limit or the iteration count
                    } while (increment != Vector<int>.Zero);

                    // This is another little helper I created. It's definitely kind of slow but I
                    // find it pleasantly succinct. It could also be written like this:
                    //
                    // for (int eNum = 0; eNum < Vector<int>.Count; eNum++)
                    //     DrawPixel(xp + eNum, yp, viters[eNum]);
                    //
                    // Neither implementation is particularly fast, because pulling individual elements
                    // is a slow operation for vector types.
                    viters.ForEach((iter, elemNum) => DrawPixel(xp + elemNum, yp, iter));
                }
            }
        }

        // Render the fractal on a single thread using raw Vector<float> data types
        // For a well commented version, go see VectorFloatRenderer.RenderSingleThreadedWithADT
        public void RenderSingleThreadedNoADT(float xmin, float xmax, float ymin, float ymax, float step)
        {
            Vector<int> vmax_iters = new Vector<int>(max_iters);
            Vector<float> vlimit = new Vector<float>(limit);
            Vector<float> vstep = new Vector<float>(step);
            Vector<float> vxmax = new Vector<float>(xmax);
            Vector<float> vinc = new Vector<float>((float)Vector<float>.Count * step);
            Vector<float> vxmin = VectorHelper.Create(i => xmin + step * i);

            float y = ymin;
            int yp = 0;
            for (Vector<float> vy = new Vector<float>(ymin); y <= ymax && !Abort; vy += vstep, y += step, yp++)
            {
                int xp = 0;
                for (Vector<float> vx = vxmin; Vector.LessThanOrEqualAny(vx, vxmax); vx += vinc, xp += Vector<int>.Count)
                {
                    Vector<float> accumx = vx;
                    Vector<float> accumy = vy;

                    Vector<int> viters = Vector<int>.Zero;
                    Vector<int> increment = Vector<int>.One;
                    do
                    {
                        Vector<float> naccumx = accumx * accumx - accumy * accumy;
                        Vector<float> naccumy = accumx * accumy + accumx * accumy;
                        accumx = naccumx + vx;
                        accumy = naccumy + vy;
                        viters += increment;
                        Vector<float> sqabs = accumx * accumx + accumy * accumy;
                        Vector<int> vCond = Vector.LessThanOrEqual(sqabs, vlimit) &
                            Vector.LessThanOrEqual(viters, vmax_iters);
                        increment = increment & vCond;
                    } while (increment != Vector<int>.Zero);

                    viters.ForEach((iter, elemNum) => DrawPixel(xp + elemNum, yp, (int)iter));
                }
            }
        }

        // Render the fractal on multiple threads using raw Vector<float> data types
        // For a well commented version, go see VectorFloatRenderer.RenderSingleThreadedWithADT
        public void RenderMultiThreadedNoADT(float xmin, float xmax, float ymin, float ymax, float step)
        {
            Vector<int> vmax_iters = new Vector<int>(max_iters);
            Vector<float> vlimit = new Vector<float>(limit);
            Vector<float> vstep = new Vector<float>(step);
            Vector<float> vinc = new Vector<float>((float)Vector<float>.Count * step);
            Vector<float> vxmax = new Vector<float>(xmax);
            Vector<float> vxmin = VectorHelper.Create(i => xmin + step * i);

            Parallel.For(0, (int)(((ymax - ymin) / step) + .5f), (yp) =>
            {
                if (Abort)
                    return;

                Vector<float> vy = new Vector<float>(ymin + step * yp);
                int xp = 0;
                for (Vector<float> vx = vxmin; Vector.LessThanOrEqualAny(vx, vxmax); vx += vinc, xp += Vector<int>.Count)
                {
                    Vector<float> accumx = vx;
                    Vector<float> accumy = vy;

                    Vector<int> viters = Vector<int>.Zero;
                    Vector<int> increment = Vector<int>.One;
                    do
                    {
                        Vector<float> naccumx = accumx * accumx - accumy * accumy;
                        Vector<float> XtimesY = accumx * accumy;
                        Vector<float> naccumy = XtimesY + XtimesY;
                        accumx = naccumx + vx;
                        accumy = naccumy + vy;
                        viters += increment;
                        Vector<float> sqabs = accumx * accumx + accumy * accumy;
                        Vector<int> vCond = Vector.LessThanOrEqual(sqabs, vlimit) &
                            Vector.LessThanOrEqual(viters, vmax_iters);
                        increment = increment & vCond;
                    } while (increment != Vector<int>.Zero);

                    viters.ForEach((iter, elemNum) => DrawPixel(xp + elemNum, yp, (int)iter));
                }
            });
        }

        // Render the fractal on multiple threads using the ComplexFloatVec data type
        // For a well commented version, go see VectorFloatRenderer.RenderSingleThreadedWithADT
        public void RenderMultiThreadedWithADT(float xmin, float xmax, float ymin, float ymax, float step)
        {
            Vector<int> vmax_iters = new Vector<int>(max_iters);
            Vector<float> vlimit = new Vector<float>(limit);
            Vector<float> vstep = new Vector<float>(step);
            Vector<float> vinc = new Vector<float>((float)Vector<float>.Count * step);
            Vector<float> vxmax = new Vector<float>(xmax);
            Vector<float> vxmin = VectorHelper.Create(i => xmin + step * i);

            Parallel.For(0, (int)(((ymax - ymin) / step) + .5f), (yp) =>
            {
                if (Abort)
                    return;

                Vector<float> vy = new Vector<float>(ymin + step * yp);
                int xp = 0;
                for (Vector<float> vx = vxmin; Vector.LessThanOrEqualAny(vx, vxmax); vx += vinc, xp += Vector<int>.Count)
                {
                    ComplexVecFloat num = new ComplexVecFloat(vx, vy);
                    ComplexVecFloat accum = num;

                    Vector<int> viters = Vector<int>.Zero;
                    Vector<int> increment = Vector<int>.One;
                    do
                    {
                        accum = accum.square() + num;
                        viters += increment;
                        Vector<int> vCond = Vector.LessThanOrEqual(accum.sqabs(), vlimit) &
                            Vector.LessThanOrEqual(viters, vmax_iters);
                        increment = increment & vCond;
                    } while (increment != Vector<int>.Zero);

                    viters.ForEach((iter, elemNum) => DrawPixel(xp + elemNum, yp, iter));
                }
            });
        }
    }
}
