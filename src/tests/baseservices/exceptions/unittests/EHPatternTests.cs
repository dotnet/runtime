// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

//
// infrastructure
//
public class Trace
{
  public Trace(string tag, string expected)
  {
    Console.WriteLine("-----------------------------");
    Console.WriteLine(tag);
    Console.WriteLine("-----------------------------");
    _expected = expected;
  }
      
  public void Write(string str)
  {
    _actual += str;
    // Console.Write(str);
  }

  public void WriteLine(string str)
  {
    _actual += str;
    _actual += Environment.NewLine;

    // Console.WriteLine(str);
  }

  public int Match()
  {
    // Console.WriteLine("");
    Console.Write(_expected);
    if (_actual.Equals(_expected))
    {
      Console.WriteLine(": PASS");
      return 100;
    }
    else
    {
      Console.WriteLine(": FAIL: _actual='" + _actual + "'");
      Console.WriteLine("_expected='" + _expected + "'");
      return 999;
    }
  }

  string _actual;
  string _expected;
}

//
// main
//

public class TestSet
{
    static void CountResults(int testReturnValue, ref int nSuccesses, ref int nFailures)
    {
        if (100 == testReturnValue)
        {
            nSuccesses++;
        }
        else
        {
            nFailures++;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int nSuccesses = 0;
        int nFailures = 0;

        // @TODO: SDM: // CountResults(new StackOverflowInLeafFunction().Run(),      ref nSuccesses, ref nFailures);

        CountResults(new BaseClassTest().Run(),                 ref nSuccesses, ref nFailures);
        CountResults(new TryCatchInFinallyTest().Run(),         ref nSuccesses, ref nFailures);
        CountResults(new RecurseTest().Run(),                   ref nSuccesses, ref nFailures);
        CountResults(new ThrowInFinallyNestedInTryTest().Run(), ref nSuccesses, ref nFailures); // FAIL: needs skip to parent code <TODO> investigate </TODO>
        CountResults(new GoryManagedPresentTest().Run(),        ref nSuccesses, ref nFailures); // FAIL: needs skip to parent code <TODO> investigate </TODO>
        CountResults(new InnerFinallyAndCatchTest().Run(),      ref nSuccesses, ref nFailures);
        CountResults(new InnerFinallyTest().Run(),              ref nSuccesses, ref nFailures);
        CountResults(new ThrowInFinallyTest().Run(),            ref nSuccesses, ref nFailures);
        CountResults(new RecursiveRethrow().Run(),              ref nSuccesses, ref nFailures);
        CountResults(new RecursiveThrowNew().Run(),             ref nSuccesses, ref nFailures);
        CountResults(new PendingTest().Run(),                   ref nSuccesses, ref nFailures);
        CountResults(new CollidedUnwindTest().Run(),            ref nSuccesses, ref nFailures);
        CountResults(new BaadbaadTest().Run(),                  ref nSuccesses, ref nFailures);
        CountResults(new GoryNativePastTest().Run(),            ref nSuccesses, ref nFailures);
        CountResults(new ThrowInCatchTest().Run(),              ref nSuccesses, ref nFailures);
        CountResults(new StrSwitchFinalTest().Run(),            ref nSuccesses, ref nFailures);
        CountResults(new RethrowAndFinallysTest().Run(),        ref nSuccesses, ref nFailures);

        if (0 == nFailures)
        {
            Console.WriteLine("OVERALL PASS: " + nSuccesses + " tests");
            return 100;
        }
        else
        {
            Console.WriteLine("OVERALL FAIL: " + nFailures + " tests failed");
            return 999;
        }
    }
}

//
// tests
//

public class RecursiveRethrow
{
    Trace _trace;

    public int Run()
    {
        _trace = new Trace("RecursiveRethrow", "210C0C1C2");
        
        try
        {
            LoveToRecurse(2);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return _trace.Match();
    }


    void SeparatorMethod(int i)
    {
        LoveToRecurse(i);
    }

    void LoveToRecurse(int i)
    {
        try
        {
            _trace.Write(i.ToString());
            if (0 == i)
            {
                throw new Exception("RecursionIsFun");
            }
            else
            {
                SeparatorMethod(i - 1);
            }
        }
        catch (Exception e)
        {
            _trace.Write("C" + i.ToString());
            Console.WriteLine(e);
            throw e;
        }
    }
}

public class RecursiveThrowNew
{
    Trace _trace;

