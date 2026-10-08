namespace STP.Lab1.Interfaces;

public interface IOrderCalc
{
    double ApplyDiscount(double total, bool isPremium);
    double CalcShipping(double total);
    double FinalPrice(double total, double discount, double shipping);
    int CountExpensiveItems(List<double>prices, double threshold);
    double ProductOddIndices(List<double>prices);
    int SumOddAboveMainDiagonal(List<List<double>> a);
}