// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using Xunit;

public class CollectionBase_OnMethods
{

    public bool runTest()
    {

        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        MyCollectionBase mycol;
        Foo f, newF;
        string expectedExceptionMessage;

        try
        {
            do
            {
                /////////////////////////  START TESTS ////////////////////////////
                ///////////////////////////////////////////////////////////////////

                //[] Vanilla Add
                iCountTestcases++;

                mycol = new MyCollectionBase();
                f = new Foo();
                mycol.Add(f);
                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (!mycol.OnInsertCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_4072ayps OnInsertComplete was never called");
                }
                else if (0 != mycol.IndexOf(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_0181salh Item was added");
                }

                //[] Add where Validate throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnValidateThrow = true;
                expectedExceptionMessage = "OnValidate";
                f = new Foo();
                try
                {
                    mycol.Add(f);
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnInsertCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_56707awwaps OnInsert was called");
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2708apsa Expected Empty Collection actual {0} elements", mycol.Count);
                }

                //[] Add where OnInsert throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnInsertThrow = true;
                expectedExceptionMessage = "OnInsert";
                f = new Foo();
                try
                {
                    mycol.Add(f);
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnInsertCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_370asjhs OnInsertComplete was called");
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_3y07aza Expected Empty Collection actual {0} elements", mycol.Count);
                }

                //[] Add where OnInsertComplete throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnInsertCompleteThrow = true;
                expectedExceptionMessage = "OnInsertComplete";
                f = new Foo();
                try
                {
                    mycol.Add(f);
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_3680ahksd Expected Empty Collection actual {0} elements", mycol.Count);
                }

                /******* Insert ******************************************************************************************/

                //[] Vanilla Insert
                iCountTestcases++;
                mycol = new MyCollectionBase();

                f = new Foo();
                mycol.Insert(0, f);
                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (!mycol.OnInsertCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_270ash OnInsertComplete was never called");
                }
                else if (0 != mycol.IndexOf(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_208anla  Item was added");
                }

                //[] Insert where Validate throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnValidateThrow = true;
                expectedExceptionMessage = "OnValidate";
                f = new Foo();
                try
                {
                    mycol.Insert(0, f);
                    iCountErrors++;
                    Console.WriteLine("Err_270ahsw Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_48708awhoh Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnInsertCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_3708anw OnInsert was called");
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_707j2awn Expected Empty Collection actual {0} elements", mycol.Count);
                }

                //[] Insert where OnInsert throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnInsertThrow = true;
                expectedExceptionMessage = "OnInsert";
                f = new Foo();
                try
                {
                    mycol.Insert(0, f);
                    iCountErrors++;
                    Console.WriteLine("Err_70378sanw Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_7302nna Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnInsertCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_1270jqna OnInsertComplete was called");
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2707nqnq Expected Empty Collection actual {0} elements", mycol.Count);
                }

                //[] Insert where OnInsertComplete throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnInsertCompleteThrow = true;
                expectedExceptionMessage = "OnInsertComplete";
                f = new Foo();
                try
                {
                    mycol.Insert(0, f);
                    iCountErrors++;
                    Console.WriteLine("Err_27087nqlkha Exception was not thrown");
                }
                catch (Exception e)
                {
                    if (expectedExceptionMessage != e.Message)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_270hy1na Exception message was wrong expected '{0}' actual '{1}'", expectedExceptionMessage, e.Message);
                    }
                }

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_270978aw Expected Empty Collection actual {0} elements", mycol.Count);
                }


                /************** Remove ***************************************************************************************************************/

                //[] Vanilla Remove
                iCountTestcases++;
                mycol = new MyCollectionBase();
                f = new Foo();

                mycol.Add(f);
                mycol.Remove(f);
                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (!mycol.OnRemoveCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_348awq OnRemoveComplete was never called");
                }
                else if (-1 != mycol.IndexOf(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_0824answ  Item was not removed");
                }

                //[] Remove where Validate throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                expectedExceptionMessage = "OnValidate";
                f = new Foo();
                try
                {
                    mycol.Add(f);
                    mycol.OnValidateThrow = true;
                    mycol.Remove(f);
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnRemoveCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_7207snmqla OnRemove was called");
                }
                else if (0 != mycol.IndexOf(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_7270pahnz Element was actually removed {0}", mycol.IndexOf(f));
                }

                //[] Remove where OnRemove throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnRemoveThrow = true;
                expectedExceptionMessage = "OnRemove";
                f = new Foo();
                try
                {
                    mycol.Add(f);
                    mycol.Remove(f);
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnRemoveCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_10871aklsj OnRemoveComplete was called");
                }
                else if (0 != mycol.IndexOf(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2081aslkjs Element was actually removed {0}", mycol.IndexOf(f));
                }

                //[] Remove where OnRemoveComplete throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnRemoveCompleteThrow = true;
                expectedExceptionMessage = "OnRemoveComplete";
                f = new Foo();
                try
                {
                    mycol.Add(f);
                    mycol.Remove(f);
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (0 != mycol.IndexOf(f))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_20909assfd Element was actually removed {0}", mycol.IndexOf(f));
                }


                /******************* Clear ************************************************************************************************/

                //[] Vanilla Clear
                iCountTestcases++;
                mycol = new MyCollectionBase();
                f = new Foo();

                mycol.Add(f);
                mycol.Clear();
                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (!mycol.OnClearCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2136sdf OnClearComplete was never called");
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9494aswa  Items were not Cleared");
                }

                //[] Clear where Validate throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                expectedExceptionMessage = "OnValidate";
                f = new Foo();

                mycol.Add(f);
                mycol.OnValidateThrow = true;
                mycol.Clear();

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (!mycol.OnClearCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6549asff OnClearComplete was not called");
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_4649awajm Items were Cleared");
                }

                //[] Clear where OnClear throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnClearThrow = true;
                expectedExceptionMessage = "OnClear";
                f = new Foo();
                try
                {
                    mycol.Add(f);
                    mycol.Clear();
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnClearCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_94944awfuu OnClearComplete was called");
                }
                else if (1 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6498  Items were not Cleared");
                }

                //[] Clear where OnClearComplete throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnClearCompleteThrow = true;
                expectedExceptionMessage = "OnClearComplete";
                f = new Foo();
                try
                {
                    mycol.Add(f);
                    mycol.Clear();
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (0 != mycol.Count)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_1081lkajs  Items were not Cleared");
                }


                /**************************** Set ***********************************************************************************************************************/

                //[] Vanilla Set
                iCountTestcases++;
                mycol = new MyCollectionBase();
                f = new Foo(1, "1");
                newF = new Foo(2, "2");

                mycol.Add(f);
                mycol[0] = newF;
                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (!mycol.OnSetCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_1698asdf OnSetComplete was never called");
                }
                else if (-1 != mycol.IndexOf(f) || 0 != mycol.IndexOf(newF))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9494safs  Item was not Set");
                }

                //[] Set where Validate throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                expectedExceptionMessage = "OnValidate";
                f = new Foo(1, "1");
                newF = new Foo(2, "2");
                try
                {
                    mycol.Add(f);
                    mycol.OnValidateThrow = true;
                    mycol[0] = newF;
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnSetCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_94887jio OnSet was called");
                }
                else if (0 != mycol.IndexOf(f) || -1 != mycol.IndexOf(newF))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6549ihyopiu Element was actually Set {0}", mycol.IndexOf(f));
                }

                //[] Set where OnSet throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnSetThrow = true;
                expectedExceptionMessage = "OnSet";
                f = new Foo(1, "1");
                newF = new Foo(2, "2");
                try
                {
                    mycol.Add(f);
                    mycol[0] = newF;
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (mycol.OnSetCompleteCalled)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_6548dasfs OnSetComplete was called");
                }
                else if (0 != mycol.IndexOf(f) || -1 != mycol.IndexOf(newF))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2797sah Element was actually Set {0}", mycol.IndexOf(f));
                }

                //[] Set where OnSetComplete throws
                iCountTestcases++;
                mycol = new MyCollectionBase();
                mycol.OnSetCompleteThrow = true;
                expectedExceptionMessage = "OnSetComplete";
                f = new Foo(1, "1");
                newF = new Foo(2, "2");
                try
                {
                    mycol.Add(f);
                    mycol[0] = newF;
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

                if (mycol.IsError)
                {
                    iCountErrors++;
                    Console.WriteLine(mycol.ErrorMsg);
                }
                else if (0 != mycol.IndexOf(f) || -1 != mycol.IndexOf(newF))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_64198ahihos Element was actually Set {0}", mycol.IndexOf(f));
                }

                ///////////////////////////////////////////////////////////////////
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
            return false;
        }
    }



    [Fact]
    public static void ExecuteCollectionBase_OnMethods()
    {
        bool bResult = false;
        var test = new CollectionBase_OnMethods();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_main! Uncaught Exception in main(), exc_main==" + exc_main);
        }
        Assert.Equal(true, bResult);
    }


