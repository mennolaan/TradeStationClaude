namespace TradeStation.Extensions;

public static class MarketUtils
{
    public static bool IsMarketOpen()
    {
        var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var easternTime = TimeZoneInfo.ConvertTime(DateTime.Now, easternZone);

        return easternTime.DayOfWeek < DayOfWeek.Saturday &&
               easternTime.Hour >= 9 &&
               easternTime.Hour < 16;
    }
}