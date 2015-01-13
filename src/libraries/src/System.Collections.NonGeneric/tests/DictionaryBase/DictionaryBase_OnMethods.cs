// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using Xunit;

public class DictionaryBase_OnMethods
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        MyDictionaryBase myDict;
        Foo f, newF;
        string expectedExceptionMessage;

        try
        {
            do
            {
                /////////////////////////  START TESTS ////////////////////////////
                ///////////////////////////////////////////////////////////////////

                /***********************************************************************************************************************
				Add
				***********************************************************************************************************************/

                //[] Vanilla Add
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                f = new Foo();
                myDict.Add(f, 0.ToString());
                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.OnInsertCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_4072ayps OnInsertComplete was never called");
                }
                else if (!myDict.Contains(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_0181salh Item was added");
                }

                //[] Add where Validate throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnValidateThrow = true;
                expectedExceptionMessage = "OnValidate";
                f = new Foo();
                try
                {
                    myDict.Add(f, 0.ToString());
                    iCountErrors++;
                    Console.WriteLine("Err_1570pyqa Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2708apsa Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnInsertCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_56707awwaps OnInsert was called");
                }
                else if (0 != myDict.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2708apsa Expected Empty Collection actual {0} elements", myDict.Count);
                }

                //[] Add where OnInsert throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnInsertThrow = true;
                expectedExceptionMessage = "OnInsert";
                f = new Foo();
                try
                {
                    myDict.Add(f, 0.ToString());
                    iCountErrors++;
                    Console.WriteLine("Err_35208asdo Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_30437sfdah Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnInsertCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_370asjhs OnInsertComplete was called");
                }
                else if (0 != myDict.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_3y07aza Expected Empty Collection actual {0} elements", myDict.Count);
                }

                //[] Add where OnInsertComplete throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnInsertCompleteThrow = true;
                expectedExceptionMessage = "OnInsertComplete";
                f = new Foo();
                try
                {
                    myDict.Add(f, 0.ToString());
                    iCountErrors++;
                    Console.WriteLine("Err_2548ashz Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2708alkw Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (0 != myDict.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_3680ahksd Expected Empty Collection actual {0} elements", myDict.Count);
                }

                /***********************************************************************************************************************
				Remove
				***********************************************************************************************************************/

                //[] Vanilla Remove
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                f = new Foo();

                myDict.Add(f, 0.ToString());
                myDict.Remove(f);
                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.OnRemoveCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_348awq OnRemoveComplete was never called");
                }
                else if (myDict.Contains(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_0824answ  Item was not removed");
                }

                //[] Remove where Validate throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                expectedExceptionMessage = "OnValidate";
                f = new Foo();
                try
                {
                    myDict.Add(f, 0.ToString());
                    myDict.OnValidateThrow = true;
                    myDict.Remove(f);
                    iCountErrors++;
                    Console.WriteLine("Err_08207hnas Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_87082aspz Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnRemoveCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_7207snmqla OnRemove was called");
                }
                else if (!myDict.Contains(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_7270pahnz Element was actually removed {0}", myDict.Contains(f));
                }

                //[] Remove where OnRemove throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnRemoveThrow = true;
                expectedExceptionMessage = "OnRemove";
                f = new Foo();
                try
                {
                    myDict.Add(f, 0.ToString());
                    myDict.Remove(f);
                    iCountErrors++;
                    Console.WriteLine("Err_7708aqyy Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_08281naws Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnRemoveCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_10871aklsj OnRemoveComplete was called");
                }
                else if (!myDict.Contains(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2081aslkjs Element was actually removed {0}", myDict.Contains(f));
                }

                //[] Remove where OnRemoveComplete throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnRemoveCompleteThrow = true;
                expectedExceptionMessage = "OnRemoveComplete";
                f = new Foo();
                try
                {
                    myDict.Add(f, 0.ToString());
                    myDict.Remove(f);
                    iCountErrors++;
                    Console.WriteLine("Err_10981nlskj Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_20981askjs Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.Contains(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_20909assfd Element was actually removed {0}", myDict.Contains(f));
                }

                /***********************************************************************************************************************
				Clear
				***********************************************************************************************************************/

                //[] Vanilla Clear
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                f = new Foo();

                myDict.Add(f, 0.ToString());
                myDict.Clear();
                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.OnClearCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2136sdf OnClearComplete was never called");
                }
                else if (0 != myDict.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9494aswa  Items were not Cleared");
                }

                //[] Clear where Validate throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                expectedExceptionMessage = "OnValidate";
                f = new Foo();

                myDict.Add(f, 0.ToString());
                myDict.OnValidateThrow = true;
                myDict.Clear();

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.OnClearCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6549asff OnClearComplete was not called");
                }
                else if (0 != myDict.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_4649awajm Items were Cleared");
                }

                //[] Clear where OnClear throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnClearThrow = true;
                expectedExceptionMessage = "OnClear";
                f = new Foo();
                try
                {
                    myDict.Add(f, 0.ToString());
                    myDict.Clear();
                    iCountErrors++;
                    Console.WriteLine("Err_9444sjpjnException was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_78941joaException message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnClearCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_94944awfuu OnClearComplete was called");
                }
                else if (1 != myDict.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6498  Items were not Cleared");
                }

                //[] Clear where OnClearComplete throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnClearCompleteThrow = true;
                expectedExceptionMessage = "OnClearComplete";
                f = new Foo();
                try
                {
                    myDict.Add(f, 0.ToString());
                    myDict.Clear();
                    iCountErrors++;
                    Console.WriteLine("Err_64977awad Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_0877asaw Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (0 != myDict.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_1081lkajs  Items were not Cleared");
                }


                /***********************************************************************************************************************
				Set that adds an item
				***********************************************************************************************************************/

                //[] Vanilla Set
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                f = new Foo(1, "1");
                newF = new Foo(2, "2");

                myDict.Add(f, 1.ToString());
                myDict[newF] = 2.ToString();
                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.OnSetCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_1698asdf OnSetComplete was never called");
                }
                else if (!myDict.Contains(f) || !myDict.Contains(newF))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9494safs  Item was not Set");
                }

                //[] Set where Validate throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                expectedExceptionMessage = "OnValidate";
                f = new Foo(1, "1");
                newF = new Foo(2, "2");
                try
                {
                    myDict.Add(f, 1.ToString());
                    myDict.OnValidateThrow = true;
                    myDict[newF] = 2.ToString();
                    iCountErrors++;
                    Console.WriteLine("Err_611awwa Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_69498hph Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnSetCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_94887jio OnSet was called");
                }
                else if (!myDict.Contains(f) || myDict.Contains(newF))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6549ihyopiu Element was actually Set {0}", myDict.Contains(f));
                }

                //[] Set where OnSet throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnSetThrow = true;
                expectedExceptionMessage = "OnSet";
                f = new Foo(1, "1");
                newF = new Foo(2, "2");
                try
                {
                    myDict.Add(f, 1.ToString());
                    myDict[newF] = 2.ToString();
                    iCountErrors++;
                    Console.WriteLine("Err_61518haiosd Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_196198sdklhsException message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnSetCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6548dasfs OnSetComplete was called");
                }
                else if (!myDict.Contains(f) || myDict.Contains(newF))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2797sah Element was actually Set {0}", myDict.Contains(f));
                }

                //[] Set where OnSetComplete throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnSetCompleteThrow = true;
                expectedExceptionMessage = "OnSetComplete";
                f = new Foo(1, "1");
                newF = new Foo(2, "2");
                try
                {
                    myDict.Add(f, 1.ToString());
                    myDict[newF] = 2.ToString();
                    iCountErrors++;
                    Console.WriteLine("Err_56498hkashException was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_13168hsaiuh Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.Contains(f) || myDict.Contains(newF))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_64198ahihos Element was actually Set {0}", myDict.Contains(f));
                }

                /***********************************************************************************************************************
				Set that sets an existing item
				***********************************************************************************************************************/

                //[] Vanilla Set
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                f = new Foo(1, "1");

                myDict.Add(f, 1.ToString());
                myDict[f] = 2.ToString();
                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.OnSetCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_1698asdf OnSetComplete was never called");
                }
                else if (!myDict.Contains(f) || myDict[f] != "2")
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9494safs  Item was not Set");
                }

                //[] Set where Validate throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                expectedExceptionMessage = "OnValidate";
                f = new Foo(1, "1");
                try
                {
                    myDict.Add(f, 1.ToString());
                    myDict.OnValidateThrow = true;
                    myDict[f] = 2.ToString();
                    iCountErrors++;
                    Console.WriteLine("Err_611awwa Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_69498hph Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnSetCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_94887jio OnSet was called");
                }
                else if (!myDict.Contains(f) || myDict[f] != "1")
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6549ihyopiu Element was actually Set {0}", myDict.Contains(f));
                }

                //[] Set where OnSet throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnSetThrow = true;
                expectedExceptionMessage = "OnSet";
                f = new Foo(1, "1");

                try
                {
                    myDict.Add(f, 1.ToString());
                    myDict[f] = 2.ToString();
                    iCountErrors++;
                    Console.WriteLine("Err_61518haiosd Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_196198sdklhsException message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (myDict.OnSetCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6548dasfs OnSetComplete was called");
                }
                else if (!myDict.Contains(f) || myDict[f] != "1")
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2797sah Element was actually Set {0}", myDict.Contains(f));
                }

                //[] Set where OnSetComplete throws
                iCountTestcases++;
                myDict = new MyDictionaryBase();
                myDict.OnSetCompleteThrow = true;
                expectedExceptionMessage = "OnSetComplete";
                f = new Foo(1, "1");

                try
                {
                    myDict.Add(f, 1.ToString());
                    myDict[f] = 2.ToString();
                    iCountErrors++;
                    Console.WriteLine("Err_56498hkashException was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_13168hsaiuh Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (myDict.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(myDict.ErrorMsg);
                }
                else if (!myDict.Contains(f) || myDict[f] != "1")
                {
                    iCountErrors++;
                    Console.WriteLine("Err_64198ahihos Element was actually Set {0}", myDict.Contains(f));
                }
                /////////////////////////// END TESTS /////////////////////////////


            } while (false);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy! exc_general==\n" + exc_general.ToString());
        }


        ////  Finish Diagnostics
        if (iCountErrors == 0)
        {
            return true;
        }
        else
        {
            Console.WriteLine("Fail iCountErrors==" + iCountErrors);
            return false;
        }
    }


    [Fact]
    public static void ExecuteDictionaryBase_OnMethods()
    {
        bool bResult = false;
        var cbA = new DictionaryBase_OnMethods();

        try
        {
            bResult = cbA.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine(" : FAiL! Error Err_9999zzz! Uncaught Exception in main(), exc_main==" + exc_main);
        }

        Assert.Equal(true, bResult);
    }

    //DictionaryBase is provided to be used as the base class for strongly typed collections. Lets use one of our own here
    public class MyDictionaryBase : DictionaryBase
    {
        public bool IsError = false;
        public string ErrorMsg = String.Empty;
        public bool OnValidateCalled, OnSetCalled, OnSetCompleteCalled, OnInsertCalled, OnInsertCompleteCalled,
                    OnClearCalled, OnClearCompleteCalled, OnRemoveCalled, OnRemoveCompleteCalled;
        public bool OnValidateThrow, OnSetThrow, OnSetCompleteThrow, OnInsertThrow, OnInsertCompleteThrow,
                OnClearThrow, OnClearCompleteThrow, OnRemoveThrow, OnRemoveCompleteThrow;


        public MyDictionaryBase()
        {
            OnValidateCalled = false;
            OnSetCalled = false;
            OnSetCompleteCalled = false;
            OnInsertCalled = false;
            OnInsertCompleteCalled = false;
            OnClearCalled = false;
            OnClearCompleteCalled = false;
            OnRemoveCalled = false;
            OnRemoveCompleteCalled = false;

            OnValidateThrow = false;
            OnSetThrow = false;
            OnSetCompleteThrow = false;
            OnInsertThrow = false;
            OnInsertCompleteThrow = false;
            OnClearThrow = false;
            OnClearCompleteThrow = false;
            OnRemoveThrow = false;
            OnRemoveCompleteThrow = false;
        }

        public void Add(Foo key, string value)
        {
            Dictionary.Add(key, value);
        }

        public string this[Foo index]
        {
            get
            {
                return (string)Dictionary[index];
            }
            set
            {
                Dictionary[index] = value;
            }
        }

        public Boolean Contains(Foo f)
        {
            return Dictionary.Contains(f);
        }

        public void Remove(Foo f)
        {
            Dictionary.Remove(f);
        }

        protected override void OnSet(Object key, Object oldValue, Object newValue)
        {
            if (!OnValidateCalled)
            {
                IsError = true;
                ErrorMsg += "Err_0882pxnk OnValidate has not been called\n";
            }

            if (this[(Foo)key] != (string)oldValue)
            {
                IsError = true;
                ErrorMsg += "Err_1204phzn Value was already set\n";
            }

            OnSetCalled = true;

            if (OnSetThrow)
                throw new Exception("OnSet");
        }

        protected override void OnInsert(Object key, Object value)
        {
            if (!OnValidateCalled)
            {
                IsError = true;
                ErrorMsg += "Err_0834halkh OnValidate has not been called\n";
            }

            if (this[(Foo)key] == (string)value)
            {
                IsError = true;
                ErrorMsg += "Err_2702apqh OnInsert called with a bad index\n";
            }

            OnInsertCalled = true;

            if (OnInsertThrow)
                throw new Exception("OnInsert");
        }

        protected override void OnClear()
        {
            if (Count == 0 || Dictionary.Count == 0)
            {
                //Assumes Clear not called on an empty list
                IsError = true;
                ErrorMsg += "Err_2247alkhz List already empty\n";
            }

            OnClearCalled = true;

            if (OnClearThrow)
                throw new Exception("OnClear");
        }

        protected override void OnRemove(Object key, Object value)
        {
            if (!OnValidateCalled)
            {
                IsError = true;
                ErrorMsg += "Err_3703pyaa OnValidate has not been called\n";
            }

            if (this[(Foo)key] != (string)value)
            {
                IsError = true;
                ErrorMsg += "Err_1708afsw Value was already set\n";
            }

            OnRemoveCalled = true;

            if (OnRemoveThrow)
                throw new Exception("OnRemove");
        }

        protected override void OnValidate(Object key, Object value)
        {
            OnValidateCalled = true;

            if (OnValidateThrow)
                throw new Exception("OnValidate");
        }

        protected override void OnSetComplete(Object key, Object oldValue, Object newValue)
        {
            if (!OnSetCalled)
            {
                IsError = true;
                ErrorMsg += "Err_0282pyahn OnSet has not been called\n";
            }

            if (this[(Foo)key] != (string)newValue)
            {
                IsError = true;
                ErrorMsg += "Err_2134pyqt Value has not been set\n";
            }

            OnSetCompleteCalled = true;

            if (OnSetCompleteThrow)
                throw new Exception("OnSetComplete");
        }

        protected override void OnInsertComplete(Object key, Object value)
        {
            if (!OnInsertCalled)
            {
                IsError = true;
                ErrorMsg += "Err_5607aspyu OnInsert has not been called\n";
            }

            if (this[(Foo)key] != (string)value)
            {
                IsError = true;
                ErrorMsg += "Err_3407ahpqValue has not been set\n";
            }

            OnInsertCompleteCalled = true;

            if (OnInsertCompleteThrow)
                throw new Exception("OnInsertComplete");
        }

        protected override void OnClearComplete()
        {
            if (!OnClearCalled)
            {
                IsError = true;
                ErrorMsg += "Err_3470sfas OnClear has not been called\n";
            }

            if (Count != 0 || Dictionary.Count != 0)
            {
                IsError = true;
                ErrorMsg += "Err_2507yuznb Value has not been set\n";
            }

            OnClearCompleteCalled = true;

            if (OnClearCompleteThrow)
                throw new Exception("OnClearComplete");
        }

        protected override void OnRemoveComplete(Object key, Object value)
        {
            if (!OnRemoveCalled)
            {
                IsError = true;
                ErrorMsg += "Err_70782sypz OnRemve has not been called\n";
            }

            if (Contains((Foo)key))
            {
                IsError = true;
                ErrorMsg += "Err_3097saq Value still exists\n";
            }

            OnRemoveCompleteCalled = true;

            if (OnRemoveCompleteThrow)
                throw new Exception("OnRemoveComplete");
        }

        public void ClearTest()
        {
            IsError = false;
            ErrorMsg = String.Empty;

            OnValidateCalled = false;
            OnSetCalled = false;
            OnSetCompleteCalled = false;
            OnInsertCalled = false;
            OnInsertCompleteCalled = false;
            OnClearCalled = false;
            OnClearCompleteCalled = false;
            OnRemoveCalled = false;
            OnRemoveCompleteCalled = false;

            OnValidateThrow = false;
            OnSetThrow = false;
            OnSetCompleteThrow = false;
            OnInsertThrow = false;
            OnInsertCompleteThrow = false;
            OnClearThrow = false;
            OnClearCompleteThrow = false;
            OnRemoveThrow = false;
            OnRemoveCompleteThrow = false;
        }
    }

    public class Foo
    {
        private Int32 _iValue;
        private String _strValue;

        public Foo()
        {
        }

        public Foo(Int32 i, String str)
        {
            _iValue = i;
            _strValue = str;
        }

        public Int32 IValue
        {
            get { return _iValue; }
            set { _iValue = value; }
        }

        public String SValue
        {
            get { return _strValue; }
            set { _strValue = value; }
        }

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is Foo))
                return false;
            if ((((Foo)obj).IValue == _iValue) && (((Foo)obj).SValue == _strValue))
                return true;
            return false;
        }

        public override Int32 GetHashCode()
        {
            return _iValue;
        }
    }
}