struct MyStruct
{
    unsigned char* str;

    int count;
};

typedef char (*TransformCharDelegate)(char);

int ProcessStructs(MyStruct* input, MyStruct* output, TransformCharDelegate transform)
{
    int acc = 0;
	for(int i = 0; i < input->count; i++)
	{
        auto value1 = input->str[i];
        auto value2 = output->str[i];
        auto value3 = transform(value1) + transform(value2);

        acc += value1 + value2 + value3;
	}
    return acc;
}
