using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Platform.Storage;
using Padcher2XQ.ViewModels;
using System.Threading.Tasks;
using Avalonia.Input;
using Padcher2XQ.Services;
using Padcher2XQ.Models;
namespace Padcher2XQ.Views;

public partial class MainWindow : Window
{
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

    
    // ---------------------------------------------------------------------------------
    // Drag-to-reorder for the multi-patch list.
    //
    // The item is dragged by its grip. While dragging, nothing is moved in the collection:
    // the dragged row follows the pointer and the other rows slide out of the way
    // (RenderTransform only). The collection is changed once, on release.
    // All coordinates are in PatchList space, which scrolls together with PatchScroll.
    // ---------------------------------------------------------------------------------

    private sealed class PatchDragState
    {
        public required int StartIndex;
        public required double StartListY;   // pointer Y (list coords) at press
        public required double[] Tops;       // original top of every row (list coords)
        public required double[] Heights;
        public int TargetIndex;
        public bool Started;                 // true after the pointer moved past the threshold
    }

    private const double DragThreshold = 4;
    private const double AutoScrollEdge = 28;   // px from the viewport edge where auto-scroll kicks in
    private const double AutoScrollMaxStep = 18; // px per tick

    private PatchDragState? _drag;
    private double _lastListY;          // last pointer Y in list coords
    private double _lastScrollY;        // last pointer Y in ScrollViewer coords (viewport space)
    private double _lastOffsetY;        // scroll offset at the moment of _lastListY
    private DispatcherTimer? _autoScrollTimer;

