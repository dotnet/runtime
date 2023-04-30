// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;

public class Program
{
	public Program()
	{
	}

	static int Main(string[] args)
	{
        Foo currentFoo;

		Bacon defaultBacon = new Bacon(-180, 180, true, false, 300f, 0.1f, 0.1f, "Foo", false);
		currentFoo = new Foo();
        try {
            currentFoo.GetBar().m_Bacon = defaultBacon;
        } catch (NullReferenceException) {
            return 100;
        }
        return 101;
	}
}

public class Foo
{
	private Bar m_Bar;
	public Bar GetBar()
	{
		return m_Bar;
	}
}


public class Bar
{
	public Bacon m_Bacon = new Bacon(-180, 180, true, false, 300f, 0.1f, 0.1f, "Foo", false);
}

public struct Bacon
{
	public float Value;
	public enum FooEnum
	{
		One,
		Two
	};

	public FooEnum m_FooEnum;
	public float m_f1;
	public float m_f2;
	public float m_f3;
	public string m_s1;
	public float m_f8;
	public bool m_bool1;
	public float m_f4;
	public float m_f5;
	public bool m_bool2;
	public FooBar m_FooBar;

	float m_f6;
	float m_f7;
	int m_i1;

	public bool bool3 { get; set; }

	public bool bool4 { get; set; }

	public interface IFooInterface
	{
		float GetFooValue(int foo);
	}

	IFooInterface m_FooProvider;
	int m_i2;

	public Bacon(
		float minValue, float maxValue, bool wrap, bool rangeLocked,
		float maxSpeed, float accelTime, float decelTime,
		string name, bool invert)
	{
		m_f4 = minValue;
		m_f5 = maxValue;
		m_bool2 = wrap;
		bool3 = rangeLocked;

		bool4 = false;
		m_FooBar = new FooBar(false, 1, 2);

		m_FooEnum = FooEnum.One;
		m_f1 = maxSpeed;
		m_f2 = accelTime;
		m_f3 = decelTime;
		Value = (minValue + maxValue) / 2;
		m_s1 = name;
		m_f8 = 0;
		m_bool1 = invert;

		m_f6 = 0f;
		m_FooProvider = null;
		m_i2 = 0;
		m_f7 = 0;
		m_i1 = 0;
	}

	public struct FooBar
	{
		public bool m_FooBar_bool1;
		public float m_FooBar_f1;
		public float m_FooBar_f2;

		float m_FooBar_f3;
		float m_FooBar_f4;
		float m_FooBar_f5;
		int m_FooBar_i1;
		int m_FooBar_i2;

		public FooBar(bool b1, float f1, float f2)
		{
			m_FooBar_bool1 = b1;
			m_FooBar_f1 = f1;
			m_FooBar_f2 = f2;
			m_FooBar_f4 = 0;
			m_FooBar_f5 = 0;
			m_FooBar_i1 = m_FooBar_i2 = -1;
			m_FooBar_f3 = 0;
		}
	}
}
