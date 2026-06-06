using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Forms = System.Windows.Forms;
using System.Windows.Input;

namespace SimpleFolderCompare;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<CompareRow> _rows = new();
    private readonly CollectionView _view;
    private string _leftPath = string.Empty;
    private string _rightPath = string.Empty;

    public MainWindow()
    {
        InitializeComponent();

        _view = (CollectionView)CollectionViewSource.GetDefaultView(_rows);
        DiffListView.ItemsSource = _view;

        AddActivity("Simple Folder Compare ready.");
        AddActivity("Features enabled: recursive scan, size check, optional SHA-256, filters, copy, and CSV export.");
    }

    private void AddActivity(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss} - {message}";
        ActivityList.Items.Insert(0, line);
        if (ActivityList.Items.Count > 250)
        {
            while (ActivityList.Items.Count > 250)
            {
                ActivityList.Items.RemoveAt(ActivityList.Items.Count - 1);
            }
        }

        StatusText.Text = message;
    }

    private void PickLeftFolder(object sender, RoutedEventArgs e)
    {
        var folder = SelectFolder(_leftPath);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        _leftPath = folder;
        LeftPathText.Text = $"Left: {folder}";
        AddActivity($"Left folder selected: {folder}");
    }

    private void PickRightFolder(object sender, RoutedEventArgs e)
    {
        var folder = SelectFolder(_rightPath);
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        _rightPath = folder;
        RightPathText.Text = $"Right: {folder}";
        AddActivity($"Right folder selected: {folder}");
    }

    private static string? SelectFolder(string initialPath)
    {
        using var dlg = new FolderBrowserDialog
        {
            ShowNewFolderButton = false,
            Description = "Select folder",
            SelectedPath = initialPath,
        };

        return dlg.ShowDialog() == Forms.DialogResult.OK ? dlg.SelectedPath : null;
    }

    private async void CompareFolders(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_leftPath) || string.IsNullOrWhiteSpace(_rightPath))
        {
            _ = System.Windows.MessageBox.Show(this, "Pick both folders first.", "Folder required", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CompareButton.IsEnabled = false;
        Cursor = System.Windows.Input.Cursors.Wait;
        _rows.Clear();
        AddActivity($"Comparing '{_leftPath}' with '{_rightPath}'.");

        var options = new CompareOptions(
            RecurseCheck.IsChecked == true,
            SizeCheck.IsChecked == true,
            HashCheck.IsChecked == true,
            EqualOnlyCheck.IsChecked == true);

        var stats = await Task.Run(() => CompareFoldersInternal(_leftPath, _rightPath, options));

        foreach (var row in stats.Rows.OrderBy(r => r.Status, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.RelativePath))
        {
            _rows.Add(row);
        }

        ApplyFilter(null, null);
        CompareButton.IsEnabled = true;
        Cursor = System.Windows.Input.Cursors.Arrow;
        SummaryText.Text = $"Added L: {stats.LeftOnly}, Added R: {stats.RightOnly}, Modified: {stats.Modified}, Unchanged: {stats.Unchanged}, Total checked: {stats.Total} - Hash compare {(options.IncludeHash ? "enabled" : "disabled")}.";
        AddActivity($"Compare complete. {stats.Total} file entries loaded.");
    }

    private static CompareStats CompareFoldersInternal(string leftRoot, string rightRoot, CompareOptions options)
    {
        var result = new CompareStats();
        var leftMap = BuildFileIndex(leftRoot, options.Recurse);
        var rightMap = BuildFileIndex(rightRoot, options.Recurse);

        var allKeys = new HashSet<string>(leftMap.Keys, StringComparer.OrdinalIgnoreCase);
        allKeys.UnionWith(rightMap.Keys);

        foreach (var key in allKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            var leftExists = leftMap.TryGetValue(key, out var leftInfo);
            var rightExists = rightMap.TryGetValue(key, out var rightInfo);

            if (!leftExists)
            {
                var row = CreateMissingRow(key, rightInfo!, false);
                result.Rows.Add(row);
                result.RightOnly++;
                result.Total++;
                continue;
            }

            if (!rightExists)
            {
                var row = CreateMissingRow(key, leftInfo!, true);
                result.Rows.Add(row);
                result.LeftOnly++;
                result.Total++;
                continue;
            }

            var diffRow = ComparePair(key, leftInfo!, rightInfo!, options);
            if (!options.ShowEqualOnly && diffRow.Status == "Unchanged")
            {
                continue;
            }

            result.Rows.Add(diffRow);
            if (diffRow.Status == "Modified")
            {
                result.Modified++;
            }
            else
            {
                result.Unchanged++;
            }

            result.Total++;
        }

        return result;
    }

    private static CompareRow CreateMissingRow(string key, FileRecord source, bool isLeft)
    {
        var status = isLeft ? "Added on Left" : "Added on Right";
        return new CompareRow(
            key,
            status,
            isLeft ? source.FileSizeText : string.Empty,
            isLeft ? string.Empty : source.FileSizeText,
            isLeft ? source.LastWrite : string.Empty,
            isLeft ? string.Empty : source.LastWrite,
            isLeft ? "Missing on right" : "Missing on left");
    }

    private static CompareRow ComparePair(string key, FileRecord left, FileRecord right, CompareOptions options)
    {
        var notes = new StringBuilder();
        var status = "Unchanged";

        if (options.IncludeSize && left.FileSize != right.FileSize)
        {
            status = "Modified";
            notes.Append("Size differs.");
        }

        if (options.IncludeHash)
        {
            var leftHash = HashFile(left.FullPath);
            var rightHash = HashFile(right.FullPath);
            if (!string.Equals(leftHash, rightHash, StringComparison.OrdinalIgnoreCase))
            {
                status = "Modified";
                if (notes.Length > 0)
                {
                    notes.Append(' ');
                }

                notes.Append("SHA-256 differs.");
            }
        }

        return new CompareRow(
            key,
            status,
            left.FileSizeText,
            right.FileSizeText,
            left.LastWrite,
            right.LastWrite,
            notes.ToString());
    }

    private static Dictionary<string, FileRecord> BuildFileIndex(string root, bool recurse)
    {
        var result = new Dictionary<string, FileRecord>(StringComparer.OrdinalIgnoreCase);
        var search = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var rootUri = new Uri(root + Path.DirectorySeparatorChar);

        foreach (var path in Directory.EnumerateFiles(root, "*", search))
        {
            var info = new FileInfo(path);
            var rel = rootUri.MakeRelativeUri(new Uri(path)).ToString().Replace('/', Path.DirectorySeparatorChar);
            result[rel] = new FileRecord(
                rel,
                path,
                info.Length,
                info.Length.ToString(CultureInfo.InvariantCulture),
                info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }

        return result;
    }

    private static string HashFile(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(stream);
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }

        return sb.ToString();
    }

    private void ApplyFilter(object? sender, RoutedEventArgs? e)
    {
        if (_view == null || SearchBox == null || FilterDropDown == null)
        {
            return;
        }

        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var statusFilter = ((ComboBoxItem?)FilterDropDown.SelectedItem)?.Content?.ToString() ?? "All";

        _view.Filter = obj =>
        {
            if (obj is not CompareRow row)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(search) &&
                !row.RelativePath.Contains(search, StringComparison.OrdinalIgnoreCase) &&
                !row.Notes.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return statusFilter == "All" || string.Equals(row.Status, statusFilter, StringComparison.OrdinalIgnoreCase);
        };
    }

    private async void CopyToLeft(object sender, RoutedEventArgs e)
    {
        await CopySelectedItems(toLeft: true);
    }

    private async void CopyToRight(object sender, RoutedEventArgs e)
    {
        await CopySelectedItems(toLeft: false);
    }

    private async Task CopySelectedItems(bool toLeft)
    {
        var selected = DiffListView.SelectedItems.Cast<CompareRow>().ToList();
        if (selected.Count == 0)
        {
            _ = System.Windows.MessageBox.Show(this, "Select at least one item first.", "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if ((toLeft && string.IsNullOrWhiteSpace(_leftPath)) || (!toLeft && string.IsNullOrWhiteSpace(_rightPath)))
        {
            _ = System.Windows.MessageBox.Show(this, "Destination folder is not selected.", "Destination required", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var targetRoot = toLeft ? _leftPath : _rightPath;
        var sourceRoot = toLeft ? _rightPath : _leftPath;

        await Task.Run(() =>
        {
            foreach (var row in selected)
            {
                var source = Path.Combine(sourceRoot, row.RelativePath);
                var target = Path.Combine(targetRoot, row.RelativePath);
                if (!File.Exists(source))
                {
                    return;
                }

                var targetDir = Path.GetDirectoryName(target);
                if (!Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir!);
                }

                File.Copy(source, target, overwrite: true);
            }
        });

        AddActivity($"Copied {selected.Count} file(s) to {(toLeft ? "Left" : "Right")}");
    }

    private void ExportCsv(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV File|*.csv",
            FileName = $"FolderCompare_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv",
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var lines = new List<string>
        {
            "RelativePath,Status,LeftSize,RightSize,LeftModified,RightModified,Notes"
        };

        foreach (var row in _view.Cast<CompareRow>())
        {
            lines.Add(string.Join(",", Escape(row.RelativePath), Escape(row.Status), Escape(row.LeftSize), Escape(row.RightSize), Escape(row.LeftModified), Escape(row.RightModified), Escape(row.Notes)));
        }

        File.WriteAllLines(dialog.FileName, lines);
        AddActivity($"Exported CSV: {dialog.FileName}");
    }

    private static string Escape(string text)
    {
        var value = text.Replace("\"", "\"\"");
        return $"\"{value}\"";
    }
}

public record CompareRow(
    string RelativePath,
    string Status,
    string LeftSize,
    string RightSize,
    string LeftModified,
    string RightModified,
    string Notes);

public record FileRecord(
    string RelativePath,
    string FullPath,
    long FileSize,
    string FileSizeText,
    string LastWrite);

public class CompareStats
{
    public List<CompareRow> Rows { get; } = new();
    public int LeftOnly { get; set; }
    public int RightOnly { get; set; }
    public int Modified { get; set; }
    public int Unchanged { get; set; }
    public int Total { get; set; }
}

public record CompareOptions(
    bool Recurse,
    bool IncludeSize,
    bool IncludeHash,
    bool ShowEqualOnly);


