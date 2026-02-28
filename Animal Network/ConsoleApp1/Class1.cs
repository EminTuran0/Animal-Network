using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;

class Program
{
    static void Main(string[] args)
    {
        string password = "123456";
        string hash = "$2b$10$q75pwFG5PZB5jLNpNd3cL.3bTcU0SoOgg4lTuICIqtCClG6Z6rETe";

        bool result = BCrypt.Net.BCrypt.Verify(password, hash);

        Console.WriteLine($"Şifre doğru mu? {result}");
    }
}