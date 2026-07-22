using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Padcher2XQ.ViewModels;
using Padcher2XQ.Models;
namespace Padcher2XQ.Views;

public partial class MainWindow : Window
{
    private PatchItemViewModel? _draggedItem;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void Window_Drop(object? sender, DragEventArgs e)
    {
        try
        {
            var items = e.DataTransfer.TryGetFiles();

            if (items != null)
            {
                var pathList = new List<string>();
                foreach (var item in items)
                {
                    if (item?.Path != null && !string.IsNullOrEmpty(item.Path.LocalPath))
                    {
                        pathList.Add(item.Path.LocalPath);
                    }
                }

                if (pathList.Count > 0 && DataContext is MainWindowViewModel vm)
                {
                    await vm.HandleDroppedFiles(pathList.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Drop Error: {ex.Message}");
        }
    }

    private void Item_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Затискаємо ліву кнопку миші на патчі
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
            sender is Control control && control.DataContext is PatchItemViewModel item)
        {
            _draggedItem = item;
        }
    }

    private void Item_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_draggedItem != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                var hitControl = topLevel.InputHitTest(e.GetPosition(topLevel)) as Control;
            
                if (hitControl?.DataContext is PatchItemViewModel targetItem && targetItem != _draggedItem)
                {
                    if (DataContext is MainWindowViewModel vm)
                    {
                        int sourceIndex = vm.SelectedPatches.IndexOf(_draggedItem);
                        int targetIndex = vm.SelectedPatches.IndexOf(targetItem);
                        if (sourceIndex >= 0 && targetIndex >= 0)
                        {
                            vm.SelectedPatches.Move(sourceIndex, targetIndex);
                        }
                    }
                }
            }
        }
    }

    private void Item_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _draggedItem = null;
    }
}