//--------------------------------------------------------------------------
//
//		Test case reader and writer
//
//		Declares Interfaces for reading and writing test cases from a file
//		in a format independent fashion
//
//		Copyright (C) Microsoft Corporation, 2003
//
//--------------------------------------------------------------------------

using System;

public interface ITestCaseReader
{
	// Opens the test case store to read
	void OpenTestCaseStore(string storeName);
	// Closes the test case store
	void CloseTestCaseStore();
	// Reads the components of the next test case
	// Returns null if no more tests cases to read
	string[] ReadNextTestCase();
}

public interface ITestCaseWriter
{
	// Opens the test case store to write from the beginning
	void OpenTestCaseStore(string storeName);
	// Opens the test case store to write from the end
	void OpenTestCaseStoreForAppend(string storeName);
	// Closes the test case store
	void CloseTestCaseStore();
	// Writes out a comment
	void WriteComment(string comment);
	// Writes out the components of the next test case
	void WriteNextTestCase(string[] testCaseComponents);
}
