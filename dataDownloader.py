import yfinance as yf
import os

tickers = [
    "AAPL", "MSFT", "JPM", "V", "PG", "UNH", "HD", "KO", "DIS", "INTC",
    "IBM", "WMT", "AXP", "NKE", "MCD", "TRV", "BA", "CAT", "CSCO", "CVX",
    "GS", "HON", "JNJ", "MMM", "MRK", "WBA", "DOW", "AMGN", "VZ", "RTX"
]

start_date = "2024-08-01"
end_date = "2024-12-31"

os.makedirs("ProjetoCarteira/data", exist_ok=True)

for ticker in tickers:
    print(f"Downloading {ticker}...")
    df = yf.download(ticker, start=start_date, end=end_date, auto_adjust=False)

    if not df.empty:
        df.columns = df.columns.get_level_values(0)  # flatten multi-level headers
        df.reset_index(inplace=True)  # make 'Date' a column, not index
        df.to_csv(f"ProjetoCarteira/data/{ticker}.csv", index=False)
        print(f"✅ Saved: {ticker}.csv")
    else:
        print(f"⚠️ No data for {ticker}")
