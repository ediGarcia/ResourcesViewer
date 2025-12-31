using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace ResourcesViewer.ViewModels;

public class ProcessInfoViewModel : ObservableObject
{
    #region Properties

    /// <summary>
    /// Gets the error message if an error has occurred while updating the process information.
    /// </summary>
    public string ErrorMessage
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets the handle count information.
    /// </summary>
    public ResourceUsageViewModel HandleCount { get; } = new();

    /// <summary>
    /// Gets a value indicating whether an error has occurred while updating the process information.
    /// </summary>
    public bool HasError
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets the process ID.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the memory usage information.
    /// </summary>
    public ResourceUsageViewModel MemoryUsage { get; } = new();

    /// <summary>
    /// Gets the display name of the process.
    /// </summary>
    public string ProcessName { get; }

    /// <summary>
    /// Gets the running time of the process.
    /// </summary>
    public TimeSpan RunningTime
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets the thread count information.
    /// </summary>
    public ResourceUsageViewModel ThreadCount { get; } = new();

    /// <summary>
    /// Gets the timestamps of the recorded data points.
    /// </summary>
    public IReadOnlyList<DateTime> TimeStamps => _timeStamps;

    #endregion

    private readonly Process _process;
    private readonly List<DateTime> _timeStamps = [];

    public ProcessInfoViewModel(Process process)
    {
        _process = process;

        Id = process.Id;
        ProcessName = _process.ProcessName;
    }

    #region Public Methods

    #region UpdateProcessInfo
    /// <summary>
    /// Updates the process information by refreshing the process data and recording the current metrics.
    /// </summary>
    public void UpdateProcessInfo()
    {
        try
        {
            _process.Refresh();

            _timeStamps.Add(DateTime.Now);
            HandleCount.AddValue(_process.HandleCount);
            MemoryUsage.AddValue(_process.WorkingSet64);
            ThreadCount.AddValue(_process.Threads.Count);

            RunningTime = DateTime.Now - _process.StartTime;
        }
        catch(Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }
    #endregion

    #endregion
}