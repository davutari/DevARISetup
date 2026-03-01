using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DevARIManager.Core.Models;

namespace DevARIManager.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isTrue = value is true;
        var colors = (parameter as string)?.Split('|') ?? ["#FF22C55E", "#FFEF4444"];
        var colorStr = isTrue ? colors[0] : (colors.Length > 1 ? colors[1] : "#FFEF4444");
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorStr);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.Gray);
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isTrue = value is true;
        var texts = (parameter as string)?.Split('|') ?? ["Evet", "Hayir"];
        return isTrue ? texts[0] : (texts.Length > 1 ? texts[1] : "Hayir");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class HealthStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is HealthStatus status ? status switch
        {
            HealthStatus.Healthy => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF22C55E")),
            HealthStatus.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAB308")),
            HealthStatus.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444")),
            _ => new SolidColorBrush(Colors.Gray)
        } : new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class HealthStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is HealthStatus status ? status switch
        {
            HealthStatus.Healthy => "CheckCircle",
            HealthStatus.Warning => "AlertCircle",
            HealthStatus.Error => "CloseCircle",
            _ => "HelpCircle"
        } : "HelpCircle";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorStr && !string.IsNullOrEmpty(colorStr))
        {
            try { return (Color)ColorConverter.ConvertFromString(colorStr); }
            catch { }
        }
        return Colors.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class IntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter is string p && p.Equals("invert", StringComparison.OrdinalIgnoreCase);
        var isNonZero = value is int i && i > 0;
        if (invert) isNonZero = !isNonZero;
        return isNonZero ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class EnvStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is EnvVarStatus status ? status switch
        {
            EnvVarStatus.Correct => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF22C55E")),
            EnvVarStatus.Missing => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEF4444")),
            EnvVarStatus.Incorrect => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEAB308")),
            EnvVarStatus.Optional => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B7280")),
            _ => new SolidColorBrush(Colors.Gray)
        } : new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
