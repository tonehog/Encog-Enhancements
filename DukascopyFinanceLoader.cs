#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Encog.MathUtil;
using Encog.Util.HTTP;
using Encog.Util;
using Encog.Util.CSV;
using System.Net;

namespace Encog.Neural.NeuralData.Market.Loader
{
    /// <summary>
    /// This class loads Foreign Exchange data from Oanda.
    /// </summary>
    // TODO: needs to parse <PRE> tag from resulted data (not clean CSV)
    class OandaFinanceLoader : IMarketLoader
    {
        /// <summary>
        /// This method builds a URL to load data from Oanda for a neural
        /// network to train with.
        /// </summary>
        /// <param name="ticker">The currency pair to access.</param>
        /// <param name="from">The begin date.</param>
        /// <param name="to">the ending date.</param>
        /// <returns>The URL to read from.</returns>
        private Uri buildURL(
            TickerSymbol ticker,
            DateTime from, 
           DateTime to
        )
        {
            // construct the url
            MemoryStream mstream = new MemoryStream();
            FormUtility form = new FormUtility(mstream, null);

            String[] currencies = ticker.Symbol.Split('/');

            // each param gets added individually as query parameter
            form.Add("exch", currencies[0].ToUpper());
            form.Add("expr2", currencies[1].ToUpper());
            form.Add("date1", from.ToString("MM-dd-yyyy"));
            form.Add("date2", to.ToString("MM-dd-yyyy"));
            form.Add("date_fmt", "us");
            form.Add("lang","en");
            form.Add("margin_fixed", "0");
            form.Add("SUBMIT", "Get+Table");
            form.Add("format", "CSV");
            form.Add("redirected", "1");
            mstream.Close();
            byte[] b = mstream.GetBuffer();

            String str = "http://www.oanda.com/convert/fxhistory?"
                + StringUtil.FromBytes(b);
            return new Uri(str);
        }

        /// <summary>
        /// Loads the specified financial data.
        /// </summary>
        /// <param name="ticker">The currency pair to load.</param>
        /// <param name="dataNeeded">The financial data needed.</param>
        /// <param name="from">The beginning date to load data from.</param>
        /// <param name="to">The ending date to load data to.</param>
        /// <returns>A collection of LoadedMarketData objects that represent
        /// the data loaded.</returns>
        public ICollection<LoadedMarketData> Load(
            TickerSymbol ticker,
            IList<MarketDataType> dataNeeded,
            DateTime from,
            DateTime to
        )
        {
            ICollection<LoadedMarketData> result =
                new List<LoadedMarketData>();
            Uri url = buildURL(ticker, from, to);
            WebRequest http = HttpWebRequest.Create(url);
            HttpWebResponse response = http.GetResponse() as HttpWebResponse;

            using (Stream istream = response.GetResponseStream())
            {
                ReadCSV csv =
                    new ReadCSV(istream, true, CSVFormat.DECIMAL_POINT);
                while (csv.Next())
                {
                    // TODO: check these values if possible
                    DateTime date = csv.GetDate("date");
                    double adjClose = csv.GetDouble("adj close"); // TODO: deprecate?
                    double open = csv.GetDouble("open");
                    double close = csv.GetDouble("close");
                    double high = csv.GetDouble("high");
                    double low = csv.GetDouble("low");
                    double volume = csv.GetDouble("volume"); // TODO: deprecate?

                    LoadedMarketData data =
                        new LoadedMarketData(date, ticker);
                    data.SetData(MarketDataType.ADJUSTED_CLOSE, adjClose);
                    data.SetData(MarketDataType.OPEN, open);
                    data.SetData(MarketDataType.CLOSE, close);
                    data.SetData(MarketDataType.HIGH, high);
                    data.SetData(MarketDataType.LOW, low);
                    data.SetData(MarketDataType.OPEN, open);
                    data.SetData(MarketDataType.VOLUME, volume);
                    result.Add(data);
                }

                csv.Close();
                istream.Close();
            }
            return result;
        }
    }

    class DukascopyLoader : IMarketLoader
    {
        /// <summary>
        /// This is a Dictionary<string, int> of currency pairs and the IDs
        /// used in building a Dukascopy query.
        /// </summary>
        /// <example>int currencyPair = DukascopyData["EURUSD"];</example>
        private Dictionary<string, int> DukascopyData =
            new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DukascopyLoader"/> class.
        /// </summary>
        public DukascopyLoader()
        {
            DukascopyData.Add("AUDJPY", 60  );
            DukascopyData.Add("AUDUSD", 10  );
            DukascopyData.Add("CADJPY", 767 );
            DukascopyData.Add("CHFJPY", 521 );
            DukascopyData.Add("EURCHF", 511 );
            DukascopyData.Add("EURGBP", 510 );
            DukascopyData.Add("EURJPY", 509 );
            DukascopyData.Add("EURUSD", 1   );
            DukascopyData.Add("GBPCHF", 518 );
            DukascopyData.Add("GBPEUR", 516 );
            DukascopyData.Add("GBPJPY", 517 );
            DukascopyData.Add("GBPUSD", 2   );
            DukascopyData.Add("JPYCHF", 515 );
            DukascopyData.Add("NZDUSD", 11  );
            // TODO: UNSURE OF THESE TWO COMMODITY CROSSES YET - CONFIRM
            DukascopyData.Add("XPDUSD", 336 );
            DukascopyData.Add("XPTUSD", 335 );
            DukascopyData.Add("USDCAD", 9   );
            DukascopyData.Add("USDCHF", 3   );
            DukascopyData.Add("USDJPY", 4   );
            DukascopyData.Add("XAGUSD", 334 );
            DukascopyData.Add("XAUUSD", 333 );
        }

