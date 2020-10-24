// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

namespace Algorithms
{
    public abstract class FractalRenderer
    {
        public delegate void Render(float xmin, float xmax, float ymin, float ymax, float step);

        private Func<bool> _abort;
        private Action<int, int, int> _drawPixel;
        protected const int max_iters = 1000; // Make this higher to see more detail when zoomed in (and slow down rendering a lot)

        protected FractalRenderer(Action<int, int, int> draw, Func<bool> checkAbort)
        {
            _drawPixel = draw; _abort = checkAbort;
        }

        protected Action<int, int, int> DrawPixel { get { return _drawPixel; } }

        public bool Abort { get { return _abort(); } }

        public static Render SelectRender(Action<int, int, int> draw, Func<bool> abort, bool useVectorTypes, bool doublePrecision, bool isMultiThreaded, bool useAbstractDataType, bool dontUseIntTypes = true)
        {
            if (useVectorTypes && doublePrecision)
            {
                if (dontUseIntTypes)
                {
                    var r = new VectorDoubleStrictRenderer(draw, abort);
                    if (isMultiThreaded)
                    {
                        if (useAbstractDataType)
                            return r.RenderMultiThreadedWithADT;
                        else // !useAbstractDataType
                            return r.RenderMultiThreadedNoADT;
                    }
                    else // !isMultiThreaded
                    {
                        if (useAbstractDataType)
                            return r.RenderSingleThreadedWithADT;
                        else // !useAbstractDataType
                            return r.RenderSingleThreadedNoADT;
                    }
                }
                else // !dontUseIntTypes
                {
                    var r = new VectorDoubleRenderer(draw, abort);
                    if (isMultiThreaded)
                    {
                        if (useAbstractDataType)
                            return r.RenderMultiThreadedWithADT;
                        else // !useAbstractDataType
                            return r.RenderMultiThreadedNoADT;
                    }
                    else // !isMultiThreaded
                    {
                        if (useAbstractDataType)
                            return r.RenderSingleThreadedWithADT;
                        else // !useAbstractDataType
                            return r.RenderSingleThreadedNoADT;
                    }
                }
            }
            else if (useVectorTypes && !doublePrecision)
            {
                if (dontUseIntTypes)
                {
                    var r = new VectorFloatStrictRenderer(draw, abort);
                    if (isMultiThreaded)
                    {
                        if (useAbstractDataType)
                            return r.RenderMultiThreadedWithADT;
                        else // !useAbstractDataType
                            return r.RenderMultiThreadedNoADT;
                    }
                    else // !isMultiThreaded
                    {
                        if (useAbstractDataType)
                            return r.RenderSingleThreadedWithADT;
                        else // !useAbstractDataType
                            return r.RenderSingleThreadedNoADT;
                    }
                }
                else // !dontUseIntTypes
                {
                    var r = new VectorFloatRenderer(draw, abort);
                    if (isMultiThreaded)
                    {
                        if (useAbstractDataType)
                            return r.RenderMultiThreadedWithADT;
                        else // !useAbstractDataType
                            return r.RenderMultiThreadedNoADT;
                    }
                    else // !isMultiThreaded
                    {
                        if (useAbstractDataType)
                            return r.RenderSingleThreadedWithADT;
                        else // !useAbstractDataType
                            return r.RenderSingleThreadedNoADT;
                    }
                }
            }
            else if (!useVectorTypes && doublePrecision)
            {
                var r = new ScalarDoubleRenderer(draw, abort);
                if (isMultiThreaded)
                {
                    if (useAbstractDataType)
                        return r.RenderMultiThreadedWithADT;
                    else // !useAbstractDataType
                        return r.RenderMultiThreadedNoADT;
                }
                else // !isMultiThreaded
                {
                    if (useAbstractDataType)
                        return r.RenderSingleThreadedWithADT;
                    else // !useAbstractDataType
                        return r.RenderSingleThreadedNoADT;
                }
            }
            else // (!useVectorTypes && !doublePrecision)
            {
                var r = new ScalarFloatRenderer(draw, abort);
                if (isMultiThreaded)
                {
                    if (useAbstractDataType)
                        return r.RenderMultiThreadedWithADT;
                    else // !useAbstractDataType
                        return r.RenderMultiThreadedNoADT;
                }
                else // !isMultiThreaded
                {
                    if (useAbstractDataType)
                        return r.RenderSingleThreadedWithADT;
                    else // !useAbstractDataType
                        return r.RenderSingleThreadedNoADT;
                }
            }
        }
    }
}
