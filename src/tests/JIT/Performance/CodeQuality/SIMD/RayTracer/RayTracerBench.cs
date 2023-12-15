// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Based on the Raytracer example from
// Samples for Parallel Programming with the .NET Framework
// https://code.msdn.microsoft.com/windowsdesktop/Samples-for-Parallel-b4b76364

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Xunit;

namespace SIMD
{
public class RayTracerBench
{
#if DEBUG

    private const int RunningTime = 200;
    private const int Width = 100;
    private const int Height = 100;
    private const int Iterations = 1;
    private const int MaxIterations = 1000;

#else

    private const int RunningTime = 1000;
    private const int Width = 250;
    private const int Height = 250;
    private const int Iterations = 7;
    private const int MaxIterations = 1000;

#endif

    public RayTracerBench()
    {
        _width = Width;
        _height = Height;
        _parallel = false;
        _showThreads = false;
        _freeBuffers = new ObjectPool<int[]>(() => new int[_width * _height]);
    }

    private double _framesPerSecond;
    private bool _parallel;
    private bool _showThreads;
    private int _width, _height;
    private int _degreeOfParallelism = Environment.ProcessorCount;
    private int _frames;
    private CancellationTokenSource _cancellation;
    private ObjectPool<int[]> _freeBuffers;

    private void RenderTest()
    {
        _cancellation = new CancellationTokenSource(RunningTime);
        RenderLoop(MaxIterations);
    }

    private void RenderLoop(int iterations)
    {
        // Create a ray tracer, and create a reference to "sphere2" that we are going to bounce
        var rayTracer = new RayTracer(_width, _height);
        var scene = rayTracer.DefaultScene;
        var sphere2 = (Sphere)scene.Things[0]; // The first item is assumed to be our sphere
        var baseY = sphere2.Radius;
        sphere2.Center.Y = sphere2.Radius;

        // Timing determines how fast the ball bounces as well as diagnostics frames/second info
        var renderingTime = new Stopwatch();
        var totalTime = Stopwatch.StartNew();

        // Keep rendering until the iteration count is hit
        for (_frames = 0; _frames < iterations; _frames++)
        {
            // Or the rendering task has been canceled
            if (_cancellation.IsCancellationRequested)
            {
                break;
            }

            // Get the next buffer
            var rgb = _freeBuffers.GetObject();

            // Determine the new position of the sphere based on the current time elapsed
            double dy2 = 0.8 * Math.Abs(Math.Sin(totalTime.ElapsedMilliseconds * Math.PI / 3000));
            sphere2.Center.Y = (float)(baseY + dy2);

            // Render the scene
            renderingTime.Reset();
            renderingTime.Start();
            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _degreeOfParallelism,
                CancellationToken = _cancellation.Token
            };
            if (!_parallel) rayTracer.RenderSequential(scene, rgb);
            else if (_showThreads) rayTracer.RenderParallelShowingThreads(scene, rgb, options);
            else rayTracer.RenderParallel(scene, rgb, options);
            renderingTime.Stop();

            _framesPerSecond = (1000.0 / renderingTime.ElapsedMilliseconds);
            _freeBuffers.PutObject(rgb);
        }
    }

    public bool Run()
    {
        RenderTest();
        Console.WriteLine("{0} frames, {1} frames/sec",
            _frames,
            _framesPerSecond.ToString("F2"));
        return true;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var r = new RayTracerBench();
        bool result = r.Run();
        return (result ? 100 : -1);
    }
}
}
