//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
// Interlocked operations
//

#ifndef __GCENV_INTERLOCKED_H__
#define __GCENV_INTERLOCKED_H__

// Interlocked operations
class Interlocked
{
public:

    // Increment the value of the specified 32-bit variable as an atomic operation.
    // Parameters:
    //  addend - variable to be incremented
    // Return:
    //  The resulting incremented value
    template<typename T>
    static T Increment(T volatile *addend);

    // Decrement the value of the specified 32-bit variable as an atomic operation.
    // Parameters:
    //  addend - variable to be decremented
    // Return:
    //  The resulting decremented value
    template<typename T>
    static T Decrement(T volatile *addend);

    // Perform an atomic AND operation on the specified values values
    // Parameters:
    //  destination - the first operand and the destination
    //  value       - second operand
    template<typename T>
    static void And(T volatile *destination, T value);

    // Perform an atomic OR operation on the specified values values
    // Parameters:
    //  destination - the first operand and the destination
    //  value       - second operand
    template<typename T>
    static void Or(T volatile *destination, T value);

    // Set a 32-bit variable to the specified value as an atomic operation. 
    // Parameters:
    //  destination - value to be exchanged
    //  value       - value to set the destination to
    // Return:
    //  The previous value of the destination
    template<typename T>
    static T Exchange(T volatile *destination, T value);

    // Set a pointer variable to the specified value as an atomic operation.
    // Parameters:
    //  destination - value to be exchanged
    //  value       - value to set the destination to
    // Return:
    //  The previous value of the destination
    template <typename T>
    static T ExchangePointer(T volatile * destination, T value);

    template <typename T>
    static T ExchangePointer(T volatile * destination, std::nullptr_t value);

    // Perform an atomic addition of two 32-bit values and return the original value of the addend.
    // Parameters:
    //  addend - variable to be added to
    //  value  - value to add
    // Return:
    //  The previous value of the addend
    template<typename T>
    static T ExchangeAdd(T volatile *addend, T value);

    // Performs an atomic compare-and-exchange operation on the specified values. 
    // Parameters:
    //  destination - value to be exchanged
    //  exchange    - value to set the destination to
    //  comparand   - value to compare the destination to before setting it to the exchange.
    //                The destination is set only if the destination is equal to the comparand.
    // Return:
    //  The original value of the destination
    template<typename T>
    static T CompareExchange(T volatile *destination, T exchange, T comparand);

    // Performs an atomic compare-and-exchange operation on the specified pointers. 
    // Parameters:
    //  destination - value to be exchanged
    //  exchange    - value to set the destination to
    //  comparand   - value to compare the destination to before setting it to the exchange.
    //                The destination is set only if the destination is equal to the comparand.
    // Return:
    //  The original value of the destination
    template <typename T>
    static T CompareExchangePointer(T volatile *destination, T exchange, T comparand);

    template <typename T>
    static T CompareExchangePointer(T volatile *destination, T exchange, std::nullptr_t comparand);
};

#endif // __GCENV_INTERLOCKED_H__
