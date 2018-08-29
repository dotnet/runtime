#pragma unmanaged
int NativeFunction()
{
    return 100;
}

#pragma managed
public ref class TestClass
{
public:
    int ManagedEntryPoint()
    {
        return NativeFunction();
    }
};
