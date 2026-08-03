using CommunityToolkit.Mvvm.ComponentModel;

namespace Padcher2XQ.ViewModels;

public partial class PatchItemViewModel : ObservableObject
{
    [ObservableProperty] private bool _isEnabled = true;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _patchName = string.Empty;
    [ObservableProperty] private string _format = string.Empty;

}