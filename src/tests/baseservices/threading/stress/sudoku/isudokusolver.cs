// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Author: SneshaA
// Date:   3/03/2006

//Interface for the Solver
//Solver guesses pieces to be placed on the Board

using System;

public interface ISudokuSolver<T>
{
    ISudokuBoard<int> Board {set;} 
    bool Guess{get; set;}	//Sets Solver with Guess token (since only one solver can guess for a given board)
    void Solve(); //Solves given Board
    int Seed{get; set;} //use default, if check for set returns false
}
