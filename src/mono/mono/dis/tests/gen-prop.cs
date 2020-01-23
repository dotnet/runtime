//Property using a generic param
class g<T>
{
	public T abc {
		get { return default (T); }
	}
}
