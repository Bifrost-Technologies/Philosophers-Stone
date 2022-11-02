using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Coinbase.Pro;


namespace Bifrost
{
    static class Program
    {
        public static CoinbaseProClient CBclient { get; private set; }
        //If you plan to use broadcasting then switch these variables to actual channel ids
        private static string telegram_chatid_daily = "TELEGRAM_CHANNEL_ID_HERE";
        private static string telegram_chatid_hourly = "TELEGRAM_CHANNEL_ID_HERE";
        private static string telegram_API_Key = "TELEGRAM BOT API KEY GOES HERE";
        private static bool broadcastToTelegram = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [MTAThread]
        static void Main()
        {
            try
            {
                if(broadcastToTelegram == true)
                    lapis_philosophorum.telegramBotClient = new TelegramBotClient(telegram_API_Key);
                Console.WriteLine("Alchemist is Online!");
                NeuralTree().Wait();
            }
            catch (Exception aa)
            {
                Console.WriteLine(aa);
                Console.ReadKey();
            }
        }
        /// <summary>
        /// Neural Tree
        /// </summary>
        public static async Task NeuralTree()
        {
            //lapis_philosophorum.lapis_philosophorum.ClearSignalsPulse();
            //lapis_philosophorum.lapis_philosophorum.ClearSignalsPulse2();
            //await Task.Delay(45000);

            //Neural-Tree
            Console.WriteLine("Watching 6 dimensions and waiting for the right time to forecast... Do not close me!");
            lapis_philosophorum.AnnounceForecast("BTC-USD", 7, 90, telegram_chatid_daily, broadcastToTelegram);
            lapis_philosophorum.AnnounceForecast("BTC-USD", 24, 288, telegram_chatid_hourly, broadcastToTelegram);
            lapis_philosophorum.AnnounceForecast("ETH-USD", 7, 90, telegram_chatid_daily, broadcastToTelegram);
            lapis_philosophorum.AnnounceForecast("ETH-USD", 24, 288, telegram_chatid_hourly, broadcastToTelegram);
            lapis_philosophorum.AnnounceForecast("SOL-USD", 7, 90, telegram_chatid_daily, broadcastToTelegram);
            await lapis_philosophorum.AnnounceForecast("SOL-USD", 24, 288, telegram_chatid_hourly, broadcastToTelegram);
        }
    }
}
