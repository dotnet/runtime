int process_add(int a, int b)
{
    return a + b;
}

float process_add2(float a, float b)
{
    return a + b;
}

float process_both(int a, float b)
{
    return (float)process_add(a, (int)b) + process_add2((float)a, b);
}
