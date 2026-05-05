using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform; 
using Avalonia.Interactivity;
using Avalonia.Platform.Storage; 
using Padcher2XQ.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Padcher2XQ.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent,DragOver);
        AddHandler(DragDrop.DropEvent,Drop);
        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }
    private void DragOver(object? sender, DragEventArgs e) { 
        e.DragEffects = DragDropEffects.Copy; 
        e.Handled = true;
    } 
    private void Drop(object? sender, DragEventArgs e) { 
        var files = e.DataTransfer.TryGetFiles();
        if (files != null) {
            var filePaths= files
                .Select(x => x.TryGetLocalPath()?? x.Path.LocalPath)
                .Where(path => !string.IsNullOrEmpty(path))
                .ToArray();
            if (filePaths.Length > 0 && DataContext is MainWindowViewModel vm)
            {
                vm.HandleDroppedFiles(filePaths);
            }
        } 
    }   
    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.V)
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard == null) return;

            var filePaths = new List<string>();

            try
            {
                var files = await clipboard.TryGetFilesAsync();
                if (files != null)
                {
                    filePaths.AddRange(files
                        .Select(x => x.TryGetLocalPath())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Cast<string>());
                }

                if (filePaths.Count == 0)
                {
                    string? text = await clipboard.TryGetTextAsync();
                    
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var parsedPaths = text
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(line => line.Trim())
                            .Select(line => 
                            {
                                if (line == "copy" || line == "cut") return null; 
                                
                                var path = line.Trim('"');
                                if (path.StartsWith("file://"))
                                    return Uri.UnescapeDataString(path.Replace("file://", ""));
                                return path;
                            })
                            .Where(path => !string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                            .Cast<string>();
                        
                        filePaths.AddRange(parsedPaths);
                    }
                }

                if (filePaths.Count > 0 && DataContext is MainWindowViewModel vm)
                {
                    e.Handled = true;   
                    vm.HandleDroppedFiles(filePaths.ToArray());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Clipboard error: {ex.Message}");
            }
        }
    }
}