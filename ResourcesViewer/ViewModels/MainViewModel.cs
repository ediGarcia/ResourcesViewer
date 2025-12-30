using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperExtensions;
using HelperMethods;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Threading;

namespace ResourcesViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    #region Properties

    /// <summary>
    /// Gets the error message to be displayed.
    /// </summary>
    public string ErrorMessage
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets or sets the monitoring interval in seconds.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ToggleMonitoringCommand))]
    private int _interval = 1;

    /// <summary>
    /// Gets a value indicating whether the error message is visible.
    /// </summary>
    public bool IsErrorMessageVisible
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets a value indicating whether monitoring is currently active.
    /// </summary>
    public bool IsMonitoring
    {
        get => _timer.IsEnabled;
        private set
        {
            _timer.Interval = TimeSpan.FromSeconds(Interval);
            _timer.IsEnabled = value;

            OnPropertyChanged();
            NotifyAllCommandsCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the search popup is open.
    /// </summary>
    public bool IsSearchPopupOpen
    {
        get;
        private set
        {
            SetProperty(ref field, value);
            NotifyAllCommandsCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets the search text used to find processes.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string _searchText;

    /// <summary>
    /// Gets the search result containing the list of found processes.
    /// </summary>
    public IReadOnlyList<Process> SearchResult
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets or sets the selected index in the search result.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddProcessCommand))]
    private int _selectedIndex = -1;

    /// <summary>
    /// Gets the collection of processes being monitored.
    /// </summary>
    public ObservableCollection<ProcessInfoViewModel> Processes { get; } = [];

    #endregion

    private readonly DispatcherTimer _timer;

    public MainViewModel()
    {
        _timer = new();
        _timer.Tick += Timer_Tick;
    }

    #region Events

    #region AddProcess
    /// <summary>
    /// Adds the selected process from the search results to the monitored processes list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddProcess))]
    private void AddProcess()
    {
        Processes.Add(new(SearchResult[SelectedIndex]));
        CloseSearchPopup();
        NotifyAllCommandsCanExecuteChanged();
    }
    #endregion

    #region CanAddProcess
    /// <summary>
    /// Determines whether the selected process can be added to the monitored processes list.
    /// </summary>
    /// <returns></returns>
    private bool CanAddProcess() =>
        !IsMonitoring && SelectedIndex >= 0 && Processes.None(_ => _.Id == SearchResult[SelectedIndex].Id);
    #endregion

    #region CanOpenSearchPopup
    /// <summary>
    /// Determines whether the search popup can be opened.
    /// </summary>
    /// <returns></returns>
    private bool CanOpenSearchPopup() =>
        !IsMonitoring && !IsSearchPopupOpen;
    #endregion

    #region CanRemoveProcess
    /// <summary>
    /// Determines whether a process can be removed from the monitored processes list.
    /// </summary>
    /// <returns></returns>
    private bool CanRemoveProcess() =>
        !IsMonitoring;
    #endregion

    #region CanSaveProcessResults
    /// <summary>
    /// Determines whether the results of a process can be saved to a file.
    /// </summary>
    /// <param name="process"></param>
    /// <returns></returns>
    private bool CanSaveProcessResults(ProcessInfoViewModel? process) =>
        !IsMonitoring && process?.TimeStamps.Any() == true;
    #endregion

    #region CanSearch
    /// <summary>
    /// Determines whether a search can be performed.
    /// </summary>
    /// <returns></returns>
    private bool CanSearch() =>
        !IsMonitoring && !SearchText.IsNullOrWhiteSpace();
    #endregion

    #region CanToggleMonitoring
    /// <summary>
    /// Determines whether monitoring can be toggled.
    /// </summary>
    /// <returns></returns>
    private bool CanToggleMonitoring() =>
        IsMonitoring || !IsMonitoring && Interval > 0 && Processes.Any();
    #endregion

    #region CloseErrorMessage
    /// <summary>
    /// Closes the error message display.
    /// </summary>
    [RelayCommand]
    private void CloseErrorMessage() =>
        IsErrorMessageVisible = false;
    #endregion

    #region CloseSearchPopup
    /// <summary>
    /// Closes the search popup and clears the search results.
    /// </summary>
    [RelayCommand]
    public void CloseSearchPopup()
    {
        IsSearchPopupOpen = false;
        SearchResult = [];
    }
    #endregion

    #region OpenSearchPopup
    /// <summary>
    /// Opens the search popup to allow the user to search for processes.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenSearchPopup))]
    private void OpenSearchPopup() =>
        IsSearchPopupOpen = true;
    #endregion

    #region RemoveProcess
    /// <summary>
    /// Removes the specified process from the monitored processes list.
    /// </summary>
    /// <param name="process"></param>
    [RelayCommand(CanExecute = nameof(CanRemoveProcess))]
    private void RemoveProcess(ProcessInfoViewModel process)
    {
        Processes.Remove(process);
        NotifyAllCommandsCanExecuteChanged();
    }
    #endregion

    #region SaveProcessResults
    /// <summary>
    /// Saves the results of the specified process to a CSV file.
    /// </summary>
    /// <param name="process"></param>
    [RelayCommand(CanExecute = nameof(CanSaveProcessResults))]
    private void SaveProcessResults(ProcessInfoViewModel process)
    {
        if (!GetTargetFileName(process, out string path))
            return;

        StringBuilder text = new();

        if (!FileHelper.Exists(path))
            text.AppendLine("timestamp,handleCount,memory,threadCount");

        IReadOnlyList<long> handleCountValue = process.HandleCount.GetValues();
        IReadOnlyList<long> memoryUsageValue = process.MemoryUsage.GetValues();
        IReadOnlyList<long> threadCountValue = process.ThreadCount.GetValues();

        for (int i = 0; i < process.TimeStamps.Count; i++)
            text.AppendLine($"{process.TimeStamps[i]:hh:mm:ss},{handleCountValue[i]},{memoryUsageValue[i]},{threadCountValue[i]}");

        try
        {
            FileHelper.WriteAllText(path, text.ToString(), mode: FileMode.Append);

            // Some system errors do not throw exceptions, so we manually check if the file was written correctly.
            if (!FileHelper.Exists(path) || FileHelper.GetFileSize(path) == 0)
            {
                FileHelper.Delete(path);
                throw new IOException("An unknown error prevented the file from being written.");
            }

        }
        catch (Exception ex)
        {
            ShowErrorMessage(ex.Message);
        }
    }
    #endregion

    #region Search
    /// <summary>
    /// Performs a search for processes based on the search text.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void Search()
    {
        try
        {
            SearchResult = SearchText.IsNumber()
                ? [Process.GetProcessById(SearchText.ToInt())]
                : Process.GetProcessesByName(SearchText).ToList();
        }
        catch
        {
            SearchResult = [];
        }

        if (SearchResult.Count > 0)
            SelectedIndex = 0;
    }
    #endregion

    #region Timer_Tick
    /// <summary>
    /// Updates the process information at each timer tick.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer_Tick(object? sender, EventArgs e) =>
        Processes.ForEach(_ => _.UpdateProcessInfo());
    #endregion

    #region ToggleMonitoring
    /// <summary>
    /// Toggles the monitoring state on or off.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanToggleMonitoring))]
    private void ToggleMonitoring() =>
        IsMonitoring = !IsMonitoring;
    #endregion

    #endregion

    #region Private Methods

    #region GetTargetFileName
    /// <summary>
    /// Opens a save file dialog to get the target file name for saving process results.
    /// </summary>
    /// <param name="process"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private bool GetTargetFileName(ProcessInfoViewModel process, out string fileName)
    {
        SaveFileDialog saveFileDialog = new()
        {
            FileName = $"{process.ProcessName.Replace(".", "_")}_{process.Id}_ResourceUsage.csv",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Save File"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            fileName = saveFileDialog.FileName;
            return true;
        }

        fileName = null;
        return false;
    }
    #endregion

    #region NotifyAllCommandsCanExecuteChanged
    /// <summary>
    /// Notifies all commands to re-evaluate their CanExecute state.
    /// </summary>
    private void NotifyAllCommandsCanExecuteChanged()
    {
        AddProcessCommand.NotifyCanExecuteChanged();
        OpenSearchPopupCommand.NotifyCanExecuteChanged();
        RemoveProcessCommand.NotifyCanExecuteChanged();
        SaveProcessResultsCommand.NotifyCanExecuteChanged();
        SearchCommand.NotifyCanExecuteChanged();
        ToggleMonitoringCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region ShowErrorMessage
    /// <summary>
    /// Shows an error message to the user.
    /// </summary>
    /// <param name="message"></param>
    private void ShowErrorMessage(string message)
    {
        ErrorMessage = message;
        IsErrorMessageVisible = true;
    }
    #endregion

    #endregion
}