        private Uri BuildURL(
            TickerSymbol ticker,
            DateTime from,
            DateTime to
        )
        {
            int selectedPair = -1;
            Uri url;

            #region Select Currency Pair
            switch (ticker.Symbol)
            {
                case "AUDJPY":
                    selectedPair = DukascopyData["AUDJPY"];
                    break;
                case "AUDUSD":
                    selectedPair = DukascopyData["AUDUSD"];
                    break;
                case "CADJPY":
                    selectedPair = DukascopyData["CADJPY"];
                    break;
                case "CHFJPY":
                    selectedPair = DukascopyData["CHFJPY"];
                    break;
                case "EURCHF":
                    selectedPair = DukascopyData["EURCHF"];
                    break;
                case "EURGBP":
                    selectedPair = DukascopyData["EURGBP"];
                    break;
                case "EURJPY":
                    selectedPair = DukascopyData["EURJPY"];
                    break;
                case "EURUSD":
                    selectedPair = DukascopyData["EURUSD"];
                    break;
                case "GBPEUR":
                    selectedPair = DukascopyData["GBPEUR"];
                    break;
                case "GBPJPY":
                    selectedPair = DukascopyData["GBPJPY"];
                    break;
                case "GBPUSD":
                    selectedPair = DukascopyData["GBPUSD"];
                    break;
                case "JPYCHF":
                    selectedPair = DukascopyData["JPYCHF"];
                    break;
                case "NZDUSD":
                    selectedPair = DukascopyData["NZDUSD"];
                    break;
                case "XPDUSD":
                    selectedPair = DukascopyData["XPDUSD"];
                    break;
                case "XPTUSD":
                    selectedPair = DukascopyData["XPTUSD"];
                    break;
                case "USDCAD":
                    selectedPair = DukascopyData["USDCAD"];
                    break;
                case "USDCHF":
                    selectedPair = DukascopyData["USDCHF"];
                    break;
                case "USDJPY":
                    selectedPair = DukascopyData["USDJPY"];
                    break;
                case "XAGUSD":
                    selectedPair = DukascopyData["XAGUSD"];
                    break;
                case "XAUUSD":
                    selectedPair = DukascopyData["XAUUSD"];
                    break;
                default:
                    break;
            }
            #endregion Select Currency Pair

            if (selectedPair != -1)
            {
                /*
                 * fromD    =   mm.dd.yyyy
                 * np       =   259, 1000, 1500, 2000
                 * interval =   60, 600, 3600, 1D, 7D, 1MO
                 * DF       =   m/d/Y, m.d.Y, d.m.Y, m-d-Y, d-m-Y
                 * endSym   =   win, unix
                 * split    =   tz, coma, tab (; , tab)
                 * 
                 */

                // construck URL
                MemoryStream mstream = new MemoryStream();
                FormUtility form = new FormUtility(mstream, null);

                form.Add("Stock", selectedPair.ToString());
                form.Add("fromD", from.ToString("mm.DD.yyyy"));
                form.Add("np", "2000");
                form.Add("interval", "1D");
                form.Add("DF", "m-d-Y"); // date format
                form.Add("endSym", "win");
                form.Add("split", "coma");

                mstream.Close();
                byte[] b = mstream.GetBuffer();

                String str = "http://www.dukascopy.com/freeApplets/exp/exp.php?" +
                    StringUtil.FromBytes(b);
                url = new Uri(str);
            }
            return url;
        }

        public ICollection<LoadedMarketData> Load(
            TickerSymbol ticker,
            IList<MarketDataType> dataNeeded,
            DateTime from,
            DateTime to)
        {
            // TODO: nyyyyyyyaaagh!

            ICollection<LoadedMarketData> result =
                new List<LoadedMarketData>();
            Uri url = BuildURL(ticker, from, to);
            WebRequest http = HttpWebRequest.Create(url);
            HttpWebResponse response = http.GetResponse() as HttpWebResponse;

            using (Stream istream = response.GetResponseStream())
            {
                ReadCSV csv = new ReadCSV(
                    istream,
                    true,
                    CSVFormat.DECIMAL_POINT
                );

                while (csv.Next())
                {
                    // todo: edit headers to match
                    DateTime date = csv.GetDate("DATE");
                    date =
                        date.Add(
                            new TimeSpan(
                                csv.GetDate("TIME").Hour,
                                csv.GetDate("TIME").Minute,
                                csv.GetDate("TIME").Second
                            )
                        );
                    double open = csv.GetDouble("OPEN");
                    double high = csv.GetDouble("MIN");
                    double low = csv.GetDouble("MAX");
                    double close = csv.GetDouble("CLOSE");
                    double volume = csv.GetDouble("VOLUME");

                    LoadedMarketData data =
                        new LoadedMarketData(date, ticker);
                    data.SetData(MarketDataType.OPEN, open);
                    data.SetData(MarketDataType.HIGH, high);
                    data.SetData(MarketDataType.LOW, low);
                    data.SetData(MarketDataType.CLOSE, close);
                    data.SetData(MarketDataType.VOLUME, volume);
                    result.Add(data);
                }

                csv.Close();
                istream.Close();
            }
            return result;
        }
    }
}




#endif