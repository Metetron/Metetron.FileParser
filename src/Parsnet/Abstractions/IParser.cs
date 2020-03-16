using System.Threading.Tasks;

namespace Parsnet.Abstractions
{
    public interface IParser
    {
        Task ParseFile(string filePath);
    }
}