    public int Run()
    {
        _trace = new Trace("RecursiveThrowNew", "210C0(eX)C1(e0)C2(e1)CM(e2)");
        
        try
        {
            LoveToRecurse(2);
        }
        catch (Exception e)
        {
            _trace.Write("CM(" + e.Message + ")");
            Console.WriteLine(e);
        }

        return _trace.Match();
    }


    void SeparatorMethod(int i)
    {
        LoveToRecurse(i);
    }

    void LoveToRecurse(int i)
    {
        try
        {
            _trace.Write(i.ToString());
            if (0 == i)
            {
                throw new Exception("eX");
            }
            else
            {
                SeparatorMethod(i - 1);
            }
        }
        catch (Exception e)
        {
            _trace.Write("C" + i.ToString() + "(" + e.Message + ")");
            Console.WriteLine(e);
            throw new Exception("e" + i.ToString());
        }
    }
}


public class BaadbaadTest
{
  Trace _trace;

  public int Run()
  {
    _trace = new Trace("BaadbaadTest", "1234");
        
    try
    {
      DoStuff();
    }
    catch(Exception e)
    {
      Console.WriteLine(e);
      _trace.Write("4");
    }
    return _trace.Match();
  }

  void DoStuff()
  {
    try
    {
      try
      {
        try
        {
          throw new Exception();
        }
        catch(Exception e)
        {
          Console.WriteLine(e);
          _trace.Write("1");
          throw;
        }
      }
      catch(Exception e)
      {
        Console.WriteLine(e);
        _trace.Write("2");
        throw;
      }
    }
    catch(Exception e)
    {
      Console.WriteLine(e);
      _trace.Write("3");
      throw;
    }
  }
}


class BaseClassTest
{
  Trace _trace;
  
  void f2()
  {
    throw new FileNotFoundException("1");
  }

  void f1()
  {
    try
    {
      f2();
    }
    catch(FileNotFoundException e)
    {
      Console.WriteLine(e);
      _trace.Write("0" + e.Message);
      throw e;
    }
    catch(IOException e)
    {
      Console.WriteLine(e);
      _trace.Write("!" + e.Message);
      throw e;
    }
    catch(Exception e)
    {
      Console.WriteLine(e);
      _trace.Write("@" + e.Message);
      throw e;
    }
  }

  public int Run() 
  {
      _trace = new Trace("BaseClassTest", "0121");
      
      try
      {
        f1();
      }
      catch(Exception e)
      {
        Console.WriteLine(e);
        _trace.Write("2" + e.Message);
      }

      return _trace.Match();
  }
}



public class CollidedUnwindTest
{
    class ExType1 : Exception
    {
    }
    
    class ExType2 : Exception
    {
    }

    Trace _trace;
    
    public int Run()
    {
        _trace = new Trace("CollidedUnwindTest", "0123456789ABCDE");
        
        try
        {
            _trace.Write("0");
            Foo();
        }
        catch (ExType2 e)
        {
            Console.WriteLine(e);
            _trace.Write("E");
        }

        return _trace.Match();
    }

    void Foo()
    {
        try
        {
            _trace.Write("1");
            FnAAA();
        }
        catch (ExType1 e)
        {
            Console.WriteLine(e);
            _trace.Write(" BAD ");
        }
    }

    void FnAAA()
    {
        try
        {
            _trace.Write("2");
            FnBBB();   
        }
        finally
        {
            _trace.Write("D");
        }
    }

    void FnBBB()
    {
        try
        {
            _trace.Write("3");
            Bar();   
        }
        finally
        {
            _trace.Write("C");
        }
    }

    void Bar()
    {
        try
        {
            _trace.Write("4");
            FnCCC();
        }
        finally
        {
            _trace.Write("B");
            throw new ExType2();
        }
    }

    void FnCCC()
    {
        try
        {
            _trace.Write("5");
            FnDDD();   
        }
        finally
        {
            _trace.Write("A");
        }
    }

    void FnDDD()
    {
        try
        {
            _trace.Write("6");
            Fubar();   
        }
        finally
        {
            _trace.Write("9");
        }
    }

    void Fubar()
    {
        try
        {
            _trace.Write("7");
            throw new ExType1();
        }
        finally
        {
            _trace.Write("8");
        }
    }
}

public class ThrowInFinallyNestedInTryTest 
{
    Trace _trace;
    
