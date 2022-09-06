// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace ILCompiler
{
    internal static partial class LazyGenericsSupport
    {
        /// <summary>
        /// A weighted directed graph abstraction. For our purposes, we only use two weights, so our "weight" is a boolean: "Flagged" or "Not Flagged".
        ///
        /// The generic type "P" denotes the type that holds the payload data of graph vertices. Its overload of Object.Equals() is used
        /// to determine whether two "P"'s represent the same vertex.
        /// </summary>
        private sealed partial class Graph<P>
        {
            /// <summary>
            /// Adds an edge from "from" to "to". If an edge already exists, the "flagged" value is merged (using boolean OR) into
            /// the existing edge.
            /// </summary>
            public void AddEdge(P from, P to, bool flagged)
            {
                Vertex fromVertex = GetVertex(from);
                Vertex toVertex = GetVertex(to);
                fromVertex.AddEdge(toVertex, flagged);
                return;
            }

            public IEnumerable<P> Vertices
            {
                get
                {
                    return this._vertexMap.Keys.ToArray();
                }
            }
        }
    }
}
