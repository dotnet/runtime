using System;

class Test
{
	public Test ()
	{

	}

	static int Main ()
	{
		TestMatrix<Test> tMat = new TestMatrix<Test> ();
		tMat.setStuff (new Test (), 0, 0);
		return 0;
	}
}

class TestMatrix<T>
{
	private T[,] _matrix;

	public TestMatrix ()
	{
		_matrix = new T[1, 1];
	}

	public void setStuff (T item, int row, int column)
	{
		_matrix[row, column] = item;
	}

}