    void MiddleMethod() 
    {
        _trace.Write("2");
        try 
        {
            _trace.Write("3");
            try 
            {
                _trace.Write("4");
            } 
            finally 
            {
                _trace.Write("5");
                try 
                {
                    _trace.Write("6");
                    throw new System.ArgumentException();
                } 
                finally 
                {
                    _trace.Write("7");
                }
            }
        } 
        finally 
        {
            _trace.Write("8");
        }
    }

    public int Run()
    {
        _trace = new Trace("ThrowInFinallyNestedInTryTest", "0123456789a");
        
        _trace.Write("0");
        try 
        {
            _trace.Write("1");
            MiddleMethod();
        } 
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("9");
        }
        _trace.Write("a");
        
        return _trace.Match();
    }
}

class ThrowInFinallyTest
{
    Trace _trace;
    
    void Dumb()
    {
        _trace.Write("2");
        try
        {
            _trace.Write("3");
            try 
            {
                _trace.Write("4");
                try 
                {
                    _trace.Write("5");
                    throw new Exception("A");
                } 
                finally
                {
                    _trace.Write("6");
                    throw new Exception("B");
                }
            } 
            finally
            {
                _trace.Write("7");
                throw new Exception("C");
            }
        }
        finally
        {
            _trace.Write("8");
        }
    }

    public int Run() 
    {
        _trace = new Trace("ThrowInFinallyTest", "0123456789Ca");
        
        _trace.Write("0");
        try
        {
            _trace.Write("1");
            Dumb();
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("9");
            _trace.Write(e.Message);
        }
        _trace.Write("a");
        return _trace.Match();
   }
}

class ThrowInCatchTest
{
    Trace _trace;
    
    public int Run()
    {
        _trace = new Trace("ThrowInCatchTest", "0123456");
        _trace.Write("0");
        try 
        {
            _trace.Write("1");
            try 
            {
                _trace.Write("2");
                throw new Exception(".....");
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
                _trace.Write("3");
                throw new Exception("5");
            }
        } 
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("4");
            _trace.Write(e.Message);
        }
        _trace.Write("6");
        return _trace.Match();
    }
}

class RecurseTest
{
    Trace _trace;
    
    void DoTest(int level)
    {
        _trace.Write(level.ToString());
        if (level <= 0)
            return;

        try
        {
            throw new Exception("" + (level - 1));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            _trace.Write(e.Message);
            DoTest(level - 2);
        }
    }

    public int Run()
    {
        int     n = 8;
        string  expected = "";

        // create expected result string
        for (int i = n; i >= 0; i--)
        {
            expected += i.ToString();
        }

        _trace = new Trace("RecurseTest", expected);
        
        DoTest(n);

        return _trace.Match();
    }
}

class PendingTest
{
    Trace _trace;
    
    void f3()
    {
        throw new Exception();
    } 

    void f2()
    {
        try
        {
            _trace.Write("1");
            f3();
        } 
        catch(Exception e) 
        {
            Console.WriteLine(e);
            _trace.Write("2");
            throw;
        }
    }

    void f1()
    {
        try
        {
            _trace.Write("0");
            f2();
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("3");
            throw e;
        }
    }

    public int Run()
    {
        _trace = new Trace("PendingTest", "0123401235");
            
        try
        {
            f1();
        }
        catch(Exception e) 
        {
            Console.WriteLine(e);
            _trace.Write("4");
        }

        try
        {
            f1();
        }
        catch(Exception e) 
        {
            Console.WriteLine(e);
            _trace.Write("5");
        }

        return _trace.Match();
    }
}


class GoryNativePastTest
{
    Trace _trace;
    
    void bar()
    {
        _trace.Write("2");
        throw new Exception("6");
    }

    void foo()
    {
        _trace.Write("1");
        try
        {
            bar();
        }
        finally
        {
            _trace.Write("3");
        }
    }

    public int Run()
    {
        _trace = new Trace("GoryNativePastTest", "0123456");
        
        _trace.Write("0");
        try
        {
            try 
            {
                foo();
            } 
            catch(Exception e)
            {
                Console.WriteLine(e);
                _trace.Write("4");
                throw;
            }
        }
        catch(Exception e)
        {
            _trace.Write("5");
            _trace.Write(e.Message);
        }
        return _trace.Match();
    }
}

class GoryManagedPresentTest
{
    Trace _trace;
    
