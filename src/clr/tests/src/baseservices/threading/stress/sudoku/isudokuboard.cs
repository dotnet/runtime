// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Author: JeffSchw
// Date:   2/21/2006

//Interface for the Board
//The Board returns pieces currently held, including the pieces that have been guessed so far. 

using System;
using System.Collections;

public interface ISudokuBoard<T>

{
    int Dimension{get;} //Returns the Board dimension (since its always sq., only one value is returned)	                    
    IEnumerable Tracer{get;}  //Trace functionality for debugging
    bool PopulateBoard(T[,] rawData);  //Populates the Board with generated puzzle                     
    T[]  GetRow(int xcoord); //Returns the Row values for given co-ords
    T[]  GetCol(int ycoord); //Returns the Column values for given co-ords
    T[]  GetShortRegion(int xcoord, int ycoord); //Returns the 3x3 matrix for given co-ords
    bool SetValue(int xcoord, int ycoord, T value); //Sets Value for a given set of co-ordinates
    T    GetValue(int xcoord, int ycoord); //Returns Value for a given set of co-ordinates
    bool Clear(); //clears only user pieces
}