    //CollectionBase is provided to be used as the base class for strongly typed collections. Lets use one of our own here
    public class MyCollectionBase : CollectionBase
    {
        public bool IsError = false;
        public string ErrorMsg = String.Empty;
        public bool OnValidateCalled, OnSetCalled, OnSetCompleteCalled, OnInsertCalled, OnInsertCompleteCalled,
                    OnClearCalled, OnClearCompleteCalled, OnRemoveCalled, OnRemoveCompleteCalled;
        public bool OnValidateThrow, OnSetThrow, OnSetCompleteThrow, OnInsertThrow, OnInsertCompleteThrow,
                OnClearThrow, OnClearCompleteThrow, OnRemoveThrow, OnRemoveCompleteThrow;


        public MyCollectionBase()
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

        public int Add(Foo f1)
        {
            return List.Add(f1);
        }

        public Foo this[int indx]
        {
            get { return (Foo)List[indx]; }
            set { List[indx] = value; }
        }

        public void CopyTo(Array array, Int32 index)
        {
            ((ICollection)List).CopyTo(array, index);
        }

        public Int32 IndexOf(Foo f)
        {
            return ((IList)List).IndexOf(f);
        }

        public Boolean Contains(Foo f)
        {
            return ((IList)List).Contains(f);
        }