    void foo(int dummy)
    {
        _trace.Write("1");
        try
        {
            _trace.Write("2");
            try 
            {
                _trace.Write("3");
                if (1234 == dummy)
                {
                    goto MyLabel;
                }
                _trace.Write("....");
            }
            finally
            {
                _trace.Write("4");
            }
        }
        finally
        {
            _trace.Write("5");
            if (1234 == dummy)
            {
                int i = 0;
                int q = 167 / i;
            }
        }

        _trace.Write("****");

    MyLabel:
        _trace.Write("~~~~");
    }

    public int Run()
    {
        _trace = new Trace("GoryManagedPresentTest", "0123456");
        try
        {
            _trace.Write("0");
            foo(1234);
            _trace.Write("%%%%");
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            _trace.Write("6");
        }

        return _trace.Match();
    }
}

class TryCatchInFinallyTest
{
    Trace _trace;
    
    public int Run()
    {
        _trace = new Trace("TryCatchInFinallyTest", "0123456");
        
        _trace.Write("0");
        try
        {
            _trace.Write("1");
        }
        finally
        {
            _trace.Write("2");
            try
            {
                _trace.Write("3");
                throw new InvalidProgramException();
            }
            catch(InvalidProgramException e)
            {
                Console.WriteLine(e);
                _trace.Write("4");
            }
            _trace.Write("5");
        }
        _trace.Write("6");

        return _trace.Match();
    }
}

class StrSwitchFinalTest
{
    Trace _trace;
    static string _expected;
    
    static StrSwitchFinalTest()
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();
        
        // Write expected output to string writer object
        expectedOut.WriteLine("s == one");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("In outer finally\r\n");
        expectedOut.WriteLine("s == two");
        expectedOut.WriteLine("After two");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("In outer finally\r\n");
        expectedOut.WriteLine("s == three");
        expectedOut.WriteLine("After three");
        expectedOut.WriteLine("Ok");
        expectedOut.WriteLine("After after three");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("Caught an exception\r\n");
        expectedOut.WriteLine("Ok\r\n");
        expectedOut.WriteLine("In outer finally\r\n");
        
        expectedOut.WriteLine("In four's finally");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("Caught an exception\r\n");
        
        expectedOut.WriteLine("Ok\r\n");
        
        expectedOut.WriteLine("In outer finally\r\n");
        
        expectedOut.WriteLine("s == five");
        expectedOut.WriteLine("Five's finally 0");
        expectedOut.WriteLine("Five's finally 1");
        expectedOut.WriteLine("Five's finally 2");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("In outer finally\r\n");
        
        expectedOut.WriteLine("Greater than five");
        expectedOut.WriteLine("in six's finally");
        expectedOut.WriteLine("In inner finally");
        expectedOut.WriteLine("In outer finally\r\n");

