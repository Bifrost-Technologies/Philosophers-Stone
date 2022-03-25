using Coinbase.Commerce.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using lapis_philosophorum;
using Telegram.Bot;
using Telegram.Bot.Args;
using Coinbase.Pro;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace DimensionalForecastAI
{
    static class Program
    {
        public static CoinbaseProClient CBclient { get; private set; }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MTAThread]
        static void Main()
        {
            try
            {
                NeuralTree().Wait();
            }
            catch (Exception aa)
            {
                Console.WriteLine(aa);
                Console.ReadKey();
            }
        }
        public static async Task NeuralTree()
        {
            //lapis_philosophorum.lapis_philosophorum.ClearSignalsPulse();
            //lapis_philosophorum.lapis_philosophorum.ClearSignalsPulse2();
            //await Task.Delay(45000);
            var chatid_daily = "TELEGRAM_CHANNEL_ID_HERE";
            var chatid_hourly = "TELEGRAM_CHANNEL_ID_HERE";
            lapis_philosophorum.lapis_philosophorum.telegramBotClient = new TelegramBotClient("API_KEY_HERE");

            lapis_philosophorum.lapis_philosophorum.AnnounceForecast("BTC-USD", 7, 90, chatid_daily, false);
            lapis_philosophorum.lapis_philosophorum.AnnounceForecast("BTC-USD", 24, 288, chatid_hourly, false);
            lapis_philosophorum.lapis_philosophorum.AnnounceForecast("ETH-USD", 7, 90, chatid_daily, false);
            lapis_philosophorum.lapis_philosophorum.AnnounceForecast("ETH-USD", 24, 288, chatid_hourly, false);
            lapis_philosophorum.lapis_philosophorum.AnnounceForecast("SOL-USD", 7, 90, chatid_daily, false);
            //TreeTrunk
            await lapis_philosophorum.lapis_philosophorum.AnnounceForecast("SOL-USD", 24, 288, chatid_hourly, false);
        }
    }
}
