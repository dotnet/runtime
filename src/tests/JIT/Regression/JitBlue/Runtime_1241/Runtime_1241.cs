// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_1241
{

    public struct Vertex
    {
        public Vector3 Position;
        public Vector2 TexCoords;

        public Vertex(Vector3 pos, Vector2 tex)
        {
            Position = pos;
            TexCoords = tex;
        }
    }

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int returnVal = 100;

            // This prints all zeros in the failure case.
            Console.WriteLine("Replacing array element with new struct directly");
            {
                var bug = Bug.Create();
                bug.MutateBroken();

                Console.WriteLine(bug.Vertices[0].Position);
                if ((bug.Vertices[0].Position.X != 1) || (bug.Vertices[0].Position.Y != 1) || (bug.Vertices[0].Position.Z != 1))
                {
                    returnVal = -1;
                }
            }

            // Works
            Console.WriteLine("Replacing array element with new struct, stored in a local variable first");
            {
                var bug = Bug.Create();
                bug.MutateWorks();

                Console.WriteLine(bug.Vertices[0].Position);
                if ((bug.Vertices[0].Position.X != 1) || (bug.Vertices[0].Position.Y != 1) || (bug.Vertices[0].Position.Z != 1))
                {
                    returnVal = -1;
                }
            }

            return returnVal;
        }
    }

    public class Bug
    {
        public static Bug Create()
        {
            return new Bug
            {
                Vertices = Enumerable.Range(1, 100).Select(i => new Vertex(new Vector3(i), Vector2.One)).ToArray()
            };
        }

        public Vertex[] Vertices { get; set; }

        public void MutateBroken()
        {
            for (var i = 0; i < Vertices.Length; i++)
            {
                var vert = Vertices[i];

                Vertices[i] = new Vertex(vert.Position, vert.TexCoords);
            }
        }

        public void MutateWorks()
        {
            for (var i = 0; i < Vertices.Length; i++)
            {
                var vert = Vertices[i];

                var newVert = new Vertex(vert.Position, vert.TexCoords);

                Vertices[i] = newVert;
            }
        }
    }
}
