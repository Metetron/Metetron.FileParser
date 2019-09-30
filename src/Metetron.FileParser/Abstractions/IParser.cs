using System.Threading.Tasks;

namespace Metetron.FileParser.Abstractions
{
    public interface IParser
    {
        Task ParseFile(string filePath);
    }
}