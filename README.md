Set ```appsettings.json```

```
{
    "Serilog": {
        "WriteTo": [
            {
                "Name": "LineNotify",
                "Args": {
                    "OutputTemplate": "{NewLine}DateTime:{Timestamp:yyyy/MM/dd HH:mm:ss},{NewLine}Log Level: {Level},{NewLine}EnvironmentName: {EnvironmentName},{NewLine}ApplicationName: {ApplicationName},{NewLine}RuntimeId: {RuntimeId},{NewLine}Message: {Message:lj},{NewLine}XCorrelationID: {XCorrelationID},{NewLine}Exception: {Exception}",
                    "LineNotifyTokens": [
                        // https://notify-bot.line.me/
                        // Line Notify token
                        "" 
                    ],
                    "MinutesForBlockDuplicatedLog": 10,
                    "RestrictedToMinimumLevel": "Error"
                }
            }
        ]
    }
}
```

Add nuget package ```Serilog.Settings.Configuration```

Apply ```appsettings.json```
```
IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional : false, reloadOnChange : true);
IConfiguration configuration = configurationBuilder.Build();

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
```