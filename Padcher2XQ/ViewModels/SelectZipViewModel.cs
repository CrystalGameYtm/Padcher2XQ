using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;

namespace Padcher2XQ.ViewModels;

// 1. Клас, який представляє один рядок у нашому списку
public partial class ZipEntryItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private string _fileName = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public partial class SelectZipViewModel : ViewModelBase
{
    // 2. Тепер колекція тримає наші об'єкти з чекбоксами
    public ObservableCollection<ZipEntryItem> Entries { get; } = new();

    public SelectZipViewModel(IEnumerable<string> entries)
    {
        foreach (var entry in entries)
        {
            Entries.Add(new ZipEntryItem
            {
                FullName = entry,
                FileName = Path.GetFileName(entry), // Показуємо тільки ім'я файлу
                IsSelected = true // За замовчуванням галочки стоять на всіх патчах
            });
        }
    }
}