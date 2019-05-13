// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Support for dynamic interface casting. Specifically implementing this interface on a type will allow the
    /// type to support interfaces (for the purposes of casting and interface dispatch) that do not appear in its
    /// interface map.
    /// </summary>
    public interface ICastable
    {
        // This is called if casting this object to the given interface type would otherwise fail. Casting
        // here means the IL isinst and castclass instructions in the case where they are given an interface
        // type as the target type.
        //
        // A return value of true indicates the cast is valid.
        //
        // If false is returned when this is called as part of a castclass then the usual InvalidCastException
        // will be thrown unless an alternate exception is assigned to the castError output parameter. This
        // parameter is ignored on successful casts or during the evaluation of an isinst (which returns null
        // rather than throwing on error).
        //
        // No exception should be thrown from this method (it will cause unpredictable effects, including the
        // possibility of an immediate failfast).
        //
        // The results of this call are not cached, so it is advisable to provide a performant implementation.
        //
        // The results of this call should be invariant for the same class, interface type pair. That is
        // because this is the only guard placed before an interface invocation at runtime. If a type decides
        // it no longer wants to implement a given interface it has no way to synchronize with callers that
        // have already cached this relationship and can invoke directly via the interface pointer.
        bool IsInstanceOfInterface(RuntimeTypeHandle interfaceType, out Exception? castError);

        // This is called as part of the interface dispatch mechanism when the dispatcher logic cannot find
        // the given interface type in the interface map of this object.
        //
        // It allows the implementor to return an alternate class type which does implement the interface. The
        // interface lookup shall be performed again on this type (failure to find the interface this time
        // resulting in a fail fast) and the corresponding implemented method on that class called instead.
        //
        // Naturally, since the call is dispatched to a method on a class which does not match the type of the
        // this pointer, extreme care must be taken in the implementation of the interface methods of this
        // surrogate type.
        //
        // No exception should be thrown from this method (it will cause unpredictable effects, including the
        // possibility of an immediate failfast).
        //
        // There is no error path defined here. By construction all interface dispatches will already have
        // been verified via the castclass/isinst mechanism (and thus a call to IsInstanceOfInterface above)
        // so this method is expected to succeed in all cases. The contract for interface dispatch does not
        // include any errors from the infrastructure, of which this is a part.
        //
        // The results of this lookup are cached so computation of the result is not as perf-sensitive as
        // IsInstanceOfInterface.
        RuntimeTypeHandle GetImplType(RuntimeTypeHandle interfaceType);
    }
}
