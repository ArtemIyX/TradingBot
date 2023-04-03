# Trading bot (ByBit)
This is a trading bot designed to help you trade cryptocurrencies on the ByBit exchange. The bot is designed to receive trading signals via webhook, send notifications via Telegram, and make orders on the ByBit exchange. It is also highly customizable, with flexible settings for each currency and the ability to use a TradingView strategy.
## Installation
To install the trading bot, you will need to follow these steps:|
1. Download the bot and place it on your Linux server. In the case of Windows, you will have to compile it yourself.
2. Make sure you have .net 6.0 installed on your machine.
3. Make the TradingBot file runable with **'chmod +x'**.
4. In the **'appsettings.json'** file, you will need to specify the following settings:
    - Where the logs should be writtenThe currencies to use and their settings
    - Whether the bot can cancel the order
    - The API key of the bot's Telegram account and the chat where it will send notifications
    - The API key and the secret key of the ByBit exchange
    - The percentage of each deal of the current balance and the leverage.
5. Run the process in the background with the Linux screen utility.
## Usage
The bot works with just one order at a time. If the bot receives a request for a second order (with a different currency), it will reject the request. If the cancel option is enabled, the order can be cancelled using the opposite side of the order

Once the bot is installed and configured, it will automatically receive trading signals, make orders, and send notifications via Telegram. You can customize the bot's settings to fit your trading strategy, and it will continue to work in the background as long as it is running on your server.

## Support
If you need help installing or using the trading bot, please reach out to us at [Telegram](https://t.me/wellsaikSignals)

We're always happy to help!
