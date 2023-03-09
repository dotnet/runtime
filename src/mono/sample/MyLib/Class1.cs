namespace MyLib;


public class Class1
{
    public void getCat() {
        Animal a = new Animal("DOG");
        Console.WriteLine(a.isCat() ? "YES" : "NO"); 
    }
}


public class Animal
{
    private string animal{get; set;}

    public Animal(string animal)
    {
        this.animal = animal;
    }

    public bool isCat() {
        return string.Compare("CAT", animal) == 0 ? true : false; 
    } 
}