        public void Insert(Int32 index, Foo f)
        {
            List.Insert(index, f);
        }

        public void Remove(Foo f)
        {
            List.Remove(f);
        }

        protected override void OnSet(int index, Object oldValue, Object newValue)
        {
            if (!OnValidateCalled)
            {
                IsError = true;
                ErrorMsg += "Err_0882pxnk OnValidate has not been called\n";
            }

            if (this[index] != oldValue)
            {
                IsError = true;
                ErrorMsg += "Err_1204phzn Value was already set\n";
            }

            OnSetCalled = true;

            if (OnSetThrow)
                throw new Exception("OnSet");
        }

        protected override void OnInsert(int index, Object value)
        {
            if (!OnValidateCalled)
            {
                IsError = true;
                ErrorMsg += "Err_0834halkh OnValidate has not been called\n";
            }

            if (index > Count)
            {
                IsError = true;
                ErrorMsg += "Err_7332pghh Value was already set\n";
            }

            if (index != Count && this[index] == value)
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
            if (Count == 0 || InnerList.Count == 0)
            {
                //Assumes Clear not called on an empty list
                IsError = true;
                ErrorMsg += "Err_2247alkhz List already empty\n";
            }

            OnClearCalled = true;

            if (OnClearThrow)
                throw new Exception("OnClear");
        }

        protected override void OnRemove(int index, Object value)
        {
            if (!OnValidateCalled)
            {
                IsError = true;
                ErrorMsg += "Err_3703pyaa OnValidate has not been called\n";
            }

            if (this[index] != value)
            {
                IsError = true;
                ErrorMsg += "Err_1708afsw Value was already set\n";
            }

            OnRemoveCalled = true;

            if (OnRemoveThrow)
                throw new Exception("OnRemove");
        }

        protected override void OnValidate(Object value)
        {
            OnValidateCalled = true;

            if (OnValidateThrow)
                throw new Exception("OnValidate");
        }

        protected override void OnSetComplete(int index, Object oldValue, Object newValue)
        {
            if (!OnSetCalled)
            {
                IsError = true;
                ErrorMsg += "Err_0282pyahn OnSet has not been called\n";
            }

            if (this[index] != newValue)
            {
                IsError = true;
                ErrorMsg += "Err_2134pyqt Value has not been set\n";
            }

            OnSetCompleteCalled = true;

            if (OnSetCompleteThrow)
                throw new Exception("OnSetComplete");
        }

        protected override void OnInsertComplete(int index, Object value)
        {
            if (!OnInsertCalled)
            {
                IsError = true;
                ErrorMsg += "Err_5607aspyu OnInsert has not been called\n";
            }

            if (this[index] != value)
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

            if (Count != 0 || InnerList.Count != 0)
            {
                IsError = true;
                ErrorMsg += "Err_2507yuznb Value has not been set\n";
            }

            OnClearCompleteCalled = true;

            if (OnClearCompleteThrow)
                throw new Exception("OnClearComplete");
        }

        protected override void OnRemoveComplete(int index, Object value)
        {
            if (!OnRemoveCalled)
            {
                IsError = true;
                ErrorMsg += "Err_70782sypz OnRemve has not been called\n";
            }

            if (IndexOf((Foo)value) == index)
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
