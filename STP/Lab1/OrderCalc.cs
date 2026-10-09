using STP.Lab1.Interfaces;

namespace STP.Lab1;

public class OrderCalc : IOrderCalc
{
    public double ApplyDiscount(double total, bool isPremium)
    {
        return total switch
        {
            < 0 => -1.0,
            >= 10000 when isPremium => total * 0.80,
            >= 10000 => total * 0.85,
            >= 5000 when isPremium => total * 0.90,
            >= 5000 => total * 0.95,
            _ => total
        };
    }

    public double CalcShipping(double total)
    {
        return total switch
        {
            < 0 => -1.0,
            >= 5000 => 0.0,
            _ => 300.0
        };
    }

    public double FinalPrice(double total, double discount, double shipping)
    {
        if (total < 0 || discount < 0 || shipping < 0)
        {
            return -1.0;
        }

        var result = total - discount + shipping;

        if (result < 0)
        {
            result = 0.0;
        }

        return Math.Round(result * 100) / 100;
    }

    public int CountExpensiveItems(List<double> prices, double threshold)
    {
        return prices.Count(t => t > threshold);
    }

    public double ProductOddIndices(List<double> prices)
    {
        return prices.Where((_, i) => i % 2 != 0).Aggregate(1.0, (product, price) => product * price);
    }

    public int SumOddAboveMainDiagonal(List<List<double>> a)
    {
        var sum = 0.0;

        for (var i = 0; i < a.Count; i++)
        {
            for (var j = i + 1; j < a[i].Count; j++)
            {
                if (a[i][j] % 2 != 0)
                {
                    sum += a[i][j];
                }
            }
        }

        return (int)sum;
    }

    public double MaxPriceOddValueOddIndex(List<double> prices)
    {
        double? max = null;

        for (var i = 1; i < prices.Count; i += 2)
        {
            var price = prices[i];

            if (Math.Truncate(price) % 2 != 0 && (!max.HasValue || price > max.Value))
            {
                max = price;
            }
        }

        return max ?? 0.0;
    }

    public int SumEvenAboveSecondaryDiagonal(List<List<int>> a)
    {
        var n = a.Count;
        var sum = 0;

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n - i - 1; j++)
            {
                if (a[i][j] % 2 == 0)
                {
                    sum += a[i][j];
                }
            }
        }

        return sum;
    }
}