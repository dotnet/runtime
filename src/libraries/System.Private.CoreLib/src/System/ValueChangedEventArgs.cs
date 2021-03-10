using System;


[Serializeable]
public class ValueChangedEventArgs<T> : EventArgs
{
    public T OldValue { get; }

    public T NewValue { get; }

    public ValueChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}

public delegate void OnValueChanged(object sender, ValueChangedEventArgs e);
