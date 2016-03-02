// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Author: JeffSchw
// Date:   2/21/2006
//
//  The SudokuGenerator should produce a valid Sudoku board.
//  Valid Sudoku boards have the following properties:
//   1. Are solvable (ie. have a solution)
//   2. Are square (ie. height and width are the same)
//   3. Dimensions have an integral root (ie. the sqrt of the width is integral)
//   4. The board is broken down into regions, rows, and columns.  Each region/row/column
//      must be able to contian 1-9 with no duplicates.

using System;

public interface ISudokuGenerator<T>
{
    // generate a valid sudoku board.  Allow for varying the difficultiy (0-9)
    T[,] Generate(int dimension, int difficulty, int randSeed);
}