    private void Grip_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_drag != null) return;
        if (sender is not Control grip || grip.DataContext is not PatchItemViewModel item) return;
        if (!e.GetCurrentPoint(grip).Properties.IsLeftButtonPressed) return;
        if (DataContext is not MainWindowViewModel vm) return;

        int count = vm.SelectedPatches.Count;
        int start = vm.SelectedPatches.IndexOf(item);
        if (start < 0 || count < 2) return;

        var tops = new double[count];
        var heights = new double[count];
        for (int i = 0; i < count; i++)
        {
            var container = PatchList.ContainerFromIndex(i);
            var topLeft = container?.TranslatePoint(new Point(0, 0), PatchList);
            if (container == null || topLeft == null) return;   // layout not ready - ignore the press
            tops[i] = topLeft.Value.Y;
            heights[i] = container.Bounds.Height;
        }

        _drag = new PatchDragState
        {
            StartIndex = start,
            StartListY = e.GetPosition(PatchList).Y,
            Tops = tops,
            Heights = heights,
            TargetIndex = start
        };
        RememberPointer(e);

        e.Pointer.Capture(grip);
        e.Handled = true;
    }

    private void Grip_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_drag == null) return;

        RememberPointer(e);

        if (!_drag.Started)
        {
            if (Math.Abs(_lastListY - _drag.StartListY) < DragThreshold) return;
            _drag.Started = true;
            _autoScrollTimer ??= CreateAutoScrollTimer();
            _autoScrollTimer.Start();
            UpdateDragVisuals(_lastListY);
        }
        else
        {
            UpdateDragVisuals(_lastListY);
        }

        e.Handled = true;
    }

    private void Grip_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_drag == null) return;
        EndDrag(commit: true);
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    // Capture can be lost for reasons we do not control (window deactivated, popup, ...): cancel the drag.
    private void Grip_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_drag != null) EndDrag(commit: false);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && _drag != null)
        {
            EndDrag(commit: false);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    private void RememberPointer(PointerEventArgs e)
    {
        _lastListY = e.GetPosition(PatchList).Y;
        _lastScrollY = e.GetPosition(PatchScroll).Y;
        _lastOffsetY = PatchScroll.Offset.Y;
    }

    private DispatcherTimer CreateAutoScrollTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) => AutoScrollTick();
        return timer;
    }

    private void AutoScrollTick()
    {
        if (_drag is not { Started: true }) return;

        double viewport = PatchScroll.Viewport.Height;
        double step = 0;
        if (_lastScrollY < AutoScrollEdge)
            step = -(AutoScrollEdge - _lastScrollY) / AutoScrollEdge * AutoScrollMaxStep;
        else if (_lastScrollY > viewport - AutoScrollEdge)
            step = (_lastScrollY - (viewport - AutoScrollEdge)) / AutoScrollEdge * AutoScrollMaxStep;

        if (step == 0) return;

        double maxOffset = Math.Max(0, PatchScroll.Extent.Height - viewport);
        double newOffset = Math.Clamp(PatchScroll.Offset.Y + step, 0, maxOffset);
        if (Math.Abs(newOffset - PatchScroll.Offset.Y) < 0.01) return;

        PatchScroll.Offset = new Vector(PatchScroll.Offset.X, newOffset);

        // the pointer did not move, but the content under it did
        UpdateDragVisuals(_lastListY + (PatchScroll.Offset.Y - _lastOffsetY));
    }

    /// <summary>Moves the dragged row with the pointer and slides the other rows to preview the result.</summary>
    private void UpdateDragVisuals(double listY)
    {
        var d = _drag;
        if (d == null) return;

        int count = d.Tops.Length;
        int s = d.StartIndex;

        // keep the dragged row inside the list
        double minDy = d.Tops[0] - d.Tops[s];
        double maxDy = d.Tops[count - 1] + d.Heights[count - 1] - (d.Tops[s] + d.Heights[s]);
        double dy = Math.Clamp(listY - d.StartListY, minDy, maxDy);

        // new index = number of OTHER rows whose centre is above the centre of the dragged row
        double draggedCenter = d.Tops[s] + d.Heights[s] / 2 + dy;
        int target = 0;
        for (int i = 0; i < count; i++)
        {
            if (i == s) continue;
            if (d.Tops[i] + d.Heights[i] / 2 < draggedCenter) target++;
        }
        d.TargetIndex = target;

        for (int i = 0; i < count; i++)
        {
            var container = PatchList.ContainerFromIndex(i);
            if (container == null) continue;

            if (i == s)
            {
                container.RenderTransform = new TranslateTransform(0, dy);
                container.ZIndex = 100;
                container.Opacity = 0.85;
                continue;
            }

            double shift = 0;
            if (s < target && i > s && i <= target) shift = -d.Heights[s];   // rows between: move up
            else if (s > target && i >= target && i < s) shift = d.Heights[s]; // rows between: move down
            container.RenderTransform = shift == 0 ? null : new TranslateTransform(0, shift);
        }
    }

    private void EndDrag(bool commit)
    {
        var d = _drag;
        _drag = null;
        _autoScrollTimer?.Stop();
        if (d == null) return;

        // reset visuals BEFORE touching the collection, containers get re-used afterwards
        for (int i = 0; i < d.Tops.Length; i++)
        {
            var container = PatchList.ContainerFromIndex(i);
            if (container == null) continue;
            container.RenderTransform = null;
            container.ZIndex = 0;
            container.Opacity = 1;
        }

        if (commit && d.Started && d.TargetIndex != d.StartIndex && DataContext is MainWindowViewModel vm)
        {
            vm.MovePatchTo(d.StartIndex, d.TargetIndex);
            PatchList.SelectedIndex = d.TargetIndex;
        }
    }

    
    private readonly PresetArchiveService _archiveService = new();

    private async void SavePreset_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Preset Package",
            DefaultExtension = "p2xq",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Padcher2XQ Preset Package (*.p2xq, *.zip)")
                {
                    Patterns = new[] { "*.p2xq", "*.zip" }
                }
            }
        });

        if (file != null && DataContext is MainWindowViewModel vm)
        {
            await _archiveService.ExportPresetArchiveAsync(file.Path.LocalPath, vm.SelectedPatches);
            vm.UpdateStatus("Preset package exported successfully!", "Green");
        }
    }

    private async void LoadPreset_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Preset Package",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Padcher2XQ Preset Package (*.p2xq, *.zip)")
                {
                    Patterns = new[] { "*.p2xq", "*.zip" }
                }
            }
        });

        if (files.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            var patches = await _archiveService.ImportPresetArchiveAsync(files[0].Path.LocalPath);
            if (patches.Any())
            {
                vm.SelectedPatches.Clear();
                foreach (var patch in patches)
                {
                    vm.SelectedPatches.Add(patch);
                }
                vm.UpdateStatus("Preset package loaded successfully!", "DodgerBlue");
            }
        }
    }
}