using CommunityToolkit.Mvvm.ComponentModel;
using Padcher2XQ.ViewModels;

namespace Padcher2XQ.Models;

public partial class PatchItemViewModel : ViewModelBase
{
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _patchName = string.Empty;
    [ObservableProperty] private string _format = string.Empty;
}