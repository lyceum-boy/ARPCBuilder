
using System.Globalization;
using UnityEngine;

public static class DimensionFormatter
{
    private static readonly CultureInfo RussianCulture =
        CultureInfo.GetCultureInfo("ru-RU");

    public static string Centimeters(float meters)
    {
        return (meters * 100f).ToString(
            "F1", RussianCulture
        ) + " ρμ";
    }

    public static string Millimeters(float meters)
    {
        return Mathf.RoundToInt(meters * 1000f) + " μμ";
    }
}
