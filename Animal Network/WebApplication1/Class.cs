using System;

class Program
{
    static void Main(string[] args)
    {
        string hash = "$2b$10$q75pwFG5PZB5jLNpNd3cL.3bTcU0SoOgg4lTuICIqtCClG6Z6rETe";
        bool result = BCrypt.Net.BCrypt.Verify("123456", hash);
        Console.WriteLine($"Şifre doğru mu? {result}");
    }
}
