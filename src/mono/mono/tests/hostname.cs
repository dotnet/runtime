using System;
using System.Net;

public class A : MarshalByRefObject
{
 public void test()
 {
  Dns.GetHostByName("localhost");
 }
 public static void Main()
 {
  Console.WriteLine("aaa");
  Dns.GetHostByName("localhost");
  Console.WriteLine("bbb");
  AppDomain domain = AppDomain.CreateDomain("aaa");
  A a = (A)domain.CreateInstanceAndUnwrap(typeof(A).Assembly.FullName,
typeof(A).FullName);
  a.test();
 }
}

