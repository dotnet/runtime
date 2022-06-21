// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.TypeInfos;

namespace System.Reflection.Runtime.Dispensers
{
    //
    // Creates the appropriate Dispenser for a scenario, based on the dispenser policy.
    //
    internal static class DispenserFactory
    {
        //
        // Note: If your K is a valuetype, use CreateDispenserV() instead. Some algorithms will not be available for use.
        //
        public static Dispenser<K, V> CreateDispenser<K, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]V>(DispenserScenario scenario, Func<K, V> factory)
            where K : class, IEquatable<K>
            where V : class
        {
            DispenserAlgorithm algorithm = s_dispenserPolicy.GetAlgorithm(scenario);
            if (algorithm == DispenserAlgorithm.ReuseAsLongAsKeyIsAlive)
                return new DispenserThatReusesAsLongAsKeyIsAlive<K, V>(factory);
            else
                return CreateDispenserV<K, V>(scenario, factory);

            throw new Exception();
        }


        //
        // This is similar to CreateDispenser() except it doesn't constrain the key to be a reference type.
        // As a result, some algorithms will not be available for use.
        //
        public static Dispenser<K, V> CreateDispenserV<K, V>(DispenserScenario scenario, Func<K, V> factory)
            where K : IEquatable<K>
            where V : class
        {
            DispenserAlgorithm algorithm = s_dispenserPolicy.GetAlgorithm(scenario);

            Debug.Assert(algorithm != DispenserAlgorithm.ReuseAsLongAsKeyIsAlive,
                "Use CreateDispenser() if you want to use this algorithm. The key must not be a valuetype.");

            if (algorithm == DispenserAlgorithm.CreateAlways)
                return new DispenserThatAlwaysCreates<K, V>(factory);
            else if (algorithm == DispenserAlgorithm.ReuseAlways)
                return new DispenserThatAlwaysReuses<K, V>(factory);
            else if (algorithm == DispenserAlgorithm.ReuseAsLongAsValueIsAlive)
                return new DispenserThatReusesAsLongAsValueIsAlive<K, V>(factory);

            throw new Exception();
        }


        private static readonly DispenserPolicy s_dispenserPolicy = new DefaultDispenserPolicy();
    }
}
