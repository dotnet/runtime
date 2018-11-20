// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

// This test was extracted from the Indexer tests in the corefx System.Memory.Tests.
// The JIT was trying to expand the form of indexer that takes a range, but was not
// correctly expanding it, as it was expecting only the scalar index form.

public class GitHub_20958
{

    public static int IndexerWithRangeTest()
    {
        int returnVal = 100;
   
        ReadOnlySpan<char> span = "Hello".AsSpan();
        ReadOnlySpan<char> sliced = span[Range.Create(new Index(1, fromEnd: false), new Index(1, fromEnd: true))];
        if (span.Slice(1, 3) != sliced)
        {
            returnVal = -1;
        }
        try
        {
            ReadOnlySpan<char> s = "Hello".AsSpan()[Range.Create(new Index(1, fromEnd: true), new Index(1, fromEnd: false))];
            returnVal = -1;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            returnVal = -1;
        }
        Span<char> span1 = new Span<char>(new char[] { 'H', 'e', 'l', 'l', 'o' });
        Span<char> sliced1 = span1[Range.Create(new Index(2, fromEnd: false), new Index(1, fromEnd: true))];
        if (span1.Slice(2, 2) != sliced1)
        {
            returnVal = -1;
        }
        try
        {
            Span<char> s = new Span<char>(new char[] { 'H', 'i' })[Range.Create(new Index(0, fromEnd: true), new Index(1, fromEnd: false))];
            returnVal = -1;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            returnVal = -1;
        }
        return returnVal;
    }

    public static int Main()
    {
        return IndexerWithRangeTest();
    }
}
