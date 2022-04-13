using TestInterface;

namespace TestClass
{
    public class Class : TestInterface.Interface
    {
        public void MainTest()
        {
            TestInterface.Class.Test();
        }
        public void Test()
        {
        }
    }
}