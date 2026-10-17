using System.Globalization;

namespace AssurCompare.Core;

/// <summary>Affichage des montants au format suisse : CHF 1'234.50</summary>
public static class Chf
{
    public static string Format(decimal amount, bool withCents = true) =>
        "CHF " + Number(amount, withCents);

    public static string Number(decimal amount, bool withCents = true) =>
        amount.ToString(withCents ? "#,##0.00" : "#,##0", CultureInfo.InvariantCulture).Replace(',', '\'');
}
