using System;
using System.Linq;
using Newtonsoft.Json;
using JsonTypeGenerator;
using JsonTypeGenerator.Json;

namespace ConsoleApp1
{
    class Program
    {
        [JsonType(ClassName = "MyGeneratedClass2")]
        private const string Json = @"{""employees"": [
                        {  ""firstName"":""John"" , ""lastName"":""Doe"", ""age"" : 35, ""blah"": 44 }, 
                        {  ""firstName"":""Anna"" , ""lastName"":""Smith"" }, 
                        { ""firstName"": ""Peter"" ,  ""lastName"": ""Jones "" }
                        ]
                        }";

        static void Main()
        {
            var myObj = JsonConvert.DeserializeObject<MyGeneratedClass2>(Json);
            var john = myObj.Employees.First();
            Console.WriteLine(john.Blah);
        }
    }
}

