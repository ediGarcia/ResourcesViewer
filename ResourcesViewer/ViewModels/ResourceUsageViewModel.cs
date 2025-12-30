using CommunityToolkit.Mvvm.ComponentModel;

namespace ResourcesViewer.ViewModels;

public class ResourceUsageViewModel : ObservableObject
{
    #region Properties

    /// <summary>
    /// Gets the latest recorded value of the resource usage.
    /// </summary>
    public long LatestValue
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets the maximum recorded value of the resource usage.
    /// </summary>
    public long MaximumValue
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>
    /// Gets the current state of the resource usage.
    /// </summary>
    public ResourceUsageState State
    {
        get;
        private set => SetProperty(ref field, value);
    }

    #endregion

    private readonly List<long> _values = [];

    #region Public Methods

    #region AddValue
    /// <summary>
    /// Adds a new value to the resource usage statistics.
    /// </summary>
    /// <param name="value"></param>
    public void AddValue(long value)
    {
        if (value == LatestValue)
            State = ResourceUsageState.Stable;
        else if (value > LatestValue)
            State = ResourceUsageState.Increasing;
        else
            State = ResourceUsageState.Decreasing;

        LatestValue = value;

        if (value > MaximumValue)
            MaximumValue = value;

        _values.Add(value);
    }
    #endregion

    #region GetValues
    /// <summary>
    /// Retrieves the list of recorded resource usage values.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<long> GetValues() =>
    _values;
    #endregion

    #endregion
}