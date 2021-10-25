// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;

namespace BugInReflection
{
    class Program
    {
        static int Main(string[] args)
        {
            // This tests the ability to load a type when
            // 1. The type implements an interface
            // 2. The interface has more virtual methods on it than the number of virtual methods
            //    on the base type of the type.
            // 3. The base type implements the interface partially (and the partial implementation
            //    has a slot number greater than the number of virtual methods on the base type + its base types)
            // 4. The type does not re-implement the interface methods implemented by the base type.
            //
            // This is permitted in IL, but is a situation which can only be reached with .il code in versions of
            // .NET prior to .NET 5.
            //
            // In .NET 5, this became straightforward to hit with default interface methods.
            //
            // To workaround the bug in .NET 5, simply make the Post class have enough virtual methods to match
            // the number of virtual methods on the ITitle interface.
            new BlogPost();
            return 100;
        }
    }

    public interface ITitle
    {
        // commenting out one or more of these NotMapped properties fixes the problem
        public string Temp1 => "abcd";
        public string Temp2 => "abcd";
        public string Temp3 => "abcd";
        public string Temp4 => "abcd";
        public string Temp5 => "abcd";

        public string Title { get; set; } // commenting out this property also fixes the problem
    }

    public abstract class Post : ITitle // making this non-abstract also fixes the problem
    {
        public string Title { get; set; }
    }
    public class BlogPost : Post { }
}
