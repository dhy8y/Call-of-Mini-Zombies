using System;
using System.Globalization;

/// <summary>
/// Short money and multiplier text: 950, 1.4K, 15.3K, 153K, 5.7M, 1B.
/// Capital letters only, because the game's own font is an all-caps font.
/// </summary>
public static class ModFormat
{
	private static readonly string[] Units = new string[4] { "K", "M", "B", "T" };

	/// <summary>Whole amounts. Always rounds down, so it never shows more than you have.</summary>
	public static string Short(long value)
	{
		if (value < 0)
		{
			return "-" + Short(-value);
		}
		if (value < 1000)
		{
			return value.ToString(CultureInfo.InvariantCulture);
		}
		double scaled = value;
		int unit = -1;
		while (scaled >= 1000.0 && unit < Units.Length - 1)
		{
			scaled /= 1000.0;
			unit++;
		}
		if (scaled >= 100.0)
		{
			return Math.Floor(scaled).ToString("0", CultureInfo.InvariantCulture) + Units[unit];
		}
		double oneDecimal = Math.Floor(scaled * 10.0) / 10.0;
		return oneDecimal.ToString("0.#", CultureInfo.InvariantCulture) + Units[unit];
	}

	/// <summary>Multipliers: 1.25, 30.5, 931, 56K.</summary>
	public static string Multiplier(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value))
		{
			return "MAX";
		}
		if (value < 10.0)
		{
			return value.ToString("0.00", CultureInfo.InvariantCulture);
		}
		if (value < 100.0)
		{
			return value.ToString("0.0", CultureInfo.InvariantCulture);
		}
		if (value >= 9E+18)
		{
			return "MAX";
		}
		return Short((long)Math.Floor(value));
	}
}
