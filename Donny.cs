/* Entry Long 
 * h3c, price above cog, grab colour is green.
 * 
 * Entry Short
 * l3c, price below cog, grab colour is red.
 * 
 * No Trade
 * price => cog 2atr value, stddev squeeze, grab colour is blue.
 * 
 * Exit
 * 0.5 position at 20 pips, scale out remainder with trail stop at 2atr above or below price.
 * 
 * Stop Loss
 * 2atr, 1%.
 * 
 * Alternate Stop
 * if before hitting normal 20 pip take profit, grab colour is blue, exit.
 */

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Donny : Robot
    {
        [Parameter("Stop Loss (pips)", DefaultValue = 10)]
        public int StopLoss { get; set; }

        [Parameter("Initial Take Profit (pips)", DefaultValue = 20)]
        public double TakeProfit1 { get; set; }

        [Parameter("Volume (percentage)", DefaultValue = 1.0)]
        public double Volume { get; set; }

        private LazyBearCOG lazyBearCog;
        private ExponentialMovingAverage emaHigh;
        private ExponentialMovingAverage emaLow;
        protected override void OnStart()
        {
            lazyBearCog = Indicators.GetIndicator<LazyBearCOG>(30, 2, 20);
            emaHigh = Indicators.ExponentialMovingAverage(MarketSeries.High, 35);
            emaLow = Indicators.ExponentialMovingAverage(MarketSeries.Low, 35);
            Print("Tell Donny you love him");
        }

        protected override void OnTick()
        {
            var longPosition = Positions.Find("Donny", Symbol.Name, TradeType.Buy);
            var shortPosition = Positions.Find("Donny", Symbol.Name, TradeType.Sell);

            var close = MarketSeries.Close.Last(1);
            var open = MarketSeries.Open.Last(1);
            var high = MarketSeries.High.Last(1);
            var lastHigh = MarketSeries.High.Last(2);
            var low = MarketSeries.Low.Last(1);
            var lastLow = MarketSeries.Low.Last(2);
            var hiMa = emaHigh.Result.Last(0);
            var loMa = emaLow.Result.Last(0);
            var lastClose = MarketSeries.Close.Last(2);
            var cog = lazyBearCog.MedianLine.Last(0);
            var upperLine = Math.Round(lazyBearCog.UpperLine.Last(1), 5);
            var lowerLine = Math.Round(lazyBearCog.LowerLine.Last(1), 5);
            var upperRange = Math.Round(lazyBearCog.UpperRange.Last(1), 5);
            var lowerRange = Math.Round(lazyBearCog.LowerRange.Last(1), 5);
            int volume = CalculateVolume();

            //entry logic
            // long
            if (upperLine > upperRange)
            {
                if (close > cog && close > lastClose && high > lastHigh && open > hiMa && close > hiMa && longPosition == null)
                {
                    if (shortPosition == null)
                        ExecuteMarketOrder(TradeType.Buy, Symbol.Name, volume, "Donny", StopLoss, null);
                }
            }
            // short 
            if (lowerLine < lowerRange)
            {
                if (close < cog && close < lastClose && low < lastLow && open < loMa && close < loMa && shortPosition == null)
                {
                    if (longPosition == null)
                        ExecuteMarketOrder(TradeType.Sell, Symbol.Name, volume, "Donny", StopLoss, null);
                }
            }
            //exit logic
            foreach (var position in Positions)
            {
                var reduce = Math.Round(position.VolumeInUnits / 2);
                int halfVolume = (int)Symbol.NormalizeVolumeInUnits(reduce, RoundingMode.ToNearest);
                var newStopLoss = position.EntryPrice;
                if (position.Pips >= TakeProfit1 && position.HasTrailingStop == false)
                {
                    ModifyPosition(position, halfVolume);
                    position.ModifyStopLossPrice(newStopLoss);
                    position.ModifyTrailingStop(true);
                }
                else if (longPosition != null && close <= hiMa)
                {
                    ClosePosition(position);
                }
                else if (shortPosition != null && close >= loMa)
                {
                    ClosePosition(position);
                }
            }

        }
        protected int CalculateVolume()
        {
            int volume;
            double costPerPip = (double)((int)(Symbol.PipValue * 10000000)) / 100;
            double posSizeForRisk = (Account.Balance * Volume / 100) / (StopLoss * costPerPip);
            double posSizeToVol = (Math.Round(posSizeForRisk, 2) * 100000);
            volume = (int)Symbol.NormalizeVolumeInUnits(posSizeToVol, RoundingMode.ToNearest);
            return volume;
        }
        protected override void OnStop()
        {
            // Closes all open positions on stop
            ClosePosition(Positions.Find(null, SymbolName));
        }
        protected override void OnError(Error error)
        {
            // Exception Handling
            Print("Error: ", error.Code);
            Stop();
        }

    }
}