        _expected = expectedOut.ToString();
    }

    public int Run()
    {
        _trace = new Trace("StrSwitchFinalTest", _expected);
        
        string[] s = {"one", "two", "three", "four", "five", "six"};

        for(int i = 0; i < s.Length; i++) 
        {

        beginloop:
            try 
            {
                try 
                {
                    try 
                    {
                        switch(s[i]) 
                        {
                            case "one":
                                try 
                                {
                                    _trace.WriteLine("s == one");
                                } 
                                catch 
                                {
                                    _trace.WriteLine("Exception at one");
                                }
                                break;
                            case "two":
                                try 
                                {
                                    _trace.WriteLine("s == two");
                                } 
                                finally 
                                {
                                    _trace.WriteLine("After two");
                                }
                                break;
                            case "three":
                                try 
                                {
                                    try 
                                    {
                                        _trace.WriteLine("s == three");
                                    } 
                                    catch(System.Exception e) 
                                    {
                                        _trace.WriteLine(e.ToString());
                                        goto continueloop;
                                    }
                                } 
                                finally 
                                {
                                    _trace.WriteLine("After three");
                                    try 
                                    { 
                                        switch(s[s.Length-1]) 
                                        {
                                            case "six":
                                                _trace.WriteLine("Ok");
                                                _trace.WriteLine(s[s.Length]);
                                                goto label2;
                                            default:
                                                try 
                                                { 
                                                    _trace.WriteLine("Ack");
                                                    goto label;
                                                } 
                                                catch 
                                                {
                                                    _trace.WriteLine("I don't think so ...");
                                                }
                                                break;
                                        }
                                    label:
                                        _trace.WriteLine("Unreached");
                                        throw new Exception();
                                    } 
                                    finally 
                                    {
                                        _trace.WriteLine("After after three");
                                    }
                                label2:
                                    _trace.WriteLine("Unreached");
                        
                                }
                                goto continueloop;

                            case "four":
                                try 
                                {
                                    try 
                                    {
                                        _trace.WriteLine("s == " + s[s.Length]);
                                        try 
                                        {
                                        } 
                                        finally 
                                        {
                                            _trace.WriteLine("Unreached");
                                        }
                                    } 
                                    catch (Exception e) 
                                    {
                                        goto test;
                                    rethrowex:
                                        throw;
                                    test:
                                        if (e is System.ArithmeticException) 
                                        {

                                            try 
                                            {
                                                _trace.WriteLine("unreached ");
                                                goto finishfour;
                                            } 
                                            finally 
                                            {
                                                _trace.WriteLine("also unreached");
                                            }
                                        } 
                                        else 
                                        {
                                            goto rethrowex;
                                        }
                                    }
                                } 
                                finally 
                                {
                                    _trace.WriteLine("In four's finally");
                                }
                                finishfour:
                                    break;
                            case "five":
                                try 
                                {
                                    try 
                                    {
                                        try 
                                        {

                                            _trace.WriteLine("s == five");
                                        } 
                                        finally 
                                        {
                                            _trace.WriteLine("Five's finally 0");
                                        }
                                    } 
                                    catch (Exception) 
                                    {
                                        _trace.WriteLine("Unreached");
                                    } 
                                    finally 
                                    {
                                        _trace.WriteLine("Five's finally 1");
                                    }
                                    break;
                                } 
                                finally 
                                {
                                    _trace.WriteLine("Five's finally 2");
                                }
                            default:
                                try 
                                {
                                    _trace.WriteLine("Greater than five");
                                    goto finish;
                                } 
                                finally 
                                {
                                    _trace.WriteLine("in six's finally");
                        
                                }
                    
                        };
                        continue;
                    } 
                    finally 
                    {
                        _trace.WriteLine("In inner finally");
                    }
                }
                catch (Exception e) 
                {
                    _trace.WriteLine("Caught an exception\r\n");
                                            
                    switch(s[i]) 
                    {
                        case "three":
                            if (e is System.IndexOutOfRangeException) 
                            {
                                _trace.WriteLine("Ok\r\n");
                                i++;
                                goto beginloop;
                            }
                            _trace.WriteLine("Unreached\r\n");
                            break;
                        case "four":
                            if (e is System.IndexOutOfRangeException) 
                            {
                                _trace.WriteLine("Ok\r\n");
                                i++;
                                goto beginloop;
                            }
                            _trace.WriteLine("Unreached\r\n");
                            break;
                        default:
                            _trace.WriteLine("****** Unreached");
                            goto continueloop;
                    }
                    
                }

                _trace.WriteLine("Unreached");
            } 
            finally 
            {
                _trace.WriteLine("In outer finally\r\n");
            }

        continueloop:
            _trace.WriteLine("Continuing");
         
        }
        finish:

        return _trace.Match();
    }
}


public class RethrowAndFinallysTest
{
    Trace _trace;
    
    public int Run()
    {
        _trace = new Trace("RethrowAndFinallysTest", "abcdefF3ED2CB1A[done]");
        try 
        {
            _trace.Write("a");
            try
            {
                _trace.Write("b");
                try 
                {
                    _trace.Write("c");
                    try
                    {
                        _trace.Write("d");
                        try 
                        {
                            _trace.Write("e");
                            try
                            {
                                _trace.Write("f");
                                throw new Exception("ex1");
                            }
                            finally
                            {
                                _trace.Write("F");
                            }
                        }
                        catch(Exception e) 
                        {
                            Console.WriteLine(e);
                            _trace.Write("3");
                            throw;
                        }
                        finally
                        {
                            _trace.Write("E");
                        }
                    }
                    finally
                    {
                        _trace.Write("D");
                    }
                }
                catch(Exception e) 
                {
                    Console.WriteLine(e);
                    _trace.Write("2");
                    throw;
                }
                finally
                {
                    _trace.Write("C");
                }
            }
            finally
            {
                _trace.Write("B");
            }
        }
        catch(Exception e) 
        {
            Console.WriteLine(e);
            _trace.Write("1");
        }
        finally
        {
            _trace.Write("A");
        }

        _trace.Write("[done]");

        return _trace.Match();
    }
}



