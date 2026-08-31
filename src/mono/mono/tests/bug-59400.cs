using System;
using System.Collections;
using System.Collections.Generic;

namespace MonoBug
{
	interface IValue<out TValue>
	{
		TValue Value { get; }
	}

	struct ValueHolder<TValue>
		: IValue<TValue>
	{
		public TValue Value { get; }

		public ValueHolder(TValue value)
		{
			Value = value;
		}
	}

	interface IPair<TKey, out TValue>
	{
		TKey Key { get; }
		TValue Value { get; }
	}

	struct Pair<TKey, TValue>
		: IPair<TKey, TValue>
		where TValue : class
	{
		public TKey Key { get; }
		public TValue Value { get; }

		public Pair(TKey key, TValue value)
		{
			Key = key;
			Value = value;
		}
	}

	struct IncorrectEnumerator1<TValue>
		: IEnumerator<ValueHolder<TValue>>, IEnumerator<IValue<TValue>>
		where TValue : class
	{
		object IEnumerator.Current => null;

		IValue<TValue> IEnumerator<IValue<TValue>>.Current
		{
			get
			{
				Console.WriteLine("IEnumerator<IValue<TValue>>.Current is called (correct)");
				Program.exit_code = 0;
				return new ValueHolder<TValue>(default(TValue));
			}
		}

		ValueHolder<TValue> IEnumerator<ValueHolder<TValue>>.Current
		{
			get
			{
				Console.WriteLine("IEnumerator<ValueHolder<TValue>>.Current is called (incorrect)");
				Program.exit_code = 1;
				return new ValueHolder<TValue>(default(TValue));
			}
		}


		public bool MoveNext() => true;
		public void Reset() { }
		public void Dispose() { }
	}


	class ValueBase
	{ }

	class Value
		: ValueBase
	{ }

	class Program
	{
		internal static int exit_code;
		static int Main(string[] args)
		{
			IEnumerator<IValue<ValueBase>> it1 = new IncorrectEnumerator1<Value>();

			var v1 = it1.Current;
			var v2 = it1.Current;

			return Program.exit_code;
		}
	}
}
