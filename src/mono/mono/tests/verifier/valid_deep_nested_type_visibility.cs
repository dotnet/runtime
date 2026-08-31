using System;


class DeeplyNestedGeneratorUsingSiblingPrivateClass
{
    private class Foo { public string Bar { get; set; } }

    private class Deeply
    {
        private class Nested
        {
            System.Collections.Generic.IEnumerable<Foo> Generator()
            {
                yield return new Foo { Bar = "blah" };
            }
        }
    }

    static void Main(string[] args)
    {
    }
}

