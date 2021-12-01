// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: DataTest.h
//

//
// Implement a self-test for the correct detection of when the target holds a
// lock we encounter in the DAC.
//
//*****************************************************************************

#ifndef DATA_TEST_H
#define DATA_TEST_H

// This class is used to test our ability to detect from the RS when the target has taken a lock.
// When the DAC executes a code path that takes a lock, we need to know if the target is holding it.
// If it is, then we assume that the locked data is in an inconsistent state. In that case, we don't
// want to report the data; we just want to throw an exception.
// This functionality in this class lets us take a lock on the LS and then signal the RS to try to
// detect whether the lock is held. The main function in this class is TestDataSafety. It deterministically
// signals the RS at key points to execute a code path that takes a lock and also passes a flag to indicate
// whether the LS actually holds the lock. With this information, we can ascertain that our lock detection
// code is working correctly. Without this special test function, it would be nearly impossible to test this
// in any kind of deterministic way.
//
// The test will run in either debug or retail builds, as long as the environment variable TestDataConsistency
// is turned on. It runs once in code:Debugger::Startup. The RS part of the test is in the cases
// DB_IPCE_TEST_CRST and DB_IPCE_TEST_RWLOCK in code:CordbProcess::RawDispatchEvent.
class DataTest
{
public:
    // constructor
    DataTest():
      m_crst1(CrstDataTest1),
      m_crst2(CrstDataTest2),
      m_rwLock(COOPERATIVE_OR_PREEMPTIVE, LOCK_TYPE_DEFAULT) {};

    // Takes a series of locks in various ways and signals the RS to test the locks at interesting
    // points to ensure we reliably detect when the LS holds a lock.
      void TestDataSafety();
private:
    // Send an event to the RS to signal that it should test to determine if a crst is held.
    // This is for testing purposes only.
    void SendDbgCrstEvent(Crst * pCrst, bool okToTake);

    // Send an event to the RS to signal that it should test to determine if a SimpleRWLock is held.
    // This is for testing purposes only.
    void SendDbgRWLockEvent(SimpleRWLock * pRWLock, bool okToTake);

private:
    // The locks must be data members (rather than locals in TestDataSafety) so we can ensure that
    // they are target instances.
    Crst             m_crst1, m_crst2;  // crsts to be taken for testing
    SimpleRWLock     m_rwLock;          // SimpleRWLock to be taken for testing
};
#endif // DATA_TEST_H
