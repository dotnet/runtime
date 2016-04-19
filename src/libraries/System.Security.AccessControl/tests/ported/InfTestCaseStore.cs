//--------------------------------------------------------------------------
//
//		INF test case reader and writer
//
//		Implements the Interfaces for reading and writing test cases from a file
//		for the simplified INF file format
//
//		Copyright (C) Microsoft Corporation, 2003
//
//
//--------------------------------------------------------------------------

using System;
using System.Collections;
using System.IO;


public class InfTestCaseStore : ITestCaseReader, ITestCaseWriter
{
	public InfTestCaseStore()
	{
		_reader = null;
		_writer = null;
		_currentLine = null;
	}

	~InfTestCaseStore()
	{
		if (null != _reader) _reader.Dispose();
		if (null != _writer) _writer.Dispose();
	}

		
	void ITestCaseReader.OpenTestCaseStore(string storeName)
	{
		if (null != _reader) _reader.Dispose();
		_reader = new StreamReader(new FileStream(storeName, FileMode.Open, FileAccess.Read, FileShare.Read));
	}

	void ITestCaseReader.CloseTestCaseStore()
	{
		if (null != _reader) _reader.Dispose();
	}

	string[] ITestCaseReader.ReadNextTestCase()
	{
		// Find the start of the first test case in the file
		while (TestCaseIdentifierTag != _currentLine)
		{
			_currentLine = _reader.ReadLine();
			if (null == _currentLine)
				return null;
		}

		ArrayList testCase = new ArrayList();
		while (true)
		{
			_currentLine = _reader.ReadLine();
			if ((null == _currentLine) || (TestCaseIdentifierTag == _currentLine))
				break;
			// Skip the empty lines and comments
			if (("" != _currentLine) && (';' != _currentLine[0]))
				testCase.Add(_currentLine);
		}

		string[] testCaseArray = (string[]) testCase.ToArray(typeof(string));
		return testCaseArray;
	}

		
	void ITestCaseWriter.OpenTestCaseStore(string storeName)
	{
		if (null != _writer) _writer.Dispose();
		_writer = new StreamWriter(new FileStream(storeName, FileMode.Create, FileAccess.Write, FileShare.Read));
	}

	void ITestCaseWriter.OpenTestCaseStoreForAppend(string storeName)
	{
		if (null != _writer) _writer.Dispose();
		_writer = new StreamWriter(new FileStream(storeName, FileMode.Append, FileAccess.Write, FileShare.Read));
	}

	void ITestCaseWriter.CloseTestCaseStore()
	{
		if (null != _writer) _writer.Dispose();
	}

	void ITestCaseWriter.WriteComment(string comment)
	{
		_writer.WriteLine();
		_writer.WriteLine(";" + comment);
	}

	void ITestCaseWriter.WriteNextTestCase(string[] testCaseComponents)
	{
		_writer.WriteLine();
		_writer.WriteLine(TestCaseIdentifierTag);

		_writer.WriteLine();
		foreach (string testCaseComponent in testCaseComponents)
			_writer.WriteLine(testCaseComponent);
	}


	private StreamReader _reader;
	private StreamWriter _writer;
	private string _currentLine;
		
	private const string TestCaseIdentifierTag = "[TestCase]";
	
	#if (Test_InfTestCaseStore)
	// Test Stub
	[STAThread]
	public static void Main(string[] args)
	{
		string[] testCase1 = {"TestCase1Param1", "TestCase1Param2", "TestCase1Param3"};
		string[] testCase2 = {"TestCase2Param1", "TestCase2Param2"};
		string[] testCase3 = {"TestCase3Param1", "TestCase3Param2", "", "TestCase3Param4", "TestCase3Param5"};
		string[] testCaseRead = null;

		ITestCaseWriter writer = new InfTestCaseStore();

		writer.OpenTestCaseStore("foo.inf");
		writer.WriteComment("Comment 1");
		writer.WriteComment("Comment 2");
		writer.WriteNextTestCase(testCase1);
		writer.WriteNextTestCase(testCase2);
		writer.WriteComment("Comment 3");
		writer.WriteNextTestCase(testCase3);
		writer.CloseTestCaseStore();

		writer.OpenTestCaseStoreForAppend("foo.inf");
		writer.WriteComment("Comment 4");
		writer.WriteNextTestCase(testCase1);
		writer.WriteComment("Comment 5");
		writer.WriteNextTestCase(testCase2);
		writer.WriteComment("Comment 6");
		writer.WriteNextTestCase(testCase3);
		writer.CloseTestCaseStore();

		ITestCaseReader reader = new InfTestCaseStore();

		reader.OpenTestCaseStore("foo.inf");
		while (null != (testCaseRead = reader.ReadNextTestCase()))
		{
			Console.WriteLine();
			Console.WriteLine("---- TestCase ----");
			foreach (string testCaseComponent in testCaseRead)
				Console.WriteLine(testCaseComponent);
		}
		reader.CloseTestCaseStore();
	}
	#endif
}
