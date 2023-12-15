// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Program
{
#if DEBUG

    private const int RunningTime = 200;
    private const int Width = 248;
    private const int Height = 248;
    private const int Iterations = 1;
    private const int MaxIterations = 1000;

#else

    private const int RunningTime = 1000;
    private const int Width = 248;
    private const int Height = 248;
    private const int Iterations = 7;
    private const int MaxIterations = 1000;

#endif

    private double _framesPerSecond;
    private bool _parallel;
    private bool _showThreads;
    private static int _width, _height;
    private int _degreeOfParallelism = Environment.ProcessorCount;
    private int _frames;
    private CancellationTokenSource _cancellation;
    private ObjectPool<int[]> _freeBuffers;

    public Program()
    {
        _width = Width;
        _height = Height;
        _parallel = false;
        _showThreads = false;
        _freeBuffers = new ObjectPool<int[]>(() => new int[_width * 3 * _height]); // Each pixel has 3 fields (RGB)
    }

    [Fact]
    public static unsafe int TestEntryPoint()
    {
        if (Avx2.IsSupported)
        {
            var r = new Program();
            // We can use `RenderTo` to generate a picture in a PPM file for debugging
            // r.RenderTo("./pic.ppm", true);
            bool result = r.Run();
            return (result ? 100 : -1);
        }
        return 100;
    }

    private void RenderTest()
    {
        _cancellation = new CancellationTokenSource(RunningTime);
        RenderLoop(MaxIterations);
    }

    private void RenderBench()
    {
        _cancellation = new CancellationTokenSource();
        RenderLoop(Iterations);
    }

    private unsafe void RenderLoop(int iterations)
    {
        // Create a ray tracer, and create a reference to "sphere2" that we are going to bounce
        var packetTracer = new Packet256Tracer(_width, _height);
        var scene = packetTracer.DefaultScene;
        var sphere2 = (SpherePacket256)scene.Things[0]; // The first item is assumed to be our sphere
        var baseY = sphere2.Radiuses;
        sphere2.Centers.Ys = sphere2.Radiuses;

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
            var rgbBuffer = _freeBuffers.GetObject();

            // Determine the new position of the sphere based on the current time elapsed
            float dy2 = 0.8f * MathF.Abs(MathF.Sin((float)(totalTime.ElapsedMilliseconds * Math.PI / 3000)));
            sphere2.Centers.Ys = Avx.Add(baseY, Vector256.Create(dy2));

            // Render the scene
            renderingTime.Reset();
            renderingTime.Start();
            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _degreeOfParallelism,
                CancellationToken = _cancellation.Token
            };
            fixed (int* ptr = rgbBuffer)
            {
                packetTracer.RenderVectorized(scene, ptr);
            }

            renderingTime.Stop();

            _framesPerSecond = (1000.0 / renderingTime.ElapsedMilliseconds);
            _freeBuffers.PutObject(rgbBuffer);
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

    private unsafe void RenderTo(string fileName, bool writeToFile)
    {
        var packetTracer = new Packet256Tracer(_width, _height);
        var scene = packetTracer.DefaultScene;
        var rgb = new int[_width * 3 * _height];
        Stopwatch stopWatch = new Stopwatch();
        stopWatch.Start();
        fixed (int* ptr = rgb)
        {
            packetTracer.RenderVectorized(scene, ptr);
        }
        stopWatch.Stop();
        TimeSpan ts = stopWatch.Elapsed;
        string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
           ts.Hours, ts.Minutes, ts.Seconds,
           ts.Milliseconds / 10);
        Console.WriteLine("RunTime " + elapsedTime);

        if (writeToFile)
        {
            using (var file = new System.IO.StreamWriter(fileName))
            {
                file.WriteLine("P3");
                file.WriteLine(_width + " " + _height);
                file.WriteLine("255");

                for (int i = 0; i < _height; i++)
                {
                    for (int j = 0; j < _width; j++)
                    {
                        // Each pixel has 3 fields (RGB)
                        int pos = (i * _width + j) * 3;
                        file.Write(rgb[pos] + " " + rgb[pos + 1] + " " + rgb[pos + 2] + " ");
                    }
                    file.WriteLine();
                }
            }

        }
    }
}
