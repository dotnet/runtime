// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

public class State;
public sealed class MarkerState : State;
public sealed class ExpectedException : Exception;

public abstract class Machine
{
    private readonly Dictionary<Type, State> _states = new();

    public void Change<T>() where T : State
    {
        _ = GetOrCreate<T>();
    }

    private T GetOrCreate<T>() where T : State
    {
        if (!_states.TryGetValue(typeof(T), out State state))
        {
            state = Construct<T>();
            _states[typeof(T)] = state;
        }

        return (T)state;
    }

    protected virtual T Construct<T>() where T : State
    {
        return Activator.CreateInstance<T>();
    }
}

public sealed class DerivedMachine : Machine
{
    protected override T Construct<T>()
    {
        throw new ExpectedException();
    }
}
