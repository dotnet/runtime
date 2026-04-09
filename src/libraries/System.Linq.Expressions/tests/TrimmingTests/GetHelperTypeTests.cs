// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

/// <summary>
/// Tests that the System.Linq.Expressions.Interpreter.CallInstruction.GetHelperType
/// method works as expected when used in a trimmed application.
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
        for (int rank = 1; rank < 6; rank++)
        {
            Array arrayObj = Array.CreateInstance(typeof(string), Enumerable.Repeat(1, rank).ToArray());
            arrayObj.SetValue("solitary value", Enumerable.Repeat(0, rank).ToArray());
            ConstantExpression array = Expression.Constant(arrayObj);
            IEnumerable<DefaultExpression> indices = Enumerable.Repeat(Expression.Default(typeof(int)), rank);
            // This code path for the Compile call exercises the method being tested.
            Func<string> func = Expression.Lambda<Func<string>>(
                Expression.ArrayAccess(array, indices)).Compile(preferInterpretation: true);

            if (func() != "solitary value")
            {
                return -1;
            }
        }

        return 100;
    }
}
