using System.Collections.Generic;
using NUnit.Framework;
using STP.Lab1;

namespace STPTests;

public class OrderCalcTests
{
    private OrderCalc _orderCalc;

    [SetUp]
    public void Setup()
    {
        _orderCalc = new OrderCalc();
    }

    //C0/C1/C2
    [TestCase(-1, false, ExpectedResult = -1)]
    [TestCase(4999, false, ExpectedResult = 4999)]
    [TestCase(5000, true, ExpectedResult = 4500)]
    [TestCase(5000, false, ExpectedResult = 4750)]
    [TestCase(10000, true, ExpectedResult = 8000)]
    [TestCase(10000, false, ExpectedResult = 8500)]
    public double ПроверяетсяДобавлениеСкидки(double total, bool isPremium)
    {
        return _orderCalc.ApplyDiscount(total, isPremium);
    }
    
    //C0/C1/C2
    [TestCase(-1, ExpectedResult = -1)]
    [TestCase(4999, ExpectedResult = 300)]
    [TestCase(5001, ExpectedResult = 0)]
    [TestCase(5000, ExpectedResult = 0)]
    public double ПроверяетсяРасчетСтоимостиДоставки(double total)
    {
        return _orderCalc.CalcShipping(total);
    }

    [TestCase(-1, 0, 0, ExpectedResult = -1)] //C0/C1/C2
    [TestCase(100, -1, 0, ExpectedResult = -1)] //C1/C2
    [TestCase(100, 0, -1, ExpectedResult = -1)] //C1/C2
    [TestCase(100, 200, 10, ExpectedResult = 0)] //C0/C1/C2
    [TestCase(100, 20, 30, ExpectedResult = 110)] //C1/C2
    [TestCase(100, 100, 0, ExpectedResult = 0)] 
    public double ПроверяетсяРасчетФинальнойСтоимости(double total, double discount, double shipping)
    {
        return _orderCalc.FinalPrice(total, discount, shipping);
    }

    [TestCase(new double[] { }, 0, ExpectedResult = 0)] //C2
    [TestCase(new double[] { 50 }, 100, ExpectedResult = 0)] //C1/C2
    [TestCase(new double[] { 100 }, 100, ExpectedResult = 0)]
    [TestCase(new double[] { 150 }, 100, ExpectedResult = 1)] //C0
    [TestCase(new double[] { 50, 100, 150 }, 100, ExpectedResult = 1)] //C0/C1
    [TestCase(new double[] { 150, 200, 300 }, 100, ExpectedResult = 3)] //C0/C2
    public int ПроверяетсяПодсчетКоличестваДорогихПредметовВКорзине(double[] prices, double threshold)
    {
        return _orderCalc.CountExpensiveItems([..prices], threshold);
    }


    [TestCase(new double[] { }, ExpectedResult = 1)] //C2
    [TestCase(new double[] { 7 }, ExpectedResult = 1)] //C1/C2
    [TestCase(new double[] { 10, 2 }, ExpectedResult = 2)] //C0
    [TestCase(new double[] { 0, 2, 0, 3 }, ExpectedResult = 6)] //C0/C1/C2
    public double ПроверяетсяОдномернаяФункция(double[] prices)
    {
        return _orderCalc.ProductOddIndices([..prices]);
    }

    [TestCaseSource(nameof(SumOddAboveMainDiagonalCases))]
    public int ПроверяетсяДвумернаяФункция(List<List<double>> a)
    {
        return _orderCalc.SumOddAboveMainDiagonal(a);
    }

    private static IEnumerable<TestCaseData> SumOddAboveMainDiagonalCases()
    {
        //C1/C2
        yield return new TestCaseData(new List<List<double>>())
            .Returns(0);

        //C1/C2
        yield return new TestCaseData(new List<List<double>>
        {
            new() { 7 }
        }).Returns(0);

        //C0
        yield return new TestCaseData(new List<List<double>>
        {
            new() { 1, 3 },
            new() { 5, 7 }
        }).Returns(3);

        //C1/C2
        yield return new TestCaseData(new List<List<double>>
        {
            new() { 1, 2 },
            new() { 3, 5 }
        }).Returns(0);

        //C0/C1
        yield return new TestCaseData(new List<List<double>>
        {
            new() { 101, 3, 4 },
            new() { 7, 103, 5 },
            new() { 9, 11, 105 }
        }).Returns(8);
    }
    
    [TestCaseSource(nameof(MaxPriceOddValueOddIndexCases))]
    public double ПроверяетсяМаксимальнаяНечётнаяЦена(List<double> prices)
    {
        return _orderCalc.MaxPriceOddValueOddIndex(prices);
    }

