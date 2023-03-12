using System.Text.Json;
using code.Models;

Console.WriteLine("begin");

Person1 person1 = new()
{
    FirstName = "thomas"
};

string jsonString = JsonSerializer.Serialize(person1);
Console.WriteLine(jsonString);

Person2a person2a = JsonSerializer.Deserialize<Person2a>(jsonString);
Person2b person2b = JsonSerializer.Deserialize<Person2b>(jsonString);

Console.WriteLine("done");
