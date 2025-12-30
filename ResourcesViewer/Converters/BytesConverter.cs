using HelperMethods;
using System.Globalization;
using System.Windows.Data;

namespace ResourcesViewer.Converters;

public class BytesConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => 
        BytesHelper.ToBestUnitString((long)value, 2);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}