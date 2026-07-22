using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;

namespace Padcher2XQ.ViewModels;

public partial class ZipEntryItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string _fileName = string.Empty;
    public string FullName { get; set; } = string.Empty;
    
    // Посилання на батьківську ViewModel для доступу до списку
    public SelectZipViewModel? ParentVm { get; set; }

    // Метод спрацьовує автоматично при будь-якій зміні _isSelected
    partial void OnIsSelectedChanged(bool value)
    {
        // Якщо файл вибрали, і ми в Одиночному режимі - знімаємо виділення з усіх інших
        if (value && ParentVm != null && !ParentVm.IsMultiMode)
        {
            foreach (var item in ParentVm.Entries)
            {
                if (item != this && item.IsSelected)
                {
                    item.IsSelected = false;
                }
            }
        }
    }
}

public partial class SelectZipViewModel : ViewModelBase
{
    public ObservableCollection<ZipEntryItem> Entries { get; } = new();
    
    [ObservableProperty] private bool _isMultiMode;

    public SelectZipViewModel(IEnumerable<string> entries, bool isMultiMode)
    {
        IsMultiMode = isMultiMode;
        
        foreach (var entry in entries)
        {
            Entries.Add(new ZipEntryItem
            {
                FullName = entry,
                FileName = Path.GetFileName(entry),
                ParentVm = this, 
                IsSelected = isMultiMode 
            });
        }

        if (!isMultiMode && Entries.Count > 0)
        {
            Entries[0].IsSelected = true;
        }
    }
}