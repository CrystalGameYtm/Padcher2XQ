using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Padcher2XQ.ViewModels;

public partial class SingleZipItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    public string FullName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public partial class MultiZipItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected = true;
    public string FullName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public record SingleZipResult(string SelectedFullName);
public record MultiZipResult(string FullName, bool IsEnabled);

public partial class SelectZipViewModel : ViewModelBase
{
    public ObservableCollection<SingleZipItem> SingleEntries { get; } = new();
    public ObservableCollection<MultiZipItem> MultiEntries { get; } = new();

    [ObservableProperty] private bool _isMultiMode;

    public SelectZipViewModel(IEnumerable<string> entries, bool isMultiMode)
    {
        IsMultiMode = isMultiMode;

        if (isMultiMode)
        {
            foreach (var entry in entries)
            {
                MultiEntries.Add(new MultiZipItem
                {
                    FullName = entry,
                    FileName = Path.GetFileName(entry),
                    IsSelected = true 
                });
            }
        }
        else
        {
            foreach (var entry in entries)
            {
                SingleEntries.Add(new SingleZipItem
                {
                    FullName = entry,
                    FileName = Path.GetFileName(entry),
                    IsSelected = false
                });
            }
            if (SingleEntries.Count > 0)
            {
                SingleEntries[0].IsSelected = true; 
            }
        }
    }

    public SingleZipResult? GetSingleResult()
    {
        var selected = SingleEntries.FirstOrDefault(x => x.IsSelected);
        return selected != null ? new SingleZipResult(selected.FullName) : null;
    }
    public List<MultiZipResult> GetMultiResults()
    {
        return MultiEntries
            .Select(x => new MultiZipResult(x.FullName, x.IsSelected))
            .ToList();
    }
}