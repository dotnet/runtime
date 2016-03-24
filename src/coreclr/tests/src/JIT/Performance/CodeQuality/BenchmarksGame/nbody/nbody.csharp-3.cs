// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/* The Computer Language Benchmarks Game
   http://benchmarksgame.alioth.debian.org/

   contributed by Isaac Gouy, optimization and use of more C# idioms by Robert F. Tobler

   modified for use with xunit-performance
*/

using Microsoft.Xunit.Performance;
using System;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public class NBody
{
    public static int Main(String[] args)
    {
        int n = args.Length > 0 ? Int32.Parse(args[0]) : 10000;
        NBodySystem bodies = new NBodySystem();
        double initialEnergy = bodies.Energy();
        Console.WriteLine("{0:f9}", initialEnergy);
        for (int i = 0; i < n; i++) bodies.Advance(0.01);
        double finalEnergy = bodies.Energy();
        Console.WriteLine("{0:f9}", finalEnergy);
        double deltaEnergy = Math.Abs(initialEnergy - finalEnergy);
        bool result = deltaEnergy < 1e-4;
        Console.WriteLine("Energy {0} conserved", result ? "was" : "was not");
        return (result ? 100 : -1);
    }

    [Benchmark]
    public static void Bench()
    {
        int n = 5000000;
        foreach (var iteration in Benchmark.Iterations)
        {
            using (iteration.StartMeasurement())
            {
                NBodySystem bodies = new NBodySystem();
                for (int i = 0; i < n; i++) bodies.Advance(0.01);
            }
        }
    }
}

internal class Body { public double x, y, z, vx, vy, vz, mass; }
internal class Pair { public Body bi, bj; }

internal class NBodySystem
{
    private Body[] _bodies;
    private Pair[] _pairs;

    private const double Pi = 3.141592653589793;
    private const double Solarmass = 4 * Pi * Pi;
    private const double DaysPeryear = 365.24;

    public NBodySystem()
    {
        _bodies = new Body[] {
            new Body() { // Sun
                mass = Solarmass,
            },
            new Body() { // Jupiter
                x = 4.84143144246472090e+00,
                y = -1.16032004402742839e+00,
                z = -1.03622044471123109e-01,
                vx = 1.66007664274403694e-03 * DaysPeryear,
                vy = 7.69901118419740425e-03 * DaysPeryear,
                vz = -6.90460016972063023e-05 * DaysPeryear,
                mass = 9.54791938424326609e-04 * Solarmass,
            },
            new Body() { // Saturn
                x = 8.34336671824457987e+00,
                y = 4.12479856412430479e+00,
                z = -4.03523417114321381e-01,
                vx = -2.76742510726862411e-03 * DaysPeryear,
                vy = 4.99852801234917238e-03 * DaysPeryear,
                vz = 2.30417297573763929e-05 * DaysPeryear,
                mass = 2.85885980666130812e-04 * Solarmass,
            },
            new Body() { // Uranus
                x = 1.28943695621391310e+01,
                y = -1.51111514016986312e+01,
                z = -2.23307578892655734e-01,
                vx = 2.96460137564761618e-03 * DaysPeryear,
                vy = 2.37847173959480950e-03 * DaysPeryear,
                vz = -2.96589568540237556e-05 * DaysPeryear,
                mass = 4.36624404335156298e-05 * Solarmass,
            },
            new Body() { // Neptune
                x = 1.53796971148509165e+01,
                y = -2.59193146099879641e+01,
                z = 1.79258772950371181e-01,
                vx = 2.68067772490389322e-03 * DaysPeryear,
                vy = 1.62824170038242295e-03 * DaysPeryear,
                vz = -9.51592254519715870e-05 * DaysPeryear,
                mass = 5.15138902046611451e-05 * Solarmass,
            },
        };

        _pairs = new Pair[_bodies.Length * (_bodies.Length - 1) / 2];
        int pi = 0;
        for (int i = 0; i < _bodies.Length - 1; i++)
            for (int j = i + 1; j < _bodies.Length; j++)
                _pairs[pi++] = new Pair() { bi = _bodies[i], bj = _bodies[j] };

        double px = 0.0, py = 0.0, pz = 0.0;
        foreach (var b in _bodies)
        {
            px += b.vx * b.mass; py += b.vy * b.mass; pz += b.vz * b.mass;
        }
        var sol = _bodies[0];
        sol.vx = -px / Solarmass; sol.vy = -py / Solarmass; sol.vz = -pz / Solarmass;
    }

    public void Advance(double dt)
    {
        foreach (var p in _pairs)
        {
            Body bi = p.bi, bj = p.bj;
            double dx = bi.x - bj.x, dy = bi.y - bj.y, dz = bi.z - bj.z;
            double d2 = dx * dx + dy * dy + dz * dz;
            double mag = dt / (d2 * Math.Sqrt(d2));
            bi.vx -= dx * bj.mass * mag; bj.vx += dx * bi.mass * mag;
            bi.vy -= dy * bj.mass * mag; bj.vy += dy * bi.mass * mag;
            bi.vz -= dz * bj.mass * mag; bj.vz += dz * bi.mass * mag;
        }
        foreach (var b in _bodies)
        {
            b.x += dt * b.vx; b.y += dt * b.vy; b.z += dt * b.vz;
        }
    }

    public double Energy()
    {
        double e = 0.0;
        for (int i = 0; i < _bodies.Length; i++)
        {
            var bi = _bodies[i];
            e += 0.5 * bi.mass * (bi.vx * bi.vx + bi.vy * bi.vy + bi.vz * bi.vz);
            for (int j = i + 1; j < _bodies.Length; j++)
            {
                var bj = _bodies[j];
                double dx = bi.x - bj.x, dy = bi.y - bj.y, dz = bi.z - bj.z;
                e -= (bi.mass * bj.mass) / Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
        }
        return e;
    }
}

