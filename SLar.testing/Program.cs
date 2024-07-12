using SerializeLib;

namespace SLar.testing;

class Program
{
    static void Main(string[] args)
    {
        var slar = SLAr.CreateArchive("testing", new List<MetaTag>() {new ("info", "This is an example metadata tag", "file1.txt")});
        
        Serializer.SerializeToFile(slar, "testing.slar");
        slar.Dispose();
        
        SLAr.Extract("testing.slar", "testing_out");
    }
}