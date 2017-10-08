using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using eToro.Trading;
using eToro.Trading.Rates;
using eToro.Trading.Trade;

namespace TradingAPIExample
{
    class Program
    {
        /// <summary>
        /// Your API Key (from the API protal)
        /// </summary>
        private const string APIKey = "b9f5fd2d351a4402a8da66196db00b70";

        /// <summary>
        /// Your eToro username
        /// </summary>
        private const string Username = "LeTraderNo1";

        /// <summary>
        /// Your eToro passwprd
        /// </summary>
        private const string Password = "danny123";

        /// <summary>
        /// Instrument ID of Bitcoin
        /// </summary>
        private const int BitCoinId = 100000;

        /// <summary>
        /// Instrument ID of Etherium
        /// </summary>
        private const int EtheriumId = 100001;


        static void Main(string[] args)
        {
            try
            {
                //
                // Create the login context
                //
                var Context = new LoginProvider().Create(Username, Password, APIKey);

                //
                // Get the rate provider and set a handler for incoming rates
                //
                RateType LastBitcoinRate = null;

                var Rates = RateFactory.Create(Context);
                Rates.OnRate += Rate =>
                {
                    Console.WriteLine($"{Rate.InstrumentId} {Rate.Bid}/{Rate.Ask}");

                    if (Rate.InstrumentId == BitCoinId)
                        LastBitcoinRate = Rate;
                };

                //
                // Subscribe to the rates I'm interested in
                //
                Rates.Subscribe(new List<int>() {BitCoinId, EtheriumId});

                //
                // Now Initiate the trade provider (using virtual money)
                //
                var Trading = TradeFactory.Create(Context, TradeFactory.AccountType.Virtual);

                //
                // Start by creating handlers for all the events
                //
                Trading.OnExitOrderExecutionReport += ExecutionReport =>
                {
                    if (ExecutionReport.Success)
                        Console.WriteLine($"Exit Order {ExecutionReport.ExecutionId} was executed");
                    else
                        Console.WriteLine($"Exit Order {ExecutionReport.ExecutionId} failed with {ExecutionReport.TradeError}");
                };
                Trading.OnCloseTradeExecutionReport += ExecutionReport =>
                {
                    if (ExecutionReport.Success)
                        Console.WriteLine($"Close trade {ExecutionReport.ExecutionId} for position {ExecutionReport.PositionId} closed with a P&L of {ExecutionReport.NetProfit}");
                    else
                        Console.WriteLine($"Close trade {ExecutionReport.ExecutionId} for position {ExecutionReport.PositionId} failed with {ExecutionReport.TradeError}");
                };
                Trading.OnEntryOrderExecutionReport += ExecutionReport =>
                {
                    if (ExecutionReport.Success)
                        Console.WriteLine($"Order {ExecutionReport.ExecutionId} has been executed @ {ExecutionReport.ExecutionRate}");
                    else
                        Console.WriteLine($"Order {ExecutionReport.ExecutionId} failed with return code of {ExecutionReport.TradeError}");
                };
                Trading.OnEditTradeExecutionReport += ExecutionReport =>
                {
                    if (ExecutionReport.Success)
                        Console.WriteLine($"Edit Trade {ExecutionReport.ExecutionId} Success");
                    else
                        Console.WriteLine($"Edit Trade {ExecutionReport.ExecutionId} failed with {ExecutionReport.TradeError}");
                };

                //
                // Get my existing portfolio, and clear it
                //
                var Positions = Trading.Positions();
                foreach (var Position in Positions)
                {
                    var ExecutionId = Trading.CreateExitOrder(Position.PositionId, ExecutionMode.IOC);
                    Console.WriteLine($"Closing {Position.PositionId} on {Position.InstrumentId} with {ExecutionId}");
                }

                //
                // And open an entry order for bitcoin (wait till we have a bitcoin rate)
                //
                while (LastBitcoinRate == null)
                {
                    Thread.Sleep(500);
                }

                var Order = new EntryOrderMarketIOC(BitCoinId, true, 1, 1000, LastBitcoinRate.Ask * 0.8M, LastBitcoinRate.Ask * 1.2M);
                var OrderId = Trading.CreateEntryOrder(Order);
                Console.WriteLine($"Order {OrderId} has been created");

                Console.ReadKey();
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message);
            }
        }
    }
}
