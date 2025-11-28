using System.Threading.Tasks;

namespace Padcher2XQ.Services;

public interface IFileDialogService
{
    Task<string?> OpenFileAsync(string title, string[] extensions);
    Task<string?> SaveFileAsync(string title, string defaultName, string extension);
}