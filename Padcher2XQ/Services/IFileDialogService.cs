using System.Collections.Generic;
using System.Threading.Tasks;

namespace Padcher2XQ.Services;

public interface IFileDialogService
{
    Task<string[]?> OpenFileAsync(string title, string[] extensions, bool allowMultiple = false);
    
    Task<string?> SaveFileAsync(string title, string defaultName, string extension);
}