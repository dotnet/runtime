
namespace Melanzana;

public static class MathHelpers
{
    public static uint Log2(uint value)
    {
        uint result = 0;
        while (value > 1)
        {
            value >>= 1;
            result++;
        }
        return result;
    }
}