    private static IEnumerable<TestCaseData> MaxPriceOddValueOddIndexCases()
    {
        yield return new TestCaseData(new List<double>())
            .SetName("01_ПустойСписок_ВозвращаетсяНоль")
            .SetDescription("Путь: 1-2-3-10(Нет)-11")
            .Returns(0.0);

        yield return new TestCaseData(new List<double> { 99, 2.4 })
            .SetName("02_ЧётнаяЦелаяЧасть_ЭлементПропускается")
            .SetDescription("Путь: 1-2-3-4-5-9-3-10(Нет)-11")
            .Returns(0.0);

        yield return new TestCaseData(new List<double> { 99, -3.7 })
            .SetName("03_ПерваяПодходящаяЦена_ПринимаетсяДажеОтрицательная")
            .SetDescription("Путь: 1-2-3-4-5-6-8-9-3-10(Да)-11")
            .Returns(-3.7);

        yield return new TestCaseData(new List<double> { 99, 2.4, 101, 4.8 })
            .SetName("04_ОбеЦелыеЧастиЧётные_ВозвращаетсяНоль")
            .SetDescription("Путь: 1-2-3-4-5-9-3-4-5-9-3-10(Нет)-11")
            .Returns(0.0);

        yield return new TestCaseData(new List<double> { 99, 2.4, 101, -3.7 })
            .SetName("05_ПервыйЭлементПропущен_ВторойСтановитсяМаксимумом")
            .SetDescription("Путь: 1-2-3-4-5-9-3-4-5-6-8-9-3-10(Да)-11")
            .Returns(-3.7);

        yield return new TestCaseData(new List<double> { 99, -3.7, 101, 2.4 })
            .SetName("06_МаксимумНайден_СледующаяЧётнаяЦелаяЧастьПропускается")
            .SetDescription("Путь: 1-2-3-4-5-6-8-9-3-4-5-9-3-10(Да)-11")
            .Returns(-3.7);

        yield return new TestCaseData(new List<double> { 99, -3.7, 101, -1.9 })
            .SetName("07_СледующаяПодходящаяЦенаБольше_МаксимумОбновляется")
            .SetDescription("Путь: 1-2-3-4-5-6-8-9-3-4-5-6-7-8-9-3-10(Да)-11")
            .Returns(-1.9);

        yield return new TestCaseData(new List<double> { 99, 5.9, 101, 3.7 })
            .SetName("08_СледующаяПодходящаяЦенаМеньше_МаксимумСохраняется")
            .SetDescription("Путь: 1-2-3-4-5-6-8-9-3-4-5-6-7-9-3-10(Да)-11")
            .Returns(5.9);
    }
    
    [TestCaseSource(nameof(SumEvenAboveSecondaryDiagonalCases))]
    public int ПроверяетсяСуммаЧётныхВышеПобочнойДиагонали(
        List<List<int>> a)
    {
        return _orderCalc.SumEvenAboveSecondaryDiagonal(a);
    }

    private static IEnumerable<TestCaseData> SumEvenAboveSecondaryDiagonalCases()
    {
        yield return new TestCaseData(new List<List<int>>())
            .SetName("01_ПустаяМатрица_ВозвращаетсяНоль")
            .SetDescription("Путь: 1-2-3-4-11")
            .Returns(0);

        yield return new TestCaseData(new List<List<int>>
            {
                new() { 8 }
            })
            .SetName("02_ЭлементНаПобочнойДиагонали_НеУчитывается")
            .SetDescription("Путь: 1-2-3-4-5-6-10-4-11")
            .Returns(0);

        yield return new TestCaseData(new List<List<int>>
            {
                new() { -4, 100 },
                new() { 200, 300 }
            })
            .SetName("03_C0_ЧётныйЭлементВышеПобочнойДиагонали_Добавляется")
            .SetDescription("Путь: 1-2-3-4-5-6-7-8-9-6-10-4-5-6-10-4-11")
            .Returns(-4);

        yield return new TestCaseData(new List<List<int>>
            {
                new() { 3, 100 },
                new() { 200, 300 }
            })
            .SetName("04_НечётныйЭлементВышеПобочнойДиагонали_Пропускается")
            .SetDescription("Путь: 1-2-3-4-5-6-7-9-6-10-4-5-6-10-4-11")
            .Returns(0);

        yield return new TestCaseData(new List<List<int>>
            {
                new() {   2,   3, 100 },
                new() {  -4, 200, 300 },
                new() { 400, 500, 600 }
            })
            .SetName("05_НесколькоЧётныхЭлементов_Суммируются")
            .SetDescription(
                "Путь: 1-2-3-4-5-6-7-8-9-6-7-9-6-10" +
                "-4-5-6-7-8-9-6-10-4-5-6-10-4-11")
            .Returns(-2);
    }
}