class InnerFinallyTest
{
    Trace _trace;

    public InnerFinallyTest() 
    {
        // Create test writer object to hold expected output
        System.IO.StringWriter expectedOut = new System.IO.StringWriter();

        // Write expected output to string writer object
        expectedOut.WriteLine(" try 1");
        expectedOut.WriteLine("\t try 1.1");
        expectedOut.WriteLine("\t finally 1.1");
        expectedOut.WriteLine("\t\t try 1.1.1");
        expectedOut.WriteLine("\t\t Throwing an exception here!");
        expectedOut.WriteLine("\t\t finally 1.1.1");
        expectedOut.WriteLine(" catch 1");
        expectedOut.WriteLine(" finally 1");
        
        _trace = new Trace("InnerFinallyTest", expectedOut.ToString());
    }
    
    public int Run() 
    {
        int x = 7, y = 0, z;

        try 
        {
            _trace.WriteLine(" try 1");
            try 
            {
                _trace.WriteLine("\t try 1.1");
            } 
            finally 
            {
                _trace.WriteLine("\t finally 1.1");
                try  
                { 
                    _trace.WriteLine("\t\t try 1.1.1");
                    _trace.WriteLine("\t\t Throwing an exception here!");
                    z = x / y;
                } 
                finally  
                {
                    _trace.WriteLine("\t\t finally 1.1.1");
                }
            }
        } 
        catch (Exception) 
        {
            _trace.WriteLine(" catch 1");
        } 
        finally  
        {
            _trace.WriteLine(" finally 1");
        }

        return _trace.Match();
    }
}


class InnerFinallyAndCatchTest
{
    Trace _trace;

    public int Run() 
    {
        _trace = new Trace("InnerFinallyAndCatchTest", "abcdefghijklm13");

        int x = 7, y = 0, z;

        int count = 0; 

        try 
        {
            _trace.Write("a");
            count++;
            try
            {
                _trace.Write("b");
                count++;
            }
            finally // 1
            {
                try
                {
                    _trace.Write("c");
                    count++;
                }
                finally // 2
                {
                    try
                    {
                        try 
                        {
                            _trace.Write("d");
                            count++;
                        } 
                        finally // 3
                        {
                            _trace.Write("e");
                            count++;
                            try  
                            { 
                                _trace.Write("f");
                                count++;
                            } 
                            finally  // 4
                            {
                                _trace.Write("g");
                                count++;
                                z = x / y;
                            }
                            _trace.Write("@@");
                            count++;
                        }
                    }
                    catch (Exception) // C2
                    {
                        _trace.Write("h");
                        count++;
                    }
                    _trace.Write("i");
                    count++;
                }
                _trace.Write("j");
                count++;
            }
            _trace.Write("k");
            count++;
        } 
        catch (Exception) // C1
        {
            _trace.Write("!!");
            count++;
        } 
        finally  // 0
        {
            _trace.Write("l");
            count++;
        }
        
        _trace.Write("m");
        count++;

        _trace.Write(count.ToString());

        return _trace.Match();
    }
}


class StackOverflowInLeafFunction
{
    Trace _trace;

/*
    int LeafFunction(int a, int b)
    {
        int c;
        
        try
        {
            // raise stack overflow
        }
        catch
        {
            c = b / a;  // this exception will not be able to dispatch
        }

        return c;
    }
*/

    unsafe void RecursiveDeath(int depth)
    {
        string msg    = String.Concat("caught at depth:", depth.ToString());
        long* pStuff = stackalloc long[128]; 

        for (int i = 0; i < 128; i++)
        {
            short d = (short)depth;
            long  dd = (long)d;
            
            long foo  = dd << 48;
                 foo |= dd << 32; 
                 foo |= dd << 16; 
                 foo |= dd; 
                 
            pStuff[i] = foo;
        }

        try
        {
            RecursiveDeath(depth + 1);
        }
        catch
        {
            Console.WriteLine(msg);
        }
        
    }

    public int Run() 
    {
        _trace = new Trace("", "123");

        _trace.Write("1");

        try
        {
            RecursiveDeath(0);
        }
        catch
        {
            _trace.Write("2");
        }

        _trace.Write("3");
        
        return _trace.Match();
    }
}

