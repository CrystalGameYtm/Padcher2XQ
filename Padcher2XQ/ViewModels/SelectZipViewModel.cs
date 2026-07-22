using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Padcher2XQ.ViewModels;

public partial class SelectZipViewModel : ViewModelBase
{
    public ObservableCollection<string> Entries { get; } = new();

    [ObservableProperty]
    private string? _selectedEntry;

    public SelectZipViewModel(IEnumerable<string> entries)
    {
        foreach (var entry in entries)
        {
            Entries.Add(entry);
        }
        if (Entries.Count > 0) SelectedEntry = Entries[0];
    }
}